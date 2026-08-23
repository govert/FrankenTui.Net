// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/buffer.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Wide-fill and continuation-tail fixes: upstream 49e55c750dd29654eab7357be1e3ad5c59225b4a
// and 81673632d38ccb64bf2f4107c9dd62573d0976f6.

using FrankenTui.Core;

namespace FrankenTui.Render;

public sealed class DirtySpanConfig
{
    public bool Enabled=true; public int MaxSpansPerRow=8; public ushort MergeGap=2; public ushort GuardBand=1;
    public static DirtySpanConfig Default=>new();
    public DirtySpanConfig WithEnabled(bool e){Enabled=e;return this;}
    public DirtySpanConfig WithMaxSpansPerRow(int m){MaxSpansPerRow=m;return this;}
    public DirtySpanConfig WithMergeGap(ushort g){MergeGap=g;return this;}
    public DirtySpanConfig WithGuardBand(ushort gb){GuardBand=gb;return this;}
}

public sealed class DirtySpanStats{public int TotalSpans,RowsWithSpans,MaxSpansInRow;public double AvgSpanWidth,AvgSpansPerRow;}

public sealed class Buffer
{
    private readonly Cell[] _cells;
    private readonly GraphemeRegistry _graphemes;
    private readonly List<Rect> _scissorStack;
    private readonly List<float> _opacityStack;
    private readonly bool[] _dirtyRows;

