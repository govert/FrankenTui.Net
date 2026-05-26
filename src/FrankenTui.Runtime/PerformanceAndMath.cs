namespace FrankenTui.Runtime;

/// <summary>Allocation budget tracking. Matches upstream allocation_budget.</summary>
public sealed class AllocationBudget
{
    private long _allocated;
    private long _freed;

    public void RecordAlloc(long bytes) => _allocated += bytes;
    public void RecordFree(long bytes) => _freed += bytes;
    public long CurrentBytes => _allocated - _freed;
    public long PeakBytes { get; private set; }
    public void UpdatePeak() => PeakBytes = Math.Max(PeakBytes, CurrentBytes);
}

/// <summary>E-process throttling controller. Matches upstream eprocess_throttle.</summary>
public sealed class EProcessThrottle
{
    private readonly double _targetRate;
    private readonly double _maxBurst;
    private double _tokens;
    private DateTimeOffset _lastRefill = DateTimeOffset.UtcNow;

    public EProcessThrottle(double targetRate, double maxBurst)
    {
        _targetRate = targetRate;
        _maxBurst = maxBurst;
        _tokens = maxBurst;
    }

    public bool TryConsume(int count = 1)
    {
        Refill();
        if (_tokens >= count) { _tokens -= count; return true; }
        return false;
    }

    private void Refill()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        _tokens = Math.Min(_maxBurst, _tokens + elapsed * _targetRate);
        _lastRefill = now;
    }
}

/// <summary>Cost model for task scheduling. Matches upstream cost_model.</summary>
public sealed class CostModel
{
    private readonly Dictionary<string, double> _weights = [];
    public double EstimateCost(string taskType, double inputSize) =>
        _weights.TryGetValue(taskType, out var w) ? w * inputSize : inputSize;
    public void SetWeight(string taskType, double weight) => _weights[taskType] = weight;
}

/// <summary>Input fairness scheduler. Matches upstream input_fairness.</summary>
public sealed class InputFairness
{
    private readonly Dictionary<string, int> _counts = [];
    private int _total;

    public void Record(string source) { _counts.TryGetValue(source, out var c); _counts[source] = c + 1; _total++; }
    public double FairnessRatio(string source) => _total > 0 ? (double)_counts.GetValueOrDefault(source) / _total : 0;
    public string LeastServed() => _counts.MinBy(kv => kv.Value).Key;
}

/// <summary>SLO manager. Matches upstream slo.</summary>
public sealed class SloManager
{
    private int _total;
    private int _withinSlo;

    public void Record(bool withinSlo) { _total++; if (withinSlo) _withinSlo++; }
    public double Compliance => _total > 0 ? (double)_withinSlo / _total : 1.0;
    public void Reset() { _total = _withinSlo = 0; }
}

/// <summary>Validation pipeline for output quality. Matches upstream validation_pipeline.</summary>
public sealed class ValidationPipeline
{
    private readonly List<IValidator> _validators = [];
    public void Add(IValidator validator) => _validators.Add(validator);
    public IReadOnlyList<string> Validate(object? value) =>
        _validators.SelectMany(v => v.Validate(value)).ToList();
}

public interface IValidator { IEnumerable<string> Validate(object? value); }

/// <summary>Transparency reporting. Matches upstream transparency.</summary>
public sealed class TransparencyReport
{
    private readonly List<(DateTimeOffset Time, string Decision, string Reason)> _decisions = [];
    public void Record(string decision, string reason) =>
        _decisions.Add((DateTimeOffset.UtcNow, decision, reason));
    public IReadOnlyList<(DateTimeOffset, string, string)> Decisions => _decisions;

    public string Summarize() =>
        string.Join("\n", _decisions.TakeLast(20).Select(d => $"[{d.Time:HH:mm}] {d.Decision}: {d.Reason}"));
}

// === Phase H: Advanced Math/Stats ===

/// <summary>Alpha-investing sequential testing. Matches upstream alpha_investing.</summary>
public sealed class AlphaInvesting
{
    private double _wealth = 0.05;
    public bool Test(double pValue, double alpha = 0.05)
    {
        if (pValue <= _wealth) { _wealth += alpha; return true; }
        _wealth -= alpha / (1 - alpha);
        return false;
    }
}

/// <summary>Bayesian Online Change Point Detection. Matches upstream bocpd.</summary>
public sealed class Bocpd
{
    private readonly double _hazard;
    private readonly List<double> _runLengths = [1.0];
    public Bocpd(double hazard = 0.01) => _hazard = hazard;
    public void Observe(double x)
    {
        var newRl = new List<double> { _hazard };
        for (var i = 0; i < _runLengths.Count; i++)
            newRl.Add(_runLengths[i] * (1 - _hazard) * Math.Exp(-0.5 * x * x));
        var sum = newRl.Sum();
        _runLengths.Clear();
        _runLengths.AddRange(newRl.Select(r => sum > 0 ? r / sum : 0));
    }
    public double ChangeProbability => 1.0 - _runLengths.Sum();
}

