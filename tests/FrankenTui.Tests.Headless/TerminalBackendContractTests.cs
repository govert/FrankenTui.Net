using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Tty;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class TerminalBackendContractTests
{
    [Fact]
    public async Task InlineModeUsesDecSaveRestoreAndClearsShrunkenRegion()
    {
        var backend = new MemoryTerminalBackend(new Size(8, 5));
        await using var session = new TerminalSession(
            backend,
            new TerminalSessionOptions
            {
                InlineMode = true
            });

        await session.EnterAsync();
        backend.DrainOutput();

        var initial = BufferFromLines(8, ["AAA", "BBB", "CCC"]);
        await session.PresentAsync(initial, BufferDiff.Full(initial.Width, initial.Height));
        backend.DrainOutput();

        var next = BufferFromLines(8, ["111", "222"]);
        var result = await session.PresentAsync(next, BufferDiff.Full(next.Width, next.Height));

        Assert.Contains(AnsiBuilder.CursorSave(), result.Output);
        Assert.Contains(AnsiBuilder.CursorRestore(), result.Output);
        Assert.Equal(string.Empty, backend.Model.RowText(2));
        Assert.Equal("111", backend.Model.RowText(3));
        Assert.Equal("222", backend.Model.RowText(4));
    }

    [Fact]
    public async Task InlineLogWritesAreSanitizedAndCursorProtected()
    {
        var backend = new MemoryTerminalBackend(new Size(14, 5));
        await using var session = new TerminalSession(
            backend,
            new TerminalSessionOptions
            {
                InlineMode = true
            });

        await session.EnterAsync();
        backend.DrainOutput();

        var buffer = BufferFromLines(14, ["UI", "Rows"]);
        await session.PresentAsync(buffer, BufferDiff.Full(buffer.Width, buffer.Height));
        backend.DrainOutput();

        await session.WriteLogAsync("safe\u001b[31m red\u001b[0m\nsecond");
        var transcript = backend.DrainOutput();

        Assert.Contains(AnsiBuilder.CursorSave(), transcript);
        Assert.Contains(AnsiBuilder.CursorRestore(), transcript);
        Assert.DoesNotContain("\u001b[31m", transcript, StringComparison.Ordinal);
        Assert.Equal("safe red", backend.Model.RowText(2));
    }

    [Fact]
    public async Task BackendFeatureRoutingSanitizesUnsupportedModesAndDiffsTransitions()
    {
        var backend = new MemoryTerminalBackend(new Size(20, 4), TerminalCapabilities.Tmux());
        await backend.InitializeAsync();
        await backend.ConfigureSessionAsync(new TerminalSessionConfiguration());

        await backend.SetFeaturesAsync(new TerminalBackendFeatures(
            MouseCapture: true,
            BracketedPaste: true,
            FocusEvents: true,
            KittyKeyboard: true));
        var enabled = backend.DrainOutput();

        Assert.Contains("\u001b[?1003h", enabled);
        Assert.Contains("\u001b[?2004h", enabled);
        Assert.DoesNotContain("\u001b[?1004h", enabled, StringComparison.Ordinal);
        Assert.DoesNotContain(AnsiBuilder.KittyKeyboardEnable(), enabled, StringComparison.Ordinal);

        await backend.SetFeaturesAsync(new TerminalBackendFeatures(
            MouseCapture: true,
            BracketedPaste: true,
            FocusEvents: true,
            KittyKeyboard: true));
        Assert.Equal(string.Empty, backend.DrainOutput());

        await backend.SetFeaturesAsync(TerminalBackendFeatures.None);
        var disabled = backend.DrainOutput();
        Assert.Contains("\u001b[?1003l", disabled);
        Assert.Contains("\u001b[?2004l", disabled);
    }

    [Fact]
    public async Task PollEventReturnsQueuedEvents()
    {
        var backend = new MemoryTerminalBackend(new Size(10, 4));
        backend.Enqueue(TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)));

        Assert.True(await backend.PollEventAsync(TimeSpan.Zero));
        Assert.IsType<KeyTerminalEvent>(await backend.ReadEventAsync());
    }

    [Fact]
    public void TerminalOutputSanitizerStripsEscapesAndC1Controls()
    {
        var sanitized = TerminalOutputSanitizer.Sanitize("safe\u001b]52;c;SGVsbG8=\u001b\\tail\u009dx\n");

        Assert.Equal("safetailx\n", sanitized);
    }

    private static RenderBuffer BufferFromLines(ushort width, string[] lines)
    {
        var buffer = new RenderBuffer(width, (ushort)lines.Length);
        for (ushort row = 0; row < lines.Length; row++)
        {
            var text = lines[row];
            for (ushort column = 0; column < text.Length && column < width; column++)
            {
                buffer.Set(column, row, Cell.FromChar(text[column]));
            }
        }

        return buffer;
    }
}
