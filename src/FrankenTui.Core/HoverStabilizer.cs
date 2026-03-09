namespace FrankenTui.Core;

public sealed class HoverStabilizer
{
    private MouseGesture? _lastGesture;
    private int _repeatCount;

    public int StableAfterRepeats { get; init; } = 2;

    public HoverTerminalEvent? Update(MouseGesture gesture, DateTimeOffset? timestamp = null)
    {
        if (_lastGesture is { } previous &&
            previous.Column == gesture.Column &&
            previous.Row == gesture.Row &&
            gesture.Kind == TerminalMouseKind.Move)
        {
            _repeatCount++;
        }
        else
        {
            _repeatCount = 1;
            _lastGesture = gesture;
        }

        var stable = _repeatCount >= StableAfterRepeats;
        return gesture.Kind == TerminalMouseKind.Move
            ? TerminalEvent.Hover(gesture.Column, gesture.Row, stable, timestamp)
            : null;
    }
}
