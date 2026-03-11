using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Backend;

public interface ITerminalBackend : IAsyncDisposable
{
    string Name { get; }

    Size Size { get; }

    TerminalCapabilities Capabilities { get; }

    TerminalLifecycleState State { get; }

    ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    ValueTask ConfigureSessionAsync(
        TerminalSessionConfiguration configuration,
        CancellationToken cancellationToken = default);

    ValueTask SetFeaturesAsync(TerminalBackendFeatures features, CancellationToken cancellationToken = default);

    ValueTask WriteControlAsync(string sequence, CancellationToken cancellationToken = default);

    ValueTask WriteLogAsync(
        string text,
        TerminalLogWriteOptions? options = null,
        CancellationToken cancellationToken = default);

    ValueTask<PresentResult> PresentAsync(
        RenderBuffer buffer,
        BufferDiff? diff = null,
        IReadOnlyDictionary<uint, string>? links = null,
        CancellationToken cancellationToken = default);

    ValueTask<TerminalEvent?> ReadEventAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> PollEventAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default);
}
