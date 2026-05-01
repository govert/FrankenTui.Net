using System.Text;
using FrankenTui.Core;

namespace FrankenTui.Render;

public sealed class TerminalModel
{
    private static readonly ModelCell DefaultCell = new(" ", PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);

    private readonly ModelCell[] _cells;
    private readonly Dictionary<uint, string> _links = [];
    private readonly StringBuilder _csiBuffer = new();
    private readonly StringBuilder _oscBuffer = new();

    private CellAttributes _currentAttributes = CellAttributes.None;
    private PackedRgba _currentForeground = PackedRgba.White;
    private PackedRgba _currentBackground = PackedRgba.Transparent;
    private bool _oscSawEscape;
    private SavedCursorState? _savedCursor;
    private ParseState _state;

    public TerminalModel(ushort width, ushort height)
    {
        Width = Math.Max(width, (ushort)1);
        Height = Math.Max(height, (ushort)1);
        _cells = Enumerable.Repeat(DefaultCell, Width * Height).ToArray();
        _links[0] = string.Empty;
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public ushort CursorColumn { get; private set; }

    public ushort CursorRow { get; private set; }

    public bool CursorVisible { get; private set; } = true;

    public int SyncOutputDepth { get; private set; }

    public uint ActiveLinkId { get; private set; }

    public void Process(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (_state == ParseState.Ground &&
                char.IsHighSurrogate(ch) &&
                i + 1 < text.Length &&
                char.IsLowSurrogate(text[i + 1]))
            {
                ProcessRune(new Rune(ch, text[i + 1]));
                i++;
                continue;
            }

            ProcessChar(ch);
        }
    }

    public void Process(ReadOnlySpan<byte> bytes) => Process(Encoding.UTF8.GetString(bytes));

    public ModelCell? Cell(ushort column, ushort row) =>
        TryIndex(column, row, out var index) ? _cells[index] : null;

    public string RowText(ushort row)
    {
        if (row >= Height)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Width);
        for (ushort column = 0; column < Width; column++)
        {
            var cell = _cells[IndexUnchecked(column, row)];
            if (cell.Text.Length == 0)
            {
                continue;
            }

            builder.Append(cell.Text);
        }

        return builder.ToString().TrimEnd();
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

    public string? LinkUrl(uint linkId) => _links.TryGetValue(linkId, out var url) ? url : null;

