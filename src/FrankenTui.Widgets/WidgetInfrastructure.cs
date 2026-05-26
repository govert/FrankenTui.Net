using FrankenTui.Core;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Undo/redo support for text operations. Matches upstream undo_support.</summary>
public sealed class UndoSupport
{
    private readonly List<UndoEntry> _undoStack = [];
    private readonly List<UndoEntry> _redoStack = [];
    private readonly int _maxEntries;

    public UndoSupport(int maxEntries = 100) => _maxEntries = maxEntries;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Record(string before, string after, int cursorBefore, int cursorAfter)
    {
        _undoStack.Add(new UndoEntry(before, after, cursorBefore, cursorAfter));
        while (_undoStack.Count > _maxEntries) _undoStack.RemoveAt(0);
        _redoStack.Clear();
    }

    public (string text, int cursor)? Undo(string current)
    {
        if (!CanUndo) return null;
        var entry = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _redoStack.Add(new UndoEntry(current, entry.Before, 0, entry.CursorBefore));
        return (entry.Before, entry.CursorBefore);
    }

    public (string text, int cursor)? Redo(string current)
    {
        if (!CanRedo) return null;
        var entry = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        _undoStack.Add(new UndoEntry(current, entry.Before, 0, entry.CursorBefore));
        return (entry.Before, entry.CursorBefore);
    }

    public void Clear() { _undoStack.Clear(); _redoStack.Clear(); }

    private readonly record struct UndoEntry(string Before, string After, int CursorBefore, int CursorAfter);
}

/// <summary>Widget state base with dirty tracking. Matches upstream state + stateful.</summary>
public abstract class StatefulWidget : IWidget
{
    private bool _dirty = true;
    public bool IsDirty => _dirty;
    protected void MarkDirty() => _dirty = true;
    protected void MarkClean() => _dirty = false;

    public abstract void Render(RuntimeRenderContext context);
    void IRuntimeView.Render(RuntimeRenderContext context) { if (_dirty || AlwaysRender) { Render(context); _dirty = false; } }

    protected virtual bool AlwaysRender => false;
    public virtual Size Measure(Size available) => available;
}

/// <summary>Measurement system for widget sizing. Matches upstream measurable + measure_cache.</summary>
public sealed class MeasureCache
{
    private readonly Dictionary<string, Size> _cache = [];
    private readonly int _maxEntries;

    public MeasureCache(int maxEntries = 256) => _maxEntries = maxEntries;

    public Size? Get(string key) => _cache.TryGetValue(key, out var s) ? s : null;

    public void Set(string key, Size size)
    {
        _cache[key] = size;
        while (_cache.Count > _maxEntries)
        {
            var first = _cache.Keys.First();
            _cache.Remove(first);
        }
    }

    public void Clear() => _cache.Clear();
}

public interface IMeasurableWidget : IWidget
{
    Size Measure(Size available);
}
