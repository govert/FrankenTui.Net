// SPDX-License-Identifier: MIT
// Port of ftui-render::terminal_model.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 5C74171590A69E122C0764D753EDE661A7A6D2BB1210ED94B298DA8F8476F0FB.
// ADAPTATION: terminal dimensions use ushort, matching the managed terminal API.
// COMPATIBILITY EXTENSION: ESC 7/8 preserve and restore cursor/style state; upstream
// currently accepts those sequences without implementing the save/restore stack.

using System.Buffers;
using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Render;

/// <summary>Current Select Graphic Rendition state.</summary>
public record struct SgrState(
    PackedRgba Foreground,
    PackedRgba Background,
    CellStyleFlags Flags)
{
    public static SgrState Default => new(PackedRgba.White, PackedRgba.Transparent, CellStyleFlags.None);

    public PackedRgba Fg => Foreground;

    public PackedRgba Bg => Background;

    public void Reset() => this = Default;
}

/// <summary>Terminal modes observed by the presenter-validation model.</summary>
public record struct ModeFlags(
    bool CursorVisible,
    bool AlternateScreen,
    uint SynchronizedOutputLevel)
{
    /// <summary>
    /// Mirrors Rust's derived <c>Default</c>: all bits/fields are zero. Use
    /// <see cref="New"/> for the initialized terminal state with a visible cursor.
    /// </summary>
    public static ModeFlags Default => default;

    public static ModeFlags New() => new(true, false, 0);

    public bool AltScreen => AlternateScreen;

    public uint SyncOutputLevel => SynchronizedOutputLevel;
}

/// <summary>
/// Minimal byte-stream terminal emulator used to validate presenter output.
/// It intentionally implements only the ANSI/OSC surface emitted by FrankenTUI.
/// </summary>
public sealed class TerminalModel
{
    private readonly ModelCell[] _cells;
    private readonly List<string> _links = [string.Empty];
    private readonly List<uint> _csiParameters = new(16);
    private readonly List<byte> _csiIntermediate = new(4);
    private readonly List<byte> _oscBuffer = new(256);
    private readonly List<byte> _utf8Pending = new(4);

    private ushort _cursorColumn;
    private ushort _cursorRow;
    private SgrState _sgr = SgrState.Default;
    private ModeFlags _modes = ModeFlags.New();
    private uint _activeLinkId;
    private ParseState _state;
    private int? _utf8Expected;
    private ulong _bytesProcessed;
    private SavedCursorState? _savedCursor;

    public TerminalModel(ushort width, ushort height)
    {
        Width = Math.Max(width, (ushort)1);
        Height = Math.Max(height, (ushort)1);
        _cells = new ModelCell[checked(Width * Height)];
        Array.Fill(_cells, ModelCell.Default);
    }

