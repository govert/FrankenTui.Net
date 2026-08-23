// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/render_context.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

namespace FrankenTui.Render;

/// <summary>Identifies the aspect of rendering described by a continuation mark.</summary>
public enum MarkKind
{
    Constraint,
    Style,
    Widget,
    Custom,
}

public static class MarkKindExtensions
{
    public static string ToDisplayString(this MarkKind kind) => kind switch
    {
        MarkKind.Constraint => "constraint",
        MarkKind.Style => "style",
        MarkKind.Widget => "widget",
        MarkKind.Custom => "custom",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}

/// <summary>Records why a widget is rendering in its current way.</summary>
public sealed record RenderMark(MarkKind Kind, string Label, string Detail)
{
    public static RenderMark Constraint(string label, string detail) =>
        new(MarkKind.Constraint, label, detail);

    public static RenderMark Style(string label, string detail) =>
        new(MarkKind.Style, label, detail);

    public static RenderMark Widget(string label) =>
        new(MarkKind.Widget, label, string.Empty);

    public static RenderMark Custom(string label, string detail) =>
        new(MarkKind.Custom, label, detail);

    public override string ToString() => string.IsNullOrEmpty(Detail)
        ? $"[{Kind.ToDisplayString()}] {Label}"
        : $"[{Kind.ToDisplayString()}] {Label}: {Detail}";
}

/// <summary>
/// Thread-local continuation-mark stack used by layout/style debugging and tracing.
/// Clear it at the beginning of every frame.
/// </summary>
public static class RenderContext
{
    [ThreadStatic]
    private static List<RenderMark>? _marks;

    private static List<RenderMark> Marks => _marks ??= [];

    public static void PushMark(RenderMark mark)
    {
        ArgumentNullException.ThrowIfNull(mark);
        Marks.Add(mark);
    }

    public static RenderMark? PopMark()
    {
        var marks = Marks;
        if (marks.Count == 0)
        {
            return null;
        }

        var index = marks.Count - 1;
        var mark = marks[index];
        marks.RemoveAt(index);
        return mark;
    }

    /// <summary>Returns a bottom-to-top snapshot independent of later stack changes.</summary>
    public static IReadOnlyList<RenderMark> CurrentMarks() => Marks.ToArray();

    public static int MarkDepth => Marks.Count;

    public static void ClearMarks() => Marks.Clear();
}

/// <summary>Pushes a mark and removes the current top mark when disposed.</summary>
public sealed class MarkGuard : IDisposable
{
    private bool _disposed;

    public MarkGuard(RenderMark mark)
    {
        RenderContext.PushMark(mark);
    }

    public static MarkGuard Constraint(string label, string detail) =>
        new(RenderMark.Constraint(label, detail));

    public static MarkGuard Style(string label, string detail) =>
        new(RenderMark.Style(label, detail));

    public static MarkGuard Widget(string label) =>
        new(RenderMark.Widget(label));

    public static MarkGuard Custom(string label, string detail) =>
        new(RenderMark.Custom(label, detail));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        RenderContext.PopMark();
    }
}
