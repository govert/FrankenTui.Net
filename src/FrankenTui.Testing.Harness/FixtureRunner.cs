using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Testing.Harness;

public sealed record FixtureExecution(
    IReadOnlyList<Sample> Samples,
    bool Passed,
    int FramesCompared,
    int FramesMatched,
    IReadOnlyList<string>? Notes = null);

public sealed record FixtureRunReport(
    FixtureSpec Fixture,
    FixtureBaseline Baseline,
    bool Passed,
    int FramesCompared,
    int FramesMatched,
    IReadOnlyList<string> Notes)
{
    public double MatchRatio => FramesCompared == 0 ? (Passed ? 1d : 0d) : FramesMatched / (double)FramesCompared;
}

public sealed record FixtureSuiteReport(IReadOnlyList<FixtureRunReport> Fixtures)
{
    public int PassedCount => Fixtures.Count(static fixture => fixture.Passed);

    public int TotalFramesCompared => Fixtures.Sum(static fixture => fixture.FramesCompared);

    public int TotalFramesMatched => Fixtures.Sum(static fixture => fixture.FramesMatched);
}

public sealed class FixtureRunner
{
    private readonly Func<FixtureSpec, FixtureExecution> _executor;

    public FixtureRunner(FixtureRegistry registry, Func<FixtureSpec, FixtureExecution>? executor = null)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _executor = executor ?? ExecuteDeterministic;
    }

    public FixtureRegistry Registry { get; }

    public FixtureSuiteReport RunAll() => Run(Registry.Fixtures);

    public FixtureSuiteReport RunPartition(SuitePartition partition) => Run(Registry.ByPartition(partition));

    public FixtureSuiteReport RunFamily(FixtureFamily family) => Run(Registry.ByFamily(family));

    private FixtureSuiteReport Run(IEnumerable<FixtureSpec> fixtures)
    {
        var reports = new List<FixtureRunReport>();
        foreach (var fixture in fixtures)
        {
            var capture = new BaselineCapture(fixture.Id, fixture.Family);
            var execution = _executor(fixture);
            capture.RecordSamples(execution.Samples);
            reports.Add(
                new FixtureRunReport(
                    fixture,
                    capture.Finalize(),
                    execution.Passed,
                    execution.FramesCompared,
                    execution.FramesMatched,
                    execution.Notes ?? []));
        }

        return new FixtureSuiteReport(reports);
    }

    private static FixtureExecution ExecuteDeterministic(FixtureSpec fixture) =>
        fixture.Family switch
        {
            FixtureFamily.Render => ExecuteRenderFixture(fixture),
            FixtureFamily.Runtime => ExecuteRuntimeFixture(fixture),
            FixtureFamily.Doctor => ExecuteDoctorFixture(fixture),
            FixtureFamily.Challenge => ExecuteChallengeFixture(fixture),
            _ => throw new InvalidOperationException($"Unsupported fixture family '{fixture.Family}'.")
        };

    private static FixtureExecution ExecuteRenderFixture(FixtureSpec fixture)
    {
        var previous = new RenderBuffer(fixture.Viewport.Width, fixture.Viewport.Height);
        var next = new RenderBuffer(fixture.Viewport.Width, fixture.Viewport.Height);
        PopulateRenderBuffer(next, fixture);

        var checksums = new HashSet<string>(StringComparer.Ordinal);
        var samples = new List<Sample>();
        const int iterations = 4;
        var dirtyCells = 0;
        var ansiBytes = 0;

        for (var index = 0; index < iterations; index++)
        {
            var diffStopwatch = Stopwatch.StartNew();
            var diff = BufferDiff.ComputeDirty(previous, next);
            diffStopwatch.Stop();

            var presenter = new Presenter(TerminalCapabilities.Modern());
            var presentStopwatch = Stopwatch.StartNew();
            var result = presenter.Present(next, diff);
            presentStopwatch.Stop();

            dirtyCells = diff.Count;
            ansiBytes = result.ByteCount;
            checksums.Add(Checksum(result.Output));
            samples.Add(Sample.LatencyUs("buffer_diff", ToMicroseconds(diffStopwatch.Elapsed)));
            samples.Add(Sample.LatencyUs("presenter_emit", ToMicroseconds(presentStopwatch.Elapsed)));
            samples.Add(Sample.OutputCost("dirty_cells", diff.Count));
            samples.Add(Sample.OutputCost("ansi_bytes", result.ByteCount));
        }

        var matched = checksums.Count == 1 ? iterations : 0;
        var notes = new List<string>
        {
            $"dirty_cells={dirtyCells}",
            $"ansi_bytes={ansiBytes}"
        };

        return new FixtureExecution(samples, matched == iterations, iterations, matched, notes);
    }

    private static FixtureExecution ExecuteRuntimeFixture(FixtureSpec fixture)
    {
        var simulator = new AppSimulator<int, string>(new Size(fixture.Viewport.Width, fixture.Viewport.Height), Theme.DefaultTheme);
        var samples = new List<Sample>();
        var checksums = new HashSet<string>(StringComparer.Ordinal);
        const int iterations = 4;

        for (var index = 0; index < iterations; index++)
        {
            var dispatchStopwatch = Stopwatch.StartNew();
            var result = simulator.DispatchAsync(new FixtureProgram(), 0, "inc").AsTask().GetAwaiter().GetResult();
            dispatchStopwatch.Stop();

            checksums.Add(Checksum(result.ScreenText));
            samples.Add(Sample.LatencyUs("runtime_dispatch", ToMicroseconds(dispatchStopwatch.Elapsed)));
            samples.Add(Sample.OutputCost("runtime_emitted_messages", result.EmittedMessages.Count));
        }

        var matched = checksums.Count == 1 ? iterations : 0;
        return new FixtureExecution(
            samples,
            matched == iterations,
            iterations,
            matched,
            [$"queue_high_water={simulator.Runtime.QueueTelemetry.HighWater}"]);
    }

    private static FixtureExecution ExecuteDoctorFixture(FixtureSpec fixture)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "frankentui-fixture-suite", fixture.Id);
        Directory.CreateDirectory(tempRoot);
        var reportPath = Path.Combine(tempRoot, "doctor-report.json");
        var benchmarkPath = Path.Combine(tempRoot, "doctor-benchmark.json");
        File.WriteAllText(
            reportPath,
            """
            {
              "trace_id": "fixture-trace",
              "created_at": "2026-03-20T00:00:00Z",
              "status": "ok",
              "runtime_lane": "doctor"
            }
            """);
        File.WriteAllText(
            benchmarkPath,
            """
            {
              "trace_id": "fixture-trace",
              "gate_name": "doctor-benchmark",
              "passed": true,
              "threshold": "advisory"
            }
            """);

        var manifestSummary = ArtifactManifestContract.BuildSummary(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["manifest"] = reportPath,
                ["benchmarks"] = benchmarkPath
            });
        var failureSummary = FailureSignatures.ValidateBatch(
        [
            new FailureLogEntry(
                FailureClass.Mismatch,
                new HashSet<string>(new[] { "reason", "frame_idx", "expected_hash", "actual_hash", "scenario", "seed" }, StringComparer.Ordinal))
        ]);

        var samples = new List<Sample>
        {
            Sample.OutputCost("artifact_count", manifestSummary.EntryCount),
            Sample.OutputCost("manifest_failure_count", manifestSummary.FailingCount),
            Sample.OutputCost("failure_signature_entries", failureSummary.EntryCount)
        };

        var passed = manifestSummary.FailingCount == 0 && failureSummary.FailureCount == 0;
        return new FixtureExecution(samples, passed, 1, passed ? 1 : 0, [$"manifest_entries={manifestSummary.EntryCount}"]);
    }

    private static FixtureExecution ExecuteChallengeFixture(FixtureSpec fixture)
    {
        var previous = new RenderBuffer(fixture.Viewport.Width, fixture.Viewport.Height);
        var next = new RenderBuffer(fixture.Viewport.Width, fixture.Viewport.Height);
        PopulateRenderBuffer(next, fixture);
        var samples = new List<Sample>();
        const int iterations = 3;
        var checksums = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < iterations; index++)
        {
            var stopwatch = Stopwatch.StartNew();
            var diff = BufferDiff.ComputeFull(previous, next);
            var presenter = new Presenter(TerminalCapabilities.Modern());
            var result = presenter.Present(next, diff);
            stopwatch.Stop();

            checksums.Add(Checksum(result.Output));
            samples.Add(Sample.LatencyUs("challenge_present", ToMicroseconds(stopwatch.Elapsed)));
            samples.Add(Sample.OutputCost("challenge_dirty_cells", diff.Count));
        }

        var matched = checksums.Count == 1 ? iterations : 0;
        return new FixtureExecution(samples, matched == iterations, iterations, matched, [$"viewport={fixture.Viewport.Label}"]);
    }

    private static void PopulateRenderBuffer(RenderBuffer buffer, FixtureSpec fixture)
    {
        var accent = UiStyle.Accent.ToCell('x');
        var success = UiStyle.Success.ToCell('x');
        var warning = UiStyle.Warning.ToCell('x');
        BufferPainter.WriteText(buffer, 1, 0, fixture.Name, accent);
        if (buffer.Height > 2)
        {
            BufferPainter.WriteText(buffer, 1, 2, fixture.Description[..Math.Min(fixture.Description.Length, Math.Max(buffer.Width - 2, 1))], success);
        }

        if (fixture.Partition is SuitePartition.Challenge)
        {
            for (ushort row = 4; row < buffer.Height; row += 3)
            {
                BufferPainter.WriteText(buffer, 0, row, new string('#', Math.Min((int)buffer.Width, 24)), warning);
            }
        }
    }

    private static string Checksum(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private static long ToMicroseconds(TimeSpan elapsed) => (long)Math.Round(elapsed.TotalMilliseconds * 1000d, MidpointRounding.AwayFromZero);

    private sealed class FixtureProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            new(
                message == "inc" ? model + 1 : model,
                AppCommand<string>.Batch("fixture-command", "tick"),
                [new Subscription<string>("fixture-subscription", static () => ["subscribed"], "fixture-subscription")]);

        public IRuntimeView BuildView(int model) =>
            new PanelWidget
            {
                Title = "Fixture",
                Child = new ParagraphWidget($"value={model}")
            };
    }
}
