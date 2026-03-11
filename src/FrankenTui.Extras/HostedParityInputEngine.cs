using FrankenTui.Core;
using FrankenTui.Runtime;

namespace FrankenTui.Extras;

public sealed class HostedParityInputEngine
{
    private readonly GestureRecognizer _gestureRecognizer;
    private readonly KeybindingResolver _keybindingResolver;
    private readonly ResizeCoalescer _resizeCoalescer;

    public HostedParityInputEngine(
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

    public HostedParityInputOutcome Process(HostedParitySession session, TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(terminalEvent);

        if (terminalEvent is ResizeTerminalEvent resizeEvent)
        {
            var decision = _resizeCoalescer.Observe(resizeEvent.Size, resizeEvent.Timestamp);
            var readySize = _resizeCoalescer.ConsumeReadySize(decision.Action);
            var nextSession = session.Advance(terminalEvent, resizeDecision: decision);
            return new HostedParityInputOutcome(
                nextSession,
                readySize,
                QuitRequested: false,
                HasWork: true,
                Label: $"resize:{decision.Action.ToString().ToLowerInvariant()}");
        }

        var translatedEvent = TranslateAliases(terminalEvent);
        var policyDecisions = _keybindingResolver.Resolve(terminalEvent, session.CreateKeybindingState());
        var semanticEvents = _gestureRecognizer.Process(terminalEvent);
        var next = session.Advance(translatedEvent, semanticEvents, policyDecisions);
        var quitRequested = policyDecisions.Any(static action =>
                action.Action is KeybindingAction.Quit or KeybindingAction.SoftQuit or KeybindingAction.HardQuit) ||
            IsQuitAlias(terminalEvent);

        return new HostedParityInputOutcome(
            next,
            ResizeToApply: null,
            QuitRequested: quitRequested,
            HasWork: true,
            Label: Describe(terminalEvent));
    }

    public HostedParityInputOutcome Tick(HostedParitySession session, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);

        var policyDecisions = _keybindingResolver.FlushExpired(session.CreateKeybindingState(), now);
        var semanticEvents = _gestureRecognizer.Flush(now);
        var resizeDecision = _resizeCoalescer.Evaluate(now);
        var resizeToApply = resizeDecision is null ? null : _resizeCoalescer.ConsumeReadySize(resizeDecision.Action);

        if (policyDecisions.Count == 0 &&
            semanticEvents.Count == 0 &&
            resizeDecision is null)
        {
            return new HostedParityInputOutcome(session, null, QuitRequested: false, HasWork: false, Label: "idle");
        }

        var next = session.Advance(
            terminalEvent: null,
            semanticEvents: semanticEvents,
            policyActions: policyDecisions,
            resizeDecision: resizeDecision);
        var quitRequested = policyDecisions.Any(static action =>
            action.Action is KeybindingAction.Quit or KeybindingAction.SoftQuit or KeybindingAction.HardQuit);

        return new HostedParityInputOutcome(
            next,
            resizeToApply,
            quitRequested,
            HasWork: true,
            Label: resizeDecision is null ? "flush" : $"resize:{resizeDecision.Action.ToString().ToLowerInvariant()}");
    }

    private static bool IsQuitAlias(TerminalEvent terminalEvent) =>
        terminalEvent is KeyTerminalEvent keyEvent &&
        keyEvent.Gesture.IsCharacter &&
        keyEvent.Gesture.Character is { } rune &&
        keyEvent.Gesture.Modifiers == TerminalModifiers.None &&
        string.Equals(rune.ToString(), "q", StringComparison.OrdinalIgnoreCase);

    private static TerminalEvent TranslateAliases(TerminalEvent terminalEvent)
    {
        if (terminalEvent is not KeyTerminalEvent keyEvent ||
            !keyEvent.Gesture.IsCharacter ||
            keyEvent.Gesture.Character is not { } rune ||
            keyEvent.Gesture.Modifiers != TerminalModifiers.None)
        {
            return terminalEvent;
        }

        return rune.ToString().ToLowerInvariant() switch
        {
            "h" => TerminalEvent.Key(new KeyGesture(TerminalKey.Left, TerminalModifiers.None), keyEvent.Timestamp),
            "j" => TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None), keyEvent.Timestamp),
            "k" => TerminalEvent.Key(new KeyGesture(TerminalKey.Up, TerminalModifiers.None), keyEvent.Timestamp),
            "l" => TerminalEvent.Key(new KeyGesture(TerminalKey.Right, TerminalModifiers.None), keyEvent.Timestamp),
            _ => terminalEvent
        };
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

public sealed record HostedParityInputOutcome(
    HostedParitySession Session,
    Size? ResizeToApply,
    bool QuitRequested,
    bool HasWork,
    string Label);
