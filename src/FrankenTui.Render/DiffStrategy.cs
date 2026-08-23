using System.Globalization;
using System.Text;

namespace FrankenTui.Render;

// Source basis: frankentui crates/ftui-render/src/diff_strategy.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// SignificantDirtyRows and the DiffRegime/ledger API are managed extensions
// retained for compatibility with the existing runtime integration.
public enum DiffStrategy
{
    Full,
    DirtyRows,
    FullRedraw,
    SignificantDirtyRows
}

public enum DiffRegime
{
    StableFrame,
    BurstyChange,
    ResizeRegime,
    DegradedTerminal
}

public sealed record DiffStrategySelection(
    int FrameIndex,
    DiffRegime Regime,
    DiffStrategy Strategy,
    double Confidence,
    int DirtyRows,
    int TotalCells,
    DiffRegime? TransitionFrom,
    string? TransitionReason);

public sealed record DiffTransitionRecord(DiffRegime From, DiffRegime To, int FrameIndex);

public sealed record DiffDecisionRecord(DiffStrategy Strategy, DiffRegime Regime, int FrameIndex);

public sealed class DiffEvidenceLedger
{
    public List<StrategyEvidence> Entries = new();
    public List<DiffTransitionRecord> Transitions = new();
    public List<DiffDecisionRecord> Decisions = new();

    public void Record(StrategyEvidence e) => Entries.Add(e);

    public void RecordDecision(DiffDecisionRecord d) => Decisions.Add(d);

    public void RecordTransition(DiffTransitionRecord t) => Transitions.Add(t);

    public DiffEvidenceLedger Clone()
    {
        var clone = new DiffEvidenceLedger();
        clone.Entries.AddRange(Entries.Select(entry => entry.Clone()));
        clone.Transitions.AddRange(Transitions);
        clone.Decisions.AddRange(Decisions);
        return clone;
    }
}

public sealed class DiffStrategyConfig
{
    public double CScan = 1.0;
    public double CEmit = 6.0;
    public double CRow = 0.1;
    public double PriorAlpha = 1.0;
    public double PriorBeta = 19.0;
    public double Decay = 0.95;
    public double HysteresisRatio = 0.05;
    public double UncertaintyGuardVariance = 0.002;
    public double ConservativeQuantile = 0.95;
    public bool Conservative;
    public int MinObservationCells = 1;

    public static DiffStrategyConfig Default => new();

    public DiffStrategyConfig Sanitized() => new()
    {
        CScan = NormalizeCost(CScan, 1.0),
        CEmit = NormalizeCost(CEmit, 6.0),
        CRow = NormalizeCost(CRow, 0.1),
        PriorAlpha = NormalizePositive(PriorAlpha, 1.0),
        PriorBeta = NormalizePositive(PriorBeta, 19.0),
        Decay = NormalizeDecay(Decay),
        Conservative = Conservative,
        ConservativeQuantile = double.IsNaN(ConservativeQuantile)
            ? 1e-6
            : Math.Clamp(ConservativeQuantile, 1e-6, 1.0 - 1e-6),
        // Rust uses usize. Clamp the target-only signed representation at its
        // source-domain boundary instead of allowing a negative threshold.
        MinObservationCells = Math.Max(0, MinObservationCells),
        HysteresisRatio = NormalizeRatio(HysteresisRatio, 0.05),
        UncertaintyGuardVariance = NormalizeCost(UncertaintyGuardVariance, 0.002)
    };

    public DiffStrategyConfig Clone() => new()
    {
        CScan = CScan,
        CEmit = CEmit,
        CRow = CRow,
        PriorAlpha = PriorAlpha,
        PriorBeta = PriorBeta,
        Decay = Decay,
        Conservative = Conservative,
        ConservativeQuantile = ConservativeQuantile,
        MinObservationCells = MinObservationCells,
        HysteresisRatio = HysteresisRatio,
        UncertaintyGuardVariance = UncertaintyGuardVariance
    };

