// Upstream source: crates/ftui-widgets/src/undo_support.rs — tests
// Tests ported from 27 #[cfg(test)] mod tests functions.

using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class UndoSupportTests
{
    [Fact] public void UndoWidgetIdUniqueness()
    {
        var id1 = UndoWidgetId.New();
        var id2 = UndoWidgetId.New();
        Assert.NotEqual(id1, id2);
    }

    [Fact] public void UndoWidgetIdFromRaw()
    {
        var id = UndoWidgetId.FromRaw(42);
        Assert.Equal(42UL, id.Raw);
    }

    [Fact] public void TextEditOperationDescription()
    {
        Assert.Equal("Insert text", new TextEditOperation.Insert(0, "x").Description());
        Assert.Equal("Delete text", new TextEditOperation.Delete(0, "x").Description());
    }

    [Fact] public void TextEditOperationSizeBytes()
    {
        var op = new TextEditOperation.Insert(0, "hello");
        Assert.Equal(5, op.SizeBytes());
    }

    [Fact] public void WidgetTextEditCmdCreation()
    {
        var widgetId = UndoWidgetId.New();
        var cmd = new WidgetTextEditCmd(widgetId, new TextEditOperation.Insert(0, "test"));
        Assert.Equal(widgetId, cmd.WidgetId);
        Assert.Equal("Insert text", cmd.Description());
    }

    [Fact] public void CmdExecuteWithoutCallbacksSucceeds()
    {
        var cmd = new WidgetTextEditCmd(UndoWidgetId.FromRaw(1), new TextEditOperation.Insert(0, "hi"));
        var result = cmd.Execute();
        Assert.True(result.IsOk);
    }

    [Fact] public void CmdUndoWithoutCallbacksSucceeds()
    {
        var cmd = new WidgetTextEditCmd(UndoWidgetId.FromRaw(1), new TextEditOperation.Delete(0, "x"));
        var result = cmd.Undo();
        Assert.True(result.IsOk);
    }

    [Fact] public void WidgetIdDisplay()
    {
        var id = UndoWidgetId.FromRaw(7);
        Assert.Equal("Widget(7)", id.ToString());
    }

    [Fact] public void WidgetIdDefaultIsUnique()
    {
        var a = UndoWidgetId.New();
        var b = UndoWidgetId.New();
        Assert.NotEqual(a, b);
    }

    [Fact] public void WidgetIdHashEq()
    {
        var id1 = UndoWidgetId.FromRaw(99);
        var id2 = UndoWidgetId.FromRaw(99);
        Assert.Equal(id1, id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact] public void TextEditReplaceDescription()
    {
        var op = new TextEditOperation.Replace(0, "old", "new");
        Assert.Equal("Replace text", op.Description());
    }

    [Fact] public void TextEditSetValueDescription()
    {
        var op = new TextEditOperation.SetValue("", "hello");
        Assert.Equal("Set value", op.Description());
    }

    [Fact] public void TextEditDeleteSizeBytes()
    {
        var op = new TextEditOperation.Delete(5, "abc");
        Assert.Equal(3, op.SizeBytes());
    }

    [Fact] public void TextEditReplaceSizeBytes()
    {
        var op = new TextEditOperation.Replace(0, "aaa", "bbbbb");
        Assert.Equal(8, op.SizeBytes());
    }

    [Fact] public void TextEditSetValueSizeBytes()
    {
        var op = new TextEditOperation.SetValue("x", "yyyy");
        Assert.Equal(5, op.SizeBytes());
    }

    [Fact] public void TreeOperationDescription()
    {
        Assert.Equal("Expand node", new TreeOperation.Expand([0]).Description());
        Assert.Equal("Collapse node", new TreeOperation.Collapse([0]).Description());
    }

    [Fact] public void TreeToggleBatchDescription()
    {
        var op = new TreeOperation.ToggleBatch([[0, 1]], [[2]]);
        Assert.Equal("Toggle nodes", op.Description());
    }

    [Fact] public void ListOperationDescription()
    {
        Assert.Equal("Change selection", new ListOperation.Select(null, 0).Description());
    }

    [Fact] public void ListMultiSelectDescription()
    {
        Assert.Equal("Change selections", new ListOperation.MultiSelect([0, 1], [2, 3]).Description());
    }

    [Fact] public void TableOperationDescription()
    {
        Assert.Equal("Change sort", new TableOperation.Sort(null, true, 0, true).Description());
        Assert.Equal("Apply filter", new TableOperation.Filter("", "test").Description());
        Assert.Equal("Select row", new TableOperation.SelectRow(0, 5).Description());
    }

    [Fact] public void SelectionOperationFields()
    {
        var op = new SelectionOperation.Changed(0, 5, null, 10);
        Assert.NotNull(op);
    }

    [Fact] public void CmdDebugFormat()
    {
        var cmd = new WidgetTextEditCmd(UndoWidgetId.FromRaw(5), new TextEditOperation.Insert(0, "abc"));
        var desc = cmd.Description();
        Assert.Equal("Insert text", desc);
    }

    [Fact] public void CmdSizeBytesNonzero()
    {
        var cmd = new WidgetTextEditCmd(UndoWidgetId.FromRaw(1), new TextEditOperation.Insert(0, "hello world"));
        Assert.Equal(11, cmd.SizeBytes());
    }

    // ---- Missing upstream tests ----
    [Fact] public void CmdRedoCallsExecute()
    {
        int count = 0;
        var cmd = new WidgetTextEditCmd(UndoWidgetId.FromRaw(1), new TextEditOperation.Insert(0, "t"));
        cmd = cmd.WithApply((_, _) => { count++; return Result<Unit, string>.Ok(Unit.Default); });
        cmd.Execute();
        Assert.Equal(1, count);
        cmd.Redo();
        Assert.Equal(2, count);
    }

    [Fact] public void WidgetTextEditCmdWithCallbacks()
    {
        bool applied = false, undone = false;
        var cmd = new WidgetTextEditCmd(UndoWidgetId.FromRaw(1), new TextEditOperation.Insert(0, "test"));
        cmd = cmd.WithApply((_, _) => { applied = true; return Result<Unit, string>.Ok(Unit.Default); });
        cmd = cmd.WithUndo((_, _) => { undone = true; return Result<Unit, string>.Ok(Unit.Default); });
        cmd.Execute();
        Assert.True(applied);
        cmd.Undo();
        Assert.True(undone);
    }

    [Fact] public void TableFilterDescription() { Assert.Equal("Apply filter", new TableOperation.Filter("", "test").Description()); }
    [Fact] public void TableSelectRowDescription() { Assert.Equal("Select row", new TableOperation.SelectRow(0, 5).Description()); }
}
