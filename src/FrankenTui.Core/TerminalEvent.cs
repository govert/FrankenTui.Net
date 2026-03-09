using System.Text;

namespace FrankenTui.Core;

public abstract record TerminalEvent(DateTimeOffset Timestamp)
{
    public static KeyTerminalEvent Key(KeyGesture gesture, DateTimeOffset? timestamp = null) =>
        new(gesture, timestamp ?? DateTimeOffset.UtcNow);

    public static MouseTerminalEvent Mouse(MouseGesture gesture, DateTimeOffset? timestamp = null) =>
        new(gesture, timestamp ?? DateTimeOffset.UtcNow);

    public static ResizeTerminalEvent Resize(Size size, DateTimeOffset? timestamp = null) =>
        new(size, timestamp ?? DateTimeOffset.UtcNow);

    public static FocusTerminalEvent Focus(bool focused, DateTimeOffset? timestamp = null) =>
        new(focused, timestamp ?? DateTimeOffset.UtcNow);

    public static PasteTerminalEvent Paste(string text, DateTimeOffset? timestamp = null) =>
        new(text, timestamp ?? DateTimeOffset.UtcNow);

    public static HoverTerminalEvent Hover(ushort column, ushort row, bool stable, DateTimeOffset? timestamp = null) =>
        new(column, row, stable, timestamp ?? DateTimeOffset.UtcNow);
}

public sealed record KeyTerminalEvent(KeyGesture Gesture, DateTimeOffset Timestamp) : TerminalEvent(Timestamp);

public sealed record MouseTerminalEvent(MouseGesture Gesture, DateTimeOffset Timestamp) : TerminalEvent(Timestamp);

public sealed record ResizeTerminalEvent(Size Size, DateTimeOffset Timestamp) : TerminalEvent(Timestamp);

public sealed record FocusTerminalEvent(bool Focused, DateTimeOffset Timestamp) : TerminalEvent(Timestamp);

public sealed record PasteTerminalEvent(string Text, DateTimeOffset Timestamp) : TerminalEvent(Timestamp);

public sealed record HoverTerminalEvent(ushort Column, ushort Row, bool Stable, DateTimeOffset Timestamp) : TerminalEvent(Timestamp);
