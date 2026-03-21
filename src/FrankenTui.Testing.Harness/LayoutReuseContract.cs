using FrankenTui.Layout;

namespace FrankenTui.Testing.Harness;

public enum ReusableComputation
{
    LayoutSolve,
    TextWidth,
    TextWrap,
    StyleResolution,
    IntrinsicMeasure
}

public enum NonReusableComputation
{
    CursorPosition,
    AnimationState,
    ScrollOffset,
    RandomEffect,
    SelectionState,
    NotificationPosition
}

public enum KeyComponent
{
    Area,
    Direction,
    ConstraintsHash,
    ContentHash,
    MaxWidth,
    StyleId,
    ThemeEpoch,
    IntrinsicHash
}

public enum InvalidationTrigger
{
    Resize,
    ConstraintChange,
    ContentChange,
    ThemeChange,
    FontChange,
    GenerationBump
}

public sealed record CacheKeySpec(
    ReusableComputation Computation,
    IReadOnlyList<KeyComponent> Components,
    IReadOnlyList<InvalidationTrigger> InvalidationTriggers);

public static class LayoutReuseContract
{
    public static CacheKeySpec DefaultLayoutSolveSpec() =>
        new(
            ReusableComputation.LayoutSolve,
            [KeyComponent.Area, KeyComponent.Direction, KeyComponent.ConstraintsHash],
            [InvalidationTrigger.Resize, InvalidationTrigger.ConstraintChange, InvalidationTrigger.GenerationBump]);

    public static bool EquivalentKey(
        LayoutPlan plan,
        LayoutTrace trace)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(trace);

        var planKey = LayoutCacheKey.Create(plan.Bounds, plan.Direction, plan.Constraints);
        var traceKey = LayoutCacheKey.Create(trace.Bounds, trace.Direction, trace.Constraints);
        return planKey == traceKey;
    }

    public static bool IsSafeToReuse(ReusableComputation computation) => computation switch
    {
        ReusableComputation.LayoutSolve => true,
        ReusableComputation.TextWidth => true,
        ReusableComputation.TextWrap => true,
        ReusableComputation.StyleResolution => true,
        ReusableComputation.IntrinsicMeasure => true,
        _ => false
    };

    public static string UnsafeReason(NonReusableComputation computation) => computation switch
    {
        NonReusableComputation.CursorPosition => "Focus state changes between frames.",
        NonReusableComputation.AnimationState => "Animation state is time-dependent.",
        NonReusableComputation.ScrollOffset => "Scroll offset is user-driven and frame-local.",
        NonReusableComputation.RandomEffect => "Random effects are intentionally non-deterministic.",
        NonReusableComputation.SelectionState => "Selection state changes with pointer and keyboard interaction.",
        _ => "Notification placement depends on dismissal timing."
    };
}
