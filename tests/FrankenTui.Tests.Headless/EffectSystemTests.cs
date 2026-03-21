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
}
