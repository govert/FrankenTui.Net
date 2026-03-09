using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public sealed class RuntimeReplayTests
{
    [Fact]
    public async Task RuntimeTraceAndReplayRemainDeterministicAcrossIdenticalRuns()
    {
        var first = Ui.CreateSimulator<int, string>(48, 12, theme: null, policy: RuntimeExecutionPolicy.Default);
        var second = Ui.CreateSimulator<int, string>(48, 12, theme: null, policy: RuntimeExecutionPolicy.Default);
        var program = new ReplayProgram();

        await first.DispatchAsync(program, 0, "inc");
        await first.DispatchAsync(program, 1, "emit");

        await second.DispatchAsync(program, 0, "inc");
        await second.DispatchAsync(program, 1, "emit");

        Assert.Equal(2, first.Trace.Entries.Count);
        Assert.Equal(first.Trace.Fingerprint, second.Trace.Fingerprint);
        Assert.Equal(first.Replay.Fingerprint, second.Replay.Fingerprint);
        Assert.True(first.Replay.IsDeterministicMatch(second.Replay));

        var json = first.Replay.ToJson();
        var restored = ReplayTape<string>.FromJson(json);
        Assert.True(first.Replay.IsDeterministicMatch(restored));
        Assert.Equal(first.Trace.ToReplayTape().Fingerprint, first.Replay.Fingerprint);
        Assert.Contains("\"StepIndex\": 1", json);
    }

    [Fact]
    public async Task RuntimeExecutionPolicyCanDisableTraceAndReplayCapture()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                CaptureTrace = false,
                CaptureReplayTape = false
            });

        var result = await simulator.DispatchAsync(new ReplayProgram(), 0, "emit");

        Assert.Null(result.TraceEntry);
        Assert.Empty(simulator.Trace.Entries);
        Assert.Empty(simulator.Replay.Entries);
        Assert.Contains("Value: 0", result.ScreenText);
    }

    private sealed class ReplayProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            message switch
            {
                "inc" => UpdateResult<int, string>.FromModel(model + 1),
                "emit" => new UpdateResult<int, string>(
                    model,
                    AppCommand<string>.Emit("echo"),
                    [new Subscription<string>("pulse", static () => ["tick"])]),
                _ => UpdateResult<int, string>.FromModel(model)
            };

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"Value: {model}");
    }
}
