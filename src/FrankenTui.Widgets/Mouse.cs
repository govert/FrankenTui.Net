// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/event.rs
// Canonical input/event types: KeyCode, KeyEvent, KeyModifiers, KeyEventKind,
// MouseEvent, MouseEventKind, MouseButton, and shared MouseResult for widget mouse handling.

using FrankenTui.Core;

namespace FrankenTui.Widgets;

// ── KeyCode ────────────────────────────────────────────────────────────────────
// Upstream: KeyCode enum with associated data (Char(char), F(u8)).
// Enums with data map to a closed abstract record hierarchy per AGENTS.md.
// Singletons (no data) use static readonly instances; equality is value-based via records.

/// <summary>Key codes for keyboard events. Port of ftui_core::event::KeyCode.</summary>
public abstract record KeyCode
{
    private KeyCode() { }

    // ── Variants with associated data ─────────────────────────────────────────

    /// <summary>A regular character key.</summary>
    public sealed record Char(char Character) : KeyCode;

    /// <summary>Function key (F1-F24).</summary>
    public sealed record F(byte Number) : KeyCode;

    // ── Singleton variants (no associated data) ───────────────────────────────

    /// <summary>Enter/Return key.</summary>
    public sealed record Enter : KeyCode;
    /// <summary>Escape key.</summary>
    public sealed record Escape : KeyCode;
    /// <summary>Backspace key.</summary>
    public sealed record Backspace : KeyCode;
    /// <summary>Tab key.</summary>
    public sealed record Tab : KeyCode;
    /// <summary>Shift+Tab (back-tab).</summary>
    public sealed record BackTab : KeyCode;
    /// <summary>Delete key.</summary>
    public sealed record Delete : KeyCode;
    /// <summary>Insert key.</summary>
    public sealed record Insert : KeyCode;
    /// <summary>Home key.</summary>
    public sealed record Home : KeyCode;
    /// <summary>End key.</summary>
    public sealed record End : KeyCode;
    /// <summary>Page Up key.</summary>
    public sealed record PageUp : KeyCode;
    /// <summary>Page Down key.</summary>
    public sealed record PageDown : KeyCode;
    /// <summary>Up arrow key.</summary>
    public sealed record Up : KeyCode;
    /// <summary>Down arrow key.</summary>
    public sealed record Down : KeyCode;
    /// <summary>Left arrow key.</summary>
    public sealed record Left : KeyCode;
    /// <summary>Right arrow key.</summary>
    public sealed record Right : KeyCode;
    /// <summary>Null character (Ctrl+Space or Ctrl+@).</summary>
    public sealed record Null : KeyCode;
    /// <summary>Media Play/Pause key.</summary>
    public sealed record MediaPlayPause : KeyCode;
    /// <summary>Media Stop key.</summary>
    public sealed record MediaStop : KeyCode;
    /// <summary>Media Next Track key.</summary>
    public sealed record MediaNextTrack : KeyCode;
    /// <summary>Media Previous Track key.</summary>
    public sealed record MediaPrevTrack : KeyCode;
}

// ── KeyEventKind ───────────────────────────────────────────────────────────────

/// <summary>The type of key event. Port of ftui_core::event::KeyEventKind.</summary>
public enum KeyEventKind
{
    /// <summary>Key was pressed (default when not distinguishable).</summary>
    Press,
    /// <summary>Key is being held (repeat event).</summary>
    Repeat,
    /// <summary>Key was released.</summary>
    Release,
}

// ── KeyModifiers ───────────────────────────────────────────────────────────────

/// <summary>Modifier keys that can be held during a key event. Port of ftui_core::event::Modifiers.</summary>
[Flags]
public enum KeyModifiers : byte
{
    /// <summary>No modifiers.</summary>
    None  = 0b0000,
    /// <summary>Shift key.</summary>
    Shift = 0b0001,
    /// <summary>Alt/Option key.</summary>
    Alt   = 0b0010,
    /// <summary>Control key.</summary>
    Ctrl  = 0b0100,
    /// <summary>Super/Meta/Command key.</summary>
    Super = 0b1000,
}

