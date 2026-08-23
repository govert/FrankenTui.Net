// SPDX-License-Identifier: Apache-2.0
// Port basis: frankentui 15cc6543f76b814394c590f9e7719dedd6684e4c
// Source: .external/frankentui/crates/ftui-render/src/frame_guardrails.rs

using System.Globalization;

namespace FrankenTui.Render;

/// <summary>Category of guardrail that triggered.</summary>
public enum GuardrailKind
{
    Memory,
    QueueDepth,
}

/// <summary>Severity of a guardrail alert, ordered from warning to emergency.</summary>
public enum AlertSeverity
{
    Warning,
    Critical,
    Emergency,
}

/// <summary>A single guardrail alert.</summary>
public record struct GuardrailAlert(
    GuardrailKind Kind,
    AlertSeverity Severity,
    DegradationLevel RecommendedLevel);

/// <summary>Configuration for rendering-memory budget enforcement.</summary>
public record struct MemoryBudgetConfig
{
    private const ulong DefaultSoftLimit = 8UL * 1024 * 1024;
    private const ulong DefaultHardLimit = 16UL * 1024 * 1024;
    private const ulong DefaultEmergencyLimit = 32UL * 1024 * 1024;

    public MemoryBudgetConfig()
        : this(DefaultSoftLimit, DefaultHardLimit, DefaultEmergencyLimit)
    {
    }

    public MemoryBudgetConfig(
        ulong softLimitBytes,
        ulong hardLimitBytes,
        ulong emergencyLimitBytes)
    {
        SoftLimitBytes = softLimitBytes;
        HardLimitBytes = hardLimitBytes;
        EmergencyLimitBytes = emergencyLimitBytes;
    }

    public ulong SoftLimitBytes { get; set; }

    public ulong HardLimitBytes { get; set; }

    public ulong EmergencyLimitBytes { get; set; }

    public static MemoryBudgetConfig Default => new();

    public static MemoryBudgetConfig Small() => new(
        2UL * 1024 * 1024,
        4UL * 1024 * 1024,
        8UL * 1024 * 1024);

    public static MemoryBudgetConfig Large() => new(
        32UL * 1024 * 1024,
        64UL * 1024 * 1024,
        128UL * 1024 * 1024);

    /// <summary>Return a copy with non-zero monotone thresholds.</summary>
    public readonly MemoryBudgetConfig Normalized()
    {
        var defaults = Default;
        var soft = SoftLimitBytes == 0 ? defaults.SoftLimitBytes : SoftLimitBytes;
        var hardCandidate = HardLimitBytes == 0 ? defaults.HardLimitBytes : HardLimitBytes;
        var hard = Math.Max(hardCandidate, soft);
        var emergencyCandidate = EmergencyLimitBytes == 0
            ? defaults.EmergencyLimitBytes
            : EmergencyLimitBytes;
        return new MemoryBudgetConfig(soft, hard, Math.Max(emergencyCandidate, hard));
    }
}

/// <summary>Tracks rendering-memory use and threshold violations.</summary>
public sealed class MemoryBudget
{
    public MemoryBudget(MemoryBudgetConfig config)
    {
        Config = config.Normalized();
    }

    private MemoryBudget(MemoryBudget source)
    {
        Config = source.Config;
        PeakBytes = source.PeakBytes;
        CurrentBytes = source.CurrentBytes;
        SoftViolations = source.SoftViolations;
        HardViolations = source.HardViolations;
    }

    public MemoryBudgetConfig Config { get; }

    public ulong CurrentBytes { get; private set; }

    public ulong PeakBytes { get; private set; }

    public uint SoftViolations { get; private set; }

    public uint HardViolations { get; private set; }

    public double UsageFraction => Config.SoftLimitBytes == 0
        ? 1.0
        : (double)CurrentBytes / Config.SoftLimitBytes;

