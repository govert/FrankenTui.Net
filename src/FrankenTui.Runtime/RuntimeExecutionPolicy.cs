namespace FrankenTui.Runtime;

public sealed record RuntimeExecutionPolicy(
    bool CaptureTrace = true,
    bool CaptureReplayTape = true,
    bool EmitTelemetry = false,
    bool PersistStateSnapshots = false,
    TelemetryConfig? Telemetry = null)
{
    public static RuntimeExecutionPolicy Default { get; } = new();
}
