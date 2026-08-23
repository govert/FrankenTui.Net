// Differential contract for crates/ftui-render/src/alloc_budget.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class AllocationBudgetTests
{
    [Fact]
    public void DefaultsMatchSource()
    {
        var config = LeakDetectorConfig.Default;

        Assert.Equal(0.05, config.Alpha);
        Assert.Equal(0.2, config.Lambda);
        Assert.Equal(8.0, config.CusumThreshold);
        Assert.Equal(0.5, config.CusumAllowance);
        Assert.Equal(30UL, config.WarmupFrames);
        Assert.Equal(0.95, config.SigmaDecay);
        Assert.Equal(1.0, config.SigmaFloor);
    }

    [Fact]
    public void ConfigurationCloneIsIndependent()
    {
        var source = LeakDetectorConfig.Default;
        var clone = source.Clone();
        clone.Alpha = 0.25;

        Assert.Equal(0.05, source.Alpha);
        Assert.Equal(0.25, clone.Alpha);
    }

    [Fact]
    public void DetectorCopiesConfigurationAtConstruction()
    {
        var config = LeakDetectorConfig.Default;
        var detector = new AllocLeakDetector(config);
        config.Alpha = 0.5;

        Assert.Equal(20.0, detector.Threshold);
    }

    [Fact]
    public void NewDetectorStartsClean()
    {
        var detector = new AllocLeakDetector();

        Assert.Equal(0UL, detector.Frames);
        Assert.Equal(1.0, detector.EValue);
        Assert.Equal(0.0, detector.CusumUpper);
        Assert.Equal(0.0, detector.CusumLower);
        Assert.Empty(detector.Ledger);
    }

    [Fact]
    public void WarmupIsInertButRecordsEvidence()
    {
        var detector = new AllocLeakDetector();

        for (var index = 0; index < 30; index++)
        {
            var alert = detector.Observe(100.0 + (index * 0.5));
            Assert.False(alert.Triggered);
            Assert.Equal(0.0, alert.CusumUpper);
            Assert.Equal(0.0, alert.CusumLower);
            Assert.Equal(1.0, alert.EValue);
        }

        Assert.Equal(30UL, detector.Frames);
        Assert.Equal(30, detector.Ledger.Count);
    }

    [Fact]
    public void StableDeterministicRunDoesNotAlert()
    {
        var random = new Lcg(0xCAFE);
        var detector = new AllocLeakDetector();

        for (var index = 0; index < 500; index++)
        {
            Assert.False(detector.Observe(random.NextNormal(100.0, 5.0)).Triggered);
        }
    }

    [Fact]
    public void CusumDetectsSustainedUpwardShift()
    {
        var detector = DetectorWith(warmup: 20);
        ObserveRepeated(detector, 100.0, 20);

        var detectedAt = Enumerable.Range(0, 200)
            .First(index => detector.Observe(110.0).CusumTriggered);

        Assert.True(detectedAt < 50);
    }

    [Fact]
    public void CusumDetectsSustainedDownwardShift()
    {
        var detector = DetectorWith(warmup: 20);
        ObserveRepeated(detector, 100.0, 20);

        var detectedAt = Enumerable.Range(0, 200)
            .First(index => detector.Observe(90.0).CusumLower > 8.0);

        Assert.True(detectedAt < 50);
        Assert.Equal(detector.CusumLower, detector.Ledger[^1].CusumLower);
    }

    [Fact]
    public void EProcessCrossesItsThresholdUnderSustainedShift()
    {
        var detector = DetectorWith(alpha: 0.05, lambda: 0.3, warmup: 10);
        ObserveRepeated(detector, 100.0, 10);

        var detectedAt = Enumerable.Range(0, 300)
            .First(index => detector.Observe(120.0).EProcessTriggered);

        Assert.True(detectedAt < 150);
        Assert.True(detector.EValue >= detector.Threshold);
    }

    [Fact]
    public void EProcessStaysBoundedUnderNullSequence()
    {
        var random = new Lcg(0xBEEF);
        var detector = DetectorWith(warmup: 20);

        for (var index = 0; index < 1_000; index++)
        {
            detector.Observe(random.NextNormal(100.0, 5.0));
        }

        Assert.True(detector.EValue < 100.0);
    }

    [Fact]
    public void EmpiricalEProcessFalsePositiveRateMatchesSourceBound()
    {
        const double alpha = 0.10;
        var random = new Lcg(0xAAAA);
        var falsePositives = 0;

        for (var run = 0; run < 200; run++)
        {
            var detector = DetectorWith(alpha: alpha, warmup: 20);
            var triggered = false;
            for (var frame = 0; frame < 200; frame++)
            {
                if (!detector.Observe(random.NextNormal(100.0, 5.0)).EProcessTriggered)
                {
                    continue;
                }

                triggered = true;
                break;
            }

            falsePositives += triggered ? 1 : 0;
        }

        Assert.True(falsePositives / 200.0 <= alpha + 0.10);
    }

    [Fact]
    public void LedgerRecordsEveryFrameInOrder()
    {
        var detector = new AllocLeakDetector();
        for (var frame = 0; frame < 50; frame++)
        {
            detector.Observe(100.0 + frame);
        }

        Assert.Equal(50, detector.Ledger.Count);
        Assert.Equal(
            Enumerable.Range(1, 50).Select(static value => (ulong)value),
            detector.Ledger.Select(static entry => entry.Frame));
    }

    [Fact]
    public void LedgerViewCannotBeMutated()
    {
        var detector = new AllocLeakDetector();
        detector.Observe(1.0);
        var collection = Assert.IsAssignableFrom<ICollection<EvidenceEntry>>(detector.Ledger);

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
    }

    [Fact]
    public void JsonlMatchesSourceFieldOrderAndPrecision()
    {
        var entry = new EvidenceEntry(
            7,
            123.45,
            -0.5678,
            3.25,
            0.0,
            2.75,
            120.0,
            4.5678);

        Assert.Equal(
            "{\"frame\":7,\"value\":123.45,\"residual\":-0.5678," +
            "\"cusum_upper\":3.2500,\"cusum_lower\":0.0000," +
            "\"e_value\":2.750000,\"mean\":120.00,\"sigma\":4.5678}",
            entry.ToJsonl());
    }

    [Fact]
    public void ResetClearsSequentialStateAndPermitsReuse()
    {
        var detector = new AllocLeakDetector();
        ObserveRepeated(detector, 100.0, 50);
        detector.Reset();

        Assert.Equal(0UL, detector.Frames);
        Assert.Equal(0.0, detector.Mean);
        Assert.Equal(0.0, detector.CusumUpper);
        Assert.Equal(0.0, detector.CusumLower);
        Assert.Equal(1.0, detector.EValue);
        Assert.Empty(detector.Ledger);

        ObserveRepeated(detector, 200.0, 50);
        Assert.Equal(200.0, detector.Mean, 10);
    }

    [Fact]
    public void GradualSyntheticLeakIsDetected()
    {
        var random = new Lcg(0x5678);
        var detector = new AllocLeakDetector();
        for (var frame = 0; frame < 50; frame++)
        {
            detector.Observe(random.NextNormal(100.0, 3.0));
        }

        var detected = false;
        for (var frame = 0; frame < 200; frame++)
        {
            detected |= detector.Observe(
                random.NextNormal(100.0 + (0.5 * frame), 3.0)).Triggered;
        }

        Assert.True(detected);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(42.0)]
    [InlineData(-100.0)]
    [InlineData(1e15)]
    [InlineData(1e-15)]
    public void ConstantFiniteInputDoesNotAlert(double value)
    {
        var detector = new AllocLeakDetector();
        for (var frame = 0; frame < 200; frame++)
        {
            Assert.False(detector.Observe(value).Triggered);
        }

        Assert.Equal(value, detector.Mean, 10);
    }

    [Fact]
    public void SigmaFloorPreventsConstantSequenceExplosion()
    {
        var detector = new AllocLeakDetector(new LeakDetectorConfig
        {
            SigmaFloor = 5.0,
            WarmupFrames = 5,
        });

        ObserveRepeated(detector, 100.0, 50);

        Assert.True(detector.Sigma >= 5.0);
        Assert.True(double.IsFinite(detector.EValue));
    }

    [Fact]
    public void LargerShiftDetectsNoLaterThanSmallerShift()
    {
        static int DetectAt(double shift)
        {
            var detector = DetectorWith(warmup: 20);
            ObserveRepeated(detector, 100.0, 20);
            for (var frame = 0; frame < 500; frame++)
            {
                if (detector.Observe(100.0 + shift).Triggered)
                {
                    return frame;
                }
            }

            return 500;
        }

        Assert.True(DetectAt(20.0) <= DetectAt(5.0));
    }

    [Fact]
    public void WelfordMeanMatchesExactMean()
    {
        var detector = new AllocLeakDetector();
        foreach (var value in new[] { 10.0, 20.0, 30.0, 40.0, 50.0 })
        {
            detector.Observe(value);
        }

        Assert.Equal(30.0, detector.Mean, 10);
    }

    [Theory]
    [InlineData(0.05, 20.0)]
    [InlineData(0.10, 10.0)]
    [InlineData(0.01, 100.0)]
    public void ThresholdIsInverseAlpha(double alpha, double threshold)
    {
        Assert.Equal(threshold, DetectorWith(alpha: alpha).Threshold, 10);
    }

    [Fact]
    public void ZeroWarmupRunsDetectorsOnFirstFrame()
    {
        var detector = DetectorWith(warmup: 0);
        var alert = detector.Observe(100.0);

        Assert.Equal(1UL, alert.Frame);
        Assert.False(double.IsNaN(alert.EValue));
        Assert.NotEqual(1.0, alert.EValue);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteInputAdvancesWithoutThrowing(double value)
    {
        var detector = new AllocLeakDetector();
        ObserveRepeated(detector, 100.0, 10);

        var exception = Record.Exception(() => detector.Observe(value));

        Assert.Null(exception);
        Assert.Equal(11UL, detector.Frames);
    }

    [Fact]
    public void OscillatingValuesDoNotAlert()
    {
        var detector = new AllocLeakDetector();
        for (var frame = 0; frame < 300; frame++)
        {
            Assert.False(detector.Observe(frame % 2 == 0 ? 105.0 : 95.0).Triggered);
        }
    }

    [Fact]
    public void BothDetectorsCanTriggerOnMassiveShift()
    {
        var detector = DetectorWith(lambda: 0.5, warmup: 5);
        ObserveRepeated(detector, 100.0, 5);

        var both = Enumerable.Range(0, 500)
            .Select(_ => detector.Observe(200.0))
            .First(alert => alert.CusumTriggered && alert.EProcessTriggered);

        Assert.True(both.Triggered);
    }

    [Fact]
    public void CusumFallsAfterTransientSpike()
    {
        var detector = DetectorWith(warmup: 10);
        ObserveRepeated(detector, 100.0, 10);
        ObserveRepeated(detector, 120.0, 3);
        var spike = detector.CusumUpper;
        ObserveRepeated(detector, 100.0, 50);

        Assert.True(detector.CusumUpper < spike);
    }

    [Fact]
    public void EProcessGrowsUnderSustainedShift()
    {
        var detector = DetectorWith(warmup: 10);
        ObserveRepeated(detector, 100.0, 10);
        var before = detector.EValue;
        ObserveRepeated(detector, 115.0, 50);

        Assert.True(detector.EValue > before);
    }

    [Fact]
    public void LedgerMeanAndSigmaConvergeAndRemainPositive()
    {
        var random = new Lcg(0xDEAD);
        var detector = new AllocLeakDetector();
        for (var frame = 0; frame < 200; frame++)
        {
            detector.Observe(random.NextNormal(50.0, 5.0));
        }

        Assert.InRange(detector.Ledger[^1].MeanEstimate, 49.0, 51.0);
        Assert.All(detector.Ledger, static entry => Assert.True(entry.SigmaEstimate > 0.0));
    }

    [Fact]
    public void WarmupKeepsEValueAtOne()
    {
        var detector = DetectorWith(warmup: 10);
        ObserveRepeated(detector, 100.0, 10);

        Assert.Equal(1.0, detector.EValue);
        Assert.All(detector.Ledger, static entry => Assert.Equal(1.0, entry.EValue));
    }

    [Fact]
    public void HighAlphaTriggersQuickly()
    {
        var detector = DetectorWith(alpha: 0.5, lambda: 0.3, warmup: 5);
        ObserveRepeated(detector, 100.0, 5);

        var triggered = Enumerable.Range(0, 100)
            .Any(_ => detector.Observe(110.0).EProcessTriggered);

        Assert.True(triggered);
    }

    private static AllocLeakDetector DetectorWith(
        double alpha = 0.05,
        double lambda = 0.2,
        ulong warmup = 30)
    {
        return new AllocLeakDetector(new LeakDetectorConfig
        {
            Alpha = alpha,
            Lambda = lambda,
            WarmupFrames = warmup,
        });
    }

    private static void ObserveRepeated(AllocLeakDetector detector, double value, int count)
    {
        for (var index = 0; index < count; index++)
        {
            detector.Observe(value);
        }
    }

    private sealed class Lcg(ulong seed)
    {
        private ulong _state = seed;

        public double NextNormal(double mean, double standardDeviation)
        {
            var sum = 0.0;
            for (var index = 0; index < 12; index++)
            {
                sum += NextUInt64() / (double)ulong.MaxValue;
            }

            return mean + (standardDeviation * (sum - 6.0));
        }

        private ulong NextUInt64()
        {
            _state = unchecked((_state * 6_364_136_223_846_793_005UL) + 1);
            return _state;
        }
    }
}