    public TerminalModel(int width, int height)
        : this(
            (ushort)Math.Clamp(width, 1, ushort.MaxValue),
            (ushort)Math.Clamp(height, 1, ushort.MaxValue))
    {
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public ushort CursorColumn => _cursorColumn;

    public ushort CursorRow => _cursorRow;

    public (ushort X, ushort Y) Cursor => (_cursorColumn, _cursorRow);

    public SgrState SgrState => _sgr;

    public ModeFlags Modes => _modes;

    public bool CursorVisible => _modes.CursorVisible;

    public int SyncOutputDepth => checked((int)_modes.SynchronizedOutputLevel);

    public uint ActiveLinkId => _activeLinkId;

    public bool HasDanglingLink => _activeLinkId != 0;

    public bool SyncOutputBalanced => _modes.SynchronizedOutputLevel == 0;

    public ReadOnlyMemory<ModelCell> Cells => _cells;

    public ModelCell? CurrentCell => Cell(_cursorColumn, _cursorRow);

    /// <summary>Number of bytes accepted by <see cref="Process(ReadOnlySpan{byte})"/>.</summary>
    public ulong BytesProcessed => _bytesProcessed;

    /// <summary>Restores the initialized state while retaining dimensions.</summary>
    public void Reset()
    {
        Array.Fill(_cells, ModelCell.Default);
        _cursorColumn = 0;
        _cursorRow = 0;
        _sgr = SgrState.Default;
        _modes = ModeFlags.New();
        _activeLinkId = 0;
        _links.Clear();
        _links.Add(string.Empty);
        _state = ParseState.Ground;
        _csiParameters.Clear();
        _csiIntermediate.Clear();
        _oscBuffer.Clear();
        _utf8Pending.Clear();
        _utf8Expected = null;
        _savedCursor = null;
    }

    /// <summary>UTF-8 convenience adapter over the source-shaped byte-stream parser.</summary>
    public void Process(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Process(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Processes a byte fragment. Incomplete UTF-8 and escape sequences remain pending so
    /// callers may split a sequence across any number of calls.
    /// </summary>
    public void Process(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            ProcessByte(value);
            _bytesProcessed++;
        }
    }

    public ModelCell? Cell(ushort column, ushort row) =>
        TryIndex(column, row, out var index) ? _cells[index] : null;

    public ReadOnlyMemory<ModelCell>? Row(ushort row)
    {
        if (row >= Height)
        {
            return null;
        }

        return _cells.AsMemory(row * Width, Width);
    }

    /// <summary>
    /// Returns trimmed row text. For backward compatibility, an out-of-range row projects
    /// source <c>None</c> as an empty string; <see cref="TryRowText"/> preserves typed absence.
    /// </summary>
    public string RowText(ushort row) => TryRowText(row, out var text) ? text : string.Empty;

    public bool TryRowText(ushort row, out string text)
    {
        if (Row(row) is not { } memory)
        {
            text = string.Empty;
            return false;
        }

        var builder = new StringBuilder(Width);
        foreach (var cell in memory.Span)
        {
            builder.Append(cell.Text);
        }

        text = builder.ToString().TrimEnd(' ');
        return true;
    }

    public string ScreenString()
    {
        var rows = new string[Height];
        for (ushort row = 0; row < Height; row++)
        {
            rows[row] = RowText(row);
        }

        return string.Join(Environment.NewLine, rows);
    }

    public string? LinkUrl(uint linkId) =>
        linkId < _links.Count ? _links[(int)linkId] : null;

    public string? DiffGrid(ReadOnlySpan<ModelCell> expected)
    {
        if (_cells.Length != expected.Length)
        {
            return $"Grid size mismatch: got {_cells.Length} cells, expected {expected.Length}";
        }

        StringBuilder? differences = null;
        for (var index = 0; index < _cells.Length; index++)
        {
            if (_cells[index] == expected[index])
            {
                continue;
            }

            differences ??= new StringBuilder("Grid differences:");
            differences.Append('\n')
                .Append("  (")
                .Append(index % Width)
                .Append(", ")
                .Append(index / Width)
                .Append("): got ")
                .Append(Quote(_cells[index].Text))
                .Append(", expected ")
                .Append(Quote(expected[index].Text));
        }

        return differences?.ToString();
    }

    /// <summary>Formats control and escape sequences for deterministic diagnostics.</summary>
    public static string DumpSequences(ReadOnlySpan<byte> bytes)
    {
        var output = new StringBuilder();
        var index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == 0x1b)
            {
                if (index + 1 >= bytes.Length)
                {
                    output.Append("\\e");
                    index++;
                    continue;
                }

                switch (bytes[index + 1])
                {
                    case (byte)'[':
                        output.Append("\\e[");
                        index += 2;
                        while (index < bytes.Length && bytes[index] is < 0x40 or > 0x7e)
                        {
                            output.Append((char)bytes[index++]);
                        }

                        if (index < bytes.Length)
                        {
                            output.Append((char)bytes[index++]);
                        }

                        break;
                    case (byte)']':
                        output.Append("\\e]");
                        index += 2;
                        while (index < bytes.Length && bytes[index] != 0x07)
                        {
                            if (bytes[index] == 0x1b && index + 1 < bytes.Length && bytes[index + 1] == (byte)'\\')
                            {
                                output.Append("\\e\\\\");
                                index += 2;
                                break;
                            }

                            output.Append((char)bytes[index++]);
                        }

                        if (index < bytes.Length && bytes[index] == 0x07)
                        {
                            output.Append("\\a");
                            index++;
                        }

                        break;
                    default:
                        output.Append("\\e").Append((char)bytes[index + 1]);
                        index += 2;
                        break;
                }
            }
            else if (bytes[index] < 0x20)
            {
                output.Append("\\x").Append(bytes[index].ToString("x2"));
                index++;
            }
            else
            {
                output.Append((char)bytes[index++]);
            }
        }

        return output.ToString();
    }

