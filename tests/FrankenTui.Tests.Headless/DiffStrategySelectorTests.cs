using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using FrankenTui;

namespace FrankenTui.Tests.Headless;

public sealed class DiffStrategySelectorTests
{
    [Fact]
    public void ResizeFrameSelectsFullRedrawAndRecordsTransition()
    {
        var selector = new DiffStrategySelector();

        var selection = selector.Select(20, 4, dirtyRows: 4, resized: true, lastWriteLatency: TimeSpan.Zero);
        selector.Observe(selection, changedCells: 80, writeLatency: TimeSpan.FromMilliseconds(1));

        Assert.Equal(DiffRegime.ResizeRegime, selection.Regime);
        Assert.Equal(DiffStrategy.FullRedraw, selection.Strategy);
        Assert.Contains(selector.Ledger.Transitions, transition => transition.To == DiffRegime.ResizeRegime);
        Assert.Contains(selector.Ledger.Decisions, decision => decision.Strategy == DiffStrategy.FullRedraw);
    }

    [Fact]
    public void HighDirtyFractionEntersBurstyRegimeAndReturnsToStable()
    {
        var selector = new DiffStrategySelector();

        var burst = selector.Select(20, 10, dirtyRows: 8, resized: false, lastWriteLatency: TimeSpan.Zero);
        selector.Observe(burst, changedCells: 160, writeLatency: TimeSpan.FromMilliseconds(2));

        Assert.Equal(DiffRegime.BurstyChange, burst.Regime);
        Assert.Equal(DiffStrategy.Full, burst.Strategy);

        for (var index = 0; index < 4; index++)
        {
            var stableProbe = selector.Select(20, 10, dirtyRows: 0, resized: false, lastWriteLatency: TimeSpan.FromMilliseconds(2));
            selector.Observe(stableProbe, changedCells: 0, writeLatency: TimeSpan.FromMilliseconds(2));
        }

        var recovered = selector.Select(20, 10, dirtyRows: 0, resized: false, lastWriteLatency: TimeSpan.FromMilliseconds(2));

        Assert.Equal(DiffRegime.StableFrame, recovered.Regime);
        Assert.Equal(DiffStrategy.DirtyRows, recovered.Strategy);
    }

    [Fact]
    public void SlowWritesEnterDegradedModeAndRecoverWhenLatencyDrops()
    {
        var selector = new DiffStrategySelector();

        var degraded = selector.Select(40, 10, dirtyRows: 1, resized: false, lastWriteLatency: TimeSpan.FromMilliseconds(12));
        selector.Observe(degraded, changedCells: 1, writeLatency: TimeSpan.FromMilliseconds(12));

        Assert.Equal(DiffRegime.DegradedTerminal, degraded.Regime);
        Assert.Equal(DiffStrategy.SignificantDirtyRows, degraded.Strategy);

        var recovered = selector.Select(40, 10, dirtyRows: 1, resized: false, lastWriteLatency: TimeSpan.FromMilliseconds(2));

        Assert.Equal(DiffRegime.StableFrame, recovered.Regime);
        Assert.Equal(DiffStrategy.DirtyRows, recovered.Strategy);
    }

    [Fact]
    public async Task AppRuntimePublishesDiffEvidenceAcrossDispatchesAndResize()
    {
        var simulator = Ui.CreateSimulator<int, string>(16, 2);
        var program = new CounterProgram();

        await simulator.DispatchAsync(program, 0, "inc");
        await simulator.Runtime.ResizeAsync(new Size(20, 3));
        await simulator.DispatchAsync(program, 1, "resize:20x3");
        await simulator.DispatchAsync(program, 1, "tick");

        Assert.NotEmpty(simulator.Runtime.DiffEvidence.Decisions);
        Assert.Contains(simulator.Runtime.DiffEvidence.Transitions, transition => transition.To == DiffRegime.ResizeRegime);
        Assert.True(simulator.Runtime.LastPresentLatency >= TimeSpan.Zero);
    }

    private sealed class CounterProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            message switch
            {
                "inc" => UpdateResult<int, string>.FromModel(model + 1),
                "tick" => UpdateResult<int, string>.FromModel(model + 1),
                _ when message.StartsWith("resize:", StringComparison.Ordinal) => UpdateResult<int, string>.FromModel(model),
                _ => UpdateResult<int, string>.FromModel(model)
            };

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"count={model}");
    }
}
