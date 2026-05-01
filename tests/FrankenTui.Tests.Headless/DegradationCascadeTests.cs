using FrankenTui.Runtime;
using System.Text.Json;

namespace FrankenTui.Tests.Headless;

public sealed class DegradationCascadeTests
{
    [Fact]
    public void RuntimeConformalPredictorUsesNPlusOneQuantile()
    {
        var predictor = new RuntimeConformalPredictor(new RuntimeConformalConfig(
            Alpha: 0.2,
            MinSamples: 1,
            WindowSize: 10,
            DefaultResidualMicroseconds: 0));
        var key = RuntimeConformalBucketKey.Default;
        predictor.Observe(key, 0, 1);
        predictor.Observe(key, 0, 2);
        predictor.Observe(key, 0, 3);

        var prediction = predictor.Predict(key, 0, 1_000);

        Assert.Equal(3, prediction.Quantile);
        Assert.Equal(0, prediction.FallbackLevel);
        Assert.Equal(3, prediction.SampleCount);
    }

    [Fact]
    public void RuntimeConformalPredictorFallsBackThroughModeDiffModeAndGlobal()
    {
        var predictor = new RuntimeConformalPredictor(new RuntimeConformalConfig(
            Alpha: 0.1,
            MinSamples: 3,
            WindowSize: 16,
            DefaultResidualMicroseconds: 42));
        var exact = new RuntimeConformalBucketKey(RuntimeModeBucket.Inline, RuntimeDiffBucket.Full, 10);
        var sameModeDiff = new RuntimeConformalBucketKey(RuntimeModeBucket.Inline, RuntimeDiffBucket.Full, 11);
        var sameMode = new RuntimeConformalBucketKey(RuntimeModeBucket.Inline, RuntimeDiffBucket.DirtyRows, 12);
        var otherMode = new RuntimeConformalBucketKey(RuntimeModeBucket.AltScreen, RuntimeDiffBucket.Full, 10);

        foreach (var value in new[] { 10.0, 20.0, 30.0 })
        {
            predictor.Observe(exact, 0, value);
        }

        Assert.Equal(1, predictor.Predict(sameModeDiff, 0, 1_000).FallbackLevel);
        Assert.Equal(2, predictor.Predict(sameMode, 0, 1_000).FallbackLevel);
        Assert.Equal(3, predictor.Predict(otherMode, 0, 1_000).FallbackLevel);

        predictor.ResetAll();
        Assert.Equal(42, predictor.Predict(otherMode, 0, 1_000).Quantile);
    }

    [Fact]
    public void RuntimeConformalPredictorEnforcesWindowAndBucketReset()
    {
        var predictor = new RuntimeConformalPredictor(new RuntimeConformalConfig(
            Alpha: 0.1,
            MinSamples: 1,
            WindowSize: 3));
        var key = RuntimeConformalBucketKey.Default;
        foreach (var value in new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })
        {
            predictor.Observe(key, 0, value);
        }

        Assert.Equal(3, predictor.BucketSamples(key));
        predictor.ResetBucket(key);
        Assert.Equal(0, predictor.BucketSamples(key));
        Assert.Equal(1UL, predictor.ResetCount);
    }

    [Fact]
    public void RuntimeDegradationCascadeStartsAtFullQuality()
    {
        var cascade = new RuntimeDegradationCascade();

        Assert.Equal(RuntimeDegradationLevel.Full, cascade.Level);
        Assert.Equal(0UL, cascade.FrameIndex);
        Assert.True(cascade.ShouldRenderWidget(essential: false));
    }

    [Fact]
    public void RuntimeDegradationCascadeDegradesWhenPredictedP99ExceedsBudget()
    {
        var cascade = new RuntimeDegradationCascade(TestConfig());
        for (var index = 0; index < 8; index++)
        {
            cascade.Observe(TimeSpan.FromMilliseconds(24));
        }

        var evidence = cascade.PreRender(TimeSpan.FromMilliseconds(16));

        Assert.Equal(RuntimeCascadeDecision.Degrade, evidence.Decision);
        Assert.Equal(RuntimeDegradationLevel.SimpleBorders, cascade.Level);
        Assert.Equal(RuntimeGuardState.AtRisk, evidence.GuardState);
        Assert.True(evidence.Prediction.ExceedsBudget);
        Assert.True(evidence.Prediction.UpperMicroseconds > 16_000);
    }

    [Fact]
    public void RuntimeDegradationCascadeRecoversAfterSustainedGoodFrames()
    {
        var cascade = new RuntimeDegradationCascade(TestConfig() with { RecoveryThreshold = 2 });
        for (var index = 0; index < 8; index++)
        {
            cascade.Observe(TimeSpan.FromMilliseconds(24));
        }

        cascade.PreRender(TimeSpan.FromMilliseconds(16));

        for (var index = 0; index < 20; index++)
        {
            cascade.Observe(TimeSpan.FromMilliseconds(1));
        }

        var first = cascade.PreRender(TimeSpan.FromMilliseconds(16));
        var second = cascade.PreRender(TimeSpan.FromMilliseconds(16));

        Assert.Equal(RuntimeCascadeDecision.Hold, first.Decision);
        Assert.Equal(RuntimeCascadeDecision.Recover, second.Decision);
        Assert.Equal(RuntimeDegradationLevel.Full, cascade.Level);
    }

    [Fact]
    public void RuntimeDegradationCascadeEvidenceUsesUpstreamSchemaNames()
    {
        var cascade = new RuntimeDegradationCascade(TestConfig());
        for (var index = 0; index < 8; index++)
        {
            cascade.Observe(TimeSpan.FromMilliseconds(24));
        }

        var evidence = cascade.PreRender(TimeSpan.FromMilliseconds(16));
        using var json = JsonDocument.Parse(evidence.ToJsonl());

        Assert.Equal("degradation-cascade-v1", json.RootElement.GetProperty("schema").GetString());
        Assert.Equal("degrade", json.RootElement.GetProperty("decision").GetString());
        Assert.Equal("at_risk", json.RootElement.GetProperty("guard_state").GetString());
        Assert.True(json.RootElement.GetProperty("p99_exceeds").GetBoolean());
    }

    [Fact]
    public void RuntimeDegradationCascadeFiltersNonEssentialWidgetsAtEssentialOnly()
    {
        var cascade = new RuntimeDegradationCascade(TestConfig() with
        {
            DegradationFloor = RuntimeDegradationLevel.EssentialOnly
        });

        for (var round = 0; round < 3; round++)
        {
            for (var index = 0; index < 8; index++)
            {
                cascade.Observe(TimeSpan.FromMilliseconds(24));
            }

            cascade.PreRender(TimeSpan.FromMilliseconds(16));
        }

        Assert.Equal(RuntimeDegradationLevel.EssentialOnly, cascade.Level);
        Assert.False(cascade.ShouldRenderWidget(essential: false));
        Assert.True(cascade.ShouldRenderWidget(essential: true));
    }

    private static DegradationCascadeConfig TestConfig() =>
        new(
            Guard: new ConformalFrameGuardConfig(
                TimeSpan.FromMilliseconds(16),
                Conformal: new RuntimeConformalConfig(
                    Alpha: 0.1,
                    MinSamples: 4,
                    WindowSize: 16,
                    DefaultResidualMicroseconds: 0),
                EmaDecay: 0.5),
            RecoveryThreshold: 3,
            DegradationFloor: RuntimeDegradationLevel.SimpleBorders);
}
