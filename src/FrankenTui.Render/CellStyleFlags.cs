namespace FrankenTui.Render;

// Ported from crates/ftui-render/src/cell.rs (`StyleFlags`).
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// The established managed name CellStyleFlags is retained as a compatibility
// adaptation; all eight source bits map one-for-one.
[Flags]
public enum CellStyleFlags : byte
{
    None = 0,
    Bold = 1 << 0,
    Dim = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    Blink = 1 << 4,
    Reverse = 1 << 5,
    Strikethrough = 1 << 6,
    Hidden = 1 << 7,
    All = byte.MaxValue
}
