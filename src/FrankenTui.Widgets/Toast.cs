// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/toast.rs
// Toast notification widget with position, icon, animation, actions, and duration.

using System.Threading;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// ── ToastId ──────────────────────────────────────────────────────────────────

/// <summary>Unique identifier for a toast notification.</summary>
public readonly struct ToastId : IEquatable<ToastId>
{
    public readonly ulong Value;

    public ToastId(ulong value) => Value = value;

    /// <summary>Create a new toast ID.</summary>
    public static ToastId New(ulong id) => new(id);

    public bool Equals(ToastId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ToastId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(ToastId a, ToastId b) => a.Value == b.Value;
    public static bool operator !=(ToastId a, ToastId b) => a.Value != b.Value;
    public override string ToString() => $"ToastId({Value})";
}

// ── ToastPosition ─────────────────────────────────────────────────────────────

/// <summary>Position where the toast should be displayed.</summary>
/// <remarks>TopRight is assigned value 0 so that <c>default(ToastPosition)</c>
/// matches Rust's <c>#[default]</c> attribute on the TopRight variant.</remarks>
public enum ToastPosition
{
    // DIVERGENCE: Rust variant order is TopLeft/TopCenter/TopRight/BottomLeft/BottomCenter/BottomRight
    // but Rust marks TopRight as #[default]. In C# the default enum value is ordinal 0, so we
    // assign TopRight = 0 explicitly and give the others sequential values to preserve the default.
    /// <summary>Top-right corner (default).</summary>
    TopRight    = 0,
    /// <summary>Top-left corner.</summary>
    TopLeft     = 1,
    /// <summary>Top center.</summary>
    TopCenter   = 2,
    /// <summary>Bottom-left corner.</summary>
    BottomLeft  = 3,
    /// <summary>Bottom center.</summary>
    BottomCenter = 4,
    /// <summary>Bottom-right corner.</summary>
    BottomRight = 5,
}

/// <summary>Extension methods on ToastPosition.</summary>
public static class ToastPositionExtensions
{
    private static ushort SatSub(ushort a, ushort b) => a >= b ? (ushort)(a - b) : (ushort)0;

    /// <summary>Calculate the toast's top-left position within a terminal area.
    /// Returns (x, y) for the toast's origin given its dimensions.</summary>
    public static (ushort x, ushort y) CalculatePosition(
        this ToastPosition self,
        ushort terminalWidth,
        ushort terminalHeight,
        ushort toastWidth,
        ushort toastHeight,
        ushort margin)
    {
        ushort x = self switch
        {
            ToastPosition.TopLeft    or ToastPosition.BottomLeft   => margin,
            ToastPosition.TopCenter  or ToastPosition.BottomCenter =>
                (ushort)(SatSub(terminalWidth, toastWidth) / 2),
            _ => SatSub(SatSub(terminalWidth, toastWidth), margin),
        };

        ushort y = self switch
        {
            ToastPosition.TopLeft   or ToastPosition.TopCenter or ToastPosition.TopRight => margin,
            _ => SatSub(SatSub(terminalHeight, toastHeight), margin),
        };

        return (x, y);
    }
}

// ── ToastIcon ─────────────────────────────────────────────────────────────────

/// <summary>Discriminator for ToastIcon variants.</summary>
public enum ToastIconKind
{
    Success,
    Error,
    Warning,
    Info,
    Custom,
}

/// <summary>Icon displayed in the toast to indicate message type.</summary>
public readonly struct ToastIcon : IEquatable<ToastIcon>
{
    public readonly ToastIconKind Kind;
    public readonly char CustomChar;

    private ToastIcon(ToastIconKind kind, char custom = '\0') { Kind = kind; CustomChar = custom; }

    public static readonly ToastIcon Success = new(ToastIconKind.Success);
    public static readonly ToastIcon Error   = new(ToastIconKind.Error);
    public static readonly ToastIcon Warning = new(ToastIconKind.Warning);
    public static readonly ToastIcon Info    = new(ToastIconKind.Info);
    public static ToastIcon Custom(char c)   => new(ToastIconKind.Custom, c);

    /// <summary>Default icon is Info.</summary>
    public static ToastIcon Default => Info;

    /// <summary>Get the display character for this icon.</summary>
    public char AsChar() => Kind switch
    {
        ToastIconKind.Success => '✓', // ✓
        ToastIconKind.Error   => '✗', // ✗
        ToastIconKind.Warning => '!',
        ToastIconKind.Info    => 'i',
        _                     => CustomChar,
    };

    /// <summary>Get the fallback ASCII character for degraded rendering.</summary>
    public char AsAscii() => Kind switch
    {
        ToastIconKind.Success => '+',
        ToastIconKind.Error   => 'x',
        ToastIconKind.Warning => '!',
        ToastIconKind.Info    => 'i',
        _ => CustomChar < 128 ? CustomChar : '*',
    };

    public bool Equals(ToastIcon other) => Kind == other.Kind && CustomChar == other.CustomChar;
    public override bool Equals(object? obj) => obj is ToastIcon other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Kind, CustomChar);
    public static bool operator ==(ToastIcon a, ToastIcon b) => a.Equals(b);
    public static bool operator !=(ToastIcon a, ToastIcon b) => !a.Equals(b);
    public override string ToString() => Kind == ToastIconKind.Custom
        ? $"Custom('{CustomChar}')" : Kind.ToString();
}

// ── ToastStyle ────────────────────────────────────────────────────────────────

/// <summary>Visual style variant for the toast.</summary>
/// <remarks>Info is assigned value 0 so that <c>default(ToastStyle)</c>
/// matches Rust's <c>#[default]</c> attribute on the Info variant.</remarks>
public enum ToastStyle
{
    // DIVERGENCE: Rust variant order is Success/Error/Warning/Info/Neutral but Rust marks
    // Info as #[default]. In C# the default enum value is ordinal 0, so we assign Info = 0.
    /// <summary>Informational style (typically blue, default).</summary>
    Info    = 0,
    /// <summary>Success style (typically green).</summary>
    Success = 1,
    /// <summary>Error style (typically red).</summary>
    Error   = 2,
    /// <summary>Warning style (typically yellow/orange).</summary>
    Warning = 3,
    /// <summary>Neutral style (no semantic coloring).</summary>
    Neutral = 4,
}