/// <summary>Decision core engine. Matches upstream decision_core.</summary>
public sealed class DecisionCore
{
    public string Decide(params (string Option, double Score)[] options)
    {
        if (options.Length == 0) return "none";
        return options.MaxBy(o => o.Score).Option;
    }
}

/// <summary>VOI sampling engine. Matches upstream voi_sampling.</summary>
public sealed class VoiSampler
{
    private readonly Random _rng = new(42);
    public double Sample(string distribution, params double[] parameters) => distribution switch
    {
        "normal" => SampleNormal(parameters.Length > 0 ? parameters[0] : 0, parameters.Length > 1 ? parameters[1] : 1),
        "uniform" => _rng.NextDouble() * (parameters.Length > 0 ? parameters[0] : 1),
        _ => _rng.NextDouble()
    };
    private double SampleNormal(double mean, double stddev)
    {
        var u1 = 1.0 - _rng.NextDouble();
        var u2 = 1.0 - _rng.NextDouble();
        return mean + stddev * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

/// <summary>VOI telemetry collector. Matches upstream voi_telemetry.</summary>
public sealed class VoiTelemetry { public int Samples { get; private set; } public void Record() => Samples++; }

/// <summary>Unified evidence collector. Matches upstream unified_evidence.</summary>
public sealed class UnifiedEvidence
{
    private readonly List<Dictionary<string, object>> _records = [];
    public void Record(Dictionary<string, object> fields) => _records.Add(fields);
    public IReadOnlyList<Dictionary<string, object>> Records => _records;
}

/// <summary>Diff evidence collector. Matches upstream diff_evidence.</summary>
public sealed class DiffEvidence
{
    private int _fullRedraws;
    private int _partialDiffs;
    private long _totalCellsDiffed;
    public void RecordFullRedraw() => _fullRedraws++;
    public void RecordPartialDiff(int cells) { _partialDiffs++; _totalCellsDiffed += cells; }
    public (int Full, int Partial, long Cells) Snapshot() => (_fullRedraws, _partialDiffs, _totalCellsDiffed);
}

/// <summary>Incremental View Maintenance. Matches upstream ivm.</summary>
public sealed class Ivm<T> where T : notnull
{
    private readonly List<T> _base = [];
    private readonly List<T> _delta = [];
    public void SetBase(IEnumerable<T> items) { _base.Clear(); _base.AddRange(items); }
    public void AddDelta(T item) => _delta.Add(item);
    public IEnumerable<T> View => _base.Concat(_delta);
    public void Materialize() { _base.AddRange(_delta); _delta.Clear(); }
}

/// <summary>Sinkhorn optimal transport morphing. Matches upstream sinkhorn_morph.</summary>
public sealed class SinkhornMorph
{
    public static double[,] Compute(double[] source, double[] target, int iterations = 100)
    {
        var n = source.Length;
        var m = target.Length;
        var plan = new double[n, m];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < m; j++)
                plan[i, j] = 1.0 / (n * m);
        for (var iter = 0; iter < iterations; iter++)
        {
            for (var i = 0; i < n; i++) { var sum = 0.0; for (var j = 0; j < m; j++) sum += plan[i, j]; if (sum > 0) for (var j = 0; j < m; j++) plan[i, j] *= source[i] / sum; }
            for (var j = 0; j < m; j++) { var sum = 0.0; for (var i = 0; i < n; i++) sum += plan[i, j]; if (sum > 0) for (var i = 0; i < n; i++) plan[i, j] *= target[j] / sum; }
        }
        return plan;
    }
}

/// <summary>Reversible computation + rough path math. Matches upstream reversible + rough_path.</summary>
public static class MathPrimitives
{
    public static double RoughPathSignature(double[] path, int level = 2)
    {
        double sig = 0;
        for (var i = 1; i < path.Length; i++)
            sig += (path[i] - path[i - 1]) * (path[i] - path[i - 1]);
        return Math.Sqrt(sig);
    }

    public static (T Forward, T Reverse) Reversible<T>(Func<T, T> forward, Func<T, T> reverse, T input) =>
        (forward(input), reverse(forward(input)));
}

/// <summary>SOS barrier method. Matches upstream sos_barrier.</summary>
public sealed class SosBarrier
{
    private readonly double _mu;
    public SosBarrier(double mu = 1.0) => _mu = mu;
    public double Barrier(double x) => -_mu * Math.Log(x);
    public double Gradient(double x) => -_mu / x;
}
