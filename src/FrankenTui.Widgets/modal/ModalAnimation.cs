// Port of .external/frankentui/crates/ftui-widgets/src/modal/animation.rs
// Modal animation system for entrance, exit, and backdrop transitions.

namespace FrankenTui.Widgets.Modal;

// ============================================================================
// Animation Phase
// ============================================================================

/// <summary>
/// Current phase of the modal animation lifecycle.
///
/// State machine: Closed → Opening → Open → Closing → Closed
///
/// Rapid toggling can skip phases (e.g., Opening → Closing directly).
/// </summary>
public enum ModalAnimationPhase
{
    /// <summary>Modal is fully closed and invisible.</summary>
    Closed,
    /// <summary>Modal is animating in (scale-up, fade-in).</summary>
    Opening,
    /// <summary>Modal is fully open and visible.</summary>
    Open,
    /// <summary>Modal is animating out (scale-down, fade-out).</summary>
    Closing,
}

/// <summary>Extension methods for <see cref="ModalAnimationPhase"/>.</summary>
public static class ModalAnimationPhaseExtensions
{
    /// <summary>Check if the modal should be rendered.</summary>
    public static bool IsVisible(this ModalAnimationPhase phase)
        => phase != ModalAnimationPhase.Closed;

    /// <summary>Check if animation is in progress.</summary>
    public static bool IsAnimating(this ModalAnimationPhase phase)
        => phase == ModalAnimationPhase.Opening || phase == ModalAnimationPhase.Closing;
}

// ============================================================================
// Entrance Animation Types
// ============================================================================

/// <summary>Entrance animation type for modal content.</summary>
public enum ModalEntranceAnimation
{
    /// <summary>Scale up from center (classic modal pop).</summary>
    ScaleIn,
    /// <summary>Fade in (opacity only, no scale).</summary>
    FadeIn,
    /// <summary>Slide down from top with fade.</summary>
    SlideDown,
    /// <summary>Slide up from bottom with fade.</summary>
    SlideUp,
    /// <summary>No animation (instant appear).</summary>
    None,
}

/// <summary>Extension methods for <see cref="ModalEntranceAnimation"/>.</summary>
public static class ModalEntranceAnimationExtensions
{
    /// <summary>
    /// Get the initial scale factor for this animation.
    ///
    /// Returns a scale in [0.0, 1.0] where 1.0 = full size.
    /// </summary>
    public static double InitialScale(this ModalEntranceAnimation anim, ModalAnimationConfig config)
        => anim switch
        {
            ModalEntranceAnimation.ScaleIn => config.MinScale,
            _ => 1.0,
        };

    /// <summary>Get the initial opacity for this animation.</summary>
    public static double InitialOpacity(this ModalEntranceAnimation anim)
        => anim switch
        {
            ModalEntranceAnimation.None => 1.0,
            _ => 0.0,
        };

    /// <summary>Get the initial Y offset in cells for this animation.</summary>
    public static short InitialYOffset(this ModalEntranceAnimation anim, ushort modalHeight)
        => anim switch
        {
            ModalEntranceAnimation.SlideDown => (short)(-(short)Math.Min((int)modalHeight, 8)),
            ModalEntranceAnimation.SlideUp   => (short)Math.Min((int)modalHeight, 8),
            _                                => 0,
        };

    /// <summary>Calculate scale at a given eased progress (0.0 to 1.0).</summary>
    public static double ScaleAtProgress(this ModalEntranceAnimation anim, double progress, ModalAnimationConfig config)
    {
        double initial = anim.InitialScale(config);
        double p = Math.Clamp(progress, 0.0, 1.0);
        return initial + (1.0 - initial) * p;
    }

    /// <summary>Calculate opacity at a given eased progress (0.0 to 1.0).</summary>
    public static double OpacityAtProgress(this ModalEntranceAnimation anim, double progress)
    {
        double initial = anim.InitialOpacity();
        double p = Math.Clamp(progress, 0.0, 1.0);
        return initial + (1.0 - initial) * p;
    }