// ── ToastAnimationPhase ───────────────────────────────────────────────────────

/// <summary>Animation phase for toast lifecycle.
/// Toasts progress through these phases: Entering → Visible → Exiting → Hidden.
/// The animation system tracks progress within each phase.</summary>
public enum ToastAnimationPhase
{
    /// <summary>Toast is animating in (slide/fade entrance).</summary>
    Entering,
    /// <summary>Toast is fully visible (no animation, default).</summary>
    Visible,
    /// <summary>Toast is animating out (slide/fade exit).</summary>
    Exiting,
    /// <summary>Toast has completed exit animation.</summary>
    Hidden,
}

// ── ToastEntranceAnimation ────────────────────────────────────────────────────

/// <summary>Entrance animation type. Determines how the toast appears on screen.</summary>
public enum ToastEntranceAnimation
{
    /// <summary>Slide in from the top edge.</summary>
    SlideFromTop,
    /// <summary>Slide in from the right edge (default).</summary>
    SlideFromRight,
    /// <summary>Slide in from the bottom edge.</summary>
    SlideFromBottom,
    /// <summary>Slide in from the left edge.</summary>
    SlideFromLeft,
    /// <summary>Fade in (opacity transition).</summary>
    FadeIn,
    /// <summary>No animation (instant appear).</summary>
    None,
}

/// <summary>Extension methods on ToastEntranceAnimation.</summary>
public static class ToastEntranceAnimationExtensions
{
    internal static short OffsetFromDimension(ushort value)
        => value > (ushort)short.MaxValue ? short.MaxValue : (short)value;

    /// <summary>Get the initial offset for this entrance animation.
    /// Returns (dx, dy) offset in cells from the final position.</summary>
    public static (short dx, short dy) InitialOffset(this ToastEntranceAnimation self, ushort toastWidth, ushort toastHeight)
    {
        short widthOffset  = OffsetFromDimension(toastWidth);
        short heightOffset = OffsetFromDimension(toastHeight);
        return self switch
        {
            ToastEntranceAnimation.SlideFromTop    => (0,             (short)-heightOffset),
            ToastEntranceAnimation.SlideFromRight  => (widthOffset,   0),
            ToastEntranceAnimation.SlideFromBottom => (0,             heightOffset),
            ToastEntranceAnimation.SlideFromLeft   => ((short)-widthOffset, 0),
            _                                      => (0, 0),
        };
    }

    /// <summary>Calculate the offset at a given progress (0.0 to 1.0).
    /// Progress of 0.0 = initial offset, 1.0 = no offset.</summary>
    public static (short dx, short dy) OffsetAtProgress(
        this ToastEntranceAnimation self, double progress, ushort toastWidth, ushort toastHeight)
    {
        var (dx, dy) = self.InitialOffset(toastWidth, toastHeight);
        var invProgress = 1.0 - Math.Clamp(progress, 0.0, 1.0);
        return (
            (short)Math.Round(dx * invProgress),
            (short)Math.Round(dy * invProgress)
        );
    }

    /// <summary>Check if this animation affects position (vs. just opacity).</summary>
    public static bool AffectsPosition(this ToastEntranceAnimation self)
        => self is not (ToastEntranceAnimation.FadeIn or ToastEntranceAnimation.None);
}

// ── ToastExitAnimation ────────────────────────────────────────────────────────

/// <summary>Exit animation type. Determines how the toast disappears from screen.</summary>
public enum ToastExitAnimation
{
    /// <summary>Fade out (opacity transition, default).</summary>
    FadeOut,
    /// <summary>Slide out in the reverse of entrance direction.</summary>
    SlideOut,
    /// <summary>Slide out to the top edge.</summary>
    SlideToTop,
    /// <summary>Slide out to the right edge.</summary>
    SlideToRight,
    /// <summary>Slide out to the bottom edge.</summary>
    SlideToBottom,
    /// <summary>Slide out to the left edge.</summary>
    SlideToLeft,
    /// <summary>No animation (instant disappear).</summary>
    None,
}

/// <summary>Extension methods on ToastExitAnimation.</summary>
public static class ToastExitAnimationExtensions
{
    /// <summary>Get the final offset for this exit animation.
    /// Returns (dx, dy) offset in cells from the starting position.</summary>
    public static (short dx, short dy) FinalOffset(
        this ToastExitAnimation self, ushort toastWidth, ushort toastHeight, ToastEntranceAnimation entrance)
    {
        short widthOffset  = ToastEntranceAnimationExtensions.OffsetFromDimension(toastWidth);
        short heightOffset = ToastEntranceAnimationExtensions.OffsetFromDimension(toastHeight);
        if (self == ToastExitAnimation.SlideOut)
        {
            var (dx, dy) = entrance.InitialOffset(toastWidth, toastHeight);
            return ((short)-dx, (short)-dy);
        }
        return self switch
        {
            ToastExitAnimation.SlideToTop    => (0,              (short)-heightOffset),
            ToastExitAnimation.SlideToRight  => (widthOffset,   0),
            ToastExitAnimation.SlideToBottom => (0,              heightOffset),
            ToastExitAnimation.SlideToLeft   => ((short)-widthOffset, 0),
            _                                => (0, 0),
        };
    }

    /// <summary>Calculate the offset at a given progress (0.0 to 1.0).
    /// Progress of 0.0 = no offset, 1.0 = final offset.</summary>
    public static (short dx, short dy) OffsetAtProgress(
        this ToastExitAnimation self, double progress, ushort toastWidth, ushort toastHeight, ToastEntranceAnimation entrance)
    {
        var (dx, dy) = self.FinalOffset(toastWidth, toastHeight, entrance);
        var p = Math.Clamp(progress, 0.0, 1.0);
        return (
            (short)Math.Round(dx * p),
            (short)Math.Round(dy * p)
        );
    }

