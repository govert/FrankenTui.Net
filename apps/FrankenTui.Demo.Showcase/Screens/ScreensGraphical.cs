using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Screen 45: Quake E1M1 — wireframe raycaster via CanvasPainter.</summary>
internal static class Screen45Quake
{
    public static IWidget Build(ShowcaseDemoState state) => new QuakeRenderer();

    private sealed class QuakeRenderer : IWidget
    {
        private readonly CanvasPainter _painter = new(96, 48, CanvasMode.Braille);

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Controls bar
            var controlsY = area.Y;
            BufferPainter.WriteText(context.Buffer, area.X, controlsY,
                "WASD move · Arrows look · Space jump · F fire · V quality [Reduced] · R reset",
                Cell.FromChar(' '));

            // Canvas area
            var canvasArea = new Rect(area.X, (ushort)(area.Y + 1),
                area.Width, (ushort)(area.Height - 1));
            if (canvasArea.IsEmpty) return;

            _painter.Clear();

            // Render wireframe: draw E1M1 outline using concentric shapes
            var cx = 48; var cy = 24; // center

            // Outer walls
            _painter.Line(cx - 40, cy - 20, cx + 40, cy - 20);
            _painter.Line(cx + 40, cy - 20, cx + 40, cy + 20);
            _painter.Line(cx + 40, cy + 20, cx - 40, cy + 20);
            _painter.Line(cx - 40, cy + 20, cx - 40, cy - 20);

            // Inner room dividers
            _painter.Line(cx - 20, cy - 20, cx - 20, cy);
            _painter.Line(cx, cy - 10, cx, cy + 10);
            _painter.Line(cx + 15, cy - 15, cx + 15, cy + 5);
            _painter.Line(cx - 40, cy, cx - 20, cy);
            _painter.Line(cx + 20, cy - 20, cx + 20, cy + 20);

            // Slipgate entrance
            _painter.Rect(cx - 5, cy - 5, 10, 10);

            // Window/opening
            _painter.Line(cx - 35, cy + 5, cx - 25, cy + 5);
            _painter.Line(cx - 35, cy + 15, cx - 25, cy + 15);
            _painter.Line(cx - 35, cy + 5, cx - 35, cy + 15);
            _painter.Line(cx - 25, cy + 5, cx - 25, cy + 15);

            // Stairs/platforms
            for (var i = 0; i < 8; i++)
                _painter.Line(cx + 25 + i * 2, cy + 20, cx + 25 + i * 2, cy + 15 - i);

            // Crosshair
            _painter.Line(cx - 4, cy, cx - 2, cy);
            _painter.Line(cx + 2, cy, cx + 4, cy);
            _painter.Line(cx, cy - 4, cx, cy - 2);
            _painter.Line(cx, cy + 2, cx, cy + 4);

            // Render braille
            _painter.Render(canvasArea, context.Buffer, Cell.FromChar(' '));
        }
    }
}

/// <summary>Screen 6: Layout Lab — pane studio with drag handles.</summary>
internal static class Screen06LayoutLab
{
    public static IWidget Build(ShowcaseDemoState state) => new LayoutLabRenderer();

    private sealed class LayoutLabRenderer : IWidget
    {
        private readonly CanvasPainter _painter = new(80, 40, CanvasMode.Braille);

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Title and preset bar
            BufferPainter.WriteText(context.Buffer, area.X, area.Y,
                "Layout + Pane Studio  |  Preset: Flex Vertical  |  n/p: cycle presets  |  [/]: step",
                Cell.FromChar(' '));

            var canvasArea = new Rect(area.X, (ushort)(area.Y + 1), area.Width, (ushort)(area.Height - 2));
            if (canvasArea.IsEmpty) return;

            _painter.Clear();

            // Render pane layout: 3 panes
            // Fixed pane (top)
            _painter.Rect(3, 4, 60, 5);
            _painter.Line(3, 6, 63, 6); // center line

            // Percentage pane (middle)
            _painter.Rect(3, 9, 60, 3);

            // Min pane (bottom)
            _painter.Rect(3, 12, 60, 3);

            // Right sidebar with mode info
            _painter.Rect(66, 4, 12, 15);
            for (var i = 0; i < 4; i++)
                _painter.Line(66, 7 + i * 3, 78, 7 + i * 3);

            // Footer
            BufferPainter.WriteText(context.Buffer, area.X, (ushort)(area.Y + area.Height - 1),
                "FlexRoot req=44x15 got=44x15 min=0x0 | Fixed=44x3 | Min=44x4 | Max=44x6",
                Cell.FromChar(' '));

            _painter.Render(canvasArea, context.Buffer, Cell.FromChar(' '));
        }
    }
}

