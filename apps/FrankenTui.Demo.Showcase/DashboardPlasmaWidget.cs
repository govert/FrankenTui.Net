// Upstream source: .external/frankentui/crates/ftui-demo-showcase/src/screens/dashboard.rs
//   fn render_plasma()
// Faithful port of the dashboard plasma canvas with accent gradient colors.

using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

internal sealed class DashboardPlasmaWidget : IWidget
{
    private readonly double _t;

    public DashboardPlasmaWidget(double t) { _t = t; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty || area.Width < 2 || area.Height < 2) return;

        int pw = area.Width * CanvasPixelRect.ColsPerCell(CanvasMode.Braille);
        int ph = area.Height * CanvasPixelRect.RowsPerCell(CanvasMode.Braille);
        var painter = new CanvasPainter((ushort)pw, (ushort)ph);

        double time = _t * 0.5;
        double hue = (time * 0.07) % 1.0;
        if (hue < 0) hue += 1.0;

        for (int py = 0; py < ph; py++)
        {
            for (int px = 0; px < pw; px++)
            {
                double x = (double)px / pw, y = (double)py / ph;
                double v = (Math.Sin(x * 10.0 + time * 2.0) +
                            Math.Sin(y * 10.0 + time * 1.5) +
                            Math.Sin((x + y) * 8.0 + time)) / 3.0;
                double n = (v + 1.0) * 0.5 + hue;
                painter.PointColored(px, py, ThemeSystem.AccentGradient(n));
            }
        }

        var ctx = new RuntimeRenderContext(frame.Buffer, area,
            Style.Theme.DefaultTheme,
            RuntimeDegradationLevel.Full);
        new CanvasWidget
        {
            Painter = painter,
            Template = Cell.FromChar(' ').WithForeground(ThemePalette.CurrentPalette.FgPrimary)
        }.Render(ctx);
    }
}