    /// <summary>Check if this animation affects position (vs. just opacity).</summary>
    public static bool AffectsPosition(this ToastExitAnimation self)
        => self is not (ToastExitAnimation.FadeOut or ToastExitAnimation.None);
}

// ── ToastEasing ───────────────────────────────────────────────────────────────

/// <summary>Easing function for animations.
/// Simplified subset of easing curves for toast animations.
/// For the full set, see ftui_extras::text_effects::Easing.</summary>
public enum ToastEasing
{
    /// <summary>Linear interpolation.</summary>
    Linear,
    /// <summary>Smooth ease-out (decelerating, default).</summary>
    EaseOut,
    /// <summary>Smooth ease-in (accelerating).</summary>
    EaseIn,
    /// <summary>Smooth S-curve.</summary>
    EaseInOut,
    /// <summary>Bouncy effect.</summary>
    Bounce,
}

/// <summary>Extension methods on ToastEasing.</summary>
public static class ToastEasingExtensions
{
    /// <summary>Apply the easing function to a progress value (0.0 to 1.0).</summary>
    public static double Apply(this ToastEasing self, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return self switch
        {
            ToastEasing.Linear    => t,
            ToastEasing.EaseOut   => 1.0 - Math.Pow(1.0 - t, 3),
            ToastEasing.EaseIn    => t * t * t,
            ToastEasing.EaseInOut => t < 0.5
                ? 4.0 * t * t * t
                : 1.0 - Math.Pow(-2.0 * t + 2.0, 3) / 2.0,
            ToastEasing.Bounce    => ApplyBounce(t),
            _                     => t,
        };
    }

    private static double ApplyBounce(double t)
    {
        const double n1 = 7.5625;
        const double d1 = 2.75;
        if (t < 1.0 / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2.0 / d1)
        {
            t -= 1.5 / d1;
            return n1 * t * t + 0.75;
        }
        else if (t < 2.5 / d1)
        {
            t -= 2.25 / d1;
            return n1 * t * t + 0.9375;
        }
        else
        {
            t -= 2.625 / d1;
            return n1 * t * t + 0.984375;
        }
    }
}

// ── ToastAnimationConfig ──────────────────────────────────────────────────────

/// <summary>Animation configuration for a toast.</summary>
public sealed class ToastAnimationConfig
{
    /// <summary>Entrance animation type.</summary>
    public ToastEntranceAnimation Entrance { get; set; } = ToastEntranceAnimation.SlideFromRight;
    /// <summary>Exit animation type.</summary>
    public ToastExitAnimation Exit { get; set; } = ToastExitAnimation.FadeOut;
    /// <summary>Duration of entrance animation.</summary>
    public TimeSpan EntranceDuration { get; set; } = TimeSpan.FromMilliseconds(200);
    /// <summary>Duration of exit animation.</summary>
    public TimeSpan ExitDuration { get; set; } = TimeSpan.FromMilliseconds(150);
    /// <summary>Easing function for entrance.</summary>
    public ToastEasing EntranceEasing { get; set; } = ToastEasing.EaseOut;
    /// <summary>Easing function for exit.</summary>
    public ToastEasing ExitEasing { get; set; } = ToastEasing.EaseIn;
    /// <summary>Whether to respect reduced-motion preference.</summary>
    public bool RespectReducedMotion { get; set; } = true;

    /// <summary>Create a config with no animations.</summary>
    public static ToastAnimationConfig CreateNone() => new()
    {
        Entrance         = ToastEntranceAnimation.None,
        Exit             = ToastExitAnimation.None,
        EntranceDuration = TimeSpan.Zero,
        ExitDuration     = TimeSpan.Zero,
    };

    /// <summary>Check if animations are effectively disabled.</summary>
    public bool IsDisabled()
        => Entrance == ToastEntranceAnimation.None && Exit == ToastExitAnimation.None;
}

// ── ToastAnimationState ───────────────────────────────────────────────────────

/// <summary>Tracks the animation state for a toast.</summary>
public sealed class ToastAnimationState
{
    // DIVERGENCE: Rust uses web_time::Instant which is Wasm-safe. C# uses DateTime.UtcNow.
    /// <summary>Current animation phase.</summary>
    public ToastAnimationPhase Phase { get; set; }
    /// <summary>When the current phase started.</summary>
    public DateTime PhaseStarted { get; set; }
    /// <summary>Whether reduced motion is active.</summary>
    public bool ReducedMotion { get; set; }

    private ToastAnimationState(ToastAnimationPhase phase, bool reducedMotion)
    {
        Phase        = phase;
        PhaseStarted = DateTime.UtcNow;
        ReducedMotion = reducedMotion;
    }

    /// <summary>Default: Entering phase, no reduced motion.</summary>
    public ToastAnimationState() : this(ToastAnimationPhase.Entering, false) { }

    /// <summary>Create a new animation state starting in the Entering phase.</summary>
    public static ToastAnimationState New() => new();

    /// <summary>Create a state with reduced motion enabled (skips to Visible).</summary>
    public static ToastAnimationState WithReducedMotion() =>
        new(ToastAnimationPhase.Visible, reducedMotion: true);

    /// <summary>Get the progress within the current phase (0.0 to 1.0).</summary>
    public double Progress(TimeSpan phaseDuration)
    {
        if (phaseDuration == TimeSpan.Zero) return 1.0;
        var elapsed = DateTime.UtcNow - PhaseStarted;
        return Math.Min(elapsed.TotalSeconds / phaseDuration.TotalSeconds, 1.0);
    }

    /// <summary>Transition to the next phase.</summary>
    public void TransitionTo(ToastAnimationPhase phase)
    {
        Phase        = phase;
        PhaseStarted = DateTime.UtcNow;
    }

    /// <summary>Start the exit animation.</summary>
    public void StartExit()
    {
        if (ReducedMotion)
            TransitionTo(ToastAnimationPhase.Hidden);
        else
            TransitionTo(ToastAnimationPhase.Exiting);
    }