// ── KeyEvent ───────────────────────────────────────────────────────────────────

/// <summary>A keyboard event. Port of ftui_core::event::KeyEvent.</summary>
public sealed class KeyEvent
{
    /// <summary>The key code that was pressed.</summary>
    public KeyCode Code { get; }

    /// <summary>Modifier keys held during the event.</summary>
    public KeyModifiers Modifiers { get; }

    /// <summary>The type of key event (press, repeat, or release).</summary>
    public KeyEventKind Kind { get; }

    /// <summary>Convenience accessor: the character for a <see cref="KeyCode.Char"/> code, else null.</summary>
    public char? Character => (Code as KeyCode.Char)?.Character;

    public KeyEvent(KeyCode code, KeyModifiers modifiers = KeyModifiers.None, KeyEventKind kind = KeyEventKind.Press)
    {
        Code = code; Modifiers = modifiers; Kind = kind;
    }

    /// <summary>Create a new key event with default modifiers and Press kind.</summary>
    public static KeyEvent New(KeyCode code) => new(code);

    /// <summary>Create a key event for a character key.</summary>
    public static KeyEvent Char(char c) => new(new KeyCode.Char(c));

    /// <summary>Create a key event with modifiers.</summary>
    public static KeyEvent WithModifiers(KeyCode code, KeyModifiers modifiers) => new(code, modifiers);

    /// <summary>Check if Ctrl modifier is held.</summary>
    public bool Ctrl() => (Modifiers & KeyModifiers.Ctrl) != 0;

    /// <summary>Check if Alt modifier is held.</summary>
    public bool Alt() => (Modifiers & KeyModifiers.Alt) != 0;

    /// <summary>Check if Shift modifier is held.</summary>
    public bool Shift() => (Modifiers & KeyModifiers.Shift) != 0;

    /// <summary>Check if Super/Meta/Cmd modifier is held.</summary>
    public bool SuperKey() => (Modifiers & KeyModifiers.Super) != 0;

    /// <summary>Check if this is a specific character key.</summary>
    public bool IsChar(char c) => Code is KeyCode.Char ch && ch.Character == c;
}

// ── MouseButton ───────────────────────────────────────────────────────────────

/// <summary>Mouse button identifiers. Port of ftui_core::event::MouseButton.</summary>
public enum MouseButton
{
    /// <summary>Left mouse button.</summary>
    Left,
    /// <summary>Right mouse button.</summary>
    Right,
    /// <summary>Middle mouse button (scroll wheel click).</summary>
    Middle,
}

// ── MouseEventKind ────────────────────────────────────────────────────────────
// Upstream: MouseEventKind enum with associated data (Down(MouseButton), Up(MouseButton), Drag(MouseButton)).
// Map to closed abstract record hierarchy per AGENTS.md.

/// <summary>The type of mouse event. Port of ftui_core::event::MouseEventKind.</summary>
public abstract record MouseEventKind
{
    private MouseEventKind() { }

    /// <summary>Mouse button pressed down.</summary>
    public sealed record Down(MouseButton Button) : MouseEventKind;
    /// <summary>Mouse button released.</summary>
    public sealed record Up(MouseButton Button) : MouseEventKind;
    /// <summary>Mouse dragged while button held.</summary>
    public sealed record Drag(MouseButton Button) : MouseEventKind;
    /// <summary>Mouse moved (no button pressed).</summary>
    public sealed record Moved : MouseEventKind { public static readonly Moved Instance = new(); }
    /// <summary>Mouse wheel scrolled up.</summary>
    public sealed record ScrollUp : MouseEventKind { public static readonly ScrollUp Instance = new(); }
    /// <summary>Mouse wheel scrolled down.</summary>
    public sealed record ScrollDown : MouseEventKind { public static readonly ScrollDown Instance = new(); }
    /// <summary>Mouse wheel scrolled left (horizontal scroll).</summary>
    public sealed record ScrollLeft : MouseEventKind { public static readonly ScrollLeft Instance = new(); }
    /// <summary>Mouse wheel scrolled right (horizontal scroll).</summary>
    public sealed record ScrollRight : MouseEventKind { public static readonly ScrollRight Instance = new(); }
}

