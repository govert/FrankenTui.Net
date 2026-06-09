// Upstream source: crates/ftui-widgets/src/modal/animation.rs (tests module)
// Full 1-1 port of all upstream animation tests.

using FrankenTui.Widgets.Modal;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class ModalAnimationTests
{
    // -------------------------------------------------------------------------
    // Phase Transitions
    // -------------------------------------------------------------------------

    [Fact]
    public void TestPhaseVisibility()
    {
        Assert.False(ModalAnimationPhase.Closed.IsVisible());
        Assert.True(ModalAnimationPhase.Opening.IsVisible());
        Assert.True(ModalAnimationPhase.Open.IsVisible());
        Assert.True(ModalAnimationPhase.Closing.IsVisible());
    }

    [Fact]
    public void TestPhaseAnimating()
    {
        Assert.False(ModalAnimationPhase.Closed.IsAnimating());
        Assert.True(ModalAnimationPhase.Opening.IsAnimating());
        Assert.False(ModalAnimationPhase.Open.IsAnimating());
        Assert.True(ModalAnimationPhase.Closing.IsAnimating());
    }

    [Fact]
    public void TestStartOpeningFromClosed()
    {
        var state = new ModalAnimationState();
        Assert.Equal(ModalAnimationPhase.Closed, state.Phase());

        state.StartOpening();
        Assert.Equal(ModalAnimationPhase.Opening, state.Phase());
        Assert.Equal(0.0, state.Progress());
    }

    [Fact]
    public void TestStartClosingFromOpen()
    {
        var state = ModalAnimationState.Open();
        Assert.Equal(ModalAnimationPhase.Open, state.Phase());

        state.StartClosing();
        Assert.Equal(ModalAnimationPhase.Closing, state.Phase());
        Assert.Equal(0.0, state.Progress());
    }

    [Fact]
    public void TestRapidToggleReversesAnimation()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        // Start opening
        state.StartOpening();
        state.Tick(TimeSpan.FromMilliseconds(100), config); // 50% through 200ms

        double openingProgress = state.Progress();
        Assert.True(openingProgress > 0.0);
        Assert.True(openingProgress < 1.0);

        // Quickly close - should reverse
        state.StartClosing();
        Assert.Equal(ModalAnimationPhase.Closing, state.Phase());

        // Progress should be inverted: if we were 50% open, we're now 50% closed
        double closingProgress = state.Progress();
        Assert.True(Math.Abs(openingProgress + closingProgress - 1.0) < 0.001);
    }

    [Fact]
    public void TestOpeningNoopWhenAlreadyOpening()
    {
        var state = new ModalAnimationState();
        state.StartOpening();
        double progress1 = state.Progress();

        state.StartOpening(); // Should be no-op
        Assert.Equal(progress1, state.Progress());
        Assert.Equal(ModalAnimationPhase.Opening, state.Phase());
    }

    // -------------------------------------------------------------------------
    // Animation Progress
    // -------------------------------------------------------------------------

    [Fact]
    public void TestTickAdvancesProgress()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        state.StartOpening();
        Assert.Equal(0.0, state.Progress());

        state.Tick(TimeSpan.FromMilliseconds(100), config);
        Assert.True(state.Progress() > 0.0);
        Assert.True(state.Progress() < 1.0);
    }

    [Fact]
    public void TestTickCompletesAnimation()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        state.StartOpening();
        bool changed = state.Tick(TimeSpan.FromMilliseconds(500), config);

        Assert.True(changed);
        Assert.Equal(ModalAnimationPhase.Open, state.Phase());
        Assert.Equal(1.0, state.Progress());
    }

    [Fact]
    public void TestZeroDurationCompletesInstantly()
    {
        var state  = new ModalAnimationState();
        var config = ModalAnimationConfig.None();

        state.StartOpening();
        bool changed = state.Tick(TimeSpan.FromMilliseconds(1), config);

        Assert.True(changed);
        Assert.Equal(ModalAnimationPhase.Open, state.Phase());
    }

    // -------------------------------------------------------------------------
    // Easing
    // -------------------------------------------------------------------------

    [Fact]
    public void TestEasingLinear()
    {
        Assert.Equal(0.0, ModalEasing.Linear.Apply(0.0));
        Assert.Equal(0.5, ModalEasing.Linear.Apply(0.5));
        Assert.Equal(1.0, ModalEasing.Linear.Apply(1.0));
    }

    [Fact]
    public void TestEasingClampsInput()
    {
        Assert.Equal(0.0, ModalEasing.Linear.Apply(-0.5));
        Assert.Equal(1.0, ModalEasing.Linear.Apply(1.5));
    }

    [Fact]
    public void TestEasingEaseOutDecelerates()
    {
        // EaseOut should be > linear at 0.5 (faster start, slower end)
        double linear  = ModalEasing.Linear.Apply(0.5);
        double easeOut = ModalEasing.EaseOut.Apply(0.5);
        Assert.True(easeOut > linear);
    }

    [Fact]
    public void TestEasingEaseInAccelerates()
    {
        // EaseIn should be < linear at 0.5 (slower start, faster end)
        double linear = ModalEasing.Linear.Apply(0.5);
        double easeIn = ModalEasing.EaseIn.Apply(0.5);
        Assert.True(easeIn < linear);
    }

    // -------------------------------------------------------------------------
    // Animation Values
    // -------------------------------------------------------------------------

    [Fact]
    public void TestScaleDuringOpening()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        // At start (closed)
        double scale = state.CurrentScale(config);
        Assert.True(Math.Abs(scale - config.MinScale) < 0.001);

        // During opening
        state.StartOpening();
        state.Tick(TimeSpan.FromMilliseconds(100), config);
        double midScale = state.CurrentScale(config);
        Assert.True(midScale > config.MinScale);
        Assert.True(midScale < 1.0);

        // At end (open)
        state.Tick(TimeSpan.FromMilliseconds(500), config);
        double finalScale = state.CurrentScale(config);
        Assert.True(Math.Abs(finalScale - 1.0) < 0.001);
    }

    [Fact]
    public void TestOpacityDuringClosing()
    {
        var state  = ModalAnimationState.Open();
        var config = new ModalAnimationConfig();

        // At start (open)
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 1.0) < 0.001);

        // During closing
        state.StartClosing();
        state.Tick(TimeSpan.FromMilliseconds(75), config);
        double midOpacity = state.CurrentOpacity(config);
        Assert.True(midOpacity > 0.0);
        Assert.True(midOpacity < 1.0);

        // At end (closed)
        state.Tick(TimeSpan.FromMilliseconds(500), config);
        double finalOpacity = state.CurrentOpacity(config);
        Assert.True(Math.Abs(finalOpacity - 0.0) < 0.001);
    }

    [Fact]
    public void TestBackdropOpacityIndependent()
    {
        var state = new ModalAnimationState();
        var config = new ModalAnimationConfig()
            .WithEntranceDuration(TimeSpan.FromMilliseconds(200))
            .WithBackdropDuration(TimeSpan.FromMilliseconds(100));

        state.StartOpening();

        // After 100ms, backdrop should be at 100% but content still animating
        state.Tick(TimeSpan.FromMilliseconds(100), config);

        double contentOpacity  = state.CurrentOpacity(config);
        double backdropOpacity = state.CurrentBackdropOpacity(config);

        // Backdrop animates faster, so should be closer to 1.0
        Assert.True(backdropOpacity > contentOpacity);
    }

    // -------------------------------------------------------------------------
    // Reduced Motion
    // -------------------------------------------------------------------------

    [Fact]
    public void TestReducedMotionConfig()
    {
        var config = ModalAnimationConfig.ReducedMotion();

        Assert.Equal(ModalEntranceAnimation.FadeIn, config.Entrance);
        Assert.Equal(ModalExitAnimation.FadeOut, config.Exit);
        Assert.True(Math.Abs(config.MinScale - 1.0) < 0.001); // No scale
    }

    [Fact]
    public void TestReducedMotionAppliesEffectiveConfig()
    {
        var state = new ModalAnimationState();
        state.SetReducedMotion(true);

        var config = new ModalAnimationConfig();

        state.StartOpening();
        double scale = state.CurrentScale(config);

        // With reduced motion, scale should be 1.0 (no scale animation)
        Assert.True(Math.Abs(scale - 1.0) < 0.001);
    }

    // -------------------------------------------------------------------------
    // Force Open/Close
    // -------------------------------------------------------------------------

    [Fact]
    public void TestForceOpen()
    {
        var state = new ModalAnimationState();
        state.ForceOpen();

        Assert.Equal(ModalAnimationPhase.Open, state.Phase());
        Assert.Equal(1.0, state.Progress());
        Assert.Equal(1.0, state.BackdropProgress());
    }

    [Fact]
    public void TestForceClose()
    {
        var state = ModalAnimationState.Open();
        state.ForceClose();

        Assert.Equal(ModalAnimationPhase.Closed, state.Phase());
        Assert.Equal(0.0, state.Progress());
        Assert.Equal(0.0, state.BackdropProgress());
    }

    // -------------------------------------------------------------------------
    // Entrance/Exit Animation Types
    // -------------------------------------------------------------------------

    [Fact]
    public void TestScaleInInitialScale()
    {
        var config  = new ModalAnimationConfig();
        double initial = ModalEntranceAnimation.ScaleIn.InitialScale(config);
        Assert.True(Math.Abs(initial - config.MinScale) < 0.001);
    }

    [Fact]
    public void TestFadeInNoScale()
    {
        var config  = new ModalAnimationConfig();
        double initial = ModalEntranceAnimation.FadeIn.InitialScale(config);
        Assert.True(Math.Abs(initial - 1.0) < 0.001);
    }

    [Fact]
    public void TestSlideDownYOffset()
    {
        short initial = ModalEntranceAnimation.SlideDown.InitialYOffset(20);
        Assert.True(initial < 0); // Above final position
    }

    [Fact]
    public void TestSlideUpYOffset()
    {
        short initial = ModalEntranceAnimation.SlideUp.InitialYOffset(20);
        Assert.True(initial > 0); // Below final position
    }

    // -------------------------------------------------------------------------
    // Invariants
    // -------------------------------------------------------------------------

    [Fact]
    public void TestProgressAlwaysInBounds()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        state.StartOpening();

        // Many ticks with large delta
        for (int i = 0; i < 100; i++)
        {
            state.Tick(TimeSpan.FromMilliseconds(100), config);
            Assert.True(state.Progress() >= 0.0);
            Assert.True(state.Progress() <= 1.0);
            Assert.True(state.BackdropProgress() >= 0.0);
            Assert.True(state.BackdropProgress() <= 1.0);
        }
    }

    [Fact]
    public void TestScaleAlwaysInBounds()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        state.StartOpening();

        for (int i = 0; i < 20; i++)
        {
            state.Tick(TimeSpan.FromMilliseconds(20), config);
            double scale = state.CurrentScale(config);
            Assert.True(
                scale >= config.MinScale,
                $"scale {scale} < min {config.MinScale} at step {i}");
            Assert.True(scale <= 1.0, $"scale {scale} > 1.0 at step {i}");
        }
    }

    [Fact]
    public void TestOpacityAlwaysInBounds()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();

        state.StartOpening();

        for (int i = 0; i < 20; i++)
        {
            state.Tick(TimeSpan.FromMilliseconds(20), config);
            double opacity = state.CurrentOpacity(config);
            Assert.True(opacity >= 0.0, $"opacity {opacity} < 0 at step {i}");
            Assert.True(opacity <= 1.0, $"opacity {opacity} > 1.0 at step {i}");
        }
    }

    // ---- Edge-case tests (bd-a4n4z) ----

    [Fact]
    public void EdgeEasingEaseInOutAtBoundary()
    {
        // At exactly 0.5 the branch flips
        double atHalf = ModalEasing.EaseInOut.Apply(0.5);
        Assert.True(
            Math.Abs(atHalf - 0.5) < 0.001,
            $"EaseInOut at 0.5 should be ~0.5, got {atHalf}");
        // Endpoints
        Assert.Equal(0.0, ModalEasing.EaseInOut.Apply(0.0));
        Assert.True(Math.Abs(ModalEasing.EaseInOut.Apply(1.0) - 1.0) < 1e-10);
    }

    [Fact]
    public void EdgeEasingBackOvershoots()
    {
        // Back easing overshoots 1.0 briefly then settles at 1.0
        // At midpoint it should overshoot
        double mid = ModalEasing.Back.Apply(0.5);
        // At t=0 and t=1
        Assert.True(Math.Abs(ModalEasing.Back.Apply(0.0)) < 1e-10);
        Assert.True(Math.Abs(ModalEasing.Back.Apply(1.0) - 1.0) < 1e-10);
        // Verify overshoot actually happens somewhere in (0, 1)
        bool foundOvershoot = false;
        for (int i = 1; i < 100; i++)
        {
            double t = i / 100.0;
            double v = ModalEasing.Back.Apply(t);
            if (v > 1.0)
            {
                foundOvershoot = true;
                break;
            }
        }
        Assert.True(
            foundOvershoot,
            $"Back easing should overshoot 1.0 at some point, mid={mid}");
    }

    [Fact]
    public void EdgeCanOvershootOnlyBack()
    {
        Assert.False(ModalEasing.Linear.CanOvershoot());
        Assert.False(ModalEasing.EaseOut.CanOvershoot());
        Assert.False(ModalEasing.EaseIn.CanOvershoot());
        Assert.False(ModalEasing.EaseInOut.CanOvershoot());
        Assert.True(ModalEasing.Back.CanOvershoot());
    }

    [Fact]
    public void EdgeEasingEaseInEndpoints()
    {
        Assert.Equal(0.0, ModalEasing.EaseIn.Apply(0.0));
        Assert.True(Math.Abs(ModalEasing.EaseIn.Apply(1.0) - 1.0) < 1e-10);
    }

    [Fact]
    public void EdgeEasingEaseOutEndpoints()
    {
        Assert.Equal(0.0, ModalEasing.EaseOut.Apply(0.0));
        Assert.True(Math.Abs(ModalEasing.EaseOut.Apply(1.0) - 1.0) < 1e-10);
    }

    [Fact]
    public void EdgeExitFinalScaleVariants()
    {
        var config = new ModalAnimationConfig();
        Assert.True(Math.Abs(ModalExitAnimation.ScaleOut.FinalScale(config) - config.MinScale) < 1e-10);
        Assert.True(Math.Abs(ModalExitAnimation.FadeOut.FinalScale(config) - 1.0) < 1e-10);
        Assert.True(Math.Abs(ModalExitAnimation.SlideUp.FinalScale(config) - 1.0) < 1e-10);
        Assert.True(Math.Abs(ModalExitAnimation.SlideDown.FinalScale(config) - 1.0) < 1e-10);
        Assert.True(Math.Abs(ModalExitAnimation.None.FinalScale(config) - 1.0) < 1e-10);
    }

    [Fact]
    public void EdgeExitFinalOpacityAllZero()
    {
        // All exit animations end at opacity 0
        Assert.Equal(0.0, ModalExitAnimation.ScaleOut.FinalOpacity());
        Assert.Equal(0.0, ModalExitAnimation.FadeOut.FinalOpacity());
        Assert.Equal(0.0, ModalExitAnimation.SlideUp.FinalOpacity());
        Assert.Equal(0.0, ModalExitAnimation.SlideDown.FinalOpacity());
        Assert.Equal(0.0, ModalExitAnimation.None.FinalOpacity());
    }

    [Fact]
    public void EdgeExitFinalYOffset()
    {
        Assert.True(ModalExitAnimation.SlideUp.FinalYOffset(20) < 0);
        Assert.True(ModalExitAnimation.SlideDown.FinalYOffset(20) > 0);
        Assert.Equal((short)0, ModalExitAnimation.ScaleOut.FinalYOffset(20));
        Assert.Equal((short)0, ModalExitAnimation.FadeOut.FinalYOffset(20));
        Assert.Equal((short)0, ModalExitAnimation.None.FinalYOffset(20));
    }

    [Fact]
    public void EdgeExitScaleAtProgress()
    {
        var config = new ModalAnimationConfig();
        // At progress 0, scale = 1.0 (fully open)
        double s0 = ModalExitAnimation.ScaleOut.ScaleAtProgress(0.0, config);
        Assert.True(Math.Abs(s0 - 1.0) < 1e-10);
        // At progress 1, scale = min_scale
        double s1 = ModalExitAnimation.ScaleOut.ScaleAtProgress(1.0, config);
        Assert.True(Math.Abs(s1 - config.MinScale) < 1e-10);
    }

    [Fact]
    public void EdgeExitOpacityAtProgress()
    {
        Assert.True(Math.Abs(ModalExitAnimation.FadeOut.OpacityAtProgress(0.0) - 1.0) < 1e-10);
        Assert.True(Math.Abs(ModalExitAnimation.FadeOut.OpacityAtProgress(1.0)) < 1e-10);
        Assert.True(Math.Abs(ModalExitAnimation.FadeOut.OpacityAtProgress(0.5) - 0.5) < 1e-10);
    }

    [Fact]
    public void EdgeExitYOffsetAtProgress()
    {
        Assert.Equal((short)0, ModalExitAnimation.SlideUp.YOffsetAtProgress(0.0, 20));
        short finalOffset = ModalExitAnimation.SlideUp.YOffsetAtProgress(1.0, 20);
        Assert.Equal(ModalExitAnimation.SlideUp.FinalYOffset(20), finalOffset);
    }

    [Fact]
    public void EdgeEntranceNoneInstant()
    {
        var config = new ModalAnimationConfig();
        Assert.True(Math.Abs(ModalEntranceAnimation.None.InitialScale(config) - 1.0) < 1e-10);
        Assert.True(Math.Abs(ModalEntranceAnimation.None.InitialOpacity() - 1.0) < 1e-10);
        Assert.Equal((short)0, ModalEntranceAnimation.None.InitialYOffset(20));
    }

    [Fact]
    public void EdgeSlideHeightClampedAt8()
    {
        // Large modal_height should clamp offset at 8
        short down = ModalEntranceAnimation.SlideDown.InitialYOffset(100);
        Assert.Equal((short)(-8), down);
        short up = ModalEntranceAnimation.SlideUp.InitialYOffset(100);
        Assert.Equal((short)8, up);

        // Exit slide clamping
        short exitUp   = ModalExitAnimation.SlideUp.FinalYOffset(100);
        Assert.Equal((short)(-8), exitUp);
        short exitDown = ModalExitAnimation.SlideDown.FinalYOffset(100);
        Assert.Equal((short)8, exitDown);
    }

    [Fact]
    public void EdgeZeroModalHeightYOffset()
    {
        Assert.Equal((short)0, ModalEntranceAnimation.SlideDown.InitialYOffset(0));
        Assert.Equal((short)0, ModalEntranceAnimation.SlideUp.InitialYOffset(0));
        Assert.Equal((short)0, ModalExitAnimation.SlideUp.FinalYOffset(0));
        Assert.Equal((short)0, ModalExitAnimation.SlideDown.FinalYOffset(0));
    }

    [Fact]
    public void EdgeConfigBuilderMethods()
    {
        var config = ModalAnimationConfig.New()
            .WithEntrance(ModalEntranceAnimation.SlideDown)
            .WithExit(ModalExitAnimation.SlideUp)
            .WithEntranceDuration(TimeSpan.FromMilliseconds(300))
            .WithExitDuration(TimeSpan.FromMilliseconds(200))
            .WithEntranceEasing(ModalEasing.Back)
            .WithExitEasing(ModalEasing.EaseInOut)
            .WithMinScale(0.8)
            .WithAnimateBackdrop(false)
            .WithBackdropDuration(TimeSpan.FromMilliseconds(50))
            .WithRespectReducedMotion(false);

        Assert.Equal(ModalEntranceAnimation.SlideDown, config.Entrance);
        Assert.Equal(ModalExitAnimation.SlideUp, config.Exit);
        Assert.Equal(TimeSpan.FromMilliseconds(300), config.EntranceDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(200), config.ExitDuration);
        Assert.Equal(ModalEasing.Back, config.EntranceEasing);
        Assert.Equal(ModalEasing.EaseInOut, config.ExitEasing);
        Assert.True(Math.Abs(config.MinScale - 0.8) < 1e-10);
        Assert.False(config.AnimateBackdrop);
        Assert.Equal(TimeSpan.FromMilliseconds(50), config.BackdropDuration);
        Assert.False(config.RespectReducedMotion);
    }

    [Fact]
    public void EdgeMinScaleClamped()
    {
        // Below 0.5 → clamped to 0.5
        var config = ModalAnimationConfig.New().WithMinScale(0.1);
        Assert.True(Math.Abs(config.MinScale - 0.5) < 1e-10);

        // Above 1.0 → clamped to 1.0
        var config2 = ModalAnimationConfig.New().WithMinScale(1.5);
        Assert.True(Math.Abs(config2.MinScale - 1.0) < 1e-10);

        // Normal value passes through
        var config3 = ModalAnimationConfig.New().WithMinScale(0.75);
        Assert.True(Math.Abs(config3.MinScale - 0.75) < 1e-10);
    }

    [Fact]
    public void EdgeIsDisabled()
    {
        var config = ModalAnimationConfig.None();
        Assert.True(config.IsDisabled());

        var config2 = new ModalAnimationConfig();
        Assert.False(config2.IsDisabled());

        // Only entrance None but exit not → not disabled
        var config3 = ModalAnimationConfig.New()
            .WithEntrance(ModalEntranceAnimation.None)
            .WithExit(ModalExitAnimation.FadeOut);
        Assert.False(config3.IsDisabled());
    }

    [Fact]
    public void EdgeEffectiveWithoutReducedMotion()
    {
        var config = new ModalAnimationConfig();
        var eff    = config.Effective(false);
        // Should return a clone of the original config
        Assert.Equal(ModalEntranceAnimation.ScaleIn, eff.Entrance);
        Assert.Equal(ModalExitAnimation.ScaleOut, eff.Exit);
    }

    [Fact]
    public void EdgeEffectiveWithReducedMotionButNotRespected()
    {
        var config = new ModalAnimationConfig().WithRespectReducedMotion(false);
        var eff    = config.Effective(true);
        // respect_reduced_motion=false → should NOT apply reduced motion
        Assert.Equal(ModalEntranceAnimation.ScaleIn, eff.Entrance);
    }

    [Fact]
    public void EdgeCurrentValuesHelper()
    {
        var state  = ModalAnimationState.Open();
        var config = new ModalAnimationConfig();
        var (scale, opacity, backdrop, yOffset) = state.CurrentValues(config, 20);
        Assert.True(Math.Abs(scale - 1.0) < 1e-10);
        Assert.True(Math.Abs(opacity - 1.0) < 1e-10);
        Assert.True(Math.Abs(backdrop - 1.0) < 1e-10);
        Assert.Equal((short)0, yOffset);
    }

    [Fact]
    public void EdgeCurrentValuesClosed()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();
        var (scale, opacity, backdrop, yOffset) = state.CurrentValues(config, 20);
        Assert.True(Math.Abs(scale - config.MinScale) < 1e-10);
        Assert.True(Math.Abs(opacity) < 1e-10);
        Assert.True(Math.Abs(backdrop) < 1e-10);
        Assert.Equal((short)0, yOffset);
    }

    [Fact]
    public void EdgeTickNoopOnOpen()
    {
        var state  = ModalAnimationState.Open();
        var config = new ModalAnimationConfig();
        bool changed = state.Tick(TimeSpan.FromMilliseconds(100), config);
        Assert.False(changed);
        Assert.Equal(ModalAnimationPhase.Open, state.Phase());
    }

    [Fact]
    public void EdgeTickNoopOnClosed()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();
        bool changed = state.Tick(TimeSpan.FromMilliseconds(100), config);
        Assert.False(changed);
        Assert.Equal(ModalAnimationPhase.Closed, state.Phase());
    }

    [Fact]
    public void EdgeTickReturnsFalseMidAnimation()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig();
        state.StartOpening();
        // Small tick that won't complete the 200ms animation
        bool changed = state.Tick(TimeSpan.FromMilliseconds(50), config);
        Assert.False(changed);
        Assert.Equal(ModalAnimationPhase.Opening, state.Phase());
    }

    [Fact]
    public void EdgeClosingAnimationCompletesToClosed()
    {
        var state  = ModalAnimationState.Open();
        var config = new ModalAnimationConfig();
        state.StartClosing();
        bool changed = state.Tick(TimeSpan.FromSeconds(1), config);
        Assert.True(changed);
        Assert.Equal(ModalAnimationPhase.Closed, state.Phase());
        Assert.Equal(0.0, state.Progress());
        Assert.Equal(0.0, state.BackdropProgress());
    }

    [Fact]
    public void EdgeStartOpeningWhenOpenIsNoop()
    {
        var state = ModalAnimationState.Open();
        state.StartOpening();
        Assert.Equal(ModalAnimationPhase.Open, state.Phase());
        Assert.Equal(1.0, state.Progress());
    }

    [Fact]
    public void EdgeStartClosingWhenClosedIsNoop()
    {
        var state = new ModalAnimationState();
        state.StartClosing();
        Assert.Equal(ModalAnimationPhase.Closed, state.Phase());
        Assert.Equal(0.0, state.Progress());
    }

    [Fact]
    public void EdgeDefaultStateEqualsNew()
    {
        var defaultState = new ModalAnimationState();
        var newState     = new ModalAnimationState();
        Assert.Equal(defaultState.Phase(), newState.Phase());
        Assert.Equal(defaultState.Progress(), newState.Progress());
        Assert.Equal(defaultState.BackdropProgress(), newState.BackdropProgress());
    }

    [Fact]
    public void EdgeBackdropNoAnimation()
    {
        var state  = new ModalAnimationState();
        var config = new ModalAnimationConfig().WithAnimateBackdrop(false);
        state.StartOpening();

        // With animate_backdrop=false, backdrop should be 1.0 during Opening
        double backdrop = state.CurrentBackdropOpacity(config);
        Assert.True(Math.Abs(backdrop - 1.0) < 1e-10);

        // Force to closing
        state.ForceOpen();
        state.StartClosing();
        double backdrop2 = state.CurrentBackdropOpacity(config);
        Assert.True(Math.Abs(backdrop2) < 1e-10);
    }

    [Fact]
    public void EdgeEntranceScaleAtProgressClamped()
    {
        var config = new ModalAnimationConfig();
        // Progress values outside [0, 1] should be clamped
        double s = ModalEntranceAnimation.ScaleIn.ScaleAtProgress(-0.5, config);
        Assert.True(Math.Abs(s - config.MinScale) < 1e-10);
        double s2 = ModalEntranceAnimation.ScaleIn.ScaleAtProgress(2.0, config);
        Assert.True(Math.Abs(s2 - 1.0) < 1e-10);
    }

    [Fact]
    public void EdgeEntranceOpacityAtProgressClamped()
    {
        double o = ModalEntranceAnimation.FadeIn.OpacityAtProgress(-1.0);
        Assert.True(Math.Abs(o) < 1e-10);
        double o2 = ModalEntranceAnimation.FadeIn.OpacityAtProgress(5.0);
        Assert.True(Math.Abs(o2 - 1.0) < 1e-10);
    }

    [Fact]
    public void EdgeEntranceYOffsetAtProgressClamped()
    {
        // At progress < 0 → clamped to 0 → full initial offset
        short y = ModalEntranceAnimation.SlideDown.YOffsetAtProgress(-1.0, 20);
        Assert.Equal(ModalEntranceAnimation.SlideDown.InitialYOffset(20), y);
        // At progress > 1 → clamped to 1 → offset 0
        short y2 = ModalEntranceAnimation.SlideDown.YOffsetAtProgress(5.0, 20);
        Assert.Equal((short)0, y2);
    }

    [Fact]
    public void EdgePhaseDefaultIsClosed()
    {
        Assert.Equal(ModalAnimationPhase.Closed, default(ModalAnimationPhase));
    }

    [Fact]
    public void EdgeEntranceDefaultIsScaleIn()
    {
        Assert.Equal(ModalEntranceAnimation.ScaleIn, default(ModalEntranceAnimation));
    }

    [Fact]
    public void EdgeExitDefaultIsScaleOut()
    {
        Assert.Equal(ModalExitAnimation.ScaleOut, default(ModalExitAnimation));
    }

    [Fact]
    public void EdgeEasingDefaultIsEaseOut()
    {
        // Upstream: assert_eq!(ModalEasing::default(), ModalEasing::EaseOut)
        // The C# enum places EaseOut at value 0 (first) so that default(ModalEasing)
        // matches the upstream #[default] semantics exactly.
        Assert.Equal(ModalEasing.EaseOut, default(ModalEasing));
    }

    [Fact]
    public void EdgeConfigNoneFields()
    {
        var config = ModalAnimationConfig.None();
        Assert.Equal(ModalEntranceAnimation.None, config.Entrance);
        Assert.Equal(ModalExitAnimation.None, config.Exit);
        Assert.Equal(TimeSpan.Zero, config.EntranceDuration);
        Assert.Equal(TimeSpan.Zero, config.ExitDuration);
        Assert.Equal(TimeSpan.Zero, config.BackdropDuration);
    }

    [Fact]
    public void EdgeStateIsVisibleIsClosedIsOpen()
    {
        var state = new ModalAnimationState();
        Assert.False(state.IsVisible());
        Assert.True(state.IsClosed());
        Assert.False(state.IsOpen());
        Assert.False(state.IsAnimating());

        state.StartOpening();
        Assert.True(state.IsVisible());
        Assert.False(state.IsClosed());
        Assert.False(state.IsOpen());
        Assert.True(state.IsAnimating());

        state.ForceOpen();
        Assert.True(state.IsVisible());
        Assert.False(state.IsClosed());
        Assert.True(state.IsOpen());
        Assert.False(state.IsAnimating());
    }

    [Fact]
    public void EdgeForceOpenDuringClosing()
    {
        var state  = ModalAnimationState.Open();
        state.StartClosing();
        var config = new ModalAnimationConfig();
        state.Tick(TimeSpan.FromMilliseconds(50), config);
        Assert.Equal(ModalAnimationPhase.Closing, state.Phase());

        state.ForceOpen();
        Assert.Equal(ModalAnimationPhase.Open, state.Phase());
        Assert.Equal(1.0, state.Progress());
    }

    [Fact]
    public void EdgeForceCloseDuringOpening()
    {
        var state  = new ModalAnimationState();
        state.StartOpening();
        var config = new ModalAnimationConfig();
        state.Tick(TimeSpan.FromMilliseconds(50), config);

        state.ForceClose();
        Assert.Equal(ModalAnimationPhase.Closed, state.Phase());
        Assert.Equal(0.0, state.Progress());
    }

    [Fact]
    public void EdgeEasedProgressOpenClosed()
    {
        var config    = new ModalAnimationConfig();
        var stateOpen = ModalAnimationState.Open();
        Assert.True(Math.Abs(stateOpen.EasedProgress(config) - 1.0) < 1e-10);

        var stateClosed = new ModalAnimationState();
        Assert.True(Math.Abs(stateClosed.EasedProgress(config)) < 1e-10);
    }

    [Fact]
    public void EdgeEasedBackdropProgressOpenClosed()
    {
        var config    = new ModalAnimationConfig();
        var stateOpen = ModalAnimationState.Open();
        Assert.True(Math.Abs(stateOpen.EasedBackdropProgress(config) - 1.0) < 1e-10);

        var stateClosed = new ModalAnimationState();
        Assert.True(Math.Abs(stateClosed.EasedBackdropProgress(config)) < 1e-10);
    }

    [Fact]
    public void EdgeCloneDebugPhase()
    {
        var phase  = ModalAnimationPhase.Opening;
        var cloned = phase;
        Assert.Equal(ModalAnimationPhase.Opening, cloned);
        // Verify ToString works (mirrors Rust's {:?} format check)
        _ = phase.ToString();
    }

    [Fact]
    public void EdgeCloneDebugEntrance()
    {
        var anim   = ModalEntranceAnimation.SlideDown;
        var cloned = anim;
        Assert.Equal(ModalEntranceAnimation.SlideDown, cloned);
        _ = anim.ToString();
    }

    [Fact]
    public void EdgeCloneDebugExit()
    {
        var anim   = ModalExitAnimation.SlideUp;
        var cloned = anim;
        Assert.Equal(ModalExitAnimation.SlideUp, cloned);
        _ = anim.ToString();
    }

    [Fact]
    public void EdgeCloneDebugEasing()
    {
        var easing = ModalEasing.Back;
        _ = easing.ToString();
        // PartialEq
        Assert.Equal(ModalEasing.Back, easing);
        Assert.NotEqual(ModalEasing.Linear, easing);
    }

    [Fact]
    public void EdgeCloneDebugConfig()
    {
        var config = new ModalAnimationConfig();
        var cloned = config.Clone();
        Assert.Equal(cloned.Entrance, config.Entrance);
        Assert.Equal(cloned.Exit, config.Exit);
        _ = config.ToString();
    }

    [Fact]
    public void EdgeCloneDebugState()
    {
        var state = new ModalAnimationState();
        state.StartOpening();
        var cloned = state.Clone();
        Assert.Equal(cloned.Phase(), state.Phase());
        Assert.Equal(cloned.Progress(), state.Progress());
        _ = state.ToString();
    }
}