    /// <summary>Check if the animation has completed (Hidden phase).</summary>
    public bool IsComplete() => Phase == ToastAnimationPhase.Hidden;

    /// <summary>Update the animation state based on elapsed time.
    /// Returns true if the phase changed.</summary>
    public bool Tick(ToastAnimationConfig config)
    {
        var prevPhase = Phase;

        switch (Phase)
        {
            case ToastAnimationPhase.Entering:
            {
                var duration = ReducedMotion ? TimeSpan.Zero : config.EntranceDuration;
                if (Progress(duration) >= 1.0)
                    TransitionTo(ToastAnimationPhase.Visible);
                break;
            }
            case ToastAnimationPhase.Exiting:
            {
                var duration = ReducedMotion ? TimeSpan.Zero : config.ExitDuration;
                if (Progress(duration) >= 1.0)
                    TransitionTo(ToastAnimationPhase.Hidden);
                break;
            }
        }

        return Phase != prevPhase;
    }

    /// <summary>Calculate the current animation offset.
    /// Returns (dx, dy) offset to apply to the toast position.</summary>
    public (short dx, short dy) CurrentOffset(ToastAnimationConfig config, ushort toastWidth, ushort toastHeight)
    {
        if (ReducedMotion) return (0, 0);

        switch (Phase)
        {
            case ToastAnimationPhase.Entering:
            {
                var rawProgress   = Progress(config.EntranceDuration);
                var easedProgress = config.EntranceEasing.Apply(rawProgress);
                return config.Entrance.OffsetAtProgress(easedProgress, toastWidth, toastHeight);
            }
            case ToastAnimationPhase.Exiting:
            {
                var rawProgress   = Progress(config.ExitDuration);
                var easedProgress = config.ExitEasing.Apply(rawProgress);
                return config.Exit.OffsetAtProgress(easedProgress, toastWidth, toastHeight, config.Entrance);
            }
            default:
                return (0, 0);
        }
    }

    /// <summary>Calculate the current opacity (0.0 to 1.0). Used for fade animations.</summary>
    public double CurrentOpacity(ToastAnimationConfig config)
    {
        if (ReducedMotion)
            return Phase == ToastAnimationPhase.Hidden ? 0.0 : 1.0;

        switch (Phase)
        {
            case ToastAnimationPhase.Entering:
                return config.Entrance == ToastEntranceAnimation.FadeIn
                    ? config.EntranceEasing.Apply(Progress(config.EntranceDuration))
                    : 1.0;
            case ToastAnimationPhase.Exiting:
                return config.Exit == ToastExitAnimation.FadeOut
                    ? 1.0 - config.ExitEasing.Apply(Progress(config.ExitDuration))
                    : 1.0;
            case ToastAnimationPhase.Visible:
                return 1.0;
            case ToastAnimationPhase.Hidden:
                return 0.0;
            default:
                return 1.0;
        }
    }
}

// ── ToastConfig ───────────────────────────────────────────────────────────────

/// <summary>Configuration for a toast notification.</summary>
public sealed class ToastConfig
{
    /// <summary>Position on screen.</summary>
    public ToastPosition Position { get; set; } = ToastPosition.TopRight;
    /// <summary>Auto-dismiss duration. null means persistent until dismissed.</summary>
    public TimeSpan? Duration { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Whether the duration/persistence policy was explicitly configured by
    /// the caller instead of inherited from a queue-level default.</summary>
    public bool DurationExplicit { get; set; }
    /// <summary>Visual style variant.</summary>
    public ToastStyle StyleVariant { get; set; } = ToastStyle.Info;
    /// <summary>Maximum width in columns.</summary>
    public ushort MaxWidth { get; set; } = 50;
    /// <summary>Margin from screen edges.</summary>
    public ushort Margin { get; set; } = 1;
    /// <summary>Whether the toast can be dismissed by the user.</summary>
    public bool Dismissable { get; set; } = true;
    /// <summary>Animation configuration.</summary>
    public ToastAnimationConfig Animation { get; set; } = new ToastAnimationConfig();
}

// ── KeyEvent ──────────────────────────────────────────────────────────────────

/// <summary>Simplified key event for toast interaction handling.
/// This is a widget-level abstraction over terminal key events. The hosting
/// application maps its native key events to these variants before passing
/// them to Toast.HandleKey.
///
/// DIVERGENCE: Rust names this <c>KeyEvent</c> local to the <c>ftui_widgets::toast</c> module,
/// but the C# namespace already has a richer <c>KeyEvent</c> from Mouse.cs (ftui-core::event).
/// Renamed to <c>ToastKeyEvent</c> to avoid the clash.</summary>
public enum ToastKeyEvent
{
    /// <summary>Escape key — dismiss the toast.</summary>
    Esc,
    /// <summary>Tab key — cycle focus through action buttons.</summary>
    Tab,
    /// <summary>Enter key — invoke the focused action.</summary>
    Enter,
    /// <summary>Any other key (not consumed by the toast).</summary>
    Other,
}

// ── ToastAction ───────────────────────────────────────────────────────────────

/// <summary>An interactive action button displayed in a toast.
/// Actions allow users to respond to a toast (e.g., "Undo", "Retry", "View").
/// Each action has a display label and a unique identifier used to match
/// callbacks when the action is invoked.
///
/// Action focus uses a simple round-robin model: Tab advances focus index
/// modulo action count. The decision rule is: next_focus = (current_focus + 1) % actions.len().</summary>
public sealed class ToastAction : IEquatable<ToastAction>
{
    /// <summary>Display label for the action button (e.g., "Undo").</summary>
    public string Label { get; }
    /// <summary>Unique identifier for callback matching.</summary>
    public string Id { get; }

