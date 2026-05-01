using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Text;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class RuntimeLayoutTextDepthTests
{
    [Fact]
    public void LayoutCacheProducesStableTraceMetadata()
    {
        var cache = new LayoutCache();
        var first = LayoutSolver.SplitWithTrace(
            new Rect(0, 0, 60, 8),
            LayoutDirection.Horizontal,
            [LayoutConstraint.Fixed(6), LayoutConstraint.Fill(), LayoutConstraint.Percentage(25)],
            cache);
        var second = LayoutSolver.SplitWithTrace(
            new Rect(0, 0, 60, 8),
            LayoutDirection.Horizontal,
            [LayoutConstraint.Fixed(6), LayoutConstraint.Fill(), LayoutConstraint.Percentage(25)],
            cache);

        Assert.False(first.CacheHit);
        Assert.True(second.CacheHit);
        Assert.Equal(60, first.TotalLength);
        Assert.Equal(39, first.RemainingLength);
        Assert.Equal(first.Result, second.Result);
    }

    [Fact]
    public void TextSegmentationAndNormalizedSearchHandleMixedScripts()
    {
        var document = TextDocument.FromString("Cafe\u0301 שלום 東京");
        var matches = TextSearch.FindAllNormalized(document, "café");
        var segments = TextSegmenter.Segment(document.PlainText);

        Assert.Single(matches);
        Assert.Contains(segments, static segment => segment.Direction == TextDirection.RightToLeft && segment.Script == TextScript.ArabicHebrew);
        Assert.Contains(segments, static segment => segment.Script == TextScript.Cjk);
    }

    [Fact]
    public void ParagraphWidgetUsesMarkupStylesDuringRendering()
    {
        var buffer = new RenderBuffer(40, 4);
        var widget = new ParagraphWidget(string.Empty)
        {
            Document = TextDocument.FromMarkup("hello **bold** _italic_ `code`")
        };

        widget.Render(new RuntimeRenderContext(buffer, Rect.FromSize(40, 4), Ui.Theme));

        var boldCell = FindFirst(buffer, 'b');
        var italicCell = FindFirst(buffer, 'i');

        Assert.NotNull(boldCell);
        Assert.NotNull(italicCell);
        Assert.Equal(FrankenTui.Style.UiStyle.Accent.Foreground, boldCell.Value.Foreground);
        Assert.True(italicCell.Value.Attributes.HasFlag(CellStyleFlags.Italic));
    }

    [Fact]
    public void TextRendererViewportMatchesFullLayoutVisibleSlice()
    {
        var document = new TextDocument(
        [
            new TextLine([new TextSpan("alpha beta gamma delta", FrankenTui.Style.UiStyle.Accent)]),
            new TextLine([new TextSpan("tail", FrankenTui.Style.UiStyle.Warning)])
        ]);
        var options = new TextRenderOptions(TextWrapMode.Word);

        var full = TextRenderer.Layout(document, 8, options);
        var viewport = TextRenderer.LayoutViewport(document, 8, firstVisualLine: 1, maxVisualLines: 2, options);

        Assert.Equal(full.Skip(1).Take(2).Select(static line => line.PlainText), viewport.Select(static line => line.PlainText));
        Assert.All(viewport.SelectMany(static line => line.Runs), static run => Assert.NotNull(run.Style));
        Assert.Equal(FrankenTui.Style.UiStyle.Accent, viewport[0].Runs[0].Style);
    }

    [Fact]
    public void ParagraphWidgetRendersRequestedVisualViewport()
    {
        var buffer = new RenderBuffer(12, 2);
        var widget = new ParagraphWidget("alpha beta gamma delta tail")
        {
            RenderOptions = new TextRenderOptions(TextWrapMode.Word, FirstVisualLine: 1, MaxVisualLines: 2)
        };

        widget.Render(new RuntimeRenderContext(buffer, Rect.FromSize(12, 2), Ui.Theme));

        Assert.Equal("gamma delta", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("tail", HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public async Task AppSessionDrainsQueuedMessagesAndHandlesResize()
    {
        var simulator = Ui.CreateSimulator<int, string>(32, 10);
        var program = new QueueProgram();
        var session = simulator.CreateSession(program);

        session.Enqueue("seed");
        var batch = await session.DrainAsync();

        Assert.False(batch.QueueRemaining);
        Assert.Equal(3, batch.Steps.Count);
        Assert.Equal(3, session.Model);

        await session.ResizeAsync(new Size(48, 12), static size => $"resize:{size.Width}x{size.Height}");

        Assert.Equal(new Size(48, 12), simulator.Runtime.Size);
        Assert.Contains("Value: 4", session.LastStep?.ScreenText);
    }

    private static Cell? FindFirst(RenderBuffer buffer, char value)
    {
        for (ushort row = 0; row < buffer.Height; row++)
        {
            foreach (var cell in buffer.GetRow(row))
            {
                if (cell.Content.AsRune()?.ToString() == value.ToString())
                {
                    return cell;
                }
            }
        }

        return null;
    }

    private sealed class QueueProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            message switch
            {
                "seed" => new UpdateResult<int, string>(
                    model + 1,
                    AppCommand<string>.Emit("follow-up"),
                    [new Subscription<string>("tail", static () => ["final"])]),
                "follow-up" => UpdateResult<int, string>.FromModel(model + 1),
                "final" => UpdateResult<int, string>.FromModel(model + 1),
                _ when message.StartsWith("resize:", StringComparison.Ordinal) => UpdateResult<int, string>.FromModel(model + 1),
                _ => UpdateResult<int, string>.FromModel(model)
            };

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"Value: {model}");
    }
}
