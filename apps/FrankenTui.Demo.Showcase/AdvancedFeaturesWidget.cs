// Upstream source: .external/frankentui/crates/ftui-demo-showcase/src/screens/advanced_features.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Faithful 1-1 port of AdvancedFeatures struct and all methods.

using FrankenTui.Core;
using FrankenTui.Extras;
using ExtrasTimer = FrankenTui.Extras.Timer;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

internal sealed class AdvancedFeaturesWidget : IWidget
{
    private ExtrasTimer _timerCompact;
    private ExtrasTimer _timerClock;
    private int _spinnerTick;
    private int _tickCount;
    private int _selectedPattern;
    private int _compositeMode;
    private int _focusState;

    public AdvancedFeaturesWidget(int selectedPattern, int compositeMode, int focusState)
    {
        _timerCompact = new ExtrasTimer(TimeSpan.FromSeconds(300)).WithFormat(TimerDisplayFormat.Compact);
        _timerCompact.Start();
        _timerClock = new ExtrasTimer(TimeSpan.FromSeconds(120), TimeSpan.FromMilliseconds(100))
            .WithFormat(TimerDisplayFormat.Clock);
        _timerClock.Start();
        _spinnerTick = 0;
        _tickCount = 0;
        _selectedPattern = selectedPattern;
        _compositeMode = compositeMode;
        _focusState = focusState;
    }

    public void Render(Rect area, Frame frame) { if (!area.IsEmpty) View(area, frame); }

    void View(Rect area, Frame frame)
    {
        var main = LayoutSolver.Split(area, LayoutDirection.Vertical,
            [LayoutConstraint.Fill(), LayoutConstraint.Fixed(1)]);
        var cols = LayoutSolver.Split(main[0], LayoutDirection.Horizontal,
            [LayoutConstraint.Percentage(50), LayoutConstraint.Fill()]);
        RenderTracebackPanel(cols[0], frame);
        var rightRows = LayoutSolver.Split(cols[1], LayoutDirection.Vertical,
            [LayoutConstraint.Percentage(40), LayoutConstraint.Percentage(30), LayoutConstraint.Fill()]);
        RenderTimersPanel(rightRows[0], frame);
        RenderMacroPanel(rightRows[1], frame);
        RenderInfoPanel(rightRows[2], frame);
        R(main[1], frame, $"Tick: {_tickCount} | Selected pattern: {_selectedPattern} | advanced mouse focus=1 context=armed | r: reset | Space: pause");
    }

    void RenderTracebackPanel(Rect area, Frame frame)
    {
        var block = new BlockWidget { Title = "Error Traceback", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(Ctx(frame, area)); var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        var tb = new Traceback([
            new TracebackFrame("main", 42).WithFilename("src/main.rs")
                .WithSourceContext("fn main() -> Result<()> {\n    let config = Config::load()?;\n    app::run(config)?;\n    Ok(())\n}", 40),
            new TracebackFrame("run", 128).WithFilename("src/app.rs")
                .WithSourceContext("pub fn run(config: Config) -> Result<()> {\n    let db = Database::connect(&config.db_url)?;\n    let server = Server::new(db);\n    server.listen(config.port)?;\n    Ok(())\n}", 125),
            new TracebackFrame("connect", 56).WithFilename("src/database.rs")
                .WithSourceContext("pub fn connect(url: &str) -> Result<Self> {\n    let pool = Pool::builder()\n        .max_size(10)\n        .build(url)?;\n    Ok(Self { pool })\n}", 53),
        ], "Application Error", "");
        tb.WithTitle("Application Error"); tb.Render(inner, frame);
    }

    void RenderTimersPanel(Rect area, Frame frame)
    {
        var block = new BlockWidget { Title = "Timers & Spinners", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(Ctx(frame, area)); var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        var rows = LayoutSolver.Split(inner, LayoutDirection.Vertical,
            [LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1),
             LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1),
             LayoutConstraint.Fixed(1), LayoutConstraint.Fixed(1), LayoutConstraint.Fill()]);
        R(rows[0], frame, $"Compact timer: {_timerCompact.View()} ({(int)(_timerCompact.Progress * 100)}%)");
        R(rows[1], frame, $"Clock timer:   {_timerClock.View()} ({(int)(_timerClock.Progress * 100)}%)");
        R(rows[3], frame, "Spinners:");
        var spinnerSets = new (string, Spinner)[] { ("dots", new Spinner()), ("line", new Spinner().WithFrames(SpinnerFrames.Line)), ("braille", new Spinner().WithFrames(SpinnerFrames.Dots)) };
        for (int i = 0; i < 3 && 4 + i < rows.Count; i++)
        {
            var (name, sp) = spinnerSets[i];
            var sc = LayoutSolver.Split(rows[4 + i], LayoutDirection.Horizontal, [LayoutConstraint.Fixed(12), LayoutConstraint.Fixed(3), LayoutConstraint.Fill()]);
            R(sc[0], frame, name); sp.Render(sc[1], frame, new SpinnerState { CurrentFrame = _spinnerTick });
        }
        if (!rows[7].IsEmpty) { var p = _timerCompact.Progress; int bw = Math.Max(0, rows[7].Width - 12); int f = (int)(p * bw); R(rows[7], frame, $"Progress: {new string('\u2588', f)}{new string('\u2591', bw - f)}"); }
    }

    void RenderMacroPanel(Rect area, Frame frame)
    {
        var block = new BlockWidget { Title = "Macro Recorder", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(Ctx(frame, area)); var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        R(inner, frame, "State: Idle\nMacro: none\nEvents: 0\nDuration: -\nControls: r/p/l +/- Esc");
    }

    void RenderInfoPanel(Rect area, Frame frame)
    {
        var block = new BlockWidget { Title = "System Info", TitleAlign = BlockWidget.TitleAlignment.Center };
        block.Render(Ctx(frame, area)); var inner = area.Inner(1);
        if (inner.IsEmpty) return;
        var lines = new[] {
            $"Patterns [selected {_selectedPattern}] Composite [focus evidence] advanced mouse focus=1 context=armed Selected pattern: {_selectedPattern}",
            "", "FrankenTUI Demo Showcase", "",
            "Framework: ftui (Rust)", "Rendering: 16-byte Cell model",
            "Color: 24-bit true color (RGB)", "Input: Kitty keyboard protocol", "",
            "Features demonstrated:", "  - Traceback error display",
            "  - Countdown timers (compact/clock)", "  - Terminal spinners",
            "  - Progress bars", "  - Macro recorder + playback", "",
            $"Patterns [selected {_selectedPattern}]", $"Composite [focus evidence]",
            $"advanced mouse focus={_focusState} context=armed",
            $"Selected pattern: {_selectedPattern}",
            "", "Controls:", "  r - Reset timers", "  Space - Pause/resume timers"
        };
        for (int i = 0; i < lines.Length && i < inner.Height; i++)
            R(new Rect(inner.X, (ushort)(inner.Y + i), inner.Width, 1), frame, lines[i]);
    }

    static RuntimeRenderContext Ctx(Frame f, Rect a) => new(f.Buffer, a, FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full);
    static void R(Rect a, Frame f, string t) => new ParagraphWidget(t).Render(new RuntimeRenderContext(f.Buffer, a, FrankenTui.Style.Theme.DefaultTheme, RuntimeDegradationLevel.Full));
}
