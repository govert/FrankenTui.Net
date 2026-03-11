namespace FrankenTui.Runtime;

public sealed record RuntimeFrameStats(
    int StepIndex,
    int ChangedCells,
    int RunCount,
    int BytesEmitted,
    double FrameDurationMs,
    double PresentDurationMs,
    double DiffDurationMs,
    int DirtyRows,
    string DegradationLevel,
    bool SyncOutput,
    bool Truncated,
    int QueueLength = 0)
{
    public static RuntimeFrameStats Empty { get; } = new(
        StepIndex: 0,
        ChangedCells: 0,
        RunCount: 0,
        BytesEmitted: 0,
        FrameDurationMs: 0,
        PresentDurationMs: 0,
        DiffDurationMs: 0,
        DirtyRows: 0,
        DegradationLevel: "FULL",
        SyncOutput: false,
        Truncated: false);
}
