using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Notification queue widget. Manages a stack of toast notifications with max visible/queued limits.
/// Matches upstream ftui-widgets notification_queue.
/// </summary>
public sealed class NotificationQueueWidget : IWidget
{
    private readonly List<ToastEntry> _toasts = [];
    public int MaxVisible { get; init; } = 5;
    public int MaxQueued { get; init; } = 20;

    public int Count => _toasts.Count;
    public IReadOnlyList<ToastEntry> Toasts => _toasts;

    public void Push(string message, ToastPriority priority = ToastPriority.Info, string? title = null)
    {
        _toasts.Insert(0, new ToastEntry(message, priority, title, DateTimeOffset.UtcNow));
        while (_toasts.Count > MaxQueued)
            _toasts.RemoveAt(_toasts.Count - 1);
    }

    public void Clear() => _toasts.Clear();

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _toasts.Count == 0) return;

        var visible = Math.Min(_toasts.Count, MaxVisible);
        var toastHeight = Math.Min((ushort)3, (ushort)(area.Height / (ushort)visible));
        if (toastHeight == 0) return;

        for (var i = 0; i < visible; i++)
        {
            var toast = _toasts[i];
            var y = (ushort)(area.Bottom - (visible - i) * toastHeight);
            var toastArea = new Rect(area.X, y, area.Width, toastHeight);
            var toastWidget = new ToastWidget
            {
                Title = toast.Title,
                Message = toast.Message,
                Priority = toast.Priority,
                Dismissible = false
            };
            ((IRuntimeView)toastWidget).Render(new RuntimeRenderContext(context.Buffer, toastArea, context.Theme));
        }
    }

    public Size Measure(Size available) => available;
}

public sealed record ToastEntry(string Message, ToastPriority Priority, string? Title, DateTimeOffset CreatedAt);
