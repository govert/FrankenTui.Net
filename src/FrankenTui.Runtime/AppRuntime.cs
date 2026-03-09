using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Runtime;

public sealed class AppRuntime<TModel, TMessage>
{
    private readonly ITerminalBackend _backend;
    private readonly RenderBuffer _current;
    private readonly RenderBuffer _next;
    private int _stepIndex;

    public AppRuntime(
        ITerminalBackend backend,
        Size size,
        Theme? theme = null,
        RuntimeExecutionPolicy? policy = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        var effectiveSize = size.IsEmpty ? new Size(1, 1) : size;
        _current = new RenderBuffer(effectiveSize.Width, effectiveSize.Height);
        _next = new RenderBuffer(effectiveSize.Width, effectiveSize.Height);
        Theme = theme ?? Theme.DefaultTheme;
        Policy = policy ?? RuntimeExecutionPolicy.Default;
    }

    public Theme Theme { get; }

    public RuntimeExecutionPolicy Policy { get; }

    public RuntimeTrace<TMessage> Trace { get; } = new();

    public ReplayTape<TMessage> Replay { get; } = new();

    public async ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        IAppProgram<TModel, TMessage> program,
        TModel model,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var update = program.Update(model, message);
        var presentation = await RenderAsync(program.BuildView(update.Model), cancellationToken).ConfigureAwait(false);
        var emitted = CollectMessages(update.Commands, update.Subscriptions);
        var screenText = HeadlessBufferView.ScreenString(_current);
        RuntimeTraceEntry<TMessage>? traceEntry = null;
        if (Policy.CaptureTrace)
        {
            Trace.Record(_stepIndex, message, emitted, screenText, presentation.Output);
            traceEntry = Trace.Entries[^1];
        }

        if (Policy.CaptureReplayTape)
        {
            Replay.Add(_stepIndex, message, emitted, screenText, presentation.Output);
        }

        _stepIndex++;
        return new RuntimeStepResult<TModel, TMessage>(
            update.Model,
            presentation,
            screenText,
            emitted,
            traceEntry);
    }

    public async ValueTask<PresentResult> RenderAsync(
        IRuntimeView view,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<uint, string>? links = null)
    {
        ArgumentNullException.ThrowIfNull(view);

        _next.Clear();
        view.Render(new RuntimeRenderContext(_next, Rect.FromSize(_next.Width, _next.Height), Theme));
        var diff = BufferDiff.Compute(_current, _next);
        var result = await _backend.PresentAsync(_next, diff, links, cancellationToken).ConfigureAwait(false);
        _current.CopyFrom(_next);
        return result;
    }

    private IReadOnlyList<TMessage> CollectMessages(
        AppCommand<TMessage> commands,
        IReadOnlyList<Subscription<TMessage>> subscriptions)
    {
        var messages = new List<TMessage>(commands.Messages);
        foreach (var subscription in subscriptions)
        {
            messages.AddRange(subscription.Invoke());
        }

        return messages;
    }
}
