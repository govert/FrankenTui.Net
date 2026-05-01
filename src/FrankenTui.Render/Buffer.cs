using FrankenTui.Core;

namespace FrankenTui.Render;

public sealed class Buffer
{
    private readonly Cell[] _cells;
    private readonly GraphemeRegistry _graphemes;
    private readonly List<Rect> _scissorStack;
    private readonly List<float> _opacityStack;
    private readonly bool[] _dirtyRows;

    public Buffer(ushort width, ushort height)
    {
        Width = Math.Max(width, (ushort)1);
        Height = Math.Max(height, (ushort)1);
        _cells = new Cell[Width * Height];
        Array.Fill(_cells, Cell.Empty);
        _graphemes = new GraphemeRegistry();
        _scissorStack = [Rect.FromSize(Width, Height)];
        _opacityStack = [1f];
        _dirtyRows = Enumerable.Repeat(true, Height).ToArray();
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public int Length => _cells.Length;

    public bool IsEmpty => _cells.Length == 0;

    public Rect Bounds => Rect.FromSize(Width, Height);

    public Rect CurrentScissor => _scissorStack[^1];

    public float CurrentOpacity => _opacityStack[^1];

    public GraphemeRegistry Graphemes => _graphemes;

    public int DirtyRowCount => _dirtyRows.Count(static row => row);

    public ReadOnlySpan<Cell> GetRow(ushort y)
    {
        if (y >= Height)
        {
            return ReadOnlySpan<Cell>.Empty;
        }

        var start = y * Width;
        return _cells.AsSpan(start, Width);
    }

    public Cell? Get(ushort x, ushort y) =>
        TryIndex(x, y, out var index) ? _cells[index] : null;

    public string? ResolveText(Cell cell)
    {
        if (cell.IsContinuation)
        {
            return null;
        }

        if (cell.IsEmpty)
        {
            return " ";
        }

        if (cell.Content.IsGrapheme)
        {
            return cell.Content.GraphemeId is { } id ? _graphemes.Resolve(id) : null;
        }

        return cell.Content.AsRune()?.ToString();
    }

    public Cell CreateTextCell(string textElement, Cell template)
    {
        ArgumentNullException.ThrowIfNull(textElement);

        if (textElement.Length == 0)
        {
            return template.WithChar(' ');
        }

        var runes = textElement.EnumerateRunes().ToArray();
        if (runes.Length == 1)
        {
            return template.WithRune(runes[0]);
        }

        var width = (byte)Math.Clamp(Math.Max(TerminalTextWidth.TextElementWidth(textElement), 1), 0, GraphemeId.MaxWidth);
        var id = _graphemes.Intern(textElement, width);
        return template.WithContent(CellContent.FromGrapheme(id));
    }

    public void SetText(ushort x, ushort y, string textElement, Cell template) =>
        Set(x, y, CreateTextCell(textElement, template));

    public Buffer Clone()
    {
        var copy = new Buffer(Width, Height);
        copy.CopyFrom(this);
        return copy;
    }

    public void CopyFrom(Buffer other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Width != Width || other.Height != Height)
        {
            throw new ArgumentException("Buffers must have identical dimensions.", nameof(other));
        }

        _graphemes.Clear();

        for (ushort y = 0; y < Height; y++)
        {
            var row = other.GetRow(y);
            for (ushort x = 0; x < Width; x++)
            {
                _cells[IndexUnchecked(x, y)] = ImportCell(row[x], other);
            }
        }

        MarkAllDirty();
    }

