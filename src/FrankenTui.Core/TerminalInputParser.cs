// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/input_parser.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DCS and Alt-prefix contracts: upstream c63f7dd4, 56170378, 5304e051, edd35dd2.
// DIVERGENCE: OSC 52 replies are bounded and consumed, but not surfaced because the
// current managed TerminalEvent model has no clipboard-event variant.

using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace FrankenTui.Core;

public sealed class TerminalInputParser
{
    private const int MaxCsiLength = 256;
    private const int MaxOscLength = 102_400;
    private const int MaxPasteLength = 1024 * 1024;

    private static ReadOnlySpan<byte> PasteEndSequence => "\u001b[201~"u8;

    private readonly List<byte> _sequenceBuffer = new(64);
    private readonly List<byte> _pasteBuffer = new();
    private readonly List<byte> _pasteTail = new(6);
    private readonly byte[] _utf8Buffer = new byte[4];
    private readonly byte[] _x10Buffer = new byte[3];

    private ParserState _state;
    private bool _inPaste;
    private bool _expectX10Mouse;
    private bool _allowLegacyMouse;
    private int _utf8Collected;
    private int _utf8Expected;
    private bool _utf8Alt;
    private int _x10Collected;
    private long _pasteTotalBytes;

    private enum ParserState
    {
        Ground,
        Escape,
        Csi,
        CsiParam,
        CsiIgnore,
        Ss3,
        Osc,
        OscContent,
        OscEscape,
        OscIgnore,
        DcsIgnore,
        DcsEscape,
        Utf8,
        MouseX10,
    }

    public void SetExpectX10Mouse(bool enabled) => _expectX10Mouse = enabled;

    public void SetAllowLegacyMouse(bool enabled) => _allowLegacyMouse = enabled;

    public bool HasPendingTimeoutState => _state is ParserState.Escape or ParserState.Utf8;

    public TerminalEvent? Timeout(DateTimeOffset? timestamp = null)
    {
        var stamp = timestamp ?? DateTimeOffset.UtcNow;
        if (_state == ParserState.Escape)
        {
            _state = ParserState.Ground;
            return Key(TerminalKey.Escape, TerminalModifiers.None, stamp);
        }

        if (_state == ParserState.Utf8)
        {
            var modifiers = _utf8Alt ? TerminalModifiers.Alt : TerminalModifiers.None;
            ResetUtf8();
            return Character(Rune.ReplacementChar, modifiers, stamp);
        }

        return null;
    }

    public IReadOnlyList<TerminalEvent> Parse(ReadOnlySpan<byte> payload, DateTimeOffset? timestamp = null)
    {
        var stamp = timestamp ?? DateTimeOffset.UtcNow;
        var events = new List<TerminalEvent>(Math.Min(payload.Length + 1, 8193));
        foreach (var value in payload)
            ProcessByte(value, stamp, events);
        return TerminalEventCoalescer.Coalesce(events);
    }

    public IReadOnlyList<TerminalEvent> Parse(string text, DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Parse(Encoding.UTF8.GetBytes(text), timestamp);
    }

    private void ProcessByte(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (_inPaste)
        {
            ProcessPasteByte(value, stamp, events);
            return;
        }

        switch (_state)
        {
            case ParserState.Ground:
                ProcessGround(value, stamp, events);
                break;
            case ParserState.Escape:
                ProcessEscape(value, stamp, events);
                break;
            case ParserState.Csi:
                ProcessCsi(value, stamp, events);
                break;
            case ParserState.CsiParam:
                ProcessCsiParam(value, stamp, events);
                break;
            case ParserState.CsiIgnore:
                ProcessCsiIgnore(value, stamp, events);
                break;
            case ParserState.Ss3:
                ProcessSs3(value, stamp, events);
                break;
            case ParserState.Osc:
            case ParserState.OscContent:
                ProcessOscContent(value, stamp, events);
                break;
            case ParserState.OscEscape:
                ProcessOscEscape(value, stamp, events);
                break;
            case ParserState.OscIgnore:
                ProcessOscIgnore(value, stamp, events);
                break;
            case ParserState.DcsIgnore:
                ProcessDcsIgnore(value, stamp, events);
                break;
            case ParserState.DcsEscape:
                ProcessDcsEscape(value, stamp, events);
                break;
            case ParserState.Utf8:
                ProcessUtf8(value, stamp, events);
                break;
            case ParserState.MouseX10:
                ProcessMouseX10(value, stamp, events);
                break;
        }
    }

