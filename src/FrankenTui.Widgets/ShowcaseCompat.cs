// Compatibility shims for the showcase app's legacy widget API.
// These wrap the new fluent-style widgets with the old property-init pattern.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Text;
using System.Text;

namespace FrankenTui.Widgets;

// ── TabsWidget ────────────────────────────────────────────────────────────────

/// <summary>Legacy compatibility wrapper for the showcase. Maps property-init to new Tabs API.</summary>
public sealed class TabsWidget : IWidget
{
    public IReadOnlyList<string> Tabs { get; init; } = [];
    public int SelectedIndex { get; init; }
    public int FocusedIndex { get; init; } = -1;
    public int HoveredIndex { get; init; } = -1;

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        var deg = frame.Degradation;
        var buffer = frame.Buffer;

        // Always clear the owned row first so shorter rerenders do not leave
        // stale trailing labels (WidgetClearContract).
        WidgetDrawing.ClearTextRow(frame, area, WidgetStyle.Default);

        // Skeleton+: content not rendered.
        if (!deg.RenderContent() || Tabs.Count == 0)
            return;

        bool applyStyling = deg.ApplyStyling();
        ushort x = area.X;
        for (var i = 0; i < Tabs.Count && x < area.Right; i++)
        {
            // Two-space separator between tabs (matches the upstream compact
            // tab strip contract: active `[label]`, inactive `label`).
            if (i > 0)
            {
                for (var s = 0; s < 2 && x < area.Right; s++)
                    buffer.SetFast(x++, area.Y, Cell.FromChar(' '));
            }

            var tab = Tabs[i];
            // Compact bracket form: `[One]` for the active tab, bare `One` otherwise.
            var label = i == SelectedIndex ? $"[{tab}]" : tab;

            // Focus/hover/selection styling (faithful to the legacy TabsWidget).
            var style = applyStyling
                ? i switch
                {
                    _ when i == SelectedIndex && i == FocusedIndex => UiStyle.Accent,
                    _ when i == SelectedIndex => UiStyle.Accent,
                    _ when i == FocusedIndex => UiStyle.Accent,
                    _ when i == HoveredIndex => UiStyle.Muted,
                    _ => UiStyle.Default,
                }
                : UiStyle.Default;

            foreach (var c in label)
            {
                if (x >= area.Right) break;
                buffer.SetFast(x++, area.Y, style.ToCell(c));
            }
        }
    }
}

// ── TreeWidget ────────────────────────────────────────────────────────────────

/// <summary>Legacy compatibility wrapper for the showcase.</summary>
public sealed class TreeWidget : IWidget
{
    public IReadOnlyList<TreeNode>? Nodes { get; init; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        var deg = frame.Degradation;

        // Always clear the owned area so shorter/stale renders do not leak.
        WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);

        // Skeleton+: content not rendered.
        if (!deg.RenderContent() || Nodes is null)
            return;

        // Branch glyph: Unicode triangle at Full, ASCII `+` at SimpleBorders/NoStyling,
        // none at EssentialOnly (decorative chrome stripped).
        string branch = deg.RenderDecorative()
            ? (deg.UseUnicodeBorders() ? "▸ " : "+ ")
            : "";
        bool indent = deg.RenderDecorative();

        var buffer = frame.Buffer;
        ushort row = area.Y;
        foreach (var node in Nodes)
        {
            if (row >= area.Bottom) return;
            RenderNode(buffer, node, area, ref row, 0, branch, indent);
        }
    }

    private static void RenderNode(
        FrankenTui.Render.Buffer buffer, TreeNode node, Rect area, ref ushort row,
        int depth, string branch, bool indent)
    {
        if (row >= area.Bottom) return;
        var prefix = indent ? new string(' ', depth * 2) : "";
        var line = prefix + branch + node.Label;
        ushort x = area.X;
        foreach (var c in line)
        {
            if (x >= area.Right) break;
            buffer.SetFast(x++, row, Cell.FromChar(c));
        }
        row++;
        foreach (var child in node.Children())
        {
            if (row >= area.Bottom) return;
            RenderNode(buffer, child, area, ref row, depth + 1, branch, indent);
        }
    }
}

// ── TreeNode 2-arg constructor compat ─────────────────────────────────────────

// TreeNode already has a public single-arg constructor. We extend it with a static factory
// via partial-class or extension. Since it's sealed, add compat constructor via subclass approach
// is not possible. Instead we add the 2-arg constructor directly to a new compat class and
// rename usages - but we can't change the callers. Instead we add a secondary constructor
// to TreeNode via extension methods won't work for constructors.
// Best approach: just add a public 2-arg constructor to the existing TreeNode class isn't
// possible without modifying Tree.cs. So instead, create a compatibility overload here.

// ── TextAreaWidget ────────────────────────────────────────────────────────────

