using FrankenTui.Core;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum PerformanceHudLevel
{
    Hidden,
    Compact,
    Full,
    Minimal
}

public sealed record PerformanceHudSnapshot(
    PerformanceHudLevel Level,
    double TotalMs,
    double ElapsedMs,
    int CellsChanged,
    int RunCount,
    int BytesEmitted,
    string DegradationLevel,
    int DroppedFrames,
    bool SyncOutput,
    bool ScrollRegion,
    bool Hyperlinks,
    string LoadGovernorAction = "stay",
    string LoadGovernorReason = "initial",
    double LoadGovernorPidOutput = 0,
    double LoadGovernorEProcessValue = 1,
    double LoadGovernorPidGateMargin = 0,
    double LoadGovernorEvidenceMargin = 0,
    bool LoadGovernorInWarmup = true,
    ulong LoadGovernorTransitionSeq = 0,
    ulong LoadGovernorTransitionCorrelationId = 0)
{
    public double RemainingMs => Math.Max(TotalMs - ElapsedMs, 0);

    public static PerformanceHudSnapshot FromSession(HostedParitySession session) =>
        new(
            session.OverlayVisible ? PerformanceHudLevel.Full : PerformanceHudLevel.Compact,
            16.0,
            Math.Min(4.5 + session.StepCount * 1.2, 24.0),
            session.AppliedEvents.Count * 8 + 48,
            Math.Max(session.SemanticLog.Count, 1),
            512 + session.AppliedEvents.Count * 64,
            session.StepCount > 10 ? "REDUCED" : "FULL",
            Math.Max(session.StepCount / 8 - 1, 0),
            !session.InlineMode,
            true,
            true);

    public static PerformanceHudSnapshot FromRuntime(
        RuntimeFrameStats stats,
        bool syncOutput,
        bool scrollRegion,
        bool hyperlinks,
        PerformanceHudLevel level = PerformanceHudLevel.Full)
    {
        ArgumentNullException.ThrowIfNull(stats);

        return new PerformanceHudSnapshot(
            level,
            TotalMs: 16.0,
            ElapsedMs: Math.Round(stats.FrameDurationMs, 2),
            CellsChanged: stats.ChangedCells,
            RunCount: stats.RunCount,
            BytesEmitted: stats.BytesEmitted,
            DegradationLevel: stats.DegradationLevel,
            DroppedFrames: Math.Max((int)Math.Ceiling(stats.FrameDurationMs / 16.0) - 1, 0),
            SyncOutput: syncOutput,
            ScrollRegion: scrollRegion,
            Hyperlinks: hyperlinks,
            LoadGovernorAction: stats.LoadGovernorAction,
            LoadGovernorReason: stats.LoadGovernorReason,
            LoadGovernorPidOutput: stats.LoadGovernorPidOutput,
            LoadGovernorEProcessValue: stats.LoadGovernorEProcessValue,
            LoadGovernorPidGateMargin: stats.LoadGovernorPidGateMargin,
            LoadGovernorEvidenceMargin: stats.LoadGovernorEvidenceMargin,
            LoadGovernorInWarmup: stats.LoadGovernorEProcessInWarmup,
            LoadGovernorTransitionSeq: stats.LoadGovernorTransitionSeq,
            LoadGovernorTransitionCorrelationId: stats.LoadGovernorTransitionCorrelationId);
    }
}

public sealed class PerformanceHudWidget : IWidget
{
    public PerformanceHudSnapshot Snapshot { get; init; } = new(PerformanceHudLevel.Hidden, 16, 0, 0, 0, 0, "FULL", 0, false, false, false);

    public void Render(RuntimeRenderContext context)
    {
        if (Snapshot.Level == PerformanceHudLevel.Hidden)
        {
            return;
        }

        var lines = Snapshot.Level == PerformanceHudLevel.Minimal
            ? new[]
            {
                $"HUD: {Snapshot.ElapsedMs:0.0} / {Snapshot.TotalMs:0.0} ms | Δ {Snapshot.CellsChanged} | {Snapshot.DegradationLevel}"
            }
            : new[]
            {
                $"Frame:   {Snapshot.ElapsedMs:0.0} / {Snapshot.TotalMs:0.0} ms",
                $"Budget:  {Snapshot.RemainingMs:0.0} ms remaining",
                $"Δ Cells: {Snapshot.CellsChanged}  Runs: {Snapshot.RunCount}",
                $"Bytes:   {Snapshot.BytesEmitted}  Deg: {Snapshot.DegradationLevel}",
                $"Gov:     {Snapshot.LoadGovernorAction}/{Snapshot.LoadGovernorReason} warmup={BoolFlag(Snapshot.LoadGovernorInWarmup)}",
                $"PID/E:   u={Snapshot.LoadGovernorPidOutput:0.###} e={Snapshot.LoadGovernorEProcessValue:0.###}",
                $"Margins: pid={Snapshot.LoadGovernorPidGateMargin:0.###} evidence={Snapshot.LoadGovernorEvidenceMargin:0.###}",
                $"Trans:   seq={Snapshot.LoadGovernorTransitionSeq} corr={Snapshot.LoadGovernorTransitionCorrelationId}",
                $"Caps:    sync={BoolFlag(Snapshot.SyncOutput)} scroll={BoolFlag(Snapshot.ScrollRegion)} osc8={BoolFlag(Snapshot.Hyperlinks)}",
                $"Drops:   {Snapshot.DroppedFrames}"
            };

        new PanelWidget
        {
            Title = "Performance HUD",
            Child = new ParagraphWidget(string.Join(Environment.NewLine, lines))
        }.Render(context);
    }

    private static string BoolFlag(bool value) => value ? "y" : "n";
}
