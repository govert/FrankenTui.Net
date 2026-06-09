// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/voi_sampling.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Value-of-Information (VOI) Sampling Policy for expensive measurements.
// Beta-Bernoulli posterior with e-process control and deferred refinement scheduling.
//
// DIVERGENCE: Tracing capture tests skipped (tracing-subscriber not ported).
// DIVERGENCE: proptest → seeded RNG loops per CHARTER.

using System.Diagnostics;

namespace FrankenTui.Runtime;

// ── Constants ─────────────────────────────────────────────────────────────

static class VoiConstants
{
    public const double Eps = 1e-12;
    public const double Mu0Min = 1e-6;
    public const double Mu0Max = 1.0 - 1e-6;
    public const double LambdaEps = 1e-9;
    public const double EMin = 1e-12;
    public const double EMax = 1e12;
    public const double VarMax = 0.25;
}

// ── Global counters ───────────────────────────────────────────────────────

static class VoiCounters
{
    private static long _taken;
    private static long _skipped;

    public static ulong TakenTotal => (ulong)Interlocked.Read(ref _taken);
    public static ulong SkippedTotal => (ulong)Interlocked.Read(ref _skipped);
    public static void IncTaken() => Interlocked.Increment(ref _taken);
    public static void IncSkipped() => Interlocked.Increment(ref _skipped);
    public static ulong VoiSamplesTakenTotal() => TakenTotal;
    public static ulong VoiSamplesSkippedTotal() => SkippedTotal;
}

// ── Math helpers ──────────────────────────────────────────────────────────

static class VoiMath
{
    public static double BetaMean(double alpha, double beta) => alpha / (alpha + beta);

    public static double BetaVariance(double alpha, double beta)
    {
        var sum = alpha + beta;
        if (sum <= 0.0) return 0.0;
        var v = (alpha * beta) / (sum * sum * (sum + 1.0));
        return Math.Min(v, VoiConstants.VarMax);
    }

    public static double ExpectedVarianceAfter(double alpha, double beta)
    {
        var p = BetaMean(alpha, beta);
        return p * BetaVariance(alpha + 1.0, beta) + (1.0 - p) * BetaVariance(alpha, beta + 1.0);
    }

    public static double BoundaryScore(double eValue, double threshold)
    {
        var e = Math.Max(eValue, VoiConstants.Eps);
        var t = Math.Max(threshold, VoiConstants.Eps);
        var gap = Math.Abs(Math.Log(e) - Math.Log(t));
        return 1.0 / (1.0 + gap);
    }

    public static double Log10Ratio(double score, double cost)
    {
        var ratio = (score + VoiConstants.Eps) / (cost + VoiConstants.Eps);
        return Math.Log(ratio) / Math.Log(10);
    }
}

// ── VoiConfig ─────────────────────────────────────────────────────────────

public sealed class VoiConfig
{
    public double Alpha { get; set; } = 0.05;
    public double PriorAlpha { get; set; } = 1.0;
    public double PriorBeta { get; set; } = 1.0;
    public double Mu0 { get; set; } = 0.05;
    public double Lambda { get; set; } = 0.5;
    public double ValueScale { get; set; } = 1.0;
    public double BoundaryWeight { get; set; } = 1.0;
    public double SampleCost { get; set; } = 0.01;
    public ulong MinIntervalMs { get; set; }
    public ulong MaxIntervalMs { get; set; } = 250;
    public ulong MinIntervalEvents { get; set; }
    public ulong MaxIntervalEvents { get; set; } = 20;
    public bool EnableLogging { get; set; }
    public int MaxLogEntries { get; set; } = 2048;

    public static VoiConfig Default => new();
}

// ── VoiDecision / VoiObservation / VoiLogEntry / VoiSummary / VoiSamplerSnapshot ──

