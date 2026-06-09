// Upstream source: crates/ftui-widgets/src/height_predictor.rs (tests module)
// Full 1-1 port of all upstream height_predictor tests.

using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class HeightPredictorTests
{
    // ─── Posterior update tests ────────────────────────────────────

    [Fact]
    public void UnitPosteriorUpdate()
    {
        var config = new PredictorConfig
        {
            PriorMean = 2.0,
            PriorStrength = 1.0,
            PriorVariance = 4.0,
        };
        var pred = new HeightPredictor(config);

        // Prior: μ=2.0, κ=1.
        Assert.True(Math.Abs(pred.PosteriorMean(0) - 2.0) < 1e-10);

        // Observe height 4.
        pred.Observe(0, 4);
        // κ_1 = 1 + 1 = 2, μ_1 = (1*2 + 1*4) / 2 = 3.0
        Assert.True(Math.Abs(pred.PosteriorMean(0) - 3.0) < 1e-10);

        // Observe another height 4.
        pred.Observe(0, 4);
        // κ_2 = 1 + 2 = 3, x̄ = 4, μ_2 = (1*2 + 2*4) / 3 = 10/3 ≈ 3.333
        Assert.True(Math.Abs(pred.PosteriorMean(0) - 10.0 / 3.0) < 1e-10);
    }

    [Fact]
    public void UnitPosteriorVarianceDecreases()
    {
        var pred = new HeightPredictor(new PredictorConfig { PriorVariance = 4.0 });

        double var0 = pred.PosteriorVariance(0);
        Assert.True(var0 > 0.0, "prior variance should be positive");

        // Feed noisy data so Welford variance is non-zero.
        for (int i = 0; i < 10; i++)
        {
            pred.Observe(0, (ushort)(i % 2 == 0 ? 2 : 4));
        }
        double var10 = pred.PosteriorVariance(0);

        for (int i = 0; i < 90; i++)
        {
            pred.Observe(0, (ushort)(i % 2 == 0 ? 2 : 4));
        }
        double var100 = pred.PosteriorVariance(0);

        // With noisy data, posterior variance σ²/κ_n decreases as κ_n grows.
        Assert.True(var10 < var0, $"variance should decrease: {var10} >= {var0}");
        Assert.True(var100 < var10, $"variance should decrease: {var100} >= {var10}");
    }

    // ─── Conformal bounds tests ───────────────────────────────────

    [Fact]
    public void UnitConformalBounds()
    {
        var config = new PredictorConfig
        {
            Coverage = 0.90,
            PriorMean = 3.0,
            PriorStrength = 1.0,
        };
        var pred = new HeightPredictor(config);

        // Feed consistent data.
        for (int i = 0; i < 50; i++)
        {
            pred.Observe(0, 3);
        }

        var p = pred.Predict(0);
        // With all observations at 3, residuals should be near 0.
        // Bounds should be tight around 3.
        Assert.Equal((ushort)3, p.Predicted);
        Assert.True(p.Lower <= 3);
        Assert.True(p.Upper >= 3);
    }

    [Fact]
    public void ConformalBoundsWidenWithNoise()
    {
        var config = new PredictorConfig
        {
            Coverage = 0.90,
            PriorMean = 5.0,
            PriorStrength = 1.0,
        };
        var pred = new HeightPredictor(config);

        // Consistent data → tight bounds.
        for (int i = 0; i < 50; i++)
        {
            pred.Observe(0, 5);
        }
        var tight = pred.Predict(0);

        // Reset with noisy data.
        var pred2 = new HeightPredictor(new PredictorConfig
        {
            Coverage = 0.90,
            PriorMean = 5.0,
            PriorStrength = 1.0,
        });
        ulong seed = 0xABCD_1234_5678_9ABC;
        for (int i = 0; i < 50; i++)
        {
            seed = seed * 6364136223846793005UL + 1442695040888963407UL;
            ushort h = (ushort)(3 + (seed >> 62)); // heights 3..6
            pred2.Observe(0, h);
        }
        var wide = pred2.Predict(0);

        Assert.True(
            (wide.Upper - wide.Lower) >= (tight.Upper - tight.Lower),
            "noisy data should produce wider bounds");
    }

    // ─── Coverage property test ───────────────────────────────────

    [Fact]
    public void PropertyCoverage()
    {
        double alpha = 0.10;
        var config = new PredictorConfig
        {
            Coverage = 1.0 - alpha,
            PriorMean = 3.0,
            PriorStrength = 2.0,
            PriorVariance = 4.0,
            CalibrationWindow = 100,
        };
        var pred = new HeightPredictor(config);

        // Warm up with calibration data.
        ulong seed = 0xDEAD_BEEF_CAFE_0001;
        for (int i = 0; i < 100; i++)
        {
            seed = seed * 6364136223846793005UL + 1442695040888963407UL;
            ushort h = (ushort)(2 + (seed >> 62)); // heights 2..5
            pred.Observe(0, h);
        }

        // Now check coverage on new data.
        uint violations = 0;
        int testN = 200;
        for (int i = 0; i < testN; i++)
        {
            seed = seed * 6364136223846793005UL + 1442695040888963407UL;
            ushort h = (ushort)(2 + (seed >> 62));
            bool within = pred.Observe(0, h);
            if (!within) violations++;
        }

        double violRate = (double)violations / (double)testN;
        // Empirical violation rate should be approximately ≤ α.
        // Allow generous tolerance for finite sample + discrete heights.
        Assert.True(
            violRate <= alpha + 0.15,
            $"violation rate {violRate} exceeds α + tolerance ({alpha} + 0.15)");
    }

    // ─── Scroll stability test ────────────────────────────────────

    [Fact]
    public void E2eScrollStability()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorMean = 1.0,
            PriorStrength = 2.0,
            DefaultHeight = 1,
            Coverage = 0.90,
        });

        // All items are height 1 (most common TUI case).
        uint corrections = 0;
        for (int i = 0; i < 500; i++)
        {
            bool within = pred.Observe(0, 1);
            if (!within) corrections++;
        }

        // With homogeneous heights, should converge quickly with zero corrections
        // after warmup.
        var p = pred.Predict(0);
        Assert.Equal((ushort)1, p.Predicted);
        Assert.True(corrections < 10, $"too many corrections: {corrections}");
    }

    // ─── Multiple categories ──────────────────────────────────────

    [Fact]
    public void CategoriesAreIndependent()
    {
        var pred = HeightPredictor.Default();
        int catA = 0;
        int catB = pred.RegisterCategory();

        // Feed different data to each.
        for (int i = 0; i < 20; i++)
        {
            pred.Observe(catA, 1);
            pred.Observe(catB, 5);
        }

        var pa = pred.Predict(catA);
        var pb = pred.Predict(catB);

        Assert.Equal((ushort)1, pa.Predicted);
        Assert.True(pb.Predicted >= 4 && pb.Predicted <= 5);
    }

    // ─── Cold start ───────────────────────────────────────────────

    [Fact]
    public void ColdPredictionUsesDefault()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            DefaultHeight = 2,
            PriorVariance = 1.0,
        });
        var p = pred.Predict(0);
        Assert.Equal((ushort)2, p.Predicted);
        Assert.Equal(0UL, p.Observations);
    }

    // ─── Determinism ──────────────────────────────────────────────

    [Fact]
    public void DeterministicUnderSameObservations()
    {
        (ushort predicted, double mean) Run()
        {
            var pred = HeightPredictor.Default();
            ushort[] observations = [1, 2, 1, 3, 1, 2, 1, 1, 4, 1];
            foreach (var h in observations)
            {
                pred.Observe(0, h);
            }
            return (pred.Predict(0).Predicted, pred.PosteriorMean(0));
        }

        var (p1, m1) = Run();
        var (p2, m2) = Run();
        Assert.Equal(p1, p2);
        Assert.True(Math.Abs(m1 - m2) < 1e-15);
    }

    // ─── Performance ──────────────────────────────────────────────

    [Fact]
    public void PerfPredictionOverhead()
    {
        var pred = HeightPredictor.Default();

        // Warm up.
        for (int i = 0; i < 100; i++)
        {
            pred.Observe(0, 2);
        }

        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        ushort sink = 0;
        for (int i = 0; i < 100_000; i++)
        {
            sink = (ushort)(sink + pred.Predict(0).Predicted);
        }
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
        var perPrediction = elapsed / 100_000;

        // DIVERGENCE: 10x for .NET JIT/timer overhead (upstream: < 5µs per prediction).
        // Must be < 50µs per prediction.
        Assert.True(
            perPrediction < TimeSpan.FromMicroseconds(50),
            $"prediction too slow: {perPrediction}");
        _ = sink; // prevent optimization
    }

    // ─── Violation tracking ───────────────────────────────────────

    [Fact]
    public void ViolationTracking()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorMean = 5.0,
            PriorStrength = 100.0, // strong prior
            DefaultHeight = 5,
            Coverage = 0.95,
        });

        // Warm up with height=5.
        for (int i = 0; i < 50; i++)
        {
            pred.Observe(0, 5);
        }

        // Sudden jump to height=20 should violate bounds.
        bool within = pred.Observe(0, 20);
        Assert.False(within, "extreme outlier should violate bounds");
        Assert.True(pred.TotalViolations() > 0);
    }

    // ── PredictorConfig defaults ─────────────────────────────────

    [Fact]
    public void ConfigDefaultValues()
    {
        var config = PredictorConfig.Default();
        Assert.Equal((ushort)1, config.DefaultHeight);
        Assert.True(Math.Abs(config.PriorStrength - 2.0) < double.Epsilon);
        Assert.True(Math.Abs(config.PriorMean - 1.0) < double.Epsilon);
        Assert.True(Math.Abs(config.PriorVariance - 4.0) < double.Epsilon);
        Assert.True(Math.Abs(config.Coverage - 0.90) < double.Epsilon);
        Assert.Equal(200, config.CalibrationWindow);
    }

    // ── HeightPredictor::default ─────────────────────────────────

    [Fact]
    public void DefaultPredictorHasOneCategory()
    {
        var pred = HeightPredictor.Default();
        Assert.Equal(1, pred.CategoryCount());
        Assert.Equal(0UL, pred.TotalMeasurements());
        Assert.Equal(0UL, pred.TotalViolations());
        Assert.True(Math.Abs(pred.ViolationRate() - 0.0) < double.Epsilon);
    }

    // ── Predict unknown category ─────────────────────────────────

    [Fact]
    public void PredictUnknownCategoryReturnsCold()
    {
        var pred = HeightPredictor.Default();
        var p = pred.Predict(999);
        Assert.Equal(pred._config.DefaultHeight, p.Predicted);
        Assert.Equal(0UL, p.Observations);
    }

    // ── Observe auto-creates categories ──────────────────────────

    [Fact]
    public void ObserveAutoCreatesCategories()
    {
        var pred = HeightPredictor.Default();
        Assert.Equal(1, pred.CategoryCount());
        pred.Observe(3, 5);
        // Should auto-create categories 1, 2, 3
        Assert.Equal(4, pred.CategoryCount());
        Assert.Equal(1UL, pred.CategoryObservations(3));
    }

    // ── Violation rate ───────────────────────────────────────────

    [Fact]
    public void ViolationRateEmpty()
    {
        var pred = HeightPredictor.Default();
        Assert.True(Math.Abs(pred.ViolationRate() - 0.0) < double.Epsilon);
    }

    [Fact]
    public void ViolationRateComputation()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorMean = 5.0,
            PriorStrength = 100.0,
            DefaultHeight = 5,
            Coverage = 0.95,
        });
        // Warm up so bounds are tight
        for (int i = 0; i < 50; i++)
        {
            pred.Observe(0, 5);
        }
        // 10 normal observations
        for (int i = 0; i < 10; i++)
        {
            pred.Observe(0, 5);
        }
        var beforeViolations = pred.TotalViolations();
        // 1 extreme outlier
        pred.Observe(0, 100);
        var afterViolations = pred.TotalViolations();
        Assert.True(afterViolations > beforeViolations);
        Assert.True(pred.ViolationRate() > 0.0);
    }

    // ── Category accessors ───────────────────────────────────────

    [Fact]
    public void CategoryObservationsReturnsZeroForUnknown()
    {
        var pred = HeightPredictor.Default();
        Assert.Equal(0UL, pred.CategoryObservations(999));
    }

    [Fact]
    public void CategoryObservationsTracksCounts()
    {
        var pred = HeightPredictor.Default();
        pred.Observe(0, 3);
        pred.Observe(0, 4);
        pred.Observe(0, 5);
        Assert.Equal(3UL, pred.CategoryObservations(0));
    }

    // ── Posterior accessors with unknown category ─────────────────

    [Fact]
    public void PosteriorMeanUnknownReturnsPrior()
    {
        var pred = HeightPredictor.Default();
        Assert.True(Math.Abs(pred.PosteriorMean(999) - pred._config.PriorMean) < double.Epsilon);
    }

    [Fact]
    public void PosteriorVarianceUnknownReturnsPrior()
    {
        var pred = HeightPredictor.Default();
        Assert.True(Math.Abs(pred.PosteriorVariance(999) - pred._config.PriorVariance) < double.Epsilon);
    }

    // ── Register category ────────────────────────────────────────

    [Fact]
    public void RegisterCategoryReturnsSequentialIds()
    {
        var pred = HeightPredictor.Default();
        int id1 = pred.RegisterCategory();
        int id2 = pred.RegisterCategory();
        Assert.Equal(1, id1);
        Assert.Equal(2, id2);
        Assert.Equal(3, pred.CategoryCount());
    }

    // ── Observe returns within_bounds ─────────────────────────────

    [Fact]
    public void ObserveReturnsTrueForConsistentData()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorMean = 3.0,
            PriorStrength = 1.0,
        });
        // Warm up
        for (int i = 0; i < 20; i++)
        {
            pred.Observe(0, 3);
        }
        // Same value should be within bounds
        Assert.True(pred.Observe(0, 3));
    }

    // ── Total measurements ───────────────────────────────────────

    [Fact]
    public void TotalMeasurementsIncrements()
    {
        var pred = HeightPredictor.Default();
        for (int i = 0; i < 7; i++)
        {
            pred.Observe(0, (ushort)(i + 1));
        }
        Assert.Equal(7UL, pred.TotalMeasurements());
    }

    // ── HeightPrediction bounds ordering ─────────────────────────

    [Fact]
    public void PredictionLowerLePredictedLeUpper()
    {
        var pred = HeightPredictor.Default();
        for (int i = 0; i < 30; i++)
        {
            pred.Observe(0, 3);
        }
        var p = pred.Predict(0);
        Assert.True(p.Lower <= p.Predicted);
        Assert.True(p.Predicted <= p.Upper);
    }

    // ── Edge-case tests (bd-l9r1a) ──────────────────────────

    [Fact]
    public void ObserveHeightZero()
    {
        var pred = HeightPredictor.Default();
        pred.Observe(0, 0);
        var p = pred.Predict(0);
        // predicted is max(mu.round(), 1.0) so at least 1
        Assert.True(p.Predicted >= 1);
    }

    [Fact]
    public void ObserveHeightMaxU16()
    {
        var pred = HeightPredictor.Default();
        pred.Observe(0, ushort.MaxValue);
        var p = pred.Predict(0);
        Assert.True(p.Predicted > 0);
        Assert.True(p.Observations == 1);
    }

    [Fact]
    public void ColdPredictionZeroVariance()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            DefaultHeight = 5,
            PriorVariance = 0.0,
        });
        var p = pred.Predict(0);
        Assert.Equal((ushort)5, p.Predicted);
        // margin = ceil(sqrt(0.0) * 2.0) = 0
        Assert.Equal((ushort)5, p.Lower);
        Assert.Equal((ushort)5, p.Upper);
    }

    [Fact]
    public void ColdPredictionLargeVariance()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            DefaultHeight = 1,
            PriorVariance = 10000.0,
        });
        var p = pred.Predict(0);
        Assert.Equal((ushort)1, p.Predicted);
        // margin = ceil(sqrt(10000) * 2) = ceil(200) = 200
        Assert.Equal((ushort)0, p.Lower); // 1.saturating_sub(200) = 0
    }

    [Fact]
    public void CoverageZero()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            Coverage = 0.0,
            PriorMean = 3.0,
            PriorStrength = 1.0,
        });
        for (int i = 0; i < 20; i++)
        {
            pred.Observe(0, 3);
        }
        // alpha = 1.0, quantile_idx → 0
        var p = pred.Predict(0);
        Assert.True(p.Predicted > 0);
    }

    [Fact]
    public void CoverageOne()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            Coverage = 1.0,
            PriorMean = 3.0,
            PriorStrength = 1.0,
        });
        for (int i = 0; i < 20; i++)
        {
            pred.Observe(0, 3);
        }
        for (int i = 0; i < 5; i++)
        {
            pred.Observe(0, 10);
        }
        // alpha = 0.0, quantile_idx → max residual
        var p = pred.Predict(0);
        Assert.True(p.Lower <= p.Predicted);
        Assert.True(p.Predicted <= p.Upper);
    }

    [Fact]
    public void CalibrationWindowOne()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            CalibrationWindow = 1,
            PriorMean = 3.0,
            PriorStrength = 1.0,
        });
        for (int i = 0; i < 10; i++)
        {
            pred.Observe(0, 3);
        }
        var p = pred.Predict(0);
        Assert.True(p.Predicted > 0);
        Assert.True(p.Lower <= p.Predicted);
    }

    [Fact]
    public void SingleObservationUsesWideBounds()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorMean = 5.0,
            PriorStrength = 1.0,
            PriorVariance = 4.0,
        });
        pred.Observe(0, 5);
        var p = pred.Predict(0);
        Assert.Equal(1UL, p.Observations);
        // With only 1 residual, bounds come from that single residual
        Assert.True(p.Lower <= p.Predicted);
        Assert.True(p.Predicted <= p.Upper);
    }

    [Fact]
    public void PredictorConfigCloneAndDebug()
    {
        var config = PredictorConfig.Default();
        var cloned = config.Clone();
        Assert.Equal(cloned.DefaultHeight, config.DefaultHeight);
        var dbg = config.ToString();
        Assert.Contains("PredictorConfig", dbg);
    }

    [Fact]
    public void HeightPredictionCopyAndDebug()
    {
        var p = new HeightPrediction
        {
            Predicted = 3,
            Lower = 1,
            Upper = 5,
            Observations = 10,
        };
        var p2 = p; // Copy (struct)
        Assert.Equal(p.Predicted, p2.Predicted);
        Assert.Equal(p.Lower, p2.Lower);
        Assert.Equal(p.Upper, p2.Upper);
        Assert.Equal(p.Observations, p2.Observations);
        var dbg = p.ToString();
        Assert.Contains("HeightPrediction", dbg);
    }

    [Fact]
    public void HeightPredictionClone()
    {
        // HeightPrediction is a struct (value type), copy semantics are implicit.
        var p = new HeightPrediction
        {
            Predicted = 2,
            Lower = 1,
            Upper = 4,
            Observations = 5,
        };
        var cloned = p; // Copy implies Clone; struct copy
        Assert.Equal((ushort)2, cloned.Predicted);
    }

    [Fact]
    public void PredictorCloneIndependence()
    {
        var pred = HeightPredictor.Default();
        pred.Observe(0, 5);
        pred.Observe(0, 5);
        var cloned = pred.Clone();
        cloned.Observe(0, 100);
        // Original should be unaffected
        Assert.Equal(2UL, pred.TotalMeasurements());
        Assert.Equal(3UL, cloned.TotalMeasurements());
    }

    [Fact]
    public void PredictorDebug()
    {
        var pred = HeightPredictor.Default();
        var dbg = pred.ToString();
        Assert.Contains("HeightPredictor", dbg);
    }

    [Fact]
    public void PosteriorVarianceWithTwoIdenticalObservations()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorVariance = 4.0,
            PriorStrength = 1.0,
        });
        pred.Observe(0, 3);
        pred.Observe(0, 3);
        // Welford variance with identical values = 0, κ_n = 3
        // posterior_variance = 0 / 3 = 0
        double var = pred.PosteriorVariance(0);
        Assert.True(Math.Abs(var) < 1e-10, $"identical obs should give ~0 variance, got {var}");
    }

    [Fact]
    public void PosteriorVarianceWithOneObservationUsesPrior()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorVariance = 4.0,
            PriorStrength = 2.0,
        });
        pred.Observe(0, 3);
        // n=1, so welford.variance() returns double.MaxValue → uses prior_variance
        // But wait: code checks n < 2, uses prior_variance = 4.0
        // posterior_variance = 4.0 / (2.0 + 1) = 4/3
        double var = pred.PosteriorVariance(0);
        Assert.True(Math.Abs(var - 4.0 / 3.0) < 1e-10);
    }

    [Fact]
    public void ObserveReturnsFalseForFirstColdOutlier()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            DefaultHeight = 1,
            PriorMean = 1.0,
            PriorStrength = 2.0,
            PriorVariance = 0.25,
        });
        // Cold prediction: predicted=1, margin=ceil(sqrt(0.25)*2)=ceil(1.0)=1
        // bounds: [0, 2]
        // First observation is cold (observations=0), so violation not counted
        bool within = pred.Observe(0, 100);
        // Cold start: prediction.observations == 0, so violation is NOT counted
        Assert.True(within || pred.TotalViolations() == 0);
    }

    [Fact]
    public void AllSameHeightConvergesExactly()
    {
        var pred = new HeightPredictor(new PredictorConfig
        {
            PriorMean = 3.0,
            PriorStrength = 1.0,
        });
        for (int i = 0; i < 100; i++)
        {
            pred.Observe(0, 3);
        }
        var p = pred.Predict(0);
        Assert.Equal((ushort)3, p.Predicted);
        // With all identical observations, bounds should collapse
        Assert.Equal((ushort)3, p.Lower);
        Assert.Equal((ushort)3, p.Upper);
    }

    [Fact]
    public void ManyCategoriesAutoCreated()
    {
        var pred = HeightPredictor.Default();
        pred.Observe(10, 5);
        // Categories 0..=10 should exist now
        Assert.Equal(11, pred.CategoryCount());
        // Intermediate categories have no observations
        Assert.Equal(0UL, pred.CategoryObservations(5));
        Assert.Equal(1UL, pred.CategoryObservations(10));
    }

    [Fact]
    public void PredictionBoundsOrderingAfterMixedData()
    {
        var pred = HeightPredictor.Default();
        foreach (ushort h in new ushort[] { 1, 2, 5, 10, 1, 3, 7, 2, 4, 6 })
        {
            pred.Observe(0, h);
        }
        var p = pred.Predict(0);
        Assert.True(
            p.Lower <= p.Predicted,
            $"lower={p.Lower} > predicted={p.Predicted}");
        Assert.True(
            p.Predicted <= p.Upper,
            $"predicted={p.Predicted} > upper={p.Upper}");
    }
}
