using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public enum WorkflowStage
{
    Seed,
    Capture,
    Suite,
    Report,
    Triage
}

public enum CostLane
{
    Subprocess,
    Network,
    FileIo,
    Computation,
    Orchestration
}

public enum OptimizationImpact
{
    TailOnly,
    Moderate,
    High,
    Critical
}

public sealed record CostEntry(
    WorkflowStage Stage,
    CostLane Lane,
    string Operation,
    long WallClockMs,
    bool Blocking,
    bool Essential,
    OptimizationImpact Impact,
    string Rationale,
    string? Subprocess,
    double StageFraction)
{
    public static CostEntry Create(WorkflowStage stage, CostLane lane, string operation, long wallClockMs) =>
        new(stage, lane, operation, wallClockMs, false, true, OptimizationImpact.Moderate, string.Empty, null, 0d);

    public CostEntry WithBlocking(bool blocking) => this with { Blocking = blocking };

    public CostEntry WithEssential(bool essential) => this with { Essential = essential };

    public CostEntry WithImpact(OptimizationImpact impact) => this with { Impact = impact };

    public CostEntry WithRationale(string rationale) => this with { Rationale = rationale };

    public CostEntry WithSubprocess(string subprocess) => this with { Subprocess = subprocess };

    public CostEntry WithStageFraction(double stageFraction) => this with { StageFraction = stageFraction };
}

public sealed record OptimizationTarget(
    WorkflowStage Stage,
    CostLane Lane,
    string Operation,
    OptimizationImpact Impact,
    long WallClockMs,
    bool Blocking,
    string Rationale);

public sealed record StageCostTotal(string Stage, long TotalMs, double Percentage);

public sealed record LaneCostTotal(string Lane, long TotalMs, double Percentage);

public sealed record CostReport(
    IReadOnlyList<CostEntry> Entries,
    IReadOnlyList<StageCostTotal> StageBreakdown,
    IReadOnlyList<LaneCostTotal> LaneBreakdown,
    long GrandTotalMs,
    long BlockingTotalMs,
    double BlockingPercentage,
    long RedundantTotalMs,
    double RedundantPercentage,
    IReadOnlyList<OptimizationTarget> OptimizationTargets)
{
    public IReadOnlyList<CostEntry> ByStage(WorkflowStage stage) =>
        Entries.Where(entry => entry.Stage == stage).ToArray();

    public IReadOnlyList<CostEntry> ByLane(CostLane lane) =>
        Entries.Where(entry => entry.Lane == lane).ToArray();

    public long StageTotal(WorkflowStage stage) =>
        StageBreakdown.FirstOrDefault(total => string.Equals(total.Stage, Label(stage), StringComparison.Ordinal))?.TotalMs ?? 0;

    public long LaneTotal(CostLane lane) =>
        LaneBreakdown.FirstOrDefault(total => string.Equals(total.Lane, Label(lane), StringComparison.Ordinal))?.TotalMs ?? 0;

    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public string Summary()
    {
        var topTargets = string.Join(
            " | ",
            OptimizationTargets.Take(3).Select(target => $"{Label(target.Stage)}/{Label(target.Lane)}:{target.Operation}:{target.WallClockMs}ms"));
        return $"total={GrandTotalMs}ms blocking={BlockingTotalMs}ms targets={OptimizationTargets.Count} top={topTargets}";
    }

    internal static string Label(WorkflowStage stage) => stage switch
    {
        WorkflowStage.Seed => "seed",
        WorkflowStage.Capture => "capture",
        WorkflowStage.Suite => "suite",
        WorkflowStage.Report => "report",
        _ => "triage"
    };

    internal static string Label(CostLane lane) => lane switch
    {
        CostLane.Subprocess => "subprocess",
        CostLane.Network => "network",
        CostLane.FileIo => "file_io",
        CostLane.Computation => "computation",
        _ => "orchestration"
    };
}

public sealed class DoctorCostProfile
{
    private readonly List<CostEntry> _entries = [];

    public void Record(CostEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }

    public static DoctorCostProfile Canonical()
    {
        var profile = new DoctorCostProfile();

        profile.Record(CostEntry.Create(WorkflowStage.Seed, CostLane.Network, "server_readiness_poll", 3000).WithBlocking(true).WithStageFraction(0.30).WithRationale("Seed bootstrap waits on server readiness."));
        profile.Record(CostEntry.Create(WorkflowStage.Seed, CostLane.Network, "rpc_send_messages", 2000).WithBlocking(true).WithStageFraction(0.20).WithRationale("Seed demo message fan-out remains sequential."));
        profile.Record(CostEntry.Create(WorkflowStage.Seed, CostLane.Orchestration, "retry_backoff_waits", 3000).WithBlocking(true).WithImpact(OptimizationImpact.High).WithStageFraction(0.30).WithRationale("Retry backoff dominates slow seed startup tails."));

        profile.Record(CostEntry.Create(WorkflowStage.Capture, CostLane.Subprocess, "runtime_capture", 45000).WithBlocking(true).WithImpact(OptimizationImpact.Critical).WithSubprocess("vhs").WithStageFraction(0.75).WithRationale("Capture remains the dominant operator wait."));
        profile.Record(CostEntry.Create(WorkflowStage.Capture, CostLane.FileIo, "artifact_writes", 350).WithStageFraction(0.01).WithRationale("Replay, transcript, html, and contract artifacts are low-cost file writes."));
        profile.Record(CostEntry.Create(WorkflowStage.Capture, CostLane.Orchestration, "timeout_poll_loop", 100).WithBlocking(true).WithImpact(OptimizationImpact.TailOnly).WithStageFraction(0.01).WithRationale("Timeout polling overhead is small in healthy runs."));

        profile.Record(CostEntry.Create(WorkflowStage.Suite, CostLane.Subprocess, "per_profile_doctor_invocation", 60000).WithBlocking(true).WithImpact(OptimizationImpact.High).WithStageFraction(0.90).WithRationale("Per-profile suite fan-out multiplies capture cost."));
        profile.Record(CostEntry.Create(WorkflowStage.Suite, CostLane.FileIo, "manifest_index_generation", 100).WithStageFraction(0.002).WithRationale("Index generation is cheap compared to subprocess work."));
        profile.Record(CostEntry.Create(WorkflowStage.Suite, CostLane.Orchestration, "profile_fanout_scheduling", 50).WithStageFraction(0.001).WithRationale("Scheduling overhead is low."));

        profile.Record(CostEntry.Create(WorkflowStage.Report, CostLane.Computation, "html_report_synthesis", 500).WithStageFraction(0.50).WithRationale("Dashboard and report synthesis are lightweight."));
        profile.Record(CostEntry.Create(WorkflowStage.Report, CostLane.Computation, "json_summary_generation", 200).WithStageFraction(0.20).WithRationale("JSON report generation is cheap."));
        profile.Record(CostEntry.Create(WorkflowStage.Report, CostLane.FileIo, "report_file_writes", 100).WithStageFraction(0.10).WithRationale("Final report files are small."));

        profile.Record(CostEntry.Create(WorkflowStage.Triage, CostLane.Computation, "failure_signature_matching", 100).WithStageFraction(0.40).WithRationale("Signature matching is low-cost triage work."));
        profile.Record(CostEntry.Create(WorkflowStage.Triage, CostLane.Computation, "remediation_hint_generation", 50).WithStageFraction(0.20).WithRationale("Hint generation is lookup-based."));
        profile.Record(CostEntry.Create(WorkflowStage.Triage, CostLane.FileIo, "replay_artifact_collection", 100).WithStageFraction(0.40).WithRationale("Replay bundle collection is inexpensive."));

        return profile;
    }

    public CostReport Finalize()
    {
        var grandTotal = _entries.Sum(static entry => entry.WallClockMs);
        var blockingTotal = _entries.Where(static entry => entry.Blocking).Sum(static entry => entry.WallClockMs);
        var redundantTotal = _entries.Where(static entry => !entry.Essential).Sum(static entry => entry.WallClockMs);

        var stageBreakdown = Enum.GetValues<WorkflowStage>()
            .Select(stage =>
            {
                var total = _entries.Where(entry => entry.Stage == stage).Sum(static entry => entry.WallClockMs);
                return new StageCostTotal(CostReport.Label(stage), total, grandTotal == 0 ? 0d : total * 100d / grandTotal);
            })
            .ToArray();

        var laneBreakdown = Enum.GetValues<CostLane>()
            .Select(lane =>
            {
                var total = _entries.Where(entry => entry.Lane == lane).Sum(static entry => entry.WallClockMs);
                return new LaneCostTotal(CostReport.Label(lane), total, grandTotal == 0 ? 0d : total * 100d / grandTotal);
            })
            .ToArray();

        var targets = _entries
            .Where(static entry => entry.Impact >= OptimizationImpact.Moderate)
            .OrderByDescending(static entry => entry.Impact)
            .ThenByDescending(static entry => entry.WallClockMs)
            .Select(static entry => new OptimizationTarget(entry.Stage, entry.Lane, entry.Operation, entry.Impact, entry.WallClockMs, entry.Blocking, entry.Rationale))
            .ToArray();

        return new CostReport(
            _entries.ToArray(),
            stageBreakdown,
            laneBreakdown,
            grandTotal,
            blockingTotal,
            grandTotal == 0 ? 0d : blockingTotal * 100d / grandTotal,
            redundantTotal,
            grandTotal == 0 ? 0d : redundantTotal * 100d / grandTotal,
            targets);
    }

    public static string WriteArtifacts(string runId, CostReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(report);

        var path = ArtifactPathBuilder.For("benchmarks", $"{runId}-doctor-cost-profile.json");
        File.WriteAllText(path, report.ToJson());
        return path;
    }
}
