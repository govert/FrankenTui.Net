using FrankenTui.Core;

namespace FrankenTui.Widgets;

/// <summary>Mouse event support utilities. Matches upstream mouse.rs.</summary>
public static class MouseSupport
{
    /// <summary>Check if a mouse event position is within a rectangle.</summary>
    public static bool Contains(this Rect rect, ushort x, ushort y) =>
        x >= rect.X && x < rect.Right && y >= rect.Y && y < rect.Bottom;

    /// <summary>Check if a mouse gesture is within a rectangle.</summary>
    public static bool Contains(this Rect rect, MouseGesture gesture) =>
        rect.Contains(gesture.Column, gesture.Row);
}