    private void ProcessByte(byte value)
    {
        switch (_state)
        {
            case ParseState.Ground:
                Ground(value);
                break;
            case ParseState.Escape:
                Escape(value);
                break;
            case ParseState.CsiEntry:
                CsiEntry(value);
                break;
            case ParseState.CsiParameter:
                CsiParameter(value);
                break;
            case ParseState.OscEntry:
                OscEntry(value);
                break;
            case ParseState.OscString:
                OscString(value);
                break;
            default:
                throw new InvalidOperationException($"Unknown parser state {_state}.");
        }
    }

    private void Ground(byte value)
    {
        switch (value)
        {
            case 0x1b:
                FlushPendingUtf8Invalid();
                _state = ParseState.Escape;
                break;
            case <= 0x1a:
            case >= 0x1c and <= 0x1f:
                FlushPendingUtf8Invalid();
                HandleC0(value);
                break;
            default:
                HandlePrintable(value);
                break;
        }
    }

    private void Escape(byte value)
    {
        switch (value)
        {
            case (byte)'[':
                _csiParameters.Clear();
                _csiIntermediate.Clear();
                _state = ParseState.CsiEntry;
                break;
            case (byte)']':
                _oscBuffer.Clear();
                _state = ParseState.OscEntry;
                break;
            case (byte)'7':
                _savedCursor = new SavedCursorState(_cursorColumn, _cursorRow, _sgr, _activeLinkId);
                _state = ParseState.Ground;
                break;
            case (byte)'8':
                if (_savedCursor is { } saved)
                {
                    _cursorColumn = saved.Column;
                    _cursorRow = saved.Row;
                    _sgr = saved.Sgr;
                    _activeLinkId = saved.LinkId;
                }

                _state = ParseState.Ground;
                break;
            case (byte)'=':
            case (byte)'>':
                _state = ParseState.Ground;
                break;
            case 0x1b:
                break;
            default:
                _state = ParseState.Ground;
                break;
        }
    }

    private void CsiEntry(byte value)
    {
        switch (value)
        {
            case >= (byte)'0' and <= (byte)'9':
                _csiParameters.Add((uint)(value - (byte)'0'));
                _state = ParseState.CsiParameter;
                break;
            case (byte)';':
                _csiParameters.Add(0);
                _csiParameters.Add(0);
                _state = ParseState.CsiParameter;
                break;
            case (byte)'?':
            case (byte)'>':
            case (byte)'!':
                _csiIntermediate.Add(value);
                _state = ParseState.CsiParameter;
                break;
            case >= 0x40 and <= 0x7e:
                ExecuteCsi(value);
                _state = ParseState.Ground;
                break;
            default:
                _state = ParseState.Ground;
                break;
        }
    }

    private void CsiParameter(byte value)
    {
        switch (value)
        {
            case >= (byte)'0' and <= (byte)'9':
                if (_csiParameters.Count == 0)
                {
                    _csiParameters.Add(0);
                }

                var lastIndex = _csiParameters.Count - 1;
                var expanded = ((ulong)_csiParameters[lastIndex] * 10) + (uint)(value - (byte)'0');
                _csiParameters[lastIndex] = expanded > uint.MaxValue ? uint.MaxValue : (uint)expanded;
                break;
            case (byte)';':
            case (byte)':':
                _csiParameters.Add(0);
                break;
            case >= 0x20 and <= 0x2f:
                _csiIntermediate.Add(value);
                break;
            case >= 0x40 and <= 0x7e:
                ExecuteCsi(value);
                _state = ParseState.Ground;
                break;
            default:
                _state = ParseState.Ground;
                break;
        }
    }

    private void OscEntry(byte value)
    {
        switch (value)
        {
            case 0x07:
                ExecuteOsc();
                _state = ParseState.Ground;
                break;
            case 0x1b:
                _state = ParseState.OscString;
                break;
            default:
                _oscBuffer.Add(value);
                break;
        }
    }

