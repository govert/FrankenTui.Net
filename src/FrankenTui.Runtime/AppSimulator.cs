using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;

namespace FrankenTui.Runtime;

public sealed class AppSimulator<TModel, TMessage>
{
    public AppSimulator(Size size, Theme? theme = null, RuntimeExecutionPolicy? policy = null)
    {
        Backend = new MemoryTerminalBackend(size);
        Runtime = new AppRuntime<TModel, TMessage>(Backend, size, theme, policy);
    }

    public MemoryTerminalBackend Backend { get; }

    public AppRuntime<TModel, TMessage> Runtime { get; }

    public RuntimeTrace<TMessage> Trace => Runtime.Trace;

    public ReplayTape<TMessage> Replay => Runtime.Replay;

    public AppSession<TModel, TMessage> CreateSession(IAppProgram<TModel, TMessage> program, TModel? model = default) =>
        new(Runtime, program, model);

    public ValueTask<PresentResult> RenderAsync(IRuntimeView view, CancellationToken cancellationToken = default) =>
        Runtime.RenderAsync(view, cancellationToken);

    public ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        IAppProgram<TModel, TMessage> program,
        TModel model,
        TMessage message,
        CancellationToken cancellationToken = default) =>
        Runtime.DispatchAsync(program, model, message, cancellationToken);
}
