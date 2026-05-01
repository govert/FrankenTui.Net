using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tty;

public sealed class TerminalSession : IAsyncDisposable
{
    private readonly ITerminalBackend _backend;
    private readonly TerminalSessionOptions _options;
    private TerminalBackendFeatures _features;

    public TerminalSession(ITerminalBackend backend, TerminalSessionOptions? options = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _options = options ?? new TerminalSessionOptions();
    }

    public TerminalCleanupPlan CleanupPlan { get; private set; } = TerminalCleanupPlan.Empty;

    public bool IsEntered { get; private set; }

    public TerminalCapabilities Capabilities => _backend.Capabilities;

    public TerminalBackendFeatures Features => _features;

    public async ValueTask EnterAsync(CancellationToken cancellationToken = default)
    {
        if (IsEntered)
        {
            return;
        }

        await _backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _backend.ConfigureSessionAsync(
            new TerminalSessionConfiguration
            {
                InlineMode = _options.InlineMode,
                CaptureInput = _options.Modes.HasFlag(TerminalMode.RawInput),
                ClaimConsoleModes = _options.Modes != TerminalMode.None,
                HostEvidenceTag = _options.InlineMode ? "inline" : "alternate"
            },
            cancellationToken).ConfigureAwait(false);
        CleanupPlan = BuildCleanupPlan(_options);
        foreach (var sequence in CleanupPlan.EnterSequences)
        {
            await _backend.WriteControlAsync(sequence, cancellationToken).ConfigureAwait(false);
        }

        _features = BuildFeatures(_options).Sanitize(_backend.Capabilities);
        await _backend.SetFeaturesAsync(_features, cancellationToken).ConfigureAwait(false);

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

    public ValueTask<bool> PollEventAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
        _backend.PollEventAsync(timeout, cancellationToken);

    public ValueTask WriteLogAsync(
        string text,
        TerminalLogWriteOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _backend.WriteLogAsync(text, options, cancellationToken);

    public async ValueTask SetMouseCaptureAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var next = (_features with { MouseCapture = enabled }).Sanitize(_backend.Capabilities);
        if (next == _features)
        {
            return;
        }

        await _backend.SetFeaturesAsync(next, cancellationToken).ConfigureAwait(false);
        _features = next;
    }

    public async ValueTask DisposeAsync()
    {
        if (IsEntered)
        {
            await _backend.SetFeaturesAsync(TerminalBackendFeatures.None).ConfigureAwait(false);
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

        return new TerminalCleanupPlan(enter, exit);
    }

    private static TerminalBackendFeatures BuildFeatures(TerminalSessionOptions options) =>
        new(
            MouseCapture: options.UseMouseTracking || options.Modes.HasFlag(TerminalMode.MouseTracking),
            BracketedPaste: options.Modes.HasFlag(TerminalMode.BracketedPaste),
            FocusEvents: options.Modes.HasFlag(TerminalMode.FocusEvents),
            KittyKeyboard: options.UseKittyKeyboard);
}
