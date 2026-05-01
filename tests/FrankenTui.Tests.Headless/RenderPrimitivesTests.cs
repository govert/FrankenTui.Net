using System.Runtime.CompilerServices;
using System.Text;
using FrankenTui.Render;
using FrankenTui.Simd;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class RenderPrimitivesTests
{
    [Fact]
    public void GraphemeIdPacksSlotGenerationAndWidth()
    {
        var id = new GraphemeId(42, 7, 3);

        Assert.Equal(42, id.Slot);
        Assert.Equal((ushort)7, id.Generation);
        Assert.Equal(3, id.Width);
    }

    [Fact]
    public void CellContentNormalizesTabsAndMeasuresWideRunes()
    {
        var tab = CellContent.FromChar('\t');
        var emoji = CellContent.FromRune(new Rune(0x1F600));

        Assert.Equal(' ', tab.AsRune()!.Value.ToString()[0]);
        Assert.Equal(2, emoji.Width());
    }

    [Fact]
    public void PackedRgbaOverMatchesExpectedHalfAlphaBlend()
    {
        var source = PackedRgba.Rgba(255, 0, 0, 128);
        var destination = PackedRgba.Rgba(0, 0, 255, 255);

        Assert.Equal(PackedRgba.Rgba(128, 0, 127, 255), source.Over(destination));
    }

    [Fact]
    public void CellAttributesRoundTripFlagsAndLink()
    {
        var attributes = new CellAttributes(CellStyleFlags.Bold | CellStyleFlags.Italic, 42);

        Assert.Equal(CellStyleFlags.Bold | CellStyleFlags.Italic, attributes.Flags);
        Assert.Equal((uint)42, attributes.LinkId);
        Assert.True(attributes.HasFlag(CellStyleFlags.Bold));
        Assert.Equal((uint)100, attributes.WithLink(100).LinkId);
    }

    [Fact]
    public void CellRetainsSixteenByteLayout()
    {
        Assert.Equal(16, Unsafe.SizeOf<Cell>());
    }

    [Fact]
    public void BufferClampsToOneByOneAndStartsDirty()
    {
        var buffer = new RenderBuffer(0, 0);

        Assert.Equal((ushort)1, buffer.Width);
        Assert.Equal((ushort)1, buffer.Height);
        Assert.True(buffer.IsRowDirty(0));
    }

    [Fact]
    public void BufferSetRespectsScissorAndMarksDirtyRows()
    {
        var buffer = new RenderBuffer(4, 2);
        buffer.ClearDirty();
        buffer.PushScissor(new FrankenTui.Core.Rect(1, 0, 1, 1));

        buffer.Set(0, 0, Cell.FromChar('A'));
        buffer.Set(1, 0, Cell.FromChar('B'));

        Assert.Equal(Cell.Empty, buffer.Get(0, 0));
        Assert.Equal(Cell.FromChar('B'), buffer.Get(1, 0));
        Assert.True(buffer.IsRowDirty(0));
    }

    [Fact]
    public void BufferWritesContinuationCellsForWideRunes()
    {
        var buffer = new RenderBuffer(4, 1);
        buffer.ClearDirty();

        buffer.Set(0, 0, Cell.FromRune(new Rune(0x1F600)));

        Assert.Equal("😀", HeadlessBufferView.RowText(buffer, 0));
        Assert.True(buffer.Get(1, 0)!.Value.IsContinuation);
    }

    [Fact]
    public void BufferSetTextPreservesMultiCodepointGraphemeText()
    {
        var buffer = new RenderBuffer(6, 1);

        buffer.SetText(0, 0, "e\u0301", Cell.FromChar('x'));
        buffer.SetText(1, 0, "🧑🏽\u200D💻", Cell.FromChar('x'));

        Assert.Equal("e\u0301🧑🏽\u200D💻", HeadlessBufferView.RowText(buffer, 0));
        Assert.True(buffer.Get(2, 0)!.Value.IsContinuation);
    }

    [Fact]
    public void BufferDiffTreatsEqualGraphemeTextAsStableAcrossLocalRegistryIds()
    {
        var previousEnabled = SimdAccelerators.IsEnabled;
        SimdAccelerators.EnableIfSupported();

        try
        {
        var oldBuffer = new RenderBuffer(6, 1);
        oldBuffer.SetText(0, 0, "e\u0301", Cell.FromChar('x'));
        oldBuffer.SetText(2, 0, "🧑🏽\u200D💻", Cell.FromChar('x'));

        var newBuffer = new RenderBuffer(6, 1);
        newBuffer.SetText(2, 0, "🧑🏽\u200D💻", Cell.FromChar('x'));
        newBuffer.SetText(0, 0, "e\u0301", Cell.FromChar('x'));

        var diff = BufferDiff.Compute(oldBuffer, newBuffer);

        Assert.True(diff.IsEmpty);
        }
        finally
        {
            if (previousEnabled)
            {
                SimdAccelerators.EnableIfSupported();
            }
            else
            {
                SimdAccelerators.Disable();
            }
        }
    }

    [Fact]
    public void BufferOverwritingWideHeadClearsTail()
    {
        var buffer = new RenderBuffer(4, 1);

        buffer.Set(0, 0, Cell.FromRune(new Rune(0x1F600)));
        buffer.Set(0, 0, Cell.FromChar('A'));

        Assert.Equal(Cell.FromChar('A'), buffer.Get(0, 0));
        Assert.Equal(Cell.Empty, buffer.Get(1, 0));
    }

    [Fact]
    public void BufferOverwritingContinuationClearsOwningHead()
    {
        var buffer = new RenderBuffer(4, 1);

        buffer.Set(0, 0, Cell.FromRune(new Rune(0x1F600)));
        buffer.Set(1, 0, Cell.FromChar('B'));

        Assert.Equal(Cell.Empty, buffer.Get(0, 0));
        Assert.Equal(Cell.FromChar('B'), buffer.Get(1, 0));
    }

    [Fact]
    public void BufferAppliesOpacityToIncomingCell()
    {
        var buffer = new RenderBuffer(2, 1);
        var cell = Cell.FromChar('X')
            .WithForeground(PackedRgba.Rgba(10, 20, 30, 255))
            .WithBackground(PackedRgba.Rgba(40, 50, 60, 200));

        buffer.PushOpacity(0.5f);
        buffer.Set(0, 0, cell);

        var stored = buffer.Get(0, 0)!.Value;
        Assert.Equal((byte)128, stored.Foreground.A);
        Assert.True(stored.Background.A >= 100);
    }
}