    public Buffer(ushort width, ushort height, GraphemePool? sharedPool = null)
    {
        Width = Math.Max(width, (ushort)1);
        Height = Math.Max(height, (ushort)1);
        _cells = new Cell[Width * Height];
        Array.Fill(_cells, Cell.Empty);
        _graphemes = sharedPool is null ? new GraphemeRegistry() : new GraphemeRegistry(sharedPool);
        _scissorStack = [Rect.FromSize(Width, Height)];
        _opacityStack = [1f];
        _dirtyRows = Enumerable.Repeat(true, Height).ToArray();
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public DegradationLevel Degradation { get; set; } = DegradationLevel.Full;

    public int Length => _cells.Length;

    public ReadOnlySpan<Cell> Cells => _cells;

    public bool IsEmpty => _cells.Length == 0;

    public Rect Bounds => Rect.FromSize(Width, Height);

    public Rect CurrentScissor => _scissorStack[^1];

    public float CurrentOpacity => _opacityStack[^1];

    public GraphemeRegistry Graphemes => _graphemes;

    public int DirtyRowCount => _dirtyRows.Count(static row => row);
    public bool[] DirtyRowsArray => _dirtyRows;
    public DirtySpanConfig DirtySpanCfg=new();
    public DirtySpanConfig GetDirtySpanConfig()=>DirtySpanCfg;
    public void SetDirtySpanConfig(DirtySpanConfig c){DirtySpanCfg=c;}
    public DirtySpanStats GetDirtySpanStats()=>new DirtySpanStats{TotalSpans=0,RowsWithSpans=DirtyRowCount,MaxSpansInRow=1,AvgSpanWidth=Width,AvgSpansPerRow=1};
    public ushort ContentHeight{get{ushort maxY=0;for(ushort y=0;y<Height;y++)if(IsRowDirty(y))maxY=y;return (ushort)Math.Clamp(maxY+1,1,Height);}}

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

    internal void AttachGraphemePool(GraphemePool pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        if (_graphemes.Uses(pool)) return;

        var replacements = new (int Index, string Text, byte Width)[_cells.Count(
            static cell => cell.Content.IsGrapheme)];
        var next = 0;
        for (var i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            if (cell.Content.GraphemeId is not { } id) continue;
            var text = _graphemes.Resolve(id);
            if (text is not null)
                replacements[next++] = (i, text, (byte)Math.Clamp(cell.Content.Width(), 0, GraphemeId.MaxWidth));
        }

        foreach (var cell in _cells) ReleaseCellIfNeeded(cell);
        _graphemes.Clear();
        _graphemes.Attach(pool);
        for (var i = 0; i < next; i++)
        {
            var replacement = replacements[i];
            var id = _graphemes.Intern(replacement.Text, replacement.Width);
            _cells[replacement.Index] = _cells[replacement.Index].WithContent(CellContent.FromGrapheme(id));
        }
    }

    public void CopyFrom(Buffer other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Width != Width || other.Height != Height)
        {
            throw new ArgumentException("Buffers must have identical dimensions.", nameof(other));
        }

        Degradation = other.Degradation;

        foreach (var cell in _cells) ReleaseCellIfNeeded(cell);
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
            // A continuation written at x may be owned by a head to its left.
            // Sweeping from x + 1 would erase the owner's remaining legitimate
            // tails for width-3+ graphemes. Sweep only beyond that owner.
            var sweepFrom = cell.IsContinuation
                ? ContinuationOwnerExtent(x, y) ?? SaturatingAdd(x, 1)
                : SaturatingAdd(x, 1);
            CleanupOrphanedTails(sweepFrom, y);
        }
    }

    public void Fill(Rect rect, Cell cell)
    {
        var clipped = CurrentScissor.Intersection(rect);
        if (clipped.IsEmpty)
        {
            return;
        }

        var cellWidth = Math.Max(cell.Content.Width(), 1);
        if (cellWidth <= 1)
        {
            for (var y = clipped.Y; y < clipped.Bottom; y++)
            {
                for (var x = clipped.X; x < clipped.Right; x++)
                {
                    Set(x, y, cell);
                }
            }

            return;
        }

        // Enforce the fill rectangle as a strict clip for wide glyphs. Clear
        // first because Set atomically rejects a wide head whose tail would
        // cross the right edge; without the clear, the final partial slot kept
        // stale content from the preceding frame.
        PushScissor(clipped);
        try
        {
            for (var y = clipped.Y; y < clipped.Bottom; y++)
            {
                for (var x = clipped.X; x < clipped.Right; x++)
                {
                    Set(x, y, Cell.Empty);
                }

                var writeX = clipped.X;
                while (writeX < clipped.Right)
                {
                    Set(writeX, y, cell);
                    writeX = SaturatingAdd(writeX, (ushort)cellWidth);
                }
            }
        }
        finally
        {
            PopScissor();
        }
    }

    internal void PaintAreaColors(
        Rect rect,
        PackedRgba? foreground,
        PackedRgba? background,
        CellStyleFlags? attributes = null,
        bool compositeBackground = false)
    {
        var clipped = CurrentScissor.Intersection(rect);
        if (clipped.IsEmpty)
        {
            return;
        }

        var opacity = CurrentOpacity;
        for (var y = clipped.Y; y < clipped.Bottom; y++)
        {
            MarkDirtySpan(y, clipped.X, clipped.Right);
            for (var x = clipped.X; x < clipped.Right; x++)
            {
                var index = IndexUnchecked(x, y);
                var cell = _cells[index];
                if (foreground is { } foregroundColor)
                {
                    cell = cell.WithForeground(opacity < 1f
                        ? foregroundColor.WithOpacity(opacity)
                        : foregroundColor);
                }

                if (background is { } backgroundColor)
                {
                    var adjustedBackground = opacity < 1f
                        ? backgroundColor.WithOpacity(opacity)
                        : backgroundColor;
                    if (compositeBackground)
                    {
                        if (adjustedBackground.A == byte.MaxValue)
                        {
                            cell = cell.WithBackground(adjustedBackground);
                        }
                        else if (adjustedBackground.A != 0)
                        {
                            cell = cell.WithBackground(adjustedBackground.Over(cell.Background));
                        }
                    }
                    else
                    {
                        cell = cell.WithBackground(opacity < 1f
                            ? adjustedBackground.Over(cell.Background)
                            : adjustedBackground);
                    }
                }

                if (attributes is { } extraAttributes)
                {
                    cell = cell.WithAttributes(cell.Attributes.MergedFlags(extraAttributes));
                }

                _cells[index] = cell;
            }
        }
    }

    public void Clear()
    {
        foreach (var cell in _cells) ReleaseCellIfNeeded(cell);
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

    public void SetFast(ushort x, ushort y, Cell cell)
    {
        // The direct path is observable-equivalent to Set only for a
        // single-width cell, trivial background alpha, and base stacks.
        var backgroundAlpha = cell.Background.A;
        if (cell.Content.Width() > 1 ||
            cell.IsContinuation ||
            (backgroundAlpha != byte.MaxValue && backgroundAlpha != 0) ||
            _scissorStack.Count != 1 ||
            _opacityStack.Count != 1)
        {
            Set(x, y, cell);
            return;
        }

        if (!TryIndex(x, y, out var index))
        {
            return;
        }

        var existing = _cells[index];
        if (existing.Content.Width() > 1 || existing.IsContinuation)
        {
            Set(x, y, cell);
            return;
        }

        var finalCell = backgroundAlpha == 0
            ? cell.WithBackground(existing.Background)
            : cell;
        ReleaseCellIfNeeded(existing);
        _cells[index] = finalCell;
        MarkDirtySpan(y, x, SaturatingAdd(x, 1));
        CleanupOrphanedTails(SaturatingAdd(x, 1), y);
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

    private ushort? ContinuationOwnerExtent(ushort x, ushort y)
    {
        var limit = x > GraphemeId.MaxWidth
            ? (ushort)(x - GraphemeId.MaxWidth)
            : (ushort)0;
        var backX = x;
        while (backX > limit)
        {
            backX--;
            if (!TryIndex(backX, y, out var index))
            {
                return null;
            }

            var candidate = _cells[index];
            if (candidate.IsContinuation)
            {
                continue;
            }

            var end = SaturatingAdd(backX, (ushort)candidate.Content.Width());
            return end > x ? end : null;
        }

        return null;
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
