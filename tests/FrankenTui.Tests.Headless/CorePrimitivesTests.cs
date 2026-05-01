using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class CorePrimitivesTests
{
    [Fact]
    public void SizeClampAndAreaFollowExpectedSemantics()
    {
        var size = new Size(10, 20);

        Assert.False(size.IsEmpty);
        Assert.Equal((uint)200, size.Area);
        Assert.Equal(new Size(8, 20), size.ClampMax(new Size(8, 40)));
        Assert.Equal(new Size(10, 25), size.ClampMin(new Size(8, 25)));
    }

    [Fact]
    public void RectContainsAndIntersectionMatchUpstreamSemantics()
    {
        var rect = new Rect(2, 3, 4, 5);

        Assert.True(rect.Contains(2, 3));
        Assert.True(rect.Contains(5, 7));
        Assert.False(rect.Contains(6, 3));
        Assert.False(rect.Contains(2, 8));
        Assert.Equal(new Rect(3, 4, 3, 4), rect.Intersection(new Rect(3, 4, 10, 10)));
        Assert.Equal(default, rect.Intersection(new Rect(20, 20, 1, 1)));
    }

    [Fact]
    public void RectInnerAndSidesConstructorsBehavePredictably()
    {
        var rect = new Rect(0, 0, 10, 10);
        var inner = rect.Inner(new Sides(1, 2, 3, 4));

        Assert.Equal(new Rect(4, 1, 4, 6), inner);
        Assert.Equal(Sides.All(3), (Sides)3);
        Assert.Equal(new Sides(0, 2, 0, 2), Sides.Horizontal(2));
        Assert.Equal(new Sides(4, 0, 4, 0), Sides.Vertical(4));
        Assert.Equal((ushort)7, new Sides(1, 2, 3, 5).HorizontalSum);
        Assert.Equal((ushort)4, new Sides(1, 2, 3, 5).VerticalSum);
    }

    [Fact]
    public void CursorManagerDetectsScreenAndRestoresEmulatedPositions()
    {
        var ansiManager = CursorManager.Detect(new TerminalCapabilities(InScreen: true));
        Assert.Equal(CursorSaveStrategy.Ansi, ansiManager.Strategy);

        var emulated = new CursorManager(CursorSaveStrategy.Emulated);
        using var stream = new MemoryStream();
        emulated.Save(stream, 10, 5);
        emulated.Restore(stream);

        Assert.True(emulated.SavedPosition.HasValue);
        Assert.Equal((ushort)10, emulated.SavedPosition.Value.Column);
        Assert.Equal((ushort)5, emulated.SavedPosition.Value.Row);
        Assert.Equal("\u001b[6;11H", Encoding.ASCII.GetString(stream.ToArray()));
    }

    [Theory]
    [InlineData('A', 1)]
    [InlineData('\t', 1)]
    [InlineData('\n', 1)]
    [InlineData('\u0301', 0)]
    public void TerminalTextWidthHandlesCommonScalarCases(char value, int expectedWidth)
    {
        Assert.Equal(expectedWidth, TerminalTextWidth.CharWidth(value));
    }

    [Fact]
    public void TerminalTextWidthTreatsEmojiAsWide()
    {
        Assert.Equal(2, TerminalTextWidth.RuneWidth(new Rune(0x1F600)));
        Assert.Equal(4, TerminalTextWidth.DisplayWidth("A😀B"));
    }

    [Fact]
    public void TerminalTextWidthTreatsCombiningSequenceAsSingleCell()
    {
        Assert.Equal(1, TerminalTextWidth.TextElementWidth("e\u0301"));
        Assert.Equal(3, TerminalTextWidth.DisplayWidth("Ae\u0301B"));
    }

    [Fact]
    public void TerminalTextWidthTreatsZwJSequenceAsWideSingleElement()
    {
        Assert.Equal(2, TerminalTextWidth.TextElementWidth("🧑🏽\u200D💻"));
        Assert.Equal(4, TerminalTextWidth.DisplayWidth("A🧑🏽\u200D💻B"));
    }
}
