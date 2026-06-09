// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/height_predictor.rs
// Bayesian height prediction with conformal bounds for virtualized lists.

using System.Collections.Generic;

namespace FrankenTui.Widgets;

/// <summary>
/// Bayesian height prediction with conformal bounds for virtualized lists.
///
/// Predicts unseen row heights to pre-allocate scroll space and avoid
/// scroll jumps when actual heights are measured lazily.
///
/// # Mathematical Model
///
/// ## Bayesian Online Estimation
///
/// Maintains a Normal-Normal conjugate model per item category:
///
/// <code>
/// Prior:     μ ~ N(μ₀, σ₀²/κ₀)
/// Likelihood: h_i ~ N(μ, σ²)
/// Posterior:  μ | data ~ N(μ_n, σ²/κ_n)
///
/// where:
///   κ_n = κ₀ + n
///   μ_n = (κ₀·μ₀ + n·x̄) / κ_n
///   σ²  estimated via running variance (Welford's algorithm)
/// </code>
///
/// ## Conformal Prediction Bounds
///
/// Given a calibration set of (predicted, actual) residuals, the conformal
/// interval is:
///
/// <code>
/// [μ_n - q_{1-α/2}, μ_n + q_{1-α/2}]
/// </code>
///
/// where q is the empirical quantile of |residuals|. This provides
/// distribution-free coverage: P(h ∈ interval) ≥ 1 - α.
///
/// # Failure Modes
///
/// | Condition | Behavior | Rationale |
/// |-----------|----------|-----------|
/// | No measurements | Return default height | Cold start fallback |
/// | n = 1 | Wide interval (use prior σ) | Insufficient data |
/// | All same height | σ → 0, interval collapses | Homogeneous data |
/// | Actual > bound | Adjust + record violation | Expected at rate α |
/// </summary>

/// <summary>Configuration for the height predictor.</summary>
public sealed class PredictorConfig
{
    /// <summary>Default height when no data is available.</summary>
    public ushort DefaultHeight { get; set; } = 1;
    /// <summary>Prior strength κ₀ (higher = more trust in default). Default: 2.0.</summary>
    public double PriorStrength { get; set; } = 2.0;
    /// <summary>Prior mean μ₀ (usually same as default_height).</summary>
    public double PriorMean { get; set; } = 1.0;
    /// <summary>Prior variance estimate. Default: 4.0.</summary>
    public double PriorVariance { get; set; } = 4.0;
    /// <summary>Conformal coverage level (1 - α). Default: 0.90.</summary>
    public double Coverage { get; set; } = 0.90;
    /// <summary>Max calibration residuals to keep. Default: 200.</summary>
    public int CalibrationWindow { get; set; } = 200;

    /// <summary>Create a PredictorConfig with default values.</summary>
    public static PredictorConfig Default() => new PredictorConfig();

    /// <summary>Clone this config.</summary>
    public PredictorConfig Clone() => new PredictorConfig
    {
        DefaultHeight = DefaultHeight,
        PriorStrength = PriorStrength,
        PriorMean = PriorMean,
        PriorVariance = PriorVariance,
        Coverage = Coverage,
        CalibrationWindow = CalibrationWindow,
    };

    public override string ToString() =>
        $"PredictorConfig {{ DefaultHeight={DefaultHeight}, PriorStrength={PriorStrength}, " +
        $"PriorMean={PriorMean}, PriorVariance={PriorVariance}, Coverage={Coverage}, " +
        $"CalibrationWindow={CalibrationWindow} }}";
}

/// <summary>Running statistics using Welford's online algorithm.</summary>
internal sealed class WelfordStats
{
    internal ulong N;
    internal double Mean;
    /// <summary>Sum of squared deviations</summary>
    internal double M2;

    internal WelfordStats() { N = 0; Mean = 0.0; M2 = 0.0; }

    internal void Update(double x)
    {
        N += 1;
        double delta = x - Mean;
        Mean += delta / (double)N;
        double delta2 = x - Mean;
        M2 += delta * delta2;
    }

    internal double Variance()
    {
        if (N < 2) return double.MaxValue;
        return M2 / (double)(N - 1);
    }

    internal WelfordStats Clone() => new WelfordStats { N = this.N, Mean = this.Mean, M2 = this.M2 };
}

