using FrankenTui.Core;

namespace FrankenTui.Runtime;

public sealed class RuntimeInputEngine
{
    private readonly GestureRecognizer _gestureRecognizer;
    private readonly KeybindingResolver _keybindingResolver;
    private readonly ResizeCoalescer _resizeCoalescer;

    public RuntimeInputEngine(
        GestureConfig? gestureConfig = null,
        KeybindingConfig? keybindingConfig = null,
        CoalescerConfig? coalescerConfig = null)
    {
        _gestureRecognizer = new GestureRecognizer(gestureConfig);
        _keybindingResolver = new KeybindingResolver(keybindingConfig);
        _resizeCoalescer = new ResizeCoalescer(coalescerConfig);
    }

    public GestureRecognizer GestureRecognizer => _gestureRecognizer;

    public KeybindingResolver KeybindingResolver => _keybindingResolver;

    public ResizeCoalescer ResizeCoalescer => _resizeCoalescer;

    public RuntimeInputEnvelope Process(RuntimeInputTranslation translation, KeybindingState state)
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(state);

        if (translation.EffectiveEvent is ResizeTerminalEvent resizeEvent)
        {
            var decision = _resizeCoalescer.Observe(resizeEvent.Size, translation.EffectiveEvent.Timestamp);
            return new RuntimeInputEnvelope(
                translation.SourceEvent,
                translation.EffectiveEvent,
                [],
                [],
                decision,
                _resizeCoalescer.ConsumeReadySize(decision.Action),
                translation.QuitRequested,
                HasWork: true,
                translation.Label ?? $"resize:{decision.Action.ToString().ToLowerInvariant()}",
                translation.EffectiveEvent.Timestamp);
        }

        var policyDecisions = _keybindingResolver.Resolve(translation.EffectiveEvent, state);
        var semanticEvents = _gestureRecognizer.Process(translation.EffectiveEvent);
        var quitRequested = translation.QuitRequested ||
            policyDecisions.Any(static decision =>
                decision.Action is KeybindingAction.Quit or KeybindingAction.SoftQuit or KeybindingAction.HardQuit);

        return new RuntimeInputEnvelope(
            translation.SourceEvent,
            translation.EffectiveEvent,
            semanticEvents,
            policyDecisions,
            null,
            null,
            quitRequested,
            HasWork: true,
            translation.Label ?? Describe(translation.SourceEvent),
            translation.EffectiveEvent.Timestamp);
    }

    public RuntimeInputEnvelope Tick(KeybindingState state, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);

        var policyDecisions = _keybindingResolver.FlushExpired(state, now);
        var semanticEvents = _gestureRecognizer.Flush(now);
        var resizeDecision = _resizeCoalescer.Evaluate(now);
        var resizeToApply = resizeDecision is null ? null : _resizeCoalescer.ConsumeReadySize(resizeDecision.Action);

        if (policyDecisions.Count == 0 &&
            semanticEvents.Count == 0 &&
            resizeDecision is null)
        {
            return RuntimeInputEnvelope.Idle(now);
        }

        var quitRequested = policyDecisions.Any(static decision =>
            decision.Action is KeybindingAction.Quit or KeybindingAction.SoftQuit or KeybindingAction.HardQuit);
        var label = resizeDecision is null ? "flush" : $"resize:{resizeDecision.Action.ToString().ToLowerInvariant()}";

        return new RuntimeInputEnvelope(
            null,
            null,
            semanticEvents,
            policyDecisions,
            resizeDecision,
            resizeToApply,
            quitRequested,
            HasWork: true,
            label,
            now,
            IsTick: true);
    }

    private static string Describe(TerminalEvent terminalEvent) =>
        terminalEvent switch
        {
            KeyTerminalEvent keyEvent when keyEvent.Gesture.IsCharacter && keyEvent.Gesture.Character is { } rune => $"key:{rune}",
            KeyTerminalEvent keyEvent => $"key:{keyEvent.Gesture.Key}",
            MouseTerminalEvent mouseEvent => $"mouse:{mouseEvent.Gesture.Kind}:{mouseEvent.Gesture.Column},{mouseEvent.Gesture.Row}",
            HoverTerminalEvent hoverEvent => $"hover:{hoverEvent.Column},{hoverEvent.Row}",
            PasteTerminalEvent => "paste",
            FocusTerminalEvent focusEvent => focusEvent.Focused ? "focus:gained" : "focus:lost",
            ResizeTerminalEvent resizeEvent => $"resize:{resizeEvent.Size.Width}x{resizeEvent.Size.Height}",
            _ => terminalEvent.GetType().Name
        };
}
