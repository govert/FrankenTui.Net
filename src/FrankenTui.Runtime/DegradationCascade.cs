using System.Globalization;
using System.Numerics;
using System.Text.Json;
using FrankenTui.Render;

namespace FrankenTui.Runtime;

public sealed record RuntimeConformalConfig(
    double Alpha = 0.05,
    int MinSamples = 20,
    int WindowSize = 256,
    double DefaultResidualMicroseconds = 10_000.0)
{
    public static RuntimeConformalConfig Default { get; } = new();
}

public enum RuntimeModeBucket
{
    Inline,
    InlineAuto,
    AltScreen
}

public enum RuntimeDiffBucket
{
    Full,
    DirtyRows,
    FullRedraw
}

public readonly record struct RuntimeConformalBucketKey(
    RuntimeModeBucket Mode,
    RuntimeDiffBucket Diff,
    byte SizeBucket)
{
    public static RuntimeConformalBucketKey Default { get; } = new(
        RuntimeModeBucket.AltScreen,
        RuntimeDiffBucket.Full,
        CalculateSizeBucket(80, 24));

    public static RuntimeConformalBucketKey FromContext(
        bool inlineMode,
        bool inlineAuto,
        DiffStrategy diffStrategy,
        ushort columns,
        ushort rows) =>
        new(
            inlineAuto
                ? RuntimeModeBucket.InlineAuto
                : inlineMode ? RuntimeModeBucket.Inline : RuntimeModeBucket.AltScreen,
            diffStrategy switch
            {
                DiffStrategy.DirtyRows or DiffStrategy.SignificantDirtyRows => RuntimeDiffBucket.DirtyRows,
                DiffStrategy.FullRedraw => RuntimeDiffBucket.FullRedraw,
                _ => RuntimeDiffBucket.Full
            },
            CalculateSizeBucket(columns, rows));

    public override string ToString() =>
        $"{ModeLabel(Mode)}:{DiffLabel(Diff)}:{SizeBucket.ToString(CultureInfo.InvariantCulture)}";

    public static string ModeLabel(RuntimeModeBucket mode) =>
        mode switch
        {
            RuntimeModeBucket.Inline => "inline",
            RuntimeModeBucket.InlineAuto => "inline_auto",
            _ => "altscreen"
        };

    public static string DiffLabel(RuntimeDiffBucket diff) =>
        diff switch
        {
            RuntimeDiffBucket.DirtyRows => "dirty",
            RuntimeDiffBucket.FullRedraw => "redraw",
            _ => "full"
        };

    public static byte CalculateSizeBucket(ushort columns, ushort rows)
    {
        var area = (uint)columns * rows;
        if (area == 0)
        {
            return 0;
        }

        var bucket = 31 - BitOperations.LeadingZeroCount(area);
        return (byte)Math.Clamp(bucket, 0, byte.MaxValue);
    }
}

public sealed record RuntimeConformalPrediction(
    double UpperMicroseconds,
    bool Risk,
    double Confidence,
    RuntimeConformalBucketKey Bucket,
    int SampleCount,
    double Quantile,
    byte FallbackLevel,
    int WindowSize,
    ulong ResetCount,
    double YHatMicroseconds,
    double BudgetMicroseconds)
{
    public string ToJsonl() =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schema"] = "conformal-v1",
            ["upper_us"] = UpperMicroseconds,
            ["risk"] = Risk,
            ["confidence"] = Confidence,
            ["bucket"] = Bucket.ToString(),
            ["samples"] = SampleCount,
            ["quantile"] = Quantile,
            ["fallback_level"] = FallbackLevel,
            ["window"] = WindowSize,
            ["resets"] = ResetCount,
            ["y_hat"] = YHatMicroseconds,
            ["budget_us"] = BudgetMicroseconds
        });
}

public sealed record RuntimeConformalUpdate(
    double ResidualMicroseconds,
    RuntimeConformalBucketKey Bucket,
    int SampleCount);

