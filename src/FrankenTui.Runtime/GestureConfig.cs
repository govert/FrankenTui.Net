namespace FrankenTui.Runtime;

public sealed record GestureConfig(
    TimeSpan DoubleClickTimeout,
    TimeSpan LongPressThreshold,
    ushort DragThreshold,
    TimeSpan ChordTimeout,
    bool EnableLongPress,
    bool EnableChord)
{
    public static GestureConfig Default { get; } = new(
        TimeSpan.FromMilliseconds(300),
        TimeSpan.FromMilliseconds(500),
        3,
        TimeSpan.FromMilliseconds(1000),
        EnableLongPress: true,
        EnableChord: true);
}
