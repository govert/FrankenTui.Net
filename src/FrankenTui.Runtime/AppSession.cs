using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Runtime;

public sealed class AppSession<TModel, TMessage>
{
    private readonly Queue<TMessage> _pending = [];
    private readonly RuntimeQueueingScheduler? _scheduler;
    private readonly Dictionary<ulong, TMessage> _scheduledMessages = [];

    public AppSession(
        AppRuntime<TModel, TMessage> runtime,
        IAppProgram<TModel, TMessage> program,
        TModel? model = default)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        Program = program ?? throw new ArgumentNullException(nameof(program));
        Model = model is null ? program.Initialize() : model;
        var effectQueue = Runtime.Policy.EffectiveEffectQueue;
        if (effectQueue.Enabled && effectQueue.Backend == RuntimeTaskExecutorBackend.EffectQueue)
        {
            var schedulerConfig = effectQueue.MaxQueueDepth > 0
                ? effectQueue.EffectiveScheduler with { MaxQueueSize = effectQueue.MaxQueueDepth }
                : effectQueue.EffectiveScheduler;
            _scheduler = new RuntimeQueueingScheduler(schedulerConfig);
        }

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

    public int PendingCount => _scheduler?.QueueLength ?? _pending.Count;

    public long DroppedMessages { get; private set; }

    public RuntimeStepResult<TModel, TMessage>? LastStep { get; private set; }

    public IReadOnlyList<TMessage> PendingMessages => _scheduler is null
        ? _pending.ToArray()
        : _scheduler.Evidence().Jobs
            .Select(job => _scheduledMessages.TryGetValue(job.JobId, out var message) ? message : default!)
            .Where(static message => message is not null)
            .ToArray();

    public RuntimeSchedulingEvidence? SchedulerEvidence => _scheduler?.Evidence();

    public void Enqueue(TMessage message, bool trackEffectQueue = true) =>
        Enqueue(message, queueSpec: null, trackEffectQueue);

    public void Enqueue(TMessage message, RuntimeQueueTaskSpec? queueSpec, bool trackEffectQueue = true)
    {
        if (_scheduler is not null)
        {
            var spec = queueSpec ?? RuntimeQueueTaskSpec.Default;
            var jobId = _scheduler.SubmitWithSources(
                spec.Weight,
                spec.EstimatedMilliseconds,
                spec.WeightSource,
                spec.EstimateSource,
                spec.Name ?? message?.GetType().Name ?? "message");
            if (jobId is null)
            {
                DroppedMessages++;
                if (trackEffectQueue)
                {
                    EffectSystem.RecordQueueDrop(1);
                }

                return;
            }

            _scheduledMessages[jobId.Value] = message;
            if (trackEffectQueue)
            {
                EffectSystem.RecordQueueEnqueue(PendingCount);
            }

            return;
        }

        if (ShouldDropForBackpressure())
        {
            DroppedMessages++;
            if (trackEffectQueue)
            {
                EffectSystem.RecordQueueDrop(1);
            }

            return;
        }

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

    public void EnqueueCommandMessages(AppCommand<TMessage> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        for (var index = 0; index < command.Messages.Count; index++)
        {
            RuntimeQueueTaskSpec? spec = null;
            command.QueueSpecs?.TryGetValue(index, out spec);
            Enqueue(command.Messages[index], spec);
        }
    }

    public void ClearPending()
    {
        EffectSystem.RecordQueueDrop(PendingCount);
        _pending.Clear();
        _scheduler?.Clear();
        _scheduledMessages.Clear();
    }

    public async ValueTask<PresentResult> RenderCurrentAsync(CancellationToken cancellationToken = default) =>
        await Runtime.RenderAsync(Program.BuildView(Model), cancellationToken).ConfigureAwait(false);

    private void RecordSchedulerSelectTelemetry(RuntimeSchedulingEvidence? evidence)
    {
        if (evidence is null || !Runtime.Policy.EmitTelemetry)
        {
            return;
        }

        Runtime.Telemetry.Record(
            "ftui.effect.queue.select",
            TelemetryEventCategory.RuntimePhase,
            Runtime.CurrentStepIndex,
            [
                new TelemetryField("current_time", evidence.CurrentTime.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)),
                new TelemetryField("selected_job_id", evidence.SelectedJobId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""),
                new TelemetryField("queue_length", evidence.QueueLength.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new TelemetryField("mean_wait_time", evidence.MeanWaitTime.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)),
                new TelemetryField("max_wait_time", evidence.MaxWaitTime.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)),
                new TelemetryField("reason", evidence.Reason.Label()),
                new TelemetryField("tie_break_reason", evidence.TieBreakReason?.Label() ?? ""),
                new TelemetryField("job_count", evidence.Jobs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ]);
    }

    private TMessage? DequeueNextPending()
    {
        if (_scheduler is null)
        {
            return _pending.Count == 0 ? default : _pending.Dequeue();
        }

        var job = _scheduler.PeekNext();
        if (job is null)
        {
            return default;
        }

        _scheduler.Cancel(job.Id);
        if (_scheduledMessages.Remove(job.Id, out var message))
        {
            return message;
        }

        return default;
    }

    private bool ShouldDropForBackpressure()
    {
        var config = Runtime.Policy.EffectiveEffectQueue;
        return _scheduler is null &&
            config.Enabled &&
            config.Backend == RuntimeTaskExecutorBackend.EffectQueue &&
            config.MaxQueueDepth > 0 &&
            _pending.Count >= config.MaxQueueDepth;
    }

    public async ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await Runtime.DispatchAsync(Program, Model, message, cancellationToken).ConfigureAwait(false);
        Model = result.Model;
        LastStep = result;
        if (result.EmittedQueueSpecs is null)
        {
            EnqueueRange(result.EmittedMessages);
        }
        else
        {
            EnqueueCommandMessages(new AppCommand<TMessage>(result.EmittedMessages, QueueSpecs: result.EmittedQueueSpecs));
        }

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
        while (PendingCount > 0 && results.Count < maxSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var schedulerEvidence = _scheduler?.Evidence();
            RecordSchedulerSelectTelemetry(schedulerEvidence);
            var next = DequeueNextPending();
            if (next is null)
            {
                break;
            }

            EffectSystem.RecordQueueProcessed(PendingCount);
            results.Add(await DispatchAsync(next, cancellationToken).ConfigureAwait(false));
        }

        return new RuntimeBatchResult<TModel, TMessage>(Model, results, PendingCount > 0);
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