public sealed class RuntimeConformalPredictor
{
    private readonly Dictionary<RuntimeConformalBucketKey, Queue<double>> _buckets = [];
    private readonly RuntimeConformalConfig _config;

    public RuntimeConformalPredictor(RuntimeConformalConfig? config = null)
    {
        _config = config ?? RuntimeConformalConfig.Default;
    }

    public RuntimeConformalConfig Config => _config;

    public ulong ResetCount { get; private set; }

    public int BucketSamples(RuntimeConformalBucketKey key) =>
        _buckets.TryGetValue(key, out var values) ? values.Count : 0;

    public void ResetAll()
    {
        _buckets.Clear();
        ResetCount++;
    }

    public void ResetBucket(RuntimeConformalBucketKey key)
    {
        if (_buckets.TryGetValue(key, out var values))
        {
            values.Clear();
            ResetCount++;
        }
    }

    public RuntimeConformalUpdate Observe(
        RuntimeConformalBucketKey key,
        double yHatMicroseconds,
        double observedMicroseconds)
    {
        var residual = observedMicroseconds - yHatMicroseconds;
        if (!double.IsFinite(residual))
        {
            return new RuntimeConformalUpdate(residual, key, BucketSamples(key));
        }

        if (!_buckets.TryGetValue(key, out var values))
        {
            values = new Queue<double>();
            _buckets[key] = values;
        }

        values.Enqueue(residual);
        var window = Math.Max(_config.WindowSize, 1);
        while (values.Count > window)
        {
            values.Dequeue();
        }

        return new RuntimeConformalUpdate(residual, key, values.Count);
    }

    public RuntimeConformalPrediction Predict(
        RuntimeConformalBucketKey key,
        double yHatMicroseconds,
        double budgetMicroseconds)
    {
        var decision = QuantileFor(key);
        var upper = yHatMicroseconds + Math.Max(decision.Quantile, 0);
        return new RuntimeConformalPrediction(
            upper,
            upper > budgetMicroseconds,
            1.0 - _config.Alpha,
            key,
            decision.SampleCount,
            decision.Quantile,
            decision.FallbackLevel,
            _config.WindowSize,
            ResetCount,
            yHatMicroseconds,
            budgetMicroseconds);
    }

    private QuantileDecision QuantileFor(RuntimeConformalBucketKey key)
    {
        var minSamples = Math.Max(_config.MinSamples, 1);

        var exact = CollectExact(key);
        if (exact.Count >= minSamples)
        {
            return QuantileDecision.Create(_config.Alpha, exact, 0);
        }

        var modeDiff = CollectModeDiff(key.Mode, key.Diff);
        if (modeDiff.Count >= minSamples)
        {
            return QuantileDecision.Create(_config.Alpha, modeDiff, 1);
        }

        var mode = CollectMode(key.Mode);
        if (mode.Count >= minSamples)
        {
            return QuantileDecision.Create(_config.Alpha, mode, 2);
        }

        var global = CollectAll();
        if (global.Count > 0)
        {
            return QuantileDecision.Create(_config.Alpha, global, 3);
        }

        return new QuantileDecision(_config.DefaultResidualMicroseconds, 0, 3);
    }

    private List<double> CollectExact(RuntimeConformalBucketKey key) =>
        _buckets.TryGetValue(key, out var values) ? values.ToList() : [];

    private List<double> CollectModeDiff(RuntimeModeBucket mode, RuntimeDiffBucket diff)
    {
        var values = new List<double>();
        foreach (var (key, bucket) in _buckets)
        {
            if (key.Mode == mode && key.Diff == diff)
            {
                values.AddRange(bucket);
            }
        }

        return values;
    }

    private List<double> CollectMode(RuntimeModeBucket mode)
    {
        var values = new List<double>();
        foreach (var (key, bucket) in _buckets)
        {
            if (key.Mode == mode)
            {
                values.AddRange(bucket);
            }
        }

        return values;
    }

    private List<double> CollectAll()
    {
        var values = new List<double>();
        foreach (var bucket in _buckets.Values)
        {
            values.AddRange(bucket);
        }

        return values;
    }

