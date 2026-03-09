namespace FrankenTui.Render;

public readonly record struct PresentResult(
    string Output,
    int ByteCount,
    int ChangedCells,
    int RunCount,
    bool UsedSyncOutput,
    bool Truncated);