/// <summary>Per-category prediction state.</summary>
internal sealed class CategoryState
{
    /// <summary>Welford running stats for observed heights.</summary>
    internal WelfordStats Welford { get; set; }
    /// <summary>Posterior mean μ_n.</summary>
    internal double PosteriorMean { get; set; }
    /// <summary>Posterior κ_n.</summary>
    internal double PosteriorKappa { get; set; }
    /// <summary>Calibration residuals |predicted - actual|.</summary>
    internal Queue<double> Residuals { get; set; }

    internal CategoryState(WelfordStats welford, double posteriorMean, double posteriorKappa)
    {
        Welford = welford;
        PosteriorMean = posteriorMean;
        PosteriorKappa = posteriorKappa;
        Residuals = new Queue<double>();
    }

    internal CategoryState Clone()
    {
        var c = new CategoryState(Welford.Clone(), PosteriorMean, PosteriorKappa);
        foreach (var r in Residuals) c.Residuals.Enqueue(r);
        return c;
    }
}

/// <summary>A prediction with conformal bounds.</summary>
public readonly struct HeightPrediction
{
    /// <summary>Point prediction (posterior mean, rounded).</summary>
    public ushort Predicted { get; init; }
    /// <summary>Lower conformal bound.</summary>
    public ushort Lower { get; init; }
    /// <summary>Upper conformal bound.</summary>
    public ushort Upper { get; init; }
    /// <summary>Number of observations for this category.</summary>
    public ulong Observations { get; init; }

    public override string ToString() =>
        $"HeightPrediction {{ Predicted={Predicted}, Lower={Lower}, Upper={Upper}, Observations={Observations} }}";
}

/// <summary>Bayesian height predictor with conformal bounds.</summary>
public sealed class HeightPredictor
{
    internal PredictorConfig _config;
    /// <summary>Per-category states. Key is category index (0 = default).</summary>
    private List<CategoryState> _categories;
    /// <summary>Total measurements across all categories.</summary>
    private ulong _totalMeasurements;
    /// <summary>Total bound violations.</summary>
    private ulong _totalViolations;

    /// <summary>Create a new predictor with default config.</summary>
    public HeightPredictor(PredictorConfig config)
    {
        _config = config;
        // Start with one default category.
        var defaultCat = new CategoryState(
            new WelfordStats(),
            config.PriorMean,
            config.PriorStrength);
        _categories = new List<CategoryState> { defaultCat };
        _totalMeasurements = 0;
        _totalViolations = 0;
    }

    /// <summary>Create a default predictor.</summary>
    public static HeightPredictor Default() => new HeightPredictor(PredictorConfig.Default());

    /// <summary>Register a new category. Returns the category id.</summary>
    public int RegisterCategory()
    {
        int id = _categories.Count;
        _categories.Add(new CategoryState(
            new WelfordStats(),
            _config.PriorMean,
            _config.PriorStrength));
        return id;
    }

    /// <summary>Predict height for an item in the given category.</summary>
    public HeightPrediction Predict(int category)
    {
        if (category < 0 || category >= _categories.Count)
            return ColdPrediction();

        var cat = _categories[category];
        if (cat.Welford.N == 0)
            return ColdPrediction();

        double mu = cat.PosteriorMean;
        ushort predicted = (ushort)Math.Max(Math.Round(mu), 1.0);

        // Conformal bounds from calibration residuals.
        var (lower, upper) = ConformalBounds(cat, mu);

        return new HeightPrediction
        {
            Predicted = predicted,
            Lower = lower,
            Upper = upper,
            Observations = cat.Welford.N,
        };
    }

    /// <summary>
    /// Record an actual measured height, updating the model.
    /// Returns whether the measurement was within the predicted bounds.
    /// </summary>
    public bool Observe(int category, ushort actualHeight)
    {
        // Ensure category exists.
        while (_categories.Count <= category)
            RegisterCategory();

        var prediction = Predict(category);
        bool withinBounds = actualHeight >= prediction.Lower && actualHeight <= prediction.Upper;

        _totalMeasurements += 1;
        if (!withinBounds && prediction.Observations > 0)
            _totalViolations += 1;

        var cat = _categories[category];
        double h = (double)actualHeight;

        // Record calibration residual.
        double residual = Math.Abs(cat.PosteriorMean - h);
        cat.Residuals.Enqueue(residual);
        if (cat.Residuals.Count > _config.CalibrationWindow)
            cat.Residuals.Dequeue();

        // Update Welford stats.
        cat.Welford.Update(h);

        // Update posterior: μ_n = (κ₀·μ₀ + n·x̄) / κ_n
        double n = (double)cat.Welford.N;
        double kappa0 = _config.PriorStrength;
        double mu0 = _config.PriorMean;
        cat.PosteriorKappa = kappa0 + n;
        cat.PosteriorMean = (kappa0 * mu0 + n * cat.Welford.Mean) / cat.PosteriorKappa;

        return withinBounds;
    }