    private readonly record struct QuantileDecision(
        double Quantile,
        int SampleCount,
        byte FallbackLevel)
    {
        public static QuantileDecision Create(double alpha, List<double> residuals, byte fallbackLevel)
        {
            residuals.Sort();
            var rank = (int)Math.Ceiling((residuals.Count + 1.0) * (1.0 - alpha));
            var index = Math.Clamp(rank - 1, 0, residuals.Count - 1);
            return new QuantileDecision(residuals[index], residuals.Count, fallbackLevel);
        }
    }
}

public sealed record ConformalFrameGuardConfig(
    TimeSpan FallbackBudget,
    RuntimeConformalConfig? Conformal = null,
    int TimeSeriesWindow = 512,
    int NonconformityWindow = 256,
    double EmaDecay = 0.95)
{
    public static ConformalFrameGuardConfig Default { get; } = new(TimeSpan.FromMilliseconds(16));

    public RuntimeConformalConfig EffectiveConformal => Conformal ?? RuntimeConformalConfig.Default;
}

public sealed record DegradationCascadeConfig(
    ConformalFrameGuardConfig? Guard = null,
    int RecoveryThreshold = 10,
    RuntimeDegradationLevel MaxDegradation = RuntimeDegradationLevel.SkipFrame,
    RuntimeDegradationLevel MinTriggerLevel = RuntimeDegradationLevel.SimpleBorders,
    RuntimeDegradationLevel DegradationFloor = RuntimeDegradationLevel.SimpleBorders)
{
    public ConformalFrameGuardConfig EffectiveGuard => Guard ?? ConformalFrameGuardConfig.Default;

    public static DegradationCascadeConfig Default { get; } = new();
}

public enum RuntimeGuardState
{
    Warmup,
    Calibrated,
    AtRisk
}

public enum RuntimeCascadeDecision
{
    Hold,
    Degrade,
    Recover
}

public sealed record RuntimeP99Prediction(
    double YHatMicroseconds,
    double UpperMicroseconds,
    double BudgetMicroseconds,
    bool ExceedsBudget,
    int CalibrationSize,
    byte FallbackLevel,
    RuntimeGuardState State,
    double IntervalWidthMicroseconds)
{
    public string ToJsonl() =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schema"] = "conformal-frame-guard-v1",
            ["y_hat_us"] = YHatMicroseconds,
            ["upper_us"] = UpperMicroseconds,
            ["budget_us"] = BudgetMicroseconds,
            ["exceeds_budget"] = ExceedsBudget,
            ["calibration_size"] = CalibrationSize,
            ["fallback_level"] = FallbackLevel,
            ["state"] = StateLabel(State),
            ["interval_width_us"] = IntervalWidthMicroseconds
        });

    public static string StateLabel(RuntimeGuardState state) =>
        state switch
        {
            RuntimeGuardState.Calibrated => "calibrated",
            RuntimeGuardState.AtRisk => "at_risk",
            _ => "warmup"
        };
}

public sealed record RuntimeCascadeEvidence(
    ulong FrameIndex,
    RuntimeCascadeDecision Decision,
    RuntimeDegradationLevel LevelBefore,
    RuntimeDegradationLevel LevelAfter,
    RuntimeGuardState GuardState,
    int RecoveryStreak,
    int RecoveryThreshold,
    double FrameTimeMicroseconds,
    double BudgetMicroseconds,
    RuntimeP99Prediction Prediction)
{
    public string ToJsonl() =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schema"] = "degradation-cascade-v1",
            ["frame_idx"] = FrameIndex,
            ["decision"] = DecisionLabel(Decision),
            ["level_before"] = LevelBefore.Label(),
            ["level_after"] = LevelAfter.Label(),
            ["guard_state"] = RuntimeP99Prediction.StateLabel(GuardState),
            ["recovery_streak"] = RecoveryStreak,
            ["recovery_threshold"] = RecoveryThreshold,
            ["frame_time_us"] = FrameTimeMicroseconds,
            ["budget_us"] = BudgetMicroseconds,
            ["p99_upper_us"] = Prediction.UpperMicroseconds,
            ["p99_exceeds"] = Prediction.ExceedsBudget,
            ["p99_fallback_level"] = Prediction.FallbackLevel,
            ["p99_calibration_size"] = Prediction.CalibrationSize,
            ["p99_interval_width_us"] = Prediction.IntervalWidthMicroseconds
        });

    public static string DecisionLabel(RuntimeCascadeDecision decision) =>
        decision switch
        {
            RuntimeCascadeDecision.Degrade => "degrade",
            RuntimeCascadeDecision.Recover => "recover",
            _ => "hold"
        };
}

