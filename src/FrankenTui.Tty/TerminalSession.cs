using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tty;

public sealed class TerminalSession : IAsyncDisposable
{
    private readonly ITerminalBackend _backend;
    private readonly TerminalSessionOptions _options;

    public TerminalSession(ITerminalBackend backend, TerminalSessionOptions? options = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _options = options ?? new TerminalSessionOptions();
    }

    public TerminalCleanupPlan CleanupPlan { get; private set; } = TerminalCleanupPlan.Empty;

    public bool IsEntered { get; private set; }

    public TerminalCapabilities Capabilities => _backend.Capabilities;

    public async ValueTask EnterAsync(CancellationToken cancellationToken = default)
    {
        if (IsEntered)
        {
            return;
        }

        await _backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        CleanupPlan = BuildCleanupPlan(_options);
        foreach (var sequence in CleanupPlan.EnterSequences)
        {
            await _backend.WriteControlAsync(sequence, cancellationToken).ConfigureAwait(false);
        }

        IsEntered = true;
    }

    public ValueTask<PresentResult> PresentAsync(
        RenderBuffer buffer,
        BufferDiff? diff = null,
        IReadOnlyDictionary<uint, string>? links = null,
        CancellationToken cancellationToken = default) =>
        _backend.PresentAsync(buffer, diff, links, cancellationToken);

    public ValueTask<TerminalEvent?> ReadEventAsync(CancellationToken cancellationToken = default) =>
        _backend.ReadEventAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (IsEntered)
        {
            for (var index = CleanupPlan.ExitSequences.Count - 1; index >= 0; index--)
            {
                await _backend.WriteControlAsync(CleanupPlan.ExitSequences[index]).ConfigureAwait(false);
            }
        }

        IsEntered = false;
        await _backend.DisposeAsync().ConfigureAwait(false);
    }

    private static TerminalCleanupPlan BuildCleanupPlan(TerminalSessionOptions options)
    {
        var enter = new List<string>();
        var exit = new List<string>();

        if (options.UseAlternateScreen)
        {
            enter.Add("\u001b[?1049h");
            exit.Add("\u001b[?1049l");
        }

        if (options.HideCursor)
        {
            enter.Add(AnsiBuilder.HideCursor());
            exit.Add(AnsiBuilder.ShowCursor());
        }

        if (options.Modes.HasFlag(TerminalMode.FocusEvents))
        {
            enter.Add("\u001b[?1004h");
            exit.Add("\u001b[?1004l");
        }

        if (options.Modes.HasFlag(TerminalMode.BracketedPaste))
        {
            enter.Add("\u001b[?2004h");
            exit.Add("\u001b[?2004l");
        }

        if (options.UseMouseTracking || options.Modes.HasFlag(TerminalMode.MouseTracking))
        {
            enter.Add("\u001b[?1003h\u001b[?1006h");
            exit.Add("\u001b[?1003l\u001b[?1006l");
        }

        return new TerminalCleanupPlan(enter, exit);
    }
}
