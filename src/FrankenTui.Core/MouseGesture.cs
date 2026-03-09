namespace FrankenTui.Core;

public readonly record struct MouseGesture(
    ushort Column,
    ushort Row,
    TerminalMouseButton Button,
    TerminalMouseKind Kind,
    TerminalModifiers Modifiers = TerminalModifiers.None);
