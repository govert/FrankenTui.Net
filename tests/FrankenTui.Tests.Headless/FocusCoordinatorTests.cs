using FrankenTui.Core;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public sealed class FocusCoordinatorTests
{
    [Fact]
    public void WidgetFocusGraphBuildsTabChainAndRemovesIncidentEdges()
    {
        var graph = new WidgetFocusGraph();
        graph.Insert(new WidgetFocusNode("one", new Rect(0, 0, 1, 1)).WithTabIndex(0));
        graph.Insert(new WidgetFocusNode("two", new Rect(1, 0, 1, 1)).WithTabIndex(1));
        graph.Insert(new WidgetFocusNode("three", new Rect(2, 0, 1, 1)).WithTabIndex(2));

        graph.BuildTabChain(wrap: true);

        Assert.Equal(["one", "two", "three"], graph.TabOrder());
        Assert.Equal("two", graph.Navigate("one", WidgetNavigationDirection.Next));
        Assert.Equal("one", graph.Navigate("three", WidgetNavigationDirection.Next));

        graph.Remove("two");

        Assert.Equal(["one", "three"], graph.TabOrder());
        Assert.Null(graph.Navigate("one", WidgetNavigationDirection.Next));
        Assert.Null(graph.Navigate("three", WidgetNavigationDirection.Prev));
    }

    [Fact]
    public void WidgetFocusManagerTracksHostFocusAndRepairsTrapSelectionAfterNodeRemoval()
    {
        var focus = new WidgetFocusManager();
        focus.UpsertNode(new WidgetFocusNode("base.one", new Rect(0, 0, 1, 1)).WithTabIndex(0));
        focus.UpsertNode(new WidgetFocusNode("base.two", new Rect(1, 0, 1, 1)).WithTabIndex(1));
        focus.UpsertNode(new WidgetFocusNode("modal.one", new Rect(0, 1, 1, 1)).WithTabIndex(2));
        focus.UpsertNode(new WidgetFocusNode("modal.two", new Rect(1, 1, 1, 1)).WithTabIndex(3));

        Assert.True(focus.Focus("base.two"));
        focus.SetGroup("modal", ["modal.one", "modal.two"]);
        Assert.True(focus.PushTrap("modal", trapId: "modal", preferredFocusId: "modal.two"));
        Assert.Equal("modal.two", focus.CurrentFocusId);

        Assert.True(focus.ApplyHostFocus(false));
        Assert.Null(focus.CurrentFocusId);
        Assert.Equal("modal.two", focus.LogicalFocusId);

        focus.RemoveNode("modal.two");
        Assert.Equal("modal.one", focus.LogicalFocusId);

        focus.ApplyHostFocus(true);
        Assert.Equal("modal.one", focus.CurrentFocusId);

        Assert.True(focus.PopTrap());
        Assert.Equal("base.two", focus.CurrentFocusId);
    }

    [Fact]
    public void FocusAwareModalStackPopByIdRepairsNonTopModalReturnTargets()
    {
        var focus = new WidgetFocusManager();
        focus.UpsertNode(new WidgetFocusNode("base", new Rect(0, 0, 1, 1)).WithTabIndex(0));
        focus.UpsertNode(new WidgetFocusNode("modal1.primary", new Rect(0, 1, 1, 1)).WithTabIndex(1));
        focus.UpsertNode(new WidgetFocusNode("modal1.dismiss", new Rect(1, 1, 1, 1)).WithTabIndex(2));
        focus.UpsertNode(new WidgetFocusNode("modal2.primary", new Rect(0, 2, 1, 1)).WithTabIndex(3));
        focus.UpsertNode(new WidgetFocusNode("modal3.primary", new Rect(0, 3, 1, 1)).WithTabIndex(4));

        var modals = new WidgetFocusAwareModalStack(focus);
        focus.Focus("base");
        Assert.Equal("base", focus.CurrentFocusId);

        var modal1 = modals.Push(new WidgetModalEntry("modal1", ["modal1.primary", "modal1.dismiss"], PreferredFocusId: "modal1.primary"));
        Assert.Equal("modal1.primary", focus.CurrentFocusId);
        Assert.True(focus.Focus("modal1.dismiss"));

        var modal2 = modals.Push(new WidgetModalEntry("modal2", ["modal2.primary"], PreferredFocusId: "modal2.primary"));
        Assert.Equal("modal2.primary", focus.CurrentFocusId);

        modals.Push(new WidgetModalEntry("modal3", ["modal3.primary"], PreferredFocusId: "modal3.primary"));
        Assert.Equal("modal3.primary", focus.CurrentFocusId);

        var removed = modals.PopById(modal2);
        Assert.NotNull(removed);
        Assert.Equal("modal3.primary", focus.CurrentFocusId);

        modals.Pop();
        Assert.Equal("modal1.dismiss", focus.CurrentFocusId);

        modals.PopById(modal1);
        Assert.Equal("base", focus.CurrentFocusId);
        Assert.True(modals.IsEmpty);
        Assert.False(modals.IsFocusTrapped);
    }

    [Fact]
    public void FocusAwareModalStackRefreshesReturnFocusWhenLowerModalFocusablesChange()
    {
        var focus = new WidgetFocusManager();
        focus.UpsertNode(new WidgetFocusNode("base", new Rect(0, 0, 1, 1)).WithTabIndex(0));
        focus.UpsertNode(new WidgetFocusNode("modal1.primary", new Rect(0, 1, 1, 1)).WithTabIndex(1));
        focus.UpsertNode(new WidgetFocusNode("modal1.dismiss", new Rect(1, 1, 1, 1)).WithTabIndex(2));
        focus.UpsertNode(new WidgetFocusNode("modal2.primary", new Rect(0, 2, 1, 1)).WithTabIndex(3));

        var modals = new WidgetFocusAwareModalStack(focus);
        focus.Focus("base");
        Assert.Equal("base", focus.CurrentFocusId);

        var modal1 = modals.Push(new WidgetModalEntry("modal1", ["modal1.primary", "modal1.dismiss"], PreferredFocusId: "modal1.dismiss"));
        Assert.Equal("modal1.dismiss", focus.CurrentFocusId);

        modals.Push(new WidgetModalEntry("modal2", ["modal2.primary"], PreferredFocusId: "modal2.primary"));
        Assert.Equal("modal2.primary", focus.CurrentFocusId);

        Assert.True(modals.Update(modal1, new WidgetModalEntry("modal1", ["modal1.primary"], PreferredFocusId: "modal1.primary")));
        modals.Pop();

        Assert.Equal("modal1.primary", focus.CurrentFocusId);
    }

    [Fact]
    public void FocusAwareModalStackHostBlurRestoreKeepsTopModalTrap()
    {
        var focus = new WidgetFocusManager();
        focus.UpsertNode(new WidgetFocusNode("base", new Rect(0, 0, 1, 1)).WithTabIndex(0));
        focus.UpsertNode(new WidgetFocusNode("modal.primary", new Rect(0, 1, 1, 1)).WithTabIndex(1));
        focus.UpsertNode(new WidgetFocusNode("modal.dismiss", new Rect(1, 1, 1, 1)).WithTabIndex(2));

        var modals = new WidgetFocusAwareModalStack(focus);
        focus.Focus("base");
        Assert.Equal("base", focus.CurrentFocusId);

        modals.Push(new WidgetModalEntry("modal", ["modal.primary", "modal.dismiss"], PreferredFocusId: "modal.primary"));
        Assert.Equal("modal.primary", focus.CurrentFocusId);

        Assert.True(modals.ApplyHostFocus(false));
        Assert.Null(focus.CurrentFocusId);
        Assert.Equal("modal.primary", focus.LogicalFocusId);

        Assert.True(focus.FocusNext());
        Assert.Equal("modal.dismiss", focus.LogicalFocusId);

        modals.ApplyHostFocus(true);
        Assert.Equal("modal.dismiss", focus.CurrentFocusId);
    }
}
