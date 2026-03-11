using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Backend;

public interface ITerminalOutputSink
{
    TerminalCapabilities Capabilities { get; }

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
}
