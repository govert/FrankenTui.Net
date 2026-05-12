using System.Globalization;

namespace FrankenTui.Runtime;

public enum RuntimeDegradationLevel
{
    Full = 0,
    SimpleBorders = 1,
    NoStyling = 2,
    EssentialOnly = 3,
    Skeleton = 4,
    SkipFrame = 5
}

public static class RuntimeDegradationLevelExtensions
{
    public static RuntimeDegradationLevel Next(this RuntimeDegradationLevel level) =>
        level switch
        {
            RuntimeDegradationLevel.Full => RuntimeDegradationLevel.SimpleBorders,
            RuntimeDegradationLevel.SimpleBorders => RuntimeDegradationLevel.NoStyling,
            RuntimeDegradationLevel.NoStyling => RuntimeDegradationLevel.EssentialOnly,
            RuntimeDegradationLevel.EssentialOnly => RuntimeDegradationLevel.Skeleton,
            RuntimeDegradationLevel.Skeleton => RuntimeDegradationLevel.SkipFrame,
            _ => RuntimeDegradationLevel.SkipFrame
        };

    public static RuntimeDegradationLevel Previous(this RuntimeDegradationLevel level) =>
        level switch
        {
            RuntimeDegradationLevel.SkipFrame => RuntimeDegradationLevel.Skeleton,
            RuntimeDegradationLevel.Skeleton => RuntimeDegradationLevel.EssentialOnly,
            RuntimeDegradationLevel.EssentialOnly => RuntimeDegradationLevel.NoStyling,
            RuntimeDegradationLevel.NoStyling => RuntimeDegradationLevel.SimpleBorders,
            _ => RuntimeDegradationLevel.Full
        };

    public static string Label(this RuntimeDegradationLevel level) =>
        level switch
        {
            RuntimeDegradationLevel.SimpleBorders => "SIMPLE_BORDERS",
            RuntimeDegradationLevel.NoStyling => "NO_STYLING",
            RuntimeDegradationLevel.EssentialOnly => "ESSENTIAL_ONLY",
            RuntimeDegradationLevel.Skeleton => "SKELETON",
            RuntimeDegradationLevel.SkipFrame => "SKIP_FRAME",
            _ => "FULL"
        };
}

public sealed record PidGains(
    double Kp = 0.5,
    double Ki = 0.05,
    double Kd = 0.2,
    double IntegralMax = 5.0)
{
    public static PidGains Default { get; } = new();
}

public sealed record EProcessConfig(
    double Lambda = 0.5,
    double Alpha = 0.05,
    double Beta = 0.5,
    double SigmaEmaDecay = 0.9,
    double SigmaFloorMilliseconds = 1.0,
    uint WarmupFrames = 10)
{
    public static EProcessConfig Default { get; } = new();
}

public sealed record BudgetControllerConfig(
    TimeSpan TargetFrameTime,
    double DegradeThreshold = 0.30,
    double UpgradeThreshold = 0.20,
    int CooldownFrames = 3,
    RuntimeDegradationLevel DegradationFloor = RuntimeDegradationLevel.SimpleBorders,
    PidGains? Pid = null,
    EProcessConfig? EProcess = null)
{
    public PidGains EffectivePid => Pid ?? PidGains.Default;

    public EProcessConfig EffectiveEProcess => EProcess ?? EProcessConfig.Default;

    public static BudgetControllerConfig Default { get; } = new(TimeSpan.FromMilliseconds(16));
}

public enum RuntimeLoadMode
{
    Healthy,
    Stressed,
    Degraded,
    Recovered,
    Unsafe
}

public static class RuntimeLoadModeExtensions
{
    public static string Label(this RuntimeLoadMode mode) =>
        mode switch
        {
            RuntimeLoadMode.Stressed => "stressed",
            RuntimeLoadMode.Degraded => "degraded",
            RuntimeLoadMode.Recovered => "recovered",
            RuntimeLoadMode.Unsafe => "unsafe",
            _ => "healthy"
        };
}

