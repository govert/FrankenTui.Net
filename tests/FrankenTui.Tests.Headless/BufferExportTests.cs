using FrankenTui.Extras;
using FrankenTui.Render;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class BufferExportTests
{
    private static Render.Buffer MakeBuf(ushort w, ushort h)
    {
        var b = new Render.Buffer(w, h);
        for (ushort y = 0; y < h; y++)
            for (ushort x = 0; x < w && x < 16; x++)
                b.Set(x, y, new Cell(CellContent.FromChar("ABCDEFGHIJKLMNOP"[x]), PackedRgba.White, PackedRgba.Black, CellAttributes.None));
        return b;
    }

    [Fact] public void ToPlainTextBasic()
    {
        var b = MakeBuf(16, 1);
        var text = BufferExport.ToPlainText(b);
        Assert.Contains("ABCDEFGHIJKLMNOP", text);
    }

    [Fact] public void ToPlainTextEmptyBuffer()
    {
        var b = new Render.Buffer(5, 3);
        var text = BufferExport.ToPlainText(b);
        var lines = text.Split(Environment.NewLine);
        // RowText trims trailing spaces, so all-empty rows become empty strings
        Assert.Equal(3, lines.Length);
    }

    [Fact] public void ToPlainTextMultiline()
    {
        var b = MakeBuf(5, 3);
        var text = BufferExport.ToPlainText(b);
        var lines = text.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.Equal("ABCDE", lines[0]);
    }

    [Fact] public void ToJsonlBasic()
    {
        var b = MakeBuf(4, 1);
        var jsonl = BufferExport.ToJsonl(b);
        Assert.Contains("\"x\":0", jsonl);
        Assert.Contains("\"y\":0", jsonl);
    }

    [Fact] public void ToJsonlAllCells()
    {
        var b = MakeBuf(2, 2);
        var jsonl = BufferExport.ToJsonl(b);
        var lines = jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length); // 2x2 = 4 cells
    }

    [Fact] public void ToJsonlContainsFields()
    {
        var b = MakeBuf(1, 1);
        var jsonl = BufferExport.ToJsonl(b);
        Assert.Contains("\"x\"", jsonl);
        Assert.Contains("\"y\"", jsonl);
        Assert.Contains("\"c\"", jsonl);
        Assert.Contains("\"fg\"", jsonl);
        Assert.Contains("\"bg\"", jsonl);
    }

    [Fact] public void ToJsonlEmptyBuffer()
    {
        var b = new Render.Buffer(2, 1);
        var jsonl = BufferExport.ToJsonl(b);
        var lines = jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
    }
}