    public void Set(ushort x, ushort y, Cell cell)
    {
        var width = cell.Content.Width();
        if (width <= 1)
        {
            if (!TryIndex(x, y, out var index) || !CurrentScissor.Contains(x, y))
            {
                return;
            }

            var spanStart = x;
            var spanEnd = SaturatingAdd(x, 1);
            var cleanup = CleanupOverlap(x, y, cell);
            if (cleanup is { } overlap)
            {
                spanStart = Math.Min(spanStart, overlap.Start);
                spanEnd = Math.Max(spanEnd, overlap.End);
            }

            var existingCell = _cells[index];
            var existingBackground = existingCell.Background;
            ReleaseCellIfNeeded(existingCell);
            var finalCell = ApplyOpacity(cell);
            finalCell = finalCell.WithBackground(finalCell.Background.Over(existingBackground));
            _cells[index] = finalCell;
            MarkDirtySpan(y, spanStart, spanEnd);
            CleanupOrphanedTails(SaturatingAdd(x, 1), y);
            return;
        }

        var scissor = CurrentScissor;
        for (var i = 0; i < width; i++)
        {
            var cx = x + i;
            if (cx >= Width || y >= Height || !scissor.Contains((ushort)cx, y))
            {
                return;
            }
        }

        var writeSpanStart = x;
        var writeSpanEnd = SaturatingAdd(x, (ushort)width);
        var headCleanup = CleanupOverlap(x, y, cell);
        if (headCleanup is { } headOverlap)
        {
            writeSpanStart = Math.Min(writeSpanStart, headOverlap.Start);
            writeSpanEnd = Math.Max(writeSpanEnd, headOverlap.End);
        }

        for (var i = 1; i < width; i++)
        {
            var cleanup = CleanupOverlap((ushort)(x + i), y, Cell.Continuation);
            if (cleanup is { } overlap)
            {
                writeSpanStart = Math.Min(writeSpanStart, overlap.Start);
                writeSpanEnd = Math.Max(writeSpanEnd, overlap.End);
            }
        }

        var headIndex = IndexUnchecked(x, y);
        var existingHead = _cells[headIndex];
        var finalWideCell = ApplyOpacity(cell);
        finalWideCell = finalWideCell.WithBackground(finalWideCell.Background.Over(existingHead.Background));
        ReleaseCellIfNeeded(existingHead);
        _cells[headIndex] = finalWideCell;
        for (var i = 1; i < width; i++)
        {
            _cells[IndexUnchecked((ushort)(x + i), y)] = Cell.Continuation;
        }

        MarkDirtySpan(y, writeSpanStart, writeSpanEnd);
        CleanupOrphanedTails(SaturatingAdd(x, (ushort)width), y);
    }

    public void SetRaw(ushort x, ushort y, Cell cell)
    {
        if (!TryIndex(x, y, out var index))
        {
            return;
        }

        var rawWideHead = cell.Content.Width() > 1 && !cell.IsContinuation;
        var spanStart = x;
        var spanEnd = SaturatingAdd(x, 1);
        if (!rawWideHead)
        {
            var cleanup = CleanupOverlap(x, y, cell);
            if (cleanup is { } overlap)
            {
                spanStart = Math.Min(spanStart, overlap.Start);
                spanEnd = Math.Max(spanEnd, overlap.End);
            }
        }

        ReleaseCellIfNeeded(_cells[index]);
        _cells[index] = cell;
        MarkDirtySpan(y, spanStart, spanEnd);
        if (!rawWideHead)
        {
            CleanupOrphanedTails(SaturatingAdd(x, 1), y);
        }
    }

    public void Fill(Rect rect, Cell cell)
    {
        var clipped = CurrentScissor.Intersection(rect);
        if (clipped.IsEmpty)
        {
            return;
        }

        for (var y = clipped.Y; y < clipped.Bottom; y++)
        {
            for (var x = clipped.X; x < clipped.Right; x++)
            {
                Set(x, y, cell);
            }
        }
    }

    public void Clear()
    {
        Array.Fill(_cells, Cell.Empty);
        _graphemes.Clear();
        MarkAllDirty();
    }

    public void MarkAllDirty() => Array.Fill(_dirtyRows, true);

    public void ClearDirty() => Array.Fill(_dirtyRows, false);

    public bool IsRowDirty(ushort y) => y < Height && _dirtyRows[y];

    public Rect PushScissor(Rect rect)
    {
        var next = CurrentScissor.Intersection(rect);
        _scissorStack.Add(next);
        return next;
    }

    public Rect PopScissor()
    {
        if (_scissorStack.Count > 1)
        {
            _scissorStack.RemoveAt(_scissorStack.Count - 1);
        }

        return CurrentScissor;
    }

    public float PushOpacity(float opacity)
    {
        var clamped = ClampOpacity(opacity);
        _opacityStack.Add(CurrentOpacity * clamped);
        return CurrentOpacity;
    }

    public float PopOpacity()
    {
        if (_opacityStack.Count > 1)
        {
            _opacityStack.RemoveAt(_opacityStack.Count - 1);
        }

        return CurrentOpacity;
    }

    private void MarkDirtySpan(ushort y, ushort start, ushort end)
    {
        if (y >= Height || start >= end)
        {
            return;
        }

        _dirtyRows[y] = true;
    }

