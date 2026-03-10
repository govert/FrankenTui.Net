using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public sealed record CountdownTimerSnapshot(string Label, TimeSpan Remaining)
{
    public bool IsExpired => Remaining <= TimeSpan.Zero;

    public string Display =>
        IsExpired
            ? "expired"
            : $"{Math.Max(Remaining.Minutes, 0):00}:{Math.Max(Remaining.Seconds, 0):00}";
}

public sealed record StopwatchSnapshot(string Label, TimeSpan Elapsed)
{
    public string Display => $"{Elapsed.Minutes:00}:{Elapsed.Seconds:00}.{Elapsed.Milliseconds / 100:0}";
}

public sealed class CountdownWidget : IWidget
{
    public CountdownTimerSnapshot Snapshot { get; init; } = new("Countdown", TimeSpan.Zero);

    public void Render(RuntimeRenderContext context)
    {
        var style = Snapshot.IsExpired ? context.Theme.Danger : context.Theme.Accent;
        BufferPainter.WriteText(
            context.Buffer,
            context.Bounds.X,
            context.Bounds.Y,
            $"{Snapshot.Label}: {Snapshot.Display}",
            style.ToCell());
    }
}

public sealed class StopwatchWidget : IWidget
{
    public StopwatchSnapshot Snapshot { get; init; } = new("Stopwatch", TimeSpan.Zero);

    public void Render(RuntimeRenderContext context) =>
        BufferPainter.WriteText(
            context.Buffer,
            context.Bounds.X,
            context.Bounds.Y,
            $"{Snapshot.Label}: {Snapshot.Display}",
            context.Theme.Success.ToCell());
}