/// <summary>Screen 5: Widget Gallery — multi-pane widget demo.</summary>
internal static class Screen05WidgetGallery
{
    public static IWidget Build(ShowcaseDemoState state) => new WidgetGalleryRenderer();

    private sealed class WidgetGalleryRenderer : IWidget
    {
        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Category tabs
            BufferPainter.WriteText(context.Buffer, area.X, area.Y,
                "▸ A: Inputs │ B: Display │ C: Status │ D: Data Viz │ E: Navigation │ F: Misc",
                Cell.FromChar(' '));

            var inner = new Rect(area.X, (ushort)(area.Y + 1), area.Width, (ushort)(area.Height - 1));
            if (inner.IsEmpty) return;

            // Render inline using simple panels
            var text = string.Join("\n",
                "╭─ TextInput ───────────╮  ╭─ Masked Input ─╮  ╭─ Command Palette ────────╮",
                "│ user@example.com      │  │ ●●●●●●●●       │  │ >Type to search...       │",
                "│                       │  │                │  │ > [App] Quit    Exit      │",
                "╰───────────────────────╯  ╰────────────────╯  │   [File] Open File       │",
                "                                               ╰──────────────────────────╯",
                "╭─ TextArea ──────────────────────────────────────────────────────────────╮",
                "│ 1  FrankenTUI text area                                                  │",
                "│ 2  - Multi-line editing                                                  │",
                "│ 3  - Soft wrap enabled                                                   │",
                "╰──────────────────────────────────────────────────────────────────────────╯");

            for (var i = 0; i < text.Split('\n').Length && i < inner.Height; i++)
            {
                var line = text.Split('\n')[i];
                var y = (ushort)(inner.Y + i);
                for (var x = 0; x < line.Length && x < inner.Width; x++)
                    context.Buffer.Set((ushort)(inner.X + x), y, Cell.FromChar(line[x]));
            }
        }
    }
}

/// <summary>Screen 18: Visual Effects — metaballs/plasma via CanvasPainter.</summary>
internal static class Screen18VisualEffects
{
    public static IWidget Build(ShowcaseDemoState state) => new VfxRenderer();

    private sealed class VfxRenderer : IWidget
    {
        private readonly CanvasPainter _painter = new(140, 36, CanvasMode.Braille);

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Header
            BufferPainter.WriteText(context.Buffer, area.X, area.Y,
                "⬤ Metaballs │ ←/→ Switch │ [t] Text FX │ FPS │ Frame time",
                Cell.FromChar(' '));

            var canvasArea = new Rect(area.X, (ushort)(area.Y + 1), area.Width, (ushort)(area.Height - 1));
            if (canvasArea.IsEmpty) return;

            _painter.Clear();

            // Render metaball-like shapes
            var w = canvasArea.Width * 2;
            var h = canvasArea.Height * 4;
            var t = (Environment.TickCount64 / 50) % 1000;
            var balls = new[] { (0.3, 0.4), (0.6, 0.3), (0.5, 0.6), (0.35, 0.55), (0.65, 0.5) };
            foreach (var (bx, by) in balls)
            {
                var cx = (int)(bx * w + Math.Sin(t * 0.001 + bx * 10) * 20);
                var cy = (int)(by * h + Math.Cos(t * 0.001 + by * 8) * 20);
                _painter.Circle(cx, cy, 8 + (int)(Math.Sin(t * 0.002) * 3));
                _painter.Circle(cx - 2, cy - 2, 5);
            }

            _painter.Render(canvasArea, context.Buffer, Cell.FromChar(' '));
        }
    }
}

/// <summary>Screen 8: Data Viz — sparklines and charts.</summary>
internal static class Screen08DataViz
{
    public static IWidget Build(ShowcaseDemoState state) => new DataVizRenderer();

    private sealed class DataVizRenderer : IWidget
    {
        private readonly CanvasPainter _painter = new(160, 32, CanvasMode.Braille);

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;
            _painter.Clear();

            // Sparklines
            for (var row = 0; row < 3; row++)
            {
                var y = 4 + row * 8;
                var seed = row * 42;
                for (var x = 0; x < 140; x++)
                {
                    var val = (int)(Math.Sin((x + seed) * 0.1) * 4 + Math.Sin((x + seed) * 0.03) * 2 + 4);
                    _painter.Point(x, y + val);
                }
                _painter.Line(0, y + 4, 140, y + 4); // baseline
            }

            // Bar chart area
            for (var x = 0; x < 20; x++)
            {
                var height = (int)(Math.Abs(Math.Sin(x * 0.5)) * 16) + 2;
                _painter.Rect(70 + x * 3, 30 - height, 2, height);
            }

            _painter.Render(area, context.Buffer, Cell.FromChar(' '));
        }
    }
}

