using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Log ring buffer widget. Stores and renders a fixed-size ring of log entries.
/// Matches upstream ftui-widgets log_ring.
/// </summary>
public sealed class LogRingBufferWidget : IWidget
{
    private readonly string?[] _buffer;
    private int _head;
    private int _count;

    public LogRingBufferWidget(int capacity = 10000)
    {
        _buffer = new string[Math.Max(capacity, 1)];
    }

    public void Append(string entry)
    {
        _buffer[_head] = entry;
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length) _count++;
    }

    public int Count => _count;

    public IEnumerable<string> Enumerate()
    {
        for (var i = 0; i < _count; i++)
        {
            var idx = (_head - _count + i + _buffer.Length) % _buffer.Length;
            var entry = _buffer[idx];
            if (entry is not null) yield return entry;
        }
    }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _count == 0) return;

        var fg = PackedRgba.Rgb(180, 190, 200);
        var entries = Enumerate().Reverse().Take(area.Height).Reverse().ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            var y = (ushort)(area.Y + i);
            var text = entries[i];
            var len = Math.Min(text.Length, area.Width);
            for (var x = 0; x < len; x++)
                context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(text[x]).WithForeground(fg));
        }
    }

    public Size Measure(Size available) => available;
}