    private void ProcessGround(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        switch (value)
        {
            case 0x1B:
                _state = ParserState.Escape;
                return;
            case 0x9B:
                BeginSequence(ParserState.Csi);
                return;
            case 0x8F:
                _state = ParserState.Ss3;
                return;
            case 0x9D:
                BeginSequence(ParserState.Osc);
                return;
            case 0x00:
                events.Add(Key(TerminalKey.Null, TerminalModifiers.None, stamp));
                return;
            case 0x08:
            case 0x7F:
                events.Add(Key(TerminalKey.Backspace, TerminalModifiers.None, stamp));
                return;
            case 0x09:
                events.Add(Key(TerminalKey.Tab, TerminalModifiers.None, stamp));
                return;
            case 0x0D:
                events.Add(Key(TerminalKey.Enter, TerminalModifiers.None, stamp));
                return;
        }

        if (value is >= 0x01 and <= 0x1A)
        {
            events.Add(Character(new Rune(value + (byte)'a' - 1), TerminalModifiers.Control, stamp));
            return;
        }

        var controlCharacter = value switch
        {
            0x1C => '\\',
            0x1D => ']',
            0x1E => '^',
            0x1F => '_',
            _ => '\0',
        };
        if (controlCharacter != '\0')
        {
            events.Add(Character(new Rune(controlCharacter), TerminalModifiers.Control, stamp));
            return;
        }

        if (value is >= 0x20 and <= 0x7E)
        {
            events.Add(Character(new Rune(value), TerminalModifiers.None, stamp));
            return;
        }

        if (TryStartUtf8(value, alt: false))
            return;

        if (value is 0xC0 or 0xC1 or >= 0xF5)
            events.Add(Character(Rune.ReplacementChar, TerminalModifiers.None, stamp));
    }

    private void ProcessEscape(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        switch (value)
        {
            case (byte)'[':
                BeginSequence(ParserState.Csi);
                return;
            case (byte)'O':
                _state = ParserState.Ss3;
                return;
            case (byte)']':
                BeginSequence(ParserState.Osc);
                return;
            case (byte)'P':
                BeginSequence(ParserState.DcsIgnore);
                return;
            case 0x1B:
                _state = ParserState.Ground;
                events.Add(Key(TerminalKey.Escape, TerminalModifiers.Alt, stamp));
                return;
            case 0x7F:
                _state = ParserState.Ground;
                events.Add(Key(TerminalKey.Backspace, TerminalModifiers.Alt, stamp));
                return;
        }

        if (value < 0x20)
        {
            _state = ParserState.Ground;
            int before = events.Count;
            ProcessGround(value, stamp, events);
            if (events.Count > before && events[^1] is KeyTerminalEvent key)
                events[^1] = key with { Gesture = key.Gesture with { Modifiers = key.Gesture.Modifiers | TerminalModifiers.Alt } };
            return;
        }

        if (value is >= 0x20 and <= 0x7E)
        {
            _state = ParserState.Ground;
            events.Add(Character(new Rune(value), TerminalModifiers.Alt, stamp));
            return;
        }

        if (TryStartUtf8(value, alt: true))
            return;

        _state = ParserState.Ground;
        if (value is 0xC0 or 0xC1 or >= 0xF5)
            events.Add(Character(Rune.ReplacementChar, TerminalModifiers.Alt, stamp));
    }

