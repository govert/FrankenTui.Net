using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class TerminalModelTests
{
    [Fact]
    public void TerminalModelAppliesStyleAndEraseSequences()
    {
        var builder = new StringBuilder();
        AnsiBuilder.AppendSgrFlags(builder, CellStyleFlags.Bold | CellStyleFlags.Underline);
        AnsiBuilder.AppendForeground(builder, PackedRgba.Green);
        builder.Append('A');
        builder.Append(AnsiBuilder.CursorPosition(0, 0));
        builder.Append(AnsiBuilder.EraseLine(EraseLineMode.All));
        builder.Append(AnsiBuilder.CursorPosition(0, 1));
        builder.Append('B');

        var model = new TerminalModel(4, 1);
        model.Process(builder.ToString());

        Assert.Equal(" B", model.RowText(0));
        Assert.Equal(CellStyleFlags.None, model.Cell(0, 0)!.Value.Attributes.Flags);
    }

    [Fact]
    public void TerminalModelTracksHyperlinksAndSyncOutput()
    {
        var model = new TerminalModel(4, 1);
        model.Process("\u001b[?2026h\u001b]8;;https://example.test\u001b\\A\u001b]8;;\u001b\\\u001b[?2026l");

        var cell = model.Cell(0, 0)!.Value;
        Assert.Equal("A", cell.Text);
        Assert.NotEqual(0u, cell.Attributes.LinkId);
        Assert.Equal("https://example.test", model.LinkUrl(cell.Attributes.LinkId));
        Assert.Equal(0, model.SyncOutputDepth);
    }
}
