using FrankenTui.Runtime;
using FrankenTui.Widgets;
using System.Diagnostics;

namespace FrankenTui.Tests.Headless;

public sealed class LoadGovernorTests
{
    [Fact]
    public void LoadGovernorConfigDefaultsToEnabledConservativeFloor()
    {
        var config = LoadGovernorConfig.Default;

        Assert.True(config.Enabled);
        Assert.Equal(TimeSpan.FromMilliseconds(16), config.EffectiveBudgetController.TargetFrameTime);
        Assert.Equal(RuntimeDegradationLevel.SimpleBorders, config.EffectiveBudgetController.DegradationFloor);
        Assert.Equal(0.5, config.EffectiveBudgetController.EffectivePid.Kp);
        Assert.Equal(0.05, config.EffectiveBudgetController.EffectiveEProcess.Alpha);
        Assert.Equal((uint)10, config.EffectiveBudgetController.EffectiveEProcess.WarmupFrames);
        Assert.Equal(0.5, config.EffectivePolicy.StressedQueueWatermark);
        Assert.Equal(0.8, config.EffectivePolicy.DegradedQueueWatermark);
        Assert.Equal(0.25, config.EffectivePolicy.RecoveryQueueWatermark);
        Assert.Equal((byte)3, config.EffectivePolicy.RecoveryIntervals);
        Assert.Equal(1.0, config.EffectivePolicy.BudgetOverrunSoftRatio);
        Assert.False(LoadGovernorConfig.Disabled.Enabled);
    }

    [Fact]
    public void LoadGovernorStableRuntimeModeLabelsMatchUpstreamContract()
    {
        Assert.Equal("healthy", RuntimeLoadMode.Healthy.Label());
        Assert.Equal("stressed", RuntimeLoadMode.Stressed.Label());
        Assert.Equal("degraded", RuntimeLoadMode.Degraded.Label());
        Assert.Equal("recovered", RuntimeLoadMode.Recovered.Label());
        Assert.Equal("unsafe", RuntimeLoadMode.Unsafe.Label());
        Assert.Equal("steady_state", RuntimePressureClass.SteadyState.Label());
        Assert.Equal("soft_overload", RuntimePressureClass.SoftOverload.Label());
        Assert.Equal("hard_overload", RuntimePressureClass.HardOverload.Label());
        Assert.Equal("unsafe", RuntimePressureClass.Unsafe.Label());
        Assert.Equal("admit_all", RuntimeWorkDisposition.AdmitAll.Label());
        Assert.Equal("coalesce_visible_defer_background", RuntimeWorkDisposition.CoalesceVisibleDeferBackground.Label());
        Assert.Equal("defer_background_drop_best_effort", RuntimeWorkDisposition.DeferBackgroundDropBestEffort.Label());
        Assert.Equal("readmit_after_hysteresis", RuntimeWorkDisposition.ReadmitAfterHysteresis.Label());
        Assert.Equal("fail_fast_strict_guarantee", RuntimeWorkDisposition.FailFastStrictGuarantee.Label());
    }

    [Fact]
    public void LoadGovernorPolicyNormalizesWatermarksLikeUpstream()
    {
        var policy = new LoadGovernorPolicy(
            StressedQueueWatermark: double.NaN,
            DegradedQueueWatermark: 0.1,
            RecoveryQueueWatermark: 0.9,
            RecoveryIntervals: 0,
            BudgetOverrunSoftRatio: -1).Normalized();

        Assert.Equal(0.9, policy.RecoveryQueueWatermark);
        Assert.Equal(0.9, policy.StressedQueueWatermark);
        Assert.Equal(0.9, policy.DegradedQueueWatermark);
        Assert.Equal((byte)1, policy.RecoveryIntervals);
        Assert.Equal(1.0, policy.BudgetOverrunSoftRatio);

        var config = LoadGovernorConfig.Default.WithPolicy(policy);
        Assert.Equal(policy, config.EffectivePolicy);
    }