/// <summary>
/// Legacy compatibility wrapper for the showcase. Restores the full render
/// contract of the original TextAreaWidget.cs (deleted during the TextArea port):
/// ClearTextArea/ClearTextRow clear-contract, placeholder rendering, cursor
/// rendering (HasFocus + Cursor), RenderOptions, and degradation tiers.
/// </summary>
public sealed class TextAreaWidget : IWidget
{
    public TextDocument? Document { get; init; }
    public TextCursor? Cursor { get; init; }
    public bool HasFocus { get; init; }
    public string? PlaceholderText { get; init; }
    public string? StatusText { get; init; }
    public TextRenderOptions? RenderOptions { get; init; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        var deg = frame.Degradation;

        // Skeleton+: clear the owned area and skip all content.
        if (!deg.RenderContent())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            return;
        }

        // Clear the full area first so shorter rerenders do not leave stale cells.
        WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);

        // Determine whether placeholder text should be used.
        var doc = Document;
        bool usingPlaceholder = (doc is null || doc.Lines.Count == 0) &&
                                 !string.IsNullOrWhiteSpace(PlaceholderText);
        if (usingPlaceholder)
            doc = TextDocument.FromString(PlaceholderText!);

        if (doc is not null)
        {
            // Placeholder is rendered in muted style when styling is active;
            // otherwise use default style.  Normal content always uses default.
            bool applyStyling = deg.ApplyStyling();
            UiStyle contentStyle = (usingPlaceholder && applyStyling)
                ? UiStyle.Muted
                : UiStyle.Default;

            var options = RenderOptions ?? new TextRenderOptions(TextWrapMode.Character);
            var lines = TextRenderer.Layout(doc, area.Width, options);

            // Reserve the last row for StatusText when present.
            int contentRows = string.IsNullOrEmpty(StatusText)
                ? area.Height
                : Math.Max(area.Height - 1, 0);

            for (var row = 0; row < Math.Min(lines.Count, contentRows); row++)
            {
                WidgetDrawing.ClearTextRow(frame, new Rect(area.X, (ushort)(area.Y + row), area.Width, 1), WidgetStyle.Default);
                TextRenderer.Write(frame.Buffer, area.X, (ushort)(area.Y + row), lines[row], contentStyle);
            }

            // Cursor: only shown when focused, styling is active or degraded,
            // and the cursor position is within the rendered content area.
            if (HasFocus && Cursor is { } cur &&
                cur.Line < contentRows && cur.Column < area.Width)
            {
                // In NoStyling tier use plain '|'; in Full/SimpleBorders use block cursor '▌'.
                char cursorChar = applyStyling ? '▌' : '|';
                var cursorStyle = applyStyling ? UiStyle.Accent : UiStyle.Default;
                frame.Buffer.Set(
                    (ushort)(area.X + cur.Column),
                    (ushort)(area.Y + cur.Line),
                    cursorStyle.ToCell(cursorChar));
            }
        }

        // Status bar: last row of the area.
        if (!string.IsNullOrEmpty(StatusText) && area.Height > 0)
        {
            bool applyStyling = deg.ApplyStyling();
            UiStyle statusStyle = applyStyling ? UiStyle.Muted : UiStyle.Default;
            ushort statusY = (ushort)(area.Bottom - 1);
            WidgetDrawing.ClearTextRow(frame, new Rect(area.X, statusY, area.Width, 1), WidgetStyle.Default);
            WidgetDrawing.DrawTextSpan(frame, area.X, statusY, StatusText, WidgetStyle.Default, area.Right);
        }
    }
}

// ── ProgressWidget ────────────────────────────────────────────────────────────

/// <summary>Legacy compatibility wrapper for the showcase.
/// Renders the tiered progress fallbacks: bracket-bar `[#####     ]` while
/// styling is stripped, percentage text `50%` at EssentialOnly, and nothing
/// at Skeleton+.</summary>
public sealed class ProgressWidget : IWidget
{
    public double Value { get; init; }
    public string? Label { get; init; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        var deg = frame.Degradation;

        // Skeleton+: clear and skip.
        if (!deg.RenderContent())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            return;
        }

        // EssentialOnly: percentage text only.
        if (!deg.RenderDecorative())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            var pct = $"{(byte)(Math.Clamp(Value, 0.0, 1.0) * 100.0)}%";
            WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, pct, WidgetStyle.Default, area.Right);
            return;
        }

        // NoStyling (and below): ASCII bracket bar with `#` fill.
        if (!deg.ApplyStyling())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            int inner = Math.Max(0, area.Width - 2);
            int filled = (int)Math.Round(Math.Clamp(Value, 0.0, 1.0) * inner);
            if (filled > inner) filled = inner;
            var sb = new StringBuilder(area.Width);
            sb.Append('[');
            sb.Append('#', filled);
            sb.Append(' ', inner - filled);
            sb.Append(']');
            WidgetDrawing.DrawTextSpan(frame, area.X, area.Y, sb.ToString(), WidgetStyle.Default, area.Right);
            return;
        }

        // Full styling: delegate to the real ProgressBar.
        new ProgressBar().Ratio(Value).Label(Label).Render(area, frame);
    }
}
