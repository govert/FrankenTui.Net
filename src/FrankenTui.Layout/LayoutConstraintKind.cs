namespace FrankenTui.Layout;

public enum LayoutConstraintKind
{
    Fixed,
    Minimum,
    Fill,
    Percentage,
    /// <summary>Like Fill but non-greedy: takes minimum then shares remaining
    /// proportionally with other MinFill constraints. Matches Rust Flex Min.</summary>
    MinFill
}