    private Cell ApplyOpacity(Cell cell)
    {
        var opacity = CurrentOpacity;
        if (opacity >= 1f)
        {
            return cell;
        }

        return cell
            .WithForeground(cell.Foreground.WithOpacity(opacity))
            .WithBackground(cell.Background.WithOpacity(opacity));
    }

    private DirtySpan? CleanupOverlap(ushort x, ushort y, Cell newCell)
    {
        if (!TryIndex(x, y, out var index))
        {
            return null;
        }

        var current = _cells[index];
        var touched = false;
        var minX = x;
        var maxX = x;

        if (current.Content.Width() > 1)
        {
            var width = current.Content.Width();
            for (var i = 1; i < width; i++)
            {
                var cx = (ushort)(x + i);
                if (TryIndex(cx, y, out var tailIndex) && _cells[tailIndex].IsContinuation)
                {
                    _cells[tailIndex] = Cell.Empty;
                    touched = true;
                    minX = Math.Min(minX, cx);
                    maxX = Math.Max(maxX, cx);
                }
            }
        }
        else if (current.IsContinuation && !newCell.IsContinuation)
        {
            var limit = x > GraphemeId.MaxWidth ? (ushort)(x - GraphemeId.MaxWidth) : (ushort)0;
            var backX = x;
            while (backX > limit)
            {
                backX--;
                if (!TryIndex(backX, y, out var headIndex))
                {
                    break;
                }

                var headCell = _cells[headIndex];
                if (headCell.IsContinuation)
                {
                    continue;
                }

                var width = headCell.Content.Width();
                if (backX + width > x)
                {
                    _cells[headIndex] = Cell.Empty;
                    ReleaseCellIfNeeded(headCell);
                    touched = true;
                    minX = Math.Min(minX, backX);
                    maxX = Math.Max(maxX, backX);

                    for (var i = 1; i < width; i++)
                    {
                        var cx = (ushort)(backX + i);
                        if (TryIndex(cx, y, out var tailIndex) && _cells[tailIndex].IsContinuation)
                        {
                            _cells[tailIndex] = Cell.Empty;
                            touched = true;
                            minX = Math.Min(minX, cx);
                            maxX = Math.Max(maxX, cx);
                        }
                    }
                }

                break;
            }
        }

        return touched ? new DirtySpan(minX, SaturatingAdd(maxX, 1)) : null;
    }

    private void CleanupOrphanedTails(ushort startX, ushort y)
    {
        if (startX >= Width || !TryIndex(startX, y, out var index) || !_cells[index].IsContinuation)
        {
            return;
        }

        var x = startX;
        var currentIndex = index;
        var maxX = x;
        var rowEnd = (y * Width) + Width;
        while (currentIndex < rowEnd && _cells[currentIndex].IsContinuation)
        {
            _cells[currentIndex] = Cell.Empty;
            maxX = x;
            x++;
            currentIndex++;
        }

        MarkDirtySpan(y, startX, SaturatingAdd(maxX, 1));
    }

    private bool TryIndex(ushort x, ushort y, out int index)
    {
        if (x < Width && y < Height)
        {
            index = (y * Width) + x;
            return true;
        }

        index = -1;
        return false;
    }

    private int IndexUnchecked(ushort x, ushort y) => (y * Width) + x;

    private static ushort SaturatingAdd(ushort left, ushort right)
    {
        var sum = left + right;
        return sum >= ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
    }

    private Cell ImportCell(Cell cell, Buffer other)
    {
        if (!cell.Content.IsGrapheme || cell.Content.GraphemeId is not { } id)
        {
            return cell;
        }

        var text = other.ResolveText(cell);
        if (string.IsNullOrEmpty(text))
        {
            return cell;
        }

        var width = (byte)Math.Clamp(cell.Content.Width(), 0, GraphemeId.MaxWidth);
        var importedId = _graphemes.Intern(text, width);
        return cell.WithContent(CellContent.FromGrapheme(importedId));
    }

    private void ReleaseCellIfNeeded(Cell cell)
    {
        if (!cell.Content.IsGrapheme || cell.Content.GraphemeId is not { } id)
        {
            return;
        }

        _graphemes.Release(id);
    }

    private static float ClampOpacity(float opacity)
    {
        if (float.IsNaN(opacity) || opacity <= 0f)
        {
            return 0f;
        }

        if (float.IsPositiveInfinity(opacity) || opacity >= 1f)
        {
            return 1f;
        }

        return opacity;
    }

    private readonly record struct DirtySpan(ushort Start, ushort End);
}