    public override string ToString() => FormattableString.Invariant(
        $"DiffStrategyConfig {{ c_scan: {CScan}, c_emit: {CEmit}, c_row: {CRow}, prior_alpha: {PriorAlpha}, prior_beta: {PriorBeta}, decay: {Decay}, conservative: {Conservative.ToString().ToLowerInvariant()}, conservative_quantile: {ConservativeQuantile}, min_observation_cells: {MinObservationCells}, hysteresis_ratio: {HysteresisRatio}, uncertainty_guard_variance: {UncertaintyGuardVariance} }}");

    private static double NormalizePositive(double value, double fallback) =>
        double.IsFinite(value) && value > 0.0 ? value : fallback;

    private static double NormalizeCost(double value, double fallback) =>
        double.IsFinite(value) && value >= 0.0 ? value : fallback;

    private static double NormalizeDecay(double value) =>
        double.IsFinite(value) && value > 0.0 ? Math.Min(value, 1.0) : 1.0;

    private static double NormalizeRatio(double value, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : fallback;
}

public sealed class ChangeRateEstimator
{
    private readonly double _pa;
    private readonly double _pb;
    private readonly double _decay;
    private readonly int _minObs;
    private double _a;
    private double _b;

    public ChangeRateEstimator(double pa, double pb, double decay, int minObs)
    {
        _pa = pa;
        _pb = pb;
        _decay = decay;
        _minObs = Math.Max(0, minObs);
        _a = pa;
        _b = pb;
    }

    private ChangeRateEstimator(
        double pa,
        double pb,
        double decay,
        int minObs,
        double alpha,
        double beta)
    {
        _pa = pa;
        _pb = pb;
        _decay = decay;
        _minObs = minObs;
        _a = alpha;
        _b = beta;
    }

    public void Reset()
    {
        _a = _pa;
        _b = _pb;
    }

    public (double, double) PosteriorParams => (_a, _b);

    public double Mean => _a / (_a + _b);

    public double Variance
    {
        get
        {
            var sum = _a + _b;
            return (_a * _b) / (sum * sum * (sum + 1.0));
        }
    }

    public void Observe(int cellsScanned, int cellsChanged)
    {
        // Rust accepts usize. Negative values are outside the source domain;
        // normalize the managed signed inputs before applying source behavior.
        cellsScanned = Math.Max(0, cellsScanned);
        cellsChanged = Math.Max(0, cellsChanged);

        if (cellsScanned < _minObs)
        {
            return;
        }

        cellsChanged = Math.Min(cellsChanged, cellsScanned);
        _a *= _decay;
        _b *= _decay;
        _a += cellsChanged;
        _b += cellsScanned - cellsChanged;
        _a = Math.Clamp(_a, 1e-6, 1e6);
        _b = Math.Clamp(_b, 1e-6, 1e6);
    }

    public double UpperQuantile(double q)
    {
        q = Math.Clamp(q, 1e-6, 1.0 - 1e-6);
        var mean = Mean;
        var standardDeviation = Math.Sqrt(Variance);

        double z;
        if (q >= 0.5)
        {
            var t = Math.Sqrt(-2.0 * Math.Log(1.0 - q));
            z = t - (2.515517 + 0.802853 * t + 0.010328 * t * t)
                / (1.0 + 1.432788 * t + 0.189269 * t * t + 0.001308 * t * t * t);
        }
        else
        {
            var t = Math.Sqrt(-2.0 * Math.Log(q));
            z = -(t - (2.515517 + 0.802853 * t + 0.010328 * t * t)
                / (1.0 + 1.432788 * t + 0.189269 * t * t + 0.001308 * t * t * t));
        }

        return Math.Clamp(mean + z * standardDeviation, 0.0, 1.0);
    }

    public ChangeRateEstimator Clone() =>
        new(_pa, _pb, _decay, _minObs, _a, _b);

    public override string ToString() => FormattableString.Invariant(
        $"ChangeRateEstimator {{ alpha: {_a}, beta: {_b}, decay: {_decay}, min_observation_cells: {_minObs} }}");
}

public sealed class StrategyEvidence
{
    public DiffStrategy Strategy;
    public double CostFull;
    public double CostDirty;
    public double CostRedraw;
    public double PosteriorMean;
    public double PosteriorVariance;
    public double Alpha;
    public double Beta;
    public double HysteresisRatio;
    public int DirtyRows;
    public int TotalRows;
    public int TotalCells;
    public string GuardReason = "none";
    public bool HysteresisApplied;

