using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public sealed class EffectSystemTests
{
    [Fact]
    public async Task RuntimeTracksCommandAndSubscriptionEffects()
    {
        var before = EffectSystem.SnapshotRuntimeDynamics();
        var simulator = Ui.CreateSimulator<int, string>(32, 8);

        await simulator.DispatchAsync(new EffectProgram(), 0, "emit");
        await simulator.DispatchAsync(new EffectProgram(), 1, "clear");

        var after = simulator.Runtime.RuntimeDynamics;
        Assert.True(after.CommandEffects >= before.CommandEffects + 1);
        Assert.True(after.SubscriptionStarts >= before.SubscriptionStarts + 1);
        Assert.True(after.SubscriptionStops >= before.SubscriptionStops + 1);
        Assert.True(after.SubscriptionEffects >= before.SubscriptionEffects + 1);
        Assert.True(after.SubscriptionMessages >= before.SubscriptionMessages + 1);
        Assert.True(after.Reconciles >= before.Reconciles + 1);
    }

    [Fact]
    public async Task SessionQueueTelemetryTracksEnqueueProcessAndDrop()
    {
        var before = EffectSystem.SnapshotQueueTelemetry();
        var simulator = Ui.CreateSimulator<int, string>(32, 8);
        var session = simulator.CreateSession(new QueueProgram());

        session.Enqueue("seed");
        session.Enqueue("drop-me");
        session.ClearPending();
        session.Enqueue("seed");
        await session.DrainAsync();

        var after = simulator.Runtime.QueueTelemetry;
        Assert.True(after.Enqueued >= before.Enqueued + 3);
        Assert.True(after.Processed >= before.Processed + 1);
        Assert.True(after.Dropped >= before.Dropped + 2);
        Assert.True(after.HighWater >= before.HighWater);
    }

    [Fact]
    public void SessionQueueBackpressureDropsBeyondConfiguredMaxDepth()
    {
        var before = EffectSystem.SnapshotQueueTelemetry();
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default
                    .WithEnabled(true)
                    .WithMaxQueueDepth(2)
            });
        var session = simulator.CreateSession(new QueueProgram());

        session.Enqueue("one");
        session.Enqueue("two");
        session.Enqueue("three");

        var after = simulator.Runtime.QueueTelemetry;
        Assert.Equal(2, session.PendingCount);
        Assert.Equal(1, session.DroppedMessages);
        Assert.Equal(new[] { "one", "two" }, session.PendingMessages);
        Assert.True(after.Enqueued >= before.Enqueued + 2);
        Assert.True(after.Dropped >= before.Dropped + 1);
    }

    [Fact]
    public async Task EffectQueueSessionDrainsThroughSchedulerEvidenceOrder()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default
                    .WithEnabled(true)
                    .WithScheduler(RuntimeQueueSchedulerConfig.Default with { ForceFifo = true })
            });
        var session = simulator.CreateSession(new QueueProgram());

        session.Enqueue("seed");
        session.Enqueue("two");

        Assert.NotNull(session.SchedulerEvidence);
        var evidence = session.SchedulerEvidence!;
        Assert.Equal(RuntimeSelectionReason.Fifo, evidence.Reason);
        Assert.Equal(new[] { "seed", "two" }, session.PendingMessages);

        await session.DrainAsync(maxSteps: 1);
        Assert.Equal("queue=1", session.LastStep?.ScreenText.Trim());
        Assert.Equal(new[] { "two", "done" }, session.PendingMessages);
    }

    [Fact]
    public async Task EffectQueueSessionUsesPerMessageSchedulerMetadata()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default.WithEnabled(true)
            });
        var session = simulator.CreateSession(new QueueProgram());

        session.Enqueue("slow", new RuntimeQueueTaskSpec(
            Weight: 1,
            EstimatedMilliseconds: 100,
            WeightSource: RuntimeWeightSource.Explicit,
            EstimateSource: RuntimeEstimateSource.Explicit,
            Name: "slow"));
        session.Enqueue("seed", new RuntimeQueueTaskSpec(
            Weight: 1,
            EstimatedMilliseconds: 1,
            WeightSource: RuntimeWeightSource.Explicit,
            EstimateSource: RuntimeEstimateSource.Explicit,
            Name: "seed"));

        var evidence = session.SchedulerEvidence!;
        Assert.Equal("seed", evidence.Jobs[0].Name);
        Assert.Equal(RuntimeSelectionReason.ShortestRemaining, evidence.Reason);

        await session.DrainAsync(maxSteps: 1);
        Assert.Equal("queue=1", session.LastStep?.ScreenText.Trim());
        Assert.Equal(new[] { "done", "slow" }, session.PendingMessages);
    }

    [Fact]
    public async Task EffectQueueSessionUsesCommandQueueSpecs()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default.WithEnabled(true)
            });
        var session = simulator.CreateSession(new QueueProgram());
        var command = AppCommand<string>
            .Batch("command", "slow", "seed")
            .WithQueueSpec(0, new RuntimeQueueTaskSpec(
                Weight: 1,
                EstimatedMilliseconds: 100,
                WeightSource: RuntimeWeightSource.Explicit,
                EstimateSource: RuntimeEstimateSource.Explicit,
                Name: "slow"))
            .WithQueueSpec(1, new RuntimeQueueTaskSpec(
                Weight: 1,
                EstimatedMilliseconds: 1,
                WeightSource: RuntimeWeightSource.Explicit,
                EstimateSource: RuntimeEstimateSource.Explicit,
                Name: "seed"));

        session.EnqueueCommandMessages(command);

        Assert.Equal(new[] { "seed", "slow" }, session.PendingMessages);
        Assert.Equal("seed", session.SchedulerEvidence!.Jobs[0].Name);

        await session.DrainAsync(maxSteps: 1);
        Assert.Equal("queue=1", session.LastStep?.ScreenText.Trim());
    }

    [Fact]
    public async Task RuntimeDispatchPropagatesCommandQueueSpecsIntoSessionScheduler()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default.WithEnabled(true)
            });
        var session = simulator.CreateSession(new CommandQueueSpecProgram());

        await session.DispatchAsync("emit");

        Assert.Equal(new[] { "fast", "slow" }, session.PendingMessages);
        Assert.Equal("fast", session.SchedulerEvidence!.Jobs[0].Name);
        Assert.Equal(RuntimeSelectionReason.ShortestRemaining, session.SchedulerEvidence.Reason);
        Assert.Equal(2, session.LastStep!.EmittedQueueSpecs!.Count);
    }

    [Fact]
    public void AppCommandEmitQueuedCarriesSchedulerMetadata()
    {
        var spec = new RuntimeQueueTaskSpec(
            Weight: 2,
            EstimatedMilliseconds: 7,
            WeightSource: RuntimeWeightSource.Explicit,
            EstimateSource: RuntimeEstimateSource.Historical,
            Name: "historical");

        var command = AppCommand<string>.EmitQueued("message", spec, "command.task");

        Assert.Equal("command.task", command.EffectKind);
        Assert.Equal(new[] { "message" }, command.Messages);
        Assert.True(command.QueueSpecs!.TryGetValue(0, out var actual));
        Assert.Equal(spec, actual);
    }

    [Fact]
    public void SessionQueueBackpressureIsUnboundedByDefault()
    {
        var simulator = Ui.CreateSimulator<int, string>(32, 8);
        var session = simulator.CreateSession(new QueueProgram());

        session.Enqueue("one");
        session.Enqueue("two");
        session.Enqueue("three");

        Assert.Equal(3, session.PendingCount);
        Assert.Equal(0, session.DroppedMessages);
    }

    [Fact]
    public async Task RuntimeFrameStatsExposeEffectQueueDropsToConservativeGovernor()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default
                    .WithEnabled(true)
                    .WithMaxQueueDepth(1)
            });
        var session = simulator.CreateSession(new QueueProgram());

        await session.RenderCurrentAsync();
        session.Enqueue("kept");
        session.Enqueue("dropped");
        await session.RenderCurrentAsync();

        Assert.Equal(1, session.DroppedMessages);
        Assert.True(simulator.Runtime.FrameStats.RuntimeQueueDroppedDelta >= 1);
        Assert.Equal("hard_overload", simulator.Runtime.FrameStats.RuntimePressureClass);
        Assert.Equal("effect_queue_drop", simulator.Runtime.FrameStats.RuntimeGovernorReason);
    }

    [Fact]
    public async Task RuntimeFrameStatsExposeConfiguredEffectQueueMaxDepth()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EffectQueue = RuntimeEffectQueueConfig.Default
                    .WithEnabled(true)
                    .WithMaxQueueDepth(2)
            });

        await simulator.RenderAsync(new ParagraphWidget("queue max depth"));

        Assert.Equal(2, simulator.Runtime.FrameStats.RuntimeQueueMaxDepth);
    }

    [Fact]
    public void RuntimeQueueSchedulerConfigDefaultsMatchUpstreamSmithScheduler()
    {
        var config = RuntimeQueueSchedulerConfig.Default;

        Assert.Equal(RuntimeSchedulingMode.Smith, config.Mode);
        Assert.Equal("smith", config.Mode.Label());
        Assert.Equal(0.1, config.AgingFactor);
        Assert.Equal(0.05, config.MinProcessingMilliseconds);
        Assert.Equal(5_000.0, config.MaxProcessingMilliseconds);
        Assert.Equal(10.0, config.DefaultEstimateMilliseconds);
        Assert.Equal(1_000.0, config.UnknownEstimateMilliseconds);
        Assert.Equal(1e-6, config.MinWeight);
        Assert.Equal(100.0, config.MaxWeight);
        Assert.Equal(1.0, config.DefaultWeight);
        Assert.Equal(1.0, config.UnknownWeight);
        Assert.Equal(500.0, config.StarvationGuardMilliseconds);
        Assert.Equal(1.5, config.StarvationBoostRatio);
        Assert.Equal(10_000, config.MaxQueueSize);
        Assert.True(config.Preemptive);
        Assert.Equal(10.0, config.TimeQuantumMilliseconds);
        Assert.False(config.EnableLogging);
    }

    [Fact]
    public void RuntimeQueueSchedulerConfigModeFollowsFifoAndSrptOverrides()
    {
        Assert.Equal(RuntimeSchedulingMode.Fifo, (RuntimeQueueSchedulerConfig.Default with { ForceFifo = true }).Mode);
        Assert.Equal("fifo", (RuntimeQueueSchedulerConfig.Default with { ForceFifo = true }).Mode.Label());
        Assert.Equal(RuntimeSchedulingMode.Srpt, (RuntimeQueueSchedulerConfig.Default with { SmithEnabled = false }).Mode);
        Assert.Equal("srpt", (RuntimeQueueSchedulerConfig.Default with { SmithEnabled = false }).Mode.Label());
    }

    [Fact]
    public void RuntimeQueueSchedulerConfigNormalizesUnsafeBounds()
    {
        var normalized = new RuntimeQueueSchedulerConfig(
            AgingFactor: -1,
            MinProcessingMilliseconds: -5,
            MaxProcessingMilliseconds: 0,
            DefaultEstimateMilliseconds: -3,
            UnknownEstimateMilliseconds: 9_999,
            MinWeight: -1,
            MaxWeight: 0,
            DefaultWeight: -2,
            UnknownWeight: 999,
            StarvationGuardMilliseconds: -1,
            StarvationBoostRatio: 0,
            MaxQueueSize: 0,
            TimeQuantumMilliseconds: -10).Normalized();

        Assert.True(normalized.AgingFactor >= 0);
        Assert.True(normalized.MinProcessingMilliseconds > 0);
        Assert.True(normalized.MaxProcessingMilliseconds >= normalized.MinProcessingMilliseconds);
        Assert.InRange(normalized.DefaultEstimateMilliseconds, normalized.MinProcessingMilliseconds, normalized.MaxProcessingMilliseconds);
        Assert.InRange(normalized.UnknownEstimateMilliseconds, normalized.MinProcessingMilliseconds, normalized.MaxProcessingMilliseconds);
        Assert.True(normalized.MinWeight > 0);
        Assert.True(normalized.MaxWeight >= normalized.MinWeight);
        Assert.InRange(normalized.DefaultWeight, normalized.MinWeight, normalized.MaxWeight);
        Assert.InRange(normalized.UnknownWeight, normalized.MinWeight, normalized.MaxWeight);
        Assert.True(normalized.StarvationGuardMilliseconds >= 0);
        Assert.True(normalized.StarvationBoostRatio >= 1);
        Assert.True(normalized.MaxQueueSize >= 1);
        Assert.True(normalized.TimeQuantumMilliseconds > 0);
    }

    [Fact]
    public void RuntimeEffectQueueConfigKeepsEnabledFlagAndBackendInSync()
    {
        var enabled = RuntimeEffectQueueConfig.Default.WithEnabled(true);
        Assert.True(enabled.Enabled);
        Assert.Equal(RuntimeTaskExecutorBackend.EffectQueue, enabled.Backend);
        Assert.Equal(0, enabled.MaxQueueDepth);

        var spawned = enabled.WithBackend(RuntimeTaskExecutorBackend.Spawned);
        Assert.False(spawned.Enabled);
        Assert.Equal(RuntimeTaskExecutorBackend.Spawned, spawned.Backend);

        Assert.Equal(0, RuntimeEffectQueueConfig.Default.WithMaxQueueDepth(-3).MaxQueueDepth);
        Assert.Equal(RuntimeSchedulingMode.Smith, RuntimeEffectQueueConfig.Default.EffectiveScheduler.Mode);
        Assert.Equal(RuntimeSchedulingMode.Fifo, RuntimeEffectQueueConfig.Default
            .WithScheduler(RuntimeQueueSchedulerConfig.Default with { ForceFifo = true })
            .EffectiveScheduler
            .Mode);
    }

    [Fact]
    public void RuntimeQueueingSchedulerSelectsSmithWeightedPriorityAndEmitsEvidence()
    {
        var scheduler = new RuntimeQueueingScheduler(RuntimeQueueSchedulerConfig.Default with
        {
            AgingFactor = 0,
            MaxQueueSize = 10
        });

        var slowImportant = scheduler.SubmitNamed(weight: 10, estimatedTime: 100, "slow-important");
        var shortDefault = scheduler.SubmitNamed(weight: 1, estimatedTime: 5, "short-default");

        var evidence = scheduler.Evidence();
        Assert.Equal(shortDefault, evidence.SelectedJobId);
        Assert.Equal(RuntimeSelectionReason.ShortestRemaining, evidence.Reason);
        Assert.Equal(RuntimeTieBreakReason.EffectivePriority, evidence.TieBreakReason);
        Assert.Equal(2, evidence.QueueLength);
        Assert.Equal(shortDefault, evidence.Jobs[0].JobId);
        Assert.Equal("explicit", evidence.Jobs[0].EstimateSource.Label());
        Assert.Contains("\"event\":\"effect_queue_select\"", evidence.ToJsonl("effect_queue_select"));
        Assert.Contains("\"reason\":\"shortest_remaining\"", evidence.ToJsonl("effect_queue_select"));

        var completed = scheduler.Tick(5);
        Assert.Equal(new[] { shortDefault.GetValueOrDefault() }, completed);
        Assert.Equal(1UL, scheduler.Stats.TotalCompleted);
        Assert.Equal(2UL, scheduler.Stats.TotalSubmitted);
        Assert.True(scheduler.Stats.MeanResponseTime > 0);
    }

    [Fact]
    public void RuntimeQueueingSchedulerHonorsFifoAndMaxQueueSize()
    {
        var scheduler = new RuntimeQueueingScheduler(RuntimeQueueSchedulerConfig.Default with
        {
            ForceFifo = true,
            MaxQueueSize = 2
        });

        var first = scheduler.SubmitNamed(weight: 1, estimatedTime: 100, "first");
        scheduler.SubmitNamed(weight: 100, estimatedTime: 1, "second");
        var rejected = scheduler.SubmitNamed(weight: 100, estimatedTime: 1, "third");

        var evidence = scheduler.Evidence();
        Assert.Null(rejected);
        Assert.Equal(first, evidence.SelectedJobId);
        Assert.Equal(RuntimeSelectionReason.Fifo, evidence.Reason);
        Assert.Equal(RuntimeTieBreakReason.ArrivalSeq, evidence.TieBreakReason);
        Assert.Equal(1UL, scheduler.Stats.TotalRejected);
        Assert.Equal(2, scheduler.MaxQueueSize);
    }

    [Fact]
    public void RuntimeQueueingSchedulerPreemptsCurrentJobWhenShorterJobArrives()
    {
        var scheduler = new RuntimeQueueingScheduler(RuntimeQueueSchedulerConfig.Default with
        {
            SmithEnabled = false,
            AgingFactor = 0,
            Preemptive = true
        });

        var longJob = scheduler.SubmitNamed(weight: 1, estimatedTime: 100, "long");
        Assert.Equal(longJob, scheduler.PeekNext()?.Id);
        Assert.Empty(scheduler.Tick(10));
        Assert.Equal(RuntimeSelectionReason.Continuation, scheduler.Evidence().Reason);

        var shortJob = scheduler.SubmitNamed(weight: 1, estimatedTime: 5, "short");
        var evidence = scheduler.Evidence();

        Assert.Equal(shortJob, evidence.SelectedJobId);
        Assert.Equal(RuntimeTieBreakReason.EffectivePriority, evidence.TieBreakReason);
        Assert.Equal(1UL, scheduler.Stats.TotalPreemptions);

        var completed = scheduler.Tick(5);
        Assert.Equal(new[] { shortJob.GetValueOrDefault() }, completed);
        Assert.Equal(longJob, scheduler.PeekNext()?.Id);
    }

    [Fact]
    public void RuntimeQueueingSchedulerCancelClearAndResetMirrorUpstreamLifecycle()
    {
        var scheduler = new RuntimeQueueingScheduler(RuntimeQueueSchedulerConfig.Default);
        var first = scheduler.SubmitNamed(weight: 1, estimatedTime: 10, "first");
        var second = scheduler.SubmitNamed(weight: 1, estimatedTime: 20, "second");

        Assert.True(scheduler.Cancel(second!.Value));
        Assert.False(scheduler.Cancel(999));
        Assert.Equal(1, scheduler.QueueLength);
        Assert.Equal(first, scheduler.PeekNext()?.Id);

        scheduler.Clear();
        Assert.Equal(0, scheduler.QueueLength);
        Assert.Equal(RuntimeSelectionReason.QueueEmpty, scheduler.Evidence().Reason);

        scheduler.SubmitNamed(weight: 1, estimatedTime: 10, "after-clear");
        scheduler.Tick(10);
        Assert.Equal(1UL, scheduler.Stats.TotalCompleted);

        scheduler.Reset();
        Assert.Equal(0UL, scheduler.Stats.TotalSubmitted);
        Assert.Equal(0, scheduler.QueueLength);
        Assert.Equal(1UL, scheduler.SubmitNamed(weight: 1, estimatedTime: 10, "after-reset"));
    }

    [Fact]
    public void RuntimeQueueingSchedulerUsesFallbackEstimateAndWeightSources()
    {
        var config = RuntimeQueueSchedulerConfig.Default with
        {
            DefaultEstimateMilliseconds = 42,
            UnknownWeight = 3,
            AgingFactor = 0
        };
        var scheduler = new RuntimeQueueingScheduler(config);

        var id = scheduler.SubmitWithSources(
            weight: double.NaN,
            estimatedTime: double.NaN,
            RuntimeWeightSource.Unknown,
            RuntimeEstimateSource.Default,
            "fallback");

        var job = Assert.Single(scheduler.Evidence().Jobs);
        Assert.Equal(id, job.JobId);
        Assert.Equal(42, job.EstimateMilliseconds);
        Assert.Equal(3, job.Weight);
        Assert.Equal(RuntimeEstimateSource.Default, job.EstimateSource);
        Assert.Equal(RuntimeWeightSource.Unknown, job.WeightSource);
    }

    [Fact]
    public async Task EffectQueueSchedulerSelectionTelemetryEmitsEvidenceFields()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EmitTelemetry = true,
                Telemetry = TelemetryConfig.FromEnvironment(
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
                    }),
                EffectQueue = RuntimeEffectQueueConfig.Default
                    .WithEnabled(true)
                    .WithScheduler(RuntimeQueueSchedulerConfig.Default with { ForceFifo = true })
            });
        var session = simulator.CreateSession(new QueueProgram());
        session.Enqueue("seed");
        session.Enqueue("two");

        await session.DrainAsync(maxSteps: 1);

        var item = Assert.Single(
            simulator.Runtime.Telemetry.Events,
            static item => item.Name == "ftui.effect.queue.select");
        Assert.Contains(item.Fields, static field => field.Key == "reason" && field.Value == "fifo");
        Assert.Contains(item.Fields, static field => field.Key == "queue_length" && field.Value == "2");
        Assert.Contains(item.Fields, static field => field.Key == "selected_job_id" && field.Value == "1");
        Assert.Contains(item.Fields, static field => field.Key == "job_count" && field.Value == "2");
    }

    [Fact]
    public async Task TelemetryIncludesEffectCountersWhenEnabled()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            32,
            8,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EmitTelemetry = true,
                Telemetry = TelemetryConfig.FromEnvironment(
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
                    })
            });

        await simulator.DispatchAsync(new EffectProgram(), 0, "emit");

        Assert.Contains(
            simulator.Runtime.Telemetry.Events,
            static item => item.Name == "ftui.program.subscriptions");
    }

    [Fact]
    public void CommandEffectTracksCancellationAndFailure()
    {
        var before = EffectSystem.SnapshotRuntimeDynamics();

        Assert.Throws<OperationCanceledException>(() =>
            EffectSystem.TraceCommandEffect<int>("command.cancel", static () => throw new OperationCanceledException()));
        Assert.Throws<InvalidOperationException>(() =>
            EffectSystem.TraceCommandEffect<int>("command.fail", static () => throw new InvalidOperationException("boom")));

        var after = EffectSystem.SnapshotRuntimeDynamics();
        Assert.True(after.CommandEffects >= before.CommandEffects + 2);
        Assert.True(after.CommandCancellations >= before.CommandCancellations + 1);
        Assert.True(after.CommandFailures >= before.CommandFailures + 1);
    }

    [Fact]
    public void SubscriptionEffectTracksCancellationAndFailure()
    {
        var before = EffectSystem.SnapshotRuntimeDynamics();

        Assert.Throws<OperationCanceledException>(() =>
            EffectSystem.TraceSubscriptionEffect(new Subscription<string>(
                "cancel",
                static () => throw new OperationCanceledException(),
                "subscription.cancel")));
        Assert.Throws<InvalidOperationException>(() =>
            EffectSystem.TraceSubscriptionEffect(new Subscription<string>(
                "fail",
                static () => throw new InvalidOperationException("boom"),
                "subscription.fail")));

        var after = EffectSystem.SnapshotRuntimeDynamics();
        Assert.True(after.SubscriptionEffects >= before.SubscriptionEffects + 2);
        Assert.True(after.SubscriptionCancellations >= before.SubscriptionCancellations + 1);
        Assert.True(after.SubscriptionFailures >= before.SubscriptionFailures + 1);
    }

    private sealed class EffectProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            message switch
            {
                "emit" => new UpdateResult<int, string>(
                    model + 1,
                    AppCommand<string>.Emit("follow-up", "command.emit"),
                    [new Subscription<string>("pulse", static () => ["tick"], "subscription.pulse")]),
                "clear" => UpdateResult<int, string>.FromModel(model + 1),
                "follow-up" => UpdateResult<int, string>.FromModel(model + 1),
                "tick" => UpdateResult<int, string>.FromModel(model + 1),
                _ => UpdateResult<int, string>.FromModel(model)
            };

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"effects={model}");
    }

    private sealed class QueueProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            message == "seed"
                ? new UpdateResult<int, string>(model + 1, AppCommand<string>.Emit("done"), [])
                : UpdateResult<int, string>.FromModel(model + 1);

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"queue={model}");
    }

    private sealed class CommandQueueSpecProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message)
        {
            if (message != "emit")
            {
                return UpdateResult<int, string>.FromModel(model + 1);
            }

            var command = AppCommand<string>
                .Batch("command.task", "slow", "fast")
                .WithQueueSpec(0, new RuntimeQueueTaskSpec(
                    Weight: 1,
                    EstimatedMilliseconds: 100,
                    WeightSource: RuntimeWeightSource.Explicit,
                    EstimateSource: RuntimeEstimateSource.Explicit,
                    Name: "slow"))
                .WithQueueSpec(1, new RuntimeQueueTaskSpec(
                    Weight: 1,
                    EstimatedMilliseconds: 1,
                    WeightSource: RuntimeWeightSource.Explicit,
                    EstimateSource: RuntimeEstimateSource.Explicit,
                    Name: "fast"));
            return new UpdateResult<int, string>(model + 1, command, []);
        }

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"command-queue={model}");
    }
}
