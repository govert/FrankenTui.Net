namespace FrankenTui.Runtime;

public sealed record RuntimeExecutionPolicy(
    bool CaptureTrace = true,
    bool CaptureReplayTape = true,
    bool EmitTelemetry = false,
    bool PersistStateSnapshots = false,
    TelemetryConfig? Telemetry = null,
    LoadGovernorConfig? LoadGovernor = null,
    RuntimePolicyConfig? PolicyConfig = null,
    RuntimeEffectQueueConfig? EffectQueue = null)
{
    public static RuntimeExecutionPolicy Default { get; } = new();

    public RuntimePolicyConfig EffectivePolicyConfig => PolicyConfig ?? RuntimePolicyConfig.Default;

    public LoadGovernorConfig EffectiveLoadGovernor => LoadGovernor ?? EffectivePolicyConfig.ToLoadGovernorConfig();

    public DegradationCascadeConfig EffectiveDegradationCascade => EffectivePolicyConfig.ToCascadeConfig();

    public RuntimeEffectQueueConfig EffectiveEffectQueue => (EffectQueue ?? RuntimeEffectQueueConfig.Default).Normalized();
}