    public string ToJsonl()
    {
        var guard = EscapeJson(GuardReason ?? string.Empty);
        var hysteresis = HysteresisApplied ? "true" : "false";
        return FormattableString.Invariant(
            $"{{\"schema\":\"diff-strategy-v1\",\"strategy\":\"{Strategy}\",\"cost_full\":{CostFull:F2},\"cost_dirty\":{CostDirty:F2},\"cost_redraw\":{CostRedraw:F2},\"posterior_mean\":{PosteriorMean:F6},\"posterior_var\":{PosteriorVariance:F8},\"alpha\":{Alpha:F4},\"beta\":{Beta:F4},\"dirty_rows\":{DirtyRows},\"total_rows\":{TotalRows},\"total_cells\":{TotalCells},\"guard\":\"{guard}\",\"hysteresis\":{hysteresis},\"hysteresis_ratio\":{HysteresisRatio:F4}}}");
    }

    public StrategyEvidence Clone() => new()
    {
        Strategy = Strategy,
        CostFull = CostFull,
        CostDirty = CostDirty,
        CostRedraw = CostRedraw,
        PosteriorMean = PosteriorMean,
        PosteriorVariance = PosteriorVariance,
        Alpha = Alpha,
        Beta = Beta,
        DirtyRows = DirtyRows,
        TotalRows = TotalRows,
        TotalCells = TotalCells,
        GuardReason = GuardReason,
        HysteresisApplied = HysteresisApplied,
        HysteresisRatio = HysteresisRatio
    };

    public override string ToString() => string.Format(
        CultureInfo.InvariantCulture,
        "Strategy: {0}\nCosts: Full={1:F2}, Dirty={2:F2}, Redraw={3:F2}\nPosterior: p~Beta({4:F2},{5:F2}), E[p]={6:F4}, Var[p]={7:F6}\nDirty: {8}/{9} rows, {10} total cells\nGuard: {11}, Hysteresis: {12} (ratio {13:F3})\n",
        Strategy,
        CostFull,
        CostDirty,
        CostRedraw,
        Alpha,
        Beta,
        PosteriorMean,
        PosteriorVariance,
        DirtyRows,
        TotalRows,
        TotalCells,
        GuardReason,
        HysteresisApplied ? "true" : "false",
        HysteresisRatio);

    private static string EscapeJson(string value)
    {
        StringBuilder? builder = null;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            string? escape = character switch
            {
                '\"' => "\\\"",
                '\\' => "\\\\",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when character < ' ' => $"\\u{(int)character:x4}",
                _ => null
            };

            if (escape is null)
            {
                builder?.Append(character);
                continue;
            }

            builder ??= new StringBuilder(value.Length + 8).Append(value, 0, index);
            builder.Append(escape);
        }

        return builder?.ToString() ?? value;
    }
}

public sealed class DiffStrategySelector
{
    private DiffStrategyConfig _c;
    private ChangeRateEstimator _e;
    private ulong _fc;
    private StrategyEvidence? _last;

    public DiffStrategySelector(DiffStrategyConfig? c = null)
    {
        _c = (c ?? DiffStrategyConfig.Default).Sanitized();
        _e = new ChangeRateEstimator(
            _c.PriorAlpha,
            _c.PriorBeta,
            _c.Decay,
            _c.MinObservationCells);
    }

    public static DiffStrategySelector WithDefaults() => new(DiffStrategyConfig.Default);

    public DiffStrategyConfig Config => _c;

    public (double, double) PosteriorParams => _e.PosteriorParams;

    public double PosteriorMean => _e.Mean;

    public double PosteriorVariance => _e.Variance;

    public StrategyEvidence? LastEvidence => _last;

    public DiffEvidenceLedger Ledger { get; } = new();

    public ulong FrameCount => _fc;

    public void OverrideLastStrategy(DiffStrategy s, string reason)
    {
        if (_last is null)
        {
            return;
        }

        _last.Strategy = s;
        _last.GuardReason = reason ?? string.Empty;
        _last.HysteresisApplied = false;
    }

