using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class TreeWidget : IWidget
{
    public IReadOnlyList<TreeNode> Nodes { get; init; } = [];

    public void Render(RuntimeRenderContext context)
    {
        var row = 0;
        foreach (var node in Flatten(Nodes, 0))
        {
            if (row >= context.Bounds.Height)
            {
                break;
            }

            var prefix = new string(' ', node.Depth * 2) + "└ ";
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + row),
                prefix + node.Node.Label,
                context.Theme.Default.ToCell());
            row++;
        }
    }

    private static IEnumerable<(TreeNode Node, int Depth)> Flatten(IEnumerable<TreeNode> nodes, int depth)
    {
        foreach (var node in nodes)
        {
            yield return (node, depth);
            foreach (var child in Flatten(node.Children, depth + 1))
            {
                yield return child;
            }
        }
    }
}

public sealed record TreeNode(string Label, IReadOnlyList<TreeNode> Children);
