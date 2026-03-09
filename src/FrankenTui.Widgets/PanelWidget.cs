namespace FrankenTui.Widgets;

public sealed class PanelWidget : IWidget
{
    public string? Title { get; init; }

    public IWidget? Child { get; init; }

    public void Render(FrankenTui.Runtime.RuntimeRenderContext context) =>
        new BlockWidget
        {
            Title = Title,
            Child = Child
        }.Render(context);
}
