// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/drift_visualization.rs
// Drift-triggered fallback visualization widget with sparklines and regime banners.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

// ---------------------------------------------------------------------------
// Color palette
// ---------------------------------------------------------------------------

internal static class DriftColors
{
    public static readonly PackedRgba ZoneGreen  = PackedRgba.Rgba(0,   180, 0,   255);
    public static readonly PackedRgba ZoneYellow = PackedRgba.Rgba(200, 180, 0,   255);
    public static readonly PackedRgba ZoneRed    = PackedRgba.Rgba(200, 50,  50,  255);
    public static readonly PackedRgba FallbackFg = PackedRgba.Rgba(255, 80,  80,  255);
    public static readonly PackedRgba FallbackBg = PackedRgba.Rgba(80,  10,  10,  255);
    public static readonly PackedRgba RegimeFg   = PackedRgba.Rgba(255, 200, 100, 255);
    public static readonly PackedRgba DimFg      = PackedRgba.Rgba(120, 120, 120, 255);
    public static readonly PackedRgba LabelFg    = PackedRgba.Rgba(160, 180, 200, 255);
}

// ---------------------------------------------------------------------------
// DomainSnapshot — one frame's per-domain data
// ---------------------------------------------------------------------------

/// <summary>A single frame's confidence snapshot for one decision domain.</summary>
public sealed class DomainSnapshot
{
    /// <summary>The decision domain.</summary>
    public DecisionDomain Domain { get; set; }
    /// <summary>Confidence value (0.0 = no confidence, 1.0 = full confidence).</summary>
    public double Confidence { get; set; }
    /// <summary>Current traffic light signal.</summary>
    public TrafficLight Signal { get; set; }
    /// <summary>Whether the domain is currently in fallback mode.</summary>
    public bool InFallback { get; set; }
    /// <summary>Active strategy/action label index (for regime display).</summary>
    public string RegimeLabel { get; set; } = "";
}

// ---------------------------------------------------------------------------
// DriftSnapshot — snapshot of all domains at a single point in time
// ---------------------------------------------------------------------------

/// <summary>Snapshot of all domains at a single point in time.</summary>
public sealed class DriftSnapshot
{
    /// <summary>Per-domain snapshots.</summary>
    public List<DomainSnapshot> Domains { get; set; } = [];
    /// <summary>Frame number or tick count for timeline reference.</summary>
    public ulong FrameId { get; set; }
}

// ---------------------------------------------------------------------------
// DriftTimeline — ring buffer of snapshots
// ---------------------------------------------------------------------------

/// <summary>Ring buffer of recent drift snapshots for sparkline rendering.</summary>
public sealed class DriftTimeline
{
    /// <summary>Circular buffer of snapshots.</summary>
    private readonly List<DriftSnapshot> _snapshots;
    /// <summary>Write cursor (next position to write).</summary>
    private int _writePos;
    /// <summary>Number of snapshots stored (≤ capacity).</summary>
    private int _len;
    /// <summary>Maximum capacity.</summary>
    private readonly int _capacity;

    /// <summary>Create a new timeline with the given capacity (max frames to retain).</summary>
    public DriftTimeline(int capacity)
    {
        _capacity = Math.Max(capacity, 1);
        _snapshots = new List<DriftSnapshot>(_capacity);
        _writePos = 0;
        _len = 0;
    }

    /// <summary>Push a new snapshot into the timeline.</summary>
    public void Push(DriftSnapshot snapshot)
    {
        if (_snapshots.Count < _capacity)
        {
            _snapshots.Add(snapshot);
        }
        else
        {
            _snapshots[_writePos] = snapshot;
        }
        _writePos = (_writePos + 1) % _capacity;
        _len = Math.Min(_len + 1, _capacity);
    }

    /// <summary>Number of snapshots stored.</summary>
    public int Len() => _len;

    /// <summary>Whether the timeline is empty.</summary>
    public bool IsEmpty() => _len == 0;

