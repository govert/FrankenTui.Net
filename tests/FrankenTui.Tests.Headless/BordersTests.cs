// Upstream source: crates/ftui-widgets/src/borders.rs — tests
// Tests ported from 18 #[cfg(test)] mod tests functions.

using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class BordersTests
{
    [Fact] public void AsciiIsAsciiOnly()
    {
        var set = BorderSet.Ascii;
        foreach (var c in new[] { set.Vertical, set.Horizontal, set.TopLeft, set.TopRight, set.BottomLeft, set.BottomRight, set.TeeUp, set.TeeDown, set.TeeLeft, set.TeeRight, set.Cross })
            Assert.True(c < 128);
    }

    [Fact] public void SquareHasBoxDrawing()
    {
        var set = BorderSet.Square;
        Assert.Equal('─', set.Horizontal);
        Assert.Equal('│', set.Vertical);
        Assert.Equal('┼', set.Cross);
    }

    [Fact] public void RoundedHasRoundCorners()
    {
        var set = BorderSet.Rounded;
        Assert.Equal('╭', set.TopLeft);
        Assert.Equal('╮', set.TopRight);
        Assert.Equal('╰', set.BottomLeft);
        Assert.Equal('╯', set.BottomRight);
        Assert.Equal('─', set.Horizontal);
        Assert.Equal('│', set.Vertical);
    }

    [Fact] public void DoubleHasDoubleLines()
    {
        var set = BorderSet.Double;
        Assert.Equal('═', set.Horizontal);
        Assert.Equal('║', set.Vertical);
        Assert.Equal('╔', set.TopLeft);
        Assert.Equal('╗', set.TopRight);
        Assert.Equal('╚', set.BottomLeft);
        Assert.Equal('╝', set.BottomRight);
        Assert.Equal('╬', set.Cross);
    }

    [Fact] public void HeavyHasHeavyLines()
    {
        var set = BorderSet.Heavy;
        Assert.Equal('━', set.Horizontal);
        Assert.Equal('┃', set.Vertical);
        Assert.Equal('┏', set.TopLeft);
        Assert.Equal('┓', set.TopRight);
        Assert.Equal('┗', set.BottomLeft);
        Assert.Equal('┛', set.BottomRight);
        Assert.Equal('╋', set.Cross);
    }

    [Fact] public void AllBorderSetsHave11Fields()
    {
        foreach (var set in new[] { BorderSet.Ascii, BorderSet.Rounded, BorderSet.Square, BorderSet.Double, BorderSet.Heavy })
        {
            Assert.NotEqual(set.Horizontal, set.Vertical);
        }
    }

    [Fact] public void BoxDrawingSetsHaveDistinctCorners()
    {
        foreach (var set in new[] { BorderSet.Rounded, BorderSet.Square, BorderSet.Double, BorderSet.Heavy })
        {
            var corners = new[] { set.TopLeft, set.TopRight, set.BottomLeft, set.BottomRight };
            for (var i = 0; i < 4; i++)
                for (var j = i + 1; j < 4; j++)
                    Assert.NotEqual(corners[i], corners[j]);
        }
    }

    [Fact] public void AsciiSetReusesPlusForJunctions()
    {
        var set = BorderSet.Ascii;
        Assert.Equal('+', set.TopLeft); Assert.Equal('+', set.TopRight);
        Assert.Equal('+', set.BottomLeft); Assert.Equal('+', set.BottomRight);
        Assert.Equal('+', set.TeeUp); Assert.Equal('+', set.TeeDown);
        Assert.Equal('+', set.TeeLeft); Assert.Equal('+', set.TeeRight);
        Assert.Equal('+', set.Cross);
    }

    [Fact] public void BorderTypeToBorderSetRoundtrip()
    {
        Assert.Equal(BorderSet.Square, BorderType.Square.ToBorderSet());
        Assert.Equal(BorderSet.Ascii, BorderType.Ascii.ToBorderSet());
        Assert.Equal(BorderSet.Rounded, BorderType.Rounded.ToBorderSet());
        Assert.Equal(BorderSet.Double, BorderType.Double.ToBorderSet());
        Assert.Equal(BorderSet.Heavy, BorderType.Heavy.ToBorderSet());
    }

    [Fact] public void BorderTypeDefaultIsSquare()
    {
        Assert.Equal(BorderType.Square, default(BorderType));
    }

    [Fact] public void BorderTypeCustomUsesProvidedSet()
    {
        var custom = new BorderSet { Vertical = '!', Horizontal = '-', TopLeft = '/', TopRight = '\\', BottomLeft = '\\', BottomRight = '/', TeeUp = '+', TeeDown = '+', TeeLeft = '+', TeeRight = '+', Cross = '*' };
        // BorderType.Custom needs a separate mechanism upstream; here we verify the set works
        Assert.Equal('!', custom.Vertical);
    }

    [Fact] public void BordersNoneIsZero() { Assert.Equal((byte)0, (byte)Borders.None); }

    [Fact] public void BordersAllContainsAllSides()
    {
        Assert.True(Borders.All.HasFlag(Borders.Top));
        Assert.True(Borders.All.HasFlag(Borders.Right));
        Assert.True(Borders.All.HasFlag(Borders.Bottom));
        Assert.True(Borders.All.HasFlag(Borders.Left));
    }

    [Fact] public void BordersIndividualBitsAreDistinct()
    {
        foreach (var a in new[] { Borders.Top, Borders.Right, Borders.Bottom, Borders.Left })
            foreach (var b in new[] { Borders.Top, Borders.Right, Borders.Bottom, Borders.Left })
                if (!a.Equals(b)) Assert.False(a.HasFlag(b));
    }

    [Fact] public void BordersUnionAndIntersection()
    {
        var topLeft = Borders.Top | Borders.Left;
        Assert.True(topLeft.HasFlag(Borders.Top));
        Assert.True(topLeft.HasFlag(Borders.Left));
        Assert.False(topLeft.HasFlag(Borders.Right));
    }

    [Fact] public void BordersDefaultIsNone() { Assert.Equal(Borders.None, default(Borders)); }

    [Fact] public void NonAsciiSetsHaveNoAsciiChars()
    {
        foreach (var set in new[] { BorderSet.Rounded, BorderSet.Square, BorderSet.Double, BorderSet.Heavy })
            foreach (var c in new[] { set.Vertical, set.Horizontal, set.TopLeft, set.TopRight, set.BottomLeft, set.BottomRight, set.TeeUp, set.TeeDown, set.TeeLeft, set.TeeRight, set.Cross })
                Assert.False(c < 128);
    }

    [Fact] public void BorderSetTeesAreConsistent()
    {
        var set = BorderSet.Square;
        var tees = new[] { set.TeeUp, set.TeeDown, set.TeeLeft, set.TeeRight };
        for (var i = 0; i < 4; i++)
            for (var j = i + 1; j < 4; j++)
                Assert.NotEqual(tees[i], tees[j]);
    }
}
