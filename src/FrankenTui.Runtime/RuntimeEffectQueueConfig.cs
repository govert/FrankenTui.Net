namespace FrankenTui.Runtime;

public enum RuntimeTaskExecutorBackend
{
    Spawned,
    EffectQueue,
    Asupersync
}

public enum RuntimeSchedulingMode
{
    Fifo,
    Smith,
    Srpt
}

public static class RuntimeSchedulingModeExtensions
{
    public static string Label(this RuntimeSchedulingMode mode) =>
        mode switch
        {
            RuntimeSchedulingMode.Fifo => "fifo",
            RuntimeSchedulingMode.Smith => "smith",
            RuntimeSchedulingMode.Srpt => "srpt",
            _ => "unknown"
        };
}

public sealed record RuntimeQueueSchedulerConfig(
    double AgingFactor = 0.1,
    double MinProcessingMilliseconds = 0.05,
    double MaxProcessingMilliseconds = 5_000.0,
    double DefaultEstimateMilliseconds = 10.0,
    double UnknownEstimateMilliseconds = 1_000.0,
    double MinWeight = 1e-6,
    double MaxWeight = 100.0,
    double DefaultWeight = 1.0,
    double UnknownWeight = 1.0,
    double StarvationGuardMilliseconds = 500.0,
    double StarvationBoostRatio = 1.5,
    bool SmithEnabled = true,
    bool ForceFifo = false,
    int MaxQueueSize = 10_000,
    bool Preemptive = true,
    double TimeQuantumMilliseconds = 10.0,
    bool EnableLogging = false)
{
    public static RuntimeQueueSchedulerConfig Default { get; } = new();

    public RuntimeSchedulingMode Mode =>
        ForceFifo ? RuntimeSchedulingMode.Fifo : SmithEnabled ? RuntimeSchedulingMode.Smith : RuntimeSchedulingMode.Srpt;

    public RuntimeQueueSchedulerConfig Normalized() =>
        this with
        {
            AgingFactor = Math.Max(AgingFactor, 0.0),
            MinProcessingMilliseconds = Math.Max(MinProcessingMilliseconds, 0.001),
            MaxProcessingMilliseconds = Math.Max(MaxProcessingMilliseconds, Math.Max(MinProcessingMilliseconds, 0.001)),
            DefaultEstimateMilliseconds = Math.Clamp(DefaultEstimateMilliseconds, Math.Max(MinProcessingMilliseconds, 0.001), Math.Max(MaxProcessingMilliseconds, Math.Max(MinProcessingMilliseconds, 0.001))),
            UnknownEstimateMilliseconds = Math.Clamp(UnknownEstimateMilliseconds, Math.Max(MinProcessingMilliseconds, 0.001), Math.Max(MaxProcessingMilliseconds, Math.Max(MinProcessingMilliseconds, 0.001))),
            MinWeight = Math.Max(MinWeight, double.Epsilon),
            MaxWeight = Math.Max(MaxWeight, Math.Max(MinWeight, double.Epsilon)),
            DefaultWeight = Math.Clamp(DefaultWeight, Math.Max(MinWeight, double.Epsilon), Math.Max(MaxWeight, Math.Max(MinWeight, double.Epsilon))),
            UnknownWeight = Math.Clamp(UnknownWeight, Math.Max(MinWeight, double.Epsilon), Math.Max(MaxWeight, Math.Max(MinWeight, double.Epsilon))),
            StarvationGuardMilliseconds = Math.Max(StarvationGuardMilliseconds, 0.0),
            StarvationBoostRatio = Math.Max(StarvationBoostRatio, 1.0),
            MaxQueueSize = Math.Max(MaxQueueSize, 1),
            TimeQuantumMilliseconds = Math.Max(TimeQuantumMilliseconds, 0.001)
        };
}

public sealed record RuntimeEffectQueueConfig(
    bool Enabled = false,
    RuntimeTaskExecutorBackend Backend = RuntimeTaskExecutorBackend.Spawned,
    RuntimeQueueSchedulerConfig? Scheduler = null,
    int MaxQueueDepth = 0)
{
    public static RuntimeEffectQueueConfig Default { get; } = new();

    public RuntimeQueueSchedulerConfig EffectiveScheduler => (Scheduler ?? RuntimeQueueSchedulerConfig.Default).Normalized();

    public RuntimeEffectQueueConfig Normalized() =>
        this with
        {
            Scheduler = EffectiveScheduler,
            MaxQueueDepth = Math.Max(MaxQueueDepth, 0)
        };

    public RuntimeEffectQueueConfig WithEnabled(bool enabled) =>
        this with
        {
            Enabled = enabled,
            Backend = enabled ? RuntimeTaskExecutorBackend.EffectQueue : RuntimeTaskExecutorBackend.Spawned
        };

    public RuntimeEffectQueueConfig WithBackend(RuntimeTaskExecutorBackend backend) =>
        this with
        {
            Backend = backend,
            Enabled = backend == RuntimeTaskExecutorBackend.EffectQueue
        };

    public RuntimeEffectQueueConfig WithScheduler(RuntimeQueueSchedulerConfig scheduler) =>
        this with { Scheduler = scheduler };

    public RuntimeEffectQueueConfig WithMaxQueueDepth(int depth) =>
        this with { MaxQueueDepth = Math.Max(depth, 0) };
}
