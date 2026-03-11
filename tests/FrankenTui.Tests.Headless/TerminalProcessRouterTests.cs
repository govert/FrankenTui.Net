using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Testing.Harness;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class TerminalProcessRouterTests
{
    [Fact]
    public async Task StreamingRunnerEmitsStdoutAndStderrChunks()
    {
        var chunks = new List<ProcessOutputChunk>();

        var result = await ProcessCommandRunner.RunStreamingAsync(
            "bash",
            ["-lc", "printf 'alpha\\n'; >&2 printf 'beta\\n'"],
            (chunk, _) =>
            {
                chunks.Add(chunk);
                return ValueTask.CompletedTask;
            });

        Assert.True(result.Success);
        Assert.Contains(chunks, chunk => chunk.Stream == ProcessOutputStream.Stdout && chunk.Text.Contains("alpha", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Stream == ProcessOutputStream.Stderr && chunk.Text.Contains("beta", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TerminalProcessRouterSanitizesForwardedSubprocessOutput()
    {
        var backend = new MemoryTerminalBackend(new Size(14, 6), TerminalCapabilities.Tmux());
        await backend.InitializeAsync();
        await backend.ConfigureSessionAsync(new TerminalSessionConfiguration
        {
            InlineMode = true
        });

        var buffer = BufferFromLines(14, ["UI", "Rows"]);
        await backend.PresentAsync(buffer, BufferDiff.Full(buffer.Width, buffer.Height));
        backend.DrainOutput();

        var result = await TerminalProcessRouter.RunAsync(
            backend,
            "bash",
            ["-lc", "printf 'safe\\e[31m red\\n'"]);
        var transcript = backend.DrainOutput();

        Assert.True(result.Success);
        Assert.Contains(AnsiBuilder.CursorSave(), transcript);
        Assert.Contains(AnsiBuilder.CursorRestore(), transcript);
        Assert.DoesNotContain("\u001b[31m", transcript, StringComparison.Ordinal);
        Assert.Contains("safe red", transcript, StringComparison.Ordinal);
    }

    private static RenderBuffer BufferFromLines(ushort width, IReadOnlyList<string> lines)
    {
        var buffer = new RenderBuffer(width, (ushort)Math.Max(lines.Count, 1));
        for (ushort row = 0; row < lines.Count; row++)
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