    private void OscString(byte value)
    {
        if (value == (byte)'\\')
        {
            ExecuteOsc();
            _state = ParseState.Ground;
            return;
        }

        _oscBuffer.Add(0x1b);
        _oscBuffer.Add(value);
        _state = ParseState.OscEntry;
    }

    private void HandleC0(byte value)
    {
        switch (value)
        {
            case 0x08 when _cursorColumn > 0:
                _cursorColumn--;
                break;
            case 0x09:
                _cursorColumn = (ushort)Math.Min(((_cursorColumn / 8) + 1) * 8, Width - 1);
                break;
            case 0x0a when _cursorRow + 1 < Height:
                _cursorRow++;
                break;
            case 0x0d:
                _cursorColumn = 0;
                break;
        }
    }

    private void HandlePrintable(byte value)
    {
        if (_utf8Expected is null)
        {
            if (value < 0x80)
            {
                PutRune(new Rune(value));
                return;
            }

            if (Utf8ExpectedLength(value) is { } expected)
            {
                _utf8Pending.Clear();
                _utf8Pending.Add(value);
                _utf8Expected = expected;
                if (expected == 1)
                {
                    FlushUtf8Sequence();
                }
            }
            else
            {
                PutRune(Rune.ReplacementChar);
            }

            return;
        }

        if (value is < 0x80 or > 0xbf)
        {
            FlushPendingUtf8Invalid();
            HandlePrintable(value);
            return;
        }

        _utf8Pending.Add(value);
        if (_utf8Pending.Count == _utf8Expected)
        {
            FlushUtf8Sequence();
        }
        else if (_utf8Pending.Count > _utf8Expected)
        {
            FlushPendingUtf8Invalid();
        }
    }

    private void FlushUtf8Sequence()
    {
        Span<byte> bytes = stackalloc byte[4];
        for (var index = 0; index < _utf8Pending.Count; index++)
        {
            bytes[index] = _utf8Pending[index];
        }

        var status = Rune.DecodeFromUtf8(bytes[.._utf8Pending.Count], out var rune, out var consumed);
        _utf8Pending.Clear();
        _utf8Expected = null;
        PutRune(status == OperationStatus.Done && consumed > 0 ? rune : Rune.ReplacementChar);
    }

    private void FlushPendingUtf8Invalid()
    {
        if (_utf8Expected is null)
        {
            return;
        }

        PutRune(Rune.ReplacementChar);
        _utf8Pending.Clear();
        _utf8Expected = null;
    }

    private static int? Utf8ExpectedLength(byte first) => first switch
    {
        <= 0x7f => 1,
        >= 0xc2 and <= 0xdf => 2,
        >= 0xe0 and <= 0xef => 3,
        >= 0xf0 and <= 0xf4 => 4,
        _ => null
    };

    private void PutRune(Rune rune)
    {
        var width = TerminalTextWidth.RuneWidth(rune);
        if (width == 0)
        {
            var index = _cursorColumn > 0
                ? (_cursorRow * Width) + _cursorColumn - 1
                : (_cursorRow * Width) + _cursorColumn;
            var current = _cells[index];
            _cells[index] = current with
            {
                Text = _cursorColumn == 0 && current.Text == " "
                    ? " " + rune
                    : current.Text + rune
            };
            return;
        }

        if (_cursorColumn < Width && _cursorRow < Height)
        {
            var index = (_cursorRow * Width) + _cursorColumn;
            _cells[index] = new ModelCell(
                rune.ToString(),
                _sgr.Foreground,
                _sgr.Background,
                new CellAttributes(_sgr.Flags, _activeLinkId),
                _activeLinkId);

            if (width == 2 && _cursorColumn + 1 < Width)
            {
                _cells[index + 1] = new ModelCell(
                    string.Empty,
                    _sgr.Foreground,
                    _sgr.Background,
                    CellAttributes.None,
                    0);
            }
        }

        var advanced = _cursorColumn + width;
        if (advanced >= Width)
        {
            _cursorColumn = 0;
            if (_cursorRow + 1 < Height)
            {
                _cursorRow++;
            }
        }
        else
        {
            _cursorColumn = (ushort)advanced;
        }
    }