    [Fact]
    public void ConservativeRuntimeLoadGovernorClassifiesQueueWatermarksAndRecovery()
    {
        var governor = new ConservativeRuntimeLoadGovernor(
            LoadGovernorConfig.Default.WithPolicy(new LoadGovernorPolicy(RecoveryIntervals: 2)),
            maxQueueDepth: 10);

        var stressed = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueInFlight: 5, QueueMaxDepth: 10));
        Assert.Equal(RuntimeLoadMode.Stressed, stressed.Mode);
        Assert.Equal(RuntimePressureClass.SoftOverload, stressed.PressureClass);
        Assert.Equal(RuntimeWorkDisposition.CoalesceVisibleDeferBackground, stressed.WorkDisposition);
        Assert.Equal("queue_stressed_watermark", stressed.ReasonCode);

        var degraded = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueInFlight: 8, QueueMaxDepth: 10));
        Assert.Equal(RuntimeLoadMode.Degraded, degraded.Mode);
        Assert.Equal(RuntimePressureClass.HardOverload, degraded.PressureClass);
        Assert.Equal(RuntimeWorkDisposition.DeferBackgroundDropBestEffort, degraded.WorkDisposition);
        Assert.Equal("queue_degraded_watermark", degraded.ReasonCode);

        var pending = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueInFlight: 0, QueueMaxDepth: 10));
        Assert.Equal(RuntimeLoadMode.Degraded, pending.Mode);
        Assert.Equal("recovery_hysteresis_pending", pending.ReasonCode);
        Assert.Equal((byte)1, pending.RecoveryIntervalsObserved);

        var recovered = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueInFlight: 0, QueueMaxDepth: 10));
        Assert.Equal(RuntimeLoadMode.Recovered, recovered.Mode);
        Assert.Equal(RuntimeWorkDisposition.ReadmitAfterHysteresis, recovered.WorkDisposition);
        Assert.Equal("recovery_hysteresis_satisfied", recovered.ReasonCode);

        var healthy = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueInFlight: 0, QueueMaxDepth: 10));
        Assert.Equal(RuntimeLoadMode.Healthy, healthy.Mode);
        Assert.Equal("recovered_interval_closed", healthy.ReasonCode);
    }

    [Fact]
    public void ConservativeRuntimeLoadGovernorStrictSemanticsFailureIsTerminal()
    {
        var governor = new ConservativeRuntimeLoadGovernor(LoadGovernorConfig.Default, maxQueueDepth: 10);

        var unsafeSnapshot = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, StrictSemanticsViolation: true));
        Assert.Equal(RuntimeLoadMode.Unsafe, unsafeSnapshot.Mode);
        Assert.Equal(RuntimePressureClass.Unsafe, unsafeSnapshot.PressureClass);
        Assert.Equal(RuntimeWorkDisposition.FailFastStrictGuarantee, unsafeSnapshot.WorkDisposition);
        Assert.Equal("strict_semantics_violation", unsafeSnapshot.ReasonCode);
        Assert.False(unsafeSnapshot.StrictSemanticsPreserved);

        var steady = governor.Observe(new RuntimeLoadGovernorObservation(10, 16));
        Assert.Equal(RuntimeLoadMode.Unsafe, steady.Mode);
        Assert.Equal(RuntimeWorkDisposition.FailFastStrictGuarantee, steady.WorkDisposition);
    }

    [Fact]
    public void ConservativeRuntimeLoadGovernorTracksDroppedAndCoalescedWork()
    {
        var governor = new ConservativeRuntimeLoadGovernor(LoadGovernorConfig.Default, maxQueueDepth: 10);

        governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueDroppedTotal: 2));
        var dropped = governor.Observe(new RuntimeLoadGovernorObservation(10, 16, QueueDroppedTotal: 5));
        Assert.Equal(RuntimePressureClass.HardOverload, dropped.PressureClass);
        Assert.Equal("effect_queue_drop", dropped.ReasonCode);
        Assert.Equal((ulong)3, dropped.QueueDroppedDelta);
        Assert.Equal((ulong)3, dropped.DroppedWorkTotal);

        var coalesced = governor.Observe(new RuntimeLoadGovernorObservation(20, 16, ResizeCoalescingActive: true, QueueDroppedTotal: 5));
        Assert.Equal(RuntimePressureClass.SoftOverload, coalesced.PressureClass);
        Assert.True(coalesced.CoalescedWorkTotal >= 1);
    }

    [Fact]
    public async Task RuntimeLoadGovernorDegradesWhenFrameExceedsConfiguredTarget()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                LoadGovernor = LoadGovernorConfig.Default.WithBudgetController(
                    new BudgetControllerConfig(
                        TimeSpan.FromTicks(1),
                        DegradeThreshold: 0.0,
                        UpgradeThreshold: 0.2,
                        CooldownFrames: 0,
                        DegradationFloor: RuntimeDegradationLevel.NoStyling,
                        EProcess: new EProcessConfig(Alpha: 0.99, SigmaFloorMilliseconds: 0.001, WarmupFrames: 0)))
            });

        var decisions = new List<(string Level, string Reason, double PidOutput, double EProcessValue, bool Warmup)>();
        for (var frame = 0; frame < 5; frame++)
        {
            await simulator.DispatchAsync(new SlowGovernorProgram(), 0, "render");
            var stats = simulator.Runtime.FrameStats;
            decisions.Add((
                stats.DegradationLevel,
                stats.LoadGovernorReason,
                stats.LoadGovernorPidOutput,
                stats.LoadGovernorEProcessValue,
                stats.LoadGovernorEProcessInWarmup));
        }

        Assert.Equal("NO_STYLING", simulator.Runtime.FrameStats.DegradationLevel);
        Assert.Contains(decisions, decision => decision.Level == "NO_STYLING");
        Assert.Contains(decisions, decision => decision.Reason is "overload_evidence_passed" or "at_degradation_floor");
        Assert.Contains(decisions, decision => decision.PidOutput > 0);
        Assert.True(decisions.Last().EProcessValue > 1);
        Assert.False(decisions.Last().Warmup);
        Assert.Equal("degraded", simulator.Runtime.FrameStats.RuntimeMode);
        Assert.Equal("hard_overload", simulator.Runtime.FrameStats.RuntimePressureClass);
        Assert.Equal("defer_background_drop_best_effort", simulator.Runtime.FrameStats.RuntimeWorkDisposition);
        Assert.Contains(
            simulator.Runtime.FrameStats.RuntimeGovernorReason,
            new[] { "budget_degradation_active", "effect_queue_drop" });
    }

    [Fact]
    public async Task RuntimeLoadGovernorRequiresEProcessEvidenceAfterWarmup()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                LoadGovernor = LoadGovernorConfig.Default.WithBudgetController(
                    new BudgetControllerConfig(
                        TimeSpan.FromTicks(1),
                        DegradeThreshold: 0.0,
                        UpgradeThreshold: 0.2,
                        CooldownFrames: 0,
                        DegradationFloor: RuntimeDegradationLevel.SimpleBorders,
                        EProcess: new EProcessConfig(WarmupFrames: 10)))
            });

        await simulator.DispatchAsync(new GovernorProgram(), 0, "render");

        Assert.Equal("FULL", simulator.Runtime.FrameStats.DegradationLevel);
        Assert.Equal("overload_evidence_insufficient", simulator.Runtime.FrameStats.LoadGovernorReason);
        Assert.True(simulator.Runtime.FrameStats.LoadGovernorEProcessInWarmup);
        Assert.Equal((uint)1, simulator.Runtime.FrameStats.LoadGovernorFramesObserved);
        Assert.True(simulator.Runtime.FrameStats.LoadGovernorEvidenceThreshold > 0);
    }

    [Fact]
    public async Task RuntimeLoadGovernorCanBeDisabled()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                LoadGovernor = LoadGovernorConfig.Disabled
            });

        await simulator.DispatchAsync(new GovernorProgram(), 0, "render");

        Assert.Equal("FULL", simulator.Runtime.FrameStats.DegradationLevel);
        Assert.Equal("disabled", simulator.Runtime.FrameStats.LoadGovernorReason);
    }

    [Fact]
    public async Task RuntimeEmitsLoadGovernorTelemetryWhenEnabled()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EmitTelemetry = true,
                Telemetry = TelemetryConfig.FromEnvironment(
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
                    }),
                LoadGovernor = LoadGovernorConfig.Default.WithBudgetController(
                    new BudgetControllerConfig(
                        TimeSpan.FromTicks(1),
                        DegradeThreshold: 0.0,
                        CooldownFrames: 0,
                        DegradationFloor: RuntimeDegradationLevel.SimpleBorders,
                        EProcess: new EProcessConfig(Alpha: 0.99, SigmaFloorMilliseconds: 0.001, WarmupFrames: 0)))
            });

        await simulator.DispatchAsync(new GovernorProgram(), 0, "render");

        var item = Assert.Single(
            simulator.Runtime.Telemetry.Events,
            static item => item.Name == "ftui.decision.degradation");
        Assert.Contains(item.Fields, static field => field.Key == "action" && field.Value == "degrade");
        Assert.Contains(item.Fields, static field => field.Key == "level_after" && field.Value == "SIMPLE_BORDERS");
        Assert.Contains(item.Fields, static field => field.Key == "pid_output");
        Assert.Contains(item.Fields, static field => field.Key == "e_value");
        Assert.Contains(item.Fields, static field => field.Key == "evidence_threshold");
        Assert.Contains(item.Fields, static field => field.Key == "transition_correlation_id" && field.Value != "0");
        Assert.Contains(item.Fields, static field => field.Key == "runtime_mode" && field.Value == "degraded");
        Assert.Contains(item.Fields, static field => field.Key == "pressure_class" && field.Value == "hard_overload");
        Assert.Contains(item.Fields, static field => field.Key == "work_disposition" && field.Value == "defer_background_drop_best_effort");
        Assert.Contains(item.Fields, static field => field.Key == "governor_reason" && field.Value == "budget_degradation_active");
        Assert.Contains(item.Fields, static field => field.Key == "strict_semantics_preserved" && field.Value == "true");
        Assert.Contains(item.Fields, static field => field.Key == "queue_in_flight");
        Assert.Contains(item.Fields, static field => field.Key == "conformal_bucket" && field.Value == "altscreen:full:8");
        Assert.Contains(item.Fields, static field => field.Key == "conformal_upper_us");
        Assert.Contains(item.Fields, static field => field.Key == "conformal_budget_us");
        Assert.Contains(item.Fields, static field => field.Key == "cascade_guard_state");
    }

    [Fact]
    public async Task RuntimeFrameStatsCarryConformalCascadeEvidence()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                PolicyConfig = new RuntimePolicyConfig(
                    Conformal: new RuntimeConformalPolicyConfig(MinSamples: 1),
                    FrameGuard: new RuntimeFrameGuardPolicyConfig(FallbackBudgetMicroseconds: 1.0),
                    Budget: new RuntimeBudgetPolicyConfig(TargetFrameMilliseconds: 0.001))
            });

        await simulator.DispatchAsync(new GovernorProgram(), 0, "render");
        await simulator.DispatchAsync(new GovernorProgram(), 0, "render");

        var stats = simulator.Runtime.FrameStats;
        Assert.Equal("altscreen:full:8", stats.ConformalBucketKey);
        Assert.True(stats.ConformalBudgetMicroseconds > 0);
        Assert.True(stats.ConformalUpperMicroseconds >= 0);
        Assert.True(stats.ConformalCalibrationSize >= 1);
        Assert.Equal("at_risk", stats.CascadeGuardState);
        Assert.Equal("degrade", stats.CascadeDecision);
        Assert.Equal("FULL", stats.CascadeLevelBefore);
        Assert.Equal("SIMPLE_BORDERS", stats.CascadeLevelAfter);
    }

    [Fact]
    public async Task RuntimeCascadeSkipFrameBypassesRenderAndPresent()
    {
        var program = new CountingProgram();
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                PolicyConfig = new RuntimePolicyConfig(
                    Conformal: new RuntimeConformalPolicyConfig(MinSamples: 1),
                    FrameGuard: new RuntimeFrameGuardPolicyConfig(FallbackBudgetMicroseconds: 1.0),
                    Cascade: new RuntimeCascadePolicyConfig(
                        MaxDegradation: RuntimeDegradationLevel.SkipFrame,
                        MinTriggerLevel: RuntimeDegradationLevel.SkipFrame,
                        DegradationFloor: RuntimeDegradationLevel.SkipFrame),
                    Budget: new RuntimeBudgetPolicyConfig(TargetFrameMilliseconds: 0.001))
            });

        await simulator.DispatchAsync(program, 0, "render");
        var skipped = await simulator.DispatchAsync(program, 0, "render");

        Assert.Equal(1, program.RenderCount);
        Assert.Equal(string.Empty, skipped.Presentation.Output);
        Assert.Equal(0, skipped.Presentation.ByteCount);
        Assert.Equal("SKIP_FRAME", simulator.Runtime.FrameStats.DegradationLevel);
        Assert.Equal("degrade", simulator.Runtime.FrameStats.CascadeDecision);
        Assert.Equal("SKIP_FRAME", simulator.Runtime.FrameStats.CascadeLevelAfter);
    }

    private sealed class GovernorProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(model);

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"Governor {model}");
    }

    private sealed class SlowGovernorProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(model);

        public IRuntimeView BuildView(int model) => new SlowGovernorView(model);

        private sealed class SlowGovernorView(int model) : IRuntimeView
        {
            public void Render(RuntimeRenderContext context)
            {
                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < TimeSpan.FromMilliseconds(2))
                {
                    Thread.SpinWait(256);
                }

                new ParagraphWidget($"Governor {model}").Render(context);
            }
        }
    }

    private sealed class CountingProgram : IAppProgram<int, string>
    {
        public int RenderCount { get; private set; }

        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(model);

        public IRuntimeView BuildView(int model) => new CountingView(this);

        private sealed class CountingView(CountingProgram owner) : IRuntimeView
        {
            public void Render(RuntimeRenderContext context)
            {
                owner.RenderCount++;
                new ParagraphWidget($"Cascade {context.DegradationLevel.Label()}").Render(context);
            }
        }
    }
}
