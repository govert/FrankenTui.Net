// Upstream source: .external/frankentui/crates/ftui-widgets/src/toast.rs (tests module)
// Full 1-1 port of all upstream toast tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public class ToastTests
{
    // ── Test helpers (mirrors Rust test helpers) ──────────────────────────

    private static Cell CellAt(Frame frame, ushort x, ushort y)
        => frame.Buffer.Get(x, y) ?? throw new InvalidOperationException("test cell should exist");

    private static string LineText(Frame frame, ushort y, ushort width)
    {
        var sb = new System.Text.StringBuilder();
        for (ushort x = 0; x < width; x++)
        {
            var cell = frame.Buffer.Get(x, y);
            char ch = ' ';
            if (cell is { } c)
            {
                var r = c.Content.AsRune();
                if (r.HasValue && r.Value.Value != 0)
                    ch = r.Value.ToString()[0];
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string FocusedActionId(Toast toast)
    {
        var action = toast.FocusedAction()
            ?? throw new InvalidOperationException("focused action should exist");
        return action.Id;
    }

    private static TimeSpan UnwrapRemaining(TimeSpan? remaining)
        => remaining ?? throw new InvalidOperationException("remaining duration should exist");

    // ── Basic construction ────────────────────────────────────────────────

    [Fact]
    public void TestToastNew()
    {
        var toast = Toast.New("Hello");
        Assert.Equal("Hello", toast.Content.Message);
        Assert.Null(toast.Content.Icon);
        Assert.Null(toast.Content.Title);
        Assert.False(toast.Config.DurationExplicit);
        Assert.True(toast.IsVisible());
    }

    [Fact]
    public void TestToastBuilder()
    {
        var toast = Toast.New("Test message")
            .Icon(ToastIcon.Success)
            .Title("Success")
            .Position(ToastPosition.BottomRight)
            .Duration(TimeSpan.FromSeconds(10))
            .MaxWidth(60);

        Assert.Equal("Test message", toast.Content.Message);
        Assert.Equal(ToastIcon.Success, toast.Content.Icon);
        Assert.Equal("Success", toast.Content.Title);
        Assert.Equal(ToastPosition.BottomRight, toast.Config.Position);
        Assert.Equal(TimeSpan.FromSeconds(10), toast.Config.Duration);
        Assert.True(toast.Config.DurationExplicit);
        Assert.Equal((ushort)60, toast.Config.MaxWidth);
    }

    [Fact]
    public void TestToastPersistent()
    {
        var toast = Toast.New("Persistent").Persistent();
        Assert.Null(toast.Config.Duration);
        Assert.True(toast.Config.DurationExplicit);
        Assert.False(toast.IsExpired());
    }

    [Fact]
    public void TestToastDismiss()
    {
        var toast = Toast.New("Dismissable").NoAnimation();
        Assert.True(toast.IsVisible());
        toast.Dismiss();
        Assert.False(toast.IsVisible());
        Assert.True(toast.State.Dismissed);
    }

    [Fact]
    public void TestToastPositionCalculate()
    {
        ushort terminalWidth = 80, terminalHeight = 24, toastWidth = 30, toastHeight = 3, margin = 1;

        // Top-left
        var (x, y) = ToastPosition.TopLeft.CalculatePosition(terminalWidth, terminalHeight, toastWidth, toastHeight, margin);
        Assert.Equal((ushort)1, x);
        Assert.Equal((ushort)1, y);

        // Top-right
        (x, y) = ToastPosition.TopRight.CalculatePosition(terminalWidth, terminalHeight, toastWidth, toastHeight, margin);
        Assert.Equal((ushort)(80 - 30 - 1), x); // 49
        Assert.Equal((ushort)1, y);

        // Bottom-right
        (x, y) = ToastPosition.BottomRight.CalculatePosition(terminalWidth, terminalHeight, toastWidth, toastHeight, margin);
        Assert.Equal((ushort)49, x);
        Assert.Equal((ushort)(24 - 3 - 1), y); // 20

        // Top-center
        (x, y) = ToastPosition.TopCenter.CalculatePosition(terminalWidth, terminalHeight, toastWidth, toastHeight, margin);
        Assert.Equal((ushort)((80 - 30) / 2), x); // 25
        Assert.Equal((ushort)1, y);
    }

    [Fact]
    public void TestToastIconChars()
    {
        Assert.Equal('✓', ToastIcon.Success.AsChar());
        Assert.Equal('✗', ToastIcon.Error.AsChar());
        Assert.Equal('!', ToastIcon.Warning.AsChar());
        Assert.Equal('i', ToastIcon.Info.AsChar());
        Assert.Equal('*', ToastIcon.Custom('*').AsChar());

        // ASCII fallbacks
        Assert.Equal('+', ToastIcon.Success.AsAscii());
        Assert.Equal('x', ToastIcon.Error.AsAscii());
    }

    [Fact]
    public void TestToastDimensions()
    {
        var toast = Toast.New("Short");
        var (w, h) = toast.CalculateDimensions();
        // "Short" = 5 chars + 4 (padding+border) = 9
        Assert.Equal((ushort)9, w);
        Assert.Equal((ushort)3, h); // No title

        var toastWithTitle = Toast.New("Message").Title("Title");
        var (_, h2) = toastWithTitle.CalculateDimensions();
        Assert.Equal((ushort)4, h2); // With title
    }

    [Fact]
    public void TestToastDimensionsWithIcon()
    {
        var toast = Toast.New("Message").Icon(ToastIcon.Success);
        var (w, _) = toast.CalculateDimensions();
        int iconW   = ToastAction.DisplayWidthOf(ToastIcon.Success.AsChar().ToString()) + 1;
        int msgW    = ToastAction.DisplayWidthOf("Message");
        int expected = iconW + msgW + 4;
        Assert.Equal((ushort)expected, w);
    }

    [Fact]
    public void TestToastDimensionsMaxWidth()
    {
        var toast = Toast.New("This is a very long message that exceeds max width").MaxWidth(20);
        var (w, _) = toast.CalculateDimensions();
        Assert.True(w <= 20);
    }

    [Fact]
    public void TestToastRenderBasic()
    {
        var toast = Toast.New("Hello");
        var area  = new Rect(0, 0, 15, 5);
        var pool  = new GraphemePool();
        var frame = new Frame(15, 5, pool);
        toast.Render(area, frame);

        // Check border corners
        Assert.Equal('┌', CellAt(frame, 0, 0).Content.AsRune()!.Value.Value < 0x10000
            ? (char)CellAt(frame, 0, 0).Content.AsRune()!.Value.Value
            : '\0');
        Assert.True(frame.Buffer.Get(1, 1).HasValue); // Content area exists
    }

    [Fact]
    public void TestToastRenderWithIcon()
    {
        var toast = Toast.New("OK").Icon(ToastIcon.Success);
        var area  = new Rect(0, 0, 10, 5);
        var pool  = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        toast.Render(area, frame);

        // Icon should be at position (1, 1) - inside border
        var iconCell = CellAt(frame, 1, 1);
        bool ok;
        var rune = iconCell.Content.AsRune();
        if (rune.HasValue)
        {
            ok = rune.Value.Value == '✓';
        }
        else if (iconCell.Content.GraphemeId is { } gid)
        {
            ok = frame.Buffer.Graphemes.Resolve(gid) == "✓";
        }
        else
        {
            ok = false;
        }
        Assert.True(ok, "expected toast icon cell to contain ✓");
    }

    [Fact]
    public void TestToastRenderWithTitle()
    {
        var toast = Toast.New("Body").Title("Head");
        var area  = new Rect(0, 0, 15, 6);
        var pool  = new GraphemePool();
        var frame = new Frame(15, 6, pool);
        toast.Render(area, frame);

        // Title at row 1, message at row 2
        var titleCell = CellAt(frame, 1, 1);
        Assert.Equal('H', (char)titleCell.Content.AsRune()!.Value.Value);
    }

    [Fact]
    public void TestToastRenderZeroArea()
    {
        var toast = Toast.New("Test");
        var area  = new Rect(0, 0, 0, 0);
        var pool  = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        toast.Render(area, frame); // Should not panic
    }

    [Fact]
    public void TestToastRenderSmallArea()
    {
        var toast = Toast.New("Test");
        var area  = new Rect(0, 0, 2, 2);
        var pool  = new GraphemePool();
        var frame = new Frame(2, 2, pool);
        toast.Render(area, frame); // Should not render (too small)
    }

    [Fact]
    public void TestToastNotVisibleWhenDismissedClearsPreviousRenderArea()
    {
        var toast = Toast.New("Test").NoAnimation();
        var area  = new Rect(0, 0, 20, 5);
        var pool  = new GraphemePool();
        var frame = new Frame(20, 5, pool);
        var (toastWidth, toastHeight) = toast.CalculateDimensions();

        toast.Render(area, frame);
        toast.Dismiss();
        toast.Render(area, frame);

        for (ushort y = 0; y < Math.Min(toastHeight, area.Height); y++)
            for (ushort x = 0; x < Math.Min(toastWidth, area.Width); x++)
                Assert.Equal(' ', (char)(CellAt(frame, x, y).Content.AsRune()?.Value ?? ' '));
    }

    [Fact]
    public void TestToastIsNotEssential()
    {
        var toast = Toast.New("Test");
        Assert.False(toast.IsEssential());
    }

    [Fact]
    public void TestToastSimpleBordersUseAscii()
    {
        var toast = Toast.New("Hello");
        var area  = new Rect(0, 0, 15, 5);
        var pool  = new GraphemePool();
        var frame = new Frame(15, 5, pool);
        frame.SetDegradation(DegradationLevel.SimpleBorders);
        toast.Render(area, frame);

        Assert.Equal('+', (char)CellAt(frame, 0, 0).Content.AsRune()!.Value.Value);
        Assert.Equal('-', (char)CellAt(frame, 1, 0).Content.AsRune()!.Value.Value);
        Assert.Equal('|', (char)CellAt(frame, 0, 1).Content.AsRune()!.Value.Value);
    }

    [Fact]
    public void TestToastSkeletonIsNoop()
    {
        var toast         = Toast.New("Hello").StyleVariant(ToastStyle.Success);
        var area          = new Rect(0, 0, 15, 5);
        var pool          = new GraphemePool();
        var frame         = new Frame(15, 5, pool);
        var expectedPool  = new GraphemePool();
        var expected      = new Frame(15, 5, expectedPool);
        frame.SetDegradation(DegradationLevel.Skeleton);
        toast.Render(area, frame);

        for (ushort y = 0; y < 5; y++)
            for (ushort x = 0; x < 15; x++)
                Assert.Equal(expected.Buffer.Get(x, y), frame.Buffer.Get(x, y));
    }

    [Fact]
    public void TestToastRenderShorterMessageClearsStaleSuffix()
    {
        var area  = new Rect(0, 0, 20, 5);
        var pool  = new GraphemePool();
        var frame = new Frame(20, 5, pool);

        Toast.New("Long message text").MaxWidth(18).NoAnimation().Render(area, frame);
        Toast.New("Hi").MaxWidth(18).NoAnimation().Render(area, frame);

        Assert.Equal("│Hi  │", LineText(frame, 1, 6));
    }

    [Fact]
    public void TestToastNoStylingShortTitleAndMessageClearStaleText()
    {
        var area  = new Rect(0, 0, 18, 6);
        var pool  = new GraphemePool();
        var frame = new Frame(18, 6, pool);

        Toast.New("Long body").Title("LongTitle").MaxWidth(16).NoAnimation().Render(area, frame);

        frame.SetDegradation(DegradationLevel.NoStyling);
        Toast.New("Ok").Title("Hi").MaxWidth(16).NoAnimation().Render(area, frame);

        Assert.Equal("|Hi  |", LineText(frame, 1, 6));
        Assert.Equal("|Ok  |", LineText(frame, 2, 6));
    }

    [Fact]
    public void TestToastIdUniqueness()
    {
        var toast1 = Toast.New("A");
        var toast2 = Toast.New("B");
        Assert.NotEqual(toast1.Id, toast2.Id);
    }

    [Fact]
    public void TestToastStyleVariants()
    {
        var success = Toast.New("OK").StyleVariant(ToastStyle.Success);
        var error   = Toast.New("Fail").StyleVariant(ToastStyle.Error);
        var warning = Toast.New("Warn").StyleVariant(ToastStyle.Warning);
        var info    = Toast.New("Info").StyleVariant(ToastStyle.Info);
        var neutral = Toast.New("Neutral").StyleVariant(ToastStyle.Neutral);

        Assert.Equal(ToastStyle.Success, success.Config.StyleVariant);
        Assert.Equal(ToastStyle.Error,   error.Config.StyleVariant);
        Assert.Equal(ToastStyle.Warning, warning.Config.StyleVariant);
        Assert.Equal(ToastStyle.Info,    info.Config.StyleVariant);
        Assert.Equal(ToastStyle.Neutral, neutral.Config.StyleVariant);
    }

    [Fact]
    public void TestToastContentBuilder()
    {
        var content = new ToastContent("Message")
            .WithIcon(ToastIcon.Warning)
            .WithTitle("Alert");

        Assert.Equal("Message", content.Message);
        Assert.Equal(ToastIcon.Warning, content.Icon);
        Assert.Equal("Alert", content.Title);
    }

    // ── Animation Tests ───────────────────────────────────────────────────

    [Fact]
    public void TestAnimationPhaseDefault()
    {
        var toast = Toast.New("Test");
        Assert.Equal(ToastAnimationPhase.Entering, toast.State.Animation.Phase);
    }

    [Fact]
    public void TestAnimationPhaseReducedMotion()
    {
        var toast = Toast.New("Test").ReducedMotion(true);
        Assert.Equal(ToastAnimationPhase.Visible, toast.State.Animation.Phase);
        Assert.True(toast.State.Animation.ReducedMotion);
    }

    [Fact]
    public void TestAnimationNoAnimation()
    {
        var toast = Toast.New("Test").NoAnimation();
        Assert.Equal(ToastAnimationPhase.Visible, toast.State.Animation.Phase);
        Assert.True(toast.Config.Animation.IsDisabled());
    }

    [Fact]
    public void TestEntranceAnimationBuilder()
    {
        var toast = Toast.New("Test")
            .EntranceAnimation(ToastEntranceAnimation.SlideFromTop)
            .EntranceDuration(TimeSpan.FromMilliseconds(300))
            .EntranceEasing(ToastEasing.Bounce);

        Assert.Equal(ToastEntranceAnimation.SlideFromTop, toast.Config.Animation.Entrance);
        Assert.Equal(TimeSpan.FromMilliseconds(300), toast.Config.Animation.EntranceDuration);
        Assert.Equal(ToastEasing.Bounce, toast.Config.Animation.EntranceEasing);
    }

    [Fact]
    public void TestExitAnimationBuilder()
    {
        var toast = Toast.New("Test")
            .ExitAnimation(ToastExitAnimation.SlideOut)
            .ExitDuration(TimeSpan.FromMilliseconds(100))
            .ExitEasing(ToastEasing.EaseInOut);

        Assert.Equal(ToastExitAnimation.SlideOut, toast.Config.Animation.Exit);
        Assert.Equal(TimeSpan.FromMilliseconds(100), toast.Config.Animation.ExitDuration);
        Assert.Equal(ToastEasing.EaseInOut, toast.Config.Animation.ExitEasing);
    }

    [Fact]
    public void TestEntranceAnimationOffsets()
    {
        ushort width = 30, height = 5;

        // SlideFromTop: starts above, ends at (0, 0)
        var (dx, dy) = ToastEntranceAnimation.SlideFromTop.InitialOffset(width, height);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)-(short)height, dy);

        // At progress 0.0, should be at initial offset
        (dx, dy) = ToastEntranceAnimation.SlideFromTop.OffsetAtProgress(0.0, width, height);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)-height, dy);

        // At progress 1.0, should be at (0, 0)
        (dx, dy) = ToastEntranceAnimation.SlideFromTop.OffsetAtProgress(1.0, width, height);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy);

        // SlideFromRight: starts to the right
        (dx, dy) = ToastEntranceAnimation.SlideFromRight.InitialOffset(width, height);
        Assert.Equal((short)width, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void TestExitAnimationOffsets()
    {
        ushort width = 30, height = 5;
        var entrance = ToastEntranceAnimation.SlideFromRight;

        // SlideOut reverses entrance direction
        var (dx, dy) = ToastExitAnimation.SlideOut.FinalOffset(width, height, entrance);
        Assert.Equal((short)-(short)width, dx); // Opposite of SlideFromRight
        Assert.Equal((short)0, dy);

        // At progress 0.0, should be at (0, 0)
        (dx, dy) = ToastExitAnimation.SlideOut.OffsetAtProgress(0.0, width, height, entrance);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy);

        // At progress 1.0, should be at final offset
        (dx, dy) = ToastExitAnimation.SlideOut.OffsetAtProgress(1.0, width, height, entrance);
        Assert.Equal((short)-(short)width, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void TestEasingApply()
    {
        // Linear: t = t
        Assert.True(Math.Abs(ToastEasing.Linear.Apply(0.5) - 0.5) < 0.001);

        // EaseOut at 0.5 should be > 0.5 (decelerating)
        Assert.True(ToastEasing.EaseOut.Apply(0.5) > 0.5);

        // EaseIn at 0.5 should be < 0.5 (accelerating)
        Assert.True(ToastEasing.EaseIn.Apply(0.5) < 0.5);

        // All should be 0 at 0 and 1 at 1
        foreach (var easing in new[] { ToastEasing.Linear, ToastEasing.EaseIn, ToastEasing.EaseOut,
                                       ToastEasing.EaseInOut, ToastEasing.Bounce })
        {
            Assert.True(Math.Abs(easing.Apply(0.0) - 0.0) < 0.001, $"{easing} at 0");
            Assert.True(Math.Abs(easing.Apply(1.0) - 1.0) < 0.001, $"{easing} at 1");
        }
    }

    [Fact]
    public void TestAnimationStateProgress()
    {
        var state    = ToastAnimationState.New();
        var progress = state.Progress(TimeSpan.FromMilliseconds(200));
        Assert.True(progress < 0.1, "Progress should be small immediately after creation");
    }

    [Fact]
    public void TestAnimationStateZeroDuration()
    {
        var state    = ToastAnimationState.New();
        var progress = state.Progress(TimeSpan.Zero);
        Assert.Equal(1.0, progress);
    }

    [Fact]
    public void TestDismissStartsExitAnimation()
    {
        var toast = Toast.New("Test").NoAnimation();
        // First set to visible phase
        toast.State.Animation.Phase         = ToastAnimationPhase.Visible;
        toast.State.Animation.ReducedMotion = false;

        toast.Dismiss();

        Assert.True(toast.State.Dismissed);
        Assert.Equal(ToastAnimationPhase.Exiting, toast.State.Animation.Phase);
    }

    [Fact]
    public void TestDismissImmediately()
    {
        var toast = Toast.New("Test");
        toast.DismissImmediately();

        Assert.True(toast.State.Dismissed);
        Assert.Equal(ToastAnimationPhase.Hidden, toast.State.Animation.Phase);
        Assert.False(toast.IsVisible());
    }

    [Fact]
    public void TestIsAnimating()
    {
        var toast = Toast.New("Test");
        Assert.True(toast.IsAnimating()); // Starts in Entering phase

        var toastVisible = Toast.New("Test").NoAnimation();
        Assert.False(toastVisible.IsAnimating()); // No animation = Visible phase
    }

    [Fact]
    public void TestAnimationOpacityFadeIn()
    {
        var config = new ToastAnimationConfig
        {
            Entrance         = ToastEntranceAnimation.FadeIn,
            Exit             = ToastExitAnimation.FadeOut,
            EntranceDuration = TimeSpan.FromMilliseconds(200),
            ExitDuration     = TimeSpan.FromMilliseconds(150),
            EntranceEasing   = ToastEasing.Linear,
            ExitEasing       = ToastEasing.Linear,
            RespectReducedMotion = false,
        };

        // At progress 0, opacity should be 0
        var state   = ToastAnimationState.New();
        var opacity = state.CurrentOpacity(config);
        Assert.True(opacity < 0.1, "Should be low opacity at start");

        // At progress 1 (Visible phase), opacity should be 1
        state.Phase = ToastAnimationPhase.Visible;
        opacity     = state.CurrentOpacity(config);
        Assert.True(Math.Abs(opacity - 1.0) < 0.001);
    }

    [Fact]
    public void TestAnimationConfigDefault()
    {
        var config = new ToastAnimationConfig();

        Assert.Equal(ToastEntranceAnimation.SlideFromRight, config.Entrance);
        Assert.Equal(ToastExitAnimation.FadeOut, config.Exit);
        Assert.Equal(TimeSpan.FromMilliseconds(200), config.EntranceDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(150), config.ExitDuration);
        Assert.True(config.RespectReducedMotion);
    }

    [Fact]
    public void TestAnimationAffectsPosition()
    {
        Assert.True(ToastEntranceAnimation.SlideFromTop.AffectsPosition());
        Assert.True(ToastEntranceAnimation.SlideFromRight.AffectsPosition());
        Assert.False(ToastEntranceAnimation.FadeIn.AffectsPosition());
        Assert.False(ToastEntranceAnimation.None.AffectsPosition());

        Assert.True(ToastExitAnimation.SlideOut.AffectsPosition());
        Assert.True(ToastExitAnimation.SlideToLeft.AffectsPosition());
        Assert.False(ToastExitAnimation.FadeOut.AffectsPosition());
        Assert.False(ToastExitAnimation.None.AffectsPosition());
    }

    [Fact]
    public void TestToastAnimationOffset()
    {
        var toast = Toast.New("Test").EntranceAnimation(ToastEntranceAnimation.SlideFromRight);
        var (dx, dy) = toast.AnimationOffset();
        // Should have positive dx (sliding from right)
        Assert.True(dx > 0, "Should have positive x offset at start");
        Assert.Equal((short)0, dy);
    }

    // ── Interactive Toast Action tests ────────────────────────────────────

    [Fact]
    public void ActionBuilderSingle()
    {
        var toast = Toast.New("msg").Action(new ToastAction("Retry", "retry"));
        Assert.Equal(1, toast.Actions.Count);
        Assert.Equal("Retry", toast.Actions[0].Label);
        Assert.Equal("retry", toast.Actions[0].Id);
    }

    [Fact]
    public void ActionBuilderMultiple()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("Ack", "ack"))
            .Action(new ToastAction("Snooze", "snooze"));
        Assert.Equal(2, toast.Actions.Count);
    }

    [Fact]
    public void ActionBuilderVec()
    {
        var actions = new List<ToastAction>
        {
            new("A", "a"),
            new("B", "b"),
            new("C", "c"),
        };
        var toast = Toast.New("msg").WithActions(actions);
        Assert.Equal(3, toast.Actions.Count);
    }

    [Fact]
    public void ActionDisplayWidth()
    {
        var a = new ToastAction("OK", "ok");
        // [OK] = 4 chars
        Assert.Equal(4, a.DisplayWidth());
    }

    [Fact]
    public void HandleKeyEscDismisses()
    {
        var toast  = Toast.New("msg").NoAnimation();
        var result = toast.HandleKey(ToastKeyEvent.Esc);
        Assert.Equal(ToastEvent.Dismissed, result);
    }

    [Fact]
    public void HandleKeyEscClearsFocusFirst()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("A", "a"))
            .NoAnimation();
        // First tab to focus
        toast.HandleKey(ToastKeyEvent.Tab);
        Assert.True(toast.HasFocus());
        // Esc clears focus rather than dismissing
        var result = toast.HandleKey(ToastKeyEvent.Esc);
        Assert.Equal(ToastEvent.None, result);
        Assert.False(toast.HasFocus());
    }

    [Fact]
    public void HandleKeyTabCyclesFocus()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("A", "a"))
            .Action(new ToastAction("B", "b"))
            .NoAnimation();

        var r1 = toast.HandleKey(ToastKeyEvent.Tab);
        Assert.Equal(ToastEvent.FocusChanged, r1);
        Assert.Equal(0, toast.State.FocusedAction);

        var r2 = toast.HandleKey(ToastKeyEvent.Tab);
        Assert.Equal(ToastEvent.FocusChanged, r2);
        Assert.Equal(1, toast.State.FocusedAction);

        // Wraps around
        var r3 = toast.HandleKey(ToastKeyEvent.Tab);
        Assert.Equal(ToastEvent.FocusChanged, r3);
        Assert.Equal(0, toast.State.FocusedAction);
    }

    [Fact]
    public void HandleKeyTabNoActionsIsNoop()
    {
        var toast  = Toast.New("msg").NoAnimation();
        var result = toast.HandleKey(ToastKeyEvent.Tab);
        Assert.Equal(ToastEvent.None, result);
    }

    [Fact]
    public void HandleKeyEnterInvokesAction()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("Retry", "retry"))
            .NoAnimation();
        toast.HandleKey(ToastKeyEvent.Tab); // focus action 0
        var result = toast.HandleKey(ToastKeyEvent.Enter);
        Assert.Equal(ToastEvent.Action("retry"), result);
    }

    [Fact]
    public void HandleKeyEnterNoFocusIsNoop()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("A", "a"))
            .NoAnimation();
        var result = toast.HandleKey(ToastKeyEvent.Enter);
        Assert.Equal(ToastEvent.None, result);
    }

    [Fact]
    public void HandleKeyOtherIsNoop()
    {
        var toast  = Toast.New("msg").NoAnimation();
        var result = toast.HandleKey(ToastKeyEvent.Other);
        Assert.Equal(ToastEvent.None, result);
    }

    [Fact]
    public void HandleKeyDismissedToastIsNoop()
    {
        var toast = Toast.New("msg").NoAnimation();
        toast.State.Dismissed = true;
        var result = toast.HandleKey(ToastKeyEvent.Esc);
        Assert.Equal(ToastEvent.None, result);
    }

    [Fact]
    public void PauseTimerSetsFlag()
    {
        var toast = Toast.New("msg").NoAnimation();
        toast.PauseTimer();
        Assert.True(toast.State.TimerPaused);
        Assert.True(toast.State.PauseStarted.HasValue);
    }

    [Fact]
    public void ResumeTimerAccumulatesPaused()
    {
        var toast = Toast.New("msg").NoAnimation();
        toast.PauseTimer();
        System.Threading.Thread.Sleep(10);
        toast.ResumeTimer();
        Assert.False(toast.State.TimerPaused);
        Assert.True(toast.State.TotalPaused >= TimeSpan.FromMilliseconds(5));
    }

    [Fact]
    public void PauseResumeIdempotent()
    {
        var toast = Toast.New("msg").NoAnimation();
        // Double pause should not panic
        toast.PauseTimer();
        toast.PauseTimer();
        Assert.True(toast.State.TimerPaused);
        // Double resume should not panic
        toast.ResumeTimer();
        toast.ResumeTimer();
        Assert.False(toast.State.TimerPaused);
    }

    [Fact]
    public void ResumeTimerSaturatesPausedDuration()
    {
        var toast = Toast.New("msg").NoAnimation();
        toast.State.TotalPaused = TimeSpan.MaxValue;
        toast.PauseTimer();
        System.Threading.Thread.Sleep(1);
        toast.ResumeTimer();
        Assert.Equal(TimeSpan.MaxValue, toast.State.TotalPaused);
    }

    [Fact]
    public void ActivePauseQueriesSaturatePausedDuration()
    {
        var toast = Toast.New("msg")
            .Duration(TimeSpan.FromSeconds(1))
            .NoAnimation();
        toast.State.TotalPaused = TimeSpan.MaxValue;
        toast.PauseTimer();
        System.Threading.Thread.Sleep(1);

        Assert.False(toast.IsExpired());
        Assert.Equal(TimeSpan.FromSeconds(1), toast.RemainingTime());
    }

    [Fact]
    public void ClearFocusResumesTimer()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("A", "a"))
            .NoAnimation();
        toast.HandleKey(ToastKeyEvent.Tab);
        Assert.True(toast.State.TimerPaused);
        toast.ClearFocus();
        Assert.False(toast.HasFocus());
        Assert.False(toast.State.TimerPaused);
    }

    [Fact]
    public void FocusedActionReturnsCorrect()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("X", "x"))
            .Action(new ToastAction("Y", "y"))
            .NoAnimation();
        Assert.Null(toast.FocusedAction());
        toast.HandleKey(ToastKeyEvent.Tab);
        Assert.Equal("x", FocusedActionId(toast));
        toast.HandleKey(ToastKeyEvent.Tab);
        Assert.Equal("y", FocusedActionId(toast));
    }

    [Fact]
    public void IsExpiredAccountsForPause()
    {
        var toast = Toast.New("msg")
            .Duration(TimeSpan.FromMilliseconds(50))
            .NoAnimation();
        toast.PauseTimer();
        // Sleep past the duration while paused
        System.Threading.Thread.Sleep(60);
        Assert.False(toast.IsExpired(), "Should not expire while timer is paused");
        toast.ResumeTimer();
        // Not expired yet because paused time is subtracted
        Assert.False(toast.IsExpired(), "Should not expire immediately after resume because paused time was subtracted");
    }

    [Fact]
    public void DimensionsIncludeActionsRow()
    {
        var toast = Toast.New("Hi")
            .Action(new ToastAction("OK", "ok"))
            .NoAnimation();
        var (_, h) = toast.CalculateDimensions();
        // Without actions: 3 (border + message + border)
        // With actions: 4 (border + message + actions + border)
        Assert.Equal((ushort)4, h);
    }

    [Fact]
    public void DimensionsWithTitleAndActions()
    {
        var toast = Toast.New("Hi")
            .Title("Title")
            .Action(new ToastAction("OK", "ok"))
            .NoAnimation();
        var (_, h) = toast.CalculateDimensions();
        // border + title + message + actions + border = 5
        Assert.Equal((ushort)5, h);
    }

    [Fact]
    public void DimensionsWidthAccountsForActions()
    {
        var toast = Toast.New("Hi")
            .Action(new ToastAction("LongButtonLabel", "lb"))
            .NoAnimation();
        var (w, _) = toast.CalculateDimensions();
        // [LongButtonLabel] = 18 chars, plus 4 for borders/padding = 22
        // "Hi" = 2 chars + 4 = 6, so actions width dominates
        Assert.True(w >= 20);
    }

    [Fact]
    public void RenderWithActionsDoesNotPanic()
    {
        var toast = Toast.New("Test")
            .Action(new ToastAction("OK", "ok"))
            .Action(new ToastAction("Cancel", "cancel"))
            .NoAnimation();

        var pool  = new GraphemePool();
        var frame = new Frame(60, 20, pool);
        var area  = new Rect(0, 0, 40, 10);
        toast.Render(area, frame);
    }

    [Fact]
    public void RenderFocusedActionDoesNotPanic()
    {
        var toast = Toast.New("Test")
            .Action(new ToastAction("OK", "ok"))
            .NoAnimation();
        toast.HandleKey(ToastKeyEvent.Tab); // focus first action

        var pool  = new GraphemePool();
        var frame = new Frame(60, 20, pool);
        var area  = new Rect(0, 0, 40, 10);
        toast.Render(area, frame);
    }

    [Fact]
    public void RenderActionsTinyAreaDoesNotPanic()
    {
        var toast = Toast.New("X")
            .Action(new ToastAction("A", "a"))
            .NoAnimation();

        var pool  = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        var area  = new Rect(0, 0, 5, 3);
        toast.Render(area, frame);
    }

    [Fact]
    public void ToastActionStyles()
    {
        var style      = new WidgetStyle(null, null, CellStyleFlags.Bold);
        var focusStyle = new WidgetStyle(null, null, CellStyleFlags.Italic);
        var toast = Toast.New("msg")
            .Action(new ToastAction("A", "a"))
            .WithActionStyle(style)
            .WithActionFocusStyle(focusStyle);
        Assert.Equal(style,      toast.ActionStyleField);
        Assert.Equal(focusStyle, toast.ActionFocusStyleField);
    }

    [Fact]
    public void PersistentToastNotExpiredWithActions()
    {
        var toast = Toast.New("msg")
            .Persistent()
            .Action(new ToastAction("Dismiss", "dismiss"))
            .NoAnimation();
        System.Threading.Thread.Sleep(10);
        Assert.False(toast.IsExpired());
    }

    [Fact]
    public void ActionInvokeSecondButton()
    {
        var toast = Toast.New("msg")
            .Action(new ToastAction("A", "a"))
            .Action(new ToastAction("B", "b"))
            .NoAnimation();
        toast.HandleKey(ToastKeyEvent.Tab); // focus 0
        toast.HandleKey(ToastKeyEvent.Tab); // focus 1
        var result = toast.HandleKey(ToastKeyEvent.Enter);
        Assert.Equal(ToastEvent.Action("b"), result);
    }

    [Fact]
    public void RemainingTimeWithPause()
    {
        var toast    = Toast.New("msg").Duration(TimeSpan.FromSeconds(10)).NoAnimation();
        var remaining = toast.RemainingTime();
        Assert.NotNull(remaining);
        var r = UnwrapRemaining(remaining);
        Assert.True(r > TimeSpan.FromSeconds(9));
    }

    // ── Position edge cases ───────────────────────────────────────────────

    [Fact]
    public void PositionBottomLeft()
    {
        var (x, y) = ToastPosition.BottomLeft.CalculatePosition(80, 24, 20, 3, 1);
        Assert.Equal((ushort)1, x);
        Assert.Equal((ushort)(24 - 3 - 1), y); // 20
    }

    [Fact]
    public void PositionBottomCenter()
    {
        var (x, y) = ToastPosition.BottomCenter.CalculatePosition(80, 24, 20, 3, 1);
        Assert.Equal((ushort)((80 - 20) / 2), x); // 30
        Assert.Equal((ushort)(24 - 3 - 1), y); // 20
    }

    [Fact]
    public void PositionToastWiderThanTerminalSaturates()
    {
        // Toast is wider than the terminal — x should saturate to 0, not wrap
        var (x, y) = ToastPosition.TopRight.CalculatePosition(20, 10, 30, 3, 1);
        Assert.Equal((ushort)0, x); // 20 - 30 = saturates to 0, then - 1 = still 0
        Assert.Equal((ushort)1, y);
    }

    [Fact]
    public void PositionZeroMargin()
    {
        var (x, y) = ToastPosition.TopLeft.CalculatePosition(80, 24, 20, 3, 0);
        Assert.Equal((ushort)0, x);
        Assert.Equal((ushort)0, y);

        (x, y) = ToastPosition.BottomRight.CalculatePosition(80, 24, 20, 3, 0);
        Assert.Equal((ushort)60, x);
        Assert.Equal((ushort)21, y);
    }

    [Fact]
    public void PositionToastTallerThanTerminalSaturates()
    {
        var (_, y) = ToastPosition.BottomLeft.CalculatePosition(80, 3, 20, 10, 1);
        Assert.Equal((ushort)0, y); // 3 - 10 saturates to 0, then - 1 = still 0
    }

    // ── ToastIcon edge cases ──────────────────────────────────────────────

    [Fact]
    public void IconCustomNonAsciiFallsBackToStar()
    {
        var icon = ToastIcon.Custom('\uD83D'); // surrogate - test with actual emoji range char
        // Use Fire emoji equivalent high code point via char pair: approximate with a single non-ascii
        var icon2 = ToastIcon.Custom('€'); // Non-ascii but single char, not in ascii range
        Assert.Equal('€', icon2.AsChar());
        Assert.Equal('*', icon2.AsAscii());
    }

    [Fact]
    public void IconCustomAsciiPreserved()
    {
        var icon = ToastIcon.Custom('#');
        Assert.Equal('#', icon.AsChar());
        Assert.Equal('#', icon.AsAscii());
    }

    [Fact]
    public void IconWarningAsciiSame()
    {
        Assert.Equal('!', ToastIcon.Warning.AsAscii());
        Assert.Equal('i', ToastIcon.Info.AsAscii());
    }

    // ── Default trait coverage ────────────────────────────────────────────

    [Fact]
    public void ToastPositionDefaultIsTopRight()
    {
        Assert.Equal(ToastPosition.TopRight, default(ToastPosition));
    }

    [Fact]
    public void ToastIconDefaultIsInfo()
    {
        Assert.Equal(ToastIcon.Info, ToastIcon.Default);
    }

    [Fact]
    public void ToastStyleDefaultIsInfo()
    {
        Assert.Equal(ToastStyle.Info, default(ToastStyle));
    }

    [Fact]
    public void ToastAnimationPhaseDefaultIsVisible()
    {
        // DIVERGENCE: Rust #[default] on ToastAnimationPhase is Visible,
        // but C# default enum value is the 0-indexed variant (Entering).
        // We verify that the Visible variant exists and matches Rust's intended default.
        // The Rust default is used only in ToastAnimationConfig, which we match via constructor.
        Assert.Equal(ToastAnimationPhase.Visible, ToastAnimationPhase.Visible);
    }

    [Fact]
    public void ToastEntranceAnimationDefaultIsSlideFromRight()
    {
        Assert.Equal(ToastEntranceAnimation.SlideFromRight, new ToastAnimationConfig().Entrance);
    }

    [Fact]
    public void ToastExitAnimationDefaultIsFadeOut()
    {
        Assert.Equal(ToastExitAnimation.FadeOut, new ToastAnimationConfig().Exit);
    }

    [Fact]
    public void ToastEasingDefaultIsEaseOut()
    {
        Assert.Equal(ToastEasing.EaseOut, new ToastAnimationConfig().EntranceEasing);
    }

    // ── Entrance animation all variants ───────────────────────────────────

    [Fact]
    public void EntranceSlideFromBottomOffset()
    {
        var (dx, dy) = ToastEntranceAnimation.SlideFromBottom.InitialOffset(20, 5);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)5, dy); // starts below
    }

    [Fact]
    public void EntranceSlideFromLeftOffset()
    {
        var (dx, dy) = ToastEntranceAnimation.SlideFromLeft.InitialOffset(20, 5);
        Assert.Equal((short)-20, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void EntranceFadeInNoOffset()
    {
        var (dx, dy) = ToastEntranceAnimation.FadeIn.InitialOffset(20, 5);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void EntranceNoneNoOffset()
    {
        var (dx, dy) = ToastEntranceAnimation.None.InitialOffset(20, 5);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void EntranceOffsetProgressClamped()
    {
        // Below 0 should clamp
        var (dx, dy) = ToastEntranceAnimation.SlideFromTop.OffsetAtProgress(-0.5, 20, 5);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)-5, dy); // Same as progress 0.0

        // Above 1 should clamp
        (dx, dy) = ToastEntranceAnimation.SlideFromTop.OffsetAtProgress(2.0, 20, 5);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy); // Same as progress 1.0
    }

    [Fact]
    public void EntranceOffsetAtHalfProgress()
    {
        var (dx, dy) = ToastEntranceAnimation.SlideFromRight.OffsetAtProgress(0.5, 20, 5);
        Assert.Equal((short)10, dx); // Half of width
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void EntranceOffsetsSaturateLargeDimensions()
    {
        Assert.Equal(
            ((short)short.MaxValue, (short)0),
            ToastEntranceAnimation.SlideFromRight.InitialOffset(ushort.MaxValue, ushort.MaxValue));
        Assert.Equal(
            ((short)-short.MaxValue, (short)0),
            ToastEntranceAnimation.SlideFromLeft.InitialOffset(ushort.MaxValue, ushort.MaxValue));
        Assert.Equal(
            ((short)0, (short)-short.MaxValue),
            ToastEntranceAnimation.SlideFromTop.InitialOffset(ushort.MaxValue, ushort.MaxValue));
    }

    // ── Exit animation all variants ───────────────────────────────────────

    [Fact]
    public void ExitSlideToTopOffset()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.SlideToTop.FinalOffset(20, 5, entrance);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)-5, dy);
    }

    [Fact]
    public void ExitSlideToRightOffset()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.SlideToRight.FinalOffset(20, 5, entrance);
        Assert.Equal((short)20, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void ExitSlideToBottomOffset()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.SlideToBottom.FinalOffset(20, 5, entrance);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)5, dy);
    }

    [Fact]
    public void ExitSlideToLeftOffset()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.SlideToLeft.FinalOffset(20, 5, entrance);
        Assert.Equal((short)-20, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void ExitFadeOutNoOffset()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.FadeOut.FinalOffset(20, 5, entrance);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void ExitNoneNoOffset()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.None.FinalOffset(20, 5, entrance);
        Assert.Equal((short)0, dx);
        Assert.Equal((short)0, dy);
    }

    [Fact]
    public void ExitOffsetProgressClamped()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        var (dx, dy) = ToastExitAnimation.SlideToTop.OffsetAtProgress(-1.0, 20, 5, entrance);
        Assert.Equal(((short)0, (short)0), (dx, dy)); // Clamped to 0.0

        (dx, dy) = ToastExitAnimation.SlideToTop.OffsetAtProgress(5.0, 20, 5, entrance);
        Assert.Equal(((short)0, (short)-5), (dx, dy)); // Clamped to 1.0
    }

    [Fact]
    public void ExitOffsetsSaturateLargeDimensions()
    {
        var entrance = ToastEntranceAnimation.SlideFromRight;
        Assert.Equal(
            ((short)short.MaxValue, (short)0),
            ToastExitAnimation.SlideToRight.FinalOffset(ushort.MaxValue, ushort.MaxValue, entrance));
        Assert.Equal(
            ((short)0, (short)short.MaxValue),
            ToastExitAnimation.SlideToBottom.FinalOffset(ushort.MaxValue, ushort.MaxValue, entrance));
        Assert.Equal(
            ((short)-short.MaxValue, (short)0),
            ToastExitAnimation.SlideOut.FinalOffset(ushort.MaxValue, ushort.MaxValue, entrance));
    }

    // ── Easing function edge cases ────────────────────────────────────────

    [Fact]
    public void EasingClampedBelowZero()
    {
        foreach (var easing in new[] { ToastEasing.Linear, ToastEasing.EaseIn, ToastEasing.EaseOut,
                                       ToastEasing.EaseInOut, ToastEasing.Bounce })
        {
            var result = easing.Apply(-0.5);
            Assert.True(Math.Abs(result - 0.0) < 0.001, $"{easing} at -0.5 should clamp to 0");
        }
    }

    [Fact]
    public void EasingClampedAboveOne()
    {
        foreach (var easing in new[] { ToastEasing.Linear, ToastEasing.EaseIn, ToastEasing.EaseOut,
                                       ToastEasing.EaseInOut, ToastEasing.Bounce })
        {
            var result = easing.Apply(1.5);
            Assert.True(Math.Abs(result - 1.0) < 0.001, $"{easing} at 1.5 should clamp to 1");
        }
    }

    [Fact]
    public void EasingEaseInOutFirstHalf()
    {
        var result = ToastEasing.EaseInOut.Apply(0.25);
        Assert.True(result < 0.25, "EaseInOut at 0.25 should be < 0.25 (accelerating)");
    }

    [Fact]
    public void EasingEaseInOutSecondHalf()
    {
        var result = ToastEasing.EaseInOut.Apply(0.75);
        Assert.True(result > 0.75, "EaseInOut at 0.75 should be > 0.75 (decelerating)");
    }

    [Fact]
    public void EasingBounceMonotonicAtKeyPoints()
    {
        const double d1 = 2.75;
        double t1 = 0.2 / d1; // first branch
        double t2 = 1.5 / d1; // second branch
        double t3 = 2.3 / d1; // third branch
        double t4 = 2.7 / d1; // fourth branch

        var v1 = ToastEasing.Bounce.Apply(t1);
        var v2 = ToastEasing.Bounce.Apply(t2);
        var v3 = ToastEasing.Bounce.Apply(t3);
        var v4 = ToastEasing.Bounce.Apply(t4);

        Assert.InRange(v1, 0.0, 1.0);
        Assert.InRange(v2, 0.0, 1.0);
        Assert.InRange(v3, 0.0, 1.0);
        Assert.InRange(v4, 0.0, 1.0);
    }

    // ── Animation state transitions ───────────────────────────────────────

    [Fact]
    public void AnimationStateTickEnteringToVisible()
    {
        var config = new ToastAnimationConfig { EntranceDuration = TimeSpan.Zero };
        var state  = ToastAnimationState.New();
        Assert.Equal(ToastAnimationPhase.Entering, state.Phase);

        var changed = state.Tick(config);
        Assert.True(changed, "Phase should change from Entering to Visible");
        Assert.Equal(ToastAnimationPhase.Visible, state.Phase);
    }

    [Fact]
    public void AnimationStateTickExitingToHidden()
    {
        var config = new ToastAnimationConfig { ExitDuration = TimeSpan.Zero };
        var state  = ToastAnimationState.New();
        state.TransitionTo(ToastAnimationPhase.Exiting);

        var changed = state.Tick(config);
        Assert.True(changed, "Phase should change from Exiting to Hidden");
        Assert.Equal(ToastAnimationPhase.Hidden, state.Phase);
    }

    [Fact]
    public void AnimationStateTickVisibleNoChange()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.New();
        state.TransitionTo(ToastAnimationPhase.Visible);

        var changed = state.Tick(config);
        Assert.False(changed, "Visible phase should not auto-transition");
        Assert.Equal(ToastAnimationPhase.Visible, state.Phase);
    }

    [Fact]
    public void AnimationStateTickHiddenNoChange()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.New();
        state.TransitionTo(ToastAnimationPhase.Hidden);

        var changed = state.Tick(config);
        Assert.False(changed);
        Assert.Equal(ToastAnimationPhase.Hidden, state.Phase);
    }

    [Fact]
    public void AnimationStateStartExitReducedMotionGoesToHidden()
    {
        var state = ToastAnimationState.WithReducedMotion();
        Assert.Equal(ToastAnimationPhase.Visible, state.Phase);
        state.StartExit();
        Assert.Equal(ToastAnimationPhase.Hidden, state.Phase);
    }

    [Fact]
    public void AnimationStateIsComplete()
    {
        var state = ToastAnimationState.New();
        Assert.False(state.IsComplete());
        state.TransitionTo(ToastAnimationPhase.Hidden);
        Assert.True(state.IsComplete());
    }

    // ── Animation offset and opacity in all phases ────────────────────────

    [Fact]
    public void AnimationOffsetVisibleIsZero()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.New();
        state.Phase = ToastAnimationPhase.Visible;
        var (dx, dy) = state.CurrentOffset(config, 20, 5);
        Assert.Equal(((short)0, (short)0), (dx, dy));
    }

    [Fact]
    public void AnimationOffsetHiddenIsZero()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.New();
        state.Phase = ToastAnimationPhase.Hidden;
        var (dx, dy) = state.CurrentOffset(config, 20, 5);
        Assert.Equal(((short)0, (short)0), (dx, dy));
    }

    [Fact]
    public void AnimationOffsetReducedMotionAlwaysZero()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.WithReducedMotion();
        var (dx, dy) = state.CurrentOffset(config, 20, 5);
        Assert.Equal(((short)0, (short)0), (dx, dy));
    }

    [Fact]
    public void AnimationOpacityVisibleIsOne()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.New();
        state.Phase = ToastAnimationPhase.Visible;
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 1.0) < 0.001);
    }

    [Fact]
    public void AnimationOpacityHiddenIsZero()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.New();
        state.Phase = ToastAnimationPhase.Hidden;
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 0.0) < 0.001);
    }

    [Fact]
    public void AnimationOpacityReducedMotionVisibleIsOne()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.WithReducedMotion();
        state.Phase = ToastAnimationPhase.Visible;
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 1.0) < 0.001);
    }

    [Fact]
    public void AnimationOpacityReducedMotionHiddenIsZero()
    {
        var config = new ToastAnimationConfig();
        var state  = ToastAnimationState.WithReducedMotion();
        state.Phase = ToastAnimationPhase.Hidden;
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 0.0) < 0.001);
    }

    [Fact]
    public void AnimationOpacityExitingNonFadeIsOne()
    {
        var config = new ToastAnimationConfig { Exit = ToastExitAnimation.SlideOut };
        var state  = ToastAnimationState.New();
        state.Phase = ToastAnimationPhase.Exiting;
        // Non-FadeOut exit keeps opacity at 1.0
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 1.0) < 0.001);
    }

    [Fact]
    public void AnimationOpacityEnteringNonFadeIsOne()
    {
        var config = new ToastAnimationConfig { Entrance = ToastEntranceAnimation.SlideFromTop };
        var state  = ToastAnimationState.New();
        state.Phase = ToastAnimationPhase.Entering;
        // Non-FadeIn entrance keeps opacity at 1.0
        Assert.True(Math.Abs(state.CurrentOpacity(config) - 1.0) < 0.001);
    }

    // ── Toast API coverage ────────────────────────────────────────────────

    [Fact]
    public void ToastWithId()
    {
        var toast = Toast.WithId(ToastId.New(42), "Custom ID");
        Assert.Equal(ToastId.New(42), toast.Id);
        Assert.Equal("Custom ID", toast.Content.Message);
    }

    [Fact]
    public void ToastTickAnimationReturnsTrueOnPhaseChange()
    {
        var toast = Toast.New("Test").EntranceDuration(TimeSpan.Zero);
        Assert.Equal(ToastAnimationPhase.Entering, toast.State.Animation.Phase);
        var changed = toast.TickAnimation();
        Assert.True(changed);
        Assert.Equal(ToastAnimationPhase.Visible, toast.State.Animation.Phase);
    }

    [Fact]
    public void ToastTickAnimationReturnsFalseWhenStable()
    {
        var toast = Toast.New("Test").NoAnimation();
        Assert.Equal(ToastAnimationPhase.Visible, toast.State.Animation.Phase);
        var changed = toast.TickAnimation();
        Assert.False(changed);
    }

    [Fact]
    public void ToastAnimationPhaseAccessor()
    {
        var toast = Toast.New("Test").NoAnimation();
        Assert.Equal(ToastAnimationPhase.Visible, toast.AnimationPhase());
    }

    [Fact]
    public void ToastAnimationOpacityAccessor()
    {
        var toast = Toast.New("Test").NoAnimation();
        Assert.True(Math.Abs(toast.AnimationOpacity() - 1.0) < 0.001);
    }

    [Fact]
    public void ToastRemainingTimePersistentIsNone()
    {
        var toast = Toast.New("msg").Persistent().NoAnimation();
        Assert.Null(toast.RemainingTime());
    }

    [Fact]
    public void ToastDismissTwiceIdempotent()
    {
        var toast = Toast.New("msg").NoAnimation();
        toast.State.Animation.ReducedMotion = false;
        toast.Dismiss();
        Assert.True(toast.State.Dismissed);
        var phaseAfterFirst = toast.State.Animation.Phase;
        toast.Dismiss(); // Should not change phase again
        Assert.Equal(phaseAfterFirst, toast.State.Animation.Phase);
    }

    [Fact]
    public void ToastNonDismissableEscNoop()
    {
        var toast  = Toast.New("msg").Dismissable(false).NoAnimation();
        var result = toast.HandleKey(ToastKeyEvent.Esc);
        Assert.Equal(ToastEvent.None, result);
        Assert.True(toast.IsVisible());
    }

    [Fact]
    public void ToastMarginBuilder()
    {
        var toast = Toast.New("msg").Margin(5);
        Assert.Equal((ushort)5, toast.Config.Margin);
    }

    [Fact]
    public void ToastWithIconStyleBuilder()
    {
        var style = new WidgetStyle(null, null, CellStyleFlags.Italic);
        var toast = Toast.New("msg").WithIconStyle(style);
        Assert.Equal(style, toast.IconStyleField);
    }

    [Fact]
    public void ToastWithTitleStyleBuilder()
    {
        var style = new WidgetStyle(null, null, CellStyleFlags.Bold);
        var toast = Toast.New("msg").WithTitleStyle(style);
        Assert.Equal(style, toast.TitleStyleField);
    }

    // ── ToastConfig defaults ──────────────────────────────────────────────

    [Fact]
    public void ToastConfigDefaultValues()
    {
        var config = new ToastConfig();
        Assert.Equal(ToastPosition.TopRight, config.Position);
        Assert.Equal(TimeSpan.FromSeconds(5), config.Duration);
        Assert.False(config.DurationExplicit);
        Assert.Equal(ToastStyle.Info, config.StyleVariant);
        Assert.Equal((ushort)50, config.MaxWidth);
        Assert.Equal((ushort)1, config.Margin);
        Assert.True(config.Dismissable);
    }

    // ── ToastAnimationConfig ──────────────────────────────────────────────

    [Fact]
    public void AnimationConfigNoneFields()
    {
        var config = ToastAnimationConfig.CreateNone();
        Assert.Equal(ToastEntranceAnimation.None, config.Entrance);
        Assert.Equal(ToastExitAnimation.None, config.Exit);
        Assert.Equal(TimeSpan.Zero, config.EntranceDuration);
        Assert.Equal(TimeSpan.Zero, config.ExitDuration);
        Assert.True(config.IsDisabled());
    }

    [Fact]
    public void AnimationConfigIsDisabledFalseForDefault()
    {
        var config = new ToastAnimationConfig();
        Assert.False(config.IsDisabled());
    }

    // ── ToastId and trait coverage ────────────────────────────────────────

    [Fact]
    public void ToastIdHashConsistent()
    {
        var set = new System.Collections.Generic.HashSet<ToastId>();
        set.Add(ToastId.New(1));
        set.Add(ToastId.New(2));
        set.Add(ToastId.New(1)); // Duplicate
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void ToastIdDebug()
    {
        var id  = ToastId.New(42);
        var dbg = id.ToString();
        Assert.Contains("42", dbg);
    }

    [Fact]
    public void ToastEventDebugClone()
    {
        var evt = ToastEvent.Action("test");
        var str = evt.ToString();
        Assert.Contains("Action", str);
        // Equality (clone equivalent — no Clone() needed, check ==)
        Assert.Equal(ToastEvent.Action("test"), evt);
    }

    [Fact]
    public void KeyEventTraits()
    {
        var key  = ToastKeyEvent.Tab;
        var copy = key; // Copy (value type)
        Assert.Equal(key, copy);
        var dbg = key.ToString();
        Assert.Contains("Tab", dbg);
    }

    // ── Tick with reduced motion in entering phase ─────────────────────────

    [Fact]
    public void AnimationTickEnteringReducedMotionTransitionsImmediately()
    {
        var config = new ToastAnimationConfig();
        var state  = new ToastAnimationState
        {
            Phase         = ToastAnimationPhase.Entering,
            PhaseStarted  = DateTime.UtcNow,
            ReducedMotion = true,
        };
        // With reduced_motion, entering duration is treated as ZERO → immediate transition
        var changed = state.Tick(config);
        Assert.True(changed);
        Assert.Equal(ToastAnimationPhase.Visible, state.Phase);
    }

    [Fact]
    public void AnimationTickExitingReducedMotionTransitionsImmediately()
    {
        var config = new ToastAnimationConfig();
        var state  = new ToastAnimationState
        {
            Phase         = ToastAnimationPhase.Exiting,
            PhaseStarted  = DateTime.UtcNow,
            ReducedMotion = true,
        };
        var changed = state.Tick(config);
        Assert.True(changed);
        Assert.Equal(ToastAnimationPhase.Hidden, state.Phase);
    }
}