    private void ExecuteCsi(byte finalByte)
    {
        var privateMode = _csiIntermediate.Contains((byte)'?');
        switch (finalByte)
        {
            case (byte)'H':
            case (byte)'f':
                CursorPosition();
                break;
            case (byte)'A':
                _cursorRow = SaturatingSubtract(_cursorRow, FirstParameterOrOne());
                break;
            case (byte)'B':
                _cursorRow = AddClamped(_cursorRow, FirstParameterOrOne(), Height - 1);
                break;
            case (byte)'C':
                _cursorColumn = AddClamped(_cursorColumn, FirstParameterOrOne(), Width - 1);
                break;
            case (byte)'D':
                _cursorColumn = SaturatingSubtract(_cursorColumn, FirstParameterOrOne());
                break;
            case (byte)'G':
                _cursorColumn = OneBasedClamped(FirstParameterOrOne(), Width - 1);
                break;
            case (byte)'d':
                _cursorRow = OneBasedClamped(FirstParameterOrOne(), Height - 1);
                break;
            case (byte)'J':
                EraseDisplay();
                break;
            case (byte)'K':
                EraseLine();
                break;
            case (byte)'m':
                ApplySgr();
                break;
            case (byte)'h' when privateMode:
                ApplyPrivateMode(enabled: true);
                break;
            case (byte)'l' when privateMode:
                ApplyPrivateMode(enabled: false);
                break;
        }
    }

    private void CursorPosition()
    {
        var row = _csiParameters.Count > 0 ? Math.Max(_csiParameters[0], 1) : 1;
        var column = _csiParameters.Count > 1 ? Math.Max(_csiParameters[1], 1) : 1;
        _cursorRow = OneBasedClamped(row, Height - 1);
        _cursorColumn = OneBasedClamped(column, Width - 1);
    }

    private uint FirstParameterOrOne() =>
        _csiParameters.Count == 0 ? 1 : Math.Max(_csiParameters[0], 1);

    private void EraseDisplay()
    {
        var mode = _csiParameters.Count == 0 ? 0 : _csiParameters[0];
        switch (mode)
        {
            case 0:
                for (var x = (int)_cursorColumn; x < Width; x++)
                {
                    EraseCell((ushort)x, _cursorRow);
                }

                for (var y = _cursorRow + 1; y < Height; y++)
                {
                    for (ushort x = 0; x < Width; x++)
                    {
                        EraseCell(x, (ushort)y);
                    }
                }

                break;
            case 1:
                for (ushort y = 0; y < _cursorRow; y++)
                {
                    for (ushort x = 0; x < Width; x++)
                    {
                        EraseCell(x, y);
                    }
                }

                for (ushort x = 0; x <= _cursorColumn; x++)
                {
                    EraseCell(x, _cursorRow);
                }

                break;
            case 2:
            case 3:
                Array.Fill(_cells, ModelCell.Default);
                break;
        }
    }

    private void EraseLine()
    {
        var mode = _csiParameters.Count == 0 ? 0 : _csiParameters[0];
        switch (mode)
        {
            case 0:
                for (var x = (int)_cursorColumn; x < Width; x++)
                {
                    EraseCell((ushort)x, _cursorRow);
                }

                break;
            case 1:
                for (ushort x = 0; x <= _cursorColumn; x++)
                {
                    EraseCell(x, _cursorRow);
                }

                break;
            case 2:
                for (ushort x = 0; x < Width; x++)
                {
                    EraseCell(x, _cursorRow);
                }

                break;
        }
    }

    private void EraseCell(ushort column, ushort row)
    {
        _cells[(row * Width) + column] = new ModelCell(
            " ",
            PackedRgba.White,
            _sgr.Background,
            CellAttributes.None,
            0);
    }