    /// <summary>Calculate Y offset at a given eased progress (0.0 to 1.0).</summary>
    public static short YOffsetAtProgress(this ModalEntranceAnimation anim, double progress, ushort modalHeight)
    {
        short initial = anim.InitialYOffset(modalHeight);
        double p = Math.Clamp(progress, 0.0, 1.0);
        double inv = 1.0 - p;
        return (short)Math.Round(initial * inv);
    }
}

// ============================================================================
// Exit Animation Types
// ============================================================================

/// <summary>Exit animation type for modal content.</summary>
public enum ModalExitAnimation
{
    /// <summary>Scale down to center (reverse of ScaleIn).</summary>
    ScaleOut,
    /// <summary>Fade out (opacity only, no scale).</summary>
    FadeOut,
    /// <summary>Slide up with fade.</summary>
    SlideUp,
    /// <summary>Slide down with fade.</summary>
    SlideDown,
    /// <summary>No animation (instant disappear).</summary>
    None,
}

/// <summary>Extension methods for <see cref="ModalExitAnimation"/>.</summary>
public static class ModalExitAnimationExtensions
{
    /// <summary>Get the final scale factor for this animation.</summary>
    public static double FinalScale(this ModalExitAnimation anim, ModalAnimationConfig config)
        => anim switch
        {
            ModalExitAnimation.ScaleOut => config.MinScale,
            _ => 1.0,
        };

    /// <summary>Get the final opacity for this animation.</summary>
    public static double FinalOpacity(this ModalExitAnimation anim)
        => 0.0; // All exit animations end at opacity 0, including None (modal is closing)

    /// <summary>Get the final Y offset in cells for this animation.</summary>
    public static short FinalYOffset(this ModalExitAnimation anim, ushort modalHeight)
        => anim switch
        {
            ModalExitAnimation.SlideUp   => (short)(-(short)Math.Min((int)modalHeight, 8)),
            ModalExitAnimation.SlideDown => (short)Math.Min((int)modalHeight, 8),
            _                            => 0,
        };

    /// <summary>
    /// Calculate scale at a given eased progress (0.0 to 1.0).
    ///
    /// Progress 0.0 = full size, 1.0 = final (shrunken).
    /// </summary>
    public static double ScaleAtProgress(this ModalExitAnimation anim, double progress, ModalAnimationConfig config)
    {
        double finalScale = anim.FinalScale(config);
        double p = Math.Clamp(progress, 0.0, 1.0);
        return 1.0 - (1.0 - finalScale) * p;
    }

    /// <summary>Calculate opacity at a given eased progress (0.0 to 1.0).</summary>
    public static double OpacityAtProgress(this ModalExitAnimation anim, double progress)
    {
        double p = Math.Clamp(progress, 0.0, 1.0);
        return 1.0 - p;
    }

    /// <summary>Calculate Y offset at a given eased progress (0.0 to 1.0).</summary>
    public static short YOffsetAtProgress(this ModalExitAnimation anim, double progress, ushort modalHeight)
    {
        short finalOffset = anim.FinalYOffset(modalHeight);
        double p = Math.Clamp(progress, 0.0, 1.0);
        return (short)Math.Round(finalOffset * p);
    }
}

// ============================================================================
// Easing Functions
// ============================================================================

/// <summary>
/// Easing function for modal animations.
///
/// Simplified subset of easing curves for modal animations.
/// For the full set, see <c>ftui_extras::text_effects::Easing</c>.
/// </summary>
public enum ModalEasing
{
    // DIVERGENCE: Rust places Linear first in source order but uses #[default] on EaseOut,
    // making EaseOut the default variant. C# enum defaults are value-0. To faithfully
    // replicate upstream's default(ModalEasing) == EaseOut, EaseOut is placed first (= 0).
    // All switch/match arms are exhaustive so numeric order does not affect runtime behavior.

    /// <summary>Smooth ease-out (decelerating) - good for entrances. Default variant.</summary>
    EaseOut,
    /// <summary>Linear interpolation.</summary>
    Linear,
    /// <summary>Smooth ease-in (accelerating) - good for exits.</summary>
    EaseIn,
    /// <summary>Smooth S-curve - good for general transitions.</summary>
    EaseInOut,
    /// <summary>Slight overshoot then settle - bouncy feel.</summary>
    Back,
}

