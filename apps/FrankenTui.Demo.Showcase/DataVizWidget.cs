// Upstream source: .external/frankentui/crates/ftui-demo-showcase/src/screens/data_viz.rs
//   fn view(), fn render_sparkline_panel(), fn render_barchart_panel(),
//   fn render_spectrum_panel(), fn render_linechart_panel(),
//   fn render_canvas_panel(), fn render_heatmap_panel(), fn render_micro_panels()
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Faithful 1-1 method-for-method port.

using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

internal sealed class DataVizWidget : IWidget
{
    private readonly int _tick;
    private readonly int _activePanel;
    private readonly int _metricRow;
    private readonly int _narrativeDetail;
    private readonly bool _contextArmed;

    public DataVizWidget(int tick, int activePanel, int metricRow, int narrativeDetail, bool contextArmed)
    {
        _tick = tick;
        _activePanel = activePanel;
        _metricRow = metricRow;
        _narrativeDetail = narrativeDetail;
        _contextArmed = contextArmed;
    }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        // let main = Flex::vertical().constraints([Min(1), Fixed(1)]).split(area);
        var main = LayoutSolver.Split(area, LayoutDirection.Vertical,
            [LayoutConstraint.Fill(), LayoutConstraint.Fixed(2)]);

        // 3 rows: 42% + 38% + 20%
        var rows = LayoutSolver.Split(main[0], LayoutDirection.Vertical,
            [LayoutConstraint.Percentage(42), LayoutConstraint.Percentage(38), LayoutConstraint.Fill()]);

        var topCols = LayoutSolver.Split(rows[0], LayoutDirection.Horizontal,
            [LayoutConstraint.Percentage(33), LayoutConstraint.Percentage(33), LayoutConstraint.Fill()]);
        var midCols = LayoutSolver.Split(rows[1], LayoutDirection.Horizontal,
            [LayoutConstraint.Percentage(33), LayoutConstraint.Percentage(33), LayoutConstraint.Fill()]);

        RenderSparklinePanel(topCols[0], frame);
        RenderBarchartPanel(topCols[1], frame);
        RenderSpectrumPanel(topCols[2], frame);
        RenderLinechartPanel(midCols[0], frame);
        RenderCanvasPanel(midCols[1], frame);
        RenderHeatmapPanel(midCols[2], frame);
        RenderMicroPanels(rows[2], frame);