    public void Observe(int scanned, int changed) => _e.Observe(scanned, changed);

    public void Observe(DiffStrategySelection sel, int changedCells, TimeSpan writeLatency)
    {
        _ = writeLatency;
        _e.Observe(sel.TotalCells, changedCells);
    }

    public void Reset()
    {
        _e.Reset();
        _fc = 0;
        _last = null;
    }

    // Managed runtime wrapper retained from the pre-existing API. It maps
    // runtime regimes to concrete render paths and records decisions, while
    // the source-shaped overloads below perform Bayesian strategy selection.
    public DiffStrategySelection Select(
        int width,
        int height,
        int dirtyRows,
        bool resized,
        TimeSpan lastWriteLatency)
    {
        var safeWidth = Math.Max(0, width);
        var safeHeight = Math.Max(0, height);
        var safeDirtyRows = Math.Max(0, dirtyRows);

        DiffRegime regime;
        DiffStrategy strategy;
        if (resized)
        {
            regime = DiffRegime.ResizeRegime;
            strategy = DiffStrategy.FullRedraw;
        }
        else if (lastWriteLatency >= DegradedLatencyThreshold)
        {
            regime = DiffRegime.DegradedTerminal;
            strategy = DiffStrategy.SignificantDirtyRows;
        }
        else if (safeHeight > 0 && (long)safeDirtyRows * 2 >= safeHeight)
        {
            regime = DiffRegime.BurstyChange;
            strategy = DiffStrategy.Full;
        }
        else
        {
            regime = DiffRegime.StableFrame;
            strategy = DiffStrategy.DirtyRows;
        }

        var frameIndex = SaturateToInt(_fc);
        var previousRegime = Ledger.Decisions.Count > 0
            ? (DiffRegime?)Ledger.Decisions[^1].Regime
            : null;
        var selection = new DiffStrategySelection(
            frameIndex,
            regime,
            strategy,
            1.0,
            safeDirtyRows,
            SaturateToInt((long)safeWidth * safeHeight),
            null,
            null);

        Ledger.RecordDecision(new DiffDecisionRecord(strategy, regime, frameIndex));
        if (!previousRegime.HasValue || previousRegime.Value != regime)
        {
            Ledger.RecordTransition(new DiffTransitionRecord(
                previousRegime ?? DiffRegime.StableFrame,
                regime,
                frameIndex));
        }

        return selection;
    }

    internal static readonly TimeSpan DegradedLatencyThreshold = TimeSpan.FromMilliseconds(10);

    public DiffStrategy Select(int width, int height, int dirtyRows)
    {
        var safeWidth = Math.Max(0, width);
        var defaultScanCells = (long)Math.Max(0, dirtyRows) * safeWidth;
        return SelectCore(width, height, dirtyRows, defaultScanCells);
    }

    public DiffStrategy SelectWithScan(int width, int height, int dirtyRows, int dirtyScanCells) =>
        SelectCore(width, height, dirtyRows, Math.Max(0, dirtyScanCells));

    // Source-name projection; SelectWithScan is retained as the established
    // managed shorthand.
    public DiffStrategy SelectWithScanEstimate(
        int width,
        int height,
        int dirtyRows,
        int dirtyScanCells) => SelectWithScan(width, height, dirtyRows, dirtyScanCells);

    public DiffStrategySelector Clone()
    {
        var clone = new DiffStrategySelector(_c.Clone())
        {
            _e = _e.Clone(),
            _fc = _fc,
            _last = _last?.Clone()
        };

        clone.Ledger.Entries.AddRange(Ledger.Entries.Select(entry => entry.Clone()));
        clone.Ledger.Transitions.AddRange(Ledger.Transitions);
        clone.Ledger.Decisions.AddRange(Ledger.Decisions);
        return clone;
    }

    public override string ToString()
    {
        var (alpha, beta) = PosteriorParams;
        return FormattableString.Invariant(
            $"DiffStrategySelector {{ frame_count: {_fc}, alpha: {alpha}, beta: {beta}, last_evidence: {_last?.Strategy.ToString() ?? "None"} }}");
    }

