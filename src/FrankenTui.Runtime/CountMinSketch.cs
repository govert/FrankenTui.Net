// Upstream source: crates/ftui-runtime/src/countmin_sketch.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of Count-Min Sketch with PAC-Bayes error budgeting.
//
// Space-efficient probabilistic frequency estimation with explicit error
// guarantees. With width w = ceil(e/epsilon) and depth d = ceil(ln(1/delta)):
// P(estimate(x) - count(x) > epsilon * N) <= delta.
//
// Overestimate only. Linear additive. Error is O(epsilon * N).

namespace FrankenTui.Runtime;

/// <summary>Configuration for the Count-Min Sketch.</summary>
public sealed class SketchConfig
{
    /// <summary>Target error rate epsilon. Error &le; epsilon * N with high probability. Default: 0.01.</summary>
    public double Epsilon { get; set; } = 0.01;
    /// <summary>Failure probability delta. P(error > bound) &le; delta. Default: 0.01.</summary>
    public double Delta { get; set; } = 0.01;
    /// <summary>Enable PAC-Bayes calibration. Default: true.</summary>
    public bool EnableCalibration { get; set; } = true;
    /// <summary>Maximum calibration samples to keep. Default: 1000.</summary>
    public int MaxCalibration { get; set; } = 1000;
    /// <summary>Enable JSONL-compatible logging. Default: false.</summary>
    public bool EnableLogging { get; set; }
}

/// <summary>Evidence ledger for error tracking.</summary>
public sealed class ErrorEvidence
{
    public double Epsilon { get; init; }
    public double Delta { get; init; }
    public ulong TotalCount { get; init; }
    public double TheoreticalBound { get; init; }
    public double? CalibratedBound { get; init; }
    public int CalibrationSamples { get; init; }
    public ulong? ObservedMaxError { get; init; }
    public double? ObservedMeanError { get; init; }
    public double? PacBayesKlTerm { get; init; }

    /// <summary>Get the best available error bound.</summary>
    public double ErrorBound() => CalibratedBound ?? TheoreticalBound;

    /// <summary>Generate summary string.</summary>
    public string Summary() =>
        $"eps={Epsilon:F4} delta={Delta:F4} N={TotalCount} bound={ErrorBound():F1} cal={CalibrationSamples}";
}

/// <summary>Aggregate statistics for the sketch.</summary>
public sealed class SketchStats
{
    public int Width { get; init; }
    public int Depth { get; init; }
    public int MemoryBytes { get; init; }
    public ulong TotalCount { get; init; }
    public int UniqueItemsEstimate { get; init; }
    public ErrorEvidence ErrorEvidence { get; init; } = null!;
}

/// <summary>
/// Count-Min Sketch with PAC-Bayes error budgeting.
/// Space-efficient probabilistic frequency estimation with explicit error guarantees.
/// </summary>
public sealed class CountMinSketch
{
    private const double MinEpsilon = 1e-6;
    private const double MinDelta = 1e-12;
    private const int MaxWidth = 1_000_000;
    private const int MaxDepth = 20;

    private readonly SketchConfig _config;
    private readonly int _width;
    private readonly int _depth;
    private readonly ulong[][] _counters;
    private ulong _totalCount;
    private readonly ulong[] _hashSeeds;
    private readonly List<CalibrationSample> _calibration = new();
    private double? _calibratedBound;

    private record struct CalibrationSample(ulong ItemHash, ulong TrueCount, ulong EstimatedCount);

    private CountMinSketch(SketchConfig config, int width, int depth)
    {
        _config = config;
        _width = width;
        _depth = depth;
        _counters = new ulong[depth][];
        for (int i = 0; i < depth; i++) _counters[i] = new ulong[width];
        _hashSeeds = new ulong[depth];
        for (int i = 0; i < depth; i++)
            _hashSeeds[i] = 0x517cc1b727220a95UL * ((ulong)i + 1);
    }

    /// <summary>Create a new sketch with given configuration.</summary>
    public static CountMinSketch Create(SketchConfig config)
    {
        double epsilon = Math.Max(config.Epsilon, MinEpsilon);
        double delta = Math.Max(config.Delta, MinDelta);
        int width = Math.Clamp((int)Math.Ceiling(Math.E / epsilon), 1, MaxWidth);
        int depth = Math.Clamp((int)Math.Ceiling(Math.Log(1.0 / delta)), 1, MaxDepth);
        return new CountMinSketch(config, width, depth);
    }

    /// <summary>Create with specific dimensions (for testing).</summary>
    public static CountMinSketch WithDimensions(int width, int depth)
    {
        width = Math.Clamp(width, 1, MaxWidth);
        depth = Math.Clamp(depth, 1, MaxDepth);
        return new CountMinSketch(new SketchConfig(), width, depth);
    }

    /// <summary>Increment count for an item by 1.</summary>
    public void Increment<T>(T item) where T : notnull => Add(item, 1);

    /// <summary>Add a count to an item.</summary>
    public void Add<T>(T item, ulong count) where T : notnull
    {
        for (int i = 0; i < _depth; i++)
        {
            int index = HashToIndex(item, _hashSeeds[i]);
            _counters[i][index] = unchecked(ulong.MaxValue - _counters[i][index] < count
                ? ulong.MaxValue : _counters[i][index] + count);
        }
        _totalCount = unchecked(ulong.MaxValue - _totalCount < count
            ? ulong.MaxValue : _totalCount + count);
        _calibratedBound = null;
    }

