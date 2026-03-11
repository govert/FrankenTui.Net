using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class MermaidContractTests
{
    [Fact]
    public void MermaidConfigParsesEnvironmentAndValidatesIncompatibleLinks()
    {
        var config = MermaidConfig.FromEnvironment(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["FTUI_MERMAID_GLYPH_MODE"] = "ascii",
                ["FTUI_MERMAID_TIER"] = "compact",
                ["FTUI_MERMAID_ENABLE_LINKS"] = "false",
                ["FTUI_MERMAID_LINK_MODE"] = "inline"
            });

        var issues = config.Validate();

        Assert.Equal(MermaidGlyphMode.Ascii, config.GlyphMode);
        Assert.Equal(MermaidTier.Compact, config.TierOverride);
        Assert.Contains(issues, static issue => issue.Field == nameof(MermaidConfig.LinkMode));
    }

    [Fact]
    public void MermaidShowcaseBuildsDeterministicWidgetAndStatusLog()
    {
        var session = HostedParitySession.Create(false, HostedParityScenarioId.Extras) with
        {
            SelectedMetricIndex = 1
        };
        var state = MermaidShowcaseSurface.BuildState(session, 72, 18);
        var buffer = new RenderBuffer(72, 18);

        MermaidShowcaseSurface.CreateWidget(state)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(72, 18), Theme.DefaultTheme));
        var screen = HeadlessBufferView.ScreenString(buffer);

        Assert.Contains("Mermaid Showcase", screen);
        Assert.Contains(state.Sample.Name, screen);
        Assert.Contains("Viewport", screen);
        Assert.Contains(state.StatusLog, static entry => entry.Event == "render_start");
        Assert.Contains(state.StatusLog, static entry => entry.Event == "render_done");
    }
}
