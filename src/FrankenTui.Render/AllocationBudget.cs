// Ported from crates/ftui-render/src/alloc_budget.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 993CA27E8508AA7D0DFA90CA58FCD332677F6F14EA285AC8022C9809CCCBCF02.
// Managed correspondence: Rust usize is represented by UInt64; the evidence
// ledger is exposed as a read-only view while retaining source insertion order.

using System.Collections.ObjectModel;
using System.Globalization;

namespace FrankenTui.Render;

/// <summary>Configuration for the sequential allocation-leak detector.</summary>
public sealed class LeakDetectorConfig
{
    public double Alpha { get; set; } = 0.05;

    public double Lambda { get; set; } = 0.2;

    public double CusumThreshold { get; set; } = 8.0;

    public double CusumAllowance { get; set; } = 0.5;

    public ulong WarmupFrames { get; set; } = 30;

    public double SigmaDecay { get; set; } = 0.95;

    public double SigmaFloor { get; set; } = 1.0;

    public static LeakDetectorConfig Default => new();

    public LeakDetectorConfig Clone() => new()
    {
        Alpha = Alpha,
        Lambda = Lambda,
        CusumThreshold = CusumThreshold,
        CusumAllowance = CusumAllowance,
        WarmupFrames = WarmupFrames,
        SigmaDecay = SigmaDecay,
        SigmaFloor = SigmaFloor,
    };
}

/// <summary>Evidence recorded for one observed frame.</summary>
public sealed record EvidenceEntry(
    ulong Frame,
    double Value,
    double Residual,
    double CusumUpper,
    double CusumLower,
    double EValue,
    double MeanEstimate,
    double SigmaEstimate)
{
    /// <summary>Serializes this entry using the source JSONL field order and precision.</summary>
    public string ToJsonl()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"frame\":{Frame},\"value\":{Value:F2},\"residual\":{Residual:F4}," +
            $"\"cusum_upper\":{CusumUpper:F4},\"cusum_lower\":{CusumLower:F4}," +
            $"\"e_value\":{EValue:F6},\"mean\":{MeanEstimate:F2},\"sigma\":{SigmaEstimate:F4}}}");
    }
}

/// <summary>Result of one allocation observation.</summary>
public sealed record LeakAlert(
    bool Triggered,
    bool CusumTriggered,
    bool EProcessTriggered,
    double EValue,
    double CusumUpper,
    double CusumLower,
    ulong Frame)
{
    internal static LeakAlert NoAlert(
        ulong frame,
        double eValue,
        double cusumUpper,
        double cusumLower) =>
        new(false, false, false, eValue, cusumUpper, cusumLower, frame);
}

/// <summary>
/// Sequential allocation-leak detector combining a two-sided CUSUM with an
/// anytime-valid e-process.
/// </summary>
public sealed class AllocLeakDetector
{
    private readonly LeakDetectorConfig _config;
    private readonly List<EvidenceEntry> _ledger = [];
    private readonly ReadOnlyCollection<EvidenceEntry> _ledgerView;
    private double _mean;
    private double _m2;
    private double _sigmaEma;
    private double _cusumUpper;
    private double _cusumLower;
    private double _eValue = 1.0;
    private ulong _frames;

    public AllocLeakDetector()
        : this(LeakDetectorConfig.Default)
    {
    }

    public AllocLeakDetector(LeakDetectorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Clone();
        _ledgerView = _ledger.AsReadOnly();
    }

    public double EValue => _eValue;

    public double CusumUpper => _cusumUpper;

    public double CusumLower => _cusumLower;

    public double Mean => _mean;

    public double Sigma => RustMax(_sigmaEma, _config.SigmaFloor);

    public ulong Frames => _frames;

    public IReadOnlyList<EvidenceEntry> Ledger => _ledgerView;

    public double Threshold => 1.0 / _config.Alpha;

    /// <summary>Observes an allocation count or byte total for one frame.</summary>
    public LeakAlert Observe(double value)
    {
        _frames = unchecked(_frames + 1);
        var n = _frames;

        var delta = value - _mean;
        _mean += delta / n;
        var delta2 = value - _mean;
        _m2 += delta * delta2;

        var welfordSigma = n > 1
            ? Math.Sqrt(_m2 / (n - 1))
            : 0.0;

        if (n == 1)
        {
            _sigmaEma = RustMax(welfordSigma, _config.SigmaFloor);
        }
        else
        {
            _sigmaEma = (_config.SigmaDecay * _sigmaEma) +
                ((1.0 - _config.SigmaDecay) * welfordSigma);
        }

        var sigma = RustMax(_sigmaEma, _config.SigmaFloor);
        var residual = delta / sigma;

        if (n <= _config.WarmupFrames)
        {
            _ledger.Add(new EvidenceEntry(
                n,
                value,
                residual,
                0.0,
                0.0,
                1.0,
                _mean,
                sigma));
            return LeakAlert.NoAlert(n, 1.0, 0.0, 0.0);
        }

        _cusumUpper = RustMax(
            _cusumUpper + residual - _config.CusumAllowance,
            0.0);
        _cusumLower = RustMax(
            _cusumLower - residual - _config.CusumAllowance,
            0.0);

        var cusumTriggered =
            _cusumUpper > _config.CusumThreshold ||
            _cusumLower > _config.CusumThreshold;

        var lambda = _config.Lambda;
        var logFactor = (lambda * residual) - ((lambda * lambda) / 2.0);
        if (double.IsNaN(logFactor))
        {
            logFactor = 0.0;
        }

        var factor = Math.Exp(Math.Clamp(logFactor, -10.0, 10.0));
        _eValue *= factor;

        var eProcessTriggered = _eValue >= Threshold;
        var triggered = cusumTriggered || eProcessTriggered;

        _ledger.Add(new EvidenceEntry(
            n,
            value,
            residual,
            _cusumUpper,
            _cusumLower,
            _eValue,
            _mean,
            sigma));

        return new LeakAlert(
            triggered,
            cusumTriggered,
            eProcessTriggered,
            _eValue,
            _cusumUpper,
            _cusumLower,
            n);
    }

    /// <summary>Clears all sequential state while preserving the copied configuration.</summary>
    public void Reset()
    {
        _mean = 0.0;
        _m2 = 0.0;
        _sigmaEma = 0.0;
        _cusumUpper = 0.0;
        _cusumLower = 0.0;
        _eValue = 1.0;
        _frames = 0;
        _ledger.Clear();
    }

    private static double RustMax(double left, double right)
    {
        if (double.IsNaN(left))
        {
            return right;
        }

        return double.IsNaN(right) ? left : Math.Max(left, right);
    }
}