/// <summary>Screen 16: Mermaid Showcase — diagram layout.</summary>
internal static class Screen16Mermaid
{
    public static IWidget Build(ShowcaseDemoState state) => new MermaidRenderer();

    private sealed class MermaidRenderer : IWidget
    {
        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            var diagram = string.Join("\n",
                "┌──────┐     ┌─────────┐",
                "│ Start ├────>│ Decision │",
                "└──────┘     └────┬────┘",
                "                  │",
                "            ┌─────┴─────┐",
                "            │            │",
                "            ▼            ▼",
                "       ┌────────┐  ┌────────┐",
                "       │Process │  │  End   │",
                "       └────────┘  └────────┘");

            for (var i = 0; i < diagram.Split('\n').Length && i < area.Height; i++)
            {
                var line = diagram.Split('\n')[i];
                var y = (ushort)(area.Y + i);
                for (var x = 0; x < line.Length && x < area.Width; x++)
                    context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(line[x]));
            }
        }
    }
}

/// <summary>Screen 4: Code Explorer — file tree + code view.</summary>
internal static class Screen04CodeExplorer
{
    public static IWidget Build(ShowcaseDemoState state) => new CodeExplorerRenderer();

    private sealed class CodeExplorerRenderer : IWidget
    {
        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            var code = string.Join("\n",
                " 1 │ /*****************************************************",
                " 2 │ ** This file is an amalgamation of many separate C",
                " 3 │ ** source files from SQLite version 3.48.0.",
                " 4 │ ** By combining all individual C code files into",
                " 5 │ ** a single large file, the entire code can be",
                " 6 │ ** compiled as a single translation unit.",
                " 7 │ ** This allows many compilers to do optimizations",
                " 8 │ ** that would not be possible if files were",
                " 9 │ ** compiled separately.",
                "10 │ ** Performance improvements of 5% or more are",
                "11 │ ** commonly seen with single-unit compilation.",
                "12 │ *****************************************************/");

            for (var i = 0; i < code.Split('\n').Length && i < area.Height; i++)
            {
                var line = code.Split('\n')[i];
                var y = (ushort)(area.Y + i);
                for (var x = 0; x < line.Length && x < area.Width; x++)
                    context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(line[x]));
            }
        }
    }
}

/// <summary>Screen 1: Guided Tour — dashboard composite.</summary>
internal static class Screen01GuidedTour
{
    public static IWidget Build(ShowcaseDemoState state) => new GuidedTourRenderer();

    private sealed class GuidedTourRenderer : IWidget
    {
        private readonly CanvasPainter _painter = new(140, 40, CanvasMode.Braille);

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Title
            BufferPainter.WriteText(context.Buffer, area.X, area.Y,
                "FRANKENTUI DASHBOARD  |  1/16 · Tour · LIVE  |  Speed 1.00x",
                Cell.FromChar(' '));

            var canvasArea = new Rect(area.X, (ushort)(area.Y + 1), (ushort)(area.Width * 3 / 5), (ushort)(area.Height - 2));
            if (!canvasArea.IsEmpty)
            {
                _painter.Clear();
                // Plasma effect
                var t = Environment.TickCount64;
                for (var y = 0; y < canvasArea.Height * 4; y++)
                    for (var x = 0; x < canvasArea.Width * 2; x++)
                    {
                        var v = Math.Sin(x * 0.05 + t * 0.001) + Math.Sin(y * 0.05 + t * 0.001) +
                                Math.Sin((x + y) * 0.03) + Math.Sin(Math.Sqrt(x * x + y * y) * 0.02);
                        if (v > 1.0) _painter.Point(x, y);
                    }
                _painter.Render(canvasArea, context.Buffer, Cell.FromChar(' '));
            }

            // Right panel: stats
            var statsX = (ushort)(area.X + area.Width * 3 / 5);
            var stats = string.Join("\n",
                "Charts · Pulse", "CPU ▃▃▂▂▁▁▄▃▄▂ 38%",
                "MEM ▅▂▃▁▄▄▂▂▇▅ 41%",
                "NET ▁▁▁▁▁ ▁ ▇▁▁ 125",
                "OUT ▁▁▁▃▁█ ▁▁ 134",
                "EPS █ 74% DSK █ 66%");
            for (var i = 0; i < stats.Split('\n').Length && i + 1 < area.Height; i++)
            {
                var line = stats.Split('\n')[i];
                var y = (ushort)(area.Y + 1 + i);
                for (var x = 0; x < line.Length && statsX + x < area.X + area.Width; x++)
                    context.Buffer.Set((ushort)(statsX + x), y, Cell.FromChar(line[x]));
            }
        }
    }
}
