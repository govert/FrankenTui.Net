// Upstream source: crates/ftui-widgets/src/undo_support.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of UndoWidgetId, TextEditOperation, SelectionOperation,
// TreeOperation, ListOperation, TableOperation, WidgetTextEditCmd, and
// the UndoSupport, TextInputUndoExt, TreeUndoExt, ListUndoExt, TableUndoExt traits.

using System.Threading;

namespace FrankenTui.Widgets;

// =============================================================================
// Simple Result<T, TError> for porting Rust-style Result returns.
// =============================================================================

/// <summary>Simple Result type for porting Rust-style Result returns.</summary>
public readonly record struct Result<T, TError>
{
    private readonly T? _value;
    private readonly TError? _error;
    private readonly bool _isOk;

    private Result(T value)
    {
        _value = value;
        _error = default;
        _isOk = true;
    }

    private Result(TError error)
    {
        _value = default;
        _error = error;
        _isOk = false;
    }

    public bool IsOk => _isOk;
    public bool IsErr => !_isOk;

    public T Unwrap()
    {
        if (!_isOk) throw new System.InvalidOperationException($"Called Unwrap on an Err result: {_error}");
        return _value!;
    }

    public TError UnwrapErr()
    {
        if (_isOk) throw new System.InvalidOperationException("Called UnwrapErr on an Ok result");
        return _error!;
    }

    public static Result<T, TError> Ok(T value) => new(value);
    public static Result<T, TError> Err(TError error) => new(error);
}

/// <summary>Unit type, analogous to Rust's ().</summary>
public readonly record struct Unit
{
    public static readonly Unit Default = new();
}

// =============================================================================
// UndoWidgetId
// =============================================================================

/// <summary>
/// Unique identifier for a widget instance.
/// Used to associate undo commands with specific widgets.
/// </summary>
public readonly record struct UndoWidgetId(ulong Raw) : IComparable<UndoWidgetId>
{
    private static ulong _nextId = 1;

    /// <summary>Create a new unique widget ID.</summary>
    public static UndoWidgetId New() =>
        new(Interlocked.Increment(ref _nextId));

    /// <summary>Create a widget ID from a raw value.</summary>
    public static UndoWidgetId FromRaw(ulong id) => new(id);

    public int CompareTo(UndoWidgetId other) => Raw.CompareTo(other.Raw);

    public override string ToString() => $"Widget({Raw})";
}

// =============================================================================
// TextEditOperation
// =============================================================================

/// <summary>Text edit operation types.</summary>
public abstract record TextEditOperation
{
    /// <summary>Insert text at a position.</summary>
    public sealed record Insert(int Position, string Text) : TextEditOperation;
    /// <summary>Delete text at a position.</summary>
    public sealed record Delete(int Position, string DeletedText) : TextEditOperation;
    /// <summary>Replace text at a position.</summary>
    public sealed record Replace(int Position, string OldText, string NewText) : TextEditOperation;
    /// <summary>Set the entire value.</summary>
    public sealed record SetValue(string OldValue, string NewValue) : TextEditOperation;

    private TextEditOperation() { }

    /// <summary>Get a description of this operation.</summary>
    public string Description() => this switch
    {
        Insert => "Insert text",
        Delete => "Delete text",
        Replace => "Replace text",
        SetValue => "Set value",
        _ => "Unknown",
    };

    /// <summary>Calculate the approximate size in bytes of this operation.</summary>
    public int SizeBytes() => this switch
    {
        Insert { Text: var t } => t.Length,
        Delete { DeletedText: var d } => d.Length,
        Replace { OldText: var o, NewText: var n } => o.Length + n.Length,
        SetValue { OldValue: var o, NewValue: var n } => o.Length + n.Length,
        _ => 0,
    };
}

// =============================================================================
// SelectionOperation
// =============================================================================

