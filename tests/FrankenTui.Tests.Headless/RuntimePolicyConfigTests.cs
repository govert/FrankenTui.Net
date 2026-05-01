using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class RuntimePolicyConfigTests
{
    [Fact]
    public void RuntimePolicyConfigDefaultsMatchUpstreamPolicyDefaults()
    {
        var policy = RuntimePolicyConfig.Default;

        var conformal = policy.ToConformalConfig();
        Assert.Equal(0.05, conformal.Alpha);
        Assert.Equal(20, conformal.MinSamples);
        Assert.Equal(256, conformal.WindowSize);
        Assert.Equal(10_000.0, conformal.DefaultResidualMicroseconds);

        var guard = policy.ToFrameGuardConfig();
        Assert.Equal(TimeSpan.FromMilliseconds(16), guard.FallbackBudget);
        Assert.Equal(512, guard.TimeSeriesWindow);
        Assert.Equal(256, guard.NonconformityWindow);

        var cascade = policy.ToCascadeConfig();
        Assert.Equal(10, cascade.RecoveryThreshold);
        Assert.Equal(RuntimeDegradationLevel.SkipFrame, cascade.MaxDegradation);
        Assert.Equal(RuntimeDegradationLevel.SimpleBorders, cascade.MinTriggerLevel);
        Assert.Equal(RuntimeDegradationLevel.SimpleBorders, cascade.DegradationFloor);

        var budget = policy.ToBudgetControllerConfig();
        Assert.Equal(TimeSpan.FromMilliseconds(16), budget.TargetFrameTime);
        Assert.Equal(0.5, budget.EffectivePid.Kp);
        Assert.Equal(0.05, budget.EffectivePid.Ki);
        Assert.Equal(0.2, budget.EffectivePid.Kd);
        Assert.Equal(5.0, budget.EffectivePid.IntegralMax);
        Assert.Equal(0.5, budget.EffectiveEProcess.Lambda);
        Assert.Equal(0.05, budget.EffectiveEProcess.Alpha);
        Assert.Equal(0.5, budget.EffectiveEProcess.Beta);
        Assert.Equal(0.9, budget.EffectiveEProcess.SigmaEmaDecay);
        Assert.Equal(1.0, budget.EffectiveEProcess.SigmaFloorMilliseconds);
        Assert.Equal((uint)10, budget.EffectiveEProcess.WarmupFrames);
    }

    [Fact]
    public void RuntimePolicyConfigConvertsCustomValues()
    {
        var policy = new RuntimePolicyConfig(
            Conformal: new RuntimeConformalPolicyConfig(0.1, 7, 32, 123.0),
            FrameGuard: new RuntimeFrameGuardPolicyConfig(8_500.0, 64, 33),
            Cascade: new RuntimeCascadePolicyConfig(
                RecoveryThreshold: 4,
                MaxDegradation: RuntimeDegradationLevel.Skeleton,
                MinTriggerLevel: RuntimeDegradationLevel.NoStyling,
                DegradationFloor: RuntimeDegradationLevel.EssentialOnly),
            Pid: new RuntimePidPolicyConfig(1.1, 1.2, 1.3, 9.0),
            EProcessBudget: new RuntimeEProcessBudgetPolicyConfig(0.7, 0.2, 0.6, 0.8, 2.5, 3),
            Budget: new RuntimeBudgetPolicyConfig(12.5, 0.4, 0.1, 6, RuntimeDegradationLevel.NoStyling));

        var cascade = policy.ToCascadeConfig();
        Assert.Equal(TimeSpan.FromMicroseconds(8_500.0), cascade.EffectiveGuard.FallbackBudget);
        Assert.Equal(0.1, cascade.EffectiveGuard.EffectiveConformal.Alpha);
        Assert.Equal(7, cascade.EffectiveGuard.EffectiveConformal.MinSamples);
        Assert.Equal(32, cascade.EffectiveGuard.EffectiveConformal.WindowSize);
        Assert.Equal(123.0, cascade.EffectiveGuard.EffectiveConformal.DefaultResidualMicroseconds);
        Assert.Equal(4, cascade.RecoveryThreshold);
        Assert.Equal(RuntimeDegradationLevel.Skeleton, cascade.MaxDegradation);
        Assert.Equal(RuntimeDegradationLevel.NoStyling, cascade.MinTriggerLevel);
        Assert.Equal(RuntimeDegradationLevel.EssentialOnly, cascade.DegradationFloor);

        var budget = policy.ToBudgetControllerConfig();
        Assert.Equal(TimeSpan.FromMilliseconds(12.5), budget.TargetFrameTime);
        Assert.Equal(0.4, budget.DegradeThreshold);
        Assert.Equal(0.1, budget.UpgradeThreshold);
        Assert.Equal(6, budget.CooldownFrames);
        Assert.Equal(RuntimeDegradationLevel.NoStyling, budget.DegradationFloor);
        Assert.Equal(1.1, budget.EffectivePid.Kp);
        Assert.Equal(1.2, budget.EffectivePid.Ki);
        Assert.Equal(1.3, budget.EffectivePid.Kd);
        Assert.Equal(9.0, budget.EffectivePid.IntegralMax);
        Assert.Equal(0.7, budget.EffectiveEProcess.Lambda);
        Assert.Equal(0.2, budget.EffectiveEProcess.Alpha);
        Assert.Equal(0.6, budget.EffectiveEProcess.Beta);
        Assert.Equal(0.8, budget.EffectiveEProcess.SigmaEmaDecay);
        Assert.Equal(2.5, budget.EffectiveEProcess.SigmaFloorMilliseconds);
        Assert.Equal((uint)3, budget.EffectiveEProcess.WarmupFrames);
    }

    [Fact]
    public void RuntimeExecutionPolicyUsesPolicyConfigForLoadGovernorWhenNoExplicitOverride()
    {
        var policy = RuntimeExecutionPolicy.Default with
        {
            PolicyConfig = new RuntimePolicyConfig(
                Budget: new RuntimeBudgetPolicyConfig(20.0, DegradationFloor: RuntimeDegradationLevel.NoStyling),
                Pid: new RuntimePidPolicyConfig(Kp: 2.0),
                EProcessBudget: new RuntimeEProcessBudgetPolicyConfig(WarmupFrames: 0))
        };

        var loadGovernor = policy.EffectiveLoadGovernor;

        Assert.True(loadGovernor.Enabled);
        Assert.Equal(TimeSpan.FromMilliseconds(20), loadGovernor.EffectiveBudgetController.TargetFrameTime);
        Assert.Equal(RuntimeDegradationLevel.NoStyling, loadGovernor.EffectiveBudgetController.DegradationFloor);
        Assert.Equal(2.0, loadGovernor.EffectiveBudgetController.EffectivePid.Kp);
        Assert.Equal((uint)0, loadGovernor.EffectiveBudgetController.EffectiveEProcess.WarmupFrames);
    }

    [Fact]
    public void RuntimeExecutionPolicyExplicitLoadGovernorOverridesPolicyConfig()
    {
        var policy = RuntimeExecutionPolicy.Default with
        {
            PolicyConfig = new RuntimePolicyConfig(
                Budget: new RuntimeBudgetPolicyConfig(20.0)),
            LoadGovernor = LoadGovernorConfig.Disabled
        };

        Assert.False(policy.EffectiveLoadGovernor.Enabled);
    }

    [Fact]
    public void RuntimeExecutionPolicyExposesPolicyBackedCascadeConfig()
    {
        var policy = RuntimeExecutionPolicy.Default with
        {
            PolicyConfig = new RuntimePolicyConfig(
                FrameGuard: new RuntimeFrameGuardPolicyConfig(9_000.0),
                Cascade: new RuntimeCascadePolicyConfig(
                    RecoveryThreshold: 5,
                    MaxDegradation: RuntimeDegradationLevel.EssentialOnly))
        };

        var cascade = policy.EffectiveDegradationCascade;

        Assert.Equal(TimeSpan.FromMicroseconds(9_000.0), cascade.EffectiveGuard.FallbackBudget);
        Assert.Equal(5, cascade.RecoveryThreshold);
        Assert.Equal(RuntimeDegradationLevel.EssentialOnly, cascade.MaxDegradation);
    }
}
