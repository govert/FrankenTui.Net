using FrankenTui.Core;

namespace FrankenTui.Backend;

public interface ITerminalBackend :
    ITerminalSessionLifecycle,
    ITerminalOutputSink,
    ITerminalEventSource,
    IAsyncDisposable
{
}