    public GuardrailAlert? Check(ulong currentBytes)
    {
        CurrentBytes = currentBytes;
        PeakBytes = Math.Max(PeakBytes, currentBytes);

        if (currentBytes >= Config.EmergencyLimitBytes)
        {
            HardViolations = SaturatingIncrement(HardViolations);
            return new GuardrailAlert(
                GuardrailKind.Memory,
                AlertSeverity.Emergency,
                DegradationLevel.SkipFrame);
        }

        if (currentBytes >= Config.HardLimitBytes)
        {
            HardViolations = SaturatingIncrement(HardViolations);
            return new GuardrailAlert(
                GuardrailKind.Memory,
                AlertSeverity.Critical,
                DegradationLevel.Skeleton);
        }

        if (currentBytes >= Config.SoftLimitBytes)
        {
            SoftViolations = SaturatingIncrement(SoftViolations);
            return new GuardrailAlert(
                GuardrailKind.Memory,
                AlertSeverity.Warning,
                DegradationLevel.SimpleBorders);
        }

        return null;
    }

    public MemoryBudget Clone() => new(this);

    public void Reset()
    {
        PeakBytes = 0;
        CurrentBytes = 0;
        SoftViolations = 0;
        HardViolations = 0;
    }

    private static uint SaturatingIncrement(uint value) =>
        value == uint.MaxValue ? value : value + 1;
}

/// <summary>Policy for handling frames when the pending queue is full.</summary>
public enum QueueDropPolicy
{
    DropOldest,
    DropNewest,
    Backpressure,
}

/// <summary>Configuration for pending-frame queue depth limits.</summary>
public record struct QueueConfig
{
    public QueueConfig()
        : this(3, 8, 16, QueueDropPolicy.DropOldest)
    {
    }

    public QueueConfig(
        uint warnDepth,
        uint maxDepth,
        uint emergencyDepth,
        QueueDropPolicy dropPolicy)
    {
        WarnDepth = warnDepth;
        MaxDepth = maxDepth;
        EmergencyDepth = emergencyDepth;
        DropPolicy = dropPolicy;
    }

    public uint WarnDepth { get; set; }

    public uint MaxDepth { get; set; }

    public uint EmergencyDepth { get; set; }

    public QueueDropPolicy DropPolicy { get; set; }

    public static QueueConfig Default => new();

    public static QueueConfig Strict() => new(2, 4, 8, QueueDropPolicy.Backpressure);

    public static QueueConfig Relaxed() => new(8, 16, 32, QueueDropPolicy.DropOldest);
}

/// <summary>An action recommended by queue-depth guardrails.</summary>
public abstract record QueueAction
{
    private QueueAction()
    {
    }

    public sealed record None : QueueAction;

    public sealed record DropOldest(uint Count) : QueueAction;

    public sealed record DropNewest(uint Count) : QueueAction;

    public sealed record Backpressure : QueueAction;

    public bool DropsFrames() => this is DropOldest or DropNewest;
}

/// <summary>Tracks pending-frame queue depth, drops, and backpressure.</summary>
public sealed class QueueGuardrails
{
    public QueueGuardrails(QueueConfig config)
    {
        Config = config;
    }

    private QueueGuardrails(QueueGuardrails source)
    {
        Config = source.Config;
        PeakDepth = source.PeakDepth;
        CurrentDepth = source.CurrentDepth;
        TotalDrops = source.TotalDrops;
        TotalBackpressureEvents = source.TotalBackpressureEvents;
    }

    public QueueConfig Config { get; }

    public uint CurrentDepth { get; private set; }

    public uint PeakDepth { get; private set; }

    public ulong TotalDrops { get; private set; }

    public ulong TotalBackpressureEvents { get; private set; }

    public (GuardrailAlert? Alert, QueueAction Action) Check(uint currentDepth)
    {
        CurrentDepth = currentDepth;
        PeakDepth = Math.Max(PeakDepth, currentDepth);

        if (currentDepth >= Config.EmergencyDepth)
        {
            var excess = currentDepth > 0 ? currentDepth - 1 : 0;
            var action = SelectAction(excess);
            return (
                new GuardrailAlert(
                    GuardrailKind.QueueDepth,
                    AlertSeverity.Emergency,
                    DegradationLevel.SkipFrame),
                action);
        }

        if (currentDepth >= Config.MaxDepth)
        {
            var excess = currentDepth >= Config.WarnDepth
                ? currentDepth - Config.WarnDepth
                : 0;
            var action = SelectAction(excess);
            return (
                new GuardrailAlert(
                    GuardrailKind.QueueDepth,
                    AlertSeverity.Critical,
                    DegradationLevel.EssentialOnly),
                action);
        }

        if (currentDepth >= Config.WarnDepth)
        {
            return (
                new GuardrailAlert(
                    GuardrailKind.QueueDepth,
                    AlertSeverity.Warning,
                    DegradationLevel.SimpleBorders),
                new QueueAction.None());
        }

        return (null, new QueueAction.None());
    }