    private DiffStrategy SelectCore(int width, int height, int dirtyRows, long dirtyScanCells)
    {
        _fc++;

        var safeWidth = Math.Max(0, width);
        var safeHeight = Math.Max(0, height);
        var safeDirtyRows = Math.Max(0, dirtyRows);
        var totalCells = (long)safeWidth * safeHeight;
        var scanCells = Math.Min(Math.Max(0L, dirtyScanCells), totalCells);
        var w = (double)safeWidth;
        var h = (double)safeHeight;
        var d = (double)safeDirtyRows;
        var n = w * h;

        var uncertaintyGuard = _c.UncertaintyGuardVariance > 0.0
            && PosteriorVariance > _c.UncertaintyGuardVariance;
        var guardReason = safeDirtyRows == 0 ? "zero_dirty_rows" : "none";
        var p = _c.Conservative || uncertaintyGuard
            ? _e.UpperQuantile(_c.ConservativeQuantile)
            : PosteriorMean;
        if (safeDirtyRows == 0)
        {
            p = 0.0;
        }

        // p is learned per scanned cell, so every expected-emission term is
        // priced against the cells scanned by that path (upstream 15cc6543).
        var dirtyRegionCells = Math.Min(d * w, n);
        var costFull = _c.CRow * h
            + _c.CScan * d * w
            + _c.CEmit * p * dirtyRegionCells;
        var costDirty = _c.CScan * scanCells + _c.CEmit * p * scanCells;
        var costRedraw = _c.CEmit * n;

        var strategy = costDirty <= costFull && costDirty <= costRedraw
            ? DiffStrategy.DirtyRows
            : costFull <= costRedraw
                ? DiffStrategy.Full
                : DiffStrategy.FullRedraw;

        if (uncertaintyGuard)
        {
            if (guardReason == "none")
            {
                guardReason = "uncertainty_variance";
            }

            if (strategy == DiffStrategy.FullRedraw)
            {
                strategy = costDirty <= costFull ? DiffStrategy.DirtyRows : DiffStrategy.Full;
            }
        }

        var hysteresisApplied = false;
        if (_last is { } previous && previous.Strategy != strategy)
        {
            var previousCost = CostFor(previous.Strategy, costFull, costDirty, costRedraw);
            var newCost = CostFor(strategy, costFull, costDirty, costRedraw);
            var ratio = _c.HysteresisRatio;
            if (ratio > 0.0
                && double.IsFinite(previousCost)
                && previousCost > 0.0
                && newCost >= previousCost * (1.0 - ratio)
                && !(uncertaintyGuard && previous.Strategy == DiffStrategy.FullRedraw))
            {
                strategy = previous.Strategy;
                hysteresisApplied = true;
            }
        }

        var (alpha, beta) = _e.PosteriorParams;
        _last = new StrategyEvidence
        {
            Strategy = strategy,
            CostFull = costFull,
            CostDirty = costDirty,
            CostRedraw = costRedraw,
            PosteriorMean = PosteriorMean,
            PosteriorVariance = PosteriorVariance,
            Alpha = alpha,
            Beta = beta,
            DirtyRows = safeDirtyRows,
            TotalRows = safeHeight,
            // Existing managed evidence uses int; preserve that surface and
            // saturate only the representation, not the cost calculation.
            TotalCells = SaturateToInt(totalCells),
            GuardReason = guardReason,
            HysteresisApplied = hysteresisApplied,
            HysteresisRatio = _c.HysteresisRatio
        };

        return strategy;
    }

    private static double CostFor(
        DiffStrategy strategy,
        double costFull,
        double costDirty,
        double costRedraw) => strategy switch
        {
            DiffStrategy.Full => costFull,
            DiffStrategy.DirtyRows => costDirty,
            DiffStrategy.FullRedraw => costRedraw,
            _ => costFull
        };

    private static int SaturateToInt(long value) =>
        value >= int.MaxValue ? int.MaxValue : value <= 0 ? 0 : (int)value;

    private static int SaturateToInt(ulong value) =>
        value >= int.MaxValue ? int.MaxValue : (int)value;
}
