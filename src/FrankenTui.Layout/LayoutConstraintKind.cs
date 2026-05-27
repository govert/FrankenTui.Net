namespace FrankenTui.Layout;

public enum LayoutConstraintKind
{
    Fixed,
    Minimum,
    Fill,
    Percentage,
    /// <summary>Size to fit content. Requires a measurer callback in the solver.
    /// Matches Rust Constraint::FitContent.</summary>
    FitContent
}