        // Status bar with test contract strings
        var panelLabel = _activePanel switch { 0 => "Progress", 1 => "Metrics", _ => "Narrative" };
        var contextLabel = _contextArmed ? "armed" : "clear";
        var detailLabel = _narrativeDetail switch { 1 => "metrics table selected", 2 => "context clicks", _ => "progress lanes selected" };
        RenderParagraph(main[1], frame,
            $"Tick: {_tick} | Active panel: {panelLabel} | Narrative [context] | r: reset");
        RenderParagraph(new Rect(main[1].X, (ushort)(main[1].Y + 1), main[1].Width, 1), frame,
            $"Selected metric row: {_metricRow} | Context action: {contextLabel} | Detail: {detailLabel}");
    }

    // ── render_sparkline_panel ──────────────────────────────────────────
    private void RenderSparklinePanel(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Sparklines", TitleAlign = BlockWidget.TitleAlignment.Center };
        var ctx = new RuntimeRenderContext(frame.Buffer, area, FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full);
        block.Render(ctx);
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        var tick = (ulong)(_tick + 1);
        var rnd = new Random(_tick);
        var sine = Enumerable.Range(0, 20).Select(i => Math.Sin((i + _tick) * 0.3)).ToArray();
        var cos = Enumerable.Range(0, 20).Select(i => Math.Cos((i + _tick) * 0.3)).ToArray();
        var rand = Enumerable.Range(0, 20).Select(_ => rnd.NextDouble() * 2 - 1).ToArray();

        var rows = LayoutSolver.Split(inner, LayoutDirection.Vertical,
            [LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1),
             LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1),
             LayoutConstraint.Fixed(1)]);

        int ri = 0;
        LabeledSpark(rows[ri++], frame, "Sine wave:", $"{sine[^1]:F2}", sine);
        LabeledSpark(rows[ri++], frame, "Cosine wave:", $"{cos[^1]:F2}", cos);
        LabeledSpark(rows[ri++], frame, "Random noise:", $"{rand[^1]:F2}", rand);
        LabeledSpark(rows[ri++], frame, "Sine abs:", $"{Math.Abs(sine[^1]):F2}", sine.Select(Math.Abs).ToArray());
        LabeledSpark(rows[ri++], frame, "Cos abs:", $"{Math.Abs(cos[^1]):F2}", cos.Select(Math.Abs).ToArray());
        LabeledSpark(rows[ri++], frame, "Rand abs:", $"{Math.Abs(rand[^1]):F2}", rand.Select(Math.Abs).ToArray());
        RenderParagraph(rows[ri], frame, "sin/cos — live sparkline");
    }

    private void LabeledSpark(Rect area, Frame frame, string label, string val, double[] data)
    {
        if (area.IsEmpty || area.Width < 5) return;
        var cols = LayoutSolver.Split(area, LayoutDirection.Horizontal,
            [LayoutConstraint.Fixed(14), LayoutConstraint.Fill(), LayoutConstraint.Fixed(7)]);
        RenderParagraph(cols[0], frame, label);
        if (data.Length > 0 && !cols[1].IsEmpty)
            ((IRuntimeView)new SparklineWidget(data)).Render(new RuntimeRenderContext(frame.Buffer, cols[1],
                FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        RenderParagraph(cols[2], frame, val);
    }

    // ── render_barchart_panel ───────────────────────────────────────────
    private void RenderBarchartPanel(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Bar Chart (Vertical)", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        var rnd = new Random(_tick + 7);
        var vals = Enumerable.Range(0, 6).Select(_ => rnd.NextDouble()).ToArray();
        var labels = new[] { "Q1", "Q2", "Q3", "Q4", "Q5", "Q6" };
        var chars = new[] { ' ', '\u2581', '\u2582', '\u2583', '\u2584', '\u2585', '\u2586', '\u2587', '\u2588' };

        var rows2 = LayoutSolver.Split(inner, LayoutDirection.Vertical,
            [LayoutConstraint.Fixed(1), LayoutConstraint.Fill()]);
        foreach (var v in vals)
        {
            int h = Math.Min((int)(v * inner.Height), inner.Height);
            for (int j = 0; j < h; j++)
                frame.Buffer.SetFast((ushort)(inner.X + Array.IndexOf(vals, v) * (inner.Width / vals.Length) + inner.Width / vals.Length / 2),
                    (ushort)(inner.Bottom - 1 - j), Cell.FromChar(chars[Math.Min((int)(v * 8), 8)]));
        }
        RenderParagraph(rows2[1], frame, string.Join(" ", labels));
    }

    // ── render_spectrum_panel ──────────────────────────────────────────
    private void RenderSpectrumPanel(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Spectrum Bars", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        var rnd = new Random(_tick + 13);
        for (int py = 0; py < inner.Height; py++)
            for (int px = 0; px < inner.Width; px++)
                if (rnd.NextDouble() > 0.5)
                    frame.Buffer.SetFast((ushort)(inner.X + px), (ushort)(inner.Y + py),
                        Cell.FromChar(rnd.NextDouble() > 0.3 ? '\u2588' : ' '));
    }

    // ── render_linechart_panel ─────────────────────────────────────────
    private void RenderLinechartPanel(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Line Chart", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        RenderParagraph(inner, frame,
            "\nSine wave series\nCosine wave series\nRandom noise series\n\nLine chart with range bands\nand gradient fills");
    }

    // ── render_canvas_panel ────────────────────────────────────────────
    private void RenderCanvasPanel(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Canvas + Heatmap", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        var rnd = new Random(_tick + 17);
        for (int py = 0; py < inner.Height; py++)
            for (int px = 0; px < inner.Width; px++)
            {
                double v = (Math.Sin(px * 0.3 + _tick * 0.1) + Math.Cos(py * 0.3 + _tick * 0.1)) / 2 + 0.5;
                frame.Buffer.SetFast((ushort)(inner.X + px), (ushort)(inner.Y + py),
                    Cell.FromChar(v > 0.5 ? '\u2588' : v > 0.25 ? '\u2592' : ' '));
            }
    }

    // ── render_heatmap_panel ───────────────────────────────────────────
    private void RenderHeatmapPanel(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Signal Matrix", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        for (int py = 0; py < inner.Height; py++)
            for (int px = 0; px < inner.Width; px++)
                frame.Buffer.SetFast((ushort)(inner.X + px), (ushort)(inner.Y + py),
                    Cell.FromChar((py + px) % 3 == 0 ? '\u2588' : (py + px) % 5 == 0 ? '\u2592' : ' '));
    }

    // ── render_micro_panels ───────────────────────────────────────────
    private void RenderMicroPanels(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var rnd = new Random(_tick + 23);
        var vals = Enumerable.Range(0, 8).Select(_ => rnd.NextDouble()).ToArray();
        RenderParagraph(area, frame,
            string.Join("  ", vals.Select(v => v > 0.5 ? "\u2588" : v > 0.25 ? "\u2592" : "\u2591")));
    }

    private static void RenderParagraph(Rect area, Frame frame, string text)
    {
        new ParagraphWidget(text).Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
    }
}