public sealed class VoiDecision
{
    public ulong EventIdx { get; set; }
    public bool ShouldSample { get; set; }
    public bool ForcedByInterval { get; set; }
    public bool BlockedByMinInterval { get; set; }
    public double VoiGain { get; set; }
    public double Score { get; set; }
    public double Cost { get; set; }
    public double LogBayesFactor { get; set; }
    public double PosteriorMean { get; set; }
    public double PosteriorVariance { get; set; }
    public double EValue { get; set; }
    public double EThreshold { get; set; }
    public double BoundaryScore { get; set; }
    public ulong EventsSinceSample { get; set; }
    public double TimeSinceSampleMs { get; set; }
    public string Reason { get; set; } = "";

    public string ToJsonl() =>
        $$"""{"event":"voi_decision","idx":{{EventIdx}},"should_sample":{{ShouldSample.ToString().ToLowerInvariant()}},"forced":{{ForcedByInterval.ToString().ToLowerInvariant()}},"blocked":{{BlockedByMinInterval.ToString().ToLowerInvariant()}},"voi_gain":{{VoiGain:F6}},"score":{{Score:F6}},"cost":{{Cost:F6}},"log_bayes_factor":{{LogBayesFactor:F4}},"posterior_mean":{{PosteriorMean:F6}},"posterior_variance":{{PosteriorVariance:F6}},"e_value":{{EValue:F6}},"e_threshold":{{EThreshold:F6}},"boundary_score":{{BoundaryScore:F6}},"events_since_sample":{{EventsSinceSample}},"time_since_sample_ms":{{TimeSinceSampleMs:F3}},"reason":"{{Reason}}"}""";
}

public sealed class VoiObservation
{
    public ulong EventIdx { get; set; }
    public ulong SampleIdx { get; set; }
    public bool Violated { get; set; }
    public double PosteriorMean { get; set; }
    public double PosteriorVariance { get; set; }
    public double Alpha { get; set; }
    public double Beta { get; set; }
    public double EValue { get; set; }
    public double EThreshold { get; set; }

    public string ToJsonl() =>
        $$"""{"event":"voi_observe","idx":{{EventIdx}},"sample_idx":{{SampleIdx}},"violated":{{Violated.ToString().ToLowerInvariant()}},"posterior_mean":{{PosteriorMean:F6}},"posterior_variance":{{PosteriorVariance:F6}},"alpha":{{Alpha:F3}},"beta":{{Beta:F3}},"e_value":{{EValue:F6}},"e_threshold":{{EThreshold:F6}}}""";
}

public abstract record VoiLogEntry
{
    public sealed record Decision(VoiDecision D) : VoiLogEntry;
    public sealed record Observation(VoiObservation O) : VoiLogEntry;

    public string ToJsonl() => this switch
    {
        Decision d => d.D.ToJsonl(),
        Observation o => o.O.ToJsonl(),
        _ => "",
    };
}

public sealed record VoiSummary(
    ulong TotalEvents, ulong TotalSamples, ulong ForcedSamples, ulong SkippedEvents,
    double CurrentMean, double CurrentVariance, double EValue, double EThreshold,
    double AvgEventsBetweenSamples, double AvgMsBetweenSamples);

public sealed record VoiSamplerSnapshot(
    ulong CapturedMs, double Alpha, double Beta, double PosteriorMean,
    double PosteriorVariance, double ExpectedVarianceAfter, double VoiGain,
    VoiDecision? LastDecision, VoiObservation? LastObservation, List<VoiLogEntry> RecentLogs);

// ── VoiSampler ────────────────────────────────────────────────────────────

public sealed class VoiSampler
{
    private VoiConfig _config;
    private double _alpha, _beta, _mu0, _lambda, _eValue, _eThreshold;
    private ulong _eventIdx, _sampleIdx, _forcedSamples, _lastSampleEvent;
    private Stopwatch _lastSampleTime, _startTime, _now;
    private bool _lastDecisionForced;
    private readonly LinkedList<VoiLogEntry> _logs = new();
    private VoiDecision? _lastDecision;
    private VoiObservation? _lastObservation;

    public VoiSampler(VoiConfig config) : this(config, Stopwatch.StartNew()) { }

