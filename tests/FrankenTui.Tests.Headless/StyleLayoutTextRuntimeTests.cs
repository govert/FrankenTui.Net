using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Testing.Harness;
using FrankenTui.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public sealed class StyleLayoutTextRuntimeTests
{
    [Fact]
    public void LayoutSolverConservesWidthAcrossSamples()
    {
        for (ushort width = 6; width < 40; width++)
        {
            var rects = LayoutSolver.Split(
                new Rect(0, 0, width, 4),
                LayoutDirection.Horizontal,
                [LayoutConstraint.Fixed(2), LayoutConstraint.Fill(), LayoutConstraint.Percentage(25)]);

            Assert.Equal(width, rects.Sum(static rect => rect.Width));
        }
    }

    [Fact]
    public void TextWrapperNeverExceedsRequestedWidth()
    {
        var document = TextDocument.FromString("alpha beta gamma delta epsilon");
        foreach (var line in TextWrapper.Wrap(document, 8, TextWrapMode.Word))
        {
            Assert.True(TerminalTextWidth.DisplayWidth(line) <= 8, line);
        }
    }

    [Fact]
    public void MarkupParserProducesStyledSpans()
    {
        var line = MarkupParser.ParseInline("hello **bold** _italic_");

        Assert.Contains(line.Spans, static span => span.Text == "bold" && span.Style == FrankenTui.Style.UiStyle.Accent);
        Assert.Contains(line.Spans, static span => span.Text == "italic");
    }

    [Fact]
    public async Task RuntimeProducesDeterministicScreensForSameMessages()
    {
        var simulator1 = Ui.CreateSimulator<int, string>(40, 12);
        var simulator2 = Ui.CreateSimulator<int, string>(40, 12);
        var program = new CounterProgram();

        var first = await simulator1.DispatchAsync(program, 0, "inc");
        var second = await simulator2.DispatchAsync(program, 0, "inc");

        Assert.Equal(first.ScreenText, second.ScreenText);
        Assert.Contains("Counter", first.ScreenText);
    }

    [Fact]
    public void DashboardWidgetRendersMeaningfulSnapshot()
    {
        MarkdownDocumentBuilder.ClearCaches();
        var snapshot = RenderHarness.Render(DashboardSurface.CreateDefault("Main", ["One", "Two", "Three"]), 50, 12);

        Assert.Contains("Main", snapshot.Text);
        Assert.Contains("One", snapshot.Text);
        Assert.Contains("Port baseline", snapshot.Text);
        Assert.Contains("Dashboard", snapshot.Text);
        Assert.True(MarkdownDocumentBuilder.CachedDocumentCount >= 1);
    }

    private sealed class CounterProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(message == "inc" ? model + 1 : model);

        public IRuntimeView BuildView(int model) =>
            Ui.Panel("Counter", new ParagraphWidget($"Value: {model}"));
    }
}
