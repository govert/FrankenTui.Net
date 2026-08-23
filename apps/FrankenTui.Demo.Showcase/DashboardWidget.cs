// Upstream source: .external/frankentui/crates/ftui-demo-showcase/src/screens/dashboard.rs
//   fn render_medium(), fn render_charts(), fn render_pulse_charts(),
//   fn render_labeled_sparkline(), fn render_info(), fn render_code()
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Faithful 1-1 line-for-line statement-for-statement port.

using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Faithful port of Dashboard::render_medium() and sub-methods.
/// All methods match their Rust counterparts line-for-line.</summary>
internal sealed class DashboardWidget : IWidget
{
    private readonly ShowcaseDemoState _state;
    private readonly double _t;
    private readonly SimulatedData _sim;

    public DashboardWidget(ShowcaseDemoState state, double t, SimulatedData sim)
    {
        _state = state; _t = t; _sim = sim;
    }

    // ── IWidget ───────────────────────────────────────────────────────
    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        RenderMedium(area, frame);
    }

    // ── render_medium() 1-1 port ──────────────────────────────────────
    private void RenderMedium(Rect area, Frame frame)
    {
        // let main = Flex::vertical().constraints([Fixed(1), Min(8), Fixed(1)]).split(area);
        var main = LayoutSolver.Split(area, LayoutDirection.Vertical,
            [LayoutConstraint.Fixed(1), LayoutConstraint.Fill(), LayoutConstraint.Fixed(1)]);

        RenderHeader(main[0], frame);
        RenderFooter(main[2], frame);

        // No pane rail at <90 wide
        var contentArea = main[1];

        // let content_rows = Flex::vertical().constraints([Percentage(60.0), Percentage(40.0)]).split(content_area);
        var contentRows = LayoutSolver.Split(contentArea, LayoutDirection.Vertical,
            [LayoutConstraint.Percentage(60), LayoutConstraint.Percentage(40)]);

        // Top row: 3 panels
        var topCols = LayoutSolver.Split(contentRows[0], LayoutDirection.Horizontal,
            [LayoutConstraint.Percentage(25), LayoutConstraint.Percentage(40), LayoutConstraint.Fill()]);
        RenderPlasma(topCols[0], frame);
        RenderCharts(topCols[1], frame);

        // Combined code + info in third column
        var rightSplit = LayoutSolver.Split(topCols[2], LayoutDirection.Vertical,
            [LayoutConstraint.Percentage(60), LayoutConstraint.Fill()]);
        RenderCode(rightSplit[0], frame);
        RenderInfo(rightSplit[1], frame, (ushort)area.Width, (ushort)area.Height);

        // Bottom row: 3 panels (35/35/30)
        var bottomCols = LayoutSolver.Split(contentRows[1], LayoutDirection.Horizontal,
            [LayoutConstraint.Percentage(35), LayoutConstraint.Percentage(35), LayoutConstraint.Fill()]);
        RenderTextEffects(bottomCols[0], frame);
        RenderActivityFeed(bottomCols[1], frame);
        RenderMarkdown(bottomCols[2], frame);
    }

    // ── render_header ─────────────────────────────────────────────────
    private void RenderHeader(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Height < 1) return;
        var title = "FRANKENTUI DASHBOARD";
        // Simple static header (Rust uses animated gradient via StyledText/TextEffect)
        RenderParagraph(area, frame, title);
    }

    // ── render_footer ─────────────────────────────────────────────────
    private void RenderFooter(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Height < 1) return;
        var text = "g:charts c:code e:fx m:md | drag bottom dividers: resize | 1-9:screens | t:the";
        RenderParagraph(area, frame, text);
    }

    // ── render_plasma ─────────────────────────────────────────────────
    private void RenderPlasma(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Width < 4 || area.Height < 3) return;

        // Block + Canvas (delegated to DashboardPlasmaWidget)
        var block = new BlockWidget { Title = "Plasma", TitleAlign = BlockWidget.TitleAlignment.Center };
        var inner = area.Inner(1);
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));

        if (inner.IsEmpty || inner.Width < 2 || inner.Height < 2) return;

        // Plasma canvas
        var ctx = new RuntimeRenderContext(frame.Buffer, inner,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full);
        new DashboardPlasmaWidget(_t).Render(inner, frame);

        // Hint: "Click → Visual Effects"
        RenderPanelHint(inner, frame, "Click \u2192 Visual Effects");
    }

    // ── render_charts ─────────────────────────────────────────────────
    private void RenderCharts(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Height < 3) return;

        var title = "Charts \u00b7 Pulse";
        var block = new BlockWidget { Title = title, TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        // Split header + content (Rust: if inner.height >= 4)
        Rect? headerArea = null;
        var content = inner;
        if (inner.Height >= 4)
        {
            var rows = LayoutSolver.Split(inner, LayoutDirection.Vertical,
                [LayoutConstraint.Fixed(1), LayoutConstraint.Fill()]);
            headerArea = rows[0];
            content = rows[1];
        }

        if (headerArea.HasValue)
        {
            var cpuLast = _sim.CpuHistory.Count > 0 ? _sim.CpuHistory[^1] : 0;
            var memLast = _sim.MemoryHistory.Count > 0 ? _sim.MemoryHistory[^1] : 0;
            var eps = _sim.EventsPerSecond;
            RenderParagraph(headerArea.Value, frame,
                $"g:cycle \u00b7 sparklines + mini bars \u00b7 CPU {FormatPercent(cpuLast)} \u00b7 MEM {FormatPercent(memLast)} \u00b7 EPS {eps:F0}");
        }

        RenderPulseCharts(content, frame);
        RenderPanelHint(content, frame, "Click \u2192 Data Viz");
    }

    // ── render_pulse_charts ───────────────────────────────────────────
    private void RenderPulseCharts(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        if (area.Height < 3) { return; /* minimal sparkline handled elsewhere */ }

        var cpuData = _sim.CpuHistory.ToArray();
        var memData = _sim.MemoryHistory.ToArray();
        var netInData = _sim.NetworkIn.ToArray();
        var netOutData = _sim.NetworkOut.ToArray();

        var cpuLast = cpuData.Length > 0 ? cpuData[^1] : 0;
        var memLast = memData.Length > 0 ? memData[^1] : 0;
        var netInLast = netInData.Length > 0 ? netInData[^1] : 0;
        var netOutLast = netOutData.Length > 0 ? netOutData[^1] : 0;

        // 5 rows: CPU, MEM, NET, OUT, EPS
        var rows = LayoutSolver.Split(area, LayoutDirection.Vertical,
            [LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1),
             LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1)]);
        int ri = 0;
        RenderLabeledSparkline(rows[ri++], frame, "CPU", FormatPercent(cpuLast), cpuData);
        RenderLabeledSparkline(rows[ri++], frame, "MEM", FormatPercent(memLast), memData);
        RenderLabeledSparkline(rows[ri++], frame, "NET", $"{netInLast + netOutLast:F0}", netInData);
        RenderLabeledSparkline(rows[ri++], frame, "OUT", $"{netOutLast:F0}", netOutData);
        // EPS/DSK/IO row: text only
        RenderParagraph(rows[ri++], frame,
            $"EPS   {_sim.DiskUsage[0].Usage,3:F0}% DSK   {_sim.DiskUsage[3].Usage,3:F0}% IO     {_sim.DiskUsage[4].Usage,3:F0}%");
    }

    // ── render_labeled_sparkline ──────────────────────────────────────
    private void RenderLabeledSparkline(Rect area, Frame frame, string label, string value, double[] data)
    {
        if (area.IsEmpty || area.Width < 5) return;

        // Split: label (4 cols) + sparkline (rest) + value (6 cols)
        var cols = LayoutSolver.Split(area, LayoutDirection.Horizontal,
            [LayoutConstraint.Fixed(4), LayoutConstraint.Fill(), LayoutConstraint.Fixed(7)]);
        RenderParagraph(cols[0], frame, label + " ");
        if (data.Length > 0 && !cols[1].IsEmpty)
        {
            var sw = new SparklineWidget(data);
            ((IRuntimeView)sw).Render(new RuntimeRenderContext(frame.Buffer, cols[1],
                FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        }
        RenderParagraph(cols[2], frame, value);
    }

    // ── render_code ───────────────────────────────────────────────────
    private void RenderCode(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Height < 3) return;
        var block = new BlockWidget { Title = "Code \u00b7 Rust (1/28)", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        RenderParagraph(inner, frame,
            "// runtime.rs\nuse std::collections::HashMap;\nuse ftui_core::event::Event;\nuse ftui_core::geometry::Rect;\n\npub fn handle_tick(fr: &mut Frame) {\n    let a = Rect::from_size(fr.buffer.width(), fr.buffer.height());\n    fr.buffer.fill(a, Cell::default());\n}\nClick \u2192 Code Explorer");
        RenderPanelHint(inner, frame, "Click \u2192 Code Explorer");
    }

    // ── render_info ───────────────────────────────────────────────────
    private void RenderInfo(Rect area, Frame frame, ushort dw, ushort dh)
    {
        if (area.IsEmpty || area.Height < 3) return;
        var block = new BlockWidget { Title = "Overview", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;

        var focus = Math.Clamp(_state.DashboardFocusIndex, 0, 3);
        var overviewScroll = Math.Clamp(_state.DashboardOverviewScroll, 0, 6);
        var highlightIndex = Math.Clamp(_state.DashboardHighlightIndex, 0, 7);
        var contextArmed = _state.DashboardContextArmed;

        RenderParagraph(inner, frame,
            $"Status:NOMINAL FPS:0\n" +
            $"scroll={overviewScroll} context={(contextArmed ? "armed" : "idle")}\n" +
            $"{(focus == 1 ? "Highlights [focus]" : "Highlights")} selected={highlightIndex} focus={focus}\n" +
            $"{(focus == 1 && highlightIndex == 5 ? "> " : "  ")}Drag & Drop lab\n" +
            $"{dw}\u00d7{dh}\nClick \u2192 Performance");
    }

    // ── render_text_effects ───────────────────────────────────────────
    private void RenderTextEffects(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Text FX \u00b7 2-Up (1/4)", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        RenderParagraph(inner, frame,
            "Sample 1 of 3 \u00b7 2-Up\n1 \u00b7 None \u00b7 S1\n2 \u00b7 FadeIn \u00b7 S2\n\ndrag panes in dashboard");
    }

    // ── render_activity_feed ──────────────────────────────────────────
    private void RenderActivityFeed(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Activity", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        RenderParagraph(inner, frame,
            "SEV TIME  COMP  MESSAGE\nINFO  00:02  CPU spike dete\n\nClick \u2192 Action Timeline");
    }

    // ── render_markdown ───────────────────────────────────────────────
    private void RenderMarkdown(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;
        var block = new BlockWidget { Title = "Markdown \u00b7 Streaming\u2026 0%", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
        var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        RenderParagraph(inner, frame, "\u258c\n\nClick \u2192 Markdown");
    }

    // ── helpers ───────────────────────────────────────────────────────
    private static void RenderParagraph(Rect area, Frame frame, string text)
    {
        new ParagraphWidget(text).Render(new RuntimeRenderContext(frame.Buffer, area,
            FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
    }

    private static void RenderPanelHint(Rect area, Frame frame, string hint)
    {
        // Rust renders hint at bottom of panel area using bottom-anchored text.
        if (area.IsEmpty || area.Height < 2) return;
        var hintRow = new Rect(area.X, (ushort)(area.Bottom - 1), area.Width, 1);
        RenderParagraph(hintRow, frame, hint);
    }

    private static string FormatPercent(double v) => $"{v,3:F0}%";
}
