using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Widgets;

public sealed class PanelWidget : IWidget, IMeasurableWidget
{
    public string? Title { get; init; }

    public IWidget? Child { get; init; }

    public void Render(FrankenTui.Runtime.RuntimeRenderContext context) =>
        new BlockWidget
        {
            Title = Title,
            Child = Child
        }.Render(context);

    public SizeConstraints MeasureConstraints(Size available) =>
        SizeConstraints.AtLeast(Size.Zero, Measure(available));

    public Size Measure(Size available)
    {
        var childSize = (Child as IMeasurableWidget)?.MeasureConstraints(available).Preferred ?? available;
        var border = (ushort)2;
        return new Size(
            (ushort)Math.Min(childSize.Width + border, available.Width),
            (ushort)Math.Min(childSize.Height + border, available.Height));
    }

    /// <summary>Intrinsic size = child size + 2 borders.</summary>
    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        var hint = (Child as IMeasurableWidget)?.MeasureAxis(available, direction) ?? SizeHint.Zero;
        var border = (ushort)2;
        return new SizeHint(
            (ushort)(hint.Min + border),
            (ushort)(hint.Preferred + border),
            hint.Max.HasValue ? (ushort)(hint.Max.Value + border) : null);
    }
}
