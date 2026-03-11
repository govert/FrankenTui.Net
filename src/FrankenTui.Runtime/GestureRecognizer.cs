using System.Text.Json;
using FrankenTui.Core;

namespace FrankenTui.Runtime;

public sealed class GestureRecognizer
{
    private readonly List<KeyGesture> _pendingChord = [];
    private MouseContact? _mouseContact;
    private TerminalPoint? _dragLastPoint;
    private ClickInfo? _lastClick;
    private DateTimeOffset _lastChordTimestamp = DateTimeOffset.MinValue;

    public GestureRecognizer(GestureConfig? config = null)
    {
        Config = config ?? GestureConfig.Default;
    }

    public GestureConfig Config { get; }

    public SemanticEvidenceLedger Ledger { get; } = new();

    public bool IsDragging => _dragLastPoint is not null;

    public IReadOnlyList<SemanticEvent> Process(TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(terminalEvent);

        var events = new List<SemanticEvent>(2);
        EmitLongPressIfDue(terminalEvent.Timestamp, events);

        switch (terminalEvent)
        {
            case MouseTerminalEvent mouseEvent:
                ProcessMouse(mouseEvent, events);
                ResetChordIfActive(mouseEvent.Timestamp, "mouse");
                break;
            case KeyTerminalEvent keyEvent:
                ProcessKey(keyEvent, events);
                break;
            case ResizeTerminalEvent:
                CancelDrag("resize", terminalEvent.Timestamp, events);
                ResetChordIfActive(terminalEvent.Timestamp, "resize");
                break;
            case FocusTerminalEvent focusEvent when !focusEvent.Focused:
                CancelDrag("focus_lost", terminalEvent.Timestamp, events);
                ResetChordIfActive(terminalEvent.Timestamp, "focus_lost");
                break;
            default:
                ResetChordIfActive(terminalEvent.Timestamp, terminalEvent.GetType().Name);
                break;
        }

        if (events.Count == 0)
        {
            Ledger.Record(terminalEvent.Timestamp, DescribeRaw(terminalEvent), "ignore", null, null, null);
        }

        return events;
    }

    public IReadOnlyList<SemanticEvent> Flush(DateTimeOffset now)
    {
        var events = new List<SemanticEvent>(1);
        EmitLongPressIfDue(now, events);
        return events;
    }

    public void Reset()
    {
        _pendingChord.Clear();
        _mouseContact = null;
        _dragLastPoint = null;
        _lastClick = null;
    }

    private void ProcessKey(KeyTerminalEvent keyEvent, List<SemanticEvent> events)
    {
        if (!Config.EnableChord)
        {
            return;
        }

        if (_pendingChord.Count > 0 &&
            keyEvent.Timestamp - _lastChordTimestamp > Config.ChordTimeout)
        {
            _pendingChord.Clear();
        }

        if (_pendingChord.Count == 0 ||
            keyEvent.Timestamp - _lastChordTimestamp <= Config.ChordTimeout)
        {
            _pendingChord.Add(keyEvent.Gesture);
            _lastChordTimestamp = keyEvent.Timestamp;
            if (_pendingChord.Count >= 2)
            {
                var chord = SemanticEvent.Chord(_pendingChord.ToArray(), keyEvent.Timestamp);
                events.Add(chord);
                Ledger.Record(keyEvent.Timestamp, DescribeRaw(keyEvent), "emit", DescribeSemantic(chord), null, null);
            }
        }
        else
        {
            _pendingChord.Clear();
            _pendingChord.Add(keyEvent.Gesture);
            _lastChordTimestamp = keyEvent.Timestamp;
        }
    }

    private void ProcessMouse(MouseTerminalEvent mouseEvent, List<SemanticEvent> events)
    {
        var point = TerminalPoint.From(mouseEvent.Gesture.Column, mouseEvent.Gesture.Row);
        switch (mouseEvent.Gesture.Kind)
        {
            case TerminalMouseKind.Down:
                _mouseContact = new MouseContact(point, mouseEvent.Gesture.Button, mouseEvent.Timestamp);
                _dragLastPoint = null;
                break;
            case TerminalMouseKind.Move:
            case TerminalMouseKind.Drag:
                ProcessDrag(mouseEvent, point, events);
                break;
            case TerminalMouseKind.Up:
                ProcessMouseRelease(mouseEvent, point, events);
                break;
        }
    }