/// <summary>Extension methods for <see cref="ModalEasing"/>.</summary>
public static class ModalEasingExtensions
{
    /// <summary>Apply the easing function to a progress value (0.0 to 1.0).</summary>
    public static double Apply(this ModalEasing easing, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        switch (easing)
        {
            case ModalEasing.Linear:
                return t;
            case ModalEasing.EaseOut:
            {
                double inv = 1.0 - t;
                return 1.0 - inv * inv * inv;
            }
            case ModalEasing.EaseIn:
                return t * t * t;
            case ModalEasing.EaseInOut:
            {
                if (t < 0.5)
                    return 4.0 * t * t * t;
                double inv2 = -2.0 * t + 2.0;
                return 1.0 - inv2 * inv2 * inv2 / 2.0;
            }
            case ModalEasing.Back:
            {
                // Back ease-out: slight overshoot then settle
                const double c1 = 1.70158;
                const double c3 = c1 + 1.0;
                double tMinus1 = t - 1.0;
                return 1.0 + c3 * tMinus1 * tMinus1 * tMinus1 + c1 * tMinus1 * tMinus1;
            }
            default:
                return t;
        }
    }

    /// <summary>Check if this easing can produce values outside 0.0-1.0.</summary>
    public static bool CanOvershoot(this ModalEasing easing)
        => easing == ModalEasing.Back;
}

// ============================================================================
// Animation Configuration
// ============================================================================

/// <summary>Animation configuration for modals.</summary>
public sealed class ModalAnimationConfig
{
    /// <summary>Entrance animation type.</summary>
    public ModalEntranceAnimation Entrance { get; set; }

    /// <summary>Exit animation type.</summary>
    public ModalExitAnimation Exit { get; set; }

    /// <summary>Duration of entrance animation.</summary>
    public TimeSpan EntranceDuration { get; set; }

    /// <summary>Duration of exit animation.</summary>
    public TimeSpan ExitDuration { get; set; }

    /// <summary>Easing function for entrance.</summary>
    public ModalEasing EntranceEasing { get; set; }

    /// <summary>Easing function for exit.</summary>
    public ModalEasing ExitEasing { get; set; }

    /// <summary>Minimum scale for scale animations (typically 0.9-0.95).</summary>
    public double MinScale { get; set; }

    /// <summary>Whether backdrop should animate independently.</summary>
    public bool AnimateBackdrop { get; set; }

    /// <summary>Backdrop fade-in duration (can differ from content).</summary>
    public TimeSpan BackdropDuration { get; set; }

    /// <summary>Whether to respect reduced-motion preference.</summary>
    public bool RespectReducedMotion { get; set; }

    /// <summary>Create a new default configuration.</summary>
    public ModalAnimationConfig()
    {
        Entrance         = ModalEntranceAnimation.ScaleIn;
        Exit             = ModalExitAnimation.ScaleOut;
        EntranceDuration = TimeSpan.FromMilliseconds(200);
        ExitDuration     = TimeSpan.FromMilliseconds(150);
        EntranceEasing   = ModalEasing.EaseOut;
        ExitEasing       = ModalEasing.EaseIn;
        MinScale         = 0.92;
        AnimateBackdrop  = true;
        BackdropDuration = TimeSpan.FromMilliseconds(150);
        RespectReducedMotion = true;
    }

    private ModalAnimationConfig(ModalAnimationConfig other)
    {
        Entrance             = other.Entrance;
        Exit                 = other.Exit;
        EntranceDuration     = other.EntranceDuration;
        ExitDuration         = other.ExitDuration;
        EntranceEasing       = other.EntranceEasing;
        ExitEasing           = other.ExitEasing;
        MinScale             = other.MinScale;
        AnimateBackdrop      = other.AnimateBackdrop;
        BackdropDuration     = other.BackdropDuration;
        RespectReducedMotion = other.RespectReducedMotion;
    }

    /// <summary>Create a new default configuration.</summary>
    public static ModalAnimationConfig New() => new();

