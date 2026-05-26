using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

// === Item 6: JSON View ===
public sealed class JsonViewWidget : IWidget
{
    public string? Json { get; init; }
    public bool Collapsed { get; init; }
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || string.IsNullOrEmpty(Json)) return;
        var fg = PackedRgba.Rgb(200, 210, 220);
        var text = Collapsed ? TruncateJson(Json) : Json;
        for (var i = 0; i < Math.Min(text.Length, area.Width); i++)
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(text[i]).WithForeground(fg));
    }
    private static string TruncateJson(string json)
    {
        var firstBrace = Math.Min(json.IndexOf('{'), json.IndexOf('['));
        if (firstBrace < 0) return json[..Math.Min(40, json.Length)];
        return json[..Math.Min(firstBrace + 30, json.Length)] + "...";
    }
    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)80), 1);
}

// === Item 4: History Panel ===
public sealed class HistoryPanelWidget : IWidget
{
    private readonly List<string> _entries = [];
    public int MaxEntries { get; init; } = 100;
    public IReadOnlyList<string> Entries => _entries;

    public void Push(string entry) { _entries.Add(entry); while (_entries.Count > MaxEntries) _entries.RemoveAt(0); }
    public void Clear() => _entries.Clear();

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        var fg = PackedRgba.Rgb(180, 190, 200);
        var muted = PackedRgba.Rgb(100, 110, 120);
        var visible = _entries.TakeLast(area.Height).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            var y = (ushort)(area.Y + i);
            var text = visible[i];
            var len = Math.Min(text.Length, area.Width);
            var color = i == visible.Count - 1 ? fg : muted;
            for (var x = 0; x < len; x++)
                context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(text[x]).WithForeground(color));
        }
    }
    public Size Measure(Size available) => available;
}

// === Item 11: Validation Error ===
public sealed class ValidationErrorWidget : IWidget
{
    public string? Message { get; init; }
    public string? Field { get; init; }
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || string.IsNullOrEmpty(Message)) return;
        var fg = PackedRgba.Rgb(255, 80, 80);
        var prefix = string.IsNullOrEmpty(Field) ? "✗ " : $"✗ {Field}: ";
        var text = (prefix + Message)[..Math.Min(prefix.Length + Message!.Length, area.Width)];
        for (var i = 0; i < text.Length; i++)
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(text[i]).WithForeground(fg));
    }
    public Size Measure(Size available) => new(available.Width, 1);
}

// === Item 26: Pretty Print ===
public sealed class PrettyWidget : IWidget
{
    public string? Text { get; init; }
    public bool Indented { get; init; } = true;
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || string.IsNullOrEmpty(Text)) return;
        var fg = PackedRgba.Rgb(200, 210, 220);
        for (var i = 0; i < Math.Min(Text.Length, area.Width); i++)
        {
            var ch = Text[i];
            if (Indented && ch == '{') ch = ' ';
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(ch).WithForeground(fg));
        }
    }
    public Size Measure(Size available) => new(Math.Min(available.Width, (ushort)80), 1);
}

// === Item 27: Timer (completing TimingWidgets parity) ===
public sealed class TimerWidget : IWidget
{
    private TimeSpan _remaining;
    private readonly TimeSpan _total;
    public TimerWidget(TimeSpan duration) { _remaining = _total = duration; }
    public double Progress => _total.Ticks > 0 ? _remaining / _total : 0;
    public void Tick(TimeSpan delta) => _remaining = _remaining > delta ? _remaining - delta : TimeSpan.Zero;
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var text = _remaining.TotalHours >= 1
            ? $"{_remaining.Hours:D2}:{_remaining.Minutes:D2}:{_remaining.Seconds:D2}"
            : $"{_remaining.Minutes:D2}:{_remaining.Seconds:D2}";
        var fg = _remaining.TotalSeconds < 10 ? PackedRgba.Rgb(255, 80, 80) : PackedRgba.White;
        for (var i = 0; i < Math.Min(text.Length, area.Width); i++)
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(text[i]).WithForeground(fg));
    }
    public Size Measure(Size available) => new((ushort)8, 1);
}
