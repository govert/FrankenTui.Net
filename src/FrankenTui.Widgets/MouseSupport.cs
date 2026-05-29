// Upstream source: crates/ftui-widgets/src/mouse.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Ported from Rust MouseResult enum — direct 1-1 port.

namespace FrankenTui.Widgets;

/// <summary>
/// Result of processing a mouse event on a widget.
/// </summary>
public enum MouseResult
{
    /// <summary>Event not relevant to this widget.</summary>
    Ignored,
    /// <summary>Selection changed to the given index.</summary>
    Selected, // carries usize index upstream; caller tracks the index separately
    /// <summary>Item activated (double-click, expand/collapse).</summary>
    Activated, // carries usize index upstream; caller tracks the index separately
    /// <summary>Scroll position changed.</summary>
    Scrolled,
    /// <summary>Hover state changed.</summary>
    HoverChanged,
}
