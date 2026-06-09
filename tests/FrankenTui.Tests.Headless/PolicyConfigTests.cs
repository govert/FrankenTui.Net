// Tests for .external/frankentui/crates/ftui-runtime/src/policy_config.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// DIVERGENCE: Tests that depend on to_*_config() bridge methods
// (blocked on Phases G/H/I config types) are skipped.

using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class PolicyConfigTests
{
    [Fact]
    public void DefaultValidatesClean()
    {
        var errors = PolicyConfig.Default.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCatchesBadAlpha()
    {
        var policy = PolicyConfig.Default;
        policy.Conformal.Alpha = 0.0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("conformal.alpha"));
    }

    [Fact]
    public void ValidateCatchesInvalidCascadeLevels()
    {
        var policy = PolicyConfig.Default;
        policy.Cascade.MinTriggerLevel = DegradationLevel.SkipFrame;
        policy.Cascade.MaxDegradation = DegradationLevel.SimpleBorders;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("cascade.min_trigger_level"));
    }

    [Fact]
    public void ValidateCatchesNegativePid()
    {
        var policy = PolicyConfig.Default;
        policy.Pid.Kp = -1.0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("pid.kp"));
    }

    [Fact]
    public void ValidateCatchesZeroMinSamples()
    {
        var policy = PolicyConfig.Default;
        policy.Conformal.MinSamples = 0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("min_samples"));
    }

    [Fact]
    public void ValidateCatchesZeroLedgerCapacity()
    {
        var policy = PolicyConfig.Default;
        policy.Evidence.LedgerCapacity = 0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("ledger_capacity"));
    }

    [Fact]
    public void ValidateCatchesBadEProcessAlpha()
    {
        var policy = PolicyConfig.Default;
        policy.EProcessBudget.Alpha = 1.5;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("eprocess_budget.alpha"));
    }

    [Fact]
    public void ValidateCatchesBadVoiCost()
    {
        var policy = PolicyConfig.Default;
        policy.Voi.SampleCost = -0.5;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("voi.sample_cost"));
    }

    [Fact]
    public void ValidateCatchesBadBocpdHazard()
    {
        var policy = PolicyConfig.Default;
        policy.Bocpd.HazardLambda = -1.0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("bocpd.hazard_lambda"));
    }

    [Fact]
    public void ValidateCatchesBadThrottleAlpha()
    {
        var policy = PolicyConfig.Default;
        policy.EProcessThrottle.Alpha = 0.0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("eprocess_throttle.alpha"));
    }

    [Fact]
    public void ToJsonlProducesValidJson()
    {
        var jsonl = PolicyConfig.Default.ToJsonl();
        Assert.StartsWith("{", jsonl);
        Assert.EndsWith("}", jsonl);
        Assert.Contains("policy-config-v1", jsonl);
    }

    [Fact]
    public void PartialOverridePreservesDefaults()
    {
        var policy = PolicyConfig.Default;
        policy.Conformal.Alpha = 0.01;
        policy.Cascade.RecoveryThreshold = 20;

        Assert.Equal(20, policy.Conformal.MinSamples);
        Assert.Equal(256, policy.Conformal.WindowSize);
        Assert.Equal(0.5, policy.Pid.Kp);
        Assert.Equal(50.0, policy.Bocpd.HazardLambda);

        Assert.Equal(0.01, policy.Conformal.Alpha);
        Assert.Equal(20U, policy.Cascade.RecoveryThreshold);
    }

    [Fact]
    public void MultipleValidationErrorsCollected()
    {
        var policy = PolicyConfig.Default;
        policy.Conformal.Alpha = 0.0;
        policy.Pid.Kp = -1.0;
        policy.Evidence.LedgerCapacity = 0;
        var errors = policy.Validate();
        Assert.True(errors.Count >= 3, $"should catch multiple errors: [{string.Join(", ", errors)}]");
    }

    [Fact]
    public void ValidateRejectsNonFiniteValues()
    {
        var policy = PolicyConfig.Default;
        policy.Conformal.QDefault = double.NaN;
        policy.FrameGuard.FallbackBudgetUs = double.PositiveInfinity;
        policy.Pid.Kp = double.NegativeInfinity;
        policy.Voi.SampleCost = double.NegativeInfinity;

        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("conformal.q_default must be finite"));
        Assert.Contains(errors, e => e.Contains("frame_guard.fallback_budget_us must be finite"));
        Assert.Contains(errors, e => e.Contains("pid.kp must be finite"));
        Assert.Contains(errors, e => e.Contains("voi.sample_cost must be finite"));
    }

    [Fact]
    public void ValidateCatchesZeroRunLength()
    {
        var policy = PolicyConfig.Default;
        policy.Bocpd.MaxRunLength = 0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("bocpd.max_run_length"));
    }

    [Fact]
    public void ValidateCatchesZeroWindowSize()
    {
        var policy = PolicyConfig.Default;
        policy.Conformal.WindowSize = 0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("conformal.window_size"));
    }

    [Fact]
    public void ValidateCatchesNegativeFallbackBudget()
    {
        var policy = PolicyConfig.Default;
        policy.FrameGuard.FallbackBudgetUs = -1.0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("frame_guard.fallback_budget_us"));
    }

    [Fact]
    public void ValidateCatchesZeroIntegralMax()
    {
        var policy = PolicyConfig.Default;
        policy.Pid.IntegralMax = 0.0;
        var errors = policy.Validate();
        Assert.Contains(errors, e => e.Contains("pid.integral_max"));
    }
}