    /// <summary>Estimate count for an item. Never underestimates.</summary>
    public ulong Estimate<T>(T item) where T : notnull
    {
        ulong min = ulong.MaxValue;
        for (int i = 0; i < _depth; i++)
        {
            int index = HashToIndex(item, _hashSeeds[i]);
            if (_counters[i][index] < min) min = _counters[i][index];
        }
        return min == ulong.MaxValue ? 0 : min;
    }

    /// <summary>Add a calibration sample (item, trueCount) for PAC-Bayes tightening.</summary>
    public void Calibrate<T>(T item, ulong trueCount) where T : notnull
    {
        ulong estimated = Estimate(item);
        ulong itemHash = HashItem(item);
        _calibration.Add(new CalibrationSample(itemHash, trueCount, estimated));
        while (_calibration.Count > _config.MaxCalibration)
            _calibration.RemoveAt(0);
        _calibratedBound = null;
    }

    /// <summary>Get current error evidence.</summary>
    public ErrorEvidence GetErrorEvidence()
    {
        double theoreticalBound = _config.Epsilon * _totalCount;
        ComputeCalibratedBound(out var calBound, out var obsMax, out var obsMean, out var klTerm);
        return new ErrorEvidence
        {
            Epsilon = _config.Epsilon, Delta = _config.Delta, TotalCount = _totalCount,
            TheoreticalBound = theoreticalBound, CalibratedBound = calBound,
            CalibrationSamples = _calibration.Count, ObservedMaxError = obsMax,
            ObservedMeanError = obsMean, PacBayesKlTerm = klTerm,
        };
    }

    private void ComputeCalibratedBound(out double? calBound, out ulong? obsMax, out double? obsMean, out double? klTerm)
    {
        calBound = null; obsMax = null; obsMean = null; klTerm = null;
        if (_calibration.Count == 0) return;

        double n = _calibration.Count;
        double kl = Math.Max(Math.Log(n), 1.0) / (2.0 * n);
        klTerm = kl;

        if (_calibratedBound.HasValue)
        {
            var errors = _calibration.Select(s => unchecked(s.EstimatedCount - s.TrueCount)).ToList();
            obsMax = errors.Any() ? errors.Max() : 0;
            obsMean = errors.Count > 0 ? errors.Aggregate(0UL, (a, b) => unchecked(a + b)) / n : 0;
            calBound = _calibratedBound;
            return;
        }

        var errorList = _calibration.Select(s => unchecked(s.EstimatedCount - s.TrueCount)).ToList();
        obsMax = errorList.Any() ? errorList.Max() : 0;
        obsMean = errorList.Count > 0 ? errorList.Aggregate(0UL, (a, b) => unchecked(a + b)) / n : 0;

        double pacBayesBound = obsMean.Value + Math.Sqrt(kl) * _totalCount;
        double theoretical = _config.Epsilon * _totalCount;
        calBound = Math.Min(pacBayesBound, theoretical);
        _calibratedBound = calBound;
    }

    /// <summary>Get sketch statistics.</summary>
    public SketchStats GetStats()
    {
        int uniqueEstimate = _counters.Length > 0 ? _counters[0].Count(c => c > 0) : 0;
        return new SketchStats
        {
            Width = _width, Depth = _depth, MemoryBytes = _width * _depth * 8,
            TotalCount = _totalCount, UniqueItemsEstimate = uniqueEstimate,
            ErrorEvidence = GetErrorEvidence(),
        };
    }

    public int Width => _width;
    public int Depth => _depth;
    public ulong TotalCount => _totalCount;

    /// <summary>Clear all counters.</summary>
    public void Clear()
    {
        for (int i = 0; i < _depth; i++) Array.Clear(_counters[i]);
        _totalCount = 0;
        _calibration.Clear();
        _calibratedBound = null;
    }

    /// <summary>Merge another sketch into this one. Both must have same dimensions.</summary>
    public bool Merge(CountMinSketch other)
    {
        if (_width != other._width || _depth != other._depth) return false;
        for (int i = 0; i < _depth; i++)
            for (int j = 0; j < _width; j++)
                _counters[i][j] = unchecked(ulong.MaxValue - _counters[i][j] < other._counters[i][j]
                    ? ulong.MaxValue : _counters[i][j] + other._counters[i][j]);
        _totalCount = unchecked(ulong.MaxValue - _totalCount < other._totalCount
            ? ulong.MaxValue : _totalCount + other._totalCount);
        _calibratedBound = null;
        return true;
    }

    private int HashToIndex<T>(T item, ulong seed) where T : notnull
    {
        // Deterministic FNV-1a hash — does not depend on .NET's randomized HashCode
        ulong hash = seed;
        var bytes = System.Text.Encoding.UTF8.GetBytes(item!.ToString()!);
        foreach (var b in bytes)
        {
            hash ^= b;
            unchecked { hash *= 0x100000001B3; }
        }
        return (int)(hash % (ulong)_width);
    }

    private static ulong HashItem<T>(T item) where T : notnull
    {
        return (ulong)item!.GetHashCode();
    }
}
