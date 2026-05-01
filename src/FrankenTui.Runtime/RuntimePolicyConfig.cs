namespace FrankenTui.Runtime;

public sealed record RuntimePolicyConfig(
    RuntimeConformalPolicyConfig? Conformal = null,
    RuntimeFrameGuardPolicyConfig? FrameGuard = null,
    RuntimeCascadePolicyConfig? Cascade = null,
    RuntimePidPolicyConfig? Pid = null,
    RuntimeEProcessBudgetPolicyConfig? EProcessBudget = null,
    RuntimeBudgetPolicyConfig? Budget = null)
{
    public static RuntimePolicyConfig Default { get; } = new();

    public RuntimeConformalPolicyConfig EffectiveConformal => Conformal ?? RuntimeConformalPolicyConfig.Default;

    public RuntimeFrameGuardPolicyConfig EffectiveFrameGuard => FrameGuard ?? RuntimeFrameGuardPolicyConfig.Default;

    public RuntimeCascadePolicyConfig EffectiveCascade => Cascade ?? RuntimeCascadePolicyConfig.Default;

    public RuntimePidPolicyConfig EffectivePid => Pid ?? RuntimePidPolicyConfig.Default;

    public RuntimeEProcessBudgetPolicyConfig EffectiveEProcessBudget =>
        EProcessBudget ?? RuntimeEProcessBudgetPolicyConfig.Default;

    public RuntimeBudgetPolicyConfig EffectiveBudget => Budget ?? RuntimeBudgetPolicyConfig.Default;

    public RuntimeConformalConfig ToConformalConfig() =>
        EffectiveConformal.ToConformalConfig();

    public ConformalFrameGuardConfig ToFrameGuardConfig() =>
        EffectiveFrameGuard.ToFrameGuardConfig(ToConformalConfig());

    public DegradationCascadeConfig ToCascadeConfig() =>
        EffectiveCascade.ToCascadeConfig(ToFrameGuardConfig());

    public PidGains ToPidGains() =>
        EffectivePid.ToPidGains();

    public EProcessConfig ToEProcessBudgetConfig() =>
        EffectiveEProcessBudget.ToEProcessConfig();

    public BudgetControllerConfig ToBudgetControllerConfig() =>
        EffectiveBudget.ToBudgetControllerConfig(ToPidGains(), ToEProcessBudgetConfig());

    public LoadGovernorConfig ToLoadGovernorConfig(bool enabled = true) =>
        new(enabled, ToBudgetControllerConfig());
}

public sealed record RuntimeConformalPolicyConfig(
    double Alpha = 0.05,
    int MinSamples = 20,
    int WindowSize = 256,
    double DefaultResidualMicroseconds = 10_000.0)
{
    public static RuntimeConformalPolicyConfig Default { get; } = new();

    public RuntimeConformalConfig ToConformalConfig() =>
        new(Alpha, MinSamples, WindowSize, DefaultResidualMicroseconds);
}

public sealed record RuntimeFrameGuardPolicyConfig(
    double FallbackBudgetMicroseconds = 16_000.0,
    int TimeSeriesWindow = 512,
    int NonconformityWindow = 256)
{
    public static RuntimeFrameGuardPolicyConfig Default { get; } = new();

    public ConformalFrameGuardConfig ToFrameGuardConfig(RuntimeConformalConfig? conformal = null) =>
        new(
            TimeSpan.FromMicroseconds(FallbackBudgetMicroseconds),
            conformal,
            TimeSeriesWindow,
            NonconformityWindow);
}

public sealed record RuntimeCascadePolicyConfig(
    int RecoveryThreshold = 10,
    RuntimeDegradationLevel MaxDegradation = RuntimeDegradationLevel.SkipFrame,
    RuntimeDegradationLevel MinTriggerLevel = RuntimeDegradationLevel.SimpleBorders,
    RuntimeDegradationLevel DegradationFloor = RuntimeDegradationLevel.SimpleBorders)
{
    public static RuntimeCascadePolicyConfig Default { get; } = new();

    public DegradationCascadeConfig ToCascadeConfig(ConformalFrameGuardConfig? guard = null) =>
        new(guard, RecoveryThreshold, MaxDegradation, MinTriggerLevel, DegradationFloor);
}

public sealed record RuntimePidPolicyConfig(
    double Kp = 0.5,
    double Ki = 0.05,
    double Kd = 0.2,
    double IntegralMax = 5.0)
{
    public static RuntimePidPolicyConfig Default { get; } = new();

    public PidGains ToPidGains() =>
        new(Kp, Ki, Kd, IntegralMax);
}

public sealed record RuntimeEProcessBudgetPolicyConfig(
    double Lambda = 0.5,
    double Alpha = 0.05,
    double Beta = 0.5,
    double SigmaEmaDecay = 0.9,
    double SigmaFloorMilliseconds = 1.0,
    uint WarmupFrames = 10)
{
    public static RuntimeEProcessBudgetPolicyConfig Default { get; } = new();

    public EProcessConfig ToEProcessConfig() =>
        new(Lambda, Alpha, Beta, SigmaEmaDecay, SigmaFloorMilliseconds, WarmupFrames);
}

public sealed record RuntimeBudgetPolicyConfig(
    double TargetFrameMilliseconds = 16.0,
    double DegradeThreshold = 0.30,
    double UpgradeThreshold = 0.20,
    int CooldownFrames = 3,
    RuntimeDegradationLevel DegradationFloor = RuntimeDegradationLevel.SimpleBorders)
{
    public static RuntimeBudgetPolicyConfig Default { get; } = new();

    public BudgetControllerConfig ToBudgetControllerConfig(PidGains? pid = null, EProcessConfig? eProcess = null) =>
        new(
            TimeSpan.FromMilliseconds(TargetFrameMilliseconds),
            DegradeThreshold,
            UpgradeThreshold,
            CooldownFrames,
            DegradationFloor,
            pid,
            eProcess);
}