// ── MouseEvent ────────────────────────────────────────────────────────────────

/// <summary>A mouse event. Port of ftui_core::event::MouseEvent.</summary>
public sealed class MouseEvent
{
    /// <summary>The type of mouse event.</summary>
    public MouseEventKind Kind { get; }

    /// <summary>X coordinate (0-indexed, leftmost column is 0).</summary>
    public ushort X { get; }

    /// <summary>Y coordinate (0-indexed, topmost row is 0).</summary>
    public ushort Y { get; }

    /// <summary>Modifier keys held during the event.</summary>
    public KeyModifiers Modifiers { get; }

    public MouseEvent(MouseEventKind kind, ushort x, ushort y, KeyModifiers modifiers = KeyModifiers.None)
    {
        Kind = kind; X = x; Y = y; Modifiers = modifiers;
    }

    /// <summary>Create a new mouse event.</summary>
    public static MouseEvent New(MouseEventKind kind, ushort x, ushort y) => new(kind, x, y);

    /// <summary>Get the position as a tuple.</summary>
    public (ushort x, ushort y) Position() => (X, Y);
}

// ── MouseResult ───────────────────────────────────────────────────────────────

/// <summary>Result of processing a mouse event on a widget.</summary>
/// <remarks>
/// Rust enum variants with associated data map to a closed class hierarchy per AGENTS.md.
/// Variants <see cref="Selected"/> and <see cref="Activated"/> carry the item index.
/// </remarks>
public abstract class MouseResult
{
    MouseResult() { }

    /// <summary>Event not relevant to this widget.</summary>
    public sealed class IgnoredCase : MouseResult { public static readonly IgnoredCase Instance = new(); }

    /// <summary>Selection changed to the given index.</summary>
    public sealed class SelectedCase : MouseResult
    {
        public int Index { get; }
        public SelectedCase(int index) => Index = index;
        public override bool Equals(object? obj) => obj is SelectedCase s && s.Index == Index;
        public override int GetHashCode() => HashCode.Combine(nameof(SelectedCase), Index);
    }

    /// <summary>Item activated (double-click, expand/collapse).</summary>
    public sealed class ActivatedCase : MouseResult
    {
        public int Index { get; }
        public ActivatedCase(int index) => Index = index;
        public override bool Equals(object? obj) => obj is ActivatedCase a && a.Index == Index;
        public override int GetHashCode() => HashCode.Combine(nameof(ActivatedCase), Index);
    }

    /// <summary>Scroll position changed.</summary>
    public sealed class ScrolledCase : MouseResult { public static readonly ScrolledCase Instance = new(); }

    /// <summary>Hover state changed.</summary>
    public sealed class HoverChangedCase : MouseResult { public static readonly HoverChangedCase Instance = new(); }

    // ── Convenience factory properties matching Rust variant names ────────

    /// <summary>Event not relevant to this widget.</summary>
    public static MouseResult Ignored => IgnoredCase.Instance;
    /// <summary>Selection changed to the given index.</summary>
    public static MouseResult Selected(int index) => new SelectedCase(index);
    /// <summary>Item activated at the given index.</summary>
    public static MouseResult Activated(int index) => new ActivatedCase(index);
    /// <summary>Scroll position changed.</summary>
    public static MouseResult Scrolled => ScrolledCase.Instance;
    /// <summary>Hover state changed.</summary>
    public static MouseResult HoverChanged => HoverChangedCase.Instance;
}
