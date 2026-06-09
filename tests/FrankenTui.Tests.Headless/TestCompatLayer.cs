// Test compatibility shims for the WidgetClearContractTests and similar tests
// that were written against the old Widget.Render(RuntimeRenderContext) API.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Text;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;
using HelpEntry = FrankenTui.Widgets.HelpEntry;
using ResizeRegime = FrankenTui.Runtime.Regime;

namespace FrankenTui.Tests.Headless;

/// <summary>Test-only UI namespace helper.</summary>
internal static class Ui
{
    public static Theme Theme => Theme.DefaultTheme;

    public static AppSimulator<TModel, TMessage> CreateSimulator<TModel, TMessage>(
        int width,
        int height,
        Theme? theme = null,
        RuntimeExecutionPolicy? policy = null)
        where TModel : notnull
        where TMessage : notnull
        => new(new Size((ushort)width, (ushort)height), theme, policy);

    public static WidgetInputState CreateInputState(IEnumerable<string> focusOrder)
        => WidgetInputState.Default.WithFocusOrder(focusOrder);

    public static IRuntimeView Panel(string title, IWidget child)
        => new PanelRuntimeViewAdapter(title, child);

    private sealed class PanelRuntimeViewAdapter(string title, IWidget child) : IRuntimeView
    {
        public void Render(RuntimeRenderContext context)
        {
            var pool = new GraphemePool();
            var frame = new Frame(context.Buffer.Width, context.Buffer.Height, pool);
            frame.BufferOverride = context.Buffer;
            frame.SetDegradation(context.DegradationLevel);
            new PanelWidget { Title = title, Child = child }.Render(context.Bounds, frame);
        }
    }
}

/// <summary>Extension that lets old-style test code call widget.Render(RuntimeRenderContext).</summary>
internal static class WidgetRenderContextExtensions
{
    public static void Render(this IWidget widget, RuntimeRenderContext context)
    {
        var pool = new GraphemePool();
        var frame = new Frame(context.Buffer.Width, context.Buffer.Height, pool);
        frame.BufferOverride = context.Buffer;
        frame.SetDegradation(context.DegradationLevel);
        widget.Render(context.Bounds, frame);
    }
}

/// <summary>StatusWidget compat for tests: label + value display.</summary>
internal sealed class StatusWidget : IWidget
{
    public string? Label { get; init; }
    public string? Value { get; init; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var text = string.IsNullOrEmpty(Label) ? Value ?? "" : $"{Label}: {Value ?? ""}";
        ushort x = area.X;
        foreach (var c in text)
        {
            if (x >= area.Right) break;
            frame.Buffer.SetFast(x++, area.Y, Cell.FromChar(c));
        }
        // Clear remaining cells on the row
        while (x < area.Right)
            frame.Buffer.SetFast(x++, area.Y, Cell.Empty);
    }
}

/// <summary>CountdownWidget compat for tests.</summary>
internal sealed class CountdownWidget : IWidget
{
    public CountdownTimerSnapshot? Snapshot { get; init; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var text = Snapshot?.RenderString() ?? "";
        ushort x = area.X;
        foreach (var c in text)
        {
            if (x >= area.Right) break;
            frame.Buffer.SetFast(x++, area.Y, Cell.FromChar(c));
        }
        while (x < area.Right)
            frame.Buffer.SetFast(x++, area.Y, Cell.Empty);
    }
}

/// <summary>CountdownTimerSnapshot compat for tests.</summary>
internal sealed class CountdownTimerSnapshot(string label, TimeSpan remaining)
{
    public string Label => label;
    public TimeSpan Remaining => remaining;

    public string RenderString()
    {
        if (remaining <= TimeSpan.Zero) return $"{label}: expired";
        return $"{label}: {(int)remaining.TotalMinutes}m{remaining.Seconds:D2}s";
    }
}