    /// <summary>Create a new toast action.
    /// Asserts in debug that label and id are non-empty after trimming.</summary>
    public ToastAction(string label, string id)
    {
        System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(label), "ToastAction label must not be empty");
        System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(id),    "ToastAction id must not be empty");
        Label = label;
        Id    = id;
    }

    /// <summary>Display width of the action button including brackets.
    /// Rendered as [Label], so width = label_width + 2 (brackets).</summary>
    public int DisplayWidth() => DisplayWidthOf(Label) + 2;

    internal static int DisplayWidthOf(string s)
    {
        int w = 0;
        var te = System.Globalization.StringInfo.GetTextElementEnumerator(s);
        while (te.MoveNext()) w += WidgetDrawing.GraphemeWidth(te.GetTextElement());
        return w;
    }

    public bool Equals(ToastAction? other) => other is not null && Label == other.Label && Id == other.Id;
    public override bool Equals(object? obj) => obj is ToastAction a && Equals(a);
    public override int GetHashCode() => HashCode.Combine(Label, Id);
}

// ── ToastEvent ────────────────────────────────────────────────────────────────

/// <summary>Result of handling a toast interaction event.
/// Returned by Toast.HandleKey to indicate what happened.</summary>
public abstract class ToastEvent : IEquatable<ToastEvent>
{
    // Closed hierarchy — no external subclasses.
    private ToastEvent() { }

    /// <summary>No interaction occurred (key not consumed).</summary>
    public sealed class NoneEvent : ToastEvent
    {
        internal NoneEvent() { }
        public override bool Equals(ToastEvent? other) => other is NoneEvent;
        public override int GetHashCode() => 0;
        public override string ToString() => "None";
    }

    /// <summary>The toast was dismissed.</summary>
    public sealed class DismissedEvent : ToastEvent
    {
        internal DismissedEvent() { }
        public override bool Equals(ToastEvent? other) => other is DismissedEvent;
        public override int GetHashCode() => 1;
        public override string ToString() => "Dismissed";
    }

    /// <summary>An action button was invoked. Contains the action ID.</summary>
    public sealed class ActionEvent : ToastEvent
    {
        public string ActionId { get; }
        internal ActionEvent(string actionId) => ActionId = actionId;
        public override bool Equals(ToastEvent? other) => other is ActionEvent a && a.ActionId == ActionId;
        public override int GetHashCode() => HashCode.Combine(2, ActionId);
        public override string ToString() => $"Action({ActionId})";
    }

    /// <summary>Focus moved between action buttons.</summary>
    public sealed class FocusChangedEvent : ToastEvent
    {
        internal FocusChangedEvent() { }
        public override bool Equals(ToastEvent? other) => other is FocusChangedEvent;
        public override int GetHashCode() => 3;
        public override string ToString() => "FocusChanged";
    }

    // Singleton/factory mirrors of Rust enum variants
    public static readonly ToastEvent None         = new NoneEvent();
    public static readonly ToastEvent Dismissed    = new DismissedEvent();
    public static readonly ToastEvent FocusChanged = new FocusChangedEvent();
    public static ToastEvent Action(string id) => new ActionEvent(id);

    public abstract bool Equals(ToastEvent? other);
    public override bool Equals(object? obj) => obj is ToastEvent e && Equals(e);
    public override abstract int GetHashCode();
    public static bool operator ==(ToastEvent? a, ToastEvent? b)
        => a is null ? b is null : a.Equals(b);
    public static bool operator !=(ToastEvent? a, ToastEvent? b) => !(a == b);
}

// ── ToastContent ──────────────────────────────────────────────────────────────

/// <summary>Content of a toast notification.</summary>
public sealed class ToastContent
{
    /// <summary>Main message text.</summary>
    public string Message { get; set; }
    /// <summary>Optional icon.</summary>
    public ToastIcon? Icon { get; set; }
    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Create new content with just a message.</summary>
    public ToastContent(string message) => Message = message;

    /// <summary>Set the icon.</summary>
    public ToastContent WithIcon(ToastIcon icon) { Icon = icon; return this; }

    /// <summary>Set the title.</summary>
    public ToastContent WithTitle(string title) { Title = title; return this; }
}

// ── ToastState ────────────────────────────────────────────────────────────────

/// <summary>Internal state tracking for a toast.</summary>
public sealed class ToastState
{
    // DIVERGENCE: Rust uses web_time::Instant (Wasm-safe). C# uses DateTime.UtcNow.
    /// <summary>When the toast was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Whether the toast has been dismissed.</summary>
    public bool Dismissed { get; set; }
    /// <summary>Animation state.</summary>
    public ToastAnimationState Animation { get; set; } = new ToastAnimationState();
    /// <summary>Index of the currently focused action, if any.</summary>
    public int? FocusedAction { get; set; }
    /// <summary>Whether the auto-dismiss timer is paused (e.g., due to action focus).</summary>
    public bool TimerPaused { get; set; }
    /// <summary>When the timer was paused, for calculating credited time.</summary>
    public DateTime? PauseStarted { get; set; }
    /// <summary>Total duration the timer has been paused (accumulated across multiple pauses).</summary>
    public TimeSpan TotalPaused { get; set; } = TimeSpan.Zero;

    /// <summary>Create a new state with reduced motion enabled.</summary>
    public static ToastState WithReducedMotion() => new()
    {
        Animation = ToastAnimationState.WithReducedMotion(),
    };
}

// ── Toast ─────────────────────────────────────────────────────────────────────

/// <summary>A toast notification widget.
///
/// Toasts display transient messages to the user, typically in a corner
/// of the screen. They can auto-dismiss after a duration or be manually dismissed.
///
/// Example:
/// <code>
///   var toast = Toast.New("Operation completed")
///       .Icon(ToastIcon.Success)
///       .Position(ToastPosition.TopRight)
///       .Duration(TimeSpan.FromSeconds(3));
///   toast.Render(area, frame);
/// </code>
/// </summary>
public sealed class Toast : IWidget
{
    private static long _nextId = 1;

    /// <summary>Unique identifier.</summary>
    public ToastId Id { get; }
    /// <summary>Toast content.</summary>
    public ToastContent Content { get; }
    /// <summary>Configuration.</summary>
    public ToastConfig Config { get; }
    /// <summary>Internal state.</summary>
    public ToastState State { get; }
    /// <summary>Interactive action buttons (e.g., "Undo", "Retry").</summary>
    public List<ToastAction> Actions { get; }