    private void ApplySgr()
    {
        if (_csiParameters.Count == 0)
        {
            _sgr = SgrState.Default;
            return;
        }

        for (var index = 0; index < _csiParameters.Count; index++)
        {
            var code = _csiParameters[index];
            switch (code)
            {
                case 0:
                    _sgr = SgrState.Default;
                    break;
                case 1:
                    AddFlag(CellStyleFlags.Bold);
                    break;
                case 2:
                    AddFlag(CellStyleFlags.Dim);
                    break;
                case 3:
                    AddFlag(CellStyleFlags.Italic);
                    break;
                case 4:
                    AddFlag(CellStyleFlags.Underline);
                    break;
                case 5:
                    AddFlag(CellStyleFlags.Blink);
                    break;
                case 7:
                    AddFlag(CellStyleFlags.Reverse);
                    break;
                case 8:
                    AddFlag(CellStyleFlags.Hidden);
                    break;
                case 9:
                    AddFlag(CellStyleFlags.Strikethrough);
                    break;
                case 21:
                case 22:
                    RemoveFlag(CellStyleFlags.Bold | CellStyleFlags.Dim);
                    break;
                case 23:
                    RemoveFlag(CellStyleFlags.Italic);
                    break;
                case 24:
                    RemoveFlag(CellStyleFlags.Underline);
                    break;
                case 25:
                    RemoveFlag(CellStyleFlags.Blink);
                    break;
                case 27:
                    RemoveFlag(CellStyleFlags.Reverse);
                    break;
                case 28:
                    RemoveFlag(CellStyleFlags.Hidden);
                    break;
                case 29:
                    RemoveFlag(CellStyleFlags.Strikethrough);
                    break;
                case >= 30 and <= 37:
                    _sgr = _sgr with { Foreground = BasicColor(code - 30) };
                    break;
                case 39:
                    _sgr = _sgr with { Foreground = PackedRgba.White };
                    break;
                case >= 40 and <= 47:
                    _sgr = _sgr with { Background = BasicColor(code - 40) };
                    break;
                case 49:
                    _sgr = _sgr with { Background = PackedRgba.Transparent };
                    break;
                case >= 90 and <= 97:
                    _sgr = _sgr with { Foreground = BrightColor(code - 90) };
                    break;
                case >= 100 and <= 107:
                    _sgr = _sgr with { Background = BrightColor(code - 100) };
                    break;
                case 38:
                    if (TryParseExtendedColor(ref index, out var foreground))
                    {
                        _sgr = _sgr with { Foreground = foreground };
                    }

                    break;
                case 48:
                    if (TryParseExtendedColor(ref index, out var background))
                    {
                        _sgr = _sgr with { Background = background };
                    }

                    break;
            }
        }
    }

    private bool TryParseExtendedColor(ref int index, out PackedRgba color)
    {
        color = default;
        if (index + 1 >= _csiParameters.Count)
        {
            return false;
        }

        switch (_csiParameters[index + 1])
        {
            case 5 when index + 2 < _csiParameters.Count:
                color = Color256(unchecked((byte)_csiParameters[index + 2]));
                index += 2;
                return true;
            case 2 when index + 4 < _csiParameters.Count:
                color = PackedRgba.Rgb(
                    unchecked((byte)_csiParameters[index + 2]),
                    unchecked((byte)_csiParameters[index + 3]),
                    unchecked((byte)_csiParameters[index + 4]));
                index += 4;
                return true;
            default:
                return false;
        }
    }

    private void AddFlag(CellStyleFlags flag) => _sgr = _sgr with { Flags = _sgr.Flags | flag };

    private void RemoveFlag(CellStyleFlags flag) => _sgr = _sgr with { Flags = _sgr.Flags & ~flag };

    private void ApplyPrivateMode(bool enabled)
    {
        foreach (var code in _csiParameters)
        {
            switch (code)
            {
                case 25:
                    _modes = _modes with { CursorVisible = enabled };
                    break;
                case 1049:
                    _modes = _modes with { AlternateScreen = enabled };
                    break;
                case 2026:
                    _modes = _modes with
                    {
                        SynchronizedOutputLevel = enabled
                            ? _modes.SynchronizedOutputLevel + 1
                            : _modes.SynchronizedOutputLevel > 0
                                ? _modes.SynchronizedOutputLevel - 1
                                : 0
                    };
                    break;
            }
        }
    }

    private void ExecuteOsc()
    {
        var data = Encoding.UTF8.GetString(_oscBuffer.ToArray());
        var separator = data.IndexOf(';');
        var codeText = separator >= 0 ? data[..separator] : data;
        if (!uint.TryParse(codeText, out var code) || code != 8 || separator < 0)
        {
            return;
        }

        var remainder = data[(separator + 1)..];
        var parameterSeparator = remainder.IndexOf(';');
        var uri = parameterSeparator >= 0 ? remainder[(parameterSeparator + 1)..] : string.Empty;
        if (uri.Length == 0)
        {
            _activeLinkId = 0;
            return;
        }

        _links.Add(uri);
        _activeLinkId = checked((uint)(_links.Count - 1));
    }

