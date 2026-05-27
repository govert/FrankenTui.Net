namespace FrankenTui.Layout;

public readonly record struct LayoutConstraint(LayoutConstraintKind Kind, ushort Value)
{
    public static LayoutConstraint Fixed(ushort value) => new(LayoutConstraintKind.Fixed, value);

    public static LayoutConstraint Minimum(ushort value) => new(LayoutConstraintKind.Minimum, value);

    public static LayoutConstraint Fill(ushort weight = 1) => new(LayoutConstraintKind.Fill, Math.Max((ushort)1, weight));

    /// <summary>Non-greedy fill: takes minimum then shares remaining space.
    /// Matches Rust Flex Constraint::Min(N).</summary>
    public static LayoutConstraint MinFill(ushort min = 1) => new(LayoutConstraintKind.MinFill, Math.Max((ushort)1, min));

    public static LayoutConstraint Percentage(ushort value) => new(LayoutConstraintKind.Percentage, value);
}