    /// <summary>Iterate snapshots in chronological order (oldest first).</summary>
    public IEnumerable<DriftSnapshot> IterChronological()
    {
        int start = _len < _capacity ? 0 : _writePos;
        for (int i = 0; i < _len; i++)
        {
            int idx = (start + i) % _capacity;
            yield return _snapshots[idx];
        }
    }

    /// <summary>Extract confidence values for a specific domain in chronological order.</summary>
    public List<double> ConfidenceSeries(DecisionDomain domain)
    {
        var result = new List<double>(_len);
        foreach (var snap in IterChronological())
        {
            var ds = snap.Domains.Find(d => d.Domain == domain);
            result.Add(ds?.Confidence ?? 0.0);
        }
        return result;
    }

    /// <summary>Find the most recent snapshot where a domain transitioned into fallback.</summary>
    public int? LastFallbackTrigger(DecisionDomain domain)
    {
        var series = new List<bool>(_len);
        foreach (var snap in IterChronological())
        {
            var ds = snap.Domains.Find(d => d.Domain == domain);
            series.Add(ds is { InFallback: true });
        }

        if (series.Count > 0 && series[0])
            return 0;

        // Find the last rising edge (false -> true)
        for (int i = series.Count - 1; i >= 1; i--)
        {
            if (series[i] && !series[i - 1])
                return i;
        }
        return null;
    }

    /// <summary>Get the latest snapshot, if any.</summary>
    public DriftSnapshot? Latest()
    {
        if (_len == 0)
            return null;
        int idx = _writePos == 0 ? _capacity - 1 : _writePos - 1;
        return idx < _snapshots.Count ? _snapshots[idx] : null;
    }
}

// ---------------------------------------------------------------------------
// TrafficLight — traffic light signal for quick confidence assessment
// ---------------------------------------------------------------------------

/// <summary>
/// Traffic light signal for quick confidence assessment.
/// Port of ftui-runtime::transparency::TrafficLight.
/// </summary>
public enum TrafficLight
{
    /// <summary>High confidence in the chosen action.</summary>
    Green,
    /// <summary>Moderate confidence — decision is reasonable but uncertain.</summary>
    Yellow,
    /// <summary>Low confidence — near-fallback territory.</summary>
    Red,
}

// ---------------------------------------------------------------------------
// DriftVisualization widget
// ---------------------------------------------------------------------------

/// <summary>
/// Compound widget rendering drift-triggered fallback visualization.
///
/// Shows one sparkline row per decision domain, color-coded by confidence zone:
/// - Green zone (&gt;0.7): high confidence, Bayesian strategy active
/// - Yellow zone (0.3–0.7): moderate confidence, potential drift
/// - Red zone (&lt;0.3): low confidence, fallback likely/active
///
/// When a domain enters fallback, a vertical marker appears on the sparkline
/// and a regime banner flashes.
/// </summary>
public sealed class DriftVisualization : IWidget
{
    /// <summary>The timeline data source.</summary>
    private readonly DriftTimeline _timeline;
    /// <summary>Which domains to display (null = all from latest snapshot).</summary>
    private List<DecisionDomain>? _domains;
    /// <summary>Border type for the widget.</summary>
    private BorderType _borderType;
    /// <summary>Base style.</summary>
    private WidgetStyle _style;
    /// <summary>Whether to show the regime banner.</summary>
    private bool _showRegimeBanner;
    /// <summary>Fallback threshold (confidence below this = red zone).</summary>
    private double _fallbackThreshold;
    /// <summary>Caution threshold (confidence below this = yellow zone).</summary>
    private double _cautionThreshold;

    /// <summary>Create a new drift visualization from a timeline.</summary>
    public DriftVisualization(DriftTimeline timeline)
    {
        _timeline = timeline;
        _domains = null;
        _borderType = BorderType.Rounded;
        _style = WidgetStyle.Default;
        _showRegimeBanner = true;
        _fallbackThreshold = 0.3;
        _cautionThreshold = 0.7;
    }