public sealed record RuntimeCascadeTelemetry(
    RuntimeDegradationLevel Level,
    int RecoveryStreak,
    int RecoveryThreshold,
    ulong FrameIndex,
    ulong TotalDegrades,
    ulong TotalRecoveries,
    RuntimeGuardState GuardState,
    ulong GuardObservations,
    double GuardEmaMicroseconds);

public sealed class RuntimeDegradationCascade
{
    private readonly DegradationCascadeConfig _config;
    private readonly RuntimeConformalPredictor _predictor;
    private readonly Queue<double> _frameTimes = new();
    private readonly Queue<double> _nonconformity = new();
    private RuntimeDegradationLevel _level = RuntimeDegradationLevel.Full;
    private RuntimeGuardState _guardState = RuntimeGuardState.Warmup;
    private double _emaMicroseconds;
    private int _recoveryStreak;

    public RuntimeDegradationCascade(DegradationCascadeConfig? config = null)
    {
        _config = config ?? DegradationCascadeConfig.Default;
        _predictor = new RuntimeConformalPredictor(_config.EffectiveGuard.EffectiveConformal);
    }

    public RuntimeDegradationLevel Level => _level;

    public int RecoveryStreak => _recoveryStreak;

    public ulong FrameIndex { get; private set; }

    public ulong TotalDegrades { get; private set; }

    public ulong TotalRecoveries { get; private set; }

    public ulong Observations { get; private set; }

    public RuntimeCascadeEvidence? LastEvidence { get; private set; }

    public void Observe(TimeSpan frameDuration) =>
        Observe(frameDuration, RuntimeConformalBucketKey.Default);

    public void Observe(TimeSpan frameDuration, RuntimeConformalBucketKey key)
    {
        var microseconds = frameDuration.TotalMilliseconds * 1000.0;
        if (!double.IsFinite(microseconds) || microseconds < 0)
        {
            return;
        }

        Observations++;
        var guard = _config.EffectiveGuard;
        if (Observations == 1)
        {
            _emaMicroseconds = microseconds;
        }
        else
        {
            var decay = Math.Clamp(guard.EmaDecay, 0, 1);
            _emaMicroseconds = decay * _emaMicroseconds + (1.0 - decay) * microseconds;
        }

        EnqueueBounded(_frameTimes, microseconds, Math.Max(guard.TimeSeriesWindow, 1));
        EnqueueBounded(_nonconformity, microseconds - _emaMicroseconds, Math.Max(guard.NonconformityWindow, 1));
        _predictor.Observe(key, _emaMicroseconds, microseconds);
        if (_predictor.BucketSamples(key) >= Math.Max(guard.EffectiveConformal.MinSamples, 1) &&
            _guardState == RuntimeGuardState.Warmup)
        {
            _guardState = RuntimeGuardState.Calibrated;
        }
    }

    public RuntimeP99Prediction Predict(TimeSpan budget) =>
        Predict(budget, RuntimeConformalBucketKey.Default);

