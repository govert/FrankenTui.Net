// Port of .external/frankentui/crates/ftui-widgets/src/progress.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Progress bar widget (ProgressBar) and compact dashboard indicator (MiniBar).

using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

// ---------------------------------------------------------------------------
// ProgressBar
// ---------------------------------------------------------------------------

/// <summary>A widget to display a progress bar.</summary>
public sealed class ProgressBar : IWidget, IMeasurableWidget, IAccessible, CanonicalA11y.IAccessible
{
    private Block? _block;
    private double _ratio;
    private string? _label;
    private WidgetStyle _style;
    private WidgetStyle _gaugeStyle;

    /// <summary>Create a new progress bar with default settings.</summary>
    public ProgressBar() { }

    /// <summary>Set the surrounding block.</summary>
    public ProgressBar WithBlock(Block block) { _block = block; return this; }

    /// <summary>Set the progress ratio (clamped to 0.0..=1.0).</summary>
    public ProgressBar Ratio(double ratio)
    {
        _ratio = double.IsNaN(ratio) ? 0.0 : Math.Clamp(ratio, 0.0, 1.0);
        return this;
    }

    /// <summary>Set the centered label text.</summary>
    public ProgressBar Label(string? label) { _label = label; return this; }

    /// <summary>Set the base style.</summary>
    public ProgressBar Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set the filled portion style.</summary>
    public ProgressBar GaugeStyle(WidgetStyle style) { _gaugeStyle = style; return this; }

    // ── IWidget.Render ─────────────────────────────────────────────────────

    /// <summary>Render the progress bar into the frame at the given area.</summary>
    public void Render(Rect area, Frame frame)
    {
        var deg = frame.Degradation;

        // Skeleton+: skip entirely
        if (!deg.RenderContent())
            return;

        // EssentialOnly: just show percentage text, no bar
        if (!deg.RenderDecorative())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            var pct = $"{(byte)(_ratio * 100.0)}%";
            WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, pct, WidgetStyle.Default, area.Right);
            return;
        }

        var baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;

        WidgetDrawing.ClearTextArea(frame, area, baseStyle);

        Rect barArea;
        if (_block is { } b)
        {
            b.Render(area, frame);
            barArea = b.Inner(area);
        }
        else
        {
            barArea = area;
        }

        if (barArea.IsEmpty)
            return;

        double maxWidth = barArea.Width;
        ushort filledWidth = _ratio >= 1.0
            ? barArea.Width
            : (ushort)Math.Floor(maxWidth * _ratio);

        // Draw filled part
        var gaugeStyle = deg.ApplyStyling() ? _gaugeStyle : WidgetStyle.Default;
        // At NoStyling, use '#' as fill char instead of background color
        char fillChar = deg.ApplyStyling() ? ' ' : '#';

        for (ushort y = barArea.Top; y < barArea.Bottom; y++)
        {
            for (ushort x = 0; x < filledWidth; x++)
            {
                ushort cellX = (ushort)Math.Min((int)barArea.Left + x, ushort.MaxValue);
                if (cellX < barArea.Right)
                {
                    var cell = Cell.FromChar(fillChar);
                    WidgetDrawing.ApplyStyle(ref cell, gaugeStyle);
                    frame.Buffer.SetFast(cellX, y, cell);
                }
            }
        }

        // Draw label (centered)
        var labelStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        if (_label is { } label)
        {
            int labelWidth = TerminalTextWidth.DisplayWidth(label);
            ushort labelX = (ushort)(barArea.Left +
                ((int)barArea.Width - labelWidth > 0 ? ((int)barArea.Width - labelWidth) / 2 : 0));
            ushort labelY = (ushort)(barArea.Top + barArea.Height / 2);

            WidgetDrawing.DrawTextSpan(frame, labelX, labelY, label, labelStyle, barArea.Right);
        }
    }

    // ── IMeasurableWidget ─────────────────────────────────────────────────

    /// <summary>
    /// ProgressBar fills available width, has fixed height of 1 (or block inner height).
    /// </summary>
    public SizeConstraints Measure(Size available)
    {
        ushort blockWidth, blockHeight;
        if (_block is { } b)
        {
            var inner = b.Inner(new Rect(0, 0, 100, 100));
            blockWidth = (ushort)(100 - inner.Width);
            blockHeight = (ushort)(100 - inner.Height);
        }
        else
        {
            blockWidth = 0;
            blockHeight = 0;
        }

        // Minimum: 1 cell for bar + block overhead
        // Preferred: fills available width, 1 row + block overhead
        ushort minWidth = (ushort)(1 + blockWidth);
        ushort minHeight = (ushort)(1 + blockHeight);

        return new SizeConstraints
        {
            Min = new Size(minWidth, minHeight),
            Preferred = new Size(minWidth, minHeight), // Fills width, so preferred = min
            Max = null,                                // Can grow to fill available space
        };
    }

    /// <summary>
    /// ProgressBar fills width, so it doesn't have true intrinsic width,
    /// but it does have intrinsic height.
    /// </summary>
    public bool HasIntrinsicSize() => true;

    /// <inheritdoc/>
    public SizeConstraints MeasureConstraints(Size available) => Measure(available);

    /// <inheritdoc/>
    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        var c = Measure(available);
        return direction == LayoutDirection.Horizontal
            ? new SizeHint(c.Min.Width, c.Preferred.Width, c.Max?.Width)
            : new SizeHint(c.Min.Height, c.Preferred.Height, c.Max?.Height);
    }

    // ── IAccessible ───────────────────────────────────────────────────────

    /// <summary>Get the legacy compatibility projection of this widget's accessibility node.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        ulong id = WidgetDrawing.A11yNodeId(area);
        uint pct = (uint)Math.Round(_ratio * 100.0, MidpointRounding.AwayFromZero);
        string name = _label is { } l ? l : $"{pct}%";

        var state = new CanonicalA11y.A11yState
        {
            ValueNow = _ratio,
            ValueMin = 0.0,
            ValueMax = 1.0,
            ValueText = $"{pct}%",
        };

        return
        [
            CanonicalA11y.A11yNodeInfo.New(id, CanonicalA11y.A11yRole.ProgressBar, area)
                .WithName(name)
                .WithState(state),
        ];
    }
}

