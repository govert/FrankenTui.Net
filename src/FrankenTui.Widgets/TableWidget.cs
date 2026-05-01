using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class TableWidget : IWidget
{
    public IReadOnlyList<string> Headers { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    public int SelectedRow { get; init; } = -1;

    public int FocusedRow { get; init; } = -1;

    public int HoveredRow { get; init; } = -1;

    public void Render(RuntimeRenderContext context)
    {
        WidgetRenderHelpers.ClearTextArea(context, context.Theme.Default);
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            return;
        }

        if (Headers.Count == 0)
        {
            return;
        }

        var headerStyle = WidgetRenderHelpers.ApplyStyling(context)
            ? context.Theme.Table.Header
            : context.Theme.Default;
        var cellStyle = WidgetRenderHelpers.ApplyStyling(context)
            ? context.Theme.Table.Cell
            : context.Theme.Default;
        var constraints = Headers.Select(static _ => LayoutConstraint.Fill()).ToArray();
        var headerRects = LayoutSolver.Split(new FrankenTui.Core.Rect(context.Bounds.X, context.Bounds.Y, context.Bounds.Width, 1), LayoutDirection.Horizontal, constraints);
        for (var index = 0; index < Headers.Count; index++)
        {
            BufferPainter.WriteText(context.Buffer, headerRects[index].X, headerRects[index].Y, Headers[index], headerStyle.ToCell());
        }

        for (var rowIndex = 0; rowIndex < Math.Min(Rows.Count, context.Bounds.Height - 1); rowIndex++)
        {
            var rects = LayoutSolver.Split(
                new FrankenTui.Core.Rect(context.Bounds.X, (ushort)(context.Bounds.Y + 1 + rowIndex), context.Bounds.Width, 1),
                LayoutDirection.Horizontal,
                constraints);
            for (var column = 0; column < Math.Min(Rows[rowIndex].Count, rects.Count); column++)
            {
                var style = WidgetRenderHelpers.ApplyStyling(context)
                    ? rowIndex switch
                    {
                        _ when rowIndex == SelectedRow && rowIndex == FocusedRow => context.Theme.Selection,
                        _ when rowIndex == SelectedRow => context.Theme.Table.Selected,
                        _ when rowIndex == FocusedRow => context.Theme.Interactive.Focused,
                        _ when rowIndex == HoveredRow => context.Theme.Interactive.Hover,
                        _ => context.Theme.Table.Cell
                    }
                    : cellStyle;
                BufferPainter.WriteText(context.Buffer, rects[column].X, rects[column].Y, Rows[rowIndex][column], style.ToCell());
            }
        }
    }
}