    // Private style fields — accessible to tests via internal properties.
    private WidgetStyle _style;
    private WidgetStyle _iconStyle;
    private WidgetStyle _titleStyle;
    private WidgetStyle _actionStyle;
    private WidgetStyle _actionFocusStyle;

    // Exposed for test assertions (matching Rust pub(crate) visibility pattern).
    internal WidgetStyle StyleField            => _style;
    internal WidgetStyle IconStyleField        => _iconStyle;
    internal WidgetStyle TitleStyleField       => _titleStyle;
    internal WidgetStyle ActionStyleField      => _actionStyle;
    internal WidgetStyle ActionFocusStyleField => _actionFocusStyle;

    private Toast(ToastId id, string message)
    {
        Id      = id;
        Content = new ToastContent(message);
        Config  = new ToastConfig();
        State   = new ToastState();
        Actions = new List<ToastAction>();
    }

    // ── Static constructors ───────────────────────────────────────────────

    /// <summary>Create a new toast with the given message.</summary>
    public static Toast New(string message)
    {
        var rawId = (ulong)Interlocked.Increment(ref _nextId);
        return new Toast(ToastId.New(rawId), message);
    }

    /// <summary>Create a toast with a specific ID.</summary>
    public static Toast WithId(ToastId id, string message) => new Toast(id, message);

    // ── Builder methods ───────────────────────────────────────────────────

    /// <summary>Set the toast icon.</summary>
    public Toast Icon(ToastIcon icon) { Content.Icon = icon; return this; }

    /// <summary>Set the toast title.</summary>
    public Toast Title(string title) { Content.Title = title; return this; }

    /// <summary>Set the toast position.</summary>
    public Toast Position(ToastPosition position) { Config.Position = position; return this; }

    /// <summary>Set the auto-dismiss duration.</summary>
    public Toast Duration(TimeSpan duration)
    {
        Config.Duration         = duration;
        Config.DurationExplicit = true;
        return this;
    }

    /// <summary>Make the toast persistent (no auto-dismiss).</summary>
    public Toast Persistent()
    {
        Config.Duration         = null;
        Config.DurationExplicit = true;
        return this;
    }

    /// <summary>Set the style variant.</summary>
    public Toast StyleVariant(ToastStyle variant) { Config.StyleVariant = variant; return this; }

    /// <summary>Set the maximum width.</summary>
    public Toast MaxWidth(ushort width) { Config.MaxWidth = width; return this; }

    /// <summary>Set the margin from screen edges.</summary>
    public Toast Margin(ushort margin) { Config.Margin = margin; return this; }

    /// <summary>Set whether the toast is dismissable.</summary>
    public Toast Dismissable(bool dismissable) { Config.Dismissable = dismissable; return this; }

    /// <summary>Set the base style.</summary>
    public Toast Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set the icon style.</summary>
    public Toast WithIconStyle(WidgetStyle style) { _iconStyle = style; return this; }

    /// <summary>Set the title style.</summary>
    public Toast WithTitleStyle(WidgetStyle style) { _titleStyle = style; return this; }

    // ── Animation builder methods ─────────────────────────────────────────

    /// <summary>Set the entrance animation.</summary>
    public Toast EntranceAnimation(ToastEntranceAnimation animation)
    { Config.Animation.Entrance = animation; return this; }

    /// <summary>Set the exit animation.</summary>
    public Toast ExitAnimation(ToastExitAnimation animation)
    { Config.Animation.Exit = animation; return this; }

    /// <summary>Set the entrance animation duration.</summary>
    public Toast EntranceDuration(TimeSpan duration)
    { Config.Animation.EntranceDuration = duration; return this; }

    /// <summary>Set the exit animation duration.</summary>
    public Toast ExitDuration(TimeSpan duration)
    { Config.Animation.ExitDuration = duration; return this; }

    /// <summary>Set the entrance easing function.</summary>
    public Toast EntranceEasing(ToastEasing easing)
    { Config.Animation.EntranceEasing = easing; return this; }

    /// <summary>Set the exit easing function.</summary>
    public Toast ExitEasing(ToastEasing easing)
    { Config.Animation.ExitEasing = easing; return this; }

    // ── Action builder methods ─────────────────────────────────────────────

    /// <summary>Add a single action button to the toast.</summary>
    public Toast Action(ToastAction action) { Actions.Add(action); return this; }

    /// <summary>Set all action buttons at once.</summary>
    public Toast WithActions(List<ToastAction> actions)
    { Actions.Clear(); Actions.AddRange(actions); return this; }

    /// <summary>Set the style for action buttons.</summary>
    public Toast WithActionStyle(WidgetStyle style) { _actionStyle = style; return this; }

    /// <summary>Set the style for the focused action button.</summary>
    public Toast WithActionFocusStyle(WidgetStyle style) { _actionFocusStyle = style; return this; }

    /// <summary>Disable all animations.</summary>
    public Toast NoAnimation()
    {
        Config.Animation = ToastAnimationConfig.CreateNone();
        State.Animation  = new ToastAnimationState
        {
            Phase         = ToastAnimationPhase.Visible,
            PhaseStarted  = DateTime.UtcNow,
            ReducedMotion = true,
        };
        return this;
    }

    /// <summary>Enable reduced motion mode (skips animations).</summary>
    public Toast ReducedMotion(bool enabled)
    {
        Config.Animation.RespectReducedMotion = enabled;
        if (enabled)
            State.Animation = ToastAnimationState.WithReducedMotion();
        return this;
    }

    // ── State methods ─────────────────────────────────────────────────────

    /// <summary>Check if the toast has expired based on its duration.
    /// Accounts for time spent paused (when actions are focused).</summary>
    public bool IsExpired()
    {
        if (Config.Duration is { } duration)
        {
            var wallElapsed      = DateTime.UtcNow - State.CreatedAt;
            var effectiveElapsed = SatSub(wallElapsed, PausedDuration());
            return effectiveElapsed >= duration;
        }
        return false;
    }