// ---------------------------------------------------------------------------
// MiniBarColors
// ---------------------------------------------------------------------------

/// <summary>Color thresholds for <see cref="MiniBar"/>.</summary>
public sealed class MiniBarColors
{
    public PackedRgba High { get; set; }
    public PackedRgba Mid { get; set; }
    public PackedRgba Low { get; set; }
    public PackedRgba Critical { get; set; }

    public MiniBarColors(PackedRgba high, PackedRgba mid, PackedRgba low, PackedRgba critical)
    {
        High = high;
        Mid = mid;
        Low = low;
        Critical = critical;
    }

    public static MiniBarColors Default() => new(
        PackedRgba.Rgb(64, 200, 120),
        PackedRgba.Rgb(255, 180, 64),
        PackedRgba.Rgb(80, 200, 240),
        PackedRgba.Rgb(160, 160, 160));
}

// ---------------------------------------------------------------------------
// MiniBarThresholds
// ---------------------------------------------------------------------------

/// <summary>Thresholds for mapping values to colors.</summary>
public sealed class MiniBarThresholds
{
    public double High { get; set; }
    public double Mid { get; set; }
    public double Low { get; set; }

    public MiniBarThresholds(double high, double mid, double low)
    {
        High = high;
        Mid = mid;
        Low = low;
    }

    public static MiniBarThresholds Default() => new(0.75, 0.50, 0.25);
}

// ---------------------------------------------------------------------------
// MiniBar
// ---------------------------------------------------------------------------

/// <summary>Compact progress indicator for dashboard-style metrics.</summary>
public sealed class MiniBar : IWidget, IMeasurableWidget
{
    private double _value;
    private ushort _width;
    private bool _showPercent;
    private WidgetStyle _style;
    private char _filledChar;
    private char _emptyChar;
    private MiniBarColors _colors;
    private MiniBarThresholds _thresholds;

    /// <summary>Create a new MiniBar with value in the 0.0..=1.0 range.</summary>
    public MiniBar(double value, ushort width)
    {
        _value = value;
        _width = width;
        _showPercent = false;
        _style = WidgetStyle.Default;
        _filledChar = '█';
        _emptyChar = '░';
        _colors = MiniBarColors.Default();
        _thresholds = MiniBarThresholds.Default();
    }

    /// <summary>Override the value (clamped to 0.0..=1.0).</summary>
    public MiniBar Value(double value) { _value = value; return this; }

    /// <summary>Override the displayed width.</summary>
    public MiniBar Width(ushort width) { _width = width; return this; }

    /// <summary>Enable or disable percentage text.</summary>
    public MiniBar ShowPercent(bool show) { _showPercent = show; return this; }

    /// <summary>Set the base style for the bar.</summary>
    public MiniBar Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Override the filled block character.</summary>
    public MiniBar FilledChar(char ch) { _filledChar = ch; return this; }

    /// <summary>Override the empty block character.</summary>
    public MiniBar EmptyChar(char ch) { _emptyChar = ch; return this; }

    /// <summary>Override the color thresholds.</summary>
    public MiniBar Thresholds(MiniBarThresholds thresholds) { _thresholds = thresholds; return this; }

    /// <summary>Override the color palette.</summary>
    public MiniBar Colors(MiniBarColors colors) { _colors = colors; return this; }

    /// <summary>Map a value to a color using default thresholds.</summary>
    public static PackedRgba ColorForValue(double value)
    {
        double v = double.IsFinite(value) ? value : 0.0;
        v = Math.Clamp(v, 0.0, 1.0);
        var thresholds = MiniBarThresholds.Default();
        var colors = MiniBarColors.Default();
        if (v > thresholds.High)
            return colors.High;
        if (v > thresholds.Mid)
            return colors.Mid;
        if (v > thresholds.Low)
            return colors.Low;
        return colors.Critical;
    }

