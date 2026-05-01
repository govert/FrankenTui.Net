using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class PresenterTests
{
    [Fact]
    public void PresenterOutputRoundTripsThroughTerminalModel()
    {
        var current = new RenderBuffer(5, 2);
        var next = new RenderBuffer(5, 2);
        next.Set(0, 0, Cell.FromChar('H'));
        next.Set(1, 0, Cell.FromChar('i'));
        next.Set(0, 1, Cell.FromRune(new Rune(0x1F600)));

        var diff = BufferDiff.Compute(current, next);
        var presenter = new Presenter(TerminalCapabilities.Modern());
        var result = presenter.Present(next, diff);
        var model = new TerminalModel(5, 2);
        model.Process(result.Output);

        Assert.False(result.Truncated);
        Assert.True(result.UsedSyncOutput);
        Assert.Equal($"Hi{Environment.NewLine}😀", model.ScreenString());
        Assert.Equal(0, model.SyncOutputDepth);
    }

    [Fact]
    public void PresenterEmitsHyperlinksWhenCapabilityAllows()
    {
        var buffer = new RenderBuffer(2, 1);
        var linkedCell = Cell.FromChar('A')
            .WithAttributes(new CellAttributes(CellStyleFlags.Underline, 1));
        buffer.Set(0, 0, linkedCell);
        buffer.Set(1, 0, Cell.FromChar('B'));

        var presenter = new Presenter(TerminalCapabilities.Modern());
        var diff = BufferDiff.Full(buffer.Width, buffer.Height);
        var result = presenter.Present(buffer, diff, new Dictionary<uint, string>
        {
            [1] = "https://example.test"
        });
        var model = new TerminalModel(2, 1);
        model.Process(result.Output);

        Assert.Contains("\u001b]8;;https://example.test\u001b\\", result.Output);
        Assert.Contains("\u001b]8;;\u001b\\", result.Output);
        Assert.Equal("AB", model.ScreenString());
        Assert.Equal("https://example.test", model.LinkUrl(model.Cell(0, 0)!.Value.Attributes.LinkId));
    }

    [Fact]
    public void PresenterSuppressesHyperlinksInsideMux()
    {
        var buffer = new RenderBuffer(1, 1);
        buffer.Set(0, 0, Cell.FromChar('X').WithAttributes(new CellAttributes(CellStyleFlags.Underline, 1)));

        var presenter = new Presenter(TerminalCapabilities.Tmux());
        var diff = BufferDiff.Full(buffer.Width, buffer.Height);
        var result = presenter.Present(buffer, diff, new Dictionary<uint, string>
        {
            [1] = "https://example.test"
        });

        Assert.DoesNotContain("\u001b]8;;", result.Output);
    }

    [Fact]
    public void PresenterHonorsFrameBudgetAndClosesState()
    {
        var buffer = new RenderBuffer(8, 1);
        var styled = Cell.FromChar('A')
            .WithForeground(PackedRgba.Red)
            .WithAttributes(new CellAttributes(CellStyleFlags.Bold, 1));
        for (ushort column = 0; column < buffer.Width; column++)
        {
            buffer.Set(column, 0, styled);
        }

        var presenter = new Presenter(TerminalCapabilities.Modern())
        {
            FrameByteBudget = 40
        };

        var result = presenter.Present(buffer, BufferDiff.Full(buffer.Width, buffer.Height), new Dictionary<uint, string>
        {
            [1] = "https://example.test"
        });

        Assert.True(result.Truncated);
        Assert.EndsWith("\u001b[?2026l", result.Output, StringComparison.Ordinal);

        if (result.Output.Contains("\u001b]8;;https://example.test\u001b\\", StringComparison.Ordinal))
        {
            Assert.Contains("\u001b]8;;\u001b\\", result.Output, StringComparison.Ordinal);
        }

        if (result.Output.Contains("\u001b[38;2;", StringComparison.Ordinal) ||
            result.Output.Contains("\u001b[1m", StringComparison.Ordinal))
        {
            Assert.Contains("\u001b[0m", result.Output, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PresenterClearsWideGlyphTailWhenReplacingWithNarrowCell()
    {
        var initial = new RenderBuffer(5, 1);
        initial.Set(0, 0, Cell.FromRune(new Rune(0x1F600)));

        var next = new RenderBuffer(5, 1);
        next.Set(0, 0, Cell.FromChar('A'));

        var presenter = new Presenter(TerminalCapabilities.Modern());
        var model = new TerminalModel(5, 1);

        var initialResult = presenter.Present(initial, BufferDiff.Full(initial.Width, initial.Height));
        model.Process(initialResult.Output);

        var nextResult = presenter.Present(next, BufferDiff.Compute(initial, next));
        model.Process(nextResult.Output);

        Assert.Equal("A", model.ScreenString());
    }

    [Fact]
    public void PresenterUsesWidthMatchedPlaceholdersForGraphemeFallback()
    {
        var buffer = new RenderBuffer(5, 1);
        var grapheme = new Cell(
            CellContent.FromGrapheme(new GraphemeId(1, 0, 2)),
            PackedRgba.White,
            PackedRgba.Transparent,
            CellAttributes.None);
        buffer.Set(0, 0, grapheme);
        buffer.Set(2, 0, Cell.FromChar('A'));

        var presenter = new Presenter(TerminalCapabilities.Modern());
        var result = presenter.Present(buffer, BufferDiff.Full(buffer.Width, buffer.Height));
        var model = new TerminalModel(5, 1);
        model.Process(result.Output);

        Assert.Equal("??A", model.ScreenString());
    }

    [Fact]
    public void PresenterRendersResolvedGraphemeTextWithoutFallbackPlaceholders()
    {
        var buffer = new RenderBuffer(5, 1);
        buffer.SetText(0, 0, "e\u0301", Cell.FromChar('x'));
        buffer.Set(1, 0, Cell.FromChar('A'));

        var presenter = new Presenter(TerminalCapabilities.Modern());
        var result = presenter.Present(buffer, BufferDiff.Full(buffer.Width, buffer.Height));
        var model = new TerminalModel(5, 1);
        model.Process(result.Output);

        Assert.Equal("e\u0301A", model.ScreenString());
        Assert.Contains("e\u0301A", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("??", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void PresenterReplacesStandaloneZeroWidthRuneToPreserveGridAlignment()
    {
        var buffer = new RenderBuffer(5, 1);
        buffer.SetRaw(0, 0, Cell.FromRune(new Rune(0x0301)));
        buffer.Set(1, 0, Cell.FromChar('A'));

        var presenter = new Presenter(TerminalCapabilities.Modern());
        var result = presenter.Present(buffer, BufferDiff.Full(buffer.Width, buffer.Height));
        var model = new TerminalModel(5, 1);
        model.Process(result.Output);

        Assert.Equal("\uFFFDA", model.ScreenString());
    }
}