    /// <summary>Create a configuration with no animations.</summary>
    public static ModalAnimationConfig None()
    {
        var cfg = new ModalAnimationConfig
        {
            Entrance         = ModalEntranceAnimation.None,
            Exit             = ModalExitAnimation.None,
            EntranceDuration = TimeSpan.Zero,
            ExitDuration     = TimeSpan.Zero,
            BackdropDuration = TimeSpan.Zero,
        };
        return cfg;
    }

    /// <summary>
    /// Create a configuration for reduced motion preference.
    ///
    /// Uses fade only (no scale/slide) with shorter durations.
    /// </summary>
    public static ModalAnimationConfig ReducedMotion() => new()
    {
        Entrance             = ModalEntranceAnimation.FadeIn,
        Exit                 = ModalExitAnimation.FadeOut,
        EntranceDuration     = TimeSpan.FromMilliseconds(100),
        ExitDuration         = TimeSpan.FromMilliseconds(100),
        EntranceEasing       = ModalEasing.Linear,
        ExitEasing           = ModalEasing.Linear,
        MinScale             = 1.0,
        AnimateBackdrop      = true,
        BackdropDuration     = TimeSpan.FromMilliseconds(100),
        RespectReducedMotion = true,
    };

    /// <summary>Set entrance animation type.</summary>
    public ModalAnimationConfig WithEntrance(ModalEntranceAnimation anim)
    {
        var cfg = Clone();
        cfg.Entrance = anim;
        return cfg;
    }

    /// <summary>Set exit animation type.</summary>
    public ModalAnimationConfig WithExit(ModalExitAnimation anim)
    {
        var cfg = Clone();
        cfg.Exit = anim;
        return cfg;
    }

    /// <summary>Set entrance duration.</summary>
    public ModalAnimationConfig WithEntranceDuration(TimeSpan duration)
    {
        var cfg = Clone();
        cfg.EntranceDuration = duration;
        return cfg;
    }

    /// <summary>Set exit duration.</summary>
    public ModalAnimationConfig WithExitDuration(TimeSpan duration)
    {
        var cfg = Clone();
        cfg.ExitDuration = duration;
        return cfg;
    }

    /// <summary>Set entrance easing function.</summary>
    public ModalAnimationConfig WithEntranceEasing(ModalEasing easing)
    {
        var cfg = Clone();
        cfg.EntranceEasing = easing;
        return cfg;
    }

    /// <summary>Set exit easing function.</summary>
    public ModalAnimationConfig WithExitEasing(ModalEasing easing)
    {
        var cfg = Clone();
        cfg.ExitEasing = easing;
        return cfg;
    }

    /// <summary>Set minimum scale for scale animations.</summary>
    public ModalAnimationConfig WithMinScale(double scale)
    {
        var cfg = Clone();
        cfg.MinScale = Math.Clamp(scale, 0.5, 1.0);
        return cfg;
    }

    /// <summary>Set whether backdrop should animate.</summary>
    public ModalAnimationConfig WithAnimateBackdrop(bool animate)
    {
        var cfg = Clone();
        cfg.AnimateBackdrop = animate;
        return cfg;
    }

    /// <summary>Set backdrop fade duration.</summary>
    public ModalAnimationConfig WithBackdropDuration(TimeSpan duration)
    {
        var cfg = Clone();
        cfg.BackdropDuration = duration;
        return cfg;
    }

    /// <summary>Set whether to respect reduced-motion preference.</summary>
    public ModalAnimationConfig WithRespectReducedMotion(bool respect)
    {
        var cfg = Clone();
        cfg.RespectReducedMotion = respect;
        return cfg;
    }

    /// <summary>Check if animations are effectively disabled.</summary>
    public bool IsDisabled()
        => Entrance == ModalEntranceAnimation.None && Exit == ModalExitAnimation.None;

    /// <summary>Get the effective config, applying reduced motion if needed.</summary>
    public ModalAnimationConfig Effective(bool reducedMotion)
        => reducedMotion && RespectReducedMotion ? ReducedMotion() : Clone();

    /// <summary>Return a deep copy of this config.</summary>
    public ModalAnimationConfig Clone() => new(this);
}

// ============================================================================
// Animation State
// ============================================================================