    /// <summary>Cold-start prediction when no data is available.</summary>
    private HeightPrediction ColdPrediction()
    {
        ushort d = _config.DefaultHeight;
        ushort margin = (ushort)Math.Ceiling(Math.Sqrt(_config.PriorVariance) * 2.0);
        return new HeightPrediction
        {
            Predicted = d,
            Lower = (ushort)Math.Max(0, (int)d - (int)margin),
            Upper = (ushort)Math.Min(ushort.MaxValue, (int)d + (int)margin),
            Observations = 0,
        };
    }

    /// <summary>Compute conformal bounds from calibration residuals.</summary>
    private (ushort lower, ushort upper) ConformalBounds(CategoryState cat, double mu)
    {
        if (cat.Residuals.Count == 0)
        {
            // Fallback: use prior variance.
            ushort margin = (ushort)Math.Ceiling(Math.Sqrt(_config.PriorVariance) * 2.0);
            ushort predicted = (ushort)Math.Max(Math.Round(mu), 1.0);
            return (
                (ushort)Math.Max(0, (int)predicted - (int)margin),
                (ushort)Math.Min(ushort.MaxValue, (int)predicted + (int)margin)
            );
        }

        // Sort residuals to find quantile.
        var sorted = new List<double>(cat.Residuals);
        sorted.Sort((a, b) =>
        {
            if (double.IsNaN(a) && double.IsNaN(b)) return 0;
            if (double.IsNaN(a)) return 1;
            if (double.IsNaN(b)) return -1;
            return a.CompareTo(b);
        });

        double alpha = 1.0 - _config.Coverage;
        double nf = (double)sorted.Count;
        int quantileIdx = (int)Math.Ceiling((1.0 - alpha) * (nf + 1.0));
        quantileIdx = Math.Min(quantileIdx, sorted.Count);
        quantileIdx = Math.Max(quantileIdx - 1, 0);
        double q = sorted[quantileIdx];

        ushort lower = (ushort)Math.Max(Math.Floor(mu - q), 1.0);
        ushort upper = (ushort)Math.Max(Math.Ceiling(mu + q), 1.0);

        return (lower, upper);
    }

    /// <summary>Get the posterior mean for a category.</summary>
    public double PosteriorMean(int category)
    {
        if (category < 0 || category >= _categories.Count)
            return _config.PriorMean;
        return _categories[category].PosteriorMean;
    }

    /// <summary>Get the posterior variance for a category.</summary>
    public double PosteriorVariance(int category)
    {
        if (category < 0 || category >= _categories.Count)
            return _config.PriorVariance;
        var c = _categories[category];
        double sigmaSq = c.Welford.N < 2 ? _config.PriorVariance : c.Welford.Variance();
        return sigmaSq / c.PosteriorKappa;
    }

    /// <summary>Total measurements observed.</summary>
    public ulong TotalMeasurements() => _totalMeasurements;

    /// <summary>Total bound violations.</summary>
    public ulong TotalViolations() => _totalViolations;

    /// <summary>Empirical violation rate.</summary>
    public double ViolationRate()
    {
        if (_totalMeasurements == 0) return 0.0;
        return (double)_totalViolations / (double)_totalMeasurements;
    }

    /// <summary>Number of categories.</summary>
    public int CategoryCount() => _categories.Count;

    /// <summary>Number of observations for a category.</summary>
    public ulong CategoryObservations(int category)
    {
        if (category < 0 || category >= _categories.Count) return 0;
        return _categories[category].Welford.N;
    }

    /// <summary>Clone this predictor (deep copy).</summary>
    public HeightPredictor Clone()
    {
        var copy = new HeightPredictor(_config.Clone());
        copy._categories.Clear();
        foreach (var cat in _categories)
            copy._categories.Add(cat.Clone());
        copy._totalMeasurements = _totalMeasurements;
        copy._totalViolations = _totalViolations;
        return copy;
    }

    public override string ToString() =>
        $"HeightPredictor {{ categories={_categories.Count}, total_measurements={_totalMeasurements}, " +
        $"total_violations={_totalViolations} }}";
}