    public VoiSampler(VoiConfig config, Stopwatch now)
    {
        var priorAlpha = double.IsNaN(config.PriorAlpha) ? VoiConstants.Eps : Math.Max(config.PriorAlpha, VoiConstants.Eps);
        var priorBeta = double.IsNaN(config.PriorBeta) ? VoiConstants.Eps : Math.Max(config.PriorBeta, VoiConstants.Eps);
        var mu0 = double.IsNaN(config.Mu0) ? 0.5 : Math.Clamp(config.Mu0, VoiConstants.Mu0Min, VoiConstants.Mu0Max);
        var lambdaMax = 1.0 / (1.0 - mu0) - VoiConstants.LambdaEps;
        var lambda = double.IsNaN(config.Lambda) ? VoiConstants.LambdaEps : Math.Clamp(config.Lambda, VoiConstants.LambdaEps, lambdaMax);
        var vs = double.IsNaN(config.ValueScale) ? VoiConstants.Eps : Math.Max(config.ValueScale, VoiConstants.Eps);
        var bw = double.IsNaN(config.BoundaryWeight) ? 0.0 : Math.Max(config.BoundaryWeight, 0.0);
        var sc = double.IsNaN(config.SampleCost) ? VoiConstants.Eps : Math.Max(config.SampleCost, VoiConstants.Eps);
        config.ValueScale = vs;
        config.BoundaryWeight = bw;
        config.SampleCost = sc;
        config.MaxLogEntries = Math.Max(1, config.MaxLogEntries);
        _config = config;
        _alpha = priorAlpha; _beta = priorBeta; _mu0 = mu0; _lambda = lambda;
        _eValue = 1.0; _eThreshold = 1.0 / Math.Max(config.Alpha, VoiConstants.Eps);
        _lastSampleTime = now; _startTime = now; _now = now;
    }

    public VoiConfig Config => _config;
    public (double, double) PosteriorParams => (_alpha, _beta);
    public double PosteriorMean => VoiMath.BetaMean(_alpha, _beta);
    public double PosteriorVariance => VoiMath.BetaVariance(_alpha, _beta);
    public double ExpectedVarianceAfter => VoiMath.ExpectedVarianceAfter(_alpha, _beta);
    public VoiDecision? LastDecision => _lastDecision;
    public VoiObservation? LastObservation => _lastObservation;
    public double EValue => _eValue;
    public double EThreshold => _eThreshold;

    public ulong ForcedSamples { get => _forcedSamples; set => _forcedSamples = value; }

    public VoiDecision Decide(Stopwatch now)
    {
        _now = now;
        _eventIdx++;
        var elapsed = now.Elapsed;
        var lastElapsed = _lastSampleTime.Elapsed;
        var eventsSinceSample = _sampleIdx == 0 ? _eventIdx : _eventIdx - _lastSampleEvent;

        var timeSinceSample = elapsed >= lastElapsed ? elapsed - lastElapsed : TimeSpan.Zero;

        var forcedByEvents = _config.MaxIntervalEvents > 0 && eventsSinceSample >= _config.MaxIntervalEvents;
        var forcedByTime = _config.MaxIntervalMs > 0 && timeSinceSample.TotalMilliseconds >= _config.MaxIntervalMs;
        var forced = forcedByEvents || forcedByTime;

        var blockedByEvents = _sampleIdx > 0 && _config.MinIntervalEvents > 0 && eventsSinceSample < _config.MinIntervalEvents;
        var blockedByTime = _sampleIdx > 0 && _config.MinIntervalMs > 0 && timeSinceSample.TotalMilliseconds < _config.MinIntervalMs;
        var blocked = blockedByEvents || blockedByTime;

        var variance = VoiMath.BetaVariance(_alpha, _beta);
        var expectedAfter = VoiMath.ExpectedVarianceAfter(_alpha, _beta);
        var voiGain = Math.Max(variance - expectedAfter, 0.0);
        var bScore = VoiMath.BoundaryScore(_eValue, _eThreshold);
        var score = voiGain * _config.ValueScale * (1.0 + _config.BoundaryWeight * bScore);
        var cost = _config.SampleCost;
        var logBf = VoiMath.Log10Ratio(score, cost);

        var shouldSample = forced || (!blocked && score >= cost);
        string reason = forced ? "forced_interval" : blocked ? "min_interval" : shouldSample ? "voi_ge_cost" : "voi_lt_cost";

        var decision = new VoiDecision
        {
            EventIdx = _eventIdx, ShouldSample = shouldSample,
            ForcedByInterval = forced, BlockedByMinInterval = blocked,
            VoiGain = voiGain, Score = score, Cost = cost,
            LogBayesFactor = logBf, PosteriorMean = VoiMath.BetaMean(_alpha, _beta),
            PosteriorVariance = variance, EValue = _eValue, EThreshold = _eThreshold,
            BoundaryScore = bScore, EventsSinceSample = eventsSinceSample,
            TimeSinceSampleMs = timeSinceSample.TotalMilliseconds, Reason = reason,
        };

        _lastDecision = decision;
        _lastDecisionForced = forced;

        if (shouldSample) VoiCounters.IncTaken(); else VoiCounters.IncSkipped();
        if (_config.EnableLogging) PushLog(new VoiLogEntry.Decision(decision));
        return decision;
    }

