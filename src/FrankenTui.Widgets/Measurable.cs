// Upstream source: crates/ftui-widgets/src/measurable.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of SizeConstraints and MeasurableWidget trait.

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Widgets;

/// <summary>
/// Size constraints returned by measure operations.
/// Captures the full sizing semantics for a widget:
/// - <see cref="Min"/>: Minimum usable size (content clips below this)
/// - <see cref="Preferred"/>: Ideal size for content display
/// - <see cref="Max"/>: Maximum useful size (no benefit beyond this)
/// </summary>
public readonly record struct SizeConstraints
{
    /// <summary>Minimum size below which the widget is unusable or clips content.</summary>
    public Size Min { get; init; }
    /// <summary>Preferred size that best displays content.</summary>
    public Size Preferred { get; init; }
    /// <summary>Maximum useful size. null means unbounded (widget can use all available space).</summary>
    public Size? Max { get; init; }

    /// <summary>Zero constraints (no minimum, no preferred, unbounded maximum).</summary>
    public static readonly SizeConstraints Zero = new()
    {
        Min = Size.Zero,
        Preferred = Size.Zero,
        Max = null,
    };

    /// <summary>Create constraints with exact sizing (min = preferred = max).</summary>
    public static SizeConstraints Exact(Size size) => new()
    {
        Min = size,
        Preferred = size,
        Max = size,
    };

    /// <summary>Create constraints with a minimum and preferred size, unbounded maximum.</summary>
    public static SizeConstraints AtLeast(Size min, Size preferred) => new()
    {
        Min = min,
        Preferred = preferred,
        Max = null,
    };

    /// <summary>Clamp a given size to these constraints.</summary>
    public Size Clamp(Size size)
    {
        var max = Max ?? Size.Max;

        var width = size.Width < Min.Width ? Min.Width
            : size.Width > max.Width ? max.Width
            : size.Width;

        var height = size.Height < Min.Height ? Min.Height
            : size.Height > max.Height ? max.Height
            : size.Height;

        return new Size(width, height);
    }

    /// <summary>Check if these constraints are satisfied by the given size.</summary>
    public bool IsSatisfiedBy(Size size)
    {
        var max = Max ?? Size.Max;
        return size.Width >= Min.Width
            && size.Height >= Min.Height
            && size.Width <= max.Width
            && size.Height <= max.Height;
    }

    /// <summary>
    /// Combine two constraints by taking the maximum minimums and minimum maximums.
    /// Useful when a widget has multiple children and needs to satisfy all constraints.
    /// </summary>
    public SizeConstraints Intersect(SizeConstraints other)
    {
        var minWidth = Math.Max(Min.Width, other.Min.Width);
        var minHeight = Math.Max(Min.Height, other.Min.Height);

        Size? max = (Max, other.Max) switch
        {
            (Size a, Size b) => new Size(
                (ushort)Math.Min(a.Width, b.Width),
                (ushort)Math.Min(a.Height, b.Height)),
            (Size a, null) => a,
            (null, Size b) => b,
            (null, null) => null,
        };

        var prefWidth = Math.Max(Preferred.Width, other.Preferred.Width);
        var prefHeight = Math.Max(Preferred.Height, other.Preferred.Height);

        return new SizeConstraints
        {
            Min = new Size(minWidth, minHeight),
            Preferred = new Size(prefWidth, prefHeight),
            Max = max,
        };
    }
}

/// <summary>
/// A widget that can report its intrinsic dimensions.
/// Implement this interface for widgets whose size depends on their content.
/// Widgets that simply fill available space can use the default implementation.
/// </summary>
public interface IMeasurableWidget
{
    /// <summary>
    /// Measure the widget given available space, returning full size constraints.
    /// Upstream Rust name: `measure(&self, available: Size) -> SizeConstraints`.
    /// </summary>
    SizeConstraints MeasureConstraints(Size available);

    /// <summary>
    /// Quick check: does this widget have content-dependent sizing?
    /// </summary>
    bool HasIntrinsicSize() => false;

    /// <summary>
    /// Measure intrinsic size along one axis for FitContent layout.
    /// </summary>
    SizeHint MeasureAxis(Size available, LayoutDirection direction);
}
