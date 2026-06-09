// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/columns.rs (507L)
// Horizontal column layout container using Flex constraints.

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ── Minimized Constraint / Flex from ftui_layout ──────────────────────────

public readonly record struct Constraint
{
    public enum Kind : byte { Fill, Fixed, Ratio, Min }
    public Kind Type { get; }
    public ushort Value { get; }
    public ushort Value2 { get; }
    Constraint(Kind k, ushort v = 0, ushort v2 = 0) => (Type, Value, Value2) = (k, v, v2);
    public static Constraint Fill => new(Kind.Fill);
    public static Constraint Fixed(ushort w) => new(Kind.Fixed, w);
    public static Constraint Ratio(ushort num, ushort den) => new(Kind.Ratio, num, den);
    public static Constraint Min(ushort w) => new(Kind.Min, w);
}

static class Flex
{
    public static Rect[] HorizontalSplit(Rect area, Constraint[] constraints, ushort gap)
    {
        int n = constraints.Length;
        if (n == 0) return Array.Empty<Rect>();
        var rects = new Rect[n];

        // First pass: fixed and min
        ushort used = 0;
        int fillCount = 0;
        for (int i = 0; i < n; i++)
        {
            if (constraints[i].Type == Constraint.Kind.Fixed || constraints[i].Type == Constraint.Kind.Min)
            {
                ushort w = constraints[i].Value;
                rects[i] = new Rect(0, area.Y, w, area.Height);
                used = (ushort)(used + w);
            }
            else if (constraints[i].Type == Constraint.Kind.Fill)
                fillCount++;
        }
        if (n > 1) used = (ushort)(used + (n - 1) * gap);

        ushort remaining = used < area.Width ? (ushort)(area.Width - used) : (ushort)0;
        if (fillCount == 0 && constraints.Any(c => c.Type == Constraint.Kind.Ratio))
        {
            int totalDen = constraints.Where(c => c.Type == Constraint.Kind.Ratio).Sum(c => c.Value2);
            for (int i = 0; i < n; i++)
            {
                if (constraints[i].Type != Constraint.Kind.Ratio) continue;
                rects[i] = new Rect(0, area.Y, (ushort)(remaining * constraints[i].Value / Math.Max(1, totalDen)), area.Height);
            }
        }
        else if (fillCount > 0)
        {
            ushort perFill = (ushort)(remaining / (ushort)fillCount);
            for (int i = 0; i < n; i++)
                if (constraints[i].Type == Constraint.Kind.Fill)
                    rects[i] = new Rect(0, area.Y, perFill, area.Height);
        }
        else
        {
            ushort perCol = (ushort)(remaining / (ushort)n);
            for (int i = 0; i < n; i++)
                if (rects[i].Width == 0)
                    rects[i] = new Rect(0, area.Y, perCol, area.Height);
        }

        // Lay out x positions
        ushort cx = area.X;
        for (int i = 0; i < n; i++)
        {
            rects[i] = rects[i] with { X = cx };
            cx = (ushort)(cx + rects[i].Width + (i < n - 1 ? gap : (ushort)0));
        }
        return rects;
    }
}

// ── Column ────────────────────────────────────────────────────────────────

public sealed class Column
{
    IWidget _widget; Constraint _constraint; Sides _padding;
    public IWidget Widget => _widget;
    public Constraint Constraint_ => _constraint;

    public Column(IWidget widget, Constraint c) { _widget = widget; _constraint = c; }
    public Column Padding(Sides p) { _padding = p; return this; }
    public Column Constraint(Constraint c) { _constraint = c; return this; }
    internal Sides Padding_ => _padding;
}

// ── Columns ───────────────────────────────────────────────────────────────

public sealed class Columns : IWidget
{
    List<Column> _cols = new(); ushort _gap;

    public Columns() { }
    public Columns Gap(ushort g) { _gap = g; return this; }
    public Columns Push(Column c) { _cols.Add(c); return this; }
    public Columns Column(IWidget w, Constraint c) { _cols.Add(new Column(w, c)); return this; }
    public Columns Add(IWidget w) { _cols.Add(new Column(w, Constraint.Fill)); return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        // Clear stale children
        for (ushort y = area.Y; y < area.Bottom; y++)
            for (ushort x = area.X; x < area.Right; x++)
                frame.Buffer.Set(x, y, Cell.Empty);

        if (_cols.Count == 0) return;

        var constraints = _cols.Select(c => c.Constraint_).ToArray();
        var rects = Flex.HorizontalSplit(area, constraints, _gap);

        for (int i = 0; i < _cols.Count && i < rects.Length; i++)
        {
            var rect = rects[i];
            if (rect.Width == 0 || rect.Height == 0) continue;
            var inner = rect.Inner(_cols[i].Padding_);
            if (inner.Width == 0 || inner.Height == 0) continue;
            frame.Buffer.PushScissor(inner);
            _cols[i].Widget.Render(inner, frame);
            frame.Buffer.PopScissor();
        }
    }

    public bool IsEssential() => _cols.Any(c => c.Widget.IsEssential());
}
