// Port of .external/frankentui/crates/ftui-widgets/src/voi_debug_overlay.rs
// VOI debug overlay widget (Galaxy-Brain).

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

/// <summary>Summary of the VOI posterior.</summary>
public sealed class VoiPosteriorSummary
{
    public double Alpha { get; init; }
    public double Beta { get; init; }
    public double Mean { get; init; }
    public double Variance { get; init; }
    public double ExpectedVarianceAfter { get; init; }
    public double VoiGain { get; init; }
}

/// <summary>Summary of the most recent VOI decision.</summary>
public sealed class VoiDecisionSummary
{
    public ulong EventIdx { get; init; }
    public bool ShouldSample { get; init; }
    public string Reason { get; init; } = "";
    public double Score { get; init; }
    public double Cost { get; init; }
    public double LogBayesFactor { get; init; }
    public double EValue { get; init; }
    public double EThreshold { get; init; }
    public double BoundaryScore { get; init; }
}

/// <summary>Summary of the most recent VOI observation.</summary>
public sealed class VoiObservationSummary
{
    public ulong SampleIdx { get; init; }
    public bool Violated { get; init; }
    public double PosteriorMean { get; init; }
    public double Alpha { get; init; }
    public double Beta { get; init; }
}

/// <summary>Ledger entries for the VOI debug overlay.</summary>
public abstract class VoiLedgerEntry
{
    private VoiLedgerEntry() { }

    /// <summary>Decision ledger entry variant.</summary>
    public sealed class Decision : VoiLedgerEntry
    {
        public ulong EventIdx { get; init; }
        public bool ShouldSample { get; init; }
        public double VoiGain { get; init; }
        public double LogBayesFactor { get; init; }
    }

    /// <summary>Observation ledger entry variant.</summary>
    public sealed class Observation : VoiLedgerEntry
    {
        public ulong SampleIdx { get; init; }
        public bool Violated { get; init; }
        public double PosteriorMean { get; init; }
    }
}

/// <summary>Full overlay data payload.</summary>
public sealed class VoiOverlayData
{
    public string Title { get; init; } = "";
    public ulong? Tick { get; init; }
    public string? Source { get; init; }
    public VoiPosteriorSummary Posterior { get; init; } = new();
    public VoiDecisionSummary? Decision { get; init; }
    public VoiObservationSummary? Observation { get; init; }
    public List<VoiLedgerEntry> Ledger { get; init; } = new();
}

/// <summary>Styling options for the VOI overlay.</summary>
public sealed class VoiOverlayStyle
{
    public WidgetStyle Border { get; init; } = WidgetStyle.Default;
    public WidgetStyle Text { get; init; } = WidgetStyle.Default;
    public PackedRgba? Background { get; init; }
    public BorderType BorderType { get; init; } = BorderType.Rounded;

    public static VoiOverlayStyle Default => new();
}

/// <summary>VOI debug overlay widget.</summary>
public sealed class VoiDebugOverlay : IWidget
{
    private readonly VoiOverlayData _data;
    private VoiOverlayStyle _style;

    /// <summary>Create a new VOI overlay widget.</summary>
    public VoiDebugOverlay(VoiOverlayData data)
    {
        _data = data;
        _style = VoiOverlayStyle.Default;
    }

    // Expose style field for test assertions (mirrors upstream struct field visibility)
    internal VoiOverlayStyle StyleField => _style;

    /// <summary>Override styling for the overlay.</summary>
    public VoiDebugOverlay WithStyle(VoiOverlayStyle style)
    {
        _style = style;
        return this;
    }

    internal List<string> BuildLines(int lineWidth)
    {
        var lines = new List<string>(20);
        var divider = new string('-', lineWidth);

        var header = _data.Title;
        if (_data.Tick.HasValue)
            header += $" (tick {_data.Tick.Value})";
        if (_data.Source is { } source)
            header += $" [{source}]";

        lines.Add(header);
        lines.Add(divider);

        if (_data.Decision is { } decision)
        {
            var verdict = decision.ShouldSample ? "SAMPLE" : "SKIP";
            lines.Add($"Decision: {verdict,-6}  reason: {decision.Reason}");
            lines.Add($"log10 BF: {decision.LogBayesFactor:+0.000;-0.000}  score/cost");
            lines.Add($"E: {decision.EValue:F3} / {decision.EThreshold:F2}  boundary: {decision.BoundaryScore:F3}");
        }
        else
        {
            lines.Add("Decision: —");
        }

        lines.Add("");
        lines.Add("Posterior Core");
        lines.Add(divider);
        lines.Add($"p ~ Beta(a,b)  a={_data.Posterior.Alpha:F2}  b={_data.Posterior.Beta:F2}");
        lines.Add($"mu={_data.Posterior.Mean:F4}  Var={_data.Posterior.Variance:F6}");
        lines.Add("VOI = Var[p] - E[Var|1]");
        lines.Add($"VOI = {_data.Posterior.Variance:F6} - {_data.Posterior.ExpectedVarianceAfter:F6} = {_data.Posterior.VoiGain:F6}");

        if (_data.Decision is { } decisionEq)
        {
            lines.Add("");
            lines.Add("Decision Equation");
            lines.Add(divider);
            lines.Add($"score={decisionEq.Score:F6}  cost={decisionEq.Cost:F6}");
            lines.Add($"log10 BF = log10({decisionEq.Score:F6}/{decisionEq.Cost:F6}) = {decisionEq.LogBayesFactor:+0.000;-0.000}");
        }

        if (_data.Observation is { } obs)
        {
            lines.Add("");
            lines.Add("Last Sample");
            lines.Add(divider);
            lines.Add($"violated: {obs.Violated.ToString().ToLowerInvariant()}  a={obs.Alpha:F1}  b={obs.Beta:F1}  mu={obs.PosteriorMean:F3}");
        }

        if (_data.Ledger.Count > 0)
        {
            lines.Add("");
            lines.Add("Evidence Ledger (Recent)");
            lines.Add(divider);
            foreach (var entry in _data.Ledger)
            {
                switch (entry)
                {
                    case VoiLedgerEntry.Decision d:
                    {
                        var verdict = d.ShouldSample ? "S" : "-";
                        lines.Add($"D#{d.EventIdx,3} {verdict} VOI={d.VoiGain:F5} logBF={d.LogBayesFactor:+0.00;-0.00}");
                        break;
                    }
                    case VoiLedgerEntry.Observation o:
                        lines.Add($"O#{o.SampleIdx,3} viol={o.Violated.ToString().ToLowerInvariant()} mu={o.PosteriorMean:F3}");
                        break;
                }
            }
        }

        return lines;
    }

    /// <inheritdoc/>
    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty)
            return;

        if (area.Width < 20 || area.Height < 6)
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

        if (deg.ApplyStyling() && _style.Background is { } bg)
        {
            var cell = Cell.Empty.WithBackground(bg);
            frame.Buffer.Fill(area, cell);
        }

        var block = Block.New()
            .Borders_(Borders.All)
            .BorderType(_style.BorderType)
            .BorderStyle(_style.Border)
            .Title(_data.Title)
            .TitleAlignment(Alignment.Center)
            .Style(_style.Text);

        var inner = block.Inner(area);
        block.Render(area, frame);

        if (inner.IsEmpty)
            return;

        int lineWidth = Math.Max(1, (int)inner.Width - 2);
        var lines = BuildLines(lineWidth);
        var text = string.Join("\n", lines);
        new Paragraph(TextContent.Raw(text))
            .Style(_style.Text)
            .Render(inner, frame);
    }
}
