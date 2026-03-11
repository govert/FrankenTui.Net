using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class AnsiSequencesTests
{
    [Fact]
    public void BuilderEmitsExpectedSgrAndCursorSequences()
    {
        var builder = new StringBuilder();
        AnsiBuilder.AppendSgrReset(builder);
        AnsiBuilder.AppendSgrFlags(builder, CellStyleFlags.Bold | CellStyleFlags.Italic);
        AnsiBuilder.AppendForeground(builder, PackedRgba.Red);
        AnsiBuilder.AppendBackground(builder, PackedRgba.Blue);
        AnsiBuilder.AppendBestCursorMove(builder, null, null, 9, 4);
        builder.Append(AnsiBuilder.EraseLine(EraseLineMode.All));

        Assert.Equal(
            "\u001b[0m\u001b[1;3m\u001b[38;2;255;0;0m\u001b[48;2;0;0;255m\u001b[5;10H\u001b[2K",
            builder.ToString());
    }

    [Fact]
    public void BuilderExposesCursorSaveRestoreAndKittyKeyboardSequences()
    {
        Assert.Equal("\u001b7", AnsiBuilder.CursorSave());
        Assert.Equal("\u001b8", AnsiBuilder.CursorRestore());
        Assert.Equal("\u001b[>15u", AnsiBuilder.KittyKeyboardEnable());
        Assert.Equal("\u001b[<u", AnsiBuilder.KittyKeyboardDisable());
    }

    [Fact]
    public void HyperlinkStartRejectsUnsafeOrOverlongUrls()
    {
        var builder = new StringBuilder();

        Assert.False(AnsiBuilder.TryAppendHyperlinkStart(builder, "https://example.test/\u001b[31m"));
        Assert.False(AnsiBuilder.TryAppendHyperlinkStart(builder, new string('a', 5000)));
        Assert.Empty(builder.ToString());
    }

    [Fact]
    public void SanitizeTextReplacesControlCharacters()
    {
        Assert.Equal("A B", AnsiBuilder.SanitizeText("A\u001bB"));
    }
}