/// <summary>Selection state operation types.</summary>
public abstract record SelectionOperation
{
    /// <summary>Selection changed.</summary>
    public sealed record Changed(int? OldAnchor, int OldCursor, int? NewAnchor, int NewCursor) : SelectionOperation;
    private SelectionOperation() { }
}

// =============================================================================
// TreeOperation
// =============================================================================

/// <summary>Tree expansion operation types.</summary>
public abstract record TreeOperation
{
    /// <summary>Node expanded.</summary>
    public sealed record Expand(int[] Path) : TreeOperation;
    /// <summary>Node collapsed.</summary>
    public sealed record Collapse(int[] Path) : TreeOperation;
    /// <summary>Multiple nodes toggled.</summary>
    public sealed record ToggleBatch(int[][] Expanded, int[][] Collapsed) : TreeOperation;
    private TreeOperation() { }

    /// <summary>Get a description of this operation.</summary>
    public string Description() => this switch
    {
        Expand => "Expand node",
        Collapse => "Collapse node",
        ToggleBatch => "Toggle nodes",
        _ => "Unknown",
    };
}

// =============================================================================
// ListOperation
// =============================================================================

/// <summary>List selection operation types.</summary>
public abstract record ListOperation
{
    /// <summary>Selection changed.</summary>
    public sealed record Select(int? OldSelection, int? NewSelection) : ListOperation;
    /// <summary>Multiple selection changed.</summary>
    public sealed record MultiSelect(int[] OldSelections, int[] NewSelections) : ListOperation;
    private ListOperation() { }

    /// <summary>Get a description of this operation.</summary>
    public string Description() => this switch
    {
        Select => "Change selection",
        MultiSelect => "Change selections",
        _ => "Unknown",
    };
}

// =============================================================================
// TableOperation
// =============================================================================

/// <summary>Table operation types.</summary>
public abstract record TableOperation
{
    /// <summary>Sort column changed.</summary>
    public sealed record Sort(int? OldColumn, bool OldAscending, int? NewColumn, bool NewAscending) : TableOperation;
    /// <summary>Filter applied.</summary>
    public sealed record Filter(string OldFilter, string NewFilter) : TableOperation;
    /// <summary>Row selection changed.</summary>
    public sealed record SelectRow(int? OldRow, int? NewRow) : TableOperation;
    private TableOperation() { }

    /// <summary>Get a description of this operation.</summary>
    public string Description() => this switch
    {
        Sort => "Change sort",
        Filter => "Apply filter",
        SelectRow => "Select row",
        _ => "Unknown",
    };
}

// =============================================================================
// Delegate types for command callbacks
// =============================================================================

/// <summary>
/// Delegate for applying a text edit operation.
/// </summary>
public delegate Result<Unit, string> TextEditApplyFn(UndoWidgetId widgetId, TextEditOperation operation);

/// <summary>
/// Delegate for undoing a text edit operation.
/// </summary>
public delegate Result<Unit, string> TextEditUndoFn(UndoWidgetId widgetId, TextEditOperation operation);

// =============================================================================
// WidgetTextEditCmd
// =============================================================================

/// <summary>
/// A widget undo command for text editing.
/// </summary>
public sealed class WidgetTextEditCmd
{
    private readonly UndoWidgetId _widgetId;
    private readonly TextEditOperation _operation;
    private TextEditApplyFn? _applyFn;
    private TextEditUndoFn? _undoFn;
    private bool _executed;

    /// <summary>Create a new text edit command.</summary>
    public WidgetTextEditCmd(UndoWidgetId widgetId, TextEditOperation operation)
    {
        _widgetId = widgetId;
        _operation = operation;
    }

    /// <summary>Set the apply callback (builder).</summary>
    public WidgetTextEditCmd WithApply(TextEditApplyFn applyFn)
    {
        _applyFn = applyFn;
        return this;
    }

