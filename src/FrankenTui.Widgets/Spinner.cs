// Upstream source: .external/frankentui/crates/ftui-widgets/src/spinner.rs
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Faithful 1-1 port including SpinnerState.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Text;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

/// <summary>Braille dot spinner animation frames. Port of DOTS constant.</summary>
public static class SpinnerFrames
{
    public static readonly string[] Dots = ["\u280B", "\u2819", "\u2839", "\u2838", "\u283C", "\u2834", "\u2826", "\u2827", "\u2807", "\u280F"];
    public static readonly string[] Line = ["|", "/", "-", "\\"];
}

/// <summary>Mutable state for a Spinner widget. Port of SpinnerState.</summary>
public sealed class SpinnerState
{
    /// <summary>Index of the currently displayed animation frame.</summary>
    public int CurrentFrame { get; set; }

    /// <summary>Advance to the next animation frame.</summary>
    public void Tick() { CurrentFrame = CurrentFrame + 1; }
}

/// <summary>Animated spinner widget. Port of Spinner struct (stateful).</summary>
public sealed class Spinner : IStatefulWidget<SpinnerState>, IWidget, IAccessible, CanonicalA11y.IAccessible
{
    private Block? _block;
    private WidgetStyle _style;
    private string[] _frames;
    private string? _label;

    public Spinner()
    {
        _style = default;
        _frames = SpinnerFrames.Dots;
        _label = null;
    }

    public Spinner WithBlock(Block block) { _block = block; return this; }
    public Spinner WithStyle(WidgetStyle style) { _style = style; return this; }
    public Spinner WithFrames(string[] frames) { _frames = frames; return this; }
    public Spinner WithLabel(string? label) { _label = label; return this; }

    // ── frame_for_render ──────────────────────────────────────────────
    string? FrameForRender(int currentFrame, bool useUnicode)
    {
        if (_frames.Length == 0) return null;
        int idx = currentFrame % _frames.Length;
        if (useUnicode) return _frames[idx];
        var candidate = _frames[idx];
        return candidate.All(char.IsAscii) ? candidate : _frames.FirstOrDefault(f => f.All(char.IsAscii)) ?? "*";
    }

    // ── StatefulWidget::render ────────────────────────────────────────
    public void Render(Rect area, Frame frame, SpinnerState state)
    {
        var deg = frame.Degradation;

        // Skeleton+: skip entirely
        if (!deg.RenderContent()) { WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default); return; }

        // EssentialOnly: only show label text
        if (!deg.RenderDecorative())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            if (_label is { } l)
                WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, l, WidgetStyle.Default, area.Right);
            return;
        }

        var style = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, area, style);

        Rect sa;
        if (_block is { } bk)
        {
            bk.Render(area, frame);
            sa = bk.Inner(area);
        }
        else sa = area;

        if (sa.IsEmpty) return;

        ushort x = sa.X;
        ushort y = sa.Y;
        if (FrameForRender(state.CurrentFrame, deg.UseUnicodeBorders()) is { } frameChar)
        {
            WidgetDrawing.DrawTextSpan(frame, x, y, frameChar, style, sa.Right);
            x += (ushort)TerminalTextWidth.DisplayWidth(frameChar);
        }

        // Render label
        if (_label is { } label)
        {
            if (x > sa.X) x += 1;
            if (x < sa.Right)
                WidgetDrawing.DrawTextSpan(frame, x, y, label, style, sa.Right);
        }
    }

    // ── IWidget ───────────────────────────────────────────────────────
    // Widget::render creates default state and delegates
    public void Render(Rect area, Frame frame)
    {
        var state = new SpinnerState();
        Render(area, frame, state);
    }

    // ── Accessibility ─────────────────────────────────────────────────────────

    /// <summary>Get the legacy compatibility projection of this spinner's accessibility node.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        string name = _label is { } label ? $"Loading: {label}" : "Loading...";
        CanonicalA11y.A11yNodeInfo node = CanonicalA11y.A11yNodeInfo
            .New(WidgetDrawing.A11yNodeId(area), CanonicalA11y.A11yRole.ProgressBar, area)
            .WithName(name)
            .WithState(new CanonicalA11y.A11yState { Busy = true });
        return [node];
    }
}