    public VoiDecision Decide() => Decide(_lastSampleTime);

    public VoiObservation ObserveAt(bool violated, Stopwatch now)
    {
        _sampleIdx++;
        _lastSampleEvent = _eventIdx;
        _lastSampleTime = now;
        if (_lastDecisionForced) _forcedSamples++;

        if (violated) _alpha += 1.0; else _beta += 1.0;
        UpdateEProcess(violated);

        var obs = new VoiObservation
        {
            EventIdx = _eventIdx, SampleIdx = _sampleIdx, Violated = violated,
            PosteriorMean = VoiMath.BetaMean(_alpha, _beta),
            PosteriorVariance = VoiMath.BetaVariance(_alpha, _beta),
            Alpha = _alpha, Beta = _beta, EValue = _eValue, EThreshold = _eThreshold,
        };
        _lastObservation = obs;
        if (_config.EnableLogging) PushLog(new VoiLogEntry.Observation(obs));
        return obs;
    }

    public VoiObservation Observe(bool violated) => ObserveAt(violated, Stopwatch.StartNew());

    public VoiSummary Summary()
    {
        var skipped = _eventIdx - _sampleIdx;
        var avgEvents = _sampleIdx > 0 ? (double)_eventIdx / _sampleIdx : 0.0;
        var elapsedMs = _startTime.Elapsed.TotalMilliseconds;
        var avgMs = _sampleIdx > 0 ? elapsedMs / _sampleIdx : 0.0;
        return new VoiSummary(_eventIdx, _sampleIdx, _forcedSamples, skipped,
            VoiMath.BetaMean(_alpha, _beta), VoiMath.BetaVariance(_alpha, _beta),
            _eValue, _eThreshold, avgEvents, avgMs);
    }

    public LinkedList<VoiLogEntry> Logs => _logs;
    public string LogsToJsonl() => string.Join("\n", _logs.Select(e => e.ToJsonl()));

    public VoiSamplerSnapshot Snapshot(int maxLogs, ulong capturedMs)
    {
        var expectedAfter = VoiMath.ExpectedVarianceAfter(_alpha, _beta);
        var variance = VoiMath.BetaVariance(_alpha, _beta);
        var voiGain = Math.Max(variance - expectedAfter, 0.0);
        var recent = _logs.Reverse().Take(Math.Max(1, maxLogs)).Reverse().ToList();
        return new VoiSamplerSnapshot(capturedMs, _alpha, _beta, VoiMath.BetaMean(_alpha, _beta),
            variance, expectedAfter, voiGain, _lastDecision, _lastObservation, recent);
    }

    private void PushLog(VoiLogEntry entry)
    {
        if (_logs.Count >= _config.MaxLogEntries) _logs.RemoveFirst();
        _logs.AddLast(entry);
    }

    private void UpdateEProcess(bool violated)
    {
        var x = violated ? 1.0 : 0.0;
        var factor = 1.0 + _lambda * (x - _mu0);
        var next = _eValue * Math.Max(factor, VoiConstants.Eps);
        _eValue = Math.Clamp(next, VoiConstants.EMin, VoiConstants.EMax);
    }
}