    private void ProcessDrag(MouseTerminalEvent mouseEvent, TerminalPoint point, List<SemanticEvent> events)
    {
        if (_mouseContact is null)
        {
            return;
        }

        var origin = _mouseContact.Value.Position;
        var moved = Distance(origin, point);
        if (_dragLastPoint is null && moved >= Config.DragThreshold)
        {
            var start = SemanticEvent.DragStart(origin, point, _mouseContact.Value.Button, mouseEvent.Timestamp);
            _dragLastPoint = point;
            events.Add(start);
            Ledger.Record(mouseEvent.Timestamp, DescribeRaw(mouseEvent), "emit", DescribeSemantic(start), $"{point.Column},{point.Row}", null);
            return;
        }

        if (_dragLastPoint is { } from)
        {
            var move = SemanticEvent.DragMove(
                from,
                point,
                (short)(point.Column - from.Column),
                (short)(point.Row - from.Row),
                mouseEvent.Timestamp);
            _dragLastPoint = point;
            events.Add(move);
            Ledger.Record(mouseEvent.Timestamp, DescribeRaw(mouseEvent), "emit", DescribeSemantic(move), $"{point.Column},{point.Row}", null);
        }
    }

    private void ProcessMouseRelease(MouseTerminalEvent mouseEvent, TerminalPoint point, List<SemanticEvent> events)
    {
        if (_mouseContact is null)
        {
            return;
        }

        if (_dragLastPoint is { })
        {
            var dragEnd = SemanticEvent.DragEnd(_mouseContact.Value.Position, point, _mouseContact.Value.Button, mouseEvent.Timestamp);
            events.Add(dragEnd);
            Ledger.Record(mouseEvent.Timestamp, DescribeRaw(mouseEvent), "emit", DescribeSemantic(dragEnd), $"{point.Column},{point.Row}", null);
            _mouseContact = null;
            _dragLastPoint = null;
            return;
        }

        var duration = mouseEvent.Timestamp - _mouseContact.Value.Timestamp;
        if (Config.EnableLongPress && duration >= Config.LongPressThreshold)
        {
            var longPress = SemanticEvent.LongPress(point, _mouseContact.Value.Button, duration, mouseEvent.Timestamp);
            events.Add(longPress);
            Ledger.Record(mouseEvent.Timestamp, DescribeRaw(mouseEvent), "emit", DescribeSemantic(longPress), $"{point.Column},{point.Row}", duration);
            _mouseContact = null;
            return;
        }

        if (_mouseContact.Value.Position == point)
        {
            var clickCount = 1;
            if (_lastClick is { } last &&
                last.Position == point &&
                last.Button == _mouseContact.Value.Button &&
                mouseEvent.Timestamp - last.Timestamp <= Config.DoubleClickTimeout)
            {
                clickCount = Math.Min(last.Count + 1, 3);
            }

            var semantic = clickCount switch
            {
                2 => (SemanticEvent)SemanticEvent.DoubleClick(point, _mouseContact.Value.Button, mouseEvent.Timestamp),
                3 => SemanticEvent.TripleClick(point, _mouseContact.Value.Button, mouseEvent.Timestamp),
                _ => SemanticEvent.Click(point, _mouseContact.Value.Button, mouseEvent.Timestamp)
            };

            _lastClick = new ClickInfo(point, _mouseContact.Value.Button, mouseEvent.Timestamp, clickCount);
            events.Add(semantic);
            Ledger.Record(mouseEvent.Timestamp, DescribeRaw(mouseEvent), "emit", DescribeSemantic(semantic), $"{point.Column},{point.Row}", duration);
        }
        else
        {
            Ledger.Record(mouseEvent.Timestamp, DescribeRaw(mouseEvent), "cancel", "click_moved", $"{point.Column},{point.Row}", duration);
        }

        _mouseContact = null;
    }

    private void EmitLongPressIfDue(DateTimeOffset now, List<SemanticEvent> events)
    {
        if (!Config.EnableLongPress ||
            _mouseContact is not { } contact ||
            _dragLastPoint is not null)
        {
            return;
        }

        var duration = now - contact.Timestamp;
        if (duration < Config.LongPressThreshold)
        {
            return;
        }

        var semantic = SemanticEvent.LongPress(contact.Position, contact.Button, duration, now);
        events.Add(semantic);
        Ledger.Record(now, "timer", "emit", DescribeSemantic(semantic), $"{contact.Position.Column},{contact.Position.Row}", duration);
        _mouseContact = null;
    }

    private void CancelDrag(string reason, DateTimeOffset timestamp, List<SemanticEvent> events)
    {
        if (_dragLastPoint is null)
        {
            return;
        }

        var semantic = SemanticEvent.DragCancel(reason, timestamp);
        events.Add(semantic);
        Ledger.Record(timestamp, reason, "emit", DescribeSemantic(semantic), null, null);
        _mouseContact = null;
        _dragLastPoint = null;
    }