    /// <summary>Set the undo callback (builder).</summary>
    public WidgetTextEditCmd WithUndo(TextEditUndoFn undoFn)
    {
        _undoFn = undoFn;
        return this;
    }

    /// <summary>Get the widget ID.</summary>
    public UndoWidgetId WidgetId => _widgetId;

    /// <summary>Get the operation.</summary>
    public TextEditOperation Operation => _operation;

    /// <summary>Execute the command.</summary>
    public Result<Unit, string> Execute()
    {
        if (_applyFn is not null)
        {
            var result = _applyFn(_widgetId, _operation);
            if (result.IsErr) return result;
        }
        _executed = true;
        return Result<Unit, string>.Ok(Unit.Default);
    }

    /// <summary>Undo the command.</summary>
    public Result<Unit, string> Undo()
    {
        if (_undoFn is not null)
        {
            var result = _undoFn(_widgetId, _operation);
            if (result.IsErr) return result;
        }
        _executed = false;
        return Result<Unit, string>.Ok(Unit.Default);
    }

    /// <summary>Redo the command (same as execute).</summary>
    public Result<Unit, string> Redo() => Execute();

    /// <summary>Get the description.</summary>
    public string Description() => _operation.Description();

    /// <summary>Get the approximate size in bytes.</summary>
    public int SizeBytes() => _operation.SizeBytes();
}

// =============================================================================
// IUndoSupport trait
// =============================================================================

/// <summary>
/// Trait for widgets that support undo operations.
/// Widgets implement this interface to provide undo/redo functionality
/// via the Command Pattern.
/// </summary>
public interface IUndoSupport
{
    /// <summary>Get the widget's unique ID for undo tracking.</summary>
    UndoWidgetId UndoWidgetId { get; }

    /// <summary>Create a snapshot of the current state for undo purposes.</summary>
    object CreateSnapshot();

    /// <summary>Restore state from a snapshot. Returns true if the restore was successful.</summary>
    bool RestoreSnapshot(object snapshot);
}

// =============================================================================
// Extension traits for specific widget types
// =============================================================================

/// <summary>Extension interface for text input widgets with undo support.</summary>
public interface ITextInputUndoExt : IUndoSupport
{
    /// <summary>Get the current text value.</summary>
    string TextValue { get; }
    /// <summary>Set the text value directly (for undo/redo).</summary>
    void SetTextValue(string value);
    /// <summary>Get the current cursor position.</summary>
    int CursorPosition { get; }
    /// <summary>Set the cursor position directly.</summary>
    void SetCursorPosition(int pos);
    /// <summary>Insert text at a position.</summary>
    void InsertTextAt(int position, string text);
    /// <summary>Delete text at a range.</summary>
    void DeleteTextRange(int start, int end);
}

/// <summary>Extension interface for tree widgets with undo support.</summary>
public interface ITreeUndoExt : IUndoSupport
{
    /// <summary>Check if a node is expanded.</summary>
    bool IsNodeExpanded(int[] path);
    /// <summary>Expand a node.</summary>
    void ExpandNode(int[] path);
    /// <summary>Collapse a node.</summary>
    void CollapseNode(int[] path);
}

/// <summary>Extension interface for list widgets with undo support.</summary>
public interface IListUndoExt : IUndoSupport
{
    /// <summary>Get the current selection.</summary>
    int? SelectedIndex { get; }
    /// <summary>Set the selection.</summary>
    void SetSelectedIndex(int? index);
}

/// <summary>Extension interface for table widgets with undo support.</summary>
public interface ITableUndoExt : IUndoSupport
{
    /// <summary>Get the current sort state (column, ascending).</summary>
    (int? Column, bool Ascending) SortState { get; }
    /// <summary>Set the sort state.</summary>
    void SetSortState(int? column, bool ascending);
    /// <summary>Get the current filter text.</summary>
    string FilterText { get; }
    /// <summary>Set the filter text.</summary>
    void SetFilterText(string filter);
}
