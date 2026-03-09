using System.Text;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class DiffAndHeadlessTests
{
    [Fact]
    public void BufferDiffCoalescesAdjacentCellsIntoRuns()
    {
        var oldBuffer = new RenderBuffer(4, 2);
        var newBuffer = new RenderBuffer(4, 2);
        oldBuffer.ClearDirty();
        newBuffer.ClearDirty();

        newBuffer.Set(1, 0, Cell.FromChar('A'));
        newBuffer.Set(2, 0, Cell.FromChar('B'));
        newBuffer.Set(0, 1, Cell.FromChar('C'));

        var diff = BufferDiff.Compute(oldBuffer, newBuffer);
        var runs = diff.Runs();

        Assert.Equal(3, diff.Count);
        Assert.Equal(
            [new ChangeRun(0, 1, 2), new ChangeRun(1, 0, 0)],
            runs);
    }

    [Fact]
    public void DirtyDiffUsesNewBufferDirtyRowsAsGate()
    {
        var oldBuffer = new RenderBuffer(4, 2);
        var newBuffer = new RenderBuffer(4, 2);
        oldBuffer.ClearDirty();
        newBuffer.ClearDirty();
        newBuffer.Set(3, 1, Cell.FromChar('Z'));

        var diff = BufferDiff.ComputeDirty(oldBuffer, newBuffer);

        Assert.Equal([new CellPosition(3, 1)], diff.Changes);
    }

    [Fact]
    public void HeadlessBufferViewTrimsTrailingSpacesAndSkipsContinuations()
    {
        var buffer = new RenderBuffer(5, 2);
        buffer.ClearDirty();
        buffer.Set(0, 0, Cell.FromChar('H'));
        buffer.Set(1, 0, Cell.FromChar('i'));
        buffer.Set(0, 1, Cell.FromRune(new Rune(0x1F600)));

        Assert.Equal("Hi", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("😀", HeadlessBufferView.RowText(buffer, 1));
        Assert.Equal($"Hi{Environment.NewLine}😀", HeadlessBufferView.ScreenString(buffer));
    }
}