    private void ResetChordIfActive(DateTimeOffset timestamp, string reason)
    {
        if (_pendingChord.Count == 0)
        {
            return;
        }

        Ledger.Record(timestamp, reason, "cancel", "chord_reset", null, null);
        _pendingChord.Clear();
        _lastChordTimestamp = DateTimeOffset.MinValue;
    }

    private static int Distance(TerminalPoint origin, TerminalPoint point) =>
        Math.Max(Math.Abs(point.Column - origin.Column), Math.Abs(point.Row - origin.Row));

    private static string DescribeRaw(TerminalEvent terminalEvent) =>
        terminalEvent switch
        {
            KeyTerminalEvent keyEvent => keyEvent.Gesture.IsCharacter && keyEvent.Gesture.Character is { } rune
                ? $"key:{rune}:{keyEvent.Gesture.Modifiers}"
                : $"key:{keyEvent.Gesture.Key}:{keyEvent.Gesture.Modifiers}",
            MouseTerminalEvent mouseEvent => $"mouse:{mouseEvent.Gesture.Kind}:{mouseEvent.Gesture.Button}:{mouseEvent.Gesture.Column},{mouseEvent.Gesture.Row}",
            ResizeTerminalEvent resizeEvent => $"resize:{resizeEvent.Size.Width}x{resizeEvent.Size.Height}",
            FocusTerminalEvent focusEvent => focusEvent.Focused ? "focus:gained" : "focus:lost",
            PasteTerminalEvent pasteEvent => $"paste:{pasteEvent.Text.Length}",
            HoverTerminalEvent hoverEvent => $"hover:{hoverEvent.Column},{hoverEvent.Row}",
            _ => terminalEvent.GetType().Name
        };

    private static string DescribeSemantic(SemanticEvent semanticEvent) =>
        semanticEvent switch
        {
            ClickSemanticEvent click => $"click:{click.Button}:{click.Position.Column},{click.Position.Row}",
            DoubleClickSemanticEvent click => $"double_click:{click.Button}:{click.Position.Column},{click.Position.Row}",
            TripleClickSemanticEvent click => $"triple_click:{click.Button}:{click.Position.Column},{click.Position.Row}",
            LongPressSemanticEvent longPress => $"long_press:{longPress.Button}:{longPress.Position.Column},{longPress.Position.Row}:{longPress.Duration.TotalMilliseconds:0}",
            DragStartSemanticEvent dragStart => $"drag_start:{dragStart.Button}:{dragStart.Position.Column},{dragStart.Position.Row}",
            DragMoveSemanticEvent dragMove => $"drag_move:{dragMove.From.Column},{dragMove.From.Row}->{dragMove.To.Column},{dragMove.To.Row}",
            DragEndSemanticEvent dragEnd => $"drag_end:{dragEnd.Button}:{dragEnd.Position.Column},{dragEnd.Position.Row}",
            DragCancelSemanticEvent dragCancel => $"drag_cancel:{dragCancel.Reason}",
            ChordSemanticEvent chord => $"chord:{string.Join('+', chord.Sequence.Select(static key => key.IsCharacter && key.Character is { } rune ? rune.ToString() : key.Key.ToString()))}",
            _ => semanticEvent.GetType().Name
        };

    private readonly record struct MouseContact(TerminalPoint Position, TerminalMouseButton Button, DateTimeOffset Timestamp);

    private readonly record struct ClickInfo(TerminalPoint Position, TerminalMouseButton Button, DateTimeOffset Timestamp, int Count);
}

public sealed class SemanticEvidenceLedger
{
    private readonly List<SemanticDecisionRecord> _entries = [];

    public IReadOnlyList<SemanticDecisionRecord> Entries => _entries;

    public void Record(
        DateTimeOffset timestamp,
        string rawEvent,
        string decision,
        string? semanticEvent,
        string? position,
        TimeSpan? duration)
    {
        _entries.Add(new SemanticDecisionRecord(
            timestamp,
            rawEvent,
            decision,
            semanticEvent,
            position,
            duration));
    }

    public string ToJson() =>
        JsonSerializer.Serialize(
            _entries,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });
}

public sealed record SemanticDecisionRecord(
    DateTimeOffset Timestamp,
    string RawEvent,
    string Decision,
    string? SemanticEvent,
    string? Position,
    TimeSpan? Duration);