/// <summary>
/// Current animation state for a modal.
///
/// Tracks progress through open/close animations and computes
/// interpolated values for scale, opacity, and position offset.
/// </summary>
public sealed class ModalAnimationState
{
    /// <summary>Current animation phase.</summary>
    private ModalAnimationPhase _phase;

    /// <summary>Progress within current phase (0.0 to 1.0).</summary>
    private double _progress;

    /// <summary>Backdrop animation progress (may differ from content).</summary>
    private double _backdropProgress;

    /// <summary>Whether reduced motion is enabled.</summary>
    private bool _reducedMotion;

    /// <summary>Create a new animation state (closed, no animation).</summary>
    public ModalAnimationState()
    {
        _phase           = ModalAnimationPhase.Closed;
        _progress        = 0.0;
        _backdropProgress = 0.0;
        _reducedMotion   = false;
    }

    private ModalAnimationState(ModalAnimationState other)
    {
        _phase            = other._phase;
        _progress         = other._progress;
        _backdropProgress = other._backdropProgress;
        _reducedMotion    = other._reducedMotion;
    }

    /// <summary>Create a state that starts fully open (for testing or instant open).</summary>
    public static ModalAnimationState Open()
    {
        var s = new ModalAnimationState();
        s._phase            = ModalAnimationPhase.Open;
        s._progress         = 1.0;
        s._backdropProgress = 1.0;
        s._reducedMotion    = false;
        return s;
    }

    /// <summary>Get the current phase.</summary>
    public ModalAnimationPhase Phase() => _phase;

    /// <summary>Get the raw progress value (0.0 to 1.0).</summary>
    public double Progress() => _progress;

    /// <summary>Get the backdrop progress value (0.0 to 1.0).</summary>
    public double BackdropProgress() => _backdropProgress;

    /// <summary>Check if the modal is visible (should be rendered).</summary>
    public bool IsVisible() => _phase.IsVisible();

    /// <summary>Check if animation is in progress.</summary>
    public bool IsAnimating() => _phase.IsAnimating();

    /// <summary>Check if the modal is fully open.</summary>
    public bool IsOpen() => _phase == ModalAnimationPhase.Open;

    /// <summary>Check if the modal is fully closed.</summary>
    public bool IsClosed() => _phase == ModalAnimationPhase.Closed;

    /// <summary>Set reduced motion preference.</summary>
    public void SetReducedMotion(bool enabled) => _reducedMotion = enabled;

    /// <summary>
    /// Start opening animation.
    ///
    /// If already opening or open, this is a no-op.
    /// If closing, reverses direction and preserves momentum.
    /// </summary>
    public void StartOpening()
    {
        switch (_phase)
        {
            case ModalAnimationPhase.Closed:
                _phase            = ModalAnimationPhase.Opening;
                _progress         = 0.0;
                _backdropProgress = 0.0;
                break;
            case ModalAnimationPhase.Closing:
                // Reverse animation, preserving progress
                _phase = ModalAnimationPhase.Opening;
                // Invert progress: if we were 30% through closing, start at 70% open
                _progress         = 1.0 - _progress;
                _backdropProgress = 1.0 - _backdropProgress;
                break;
            case ModalAnimationPhase.Opening:
            case ModalAnimationPhase.Open:
                // Already opening or open, nothing to do
                break;
        }
    }

    /// <summary>
    /// Start closing animation.
    ///
    /// If already closing or closed, this is a no-op.
    /// If opening, reverses direction and preserves momentum.
    /// </summary>
    public void StartClosing()
    {
        switch (_phase)
        {
            case ModalAnimationPhase.Open:
                _phase            = ModalAnimationPhase.Closing;
                _progress         = 0.0;
                _backdropProgress = 0.0;
                break;
            case ModalAnimationPhase.Opening:
                // Reverse animation, preserving progress
                _phase = ModalAnimationPhase.Closing;
                // Invert progress
                _progress         = 1.0 - _progress;
                _backdropProgress = 1.0 - _backdropProgress;
                break;
            case ModalAnimationPhase.Closing:
            case ModalAnimationPhase.Closed:
                // Already closing or closed, nothing to do
                break;
        }
    }