    /// <summary>Only display the specified domains.</summary>
    public DriftVisualization Domains(List<DecisionDomain> domains)
    {
        _domains = domains;
        return this;
    }

    /// <summary>Set the border type.</summary>
    public DriftVisualization WithBorderType(BorderType borderType)
    {
        _borderType = borderType;
        return this;
    }

    /// <summary>Set the base style.</summary>
    public DriftVisualization WithStyle(WidgetStyle style)
    {
        _style = style;
        return this;
    }

    /// <summary>Enable or disable the regime banner row.</summary>
    public DriftVisualization ShowRegimeBanner(bool show)
    {
        _showRegimeBanner = show;
        return this;
    }

    /// <summary>Set the fallback confidence threshold (default 0.3).</summary>
    public DriftVisualization FallbackThreshold(double t)
    {
        _fallbackThreshold = t;
        return this;
    }

    /// <summary>Set the caution confidence threshold (default 0.7).</summary>
    public DriftVisualization CautionThreshold(double t)
    {
        _cautionThreshold = t;
        return this;
    }

    /// <summary>Determine which domains to render.</summary>
    private List<DecisionDomain> ActiveDomains()
    {
        if (_domains is not null)
            return _domains;
        var latest = _timeline.Latest();
        if (latest is not null)
            return latest.Domains.Select(d => d.Domain).ToList();
        return [];
    }

    /// <summary>Color for a confidence value.</summary>
    public PackedRgba ConfidenceColor(double confidence)
    {
        double fallback = Math.Clamp(_fallbackThreshold, 0.0, 1.0);
        double caution  = Math.Clamp(_cautionThreshold, fallback, 1.0);

        if (confidence >= caution)
        {
            return DriftColors.ZoneGreen;
        }
        else if (caution > fallback && confidence >= fallback)
        {
            // Interpolate yellow
            double t = (confidence - fallback) / (caution - fallback);
            return DriftHelpers.LerpColor(DriftColors.ZoneYellow, DriftColors.ZoneGreen, t);
        }
        else
        {
            // Interpolate red
            double t = fallback <= double.Epsilon ? 0.0 : confidence / fallback;
            return DriftHelpers.LerpColor(DriftColors.ZoneRed, DriftColors.ZoneYellow, t);
        }
    }

    /// <summary>Minimum height needed for the widget.</summary>
    public ushort MinHeight()
    {
        var domains = ActiveDomains();
        ushort domainRows = (ushort)domains.Count;
        // border_top + title + domains*(label_row + sparkline_row) + banner? + border_bottom
        ushort h = 2; // top + bottom border
        h += 1;       // title row
        h += (ushort)(domainRows * 2); // label + sparkline per domain
        if (_showRegimeBanner)
            h += 1;
        return h;
    }

