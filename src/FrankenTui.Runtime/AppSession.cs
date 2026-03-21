using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Runtime;

public sealed class AppSession<TModel, TMessage>
{
    private readonly Queue<TMessage> _pending = [];

    public AppSession(
        AppRuntime<TModel, TMessage> runtime,
        IAppProgram<TModel, TMessage> program,
        TModel? model = default)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        Program = program ?? throw new ArgumentNullException(nameof(program));
        Model = model is null ? program.Initialize() : model;

        if (Runtime.Policy.EmitTelemetry)
        {
            Runtime.Telemetry.Record(
                "ftui.program.init",
                TelemetryEventCategory.RuntimePhase,
                Runtime.CurrentStepIndex,
                [
                    TelemetryRedactor.TypeField("model_type", Model?.GetType() ?? typeof(TModel), Runtime.Telemetry.Config.Verbose),
                    new TelemetryField("cmd_count", "0"),
                    new TelemetryField("subscription_count", "0")
                ]);
        }
    }

    public AppRuntime<TModel, TMessage> Runtime { get; }

    public IAppProgram<TModel, TMessage> Program { get; }

    public TModel Model { get; private set; }

    public int PendingCount => _pending.Count;

    public RuntimeStepResult<TModel, TMessage>? LastStep { get; private set; }

    public IReadOnlyList<TMessage> PendingMessages => _pending.ToArray();

    public void Enqueue(TMessage message, bool trackEffectQueue = true)
    {
        _pending.Enqueue(message);
        if (trackEffectQueue)
        {
            EffectSystem.RecordQueueEnqueue(_pending.Count);
        }
    }

    public void EnqueueRange(IEnumerable<TMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
        {
            Enqueue(message);
        }
    }

    public void ClearPending()
    {
        EffectSystem.RecordQueueDrop(_pending.Count);
        _pending.Clear();
    }

    public async ValueTask<PresentResult> RenderCurrentAsync(CancellationToken cancellationToken = default) =>
        await Runtime.RenderAsync(Program.BuildView(Model), cancellationToken).ConfigureAwait(false);

    public async ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await Runtime.DispatchAsync(Program, Model, message, cancellationToken).ConfigureAwait(false);
        Model = result.Model;
        LastStep = result;
        EnqueueRange(result.EmittedMessages);
        return result;
    }

    public async ValueTask<RuntimeBatchResult<TModel, TMessage>> DrainAsync(
        int maxSteps = 256,
        CancellationToken cancellationToken = default)
    {
        if (maxSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSteps), maxSteps, "Max steps must be positive.");
        }

        var results = new List<RuntimeStepResult<TModel, TMessage>>();
        while (_pending.Count > 0 && results.Count < maxSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = _pending.Dequeue();
            EffectSystem.RecordQueueProcessed(_pending.Count);
            results.Add(await DispatchAsync(next, cancellationToken).ConfigureAwait(false));
        }

        return new RuntimeBatchResult<TModel, TMessage>(Model, results, _pending.Count > 0);
    }

    public async ValueTask<PresentResult> ResizeAsync(
        Size size,
        Func<Size, TMessage>? resizeMessageFactory = null,
        CancellationToken cancellationToken = default)
    {
        await Runtime.ResizeAsync(size, cancellationToken).ConfigureAwait(false);

        if (resizeMessageFactory is null)
        {
            return await RenderCurrentAsync(cancellationToken).ConfigureAwait(false);
        }

        Enqueue(resizeMessageFactory(size));
        await DrainAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return LastStep?.Presentation ?? await RenderCurrentAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed record RuntimeBatchResult<TModel, TMessage>(
    TModel Model,
    IReadOnlyList<RuntimeStepResult<TModel, TMessage>> Steps,
    bool QueueRemaining);