// ── DeferredRefinementScheduler ───────────────────────────────────────────

public sealed record DeferredRefinementConfig(
    ulong MinSpareBudgetUs = 500, int MaxRefinementsPerFrame = 2,
    double VoiGainCutoff = 0.01, double FairnessBoostPerSkip = 0.02, double FairnessBoostCap = 1.0);

public readonly record struct RefinementCandidate(ulong RegionId, ulong EstimatedCostUs, double VoiGain);

public readonly record struct RefinementSelection(
    ulong RegionId, ulong EstimatedCostUs, double VoiGain,
    double FairnessBoost, double EffectiveVoi, double Score);

public sealed record DeferredRefinementPlan(
    ulong FrameBudgetUs, ulong MandatoryWorkUs, ulong ReservedSpareUs,
    ulong OptionalBudgetUs, ulong SpentOptionalUs, List<RefinementSelection> Selected)
{
    public bool HardBudgetRespected =>
        MandatoryWorkUs + ReservedSpareUs + SpentOptionalUs <= FrameBudgetUs;
}

public sealed class DeferredRefinementScheduler
{
    private readonly DeferredRefinementConfig _config;
    private readonly SortedDictionary<ulong, uint> _skipped = new();

    public DeferredRefinementScheduler(DeferredRefinementConfig config) { _config = config; }
    public DeferredRefinementConfig Config => _config;
    public uint SkippedFramesFor(ulong regionId) => _skipped.GetValueOrDefault(regionId);

    public DeferredRefinementPlan PlanFrame(ulong frameBudgetUs, ulong mandatoryWorkUs, ReadOnlySpan<RefinementCandidate> candidates)
    {
        var reserved = _config.MinSpareBudgetUs;
        var available = frameBudgetUs - mandatoryWorkUs;
        var optionalBudget = available > reserved ? available - reserved : 0UL;

        var scored = new (RefinementCandidate c, double fb, double ev, double s, ulong rid)[candidates.Length];
        for (int i = 0; i < candidates.Length; i++)
        {
            var c = candidates[i];
            var skipCount = SkippedFramesFor(c.RegionId);
            var fb = Math.Min(skipCount * _config.FairnessBoostPerSkip, _config.FairnessBoostCap);
            var vg = double.IsFinite(c.VoiGain) ? Math.Max(c.VoiGain, 0.0) : 0.0;
            var ev = vg + fb;
            var cost = Math.Max(c.EstimatedCostUs, 1UL);
            var s = ev / cost;
            scored[i] = (c, fb, ev, s, c.RegionId);
        }

        Array.Sort(scored, (a, b) =>
        {
            int cmp = b.s.CompareTo(a.s);
            if (cmp != 0) return cmp;
            cmp = b.ev.CompareTo(a.ev);
            if (cmp != 0) return cmp;
            return a.rid.CompareTo(b.rid);
        });

        var remaining = optionalBudget;
        var selected = new List<RefinementSelection>();
        var selectedIds = new HashSet<ulong>();

        foreach (var (c, fb, ev, s, _) in scored)
        {
            if (selected.Count >= _config.MaxRefinementsPerFrame) break;
            if (ev < _config.VoiGainCutoff) continue;
            if (c.EstimatedCostUs > remaining) continue;
            selected.Add(new RefinementSelection(c.RegionId, c.EstimatedCostUs,
                double.IsFinite(c.VoiGain) ? Math.Max(c.VoiGain, 0.0) : 0.0, fb, ev, s));
            selectedIds.Add(c.RegionId);
            remaining -= c.EstimatedCostUs;
        }

        foreach (var c in candidates)
        {
            if (selectedIds.Contains(c.RegionId))
                _skipped[c.RegionId] = 0;
            else
                _skipped[c.RegionId] = SkippedFramesFor(c.RegionId) + 1;
        }

        var spent = optionalBudget - remaining;
        return new DeferredRefinementPlan(frameBudgetUs, mandatoryWorkUs, reserved, optionalBudget, spent, selected);
    }
}
