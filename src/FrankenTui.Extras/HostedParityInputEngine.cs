using FrankenTui.Core;
using FrankenTui.Runtime;

namespace FrankenTui.Extras;

public sealed class HostedParityInputEngine
{
    private readonly RuntimeInputEngine _engine;

    public HostedParityInputEngine(
        GestureConfig? gestureConfig = null,
        KeybindingConfig? keybindingConfig = null,
        CoalescerConfig? coalescerConfig = null)
    {
        _engine = new RuntimeInputEngine(gestureConfig, keybindingConfig, coalescerConfig);
    }

    public GestureRecognizer GestureRecognizer => _engine.GestureRecognizer;

    public KeybindingResolver KeybindingResolver => _engine.KeybindingResolver;

    public ResizeCoalescer ResizeCoalescer => _engine.ResizeCoalescer;

    public HostedParityInputOutcome Process(HostedParitySession session, TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(terminalEvent);

        var input = _engine.Process(
            Translate(terminalEvent),
            session.CreateKeybindingState());
        return new HostedParityInputOutcome(session.Advance(input), input);
    }

    public HostedParityInputOutcome Tick(HostedParitySession session, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);

        var input = _engine.Tick(session.CreateKeybindingState(), now);
        return new HostedParityInputOutcome(input.HasWork ? session.Advance(input) : session, input);
    }

    public static RuntimeInputTranslation Translate(TerminalEvent terminalEvent)
    {
        if (terminalEvent is not KeyTerminalEvent keyEvent ||
            !keyEvent.Gesture.IsCharacter ||
            keyEvent.Gesture.Character is not { } rune ||
            keyEvent.Gesture.Modifiers != TerminalModifiers.None)
        {
            return new RuntimeInputTranslation(terminalEvent, terminalEvent, Label: Describe(terminalEvent));
        }

        var lower = rune.ToString().ToLowerInvariant();
        var effective = lower switch
        {
            "h" => TerminalEvent.Key(new KeyGesture(TerminalKey.Left, TerminalModifiers.None), keyEvent.Timestamp),
            "j" => TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None), keyEvent.Timestamp),
            "k" => TerminalEvent.Key(new KeyGesture(TerminalKey.Up, TerminalModifiers.None), keyEvent.Timestamp),
            "l" => TerminalEvent.Key(new KeyGesture(TerminalKey.Right, TerminalModifiers.None), keyEvent.Timestamp),
            _ => terminalEvent
        };

        return new RuntimeInputTranslation(
            terminalEvent,
            effective,
            QuitRequested: string.Equals(lower, "q", StringComparison.OrdinalIgnoreCase),
            Label: Describe(terminalEvent));
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

public sealed record HostedParityInputOutcome(HostedParitySession Session, RuntimeInputEnvelope Input)
{
    public Size? ResizeToApply => Input.ResizeToApply;

    public bool QuitRequested => Input.QuitRequested;

    public bool HasWork => Input.HasWork;

    public string Label => Input.Label;
}
