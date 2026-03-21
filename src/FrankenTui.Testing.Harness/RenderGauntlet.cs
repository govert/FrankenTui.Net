using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Testing.Harness;

public enum GauntletGate
{
    Equivalence,
    Replay,
    TailLatency,
    Certificate,
    Challenge,
    NegativeControl
}

public enum FailureCategory
{
    SemanticRegression,
    ObservabilityGap,
    BenchmarkOverfit,
    ExpectedFallback,
    StaleCertificate,
    StaleCache,
    TailRegression,
    ResourceLeak,
    Crash
}

public sealed record GateFailure(
    string FixtureId,
    string Reason,
    FailureCategory Category,
    IReadOnlyList<string> Artifacts);

public sealed record GateResult(
    GauntletGate Gate,
    bool Passed,
    int FixturesTested,
    int FixturesPassed,
    string Summary,
    IReadOnlyList<GateFailure> Failures,
    long DurationMs);

public sealed record GauntletConfig(
    double TailLatencyP95BudgetMs,
    int ReplayIterations,
    bool Strict)
{
    public static GauntletConfig Default() => new(20, 8, false);
}

public sealed record GauntletReport(
    IReadOnlyList<GateResult> Results)
{
    public bool Passed => Results.All(static result => result.Passed);

    public string Summary() =>
        string.Join(", ", Results.Select(result => $"{result.Gate}:{(result.Passed ? "pass" : "fail")}"));

    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

internal sealed record RenderGauntletFixture(
    string Id,
    RenderBuffer Previous,
    RenderBuffer Next,
    IReadOnlyDictionary<uint, string>? Links = null);

public sealed class RenderGauntlet
{
    private readonly GauntletConfig _config;

    public RenderGauntlet(GauntletConfig? config = null)
    {
        _config = config ?? GauntletConfig.Default();
    }

    public GauntletReport Run()
    {
        var fixtures = BuildFixtures();
        return new GauntletReport(
        [
            RunEquivalenceGate(fixtures),
            RunReplayGate(fixtures),
            RunTailLatencyGate(fixtures),
            RunCertificateGate(fixtures),
            RunChallengeGate(fixtures),
            RunNegativeControlGate(fixtures)
        ]);
    }

    private static IReadOnlyList<RenderGauntletFixture> BuildFixtures()
    {
        var plainPrevious = new RenderBuffer(20, 4);
        var plainNext = new RenderBuffer(20, 4);
        BufferPainter.WriteText(plainNext, 1, 0, "hello", UiStyle.Accent.ToCell('x'));
        BufferPainter.WriteText(plainNext, 1, 2, "world", UiStyle.Success.ToCell('x'));

        var emojiPrevious = new RenderBuffer(8, 2);
        var emojiNext = new RenderBuffer(8, 2);
        emojiNext.Set(0, 0, Cell.FromRune(new Rune(0x1F600)));
        emojiNext.Set(0, 1, Cell.FromChar('A'));

        var linkPrevious = new RenderBuffer(8, 1);
        var linkNext = new RenderBuffer(8, 1);
        linkNext.Set(0, 0, Cell.FromChar('L').WithAttributes(new CellAttributes(CellStyleFlags.Underline, 1)));
        linkNext.Set(1, 0, Cell.FromChar('N'));

        return
        [
            new RenderGauntletFixture("plain-text", plainPrevious, plainNext),
            new RenderGauntletFixture("wide-glyph", emojiPrevious, emojiNext),
            new RenderGauntletFixture("hyperlink", linkPrevious, linkNext, new Dictionary<uint, string> { [1] = "https://example.test" })
        ];
    }

    private GateResult RunEquivalenceGate(IReadOnlyList<RenderGauntletFixture> fixtures)
    {
        var stopwatch = Stopwatch.StartNew();
        var failures = new List<GateFailure>();

        foreach (var fixture in fixtures)
        {
            var full = Present(fixture.Next, BufferDiff.ComputeFull(fixture.Previous, fixture.Next), fixture.Links);
            var dirty = Present(fixture.Next, BufferDiff.ComputeDirty(fixture.Previous, fixture.Next), fixture.Links);
            var report = PresenterEquivalence.Compare(full.Output, dirty.Output, fixture.Next.Width, fixture.Next.Height);
            if (!report.Equivalent)
            {
                failures.Add(new GateFailure(
                    fixture.Id,
                    string.Join("; ", report.Violations.Select(static violation => violation.Detail)),
                    FailureCategory.SemanticRegression,
                    [report.ExpectedChecksum, report.ActualChecksum]));
            }
        }

        stopwatch.Stop();
        return BuildResult(GauntletGate.Equivalence, fixtures.Count, failures, stopwatch.ElapsedMilliseconds);
    }

    private GateResult RunReplayGate(IReadOnlyList<RenderGauntletFixture> fixtures)
    {
        var stopwatch = Stopwatch.StartNew();
        var failures = new List<GateFailure>();

        foreach (var fixture in fixtures)
        {
            var checksums = new HashSet<string>(StringComparer.Ordinal);
            for (var iteration = 0; iteration < _config.ReplayIterations; iteration++)
            {
                var result = Present(fixture.Next, BufferDiff.ComputeDirty(fixture.Previous, fixture.Next), fixture.Links);
                checksums.Add(Checksum(result.Output));
            }

            if (checksums.Count != 1)
            {
                failures.Add(new GateFailure(
                    fixture.Id,
                    "Repeated presenter runs did not produce identical output.",
                    FailureCategory.SemanticRegression,
                    checksums.ToArray()));
            }
        }

        stopwatch.Stop();
        return BuildResult(GauntletGate.Replay, fixtures.Count, failures, stopwatch.ElapsedMilliseconds);
    }

    private GateResult RunTailLatencyGate(IReadOnlyList<RenderGauntletFixture> fixtures)
    {
        var stopwatch = Stopwatch.StartNew();
        var failures = new List<GateFailure>();

        foreach (var fixture in fixtures)
        {
            _ = Present(fixture.Next, BufferDiff.ComputeDirty(fixture.Previous, fixture.Next), fixture.Links);
            var samples = new List<double>(_config.ReplayIterations);
            for (var iteration = 0; iteration < _config.ReplayIterations; iteration++)
            {
                var started = Stopwatch.GetTimestamp();
                _ = Present(fixture.Next, BufferDiff.ComputeDirty(fixture.Previous, fixture.Next), fixture.Links);
                var elapsedTicks = Stopwatch.GetTimestamp() - started;
                samples.Add(elapsedTicks * 1000d / Stopwatch.Frequency);
            }

            samples.Sort();
            var p95 = samples[(int)Math.Clamp(Math.Ceiling(samples.Count * 0.95) - 1, 0, samples.Count - 1)];
            if (p95 > _config.TailLatencyP95BudgetMs)
            {
                failures.Add(new GateFailure(
                    fixture.Id,
                    $"Presenter p95 {p95:0.###}ms exceeded budget {_config.TailLatencyP95BudgetMs:0.###}ms.",
                    FailureCategory.TailRegression,
                    [p95.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)]));
            }
        }

        stopwatch.Stop();
        return BuildResult(GauntletGate.TailLatency, fixtures.Count, failures, stopwatch.ElapsedMilliseconds);
    }

    private GateResult RunCertificateGate(IReadOnlyList<RenderGauntletFixture> fixtures)
    {
        var stopwatch = Stopwatch.StartNew();
        var failures = new List<GateFailure>();

        foreach (var fixture in fixtures)
        {
            var full = Present(fixture.Next, BufferDiff.ComputeFull(fixture.Previous, fixture.Next), fixture.Links);
            var rows = BufferDiff.CollectDirtyRows(fixture.Previous, fixture.Next);
            var certified = Present(fixture.Next, BufferDiff.ComputeCertified(fixture.Previous, fixture.Next, DiffSkipHint.NarrowToRows(rows)), fixture.Links);
            var report = PresenterEquivalence.Compare(full.Output, certified.Output, fixture.Next.Width, fixture.Next.Height);
            if (!report.Equivalent)
            {
                failures.Add(new GateFailure(
                    fixture.Id,
                    "Certified diff hint produced terminal-visible divergence.",
                    FailureCategory.StaleCertificate,
                    [report.ExpectedChecksum, report.ActualChecksum]));
            }
        }

        stopwatch.Stop();
        return BuildResult(GauntletGate.Certificate, fixtures.Count, failures, stopwatch.ElapsedMilliseconds);
    }

    private GateResult RunChallengeGate(IReadOnlyList<RenderGauntletFixture> fixtures)
    {
        var stopwatch = Stopwatch.StartNew();
        var failures = new List<GateFailure>();

        try
        {
            var cache = new LayoutCache();
            var constraints = new[]
            {
                LayoutConstraint.Fixed(8),
                LayoutConstraint.Fill(),
                LayoutConstraint.Percentage(30)
            };
            var trace = LayoutSolver.SplitWithTrace(new Rect(0, 0, 40, 10), LayoutDirection.Horizontal, constraints);
            cache.Set(trace);
            var plan = new LayoutPlan(new Rect(0, 0, 40, 10), LayoutDirection.Horizontal, constraints);
            if (!cache.TryGet(plan.Bounds, plan.Direction, plan.Constraints, out var cached) ||
                !LayoutReuseContract.EquivalentKey(plan, cached))
            {
                failures.Add(new GateFailure(
                    "layout-cache",
                    "Layout cache failed to round-trip an equivalent key.",
                    FailureCategory.StaleCache,
                    [trace.CacheKey]));
            }

            foreach (var fixture in fixtures)
            {
                _ = Present(fixture.Next, BufferDiff.ComputeDirty(fixture.Previous, fixture.Next), fixture.Links);
            }
        }
        catch (Exception ex)
        {
            failures.Add(new GateFailure(
                "challenge",
                ex.Message,
                FailureCategory.Crash,
                [ex.GetType().FullName ?? ex.GetType().Name]));
        }

        stopwatch.Stop();
        return BuildResult(GauntletGate.Challenge, fixtures.Count, failures, stopwatch.ElapsedMilliseconds);
    }

    private GateResult RunNegativeControlGate(IReadOnlyList<RenderGauntletFixture> fixtures)
    {
        var stopwatch = Stopwatch.StartNew();
        var failures = new List<GateFailure>();

        foreach (var fixture in fixtures)
        {
            var skip = BufferDiff.ComputeCertified(fixture.Next, fixture.Next.Clone(), DiffSkipHint.SkipDiff);
            if (!skip.IsEmpty)
            {
                failures.Add(new GateFailure(
                    fixture.Id,
                    "SkipDiff should produce an empty diff for unchanged buffers.",
                    FailureCategory.SemanticRegression,
                    []));
            }
        }

        stopwatch.Stop();
        return BuildResult(GauntletGate.NegativeControl, fixtures.Count, failures, stopwatch.ElapsedMilliseconds);
    }

    private static GateResult BuildResult(GauntletGate gate, int tested, IReadOnlyList<GateFailure> failures, long durationMs)
    {
        var passed = failures.Count == 0;
        return new GateResult(
            gate,
            passed,
            tested,
            passed ? tested : tested - failures.Count,
            passed ? $"{tested}/{tested} fixtures passed" : $"{tested - failures.Count}/{tested} fixtures passed",
            failures,
            durationMs);
    }

    private static PresentResult Present(RenderBuffer buffer, BufferDiff diff, IReadOnlyDictionary<uint, string>? links)
    {
        var presenter = new Presenter(TerminalCapabilities.Modern());
        return presenter.Present(buffer, diff, links);
    }

    private static string Checksum(string output)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(output));
        return Convert.ToHexString(bytes);
    }
}