    private void ProcessChar(char ch)
    {
        switch (_state)
        {
            case ParseState.Ground:
                Ground(ch);
                break;
            case ParseState.Escape:
                Escape(ch);
                break;
            case ParseState.Csi:
                Csi(ch);
                break;
            case ParseState.Osc:
                Osc(ch);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Ground(char ch)
    {
        switch (ch)
        {
            case '\u001b':
                _state = ParseState.Escape;
                return;
            case '\r':
                CursorColumn = 0;
                return;
            case '\n':
                CursorRow = (ushort)Math.Min(CursorRow + 1, Height - 1);
                return;
            case '\b':
                CursorColumn = (ushort)Math.Max(CursorColumn - 1, 0);
                return;
            default:
                ProcessRune(new Rune(ch));
                return;
        }
    }

    private void Escape(char ch)
    {
        switch (ch)
        {
            case '[':
                _csiBuffer.Clear();
                _state = ParseState.Csi;
                return;
            case ']':
                _oscBuffer.Clear();
                _oscSawEscape = false;
                _state = ParseState.Osc;
                return;
            case '7':
                _savedCursor = new SavedCursorState(
                    CursorColumn,
                    CursorRow,
                    _currentForeground,
                    _currentBackground,
                    _currentAttributes,
                    ActiveLinkId);
                _state = ParseState.Ground;
                return;
            case '8':
                if (_savedCursor is { } saved)
                {
                    CursorColumn = saved.Column;
                    CursorRow = saved.Row;
                    _currentForeground = saved.Foreground;
                    _currentBackground = saved.Background;
                    _currentAttributes = saved.Attributes;
                    ActiveLinkId = saved.LinkId;
                }

                _state = ParseState.Ground;
                return;
            default:
                _state = ParseState.Ground;
                return;
        }
    }

    private void Csi(char ch)
    {
        if (ch >= '@' && ch <= '~')
        {
            HandleCsi(_csiBuffer.ToString(), ch);
            _state = ParseState.Ground;
            _csiBuffer.Clear();
            return;
        }

        _csiBuffer.Append(ch);
    }

    private void Osc(char ch)
    {
        if (_oscSawEscape)
        {
            if (ch == '\\')
            {
                HandleOsc(_oscBuffer.ToString());
                _state = ParseState.Ground;
                _oscBuffer.Clear();
                _oscSawEscape = false;
                return;
            }

            _oscBuffer.Append('\u001b');
            _oscSawEscape = false;
        }

        if (ch == '\u001b')
        {
            _oscSawEscape = true;
            return;
        }

        if (ch == '\a')
        {
            HandleOsc(_oscBuffer.ToString());
            _state = ParseState.Ground;
            _oscBuffer.Clear();
            return;
        }

        _oscBuffer.Append(ch);
    }

    private void ProcessRune(Rune rune)
    {
        var text = rune.ToString();
        var width = Math.Max(TerminalTextWidth.RuneWidth(rune), 1);
        if (TryIndex(CursorColumn, CursorRow, out var index))
        {
            _cells[index] = new ModelCell(text, _currentForeground, _currentBackground, _currentAttributes.WithLink(ActiveLinkId));
        }

        if (width > 1 && CursorColumn + 1 < Width && TryIndex((ushort)(CursorColumn + 1), CursorRow, out var tailIndex))
        {
            _cells[tailIndex] = new ModelCell(string.Empty, _currentForeground, _currentBackground, _currentAttributes.WithLink(ActiveLinkId));
        }

        CursorColumn = (ushort)Math.Min(Width, CursorColumn + width);
    }

    private void HandleCsi(string payload, char final)
    {
        var privateMode = payload.StartsWith('?');
        var arguments = privateMode ? payload[1..] : payload;
        var parameters = ParseCsiParameters(arguments);

        switch (final)
        {
            case 'H':
            case 'f':
                var row = GetParameter(parameters, 0, 1);
                var column = GetParameter(parameters, 1, 1);
                CursorRow = (ushort)Math.Clamp(row - 1, 0, Height - 1);
                CursorColumn = (ushort)Math.Clamp(column - 1, 0, Width - 1);
                break;
            case 'G':
                CursorColumn = (ushort)Math.Clamp(GetParameter(parameters, 0, 1) - 1, 0, Width - 1);
                break;
            case 'C':
                CursorColumn = (ushort)Math.Min(Width, CursorColumn + Math.Max(GetParameter(parameters, 0, 1), 1));
                break;
            case 'D':
                CursorColumn = (ushort)Math.Max(0, CursorColumn - Math.Max(GetParameter(parameters, 0, 1), 1));
                break;
            case 'J':
                if ((EraseDisplayMode)GetParameter(parameters, 0, 0) == EraseDisplayMode.All)
                {
                    Array.Fill(_cells, DefaultCell);
                }

                break;
            case 'K':
                ApplyEraseLine((EraseLineMode)GetParameter(parameters, 0, 0));
                break;
            case 'm':
                ApplySgr(parameters);
                break;
            case 'h':
                if (privateMode)
                {
                    ApplyPrivateMode(parameters, enabled: true);
                }

                break;
            case 'l':
                if (privateMode)
                {
                    ApplyPrivateMode(parameters, enabled: false);
                }

                break;
        }
    }

    private void HandleOsc(string payload)
    {
        if (!payload.StartsWith("8;;", StringComparison.Ordinal))
        {
            return;
        }

        var url = payload[3..];
        if (string.IsNullOrEmpty(url))
        {
            ActiveLinkId = 0;
            return;
        }

        var linkId = (uint)_links.Count;
        _links[linkId] = url;
        ActiveLinkId = linkId;
    }

    private void ApplyEraseLine(EraseLineMode mode)
    {
        switch (mode)
        {
            case EraseLineMode.Right:
                for (var column = CursorColumn; column < Width; column++)
                {
                    _cells[IndexUnchecked(column, CursorRow)] = DefaultCell;
                }

                break;
            case EraseLineMode.Left:
                for (ushort column = 0; column <= CursorColumn && column < Width; column++)
                {
                    _cells[IndexUnchecked(column, CursorRow)] = DefaultCell;
                }

                break;
            case EraseLineMode.All:
                for (ushort column = 0; column < Width; column++)
                {
                    _cells[IndexUnchecked(column, CursorRow)] = DefaultCell;
                }

                break;
        }
    }

    private void ApplySgr(IReadOnlyList<int> parameters)
    {
        if (parameters.Count == 0)
        {
            ResetStyle();
            return;
        }

        for (var index = 0; index < parameters.Count; index++)
        {
            switch (parameters[index])
            {
                case 0:
                    ResetStyle();
                    break;
                case 1:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Bold);
                    break;
                case 2:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Dim);
                    break;
                case 3:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Italic);
                    break;
                case 4:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Underline);
                    break;
                case 5:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Blink);
                    break;
                case 7:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Reverse);
                    break;
                case 8:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Hidden);
                    break;
                case 9:
                    _currentAttributes = _currentAttributes.MergedFlags(CellStyleFlags.Strikethrough);
                    break;
                case 22:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~(CellStyleFlags.Bold | CellStyleFlags.Dim));
                    break;
                case 23:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~CellStyleFlags.Italic);
                    break;
                case 24:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~CellStyleFlags.Underline);
                    break;
                case 25:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~CellStyleFlags.Blink);
                    break;
                case 27:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~CellStyleFlags.Reverse);
                    break;
                case 28:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~CellStyleFlags.Hidden);
                    break;
                case 29:
                    _currentAttributes = _currentAttributes.WithFlags(_currentAttributes.Flags & ~CellStyleFlags.Strikethrough);
                    break;
                case 38 when HasTrueColor(parameters, index):
                    _currentForeground = PackedRgba.Rgb((byte)parameters[index + 2], (byte)parameters[index + 3], (byte)parameters[index + 4]);
                    index += 4;
                    break;
                case 48 when HasTrueColor(parameters, index):
                    _currentBackground = PackedRgba.Rgb((byte)parameters[index + 2], (byte)parameters[index + 3], (byte)parameters[index + 4]);
                    index += 4;
                    break;
                case 39:
                    _currentForeground = PackedRgba.White;
                    break;
                case 49:
                    _currentBackground = PackedRgba.Transparent;
                    break;
            }
        }
    }

    private void ApplyPrivateMode(IReadOnlyList<int> parameters, bool enabled)
    {
        foreach (var parameter in parameters)
        {
            switch (parameter)
            {
                case 25:
                    CursorVisible = enabled;
                    break;
                case 2026:
                    SyncOutputDepth = enabled
                        ? SyncOutputDepth + 1
                        : Math.Max(SyncOutputDepth - 1, 0);
                    break;
            }
        }
    }

    private void ResetStyle()
    {
        _currentForeground = PackedRgba.White;
        _currentBackground = PackedRgba.Transparent;
        _currentAttributes = CellAttributes.None;
    }

    private static bool HasTrueColor(IReadOnlyList<int> parameters, int index) =>
        index + 4 < parameters.Count && parameters[index + 1] == 2;

    private static int[] ParseCsiParameters(string arguments) =>
        arguments.Length == 0
            ? [0]
            : arguments.Split([';', ':']).Select(static part => int.TryParse(part, out var value) ? value : 0).ToArray();

    private static int GetParameter(IReadOnlyList<int> parameters, int index, int defaultValue) =>
        index < parameters.Count && parameters[index] != 0 ? parameters[index] : defaultValue;

    private bool TryIndex(ushort column, ushort row, out int index)
    {
        if (column >= Width || row >= Height)
        {
            index = -1;
            return false;
        }

        index = IndexUnchecked(column, row);
        return true;
    }

    private int IndexUnchecked(ushort column, ushort row) => row * Width + column;

    public readonly record struct ModelCell(
        string Text,
        PackedRgba Foreground,
        PackedRgba Background,
        CellAttributes Attributes);

    private enum ParseState : byte
    {
        Ground,
        Escape,
        Csi,
        Osc
    }

    private readonly record struct SavedCursorState(
        ushort Column,
        ushort Row,
        PackedRgba Foreground,
        PackedRgba Background,
        CellAttributes Attributes,
        uint LinkId);
}