public enum RuntimePressureClass
{
    SteadyState,
    SoftOverload,
    HardOverload,
    Unsafe
}

public static class RuntimePressureClassExtensions
{
    public static string Label(this RuntimePressureClass pressure) =>
        pressure switch
        {
            RuntimePressureClass.SoftOverload => "soft_overload",
            RuntimePressureClass.HardOverload => "hard_overload",
            RuntimePressureClass.Unsafe => "unsafe",
            _ => "steady_state"
        };
}

public enum RuntimeWorkDisposition
{
    AdmitAll,
    CoalesceVisibleDeferBackground,
    DeferBackgroundDropBestEffort,
    ReadmitAfterHysteresis,
    FailFastStrictGuarantee
}

public static class RuntimeWorkDispositionExtensions
{
    public static string Label(this RuntimeWorkDisposition disposition) =>
        disposition switch
        {
            RuntimeWorkDisposition.CoalesceVisibleDeferBackground => "coalesce_visible_defer_background",
            RuntimeWorkDisposition.DeferBackgroundDropBestEffort => "defer_background_drop_best_effort",
            RuntimeWorkDisposition.ReadmitAfterHysteresis => "readmit_after_hysteresis",
            RuntimeWorkDisposition.FailFastStrictGuarantee => "fail_fast_strict_guarantee",
            _ => "admit_all"
        };
}

