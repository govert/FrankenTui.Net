using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Log entry with metadata. Matches upstream log entry concept.</summary>
public sealed record LogEntry(string Message, DateTimeOffset Timestamp, LogLevel Level = LogLevel.Info);

public enum LogLevel { Debug, Info, Warn, Error }

/// <summary>Scrollable log viewer widget. Matches upstream ftui-widgets log_viewer.</summary>
public sealed class LogViewerWidget : IWidget
{
    private readonly List<LogEntry> _entries = [];
    private int _scrollOffset;

    public IReadOnlyList<LogEntry> Entries => _entries;
    public bool FollowMode { get; set; } = true;
    public string? Filter { get; set; }
    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    public void Append(LogEntry entry) { _entries.Add(entry); }
    public void Append(string message, LogLevel level = LogLevel.Info) => Append(new LogEntry(message, DateTimeOffset.UtcNow, level));

    public void ScrollUp(int lines = 1) { _scrollOffset = Math.Min(_scrollOffset + lines, Math.Max(0, _entries.Count - 1)); FollowMode = false; }
    public void ScrollDown(int lines = 1) { _scrollOffset = Math.Max(0, _scrollOffset - lines); if (_scrollOffset == 0) FollowMode = true; }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || area.Height == 0) return;

        var filtered = _entries
            .Where(e => e.Level >= MinLevel)
            .Where(e => Filter == null || e.Message.Contains(Filter, StringComparison.OrdinalIgnoreCase))
            .Reverse().Skip(_scrollOffset).Take(area.Height).Reverse().ToList();

        for (var i = 0; i < filtered.Count; i++)
        {
            var entry = filtered[i];
            var y = (ushort)(area.Y + i);
            var color = entry.Level switch
            {
                LogLevel.Error => PackedRgba.Rgb(255, 80, 80),
                LogLevel.Warn => PackedRgba.Rgb(255, 200, 50),
                LogLevel.Debug => PackedRgba.Rgb(100, 120, 140),
                _ => PackedRgba.Rgb(200, 210, 220)
            };
            var ts = entry.Timestamp.ToString("HH:mm:ss");
            var text = $"{ts} {entry.Message}";
            for (var x = 0; x < Math.Min(text.Length, area.Width); x++)
                context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(text[x]).WithForeground(color));
        }
    }

    public Size Measure(Size available) => available;
}