    /// <summary>Check if the toast should be visible.
    /// A toast is visible if it's not in the Hidden animation phase.</summary>
    public bool IsVisible() => State.Animation.Phase != ToastAnimationPhase.Hidden;

    /// <summary>Check if the toast is currently animating.</summary>
    public bool IsAnimating() => State.Animation.Phase
        is ToastAnimationPhase.Entering or ToastAnimationPhase.Exiting;

    /// <summary>Dismiss the toast, starting exit animation.</summary>
    public void Dismiss()
    {
        if (!State.Dismissed)
        {
            State.Dismissed = true;
            State.Animation.StartExit();
        }
    }

    /// <summary>Dismiss immediately without animation.</summary>
    public void DismissImmediately()
    {
        State.Dismissed = true;
        State.Animation.TransitionTo(ToastAnimationPhase.Hidden);
    }

    /// <summary>Update the animation state. Call this each frame.
    /// Returns true if the animation phase changed.</summary>
    public bool TickAnimation() => State.Animation.Tick(Config.Animation);

    /// <summary>Get the current animation phase.</summary>
    public ToastAnimationPhase AnimationPhase() => State.Animation.Phase;

    /// <summary>Get the current animation offset for rendering.
    /// Returns (dx, dy) offset to apply to the position.</summary>
    public (short dx, short dy) AnimationOffset()
    {
        var (w, h) = CalculateDimensions();
        return State.Animation.CurrentOffset(Config.Animation, w, h);
    }

    /// <summary>Get the current opacity for rendering (0.0 to 1.0).</summary>
    public double AnimationOpacity() => State.Animation.CurrentOpacity(Config.Animation);

    /// <summary>Get the remaining time before auto-dismiss. Accounts for paused time.</summary>
    public TimeSpan? RemainingTime()
    {
        if (Config.Duration is { } d)
        {
            var wallElapsed      = DateTime.UtcNow - State.CreatedAt;
            var effectiveElapsed = SatSub(wallElapsed, PausedDuration());
            return SatSub(d, effectiveElapsed);
        }
        return null;
    }

    // ── Interaction methods ────────────────────────────────────────────────

    /// <summary>Handle a key event for toast interaction.
    /// Supported keys: Esc (dismiss if dismissable), Tab (cycle focus), Enter (invoke action).</summary>
    public ToastEvent HandleKey(ToastKeyEvent key)
    {
        if (!IsVisible() || State.Dismissed)
            return ToastEvent.None;

        switch (key)
        {
            case ToastKeyEvent.Esc:
                if (HasFocus())
                {
                    ClearFocus();
                    return ToastEvent.None;
                }
                else if (Config.Dismissable)
                {
                    Dismiss();
                    return ToastEvent.Dismissed;
                }
                return ToastEvent.None;

            case ToastKeyEvent.Tab:
                if (Actions.Count == 0) return ToastEvent.None;
                int next = State.FocusedAction is { } i ? (i + 1) % Actions.Count : 0;
                State.FocusedAction = next;
                PauseTimer();
                return ToastEvent.FocusChanged;

            case ToastKeyEvent.Enter:
                if (State.FocusedAction is { } idx && idx < Actions.Count)
                {
                    var actionId = Actions[idx].Id;
                    Dismiss();
                    return ToastEvent.Action(actionId);
                }
                return ToastEvent.None;

            default:
                return ToastEvent.None;
        }
    }

    /// <summary>Pause the auto-dismiss timer.</summary>
    public void PauseTimer()
    {
        if (!State.TimerPaused)
        {
            State.TimerPaused  = true;
            State.PauseStarted = DateTime.UtcNow;
        }
    }

    /// <summary>Resume the auto-dismiss timer.</summary>
    public void ResumeTimer()
    {
        if (State.TimerPaused)
        {
            if (State.PauseStarted is { } pauseStart)
            {
                var additional     = DateTime.UtcNow - pauseStart;
                State.TotalPaused  = SaturatingAdd(State.TotalPaused, additional);
                State.PauseStarted = null;
            }
            State.TimerPaused = false;
        }
    }

    /// <summary>Clear action focus and resume the timer.</summary>
    public void ClearFocus()
    {
        State.FocusedAction = null;
        ResumeTimer();
    }

    /// <summary>Check whether any action is currently focused.</summary>
    public bool HasFocus() => State.FocusedAction.HasValue;

    /// <summary>Get the currently focused action, if any.</summary>
    public ToastAction? FocusedAction()
        => State.FocusedAction is { } idx && idx < Actions.Count ? Actions[idx] : null;

    private TimeSpan PausedDuration()
    {
        var paused = State.TotalPaused;
        if (State.TimerPaused && State.PauseStarted is { } ps)
            paused = SaturatingAdd(paused, DateTime.UtcNow - ps);
        return paused;
    }

    /// <summary>Calculate the toast dimensions based on content.</summary>
    public (ushort width, ushort height) CalculateDimensions()
    {
        int maxWidth = Config.MaxWidth;

        // Calculate content width
        int iconWidth = Content.Icon is { } icon
            ? ToastAction.DisplayWidthOf(icon.AsChar().ToString()) + 1 // icon + space
            : 0;
        int messageWidth = ToastAction.DisplayWidthOf(Content.Message);
        int titleWidth   = Content.Title is { } t ? ToastAction.DisplayWidthOf(t) : 0;

        // Content width is max of title and (icon + message)
        int contentWidth = Math.Max(iconWidth + messageWidth, titleWidth);

        // Account for actions row width
        if (Actions.Count > 0)
        {
            int actionsWidth = Actions.Sum(a => a.DisplayWidth())
                               + Math.Max(Actions.Count - 1, 0);
            contentWidth = Math.Max(contentWidth, actionsWidth);
        }

        // Add padding (1 each side) and border (1 each side) = +4
        int totalWidth = Math.Min(contentWidth + 4, maxWidth);

        // Height: border top (1) + optional title + message + optional actions + border bot (1)
        bool hasTitle   = Content.Title is not null;
        bool hasActions = Actions.Count > 0;
        int height = 3 + (hasTitle ? 1 : 0) + (hasActions ? 1 : 0);

        return ((ushort)Math.Max(totalWidth, 0), (ushort)height);
    }