    public QueueGuardrails Clone() => new(this);

    public void Reset()
    {
        PeakDepth = 0;
        CurrentDepth = 0;
        TotalDrops = 0;
        TotalBackpressureEvents = 0;
    }

    private QueueAction SelectAction(uint excess)
    {
        switch (Config.DropPolicy)
        {
            case QueueDropPolicy.DropOldest:
                TotalDrops = SaturatingAdd(TotalDrops, excess);
                return new QueueAction.DropOldest(excess);

            case QueueDropPolicy.DropNewest:
                TotalDrops = SaturatingAdd(TotalDrops, excess);
                return new QueueAction.DropNewest(excess);

            case QueueDropPolicy.Backpressure:
                TotalBackpressureEvents = SaturatingIncrement(TotalBackpressureEvents);
                return new QueueAction.Backpressure();

            default:
                throw new ArgumentOutOfRangeException(nameof(Config.DropPolicy));
        }
    }

    private static ulong SaturatingAdd(ulong left, uint right) =>
        left > ulong.MaxValue - right ? ulong.MaxValue : left + right;

    private static ulong SaturatingIncrement(ulong value) =>
        value == ulong.MaxValue ? value : value + 1;
}

/// <summary>Composite configuration for memory and queue guardrails.</summary>
public sealed class GuardrailsConfig
{
    public GuardrailsConfig()
    {
    }

    public GuardrailsConfig(MemoryBudgetConfig memory, QueueConfig queue)
    {
        Memory = memory;
        Queue = queue;
    }

    public MemoryBudgetConfig Memory { get; set; } = MemoryBudgetConfig.Default;

    public QueueConfig Queue { get; set; } = QueueConfig.Default;

    public static GuardrailsConfig Default => new();

    public GuardrailsConfig Clone() => new(Memory, Queue);
}

/// <summary>A combined result from one frame guardrail check.</summary>
public sealed class GuardrailVerdict
{
    public GuardrailVerdict(
        IEnumerable<GuardrailAlert> alerts,
        QueueAction queueAction,
        DegradationLevel recommendedLevel)
    {
        ArgumentNullException.ThrowIfNull(alerts);
        ArgumentNullException.ThrowIfNull(queueAction);
        Alerts = alerts.ToList();
        QueueAction = queueAction;
        RecommendedLevel = recommendedLevel;
    }

    public List<GuardrailAlert> Alerts { get; }

    public QueueAction QueueAction { get; set; }

    public DegradationLevel RecommendedLevel { get; set; }

    public bool ShouldDropFrame() => RecommendedLevel >= DegradationLevel.SkipFrame;

    public bool ShouldDegrade() =>
        RecommendedLevel > DegradationLevel.Full
        && RecommendedLevel < DegradationLevel.SkipFrame;

    public bool IsClear() => Alerts.Count == 0;

    public AlertSeverity? MaxSeverity() => Alerts.Count == 0
        ? null
        : Alerts.Max(static alert => alert.Severity);

    public GuardrailVerdict Clone() => new(Alerts, QueueAction, RecommendedLevel);
}

/// <summary>Unified rendering-memory and pending-frame queue guardrails.</summary>
public sealed class FrameGuardrails
{
    /// <summary>Source-model size of one packed render cell.</summary>
    public const ulong CellSizeBytes = 16;

    private readonly MemoryBudget _memory;
    private readonly QueueGuardrails _queue;
    private ulong _framesChecked;
    private ulong _framesWithAlerts;

