using System.Diagnostics;
using System.Text.Json;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Text;
using FrankenTui.Web;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Testing.Harness;

public sealed record BenchmarkBudget(
    string Name,
    string UpstreamKey,
    string Description,
    double MeanBudgetNs,
    double P95BudgetNs,
    int Iterations);

public sealed record BenchmarkMeasurement(
    string Name,
    string Description,
    string UpstreamKey,
    double MeanNs,
    double P95Ns,
    double MaxNs,
    int Iterations);

public sealed record BenchmarkSuiteResult(IReadOnlyList<BenchmarkMeasurement> Measurements)
{
    public string ToJson() =>
        JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public IReadOnlyList<string> ValidateAgainst(IReadOnlyList<BenchmarkBudget> budgets)
    {
        var errors = new List<string>();
        foreach (var budget in budgets)
        {
            var measurement = Measurements.FirstOrDefault(candidate => string.Equals(candidate.Name, budget.Name, StringComparison.Ordinal));
            if (measurement is null)
            {
                errors.Add($"Missing benchmark measurement for {budget.Name}.");
                continue;
            }

            if (measurement.MeanNs > budget.MeanBudgetNs)
            {
                errors.Add($"{budget.Name} mean {measurement.MeanNs:0}ns exceeded budget {budget.MeanBudgetNs:0}ns.");
            }

            if (measurement.P95Ns > budget.P95BudgetNs)
            {
                errors.Add($"{budget.Name} p95 {measurement.P95Ns:0}ns exceeded budget {budget.P95BudgetNs:0}ns.");
            }
        }

        return errors;
    }

    public string WriteArtifacts(string fileName = "latest-benchmarks.json")
    {
        var path = ArtifactPathBuilder.For("benchmarks", fileName);
        File.WriteAllText(path, ToJson());
        return path;
    }
}

public static class PerformanceBenchmarkRunner
{
    public static string DefaultBudgetPath =>
        Path.Combine(RepositoryPaths.FindRepositoryRoot(), "tests", "fixtures", "358-vrf-performance-baseline.json");

    public static IReadOnlyList<BenchmarkBudget> LoadBudgets(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return JsonSerializer.Deserialize<List<BenchmarkBudget>>(File.ReadAllText(path), HarnessJson.SnakeCase) ?? [];
    }

    public static BenchmarkSuiteResult RunDefault(IReadOnlyList<BenchmarkBudget> budgets)
    {
        ArgumentNullException.ThrowIfNull(budgets);

        var measurements = new List<BenchmarkMeasurement>();
        foreach (var budget in budgets)
        {
            measurements.Add(
                budget.Name switch
                {
                    "ansi_emit" => Measure(budget, AnsiEmitBenchmark),
                    "buffer_new_80x24" => Measure(budget, BufferNewBenchmark),
                    "diff_strategy" => Measure(budget, DiffStrategyBenchmark),
                    "frame_render" => Measure(budget, FrameRenderBenchmark),
                    "layout_computation" => Measure(budget, LayoutBenchmark),
                    "text_wrap_word" => Measure(budget, TextWrapBenchmark),
                    "runtime_dispatch" => Measure(budget, RuntimeDispatchBenchmark),
                    "widget_render_block" => Measure(budget, WidgetRenderBlockBenchmark),
                    "widget_render_table" => Measure(budget, WidgetRenderBenchmark),
                    "web_document" => Measure(budget, WebDocumentBenchmark),
                    _ => throw new InvalidOperationException($"Unknown benchmark '{budget.Name}'.")
                });
        }

        return new BenchmarkSuiteResult(measurements);
    }

    private static BenchmarkMeasurement Measure(BenchmarkBudget budget, Action action)
    {
        for (var warmup = 0; warmup < 5; warmup++)
        {
            action();
        }

        var samples = new double[budget.Iterations];
        for (var index = 0; index < budget.Iterations; index++)
        {
            var started = Stopwatch.GetTimestamp();
            action();
            var elapsed = Stopwatch.GetTimestamp() - started;
            samples[index] = elapsed * (1_000_000_000d / Stopwatch.Frequency);
        }

        Array.Sort(samples);
        return new BenchmarkMeasurement(
            budget.Name,
            budget.Description,
            budget.UpstreamKey,
            samples.Average(),
            Percentile(samples, 0.95),
            samples[^1],
            budget.Iterations);
    }

    public static (BenchmarkSuiteResult Suite, IReadOnlyList<string> Errors) RunGate(IReadOnlyList<BenchmarkBudget> budgets)
    {
        ArgumentNullException.ThrowIfNull(budgets);

        var suite = RunDefault(budgets);
        return (suite, suite.ValidateAgainst(budgets));
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Clamp(Math.Ceiling(sortedValues.Count * percentile) - 1, 0, sortedValues.Count - 1);
        return sortedValues[index];
    }

