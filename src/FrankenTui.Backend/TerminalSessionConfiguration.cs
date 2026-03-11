namespace FrankenTui.Backend;

public sealed record TerminalSessionConfiguration
{
    public bool InlineMode { get; init; }

    public bool CaptureInput { get; init; } = true;

    public bool ClaimConsoleModes { get; init; } = true;

    public string? HostEvidenceTag { get; init; }
}