    private ushort RenderDomainRow(
        DecisionDomain domain,
        ushort x,
        ushort y,
        ushort width,
        Frame frame)
    {
        var deg = frame.Degradation;
        bool applyStyling = deg.ApplyStyling();
        ushort maxX = (ushort)(x + width);

        // Row 1: Domain label + current confidence badge
        string label = DecisionDomainMeta.AsStr(domain);
        var labelStyle = applyStyling
            ? new WidgetStyle(DriftColors.LabelFg, null, null)
            : WidgetStyle.Default;
        ushort cx = WidgetDrawing.DrawTextSpan(frame, x, y, label, labelStyle, maxX);

        // Current confidence badge
        var latest = _timeline.Latest();
        if (latest is not null)
        {
            var ds = latest.Domains.Find(d => d.Domain == domain);
            if (ds is not null)
            {
                string confPct = $" {ds.Confidence * 100.0:F0}%";
                PackedRgba confColor = ConfidenceColor(ds.Confidence);
                var confStyle = applyStyling
                    ? new WidgetStyle(confColor, null, CellStyleFlags.Bold)
                    : WidgetStyle.Default;
                if (cx < maxX)
                    cx = WidgetDrawing.DrawTextSpan(frame, cx, y, " ", WidgetStyle.Default, maxX);
                cx = WidgetDrawing.DrawTextSpan(frame, cx, y, confPct, confStyle, maxX);

                if (ds.InFallback)
                {
                    var fbStyle = applyStyling
                        ? new WidgetStyle(DriftColors.FallbackFg, DriftColors.FallbackBg, CellStyleFlags.Bold)
                        : WidgetStyle.Default;
                    if (cx < maxX)
                        cx = WidgetDrawing.DrawTextSpan(frame, cx, y, " ", WidgetStyle.Default, maxX);
                    cx = WidgetDrawing.DrawTextSpan(frame, cx, y, " FALLBACK ", fbStyle, maxX);
                }
                _ = cx;
            }
        }

        // Row 2: Sparkline
        var series = _timeline.ConfidenceSeries(domain);
        if (series.Count > 0)
        {
            ushort sparklineWidth = (ushort)Math.Min(width, series.Count);
            // Take the last `sparklineWidth` values
            int start = series.Count > sparklineWidth ? series.Count - sparklineWidth : 0;
            var visible = series.Skip(start).ToArray();

            var sparkline = new Sparkline(visible)
                .Min(0.0)
                .Max(1.0)
                .Gradient(DriftColors.ZoneRed, DriftColors.ZoneGreen);
            var sparkArea = new Rect(x, (ushort)(y + 1), sparklineWidth, 1);
            sparkline.Render(sparkArea, frame);

            // Overlay fallback trigger marker (vertical bar at trigger point)
            int? triggerIdx = _timeline.LastFallbackTrigger(domain);
            if (triggerIdx.HasValue)
            {
                int visibleStart = series.Count > sparklineWidth ? series.Count - sparklineWidth : 0;
                if (triggerIdx.Value >= visibleStart)
                {
                    ushort markerX = (ushort)(x + (triggerIdx.Value - visibleStart));
                    if (markerX < maxX)
                    {
                        var cell = Cell.FromChar('|');
                        if (applyStyling)
                            WidgetDrawing.ApplyStyle(ref cell, new WidgetStyle(DriftColors.FallbackFg, null, CellStyleFlags.Bold));
                        frame.Buffer.SetFast(markerX, (ushort)(y + 1), cell);
                    }
                }
            }
        }

        return (ushort)(y + 2); // consumed 2 rows
    }

    private void RenderRegimeBanner(ushort x, ushort y, ushort maxX, Frame frame)
    {
        var latest = _timeline.Latest();
        if (latest is null)
            return;
        bool applyStyling = frame.Degradation.ApplyStyling();

        // Find any domain in fallback
        var fallbackDomain = latest.Domains.Find(d => d.InFallback);

        if (fallbackDomain is not null)
        {
            string banner = $" REGIME: {DecisionDomainMeta.AsStr(fallbackDomain.Domain)} -> deterministic ({fallbackDomain.RegimeLabel}) ";
            var style = applyStyling
                ? new WidgetStyle(DriftColors.RegimeFg, DriftColors.FallbackBg, CellStyleFlags.Bold)
                : WidgetStyle.Default;
            WidgetDrawing.DrawTextSpan(frame, x, y, banner, style, maxX);
        }
        else
        {
            // Normal operation
            var style = applyStyling
                ? new WidgetStyle(DriftColors.DimFg, null, null)
                : WidgetStyle.Default;
            WidgetDrawing.DrawTextSpan(frame, x, y, "All domains: Bayesian (normal)", style, maxX);
        }
    }

