using System.Globalization;
using System.Text;

namespace FrankenTui.Core;

public sealed class TerminalInputParser
{
    public IReadOnlyList<TerminalEvent> Parse(ReadOnlySpan<byte> payload, DateTimeOffset? timestamp = null)
    {
        var text = Encoding.UTF8.GetString(payload);
        return Parse(text, timestamp);
    }

    public IReadOnlyList<TerminalEvent> Parse(string text, DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var stamp = timestamp ?? DateTimeOffset.UtcNow;
        var events = new List<TerminalEvent>();

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (current == '\u001b')
            {
                if (TryParsePaste(text, index, stamp, out var paste, out var consumed))
                {
                    events.Add(paste);
                    index += consumed - 1;
                    continue;
                }

                if (TryParseCsi(text, index, stamp, out var parsed, out consumed))
                {
                    events.Add(parsed);
                    index += consumed - 1;
                    continue;
                }

                events.Add(TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), stamp));
                continue;
            }

            if (char.IsHighSurrogate(current) &&
                index + 1 < text.Length &&
                char.IsLowSurrogate(text[index + 1]))
            {
                events.Add(TerminalEvent.Key(
                    new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(current, text[index + 1])),
                    stamp));
                index++;
                continue;
            }

            events.Add(current switch
            {
                '\r' or '\n' => TerminalEvent.Key(new KeyGesture(TerminalKey.Enter, TerminalModifiers.None), stamp),
                '\t' => TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None), stamp),
                '\b' or '\u007f' => TerminalEvent.Key(new KeyGesture(TerminalKey.Backspace, TerminalModifiers.None), stamp),
                _ => TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(current)), stamp)
            });
        }

        return TerminalEventCoalescer.Coalesce(events);
    }

    private static bool TryParsePaste(
        string text,
        int index,
        DateTimeOffset timestamp,
        out PasteTerminalEvent parsed,
        out int consumed)
    {
        const string pasteStart = "\u001b[200~";
        const string pasteEnd = "\u001b[201~";

        parsed = null!;
        consumed = 0;
        if (!text.AsSpan(index).StartsWith(pasteStart, StringComparison.Ordinal))
        {
            return false;
        }

        var contentStart = index + pasteStart.Length;
        var endIndex = text.IndexOf(pasteEnd, contentStart, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return false;
        }

        parsed = TerminalEvent.Paste(text[contentStart..endIndex], timestamp);
        consumed = (endIndex - index) + pasteEnd.Length;
        return true;
    }

    private static bool TryParseCsi(
        string text,
        int index,
        DateTimeOffset timestamp,
        out TerminalEvent parsed,
        out int consumed)
    {
        parsed = null!;
        consumed = 0;
        if (index + 2 >= text.Length || text[index + 1] != '[')
        {
            return false;
        }

        var final = text[index + 2];
        switch (final)
        {
            case 'A':
                parsed = TerminalEvent.Key(new KeyGesture(TerminalKey.Up, TerminalModifiers.None), timestamp);
                consumed = 3;
                return true;
            case 'B':
                parsed = TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None), timestamp);
                consumed = 3;
                return true;
            case 'C':
                parsed = TerminalEvent.Key(new KeyGesture(TerminalKey.Right, TerminalModifiers.None), timestamp);
                consumed = 3;
                return true;
            case 'D':
                parsed = TerminalEvent.Key(new KeyGesture(TerminalKey.Left, TerminalModifiers.None), timestamp);
                consumed = 3;
                return true;
            case 'I':
                parsed = TerminalEvent.Focus(true, timestamp);
                consumed = 3;
                return true;
            case 'O':
                parsed = TerminalEvent.Focus(false, timestamp);
                consumed = 3;
                return true;
            case '<':
                return TryParseMouse(text, index, timestamp, out parsed, out consumed);
            default:
                return false;
        }
    }

    private static bool TryParseMouse(
        string text,
        int index,
        DateTimeOffset timestamp,
        out TerminalEvent parsed,
        out int consumed)
    {
        parsed = null!;
        consumed = 0;
        var terminatorIndex = text.IndexOfAny(['M', 'm'], index + 3);
        if (terminatorIndex < 0)
        {
            return false;
        }

        var payload = text[(index + 3)..terminatorIndex];
        var pieces = payload.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pieces.Length != 3 ||
            !int.TryParse(pieces[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) ||
            !int.TryParse(pieces[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(pieces[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
        {
            return false;
        }

        var button = DecodeButton(code);
        var kind = DecodeKind(code, text[terminatorIndex]);
        var modifiers = DecodeModifiers(code);
        parsed = TerminalEvent.Mouse(new MouseGesture(
            (ushort)Math.Max(0, x - 1),
            (ushort)Math.Max(0, y - 1),
            button,
            kind,
            modifiers), timestamp);
        consumed = (terminatorIndex - index) + 1;
        return true;
    }

    private static TerminalMouseButton DecodeButton(int code)
    {
        if ((code & 64) != 0)
        {
            return (code & 1) == 0 ? TerminalMouseButton.WheelUp : TerminalMouseButton.WheelDown;
        }

        return (code & 0b11) switch
        {
            0 => TerminalMouseButton.Left,
            1 => TerminalMouseButton.Middle,
            2 => TerminalMouseButton.Right,
            _ => TerminalMouseButton.None
        };
    }

    private static TerminalMouseKind DecodeKind(int code, char terminator)
    {
        if ((code & 64) != 0)
        {
            return TerminalMouseKind.Scroll;
        }

        if ((code & 32) != 0)
        {
            return TerminalMouseKind.Drag;
        }

        return terminator == 'm' ? TerminalMouseKind.Up : TerminalMouseKind.Down;
    }

    private static TerminalModifiers DecodeModifiers(int code)
    {
        var modifiers = TerminalModifiers.None;
        if ((code & 4) != 0)
        {
            modifiers |= TerminalModifiers.Shift;
        }

        if ((code & 8) != 0)
        {
            modifiers |= TerminalModifiers.Alt;
        }

        if ((code & 16) != 0)
        {
            modifiers |= TerminalModifiers.Control;
        }

        return modifiers;
    }
}
