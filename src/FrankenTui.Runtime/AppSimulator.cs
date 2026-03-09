using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;

namespace FrankenTui.Runtime;

public sealed class AppSimulator<TModel, TMessage>
{
    public AppSimulator(Size size, Theme? theme = null)
    {
        Backend = new MemoryTerminalBackend(size);
        Runtime = new AppRuntime<TModel, TMessage>(Backend, size, theme);
    }

    public MemoryTerminalBackend Backend { get; }

    public AppRuntime<TModel, TMessage> Runtime { get; }

    public ValueTask<PresentResult> RenderAsync(IRuntimeView view, CancellationToken cancellationToken = default) =>
        Runtime.RenderAsync(view, cancellationToken);

    public ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        IAppProgram<TModel, TMessage> program,
        TModel model,
        TMessage message,
        CancellationToken cancellationToken = default) =>
        Runtime.DispatchAsync(program, model, message, cancellationToken);
}
