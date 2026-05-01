using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Text;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class WidgetClearContractTests
{
    [Fact]
    public void ParagraphWidgetShorterSecondRenderClearsStaleSuffix()
    {
        var buffer = new RenderBuffer(20, 2);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(20, 2), Ui.Theme);

        new ParagraphWidget("LONG VALUE").Render(context);
        new ParagraphWidget("OK").Render(context);

        Assert.Equal("OK", HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void ListWidgetShorterSecondRenderClearsRowsAndSuffixes()
    {
        var buffer = new RenderBuffer(20, 3);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(20, 3), Ui.Theme);

        new ListWidget { Items = ["LONG ITEM", "SECOND"], SelectedIndex = 0 }.Render(context);
        new ListWidget { Items = ["OK"], SelectedIndex = 0 }.Render(context);

        Assert.Equal("› OK", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void StatusWidgetShorterValueClearsStaleSuffix()
    {
        var buffer = new RenderBuffer(24, 1);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(24, 1), Ui.Theme);

        new StatusWidget { Label = "Health", Value = "Nominal-Long" }.Render(context);
        new StatusWidget { Label = "Health", Value = "OK" }.Render(context);

        Assert.Equal("Health: OK", HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void TabsWidgetShorterSecondRenderClearsTrailingLabels()
    {
        var buffer = new RenderBuffer(24, 1);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(24, 1), Ui.Theme);

        new TabsWidget { Tabs = ["Overview", "Interaction"], SelectedIndex = 0 }.Render(context);
        new TabsWidget { Tabs = ["One"], SelectedIndex = 0 }.Render(context);

        Assert.Equal("[One]", HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void TextAreaWidgetShorterSecondRenderClearsContentAndStatus()
    {
        var buffer = new RenderBuffer(24, 3);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(24, 3), Ui.Theme);

        new TextAreaWidget
        {
            Document = TextDocument.FromString("LONG VALUE"),
            StatusText = "status-long"
        }.Render(context);

        new TextAreaWidget
        {
            Document = TextDocument.FromString("OK"),
            StatusText = "ok"
        }.Render(context);

        Assert.Equal("OK", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("ok", HeadlessBufferView.RowText(buffer, 2));
    }

    [Fact]
    public void HelpWidgetShorterSecondRenderClearsUnusedRows()
    {
        var buffer = new RenderBuffer(24, 3);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(24, 3), Ui.Theme);

        new HelpWidget
        {
            Entries =
            [
                new HelpEntry("Enter", "Open long action"),
                new HelpEntry("Esc", "Close")
            ]
        }.Render(context);

        new HelpWidget
        {
            Entries =
            [
                new HelpEntry("E", "Go")
            ]
        }.Render(context);

        Assert.Equal("E        Go", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void CountdownWidgetShorterSecondRenderClearsStaleSuffix()
    {
        var buffer = new RenderBuffer(24, 1);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(24, 1), Ui.Theme);

        new CountdownWidget { Snapshot = new CountdownTimerSnapshot("CountdownLong", TimeSpan.FromSeconds(125)) }.Render(context);
        new CountdownWidget { Snapshot = new CountdownTimerSnapshot("Go", TimeSpan.Zero) }.Render(context);

        Assert.Equal("Go: expired", HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void BlockWidgetUsesAsciiBordersAtSimpleBorders()
    {
        var buffer = new RenderBuffer(12, 4);
        var context = new RuntimeRenderContext(buffer, Rect.FromSize(12, 4), Ui.Theme)
            .WithDegradation(RuntimeDegradationLevel.SimpleBorders);

        new BlockWidget { Title = "Box", Child = new ParagraphWidget("Body") }.Render(context);

        Assert.Equal("+- Box ----+", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("|Body      |", HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void BlockWidgetClearsDecorativeChromeAtEssentialOnly()
    {
        var buffer = new RenderBuffer(12, 4);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(12, 4), Ui.Theme);
        var essential = full.WithDegradation(RuntimeDegradationLevel.EssentialOnly);

        new BlockWidget { Title = "Box", Child = new ParagraphWidget("Body") }.Render(full);
        new BlockWidget { Title = "Box", Child = new ParagraphWidget("Body") }.Render(essential);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void ParagraphWidgetClearsAtSkeleton()
    {
        var buffer = new RenderBuffer(16, 2);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(16, 2), Ui.Theme);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new ParagraphWidget("Stale text").Render(full);
        new ParagraphWidget("New text").Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void ProgressWidgetUsesTieredFallbacks()
    {
        var buffer = new RenderBuffer(12, 3);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(12, 3), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var essential = full.WithDegradation(RuntimeDegradationLevel.EssentialOnly);

        new ProgressWidget { Value = 0.5, Label = "Half" }.Render(noStyling);
        Assert.Equal("[#####     ]", HeadlessBufferView.RowText(buffer, 0));

        new ProgressWidget { Value = 0.5, Label = "Half" }.Render(essential);
        Assert.Equal("50%", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void ListWidgetStripsStylingAndClearsAtSkeleton()
    {
        var buffer = new RenderBuffer(20, 3);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(20, 3), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new ListWidget { Items = ["Alpha", "Beta"], SelectedIndex = 0 }.Render(noStyling);

        Assert.Equal("› Alpha", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(0, 0)!.Value.Foreground);

        new ListWidget { Items = ["Alpha", "Beta"], SelectedIndex = 0 }.Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void StatusWidgetKeepsEssentialTextAtSkeleton()
    {
        var buffer = new RenderBuffer(24, 1);
        var skeleton = new RuntimeRenderContext(buffer, Rect.FromSize(24, 1), Ui.Theme)
            .WithDegradation(RuntimeDegradationLevel.Skeleton);

        new StatusWidget { Label = "Health", Value = "OK" }.Render(skeleton);

        Assert.Equal("Health: OK", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(8, 0)!.Value.Foreground);
    }

    [Fact]
    public void TabsWidgetStripsStylingAndClearsAtSkeleton()
    {
        var buffer = new RenderBuffer(24, 1);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(24, 1), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new TabsWidget { Tabs = ["One", "Two"], SelectedIndex = 0 }.Render(noStyling);

        Assert.Equal("[One]  Two", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(0, 0)!.Value.Foreground);

        new TabsWidget { Tabs = ["One", "Two"], SelectedIndex = 0 }.Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void TextAreaWidgetStripsStylingAndClearsAtSkeleton()
    {
        var buffer = new RenderBuffer(24, 3);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(24, 3), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new TextAreaWidget
        {
            Document = TextDocument.FromString("OK"),
            HasFocus = true,
            Cursor = new TextCursor(0, 2),
            StatusText = "status"
        }.Render(noStyling);

        Assert.Equal("OK|", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("status", HeadlessBufferView.RowText(buffer, 2));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(2, 0)!.Value.Foreground);

        new TextAreaWidget
        {
            Document = TextDocument.FromString("Later"),
            StatusText = "next"
        }.Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 2));
    }

    [Fact]
    public void TableWidgetStripsStylingAndClearsAtSkeleton()
    {
        var buffer = new RenderBuffer(24, 3);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(24, 3), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new TableWidget
        {
            Headers = ["Name", "State"],
            Rows = [["A", "Ready"]],
            SelectedRow = 0,
            FocusedRow = 0
        }.Render(noStyling);

        Assert.Equal("Name        State", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("A           Ready", HeadlessBufferView.RowText(buffer, 1));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(0, 1)!.Value.Foreground);

        new TableWidget
        {
            Headers = ["Name", "State"],
            Rows = [["B", "Later"]]
        }.Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void TreeWidgetUsesAsciiBranchesAndClearsAtSkeleton()
    {
        var buffer = new RenderBuffer(24, 3);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(24, 3), Ui.Theme);
        var simpleBorders = full.WithDegradation(RuntimeDegradationLevel.SimpleBorders);
        var essential = full.WithDegradation(RuntimeDegradationLevel.EssentialOnly);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);
        TreeNode[] nodes = [new("Root", [new TreeNode("Leaf", [])])];

        new TreeWidget { Nodes = nodes }.Render(simpleBorders);

        Assert.Equal("+ Root", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("  + Leaf", HeadlessBufferView.RowText(buffer, 1));

        new TreeWidget { Nodes = nodes }.Render(essential);

        Assert.Equal("Root", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal("Leaf", HeadlessBufferView.RowText(buffer, 1));

        new TreeWidget { Nodes = nodes }.Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void ScrollbarWidgetUsesAsciiAndClearsAtEssentialOnly()
    {
        var buffer = new RenderBuffer(4, 4);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(4, 4), Ui.Theme);
        var simpleBorders = full.WithDegradation(RuntimeDegradationLevel.SimpleBorders);
        var essential = full.WithDegradation(RuntimeDegradationLevel.EssentialOnly);

        new ScrollbarWidget { TotalItems = 8, ViewportItems = 2, Offset = 2 }.Render(simpleBorders);

        Assert.Equal("|", HeadlessBufferView.RowText(buffer, 1));
        Assert.Equal(Ui.Theme.Border.Foreground, buffer.Get(0, 0)!.Value.Foreground);

        new ScrollbarWidget { TotalItems = 8, ViewportItems = 2, Offset = 2 }.Render(essential);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }

    [Fact]
    public void BufferInspectorStripsStylingAndClearsAtSkeleton()
    {
        var source = new RenderBuffer(12, 2);
        var sourceContext = new RuntimeRenderContext(source, Rect.FromSize(12, 2), Ui.Theme);
        new ParagraphWidget("Inspect").Render(sourceContext);

        var buffer = new RenderBuffer(16, 2);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(16, 2), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new BufferInspectorWidget(source).Render(noStyling);

        Assert.Equal("Inspect", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(0, 0)!.Value.Foreground);

        new BufferInspectorWidget(source).Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
    }

    [Fact]
    public void LayoutInspectorStripsStylingAndClearsAtSkeleton()
    {
        var trace = LayoutSolver.SplitWithTrace(
            Rect.FromSize(12, 1),
            LayoutDirection.Horizontal,
            [LayoutConstraint.Fixed(4), LayoutConstraint.Fill()]);
        var buffer = new RenderBuffer(40, 3);
        var full = new RuntimeRenderContext(buffer, Rect.FromSize(40, 3), Ui.Theme);
        var noStyling = full.WithDegradation(RuntimeDegradationLevel.NoStyling);
        var skeleton = full.WithDegradation(RuntimeDegradationLevel.Skeleton);

        new LayoutInspectorWidget(trace).Render(noStyling);

        Assert.StartsWith("Horizontal total=12", HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(Ui.Theme.Default.Foreground, buffer.Get(0, 0)!.Value.Foreground);

        new LayoutInspectorWidget(trace).Render(skeleton);

        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 0));
        Assert.Equal(string.Empty, HeadlessBufferView.RowText(buffer, 1));
    }
}