    /// <summary>Force the modal to be fully open (skip animation).</summary>
    public void ForceOpen()
    {
        _phase            = ModalAnimationPhase.Open;
        _progress         = 1.0;
        _backdropProgress = 1.0;
    }

    /// <summary>Force the modal to be fully closed (skip animation).</summary>
    public void ForceClose()
    {
        _phase            = ModalAnimationPhase.Closed;
        _progress         = 0.0;
        _backdropProgress = 0.0;
    }

    /// <summary>
    /// Advance the animation by the given delta time.
    ///
    /// Returns <c>true</c> if the animation phase changed (e.g., Opening → Open).
    /// </summary>
    public bool Tick(TimeSpan delta, ModalAnimationConfig config)
    {
        double deltaSecs = Math.Max(delta.TotalSeconds, 0.0);
        var eff = config.Effective(_reducedMotion);

        switch (_phase)
        {
            case ModalAnimationPhase.Opening:
            {
                double contentDuration  = eff.EntranceDuration.TotalSeconds;
                double backdropDuration = eff.AnimateBackdrop ? eff.BackdropDuration.TotalSeconds : 0.0;

                // Advance content progress
                if (contentDuration > 0.0)
                    _progress += deltaSecs / contentDuration;
                else
                    _progress = 1.0;

                // Advance backdrop progress
                if (backdropDuration > 0.0)
                    _backdropProgress += deltaSecs / backdropDuration;
                else
                    _backdropProgress = 1.0;

                // Clamp and check for completion
                _progress         = Math.Min(_progress, 1.0);
                _backdropProgress = Math.Min(_backdropProgress, 1.0);

                if (_progress >= 1.0 && _backdropProgress >= 1.0)
                {
                    _phase            = ModalAnimationPhase.Open;
                    _progress         = 1.0;
                    _backdropProgress = 1.0;
                    return true;
                }
                break;
            }
            case ModalAnimationPhase.Closing:
            {
                double contentDuration  = eff.ExitDuration.TotalSeconds;
                double backdropDuration = eff.AnimateBackdrop ? eff.BackdropDuration.TotalSeconds : 0.0;

                // Advance content progress
                if (contentDuration > 0.0)
                    _progress += deltaSecs / contentDuration;
                else
                    _progress = 1.0;

                // Advance backdrop progress
                if (backdropDuration > 0.0)
                    _backdropProgress += deltaSecs / backdropDuration;
                else
                    _backdropProgress = 1.0;

                // Clamp and check for completion
                _progress         = Math.Min(_progress, 1.0);
                _backdropProgress = Math.Min(_backdropProgress, 1.0);

                if (_progress >= 1.0 && _backdropProgress >= 1.0)
                {
                    _phase            = ModalAnimationPhase.Closed;
                    _progress         = 0.0;
                    _backdropProgress = 0.0;
                    return true;
                }
                break;
            }
            case ModalAnimationPhase.Open:
            case ModalAnimationPhase.Closed:
                // No animation in progress
                break;
        }

        return false;
    }

    /// <summary>Get the current eased progress for content animation.</summary>
    public double EasedProgress(ModalAnimationConfig config)
    {
        var eff = config.Effective(_reducedMotion);
        return _phase switch
        {
            ModalAnimationPhase.Opening => eff.EntranceEasing.Apply(_progress),
            ModalAnimationPhase.Closing => eff.ExitEasing.Apply(_progress),
            ModalAnimationPhase.Open    => 1.0,
            ModalAnimationPhase.Closed  => 0.0,
            _                           => 0.0,
        };
    }

    /// <summary>Get the current eased progress for backdrop animation.</summary>
    public double EasedBackdropProgress(ModalAnimationConfig config)
    {
        // Backdrop always uses EaseOut for fade-in and EaseIn for fade-out
        return _phase switch
        {
            ModalAnimationPhase.Opening => ModalEasing.EaseOut.Apply(_backdropProgress),
            ModalAnimationPhase.Closing => ModalEasing.EaseIn.Apply(_backdropProgress),
            ModalAnimationPhase.Open    => 1.0,
            ModalAnimationPhase.Closed  => 0.0,
            _                           => 0.0,
        };
    }