    private static PackedRgba BasicColor(uint index) => index switch
    {
        0 => PackedRgba.Rgb(0, 0, 0),
        1 => PackedRgba.Rgb(128, 0, 0),
        2 => PackedRgba.Rgb(0, 128, 0),
        3 => PackedRgba.Rgb(128, 128, 0),
        4 => PackedRgba.Rgb(0, 0, 128),
        5 => PackedRgba.Rgb(128, 0, 128),
        6 => PackedRgba.Rgb(0, 128, 128),
        7 => PackedRgba.Rgb(192, 192, 192),
        _ => PackedRgba.White
    };

    private static PackedRgba BrightColor(uint index) => index switch
    {
        0 => PackedRgba.Rgb(128, 128, 128),
        1 => PackedRgba.Rgb(255, 0, 0),
        2 => PackedRgba.Rgb(0, 255, 0),
        3 => PackedRgba.Rgb(255, 255, 0),
        4 => PackedRgba.Rgb(0, 0, 255),
        5 => PackedRgba.Rgb(255, 0, 255),
        6 => PackedRgba.Rgb(0, 255, 255),
        7 => PackedRgba.Rgb(255, 255, 255),
        _ => PackedRgba.White
    };

    private static PackedRgba Color256(byte index)
    {
        if (index <= 7)
        {
            return BasicColor(index);
        }

        if (index <= 15)
        {
            return BrightColor((uint)(index - 8));
        }

        if (index <= 231)
        {
            var cube = index - 16;
            var red = (byte)((cube / 36) % 6);
            var green = (byte)((cube / 6) % 6);
            var blue = (byte)(cube % 6);
            return PackedRgba.Rgb(ColorCubeChannel(red), ColorCubeChannel(green), ColorCubeChannel(blue));
        }

        var gray = (byte)(8 + ((index - 232) * 10));
        return PackedRgba.Rgb(gray, gray, gray);
    }

    private static byte ColorCubeChannel(byte value) => value == 0 ? (byte)0 : (byte)(55 + (value * 40));

    private bool TryIndex(ushort column, ushort row, out int index)
    {
        if (column >= Width || row >= Height)
        {
            index = -1;
            return false;
        }

        index = (row * Width) + column;
        return true;
    }

    private static ushort SaturatingSubtract(ushort value, uint decrement) =>
        decrement >= value ? (ushort)0 : (ushort)(value - decrement);

    private static ushort AddClamped(ushort value, uint increment, int maximum) =>
        (ushort)Math.Min((ulong)value + increment, (ulong)maximum);

    private static ushort OneBasedClamped(uint value, int maximum) =>
        (ushort)Math.Min((ulong)Math.Max(value, 1) - 1, (ulong)maximum);

    private static string Quote(string text) =>
        "\"" + text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal) + "\"";

    public readonly record struct ModelCell(
        string Text,
        PackedRgba Foreground,
        PackedRgba Background,
        CellAttributes Attributes,
        uint LinkId)
    {
        public ModelCell(
            string text,
            PackedRgba foreground,
            PackedRgba background,
            CellAttributes attributes)
            : this(text, foreground, background, attributes, attributes.LinkId)
        {
        }

        public static ModelCell Default =>
            new(" ", PackedRgba.White, PackedRgba.Transparent, CellAttributes.None, 0);

        public PackedRgba Fg => Foreground;

        public PackedRgba Bg => Background;

        public static ModelCell WithChar(char value) =>
            new(value.ToString(), PackedRgba.White, PackedRgba.Transparent, CellAttributes.None, 0);

        public static ModelCell WithChar(Rune value) =>
            new(value.ToString(), PackedRgba.White, PackedRgba.Transparent, CellAttributes.None, 0);
    }

    private enum ParseState : byte
    {
        Ground,
        Escape,
        CsiEntry,
        CsiParameter,
        OscEntry,
        OscString
    }

    private readonly record struct SavedCursorState(
        ushort Column,
        ushort Row,
        SgrState Sgr,
        uint LinkId);
}