    // ── IWidget ──────────────────────────────────────────────────────────────

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0)
            return;

        if (area.Width < 6 || area.Height < 4)
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            return;
        }

        var deg = frame.Degradation;
        if (!deg.RenderContent())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            return;
        }

        var baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, area, baseStyle);

        // Draw border
        if (deg.RenderDecorative())
        {
            var set = deg.UseUnicodeBorders()
                ? _borderType.ToBorderSet()
                : BorderSet.Ascii;
            var borderStyle = deg.ApplyStyling()
                ? new WidgetStyle(DriftColors.LabelFg, null, null)
                : WidgetStyle.Default;
            DriftHelpers.RenderBorder(area, frame, set, borderStyle);
        }

        // Inner area
        ushort innerX    = (ushort)(area.X + 1);
        ushort innerMaxX = (ushort)(area.Right - 1);
        ushort innerWidth = (ushort)(innerMaxX > innerX ? innerMaxX - innerX : 0);
        ushort y    = (ushort)(area.Y + 1);
        ushort maxY = (ushort)(area.Bottom - 1);

        if (innerWidth < 4 || y >= maxY)
            return;

        // Title row
        var titleStyle = deg.ApplyStyling()
            ? new WidgetStyle(DriftColors.LabelFg, null, CellStyleFlags.Bold)
            : WidgetStyle.Default;
        WidgetDrawing.DrawTextSpan(frame, innerX, y, "Drift Monitor", titleStyle, innerMaxX);
        y++;

        // Domain rows
        var domains = ActiveDomains();
        foreach (var domain in domains)
        {
            if (y + 1 >= maxY)
                break;
            y = RenderDomainRow(domain, innerX, y, innerWidth, frame);
        }

        // Regime banner
        if (_showRegimeBanner && y < maxY)
            RenderRegimeBanner(innerX, y, innerMaxX, frame);
    }

    public bool IsEssential() => false;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

internal static class DriftHelpers
{
    /// <summary>Linear interpolation between two colors.</summary>
    public static PackedRgba LerpColor(PackedRgba a, PackedRgba b, double t)
    {
        float ft = (float)Math.Clamp(t, 0.0, 1.0);
        byte r  = (byte)MathF.Round(a.R * (1.0f - ft) + b.R * ft);
        byte g  = (byte)MathF.Round(a.G * (1.0f - ft) + b.G * ft);
        byte bv = (byte)MathF.Round(a.B * (1.0f - ft) + b.B * ft);
        return PackedRgba.Rgba(r, g, bv, 255);
    }

    /// <summary>Render a full border (edges and corners) for the given area.</summary>
    public static void RenderBorder(Rect area, Frame frame, BorderSet set, WidgetStyle style)
    {
        Cell BorderCell(char c)
        {
            var cell = Cell.FromChar(c);
            WidgetDrawing.ApplyStyle(ref cell, style);
            return cell;
        }

        ushort rightX  = (ushort)(area.Right - 1);
        ushort bottomY = (ushort)(area.Bottom - 1);

        // Edges
        for (ushort ex = area.X; ex < area.Right; ex++)
        {
            frame.Buffer.SetFast(ex, area.Y,  BorderCell(set.Horizontal));
            frame.Buffer.SetFast(ex, bottomY, BorderCell(set.Horizontal));
        }
        for (ushort ey = area.Y; ey < area.Bottom; ey++)
        {
            frame.Buffer.SetFast(area.X, ey, BorderCell(set.Vertical));
            frame.Buffer.SetFast(rightX, ey, BorderCell(set.Vertical));
        }

        // Corners
        frame.Buffer.SetFast(area.X,  area.Y,  BorderCell(set.TopLeft));
        frame.Buffer.SetFast(rightX,  area.Y,  BorderCell(set.TopRight));
        frame.Buffer.SetFast(area.X,  bottomY, BorderCell(set.BottomLeft));
        frame.Buffer.SetFast(rightX,  bottomY, BorderCell(set.BottomRight));
    }
}
