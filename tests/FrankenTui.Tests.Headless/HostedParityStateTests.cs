using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Testing.Harness;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public sealed class HostedParityStateTests
{
    [Fact]
    public void WidgetInputStateCyclesFocusAndCapturesPasteAnnouncements()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("tabs");

        state = state.Apply(TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)));
        state = state.Apply(TerminalEvent.Paste("Hosted parity evidence"));

        Assert.Equal("modules", state.EffectiveFocusId);
        Assert.Contains("Paste", state.LiveRegionText);
    }

    [Fact]
    public void HostedParitySessionProducesMeaningfulDescriptionAndSnapshot()
    {
        var session = HostedParitySession.ForFrame(
            inlineMode: false,
            frame: 2,
            scenarioId: HostedParityScenarioId.Interaction,
            language: "de-DE",
            flowDirection: WidgetFlowDirection.RightToLeft);
        var description = HostedParitySurface.Describe(session);
        var snapshot = RenderHarness.Render(HostedParitySurface.Create(session), 72, 18);

        Assert.Equal("de-DE", description.Language);
        Assert.Equal("rtl", description.Direction);
        Assert.Contains("312-WGT", description.WorkstreamCodes);
        Assert.NotEmpty(description.Accessibility.Nodes);
        Assert.Contains("Interaktion", snapshot.Text);
        Assert.Contains("Focus", snapshot.Text);
    }
}
