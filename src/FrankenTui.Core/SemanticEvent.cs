namespace FrankenTui.Core;

public readonly record struct TerminalPoint(ushort Column, ushort Row)
{
    public static TerminalPoint From(ushort column, ushort row) => new(column, row);
}

public abstract record SemanticEvent(DateTimeOffset Timestamp)
{
    public static ClickSemanticEvent Click(TerminalPoint position, TerminalMouseButton button, DateTimeOffset? timestamp = null) =>
        new(position, button, timestamp ?? DateTimeOffset.UtcNow);

    public static DoubleClickSemanticEvent DoubleClick(TerminalPoint position, TerminalMouseButton button, DateTimeOffset? timestamp = null) =>
        new(position, button, timestamp ?? DateTimeOffset.UtcNow);

    public static TripleClickSemanticEvent TripleClick(TerminalPoint position, TerminalMouseButton button, DateTimeOffset? timestamp = null) =>
        new(position, button, timestamp ?? DateTimeOffset.UtcNow);

    public static LongPressSemanticEvent LongPress(
        TerminalPoint position,
        TerminalMouseButton button,
        TimeSpan duration,
        DateTimeOffset? timestamp = null) =>
        new(position, button, duration, timestamp ?? DateTimeOffset.UtcNow);

    public static DragStartSemanticEvent DragStart(
        TerminalPoint origin,
        TerminalPoint position,
        TerminalMouseButton button,
        DateTimeOffset? timestamp = null) =>
        new(origin, position, button, timestamp ?? DateTimeOffset.UtcNow);

    public static DragMoveSemanticEvent DragMove(
        TerminalPoint from,
        TerminalPoint to,
        short deltaColumn,
        short deltaRow,
        DateTimeOffset? timestamp = null) =>
        new(from, to, deltaColumn, deltaRow, timestamp ?? DateTimeOffset.UtcNow);

    public static DragEndSemanticEvent DragEnd(
        TerminalPoint origin,
        TerminalPoint position,
        TerminalMouseButton button,
        DateTimeOffset? timestamp = null) =>
        new(origin, position, button, timestamp ?? DateTimeOffset.UtcNow);

    public static DragCancelSemanticEvent DragCancel(string reason, DateTimeOffset? timestamp = null) =>
        new(reason, timestamp ?? DateTimeOffset.UtcNow);

    public static ChordSemanticEvent Chord(IReadOnlyList<KeyGesture> sequence, DateTimeOffset? timestamp = null) =>
        new(sequence, timestamp ?? DateTimeOffset.UtcNow);
}

public sealed record ClickSemanticEvent(
    TerminalPoint Position,
    TerminalMouseButton Button,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record DoubleClickSemanticEvent(
    TerminalPoint Position,
    TerminalMouseButton Button,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record TripleClickSemanticEvent(
    TerminalPoint Position,
    TerminalMouseButton Button,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record LongPressSemanticEvent(
    TerminalPoint Position,
    TerminalMouseButton Button,
    TimeSpan Duration,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record DragStartSemanticEvent(
    TerminalPoint Origin,
    TerminalPoint Position,
    TerminalMouseButton Button,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record DragMoveSemanticEvent(
    TerminalPoint From,
    TerminalPoint To,
    short DeltaColumn,
    short DeltaRow,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record DragEndSemanticEvent(
    TerminalPoint Origin,
    TerminalPoint Position,
    TerminalMouseButton Button,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record DragCancelSemanticEvent(
    string Reason,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);

public sealed record ChordSemanticEvent(
    IReadOnlyList<KeyGesture> Sequence,
    DateTimeOffset Timestamp) : SemanticEvent(Timestamp);