public sealed record LoadGovernorPolicy(
    double StressedQueueWatermark = 0.5,
    double DegradedQueueWatermark = 0.8,
    double RecoveryQueueWatermark = 0.25,
    byte RecoveryIntervals = 3,
    double BudgetOverrunSoftRatio = 1.0)
{
    public static LoadGovernorPolicy Default { get; } = new();

    public LoadGovernorPolicy Normalized()
    {
        var recovery = NormalizeRatio(RecoveryQueueWatermark, 0.25);
        var stressed = Math.Max(NormalizeRatio(StressedQueueWatermark, 0.5), recovery);
        var degraded = Math.Max(NormalizeRatio(DegradedQueueWatermark, 0.8), stressed);
        return this with
        {
            RecoveryQueueWatermark = recovery,
            StressedQueueWatermark = stressed,
            DegradedQueueWatermark = degraded,
            RecoveryIntervals = (byte)Math.Max(RecoveryIntervals, (byte)1),
            BudgetOverrunSoftRatio = double.IsFinite(BudgetOverrunSoftRatio) && BudgetOverrunSoftRatio > 0
                ? BudgetOverrunSoftRatio
                : 1.0
        };
    }

    private static double NormalizeRatio(double value, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : fallback;
}

public sealed record LoadGovernorConfig(
    bool Enabled = true,
    BudgetControllerConfig? BudgetController = null,
    LoadGovernorPolicy? Policy = null)
{
    public BudgetControllerConfig EffectiveBudgetController => BudgetController ?? BudgetControllerConfig.Default;

    public LoadGovernorPolicy EffectivePolicy => (Policy ?? LoadGovernorPolicy.Default).Normalized();

    public static LoadGovernorConfig Default { get; } = new();

    public static LoadGovernorConfig Disabled { get; } = new(Enabled: false);

    public LoadGovernorConfig WithEnabled(bool enabled) => this with { Enabled = enabled };

    public LoadGovernorConfig WithBudgetController(BudgetControllerConfig config) =>
        this with { BudgetController = config ?? throw new ArgumentNullException(nameof(config)) };

    public LoadGovernorConfig WithPolicy(LoadGovernorPolicy policy) =>
        this with { Policy = (policy ?? throw new ArgumentNullException(nameof(policy))).Normalized() };
}

public sealed record RuntimeLoadGovernorObservation(
    double FrameDurationMs,
    double BudgetMs,
    RuntimeDegradationLevel DegradationLevel = RuntimeDegradationLevel.Full,
    ulong QueueInFlight = 0,
    int? QueueMaxDepth = null,
    ulong QueueDroppedTotal = 0,
    bool ResizeCoalescingActive = false,
    bool StrictSemanticsViolation = false);

public sealed record RuntimeLoadGovernorSnapshot(
    RuntimeLoadMode Mode,
    RuntimeLoadMode ModeBefore,
    RuntimePressureClass PressureClass,
    RuntimeWorkDisposition WorkDisposition,
    string ReasonCode,
    bool Transition,
    bool StrictSemanticsPreserved,
    ulong QueueInFlight,
    int? QueueMaxDepth,
    ulong QueueDroppedDelta,
    bool ResizeCoalescingActive,
    byte RecoveryIntervalsObserved,
    byte RecoveryIntervalsRequired,
    ulong DeferredWorkTotal,
    ulong CoalescedWorkTotal,
    ulong DroppedWorkTotal);

public sealed class ConservativeRuntimeLoadGovernor
{
    private readonly bool _enabled;
    private readonly LoadGovernorPolicy _policy;
    private readonly int _maxQueueDepth;
    private RuntimeLoadMode _mode = RuntimeLoadMode.Healthy;
    private byte _recoveryIntervalsObserved;
    private ulong _lastQueueDropped;
    private bool _queueBaselineInitialized;
    private ulong _deferredWorkTotal;
    private ulong _coalescedWorkTotal;
    private ulong _droppedWorkTotal;

    public ConservativeRuntimeLoadGovernor(LoadGovernorConfig config, int maxQueueDepth = 0)
    {
        ArgumentNullException.ThrowIfNull(config);
        _enabled = config.Enabled;
        _policy = config.EffectivePolicy;
        _maxQueueDepth = Math.Max(maxQueueDepth, 0);
    }

    public RuntimeLoadMode Mode => _mode;

    public RuntimeLoadGovernorSnapshot Observe(RuntimeLoadGovernorObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (!_enabled)
        {
            return Snapshot(
                RuntimeLoadMode.Healthy,
                RuntimeLoadMode.Healthy,
                RuntimePressureClass.SteadyState,
                RuntimeWorkDisposition.AdmitAll,
                "governor_disabled",
                transition: false,
                strictSemanticsPreserved: true,
                observation,
                droppedDelta: 0);
        }

        var droppedDelta = QueueDroppedDelta(observation.QueueDroppedTotal);
        var pressure = ClassifyPressure(observation, droppedDelta);
        var modeBefore = _mode;
        var reason = ReasonCode(observation, pressure, droppedDelta);

        switch (pressure)
        {
            case RuntimePressureClass.Unsafe:
                _mode = RuntimeLoadMode.Unsafe;
                _recoveryIntervalsObserved = 0;
                break;
            case RuntimePressureClass.HardOverload:
                _mode = RuntimeLoadMode.Degraded;
                _recoveryIntervalsObserved = 0;
                break;
            case RuntimePressureClass.SoftOverload:
                if (_mode != RuntimeLoadMode.Degraded)
                {
                    _mode = RuntimeLoadMode.Stressed;
                }

                _recoveryIntervalsObserved = 0;
                break;
            default:
                ObserveSteadyInterval();
                break;
        }

        RecordWorkDisposition(observation, droppedDelta);
        var disposition = DispositionForMode(_mode);
        var snapshotReason = _mode == RuntimeLoadMode.Recovered
            ? "recovery_hysteresis_satisfied"
            : modeBefore == RuntimeLoadMode.Recovered && _mode == RuntimeLoadMode.Healthy
                ? "recovered_interval_closed"
                : reason;
        return Snapshot(
            _mode,
            modeBefore,
            pressure,
            disposition,
            snapshotReason,
            modeBefore != _mode,
            pressure != RuntimePressureClass.Unsafe,
            observation,
            droppedDelta);
    }

    private ulong QueueDroppedDelta(ulong droppedTotal)
    {
        if (!_queueBaselineInitialized)
        {
            _queueBaselineInitialized = true;
            _lastQueueDropped = droppedTotal;
            return 0;
        }

        var delta = droppedTotal >= _lastQueueDropped ? droppedTotal - _lastQueueDropped : 0;
        _lastQueueDropped = droppedTotal;
        return delta;
    }

    private RuntimePressureClass ClassifyPressure(RuntimeLoadGovernorObservation observation, ulong droppedDelta)
    {
        if (observation.StrictSemanticsViolation)
        {
            return RuntimePressureClass.Unsafe;
        }

        if (droppedDelta > 0 ||
            QueueRatio(observation.QueueInFlight) is { } degradedRatio && degradedRatio >= _policy.DegradedQueueWatermark ||
            observation.DegradationLevel > RuntimeDegradationLevel.Full)
        {
            return RuntimePressureClass.HardOverload;
        }

        if (QueueRatio(observation.QueueInFlight) is { } stressedRatio && stressedRatio >= _policy.StressedQueueWatermark ||
            observation.ResizeCoalescingActive ||
            observation.FrameDurationMs > Math.Max(observation.BudgetMs, 0.001) * _policy.BudgetOverrunSoftRatio)
        {
            return RuntimePressureClass.SoftOverload;
        }

        return RuntimePressureClass.SteadyState;
    }

    private void ObserveSteadyInterval()
    {
        switch (_mode)
        {
            case RuntimeLoadMode.Degraded:
                _recoveryIntervalsObserved = (byte)Math.Min(_recoveryIntervalsObserved + 1, _policy.RecoveryIntervals);
                if (_recoveryIntervalsObserved >= _policy.RecoveryIntervals)
                {
                    _mode = RuntimeLoadMode.Recovered;
                }
                break;
            case RuntimeLoadMode.Recovered:
            case RuntimeLoadMode.Stressed:
                _mode = RuntimeLoadMode.Healthy;
                _recoveryIntervalsObserved = 0;
                break;
            case RuntimeLoadMode.Healthy:
                _recoveryIntervalsObserved = 0;
                break;
        }
    }

    private void RecordWorkDisposition(RuntimeLoadGovernorObservation observation, ulong droppedDelta)
    {
        if (droppedDelta > 0)
        {
            _droppedWorkTotal += droppedDelta;
        }

        if (_mode == RuntimeLoadMode.Stressed && observation.ResizeCoalescingActive)
        {
            _coalescedWorkTotal++;
        }
        else if (_mode == RuntimeLoadMode.Degraded)
        {
            _deferredWorkTotal++;
            if (observation.ResizeCoalescingActive)
            {
                _coalescedWorkTotal++;
            }
        }
    }

    private string ReasonCode(RuntimeLoadGovernorObservation observation, RuntimePressureClass pressure, ulong droppedDelta) =>
        pressure switch
        {
            RuntimePressureClass.Unsafe => "strict_semantics_violation",
            RuntimePressureClass.HardOverload when droppedDelta > 0 => "effect_queue_drop",
            RuntimePressureClass.HardOverload when QueueRatio(observation.QueueInFlight) is { } ratio && ratio >= _policy.DegradedQueueWatermark => "queue_degraded_watermark",
            RuntimePressureClass.HardOverload => "budget_degradation_active",
            RuntimePressureClass.SoftOverload when QueueRatio(observation.QueueInFlight) is { } ratio && ratio >= _policy.StressedQueueWatermark => "queue_stressed_watermark",
            RuntimePressureClass.SoftOverload when observation.ResizeCoalescingActive => "resize_coalescing_active",
            RuntimePressureClass.SoftOverload => "frame_budget_overrun",
            RuntimePressureClass.SteadyState when _mode == RuntimeLoadMode.Degraded => "recovery_hysteresis_pending",
            _ => "steady_state"
        };

    private double? QueueRatio(ulong inFlight) =>
        _maxQueueDepth > 0 ? inFlight / (double)_maxQueueDepth : null;

    private static RuntimeWorkDisposition DispositionForMode(RuntimeLoadMode mode) =>
        mode switch
        {
            RuntimeLoadMode.Stressed => RuntimeWorkDisposition.CoalesceVisibleDeferBackground,
            RuntimeLoadMode.Degraded => RuntimeWorkDisposition.DeferBackgroundDropBestEffort,
            RuntimeLoadMode.Recovered => RuntimeWorkDisposition.ReadmitAfterHysteresis,
            RuntimeLoadMode.Unsafe => RuntimeWorkDisposition.FailFastStrictGuarantee,
            _ => RuntimeWorkDisposition.AdmitAll
        };

    private RuntimeLoadGovernorSnapshot Snapshot(
        RuntimeLoadMode mode,
        RuntimeLoadMode modeBefore,
        RuntimePressureClass pressureClass,
        RuntimeWorkDisposition disposition,
        string reasonCode,
        bool transition,
        bool strictSemanticsPreserved,
        RuntimeLoadGovernorObservation observation,
        ulong droppedDelta) =>
        new(
            mode,
            modeBefore,
            pressureClass,
            disposition,
            reasonCode,
            transition,
            strictSemanticsPreserved,
            observation.QueueInFlight,
            observation.QueueMaxDepth ?? (_maxQueueDepth > 0 ? _maxQueueDepth : null),
            droppedDelta,
            observation.ResizeCoalescingActive,
            _recoveryIntervalsObserved,
            _policy.RecoveryIntervals,
            _deferredWorkTotal,
            _coalescedWorkTotal,
            _droppedWorkTotal);
}

public sealed record LoadGovernorDecision(
    RuntimeDegradationLevel LevelBefore,
    RuntimeDegradationLevel LevelAfter,
    string Action,
    string Reason,
    double FrameDurationMs,
    double TargetFrameMs,
    double NormalizedError,
    double PidOutput,
    double PidP,
    double PidI,
    double PidD,
    double EProcessValue,
    double EProcessSigmaMs,
    uint FramesObserved,
    int FramesSinceChange,
    double PidGateThreshold,
    double PidGateMargin,
    double EvidenceThreshold,
    double EvidenceMargin,
    bool EProcessInWarmup,
    ulong TransitionSeq,
    ulong TransitionCorrelationId);

internal sealed class RuntimeLoadGovernor
{
    private readonly LoadGovernorConfig _config;
    private readonly PidState _pid = new();
    private readonly EProcessState _eprocess = new();
    private RuntimeDegradationLevel _level = RuntimeDegradationLevel.Full;
    private int _framesSinceChange;
    private ulong _transitionSeq;
    private ulong _lastTransitionCorrelationId;
    private LoadGovernorDecision _lastDecision = new(
        RuntimeDegradationLevel.Full,
        RuntimeDegradationLevel.Full,
        "stay",
        "initial",
        0,
        BudgetControllerConfig.Default.TargetFrameTime.TotalMilliseconds,
        0,
        PidOutput: 0,
        PidP: 0,
        PidI: 0,
        PidD: 0,
        EProcessValue: 1,
        EProcessSigmaMs: EProcessConfig.Default.SigmaFloorMilliseconds,
        FramesObserved: 0,
        FramesSinceChange: 0,
        PidGateThreshold: 0,
        PidGateMargin: 0,
        EvidenceThreshold: 0,
        EvidenceMargin: 0,
        EProcessInWarmup: true,
        TransitionSeq: 0,
        TransitionCorrelationId: 0);

    public RuntimeLoadGovernor(LoadGovernorConfig config)
    {
        _config = config;
    }

    public RuntimeDegradationLevel Level => _config.Enabled ? _level : RuntimeDegradationLevel.Full;

    public LoadGovernorDecision LastDecision => _lastDecision;

    public LoadGovernorDecision Observe(TimeSpan frameDuration)
    {
        if (!_config.Enabled)
        {
            _level = RuntimeDegradationLevel.Full;
            _pid.Reset();
            _eprocess.Reset();
            _framesSinceChange = 0;
            _transitionSeq = 0;
            _lastTransitionCorrelationId = 0;
            _lastDecision = CreateDecision(_level, _level, "stay", "disabled", frameDuration, 0);
            return _lastDecision;
        }

        var controller = _config.EffectiveBudgetController;
        var targetMs = Math.Max(controller.TargetFrameTime.TotalMilliseconds, 0.001);
        var frameMs = frameDuration.TotalMilliseconds;
        var normalizedError = (frameMs - targetMs) / targetMs;
        var pidOutput = _pid.Update(normalizedError, controller.EffectivePid);
        _eprocess.Update(frameMs, targetMs, controller.EffectiveEProcess);
        _framesSinceChange++;

        var before = _level;
        var after = before;
        var action = "stay";
        var reason = "within_threshold_band";
        var pidGateThreshold = 0.0;
        var pidGateMargin = 0.0;
        var evidenceThreshold = 0.0;
        var evidenceMargin = 0.0;

        if (_framesSinceChange < Math.Max(controller.CooldownFrames, 0))
        {
            reason = "cooldown_active";
        }
        else if (pidOutput > controller.DegradeThreshold)
        {
            pidGateThreshold = controller.DegradeThreshold;
            pidGateMargin = pidOutput - pidGateThreshold;
            evidenceThreshold = 1.0 / controller.EffectiveEProcess.Alpha;
            evidenceMargin = _eprocess.Value - evidenceThreshold;

            if (before == RuntimeDegradationLevel.SkipFrame)
            {
                reason = "at_max_degradation";
            }
            else if (before >= controller.DegradationFloor)
            {
                reason = "at_degradation_floor";
            }
            else if (_eprocess.ShouldDegrade(controller.EffectiveEProcess))
            {
                after = before.Next();
                if (after > controller.DegradationFloor)
                {
                    after = controller.DegradationFloor;
                }

                action = "degrade";
                reason = "overload_evidence_passed";
            }
            else
            {
                reason = "overload_evidence_insufficient";
            }
        }
        else if (pidOutput < -controller.UpgradeThreshold)
        {
            pidGateThreshold = -controller.UpgradeThreshold;
            pidGateMargin = (-pidOutput) - controller.UpgradeThreshold;
            evidenceThreshold = controller.EffectiveEProcess.Beta;
            evidenceMargin = evidenceThreshold - _eprocess.Value;

            if (before == RuntimeDegradationLevel.Full)
            {
                reason = "at_full_quality";
            }
            else if (_eprocess.ShouldUpgrade(controller.EffectiveEProcess))
            {
                after = before.Previous();
                action = "upgrade";
                reason = "underload_evidence_passed";
            }
            else
            {
                reason = "underload_evidence_insufficient";
            }
        }

        if (after != before)
        {
            _framesSinceChange = 0;
            _transitionSeq++;
            _lastTransitionCorrelationId = (_transitionSeq << 32) ^ _eprocess.FramesObserved;
        }

        _level = after;
        _lastDecision = CreateDecision(
            before,
            after,
            action,
            reason,
            frameDuration,
            normalizedError,
            pidGateThreshold,
            pidGateMargin,
            evidenceThreshold,
            evidenceMargin);
        return _lastDecision;
    }

    private LoadGovernorDecision CreateDecision(
        RuntimeDegradationLevel before,
        RuntimeDegradationLevel after,
        string action,
        string reason,
        TimeSpan frameDuration,
        double normalizedError,
        double pidGateThreshold = 0,
        double pidGateMargin = 0,
        double evidenceThreshold = 0,
        double evidenceMargin = 0)
    {
        var controller = _config.EffectiveBudgetController;
        var targetMs = controller.TargetFrameTime.TotalMilliseconds;
        var eprocessConfig = controller.EffectiveEProcess;
        return new LoadGovernorDecision(
            before,
            after,
            action,
            reason,
            frameDuration.TotalMilliseconds,
            targetMs,
            double.IsFinite(normalizedError) ? normalizedError : 0,
            double.IsFinite(_pid.Output) ? _pid.Output : 0,
            double.IsFinite(_pid.LastP) ? _pid.LastP : 0,
            double.IsFinite(_pid.LastI) ? _pid.LastI : 0,
            double.IsFinite(_pid.LastD) ? _pid.LastD : 0,
            double.IsFinite(_eprocess.Value) ? _eprocess.Value : 1,
            _eprocess.SigmaMs(eprocessConfig),
            _eprocess.FramesObserved,
            _framesSinceChange,
            pidGateThreshold,
            pidGateMargin,
            evidenceThreshold,
            evidenceMargin,
            _eprocess.FramesObserved < eprocessConfig.WarmupFrames,
            _transitionSeq,
            _lastTransitionCorrelationId);
    }

    public static string FormatNormalizedError(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed class PidState
    {
        private double _integral;
        private double _previousError;

        public double Output { get; private set; }

        public double LastP { get; private set; }

        public double LastI { get; private set; }

        public double LastD { get; private set; }

        public double Update(double error, PidGains gains)
        {
            if (!double.IsFinite(error))
            {
                Output = 0;
                return Output;
            }

            _integral = Math.Clamp(_integral + error, -gains.IntegralMax, gains.IntegralMax);
            var derivative = error - _previousError;
            _previousError = error;

            LastP = gains.Kp * error;
            LastI = gains.Ki * _integral;
            LastD = gains.Kd * derivative;
            Output = LastP + LastI + LastD;
            return Output;
        }

        public void Reset()
        {
            _integral = 0;
            _previousError = 0;
            Output = 0;
            LastP = 0;
            LastI = 0;
            LastD = 0;
        }
    }

    private sealed class EProcessState
    {
        private double _meanEma;
        private double _sigmaEma;

        public double Value { get; private set; } = 1.0;

        public uint FramesObserved { get; private set; }

        public double SigmaMs(EProcessConfig config) =>
            Math.Max(_sigmaEma, config.SigmaFloorMilliseconds);

        public void Update(double frameTimeMs, double targetMs, EProcessConfig config)
        {
            FramesObserved++;
            if (FramesObserved == 1)
            {
                _meanEma = frameTimeMs;
                _sigmaEma = config.SigmaFloorMilliseconds;
            }
            else
            {
                var decay = Math.Clamp(config.SigmaEmaDecay, 0, 1);
                _meanEma = decay * _meanEma + (1.0 - decay) * frameTimeMs;
                var deviation = Math.Abs(frameTimeMs - _meanEma);
                _sigmaEma = decay * _sigmaEma + (1.0 - decay) * deviation;
            }

            var sigma = Math.Max(_sigmaEma, config.SigmaFloorMilliseconds);
            var residual = (frameTimeMs - targetMs) / sigma;
            var logFactor = config.Lambda * residual - config.Lambda * config.Lambda / 2.0;
            if (double.IsFinite(logFactor))
            {
                Value = Math.Clamp(Value * Math.Exp(logFactor), 1e-10, 1e10);
            }
        }

        public bool ShouldDegrade(EProcessConfig config) =>
            FramesObserved >= config.WarmupFrames && Value > 1.0 / config.Alpha;

        public bool ShouldUpgrade(EProcessConfig config) =>
            FramesObserved < config.WarmupFrames || Value < config.Beta;

        public void Reset()
        {
            _meanEma = 0;
            _sigmaEma = 0;
            Value = 1.0;
            FramesObserved = 0;
        }
    }
}