    public FrameGuardrails(GuardrailsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _memory = new MemoryBudget(config.Memory);
        _queue = new QueueGuardrails(config.Queue);
    }

    private FrameGuardrails(FrameGuardrails source)
    {
        _memory = source._memory.Clone();
        _queue = source._queue.Clone();
        _framesChecked = source._framesChecked;
        _framesWithAlerts = source._framesWithAlerts;
    }

    public MemoryBudget Memory => _memory;

    public QueueGuardrails Queue => _queue;

    public ulong FramesChecked => _framesChecked;

    public ulong FramesWithAlerts => _framesWithAlerts;

    public double AlertRate => _framesChecked == 0
        ? 0.0
        : (double)_framesWithAlerts / _framesChecked;

    public GuardrailVerdict CheckFrame(ulong memoryBytes, uint queueDepth)
    {
        _framesChecked = SaturatingIncrement(_framesChecked);
        var alerts = new List<GuardrailAlert>(2);
        var maximumLevel = DegradationLevel.Full;

        if (_memory.Check(memoryBytes) is { } memoryAlert)
        {
            maximumLevel = Max(maximumLevel, memoryAlert.RecommendedLevel);
            alerts.Add(memoryAlert);
        }

        var (queueAlert, queueAction) = _queue.Check(queueDepth);
        if (queueAlert is { } alert)
        {
            maximumLevel = Max(maximumLevel, alert.RecommendedLevel);
            alerts.Add(alert);
        }

        if (alerts.Count != 0)
        {
            _framesWithAlerts = SaturatingIncrement(_framesWithAlerts);
        }

        return new GuardrailVerdict(alerts, queueAction, maximumLevel);
    }

    public GuardrailSnapshot Snapshot() => new(
        _memory.CurrentBytes,
        _memory.PeakBytes,
        _memory.UsageFraction,
        _memory.SoftViolations,
        _memory.HardViolations,
        _queue.CurrentDepth,
        _queue.PeakDepth,
        _queue.TotalDrops,
        _queue.TotalBackpressureEvents,
        _framesChecked,
        _framesWithAlerts);

    public FrameGuardrails Clone() => new(this);

    public void Reset()
    {
        _memory.Reset();
        _queue.Reset();
        _framesChecked = 0;
        _framesWithAlerts = 0;
    }

    /// <summary>Compute the source-model cell-array footprint of a buffer.</summary>
    public static ulong BufferMemoryBytes(ushort width, ushort height) =>
        (ulong)width * height * CellSizeBytes;

    private static DegradationLevel Max(DegradationLevel left, DegradationLevel right) =>
        left >= right ? left : right;

    private static ulong SaturatingIncrement(ulong value) =>
        value == ulong.MaxValue ? value : value + 1;
}

/// <summary>An allocation-free diagnostic snapshot of guardrail state.</summary>
public record struct GuardrailSnapshot(
    ulong MemoryBytes,
    ulong MemoryPeakBytes,
    double MemoryUsageFraction,
    uint MemorySoftViolations,
    uint MemoryHardViolations,
    uint QueueDepth,
    uint QueuePeakDepth,
    ulong QueueTotalDrops,
    ulong QueueTotalBackpressure,
    ulong FramesChecked,
    ulong FramesWithAlerts)
{
    public readonly string ToJsonl() => string.Format(
        CultureInfo.InvariantCulture,
        "{{\"memory_bytes\":{0},\"memory_peak\":{1},\"memory_frac\":{2:F4},"
        + "\"mem_soft_violations\":{3},\"mem_hard_violations\":{4},"
        + "\"queue_depth\":{5},\"queue_peak\":{6},\"queue_drops\":{7},"
        + "\"queue_backpressure\":{8},\"frames_checked\":{9},\"frames_alerted\":{10}}}",
        MemoryBytes,
        MemoryPeakBytes,
        MemoryUsageFraction,
        MemorySoftViolations,
        MemoryHardViolations,
        QueueDepth,
        QueuePeakDepth,
        QueueTotalDrops,
        QueueTotalBackpressure,
        FramesChecked,
        FramesWithAlerts);
}
