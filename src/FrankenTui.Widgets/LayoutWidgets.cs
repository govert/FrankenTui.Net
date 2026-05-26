using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

// === Item 3: File Picker ===
public sealed class FilePickerWidget : IWidget
{
    private string _currentPath = "/";
    private readonly List<string> _entries = [];
    private int _selectedIndex;

    public string CurrentPath => _currentPath;
    public string? SelectedEntry => _selectedIndex >= 0 && _selectedIndex < _entries.Count ? _entries[_selectedIndex] : null;

    public void Navigate(string path) { _currentPath = path; Refresh(); }
    public void MoveUp() { if (_selectedIndex > 0) _selectedIndex--; }
    public void MoveDown() { if (_selectedIndex < _entries.Count - 1) _selectedIndex++; }

    private void Refresh()
    {
        _entries.Clear();
        try
        {
            if (Directory.Exists(_currentPath))
            {
                _entries.Add("..");
                _entries.AddRange(Directory.GetDirectories(_currentPath).Select(Path.GetFileName)!);
                _entries.AddRange(Directory.GetFiles(_currentPath).Select(Path.GetFileName)!);
            }
        }
        catch { _entries.Add("<error>"); }
    }

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var fg = PackedRgba.Rgb(200, 210, 220);
        var selFg = PackedRgba.Rgb(255, 255, 255);
        var selBg = PackedRgba.Rgb(60, 80, 120);

        // Path header
        var pathText = _currentPath;
        for (var i = 0; i < Math.Min(pathText.Length, area.Width); i++)
            context.Buffer.Set((ushort)(area.X + i), (ushort)area.Y, Cell.FromChar(pathText[i]).WithForeground(PackedRgba.Rgb(100, 180, 255)));

        // Entries
        for (var i = 0; i < Math.Min(_entries.Count, area.Height - 1); i++)
        {
            var y = (ushort)(area.Y + 1 + i);
            var isSelected = i == _selectedIndex;
            var entry = _entries[i];
            for (var x = 0; x < Math.Min(entry.Length, area.Width); x++)
            {
                var cell = isSelected
                    ? new Cell(CellContent.FromChar(entry[x]), selFg, selBg, CellAttributes.None)
                    : Cell.FromChar(entry[x]).WithForeground(fg);
                context.Buffer.Set((ushort)(area.X + x), y, cell);
            }
        }
    }
    public Size Measure(Size available) => available;
}

// === Item 23: Align ===
public sealed class AlignWidget : IWidget
{
    public required IWidget Child { get; init; }
    public HorizontalAlign Horizontal { get; init; } = HorizontalAlign.Left;
    public VerticalAlign Vertical { get; init; } = VerticalAlign.Top;
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        Child.Render(context);
    }
    public Size Measure(Size available) => available;
}
public enum HorizontalAlign { Left, Center, Right }
public enum VerticalAlign { Top, Center, Bottom }

// === Item 24: Cached ===
public sealed class CachedWidget : IWidget
{
    private readonly IWidget _child;
    private string? _lastRendered;
    private Rect _lastArea;
    public CachedWidget(IWidget child) => _child = child;
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        _child.Render(context);
        _lastArea = context.Bounds;
    }
    public Size Measure(Size available) => available;
}

// === Item 25: Choreography (animation step) ===
public sealed class ChoreographyWidget : IWidget
{
    public required IWidget Child { get; init; }
    public int Step { get; set; }
    public int TotalSteps { get; init; } = 10;
    public float Progress => TotalSteps > 0 ? Math.Clamp((float)Step / TotalSteps, 0, 1) : 1;
    void IRuntimeView.Render(RuntimeRenderContext context) => Child.Render(context);
    public Size Measure(Size available) => available;
}

// === Item 22: Height Predictor ===
public sealed class HeightPredictorWidget : IWidget
{
    public required IWidget Child { get; init; }
    public ushort PredictedHeight { get; private set; } = 1;
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        Child.Render(context);
        PredictedHeight = context.Bounds.Height;
    }
    public Size Measure(Size available) => new(available.Width, PredictedHeight);
}