    // ── IWidget ────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        var (contentWidth, contentHeight) = CalculateDimensions();
        ushort width  = Math.Min(area.Width,  contentWidth);
        ushort height = Math.Min(area.Height, contentHeight);

        if (width < 3 || height < 3) return; // Too small to render

        var renderArea = new Rect(area.X, area.Y, width, height);

        if (!IsVisible())
        {
            WidgetDrawing.ClearTextArea(frame, renderArea, WidgetStyle.Default);
            return;
        }

        var deg = frame.Degradation;
        if (!deg.RenderContent()) return;

        var baseStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.ClearTextArea(frame, renderArea, baseStyle);

        // Draw border
        bool useUnicode = deg.UseUnicodeBorders();
        char tl, tr, bl, br, h, v;
        if (useUnicode)
            (tl, tr, bl, br, h, v) = ('┌', '┐', '└', '┘', '─', '│');
        else
            (tl, tr, bl, br, h, v) = ('+', '+', '+', '+', '-', '|');

        ushort rx = renderArea.X, ry = renderArea.Y;
        ushort rright  = SatSub16(renderArea.Right,  1);
        ushort rbottom = SatSub16(renderArea.Bottom, 1);

        // Top border
        SetBorderCell(frame, deg, rx, ry, tl);
        for (ushort x = (ushort)(rx + 1); x < rright; x++)
            SetBorderCell(frame, deg, x, ry, h);
        SetBorderCell(frame, deg, rright, ry, tr);

        // Bottom border
        SetBorderCell(frame, deg, rx, rbottom, bl);
        for (ushort x = (ushort)(rx + 1); x < rright; x++)
            SetBorderCell(frame, deg, x, rbottom, h);
        SetBorderCell(frame, deg, rright, rbottom, br);

        // Side borders
        for (ushort y = (ushort)(ry + 1); y < rbottom; y++)
        {
            SetBorderCell(frame, deg, rx,     y, v);
            SetBorderCell(frame, deg, rright, y, v);
        }

        // Draw content
        ushort contentX   = (ushort)(rx + 1);
        ushort contentWid = SatSub16(width, 2);
        ushort contentY   = (ushort)(ry + 1);

        // Draw title if present
        if (Content.Title is { } title)
        {
            var titleStyle = deg.ApplyStyling() ? MergeStyles(_titleStyle, _style) : WidgetStyle.Default;
            WidgetDrawing.DrawTextSpan(frame, contentX, contentY, title, titleStyle,
                (ushort)(contentX + contentWid));
            contentY++;
        }

        // Draw icon and message
        ushort msgX = contentX;

        if (Content.Icon is { } iconVal)
        {
            char iconChar  = useUnicode ? iconVal.AsChar() : iconVal.AsAscii();
            var iconStyle  = deg.ApplyStyling() ? MergeStyles(_iconStyle, _style) : WidgetStyle.Default;
            string iconStr = iconChar.ToString();
            msgX = WidgetDrawing.DrawTextSpan(frame, msgX, contentY, iconStr, iconStyle,
                (ushort)(contentX + contentWid));
            msgX = WidgetDrawing.DrawTextSpan(frame, msgX, contentY, " ", WidgetStyle.Default,
                (ushort)(contentX + contentWid));
        }

        // Draw message
        var msgStyle = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        WidgetDrawing.DrawTextSpan(frame, msgX, contentY, Content.Message, msgStyle,
            (ushort)(contentX + contentWid));

        // Draw action buttons if present
        if (Actions.Count > 0)
        {
            contentY++;
            ushort btnX = contentX;
            ushort maxX = (ushort)(contentX + contentWid);

            for (int idx = 0; idx < Actions.Count; idx++)
            {
                var action    = Actions[idx];
                bool isFocused = State.FocusedAction == idx;
                WidgetStyle btnStyle;
                if (isFocused && deg.ApplyStyling())
                    btnStyle = MergeStyles(_actionFocusStyle, _style);
                else if (deg.ApplyStyling())
                    btnStyle = MergeStyles(_actionStyle, _style);
                else
                    btnStyle = WidgetStyle.Default;

                var label = $"[{action.Label}]";
                btnX = WidgetDrawing.DrawTextSpan(frame, btnX, contentY, label, btnStyle, maxX);

                if (idx + 1 < Actions.Count)
                    btnX = WidgetDrawing.DrawTextSpan(frame, btnX, contentY, " ", WidgetStyle.Default, maxX);
            }
        }
    }

    /// <inheritdoc />
    public bool IsEssential() => false; // Toasts are informational, not essential

    // ── Private helpers ───────────────────────────────────────────────────

    private void SetBorderCell(Frame frame, DegradationLevel deg, ushort x, ushort y, char c)
    {
        var cell = Cell.FromChar(c);
        if (deg.ApplyStyling()) WidgetDrawing.ApplyStyle(ref cell, _style);
        frame.Buffer.SetFast(x, y, cell);
    }

    private static TimeSpan SatSub(TimeSpan a, TimeSpan b)
        => a > b ? a - b : TimeSpan.Zero;

    private static TimeSpan SaturatingAdd(TimeSpan a, TimeSpan b)
    {
        // Overflow guard matching Rust saturating_add
        try { return a + b; }
        catch { return TimeSpan.MaxValue; }
    }

    private static ushort SatSub16(ushort a, ushort b)
        => a >= b ? (ushort)(a - b) : (ushort)0;

    /// <summary>Overlay style over base: overlay's non-null fields win (matches Rust Style::merge).</summary>
    private static WidgetStyle MergeStyles(WidgetStyle overlay, WidgetStyle baseStyle) => new(
        overlay.Fg    ?? baseStyle.Fg,
        overlay.Bg    ?? baseStyle.Bg,
        overlay.Attrs ?? baseStyle.Attrs
    );
}
