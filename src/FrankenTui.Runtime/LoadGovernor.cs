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

public sealed record LoadGovernorConfig(
    bool Enabled = true,
    BudgetControllerConfig? BudgetController = null)
{
    public BudgetControllerConfig EffectiveBudgetController => BudgetController ?? BudgetControllerConfig.Default;

    public static LoadGovernorConfig Default { get; } = new();

    public static LoadGovernorConfig Disabled { get; } = new(Enabled: false);

    public LoadGovernorConfig WithEnabled(bool enabled) => this with { Enabled = enabled };

    public LoadGovernorConfig WithBudgetController(BudgetControllerConfig config) =>
        this with { BudgetController = config ?? throw new ArgumentNullException(nameof(config)) };
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