    public RuntimeP99Prediction Predict(TimeSpan budget, RuntimeConformalBucketKey key)
    {
        var guard = _config.EffectiveGuard;
        var budgetUs = Math.Max(budget.TotalMilliseconds * 1000.0, 1.0);
        var bucketSamples = _predictor.BucketSamples(key);
        var calibrated = bucketSamples >= Math.Max(guard.EffectiveConformal.MinSamples, 1);

        if (calibrated)
        {
            var conformal = _predictor.Predict(key, _emaMicroseconds, budgetUs);
            var exceeds = conformal.Risk;
            _guardState = exceeds ? RuntimeGuardState.AtRisk : RuntimeGuardState.Calibrated;
            return new RuntimeP99Prediction(
                _emaMicroseconds,
                conformal.UpperMicroseconds,
                budgetUs,
                exceeds,
                conformal.SampleCount,
                conformal.FallbackLevel,
                _guardState,
                Math.Max(conformal.UpperMicroseconds - _emaMicroseconds, 0));
        }

        var fallbackUs = Math.Max(guard.FallbackBudget.TotalMilliseconds * 1000.0, 1.0);
        var fallbackExceeds = _emaMicroseconds > fallbackUs;
        _guardState = fallbackExceeds ? RuntimeGuardState.AtRisk : RuntimeGuardState.Warmup;
        return new RuntimeP99Prediction(
            _emaMicroseconds,
            _emaMicroseconds,
            fallbackUs,
            fallbackExceeds,
            bucketSamples,
            FallbackLevel: 4,
            _guardState,
            IntervalWidthMicroseconds: 0);
    }

    public RuntimeCascadeEvidence PreRender(TimeSpan budget) =>
        PreRender(budget, RuntimeConformalBucketKey.Default);

    public RuntimeCascadeEvidence PreRender(TimeSpan budget, RuntimeConformalBucketKey key)
    {
        FrameIndex++;
        var before = _level;
        var prediction = Predict(budget, key);
        var decision = RuntimeCascadeDecision.Hold;

        if (prediction.ExceedsBudget)
        {
            if (_level < _config.MaxDegradation && _level < _config.DegradationFloor)
            {
                _level = _level.Next();
                if (_level < _config.MinTriggerLevel)
                {
                    _level = _config.MinTriggerLevel;
                }

                if (_level > _config.DegradationFloor)
                {
                    _level = _config.DegradationFloor;
                }

                if (_level > _config.MaxDegradation)
                {
                    _level = _config.MaxDegradation;
                }

                _recoveryStreak = 0;
                TotalDegrades++;
                decision = RuntimeCascadeDecision.Degrade;
            }
            else
            {
                _recoveryStreak = 0;
            }
        }
        else
        {
            _recoveryStreak++;
            if (_recoveryStreak >= Math.Max(_config.RecoveryThreshold, 1) &&
                _level != RuntimeDegradationLevel.Full)
            {
                _level = _level.Previous();
                _recoveryStreak = 0;
                TotalRecoveries++;
                decision = RuntimeCascadeDecision.Recover;
            }
        }

        LastEvidence = new RuntimeCascadeEvidence(
            FrameIndex,
            decision,
            before,
            _level,
            _guardState,
            _recoveryStreak,
            _config.RecoveryThreshold,
            _emaMicroseconds,
            prediction.BudgetMicroseconds,
            prediction);
        return LastEvidence;
    }

    public bool ShouldRenderWidget(bool essential) =>
        _level < RuntimeDegradationLevel.EssentialOnly || essential;

    public RuntimeCascadeTelemetry Telemetry() =>
        new(
            _level,
            _recoveryStreak,
            _config.RecoveryThreshold,
            FrameIndex,
            TotalDegrades,
            TotalRecoveries,
            _guardState,
            Observations,
            _emaMicroseconds);

    public void Reset()
    {
        _frameTimes.Clear();
        _nonconformity.Clear();
        _predictor.ResetAll();
        _level = RuntimeDegradationLevel.Full;
        _guardState = RuntimeGuardState.Warmup;
        _emaMicroseconds = 0;
        _recoveryStreak = 0;
        FrameIndex = 0;
        LastEvidence = null;
    }

    private static void EnqueueBounded(Queue<double> queue, double value, int capacity)
    {
        queue.Enqueue(value);
        while (queue.Count > capacity)
        {
            queue.Dequeue();
        }
    }

    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"level={_level.Label()} state={RuntimeP99Prediction.StateLabel(_guardState)} observations={Observations}");
}