    private void ProcessCsi(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x1B)
        {
            BeginSequence(ParserState.Escape);
            return;
        }

        _sequenceBuffer.Add(value);
        if (value is >= 0x20 and <= 0x3F)
        {
            _state = ParserState.CsiParam;
            return;
        }

        if (value is >= 0x40 and <= 0x7E)
        {
            if (_expectX10Mouse && value == (byte)'M' && _sequenceBuffer.Count == 1)
            {
                _sequenceBuffer.Clear();
                _x10Collected = 0;
                _state = ParserState.MouseX10;
                return;
            }

            _state = ParserState.Ground;
            ParseBufferedCsi(stamp, events);
            return;
        }

        BeginSequence(ParserState.Ground);
        ProcessGround(value, stamp, events);
    }

    private void ProcessCsiParam(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x1B)
        {
            BeginSequence(ParserState.Escape);
            return;
        }

        if (_sequenceBuffer.Count >= MaxCsiLength && value is not (>= 0x40 and <= 0x7E))
        {
            BeginSequence(ParserState.CsiIgnore);
            return;
        }

        _sequenceBuffer.Add(value);
        if (value is >= 0x20 and <= 0x3F)
            return;

        if (value is >= 0x40 and <= 0x7E)
        {
            _state = ParserState.Ground;
            ParseBufferedCsi(stamp, events);
            return;
        }

        BeginSequence(ParserState.Ground);
        ProcessGround(value, stamp, events);
    }

    private void ProcessCsiIgnore(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x1B)
        {
            _state = ParserState.Escape;
            return;
        }

        if (value is >= 0x40 and <= 0x7E)
        {
            _state = ParserState.Ground;
            return;
        }

        if (value is not (>= 0x20 and <= 0x3F))
        {
            _state = ParserState.Ground;
            ProcessGround(value, stamp, events);
        }
    }

    private void ParseBufferedCsi(DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (_sequenceBuffer.Count == 0)
            return;

        char final = (char)_sequenceBuffer[^1];
        string parameters = Encoding.ASCII.GetString(_sequenceBuffer.ToArray(), 0, _sequenceBuffer.Count - 1);
        _sequenceBuffer.Clear();

        if (parameters.Length == 0 && final == 'I')
        {
            events.Add(TerminalEvent.Focus(true, stamp));
            return;
        }
        if (parameters.Length == 0 && final == 'O')
        {
            events.Add(TerminalEvent.Focus(false, stamp));
            return;
        }
        if (parameters == "200" && final == '~')
        {
            _inPaste = true;
            _pasteBuffer.Clear();
            _pasteTail.Clear();
            _pasteTotalBytes = 0;
            return;
        }
        if (parameters == "201" && final == '~')
            return;

        if (parameters.StartsWith('<') && final is 'M' or 'm')
        {
            if (TryParseSgrMouse(parameters, final, stamp, out var mouse))
                events.Add(mouse);
            return;
        }

        if ((_allowLegacyMouse || _expectX10Mouse) && final == 'M'
            && TryParseLegacyMouse(parameters, stamp, out var legacyMouse))
        {
            events.Add(legacyMouse);
            return;
        }

        var (modifiers, kind) = ParseModifierAndKind(parameters);
        TerminalKey? key = final switch
        {
            'A' => TerminalKey.Up,
            'B' => TerminalKey.Down,
            'C' => TerminalKey.Right,
            'D' => TerminalKey.Left,
            'H' => TerminalKey.Home,
            'F' => TerminalKey.End,
            'P' => TerminalKey.F1,
            'Q' => TerminalKey.F2,
            'R' => TerminalKey.F3,
            'S' => TerminalKey.F4,
            'Z' => TerminalKey.Tab,
            _ => null,
        };
        if (key.HasValue)
        {
            if (final == 'Z')
                modifiers |= TerminalModifiers.Shift;
            events.Add(Key(key.Value, modifiers, stamp, kind));
            return;
        }

        if (final == '~' && TryMapTildeKey(parameters, out var tildeKey))
        {
            events.Add(Key(tildeKey, modifiers, stamp, kind));
            return;
        }

        if (final == 'u' && TryParseKittyKey(parameters, stamp, out var kitty))
            events.Add(kitty);
    }

    private void ProcessSs3(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x1B)
        {
            _state = ParserState.Escape;
            return;
        }

        _state = ParserState.Ground;
        var key = value switch
        {
            (byte)'A' => TerminalKey.Up,
            (byte)'B' => TerminalKey.Down,
            (byte)'C' => TerminalKey.Right,
            (byte)'D' => TerminalKey.Left,
            (byte)'P' => TerminalKey.F1,
            (byte)'Q' => TerminalKey.F2,
            (byte)'R' => TerminalKey.F3,
            (byte)'S' => TerminalKey.F4,
            (byte)'H' => TerminalKey.Home,
            (byte)'F' => TerminalKey.End,
            _ => TerminalKey.Unknown,
        };
        if (key != TerminalKey.Unknown)
            events.Add(Key(key, TerminalModifiers.None, stamp));
    }

    private void ProcessOscContent(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x1B)
        {
            _state = ParserState.OscEscape;
            return;
        }
        if (value == 0x9C || value == 0x07)
        {
            BeginSequence(ParserState.Ground);
            return;
        }
        if (value < 0x20)
        {
            BeginSequence(ParserState.Ground);
            ProcessGround(value, stamp, events);
            return;
        }
        if (_sequenceBuffer.Count >= MaxOscLength)
        {
            BeginSequence(ParserState.OscIgnore);
            return;
        }

        _sequenceBuffer.Add(value);
        _state = ParserState.OscContent;
    }

    private void ProcessOscEscape(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == (byte)'\\')
        {
            BeginSequence(ParserState.Ground);
            return;
        }
        if (value == 0x1B)
        {
            BeginSequence(ParserState.Escape);
            return;
        }

        BeginSequence(ParserState.Escape);
        ProcessEscape(value, stamp, events);
    }

    private void ProcessOscIgnore(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x07 || value == 0x9C)
        {
            _state = ParserState.Ground;
            return;
        }
        if (value == 0x1B)
        {
            _state = ParserState.OscEscape;
            return;
        }
        if (value < 0x20)
        {
            _state = ParserState.Ground;
            ProcessGround(value, stamp, events);
        }
    }

    private void ProcessDcsIgnore(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == 0x07 || value == 0x9C)
        {
            _state = ParserState.Ground;
            return;
        }
        if (value == 0x1B)
        {
            _state = ParserState.DcsEscape;
            return;
        }
        if (value < 0x20)
        {
            _state = ParserState.Ground;
            ProcessGround(value, stamp, events);
        }
    }

    private void ProcessDcsEscape(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value == (byte)'\\')
        {
            _state = ParserState.Ground;
            return;
        }
        if (value == 0x1B)
        {
            _state = ParserState.Escape;
            return;
        }

        _state = ParserState.Escape;
        ProcessEscape(value, stamp, events);
    }

    private bool TryStartUtf8(byte value, bool alt)
    {
        _utf8Expected = value switch
        {
            >= 0xC2 and <= 0xDF => 2,
            >= 0xE0 and <= 0xEF => 3,
            >= 0xF0 and <= 0xF4 => 4,
            _ => 0,
        };
        if (_utf8Expected == 0)
            return false;

        _utf8Buffer[0] = value;
        _utf8Collected = 1;
        _utf8Alt = alt;
        _state = ParserState.Utf8;
        return true;
    }

    private void ProcessUtf8(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        if (value is not (>= 0x80 and <= 0xBF))
        {
            var modifiers = _utf8Alt ? TerminalModifiers.Alt : TerminalModifiers.None;
            ResetUtf8();
            events.Add(Character(Rune.ReplacementChar, modifiers, stamp));
            ProcessGround(value, stamp, events);
            return;
        }

        _utf8Buffer[_utf8Collected++] = value;
        if (_utf8Collected < _utf8Expected)
            return;

        var modifiersForRune = _utf8Alt ? TerminalModifiers.Alt : TerminalModifiers.None;
        var status = Rune.DecodeFromUtf8(
            _utf8Buffer.AsSpan(0, _utf8Expected),
            out var rune,
            out _);
        ResetUtf8();
        events.Add(Character(status == OperationStatus.Done ? rune : Rune.ReplacementChar, modifiersForRune, stamp));
    }

    private void ProcessMouseX10(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        _x10Buffer[_x10Collected++] = value;
        if (_x10Collected < 3)
            return;

        _x10Collected = 0;
        _state = ParserState.Ground;
        if (_x10Buffer[0] < 32 || _x10Buffer[1] < 33 || _x10Buffer[2] < 33)
            return;

        int code = _x10Buffer[0] - 32;
        var kind = (code & 64) != 0
            ? TerminalMouseKind.Scroll
            : (code & 3) == 3
                ? TerminalMouseKind.Up
                : TerminalMouseKind.Down;
        events.Add(TerminalEvent.Mouse(
            new MouseGesture(
                (ushort)(_x10Buffer[1] - 33),
                (ushort)(_x10Buffer[2] - 33),
                DecodeButton(code),
                kind,
                DecodeModifiers(code)),
            stamp));
    }

    private void ProcessPasteByte(byte value, DateTimeOffset stamp, List<TerminalEvent> events)
    {
        _pasteTotalBytes++;
        if (_pasteBuffer.Count < MaxPasteLength)
            _pasteBuffer.Add(value);

        if (_pasteTail.Count == PasteEndSequence.Length)
            _pasteTail.RemoveAt(0);
        _pasteTail.Add(value);

        if (!CollectionsMarshal.AsSpan(_pasteTail).SequenceEqual(PasteEndSequence))
            return;

        long actualContentLength = Math.Max(0, _pasteTotalBytes - PasteEndSequence.Length);
        int capturedContentLength = (int)Math.Min(actualContentLength, _pasteBuffer.Count);
        string content = Encoding.UTF8.GetString(_pasteBuffer.ToArray(), 0, capturedContentLength);
        _inPaste = false;
        _pasteBuffer.Clear();
        _pasteTail.Clear();
        _pasteTotalBytes = 0;
        events.Add(TerminalEvent.Paste(content, stamp));
    }

    private static (TerminalModifiers Modifiers, TerminalKeyEventKind Kind) ParseModifierAndKind(
        string parameters)
    {
        string modifierPart = parameters.Split(';').ElementAtOrDefault(1) ?? string.Empty;
        var pieces = modifierPart.Split(':');
        uint modifierValue = uint.TryParse(pieces.ElementAtOrDefault(0), out var parsedModifier)
            ? parsedModifier
            : 1;
        uint kindValue = uint.TryParse(pieces.ElementAtOrDefault(1), out var parsedKind)
            ? parsedKind
            : 1;
        uint bits = modifierValue > 0 ? modifierValue - 1 : 0;
        var modifiers = TerminalModifiers.None;
        if ((bits & 1) != 0) modifiers |= TerminalModifiers.Shift;
        if ((bits & 2) != 0) modifiers |= TerminalModifiers.Alt;
        if ((bits & 4) != 0) modifiers |= TerminalModifiers.Control;
        if ((bits & 8) != 0) modifiers |= TerminalModifiers.Super;
        var kind = kindValue switch
        {
            2 => TerminalKeyEventKind.Repeat,
            3 => TerminalKeyEventKind.Release,
            _ => TerminalKeyEventKind.Press,
        };
        return (modifiers, kind);
    }

    private static bool TryParseKittyKey(
        string parameters,
        DateTimeOffset stamp,
        out KeyTerminalEvent parsed)
    {
        parsed = null!;
        var parts = parameters.Split(';');
        string keyPart = parts.ElementAtOrDefault(0) ?? string.Empty;
        if (!uint.TryParse(keyPart.Split(':')[0], out var keyCode))
            return false;

        var (modifiers, kind) = ParseModifierAndKind(parameters);
        if (!TryMapKittyKey(keyCode, out var key, out var character))
            return false;

        parsed = Key(key, modifiers, stamp, kind, character);
        return true;
    }

    private static bool TryMapKittyKey(uint code, out TerminalKey key, out Rune? character)
    {
        character = null;
        key = code switch
        {
            9 or 57_346 => TerminalKey.Tab,
            13 or 57_345 => TerminalKey.Enter,
            27 or 57_344 => TerminalKey.Escape,
            8 or 127 or 57_347 => TerminalKey.Backspace,
            57_348 => TerminalKey.Insert,
            57_349 => TerminalKey.Delete,
            57_350 => TerminalKey.Left,
            57_351 => TerminalKey.Right,
            57_352 => TerminalKey.Up,
            57_353 => TerminalKey.Down,
            57_354 => TerminalKey.PageUp,
            57_355 => TerminalKey.PageDown,
            57_356 => TerminalKey.Home,
            57_357 => TerminalKey.End,
            >= 57_364 and <= 57_387 => FunctionKey((int)(code - 57_364 + 1)),
            _ => TerminalKey.Unknown,
        };
        if (key != TerminalKey.Unknown)
            return true;

        if (code is >= 57_358 and <= 57_363 or >= 57_388 and <= 63_743
            || code > int.MaxValue
            || !Rune.TryCreate((int)code, out var rune))
            return false;

        key = TerminalKey.Character;
        character = rune;
        return true;
    }

    private static bool TryMapTildeKey(string parameters, out TerminalKey key)
    {
        key = TerminalKey.Unknown;
        string first = parameters.Split(';')[0];
        if (!int.TryParse(first, out int number))
            return false;
        key = number switch
        {
            1 => TerminalKey.Home,
            2 => TerminalKey.Insert,
            3 => TerminalKey.Delete,
            4 => TerminalKey.End,
            5 => TerminalKey.PageUp,
            6 => TerminalKey.PageDown,
            15 => TerminalKey.F5,
            17 => TerminalKey.F6,
            18 => TerminalKey.F7,
            19 => TerminalKey.F8,
            20 => TerminalKey.F9,
            21 => TerminalKey.F10,
            23 => TerminalKey.F11,
            24 => TerminalKey.F12,
            _ => TerminalKey.Unknown,
        };
        return key != TerminalKey.Unknown;
    }

    private static bool TryParseSgrMouse(
        string parameters,
        char final,
        DateTimeOffset stamp,
        out MouseTerminalEvent parsed)
    {
        parsed = null!;
        var parts = parameters[1..].Split(';');
        if (parts.Length < 3
            || !TryParseIntegerPrefix(parts[0], out int code)
            || !TryParseIntegerPrefix(parts[1], out int rawX)
            || !TryParseIntegerPrefix(parts[2], out int rawY))
            return false;

        code = Math.Clamp(code, 0, ushort.MaxValue);
        var kind = final == 'm'
            ? TerminalMouseKind.Up
            : (code & 64) != 0
                ? TerminalMouseKind.Scroll
                : (code & 32) != 0
                    ? (code & 3) == 3 ? TerminalMouseKind.Move : TerminalMouseKind.Drag
                    : (code & 3) == 3 ? TerminalMouseKind.Up : TerminalMouseKind.Down;
        parsed = TerminalEvent.Mouse(
            new MouseGesture(
                NormalizeMouseCoordinate(rawX),
                NormalizeMouseCoordinate(rawY),
                DecodeButton(code),
                kind,
                DecodeModifiers(code)),
            stamp);
        return true;
    }

    private static bool TryParseLegacyMouse(
        string parameters,
        DateTimeOffset stamp,
        out MouseTerminalEvent parsed)
    {
        parsed = null!;
        if (parameters.Length == 0 || parameters[0] == '<')
            return false;
        var parts = parameters.Split(';');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int code)
            || !ushort.TryParse(parts[1], out ushort x)
            || !ushort.TryParse(parts[2], out ushort y))
            return false;
        var kind = (code & 64) != 0
            ? TerminalMouseKind.Scroll
            : (code & 32) != 0
                ? (code & 3) == 3 ? TerminalMouseKind.Move : TerminalMouseKind.Drag
                : (code & 3) == 3 ? TerminalMouseKind.Up : TerminalMouseKind.Down;
        parsed = TerminalEvent.Mouse(
            new MouseGesture(
                x == 0 ? (ushort)0 : (ushort)(x - 1),
                y == 0 ? (ushort)0 : (ushort)(y - 1),
                DecodeButton(code),
                kind,
                DecodeModifiers(code)),
            stamp);
        return true;
    }

    private static bool TryParseIntegerPrefix(string value, out int parsed)
    {
        parsed = 0;
        if (value.Length == 0)
            return false;
        int length = value[0] is '+' or '-' ? 1 : 0;
        while (length < value.Length && char.IsAsciiDigit(value[length]))
            length++;
        return length > (value[0] is '+' or '-' ? 1 : 0)
            && int.TryParse(value[..length], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }

    private static ushort NormalizeMouseCoordinate(int raw)
    {
        if (raw <= 1)
            return 0;
        return (ushort)Math.Min(raw - 1, ushort.MaxValue);
    }

    private static TerminalKey FunctionKey(int number) => number switch
    {
        1 => TerminalKey.F1, 2 => TerminalKey.F2, 3 => TerminalKey.F3, 4 => TerminalKey.F4,
        5 => TerminalKey.F5, 6 => TerminalKey.F6, 7 => TerminalKey.F7, 8 => TerminalKey.F8,
        9 => TerminalKey.F9, 10 => TerminalKey.F10, 11 => TerminalKey.F11, 12 => TerminalKey.F12,
        13 => TerminalKey.F13, 14 => TerminalKey.F14, 15 => TerminalKey.F15, 16 => TerminalKey.F16,
        17 => TerminalKey.F17, 18 => TerminalKey.F18, 19 => TerminalKey.F19, 20 => TerminalKey.F20,
        21 => TerminalKey.F21, 22 => TerminalKey.F22, 23 => TerminalKey.F23, 24 => TerminalKey.F24,
        _ => TerminalKey.Unknown,
    };

    private static KeyTerminalEvent Key(
        TerminalKey key,
        TerminalModifiers modifiers,
        DateTimeOffset stamp,
        TerminalKeyEventKind kind = TerminalKeyEventKind.Press,
        Rune? character = null) =>
        TerminalEvent.Key(new KeyGesture(key, modifiers, character), stamp, kind);

    private static KeyTerminalEvent Character(
        Rune character,
        TerminalModifiers modifiers,
        DateTimeOffset stamp) =>
        Key(TerminalKey.Character, modifiers, stamp, character: character);

    private void BeginSequence(ParserState state)
    {
        _state = state;
        _sequenceBuffer.Clear();
    }

    private void ResetUtf8()
    {
        _state = ParserState.Ground;
        _utf8Collected = 0;
        _utf8Expected = 0;
        _utf8Alt = false;
        Array.Clear(_utf8Buffer);
    }

    private static bool TryConsumeDcs(string text, int index, out int consumed)
    {
        consumed = 0;
        if (index + 1 >= text.Length || text[index + 1] != 'P')
        {
            return false;
        }

        // DCS carries terminal query responses such as XTGETTCAP. Decode none
        // of its payload as keystrokes. Deliberately do not intercept sibling
        // SOS/PM/APC introducers: upstream 56170378 keeps those available as
        // Alt-prefixed input because terminals do not normally return them.
        for (var cursor = index + 2; cursor < text.Length; cursor++)
        {
            var current = text[cursor];
            if (current == '\u0007')
            {
                consumed = (cursor - index) + 1;
                return true;
            }

            if (current == '\u001b')
            {
                if (cursor + 1 < text.Length && text[cursor + 1] == '\\')
                {
                    consumed = (cursor - index) + 2;
                    return true;
                }

                if (cursor + 1 == text.Length)
                {
                    // Upstream remains in DcsEscape awaiting a possible ST.
                    // With call-scoped parsing, consuming the trailing ESC is
                    // safer than manufacturing a standalone Escape key event.
                    consumed = text.Length - index;
                    return true;
                }

                // A non-ST ESC cancels the DCS. Leave it for the outer parser,
                // which will interpret the following byte as a fresh escape.
                consumed = cursor - index;
                return true;
            }

            if (current < ' ')
            {
                // Malformed control strings must not swallow subsequent input.
                // Leave the control byte to be processed in ground state.
                consumed = cursor - index;
                return true;
            }
        }

        // This parser is call-scoped, so an unterminated DCS consumes the
        // remainder of this payload rather than injecting its bytes as keys.
        consumed = text.Length - index;
        return true;
    }

    private static bool TryConsumeStrayPasteTerminator(string text, int index, out int consumed)
    {
        const string pasteEnd = "\u001b[201~";
        consumed = text.AsSpan(index).StartsWith(pasteEnd, StringComparison.Ordinal)
            ? pasteEnd.Length
            : 0;
        return consumed != 0;
    }

    private static bool TryConsumeCsiPrefixBeforeControl(string text, int index, out int consumed)
    {
        consumed = 0;
        if (index + 2 >= text.Length || text[index + 1] != '[' || !char.IsControl(text[index + 2]))
        {
            return false;
        }

        // Abort ESC [ and reprocess the control byte in ground state instead
        // of emitting the introducer as literal input or swallowing the key.
        consumed = 2;
        return true;
    }

    private static bool TryParseAltKey(
        string text,
        int index,
        DateTimeOffset timestamp,
        out TerminalEvent parsed,
        out int consumed)
    {
        parsed = null!;
        consumed = 0;
        if (index + 1 >= text.Length)
        {
            return false;
        }

        var next = text[index + 1];
        if (next is '[' or 'O' or ']' or 'P')
        {
            return false;
        }

        if (next == '\u001b')
        {
            parsed = TerminalEvent.Key(
                new KeyGesture(TerminalKey.Escape, TerminalModifiers.Alt),
                timestamp);
            consumed = 2;
            return true;
        }

        if (next is '\r' or '\n')
        {
            parsed = TerminalEvent.Key(
                new KeyGesture(TerminalKey.Enter, TerminalModifiers.Alt),
                timestamp);
            consumed = 2;
            return true;
        }

        if (next == '\t')
        {
            parsed = TerminalEvent.Key(
                new KeyGesture(TerminalKey.Tab, TerminalModifiers.Alt),
                timestamp);
            consumed = 2;
            return true;
        }

        if (next is '\b' or '\u007f')
        {
            parsed = TerminalEvent.Key(
                new KeyGesture(TerminalKey.Backspace, TerminalModifiers.Alt),
                timestamp);
            consumed = 2;
            return true;
        }

        Rune rune;
        if (char.IsHighSurrogate(next) &&
            index + 2 < text.Length &&
            char.IsLowSurrogate(text[index + 2]))
        {
            rune = new Rune(next, text[index + 2]);
            consumed = 3;
        }
        else if (Rune.TryCreate(next, out rune))
        {
            consumed = 2;
        }
        else
        {
            return false;
        }

        parsed = TerminalEvent.Key(
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Alt, rune),
            timestamp);
        return true;
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