    /// <summary>Render the bar as a string (for testing/debugging).</summary>
    public string RenderString()
    {
        int width = _width;
        if (width == 0)
            return string.Empty;
        int filled = FilledCells(width);
        int empty = Math.Max(0, width - filled);
        return new string(_filledChar, filled) + new string(_emptyChar, empty);
    }

    private double NormalizedValue()
    {
        return double.IsFinite(_value) ? Math.Clamp(_value, 0.0, 1.0) : 0.0;
    }

    private int FilledCells(int width)
    {
        if (width == 0)
            return 0;
        double v = NormalizedValue();
        int filled = (int)Math.Round(v * width);
        return Math.Min(filled, width);
    }

    /// <summary>Map a value to a color using this bar's palette and thresholds.</summary>
    public PackedRgba ColorForValueWithPalette(double value)
    {
        double v = double.IsFinite(value) ? value : 0.0;
        v = Math.Clamp(v, 0.0, 1.0);
        if (v > _thresholds.High)
            return _colors.High;
        if (v > _thresholds.Mid)
            return _colors.Mid;
        if (v > _thresholds.Low)
            return _colors.Low;
        return _colors.Critical;
    }

    // ── IWidget.Render ─────────────────────────────────────────────────────

    /// <summary>Render the MiniBar into the frame at the given area.</summary>
    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty)
            return;

        var deg = frame.Degradation;
        if (!deg.RenderContent())
            return;

        var baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextRow(frame, area, baseStyle);

        double value = NormalizedValue();

        if (!deg.RenderDecorative())
        {
            if (_showPercent)
            {
                var pct = $"{value * 100.0,3:F0}%";
                int pctWidth = TerminalTextWidth.DisplayWidth(pct);
                if (area.Width >= pctWidth)
                {
                    ushort textX = (ushort)(area.Right - pctWidth);
                    WidgetDrawing.DrawTextSpan(frame, textX, area.Y, pct, WidgetStyle.Default, area.Right);
                }
                else
                {
                    WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, pct.TrimStart(), WidgetStyle.Default, area.Right);
                }
            }
            return;
        }

        int barWidth = Math.Min(_width, area.Width);
        bool renderPercent = false;
        string percentText = string.Empty;
        string percentOnlyText = string.Empty;
        int percentWidth = 0;

        if (_showPercent)
        {
            percentText = $" {value * 100.0,3:F0}%";
            percentOnlyText = percentText.TrimStart();
            renderPercent = true;
            percentWidth = TerminalTextWidth.DisplayWidth(percentText);
        }

        if (renderPercent)
        {
            if (area.Width <= percentWidth)
            {
                barWidth = 0;
            }
            else
            {
                int available = area.Width - percentWidth;
                barWidth = Math.Min(barWidth, available);
            }
        }

        if (barWidth == 0)
        {
            if (renderPercent)
            {
                WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, percentOnlyText, WidgetStyle.Default, area.Right);
            }
            return;
        }

        var color = ColorForValueWithPalette(value);
        int filled = FilledCells(barWidth);

        for (int i = 0; i < barWidth; i++)
        {
            ushort x = (ushort)(area.X + i);
            if (x >= area.Right)
                break;
            char ch = i < filled ? _filledChar : _emptyChar;
            var cell = Cell.FromChar(ch);
            if (deg.ApplyStyling())
            {
                WidgetDrawing.ApplyStyle(ref cell, _style);
                if (i < filled)
                    cell = cell.WithForeground(color);
            }
            frame.Buffer.SetFast(x, area.Y, cell);
        }

        if (renderPercent)
        {
            ushort textX = (ushort)(area.X + barWidth);
            WidgetDrawing.DrawTextSpan(frame, textX, area.Y, percentText, WidgetStyle.Default, area.Right);
        }
    }

    // ── IMeasurableWidget ─────────────────────────────────────────────────

    /// <summary>MiniBar has fixed dimensions.</summary>
    public SizeConstraints Measure(Size available)
    {
        // " XXX%" = 5 chars when show_percent
        ushort percentWidthVal = _showPercent ? (ushort)5 : (ushort)0;
        ushort totalWidth = (ushort)(_width + percentWidthVal);

        return new SizeConstraints
        {
            Min = new Size(1, 1), // At least show something
            Preferred = new Size(totalWidth, 1),
            Max = new Size(totalWidth, 1), // Fixed size
        };
    }

    public bool HasIntrinsicSize() => _width > 0;

    /// <inheritdoc/>
    public SizeConstraints MeasureConstraints(Size available) => Measure(available);

    /// <inheritdoc/>
    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        var c = Measure(available);
        return direction == LayoutDirection.Horizontal
            ? new SizeHint(c.Min.Width, c.Preferred.Width, c.Max?.Width)
            : new SizeHint(c.Min.Height, c.Preferred.Height, c.Max?.Height);
    }
}
