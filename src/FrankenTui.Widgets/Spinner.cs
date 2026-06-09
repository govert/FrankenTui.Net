// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/spinner.rs (550L)
// Animated spinner widget with configurable frames.

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

public sealed class Spinner : IWidget
{
    string[] _frames; int _tick; WidgetStyle _style;

    public Spinner() { _frames = new[]{"⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"}; _tick = 0; }

    public Spinner Frames(string[] f) { _frames = f; return this; }
    public Spinner Tick(int t) { _tick = t; return this; }
    public Spinner Style(WidgetStyle s) { _style = s; return this; }
    public void Advance() => _tick = (_tick + 1) % _frames.Length;

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        var frame_ = _frames[Math.Abs(_tick) % _frames.Length];
        WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, frame_, _style, area.Right);
    }
}
