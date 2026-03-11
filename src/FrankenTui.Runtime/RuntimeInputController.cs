using FrankenTui.Core;
using System.Globalization;

namespace FrankenTui.Runtime;

public sealed class RuntimeInputController<TModel, TMessage>
{
    private readonly RuntimeInputEngine _engine;
    private readonly Func<TModel, KeybindingState> _keybindingStateFactory;
    private readonly Func<TModel, RuntimeInputEnvelope, IReadOnlyList<TMessage>> _messageFactory;
    private readonly Func<TModel, TerminalEvent, RuntimeInputTranslation> _translationFactory;

    public RuntimeInputController(
        Func<TModel, KeybindingState> keybindingStateFactory,
        Func<TModel, RuntimeInputEnvelope, IReadOnlyList<TMessage>> messageFactory,
        Func<TModel, TerminalEvent, RuntimeInputTranslation>? translationFactory = null,
        GestureConfig? gestureConfig = null,
        KeybindingConfig? keybindingConfig = null,
        CoalescerConfig? coalescerConfig = null)
    {
        _keybindingStateFactory = keybindingStateFactory ?? throw new ArgumentNullException(nameof(keybindingStateFactory));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _translationFactory = translationFactory ?? ((_, terminalEvent) => new RuntimeInputTranslation(terminalEvent, terminalEvent));
        _engine = new RuntimeInputEngine(gestureConfig, keybindingConfig, coalescerConfig);
    }

    public RuntimeInputEngine Engine => _engine;

    public async ValueTask<RuntimeInputDispatchResult<TModel, TMessage>> ProcessAsync(
        AppSession<TModel, TMessage> session,
        TerminalEvent terminalEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(terminalEvent);

        var input = _engine.Process(
            _translationFactory(session.Model, terminalEvent),
            _keybindingStateFactory(session.Model));
        return await ApplyAsync(session, input, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<RuntimeInputDispatchResult<TModel, TMessage>> TickAsync(
        AppSession<TModel, TMessage> session,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var input = _engine.Tick(_keybindingStateFactory(session.Model), now);
        return await ApplyAsync(session, input, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<RuntimeInputDispatchResult<TModel, TMessage>> ApplyAsync(
        AppSession<TModel, TMessage> session,
        RuntimeInputEnvelope input,
        CancellationToken cancellationToken)
    {
        if (input.ResizeToApply is { } size)
        {
            await session.Runtime.ResizeAsync(size, cancellationToken).ConfigureAwait(false);
        }

        if (!input.HasWork)
        {
            return new RuntimeInputDispatchResult<TModel, TMessage>(input, [], null, session.Model);
        }

        var messages = _messageFactory(session.Model, input) ?? [];
        EmitTelemetry(session, input, messages.Count);
        RuntimeBatchResult<TModel, TMessage>? batch = null;
        if (messages.Count > 0)
        {
            session.EnqueueRange(messages);
            batch = await session.DrainAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else if (input.ResizeToApply is not null)
        {
            await session.RenderCurrentAsync(cancellationToken).ConfigureAwait(false);
        }

        return new RuntimeInputDispatchResult<TModel, TMessage>(input, messages, batch, session.Model);
    }

    private static void EmitTelemetry(
        AppSession<TModel, TMessage> session,
        RuntimeInputEnvelope input,
        int messageCount)
    {
        if (!session.Runtime.Policy.EmitTelemetry)
        {
            return;
        }

        var fields = new List<TelemetryField>
        {
            new("decision_count", input.PolicyDecisions.Count.ToString(CultureInfo.InvariantCulture)),
            new("event_type", EventType(input.SourceEvent, input.IsTick)),
            new("message_count", messageCount.ToString(CultureInfo.InvariantCulture)),
            new("semantic_count", input.SemanticEvents.Count.ToString(CultureInfo.InvariantCulture)),
            new("tick", input.IsTick ? "true" : "false")
        };
        if (input.EffectiveEvent is not null &&
            !string.Equals(EventType(input.SourceEvent, input.IsTick), EventType(input.EffectiveEvent, isTick: false), StringComparison.Ordinal))
        {
            fields.Add(new TelemetryField("effective_event_type", EventType(input.EffectiveEvent, isTick: false)));
        }

        session.Runtime.Telemetry.Record(
            "ftui.input.event",
            TelemetryEventCategory.Input,
            session.Runtime.CurrentStepIndex,
            fields);

        if (input.ResizeDecision is not { } resize)
        {
            return;
        }

        session.Runtime.Telemetry.Record(
            "ftui.decision.resize",
            TelemetryEventCategory.Decision,
            session.Runtime.CurrentStepIndex,
            [
                new TelemetryField("coalesced", (resize.Action is not CoalesceAction.RenderNow).ToString().ToLowerInvariant()),
                new TelemetryField("debounce_active", (resize.Action is not CoalesceAction.RenderNow).ToString().ToLowerInvariant()),
                new TelemetryField("height", resize.Size.Height.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("rate_hz", resize.EventRate.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("same_size", "false"),
                new TelemetryField("strategy", resize.Action.ToString().ToLowerInvariant()),
                new TelemetryField("width", resize.Size.Width.ToString(CultureInfo.InvariantCulture))
            ]);

        var reflowEvent = input.ResizeToApply is null ? "ftui.reflow.placeholder" : "ftui.reflow.apply";
        session.Runtime.Telemetry.Record(
            reflowEvent,
            TelemetryEventCategory.RenderPipeline,
            session.Runtime.CurrentStepIndex,
            [
                new TelemetryField("debounce_ms", resize.CoalesceMs.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("height", resize.Size.Height.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("latency_ms", resize.CoalesceMs.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("rate_hz", resize.EventRate.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("width", resize.Size.Width.ToString(CultureInfo.InvariantCulture))
            ]);
    }

    private static string EventType(TerminalEvent? terminalEvent, bool isTick) =>
        terminalEvent switch
        {
            null when isTick => "tick",
            KeyTerminalEvent => "key",
            MouseTerminalEvent => "mouse",
            ResizeTerminalEvent => "resize",
            FocusTerminalEvent => "focus",
            PasteTerminalEvent => "paste",
            HoverTerminalEvent => "hover",
            null => "none",
            _ => terminalEvent.GetType().Name.ToLowerInvariant()
        };
}

public sealed record RuntimeInputDispatchResult<TModel, TMessage>(
    RuntimeInputEnvelope Input,
    IReadOnlyList<TMessage> Messages,
    RuntimeBatchResult<TModel, TMessage>? Batch,
    TModel Model);
