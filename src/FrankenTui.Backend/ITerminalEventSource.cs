using FrankenTui.Core;

namespace FrankenTui.Backend;

public interface ITerminalEventSource
{
    Size Size { get; }

    ValueTask<TerminalEvent?> ReadEventAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> PollEventAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default);
}
