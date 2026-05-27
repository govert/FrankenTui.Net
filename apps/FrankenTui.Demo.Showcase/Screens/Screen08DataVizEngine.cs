using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

/// <summary>Screen 8: Data Viz — sparklines, bar charts, canvas. Ported from data_viz.rs.</summary>
internal static class Screen08DataVizPort
{
    public static IWidget Build(ShowcaseDemoState state) => new DataVizWidget();

    private sealed class DataVizWidget : IWidget
    {
        readonly CanvasPainter _canvasPainter = new(60, 30, CanvasMode.Braille);
        long _tick;

        void IRuntimeView.Render(RuntimeRenderContext context)
        {
            var area = context.Bounds;
            if (area.IsEmpty) return;

            // Layout: panels (fill) + status (1)
            var mainH = area.Height - 1;
            if (mainH <= 0) return;
            int rowH1 = mainH * 42 / 100, rowH2 = mainH * 38 / 100, rowH3 = mainH - rowH1 - rowH2;
            int colW = area.Width / 3;
            int colW3 = area.Width - colW * 2;

            int y = area.Y;
            var r1 = new Rect(area.X, (ushort)y, (ushort)colW, (ushort)rowH1);
            var r2 = new Rect((ushort)(area.X + colW), (ushort)y, (ushort)colW, (ushort)rowH1);
            var r3 = new Rect((ushort)(area.X + colW * 2), (ushort)y, (ushort)colW3, (ushort)rowH1);
            y += rowH1;
            var r4 = new Rect(area.X, (ushort)y, (ushort)colW, (ushort)rowH2);
            var r5 = new Rect((ushort)(area.X + colW), (ushort)y, (ushort)colW, (ushort)rowH2);
            var r6 = new Rect((ushort)(area.X + colW * 2), (ushort)y, (ushort)colW3, (ushort)rowH2);
            y += rowH2;
            var r7 = new Rect(area.X, (ushort)y, area.Width, (ushort)rowH3);

            // Row 1: Sparkline | Bar Chart | Spectrum
            RenderSparkline(context, r1);
            RenderBarChart(context, r2);
            RenderSpectrum(context, r3);

            // Row 2: Line Chart | Canvas | Heatmap
            RenderLineChart(context, r4);
            RenderCanvas(context, r5);
            RenderHeatmap(context, r6);

            // Row 3: Micro panels with status bars
            RenderStatusBar(context, r7);

            // Bottom status
            var statusY = (ushort)(area.Y + area.Height - 1);
            BufferPainter.WriteText(context.Buffer, area.X, statusY,
                $"Tick: {_tick} | Charts: 2x3 grid | Arrows: panels | Space: pause | r: reset",
                Cell.FromChar(' '));
            _tick++;
        }

        static void RenderSparkline(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            // Draw sparkline using CanvasPainter
            var p = new CanvasPainter((ushort)(area.Width * 2), (ushort)(area.Height * 4), CanvasMode.Braille);
            long t = Environment.TickCount64;
            for (int x = 0; x < area.Width * 2; x++)
            {
                double v = Math.Sin((x + t * 0.01) * 0.08) * 3 + Math.Sin(x * 0.03 + t * 0.005) * 2 + area.Height * 2;
                int y = (int)(v + area.Height * 1.5);
                if (y >= 0 && y < area.Height * 4) p.Point(x, y);
            }
            p.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }

        static void RenderBarChart(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            var p = new CanvasPainter((ushort)(area.Width * 2), (ushort)(area.Height * 4), CanvasMode.Braille);
            int barCount = 8, barW = area.Width * 2 / barCount;
            for (int i = 0; i < barCount; i++)
            {
                double v = 0.3 + Math.Abs(Math.Sin(i * 0.7 + _staticTick * 0.01)) * 0.7;
                int h = (int)(v * area.Height * 4 * 0.8);
                for (int dx = 0; dx < barW - 1; dx++)
                    for (int dy = 0; dy < h; dy++)
                        p.Point(i * barW + dx, area.Height * 4 - 1 - dy);
            }
            p.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }
        static long _staticTick => Environment.TickCount64;

        static void RenderSpectrum(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            var p = new CanvasPainter((ushort)(area.Width * 2), (ushort)(area.Height * 4), CanvasMode.Braille);
            for (int y = 0; y < area.Height * 4; y++)
            {
                double v = Math.Sin(y * 0.02 + Environment.TickCount64 * 0.003);
                int w = (int)(Math.Abs(v) * area.Width * 2 * 0.8);
                for (int x = 0; x < w; x++) p.Point(x, y);
            }
            p.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }

        static void RenderLineChart(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            var p = new CanvasPainter((ushort)(area.Width * 2), (ushort)(area.Height * 4), CanvasMode.Braille);
            long t = Environment.TickCount64;
            int prevX = 0, prevY = area.Height * 2;
            for (int x = 0; x < area.Width * 2; x++)
            {
                double v = Math.Sin((x + t * 0.005) * 0.05) * area.Height * 1.5;
                int y = (int)(area.Height * 2 + v);
                p.Line(prevX, prevY, x, y);
                prevX = x; prevY = y;
            }
            p.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }

        void RenderCanvas(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            _canvasPainter.EnsureForArea(area, CanvasMode.Braille);
            _canvasPainter.Clear();
            long t = Environment.TickCount64;
            // Lissajous curve
            int cx = _canvasPainter.Width / 2, cy = _canvasPainter.Height / 2;
            for (double a = 0; a < Math.PI * 2; a += 0.02)
            {
                double x = cx + Math.Sin(a * 3 + t * 0.002) * cx * 0.7;
                double y = cy + Math.Sin(a * 2) * cy * 0.7;
                _canvasPainter.Point((int)x, (int)y);
            }
            _canvasPainter.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }

        static void RenderHeatmap(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            var p = new CanvasPainter((ushort)(area.Width * 2), (ushort)(area.Height * 4), CanvasMode.Braille);
            long t = Environment.TickCount64;
            for (int y = 0; y < area.Height * 4; y++)
                for (int x = 0; x < area.Width * 2; x++)
                    if (Math.Sin(x * 0.1 + t * 0.001) * Math.Cos(y * 0.1) > 0.3)
                        p.Point(x, y);
            p.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }

        static void RenderStatusBar(RuntimeRenderContext ctx, Rect area)
        {
            if (area.IsEmpty) return;
            var p = new CanvasPainter((ushort)(area.Width * 2), (ushort)(area.Height * 4), CanvasMode.Braille);
            // Micro sparklines in status bar
            long t = Environment.TickCount64;
            for (int row = 0; row < 3; row++)
            {
                int baseY = row * area.Height * 4 / 3;
                for (int x = 0; x < area.Width * 2; x++)
                {
                    double v = Math.Sin((x + row * 100 + t * 0.01) * 0.05);
                    int y = (int)(baseY + area.Height / 2 + v * area.Height / 3);
                    if (y >= 0 && y < area.Height * 4) p.Point(x, y);
                }
            }
            p.Render(area, ctx.Buffer, Cell.FromChar(' '));
        }
    }
}
