using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public sealed class PaneWorkspaceWidget : IWidget
{
    public required PaneWorkspaceState Workspace { get; init; }

    public void Render(RuntimeRenderContext context) =>
        RenderNode(context, Workspace.Root, context.Bounds, isRoot: true);

    private void RenderNode(RuntimeRenderContext context, PaneWorkspaceNode node, Rect bounds, bool isRoot)
    {
        if (bounds.IsEmpty)
        {
            return;
        }

        if (node.IsLeaf || node.Children.Count < 2 || node.SplitDirection is null)
        {
            var selected = string.Equals(node.Id, Workspace.SelectedPaneId, StringComparison.Ordinal);
            new BlockWidget
            {
                Title = isRoot
                    ? $"Pane Workspace [{Workspace.Mode}]"
                    : selected ? $"{node.Title} *" : node.Title,
                BorderStyle = selected ? context.Theme.Selection : context.Theme.Border,
                Child = new PaddingWidget(
                    new ParagraphWidget(selected
                        ? $"selected={node.Id}\nmode={Workspace.Mode}\nhash={Workspace.SnapshotHash()}"
                        : node.Id),
                    Sides.All(1))
            }.Render(context.WithBounds(bounds));
            return;
        }

        var ratio = isRoot ? Workspace.PrimaryRatioPermille / 1000d : 0.5d;
        var constraints = node.SplitDirection == PaneSplitDirection.Horizontal
            ? new[] { LayoutConstraint.Percentage((ushort)Math.Round(ratio * 100)), LayoutConstraint.Fill() }
            : new[] { LayoutConstraint.Percentage((ushort)Math.Round(ratio * 100)), LayoutConstraint.Fill() };
        var direction = node.SplitDirection == PaneSplitDirection.Horizontal ? LayoutDirection.Horizontal : LayoutDirection.Vertical;
        var rects = LayoutSolver.Split(bounds, direction, constraints);
        RenderNode(context, node.Children[0], rects[0], isRoot: false);
        RenderNode(context, node.Children[1], rects[1], isRoot: false);
    }
}