    /// <summary>
    /// Get the current scale factor for the modal content.
    ///
    /// Returns a value in [min_scale, 1.0].
    /// </summary>
    public double CurrentScale(ModalAnimationConfig config)
    {
        var eff   = config.Effective(_reducedMotion);
        double ep = EasedProgress(eff);

        return _phase switch
        {
            ModalAnimationPhase.Opening => eff.Entrance.ScaleAtProgress(ep, eff),
            ModalAnimationPhase.Closing => eff.Exit.ScaleAtProgress(ep, eff),
            ModalAnimationPhase.Open    => 1.0,
            ModalAnimationPhase.Closed  => eff.Entrance.InitialScale(eff),
            _                           => 1.0,
        };
    }

    /// <summary>
    /// Get the current opacity for the modal content.
    ///
    /// Returns a value in [0.0, 1.0].
    /// </summary>
    public double CurrentOpacity(ModalAnimationConfig config)
    {
        var eff   = config.Effective(_reducedMotion);
        double ep = EasedProgress(eff);

        return _phase switch
        {
            ModalAnimationPhase.Opening => eff.Entrance.OpacityAtProgress(ep),
            ModalAnimationPhase.Closing => eff.Exit.OpacityAtProgress(ep),
            ModalAnimationPhase.Open    => 1.0,
            ModalAnimationPhase.Closed  => 0.0,
            _                           => 0.0,
        };
    }

    /// <summary>
    /// Get the current backdrop opacity.
    ///
    /// Returns a value in [0.0, 1.0] to be multiplied with the backdrop's configured opacity.
    /// </summary>
    public double CurrentBackdropOpacity(ModalAnimationConfig config)
    {
        var eff = config.Effective(_reducedMotion);

        if (!eff.AnimateBackdrop)
        {
            return _phase switch
            {
                ModalAnimationPhase.Open    => 1.0,
                ModalAnimationPhase.Opening => 1.0,
                ModalAnimationPhase.Closed  => 0.0,
                ModalAnimationPhase.Closing => 0.0,
                _                           => 0.0,
            };
        }

        double eased = EasedBackdropProgress(eff);

        return _phase switch
        {
            ModalAnimationPhase.Opening => eased,
            ModalAnimationPhase.Closing => 1.0 - eased,
            ModalAnimationPhase.Open    => 1.0,
            ModalAnimationPhase.Closed  => 0.0,
            _                           => 0.0,
        };
    }

    /// <summary>
    /// Get the current Y offset for the modal content.
    ///
    /// Returns an offset in cells (negative = above final position).
    /// </summary>
    public short CurrentYOffset(ModalAnimationConfig config, ushort modalHeight)
    {
        var eff   = config.Effective(_reducedMotion);
        double ep = EasedProgress(eff);

        return _phase switch
        {
            ModalAnimationPhase.Opening => eff.Entrance.YOffsetAtProgress(ep, modalHeight),
            ModalAnimationPhase.Closing => eff.Exit.YOffsetAtProgress(ep, modalHeight),
            ModalAnimationPhase.Open    => 0,
            ModalAnimationPhase.Closed  => 0,
            _                           => 0,
        };
    }

    /// <summary>
    /// Get all current animation values at once.
    ///
    /// Returns (scale, opacity, backdrop_opacity, y_offset).
    /// </summary>
    public (double Scale, double Opacity, double BackdropOpacity, short YOffset) CurrentValues(
        ModalAnimationConfig config,
        ushort modalHeight)
    {
        return (
            CurrentScale(config),
            CurrentOpacity(config),
            CurrentBackdropOpacity(config),
            CurrentYOffset(config, modalHeight)
        );
    }

    /// <summary>Return a deep copy of this state.</summary>
    public ModalAnimationState Clone() => new(this);

    /// <summary>Returns a debug-style string representation.</summary>
    public override string ToString()
        => $"ModalAnimationState {{ Phase={_phase}, Progress={_progress}, BackdropProgress={_backdropProgress}, ReducedMotion={_reducedMotion} }}";
}