    private static void FrameRenderBenchmark()
    {
        var previous = new RenderBuffer(80, 24);
        var next = new RenderBuffer(80, 24);
        var template = UiStyle.Accent.ToCell('x');
        for (ushort row = 0; row < 24; row += 5)
        {
            BufferPainter.WriteText(next, 2, row, "frankentui", template);
        }

        var diff = BufferDiff.Compute(previous, next);
        var presenter = new Presenter(TerminalCapabilities.Modern());
        var result = presenter.Present(next, diff);
        GC.KeepAlive(result);
    }

    private static void DiffStrategyBenchmark()
    {
        var previous = new RenderBuffer(80, 24);
        var next = new RenderBuffer(80, 24);
        var template = UiStyle.Accent.ToCell('x');
        for (ushort row = 0; row < 24; row += 5)
        {
            BufferPainter.WriteText(next, 2, row, "frankentui", template);
        }

        var diff = BufferDiff.Compute(previous, next);
        GC.KeepAlive(diff);
    }

    private static void AnsiEmitBenchmark()
    {
        var previous = new RenderBuffer(80, 24);
        var next = new RenderBuffer(80, 24);
        var template = UiStyle.Accent.ToCell('x');
        for (ushort row = 0; row < 24; row += 5)
        {
            BufferPainter.WriteText(next, 2, row, "frankentui", template);
        }

        var diff = BufferDiff.Compute(previous, next);
        var presenter = new Presenter(TerminalCapabilities.Modern());
        var result = presenter.Present(next, diff);
        GC.KeepAlive(result.Output);
    }

    private static void BufferNewBenchmark()
    {
        var buffer = new RenderBuffer(80, 24);
        GC.KeepAlive(buffer);
    }

    private static void LayoutBenchmark()
    {
        _ = LayoutSolver.Split(
            new Rect(0, 0, 120, 30),
            LayoutDirection.Horizontal,
            [
                LayoutConstraint.Fixed(8),
                LayoutConstraint.Fill(),
                LayoutConstraint.Fixed(6),
                LayoutConstraint.Percentage(20),
                LayoutConstraint.Fill(),
                LayoutConstraint.Fixed(4),
                LayoutConstraint.Percentage(15),
                LayoutConstraint.Fill(),
                LayoutConstraint.Fixed(3),
                LayoutConstraint.Fill()
            ]);
    }

    private static void TextWrapBenchmark()
    {
        var document = TextDocument.FromString("alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu");
        var lines = TextWrapper.Wrap(document, 18, TextWrapMode.Word);
        GC.KeepAlive(lines);
    }

    private static void RuntimeDispatchBenchmark()
    {
        var simulator = new AppSimulator<int, string>(new Size(40, 12), Theme.DefaultTheme);
        var result = simulator.DispatchAsync(new BenchmarkProgram(), 0, "inc").AsTask().GetAwaiter().GetResult();
        GC.KeepAlive(result);
    }

    private static void WidgetRenderBlockBenchmark()
    {
        var buffer = new RenderBuffer(80, 24);
        var widget = new PanelWidget
        {
            Title = "Block",
            Child = new ParagraphWidget("Hosted parity block benchmark")
        };
        widget.Render(new RuntimeRenderContext(buffer, Rect.FromSize(80, 24), Theme.DefaultTheme));
    }

    private static void WidgetRenderBenchmark()
    {
        var buffer = new RenderBuffer(80, 24);
        var table = new TableWidget
        {
            Headers = ["Metric", "Value", "State"],
            Rows =
            [
                new[] { "render", "ok", "warm" },
                new[] { "layout", "ok", "warm" },
                new[] { "runtime", "ok", "warm" },
                new[] { "widgets", "ok", "warm" }
            ],
            SelectedRow = 1,
            FocusedRow = 1
        };
        table.Render(new RuntimeRenderContext(buffer, Rect.FromSize(80, 24), Theme.DefaultTheme));
    }

    private static void WebDocumentBenchmark()
    {
        var session = HostedParitySession.ForFrame(false, 2, HostedParityScenarioId.Tooling);
        var view = HostedParitySurface.Create(session);
        var frame = WebHost.Render(view, new Size(64, 18), options: HostedParitySurface.CreateWebOptions(session));
        GC.KeepAlive(frame);
    }

    private sealed class BenchmarkProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(message == "inc" ? model + 1 : model);

        public IRuntimeView BuildView(int model) =>
            new PanelWidget
            {
                Title = "Bench",
                Child = new ParagraphWidget($"Value: {model}")
            };
    }
}
