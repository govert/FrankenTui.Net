using System.Text;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class InputContractsTests
{
    [Fact]
    public void GestureRecognizerRecognizesClickDoubleClickDragAndCancel()
    {
        var start = new DateTimeOffset(2026, 3, 11, 8, 0, 0, TimeSpan.Zero);
        var recognizer = new GestureRecognizer();

        var firstClick = recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Down),
            start));
        Assert.Empty(firstClick);

        var click = recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Up),
            start + TimeSpan.FromMilliseconds(40)));
        Assert.Single(click);
        Assert.IsType<ClickSemanticEvent>(click[0]);

        recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Down),
            start + TimeSpan.FromMilliseconds(120)));
        var doubleClick = recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(3, 2, TerminalMouseButton.Left, TerminalMouseKind.Up),
            start + TimeSpan.FromMilliseconds(150)));
        Assert.Single(doubleClick);
        Assert.IsType<DoubleClickSemanticEvent>(doubleClick[0]);

        recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(1, 1, TerminalMouseButton.Left, TerminalMouseKind.Down),
            start + TimeSpan.FromMilliseconds(200)));
        var dragStart = recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(5, 1, TerminalMouseButton.Left, TerminalMouseKind.Drag),
            start + TimeSpan.FromMilliseconds(220)));
        Assert.Single(dragStart);
        Assert.IsType<DragStartSemanticEvent>(dragStart[0]);

        var dragCancel = recognizer.Process(TerminalEvent.Focus(false, start + TimeSpan.FromMilliseconds(230)));
        Assert.Single(dragCancel);
        Assert.IsType<DragCancelSemanticEvent>(dragCancel[0]);
    }

    [Fact]
    public void GestureRecognizerRecognizesLongPressAndChord()
    {
        var start = new DateTimeOffset(2026, 3, 11, 8, 10, 0, TimeSpan.Zero);
        var recognizer = new GestureRecognizer();

        recognizer.Process(TerminalEvent.Mouse(
            new MouseGesture(7, 4, TerminalMouseButton.Left, TerminalMouseKind.Down),
            start));
        var longPress = recognizer.Flush(start + TimeSpan.FromMilliseconds(700));
        Assert.Single(longPress);
        var longPressEvent = Assert.IsType<LongPressSemanticEvent>(longPress[0]);
        Assert.True(longPressEvent.Duration >= TimeSpan.FromMilliseconds(500));

        recognizer.Reset();
        recognizer.Process(TerminalEvent.Key(
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('k')),
            start + TimeSpan.FromMilliseconds(800)));
        var chord = recognizer.Process(TerminalEvent.Key(
            new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('c')),
            start + TimeSpan.FromMilliseconds(900)));
        Assert.Single(chord);
        var chordEvent = Assert.IsType<ChordSemanticEvent>(chord[0]);
        Assert.Equal(2, chordEvent.Sequence.Count);
    }

    [Fact]
    public void KeybindingResolverHonorsPriorityAndEscapeSequence()
    {
        var start = new DateTimeOffset(2026, 3, 11, 8, 20, 0, TimeSpan.Zero);
        var resolver = new KeybindingResolver();

        var modalCtrlC = resolver.Resolve(
            TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('c')), start),
            new KeybindingState(ModalOpen: true));
        Assert.Equal(KeybindingAction.DismissModal, Assert.Single(modalCtrlC).Action);

        var firstEscape = resolver.Resolve(
            TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(10)),
            new KeybindingState(ViewOverlay: true));
        Assert.Empty(firstEscape);

        var secondEscape = resolver.Resolve(
            TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(40)),
            new KeybindingState(ViewOverlay: true));
        Assert.Equal(KeybindingAction.ToggleTreeView, Assert.Single(secondEscape).Action);

        var pendingEscape = resolver.Resolve(
            TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(400)),
            new KeybindingState(InputNonEmpty: true));
        Assert.Empty(pendingEscape);

        var flushed = resolver.FlushExpired(
            new KeybindingState(InputNonEmpty: true),
            start + TimeSpan.FromMilliseconds(700));
        Assert.Equal(KeybindingAction.ClearInput, Assert.Single(flushed).Action);
    }

    [Fact]
    public void ResizeCoalescerCoalescesBurstAndHonorsDeadline()
    {
        var start = new DateTimeOffset(2026, 3, 11, 8, 30, 0, TimeSpan.Zero);
        var coalescer = new ResizeCoalescer();

        var first = coalescer.Observe(new Size(80, 24), start);
        Assert.Equal(CoalesceAction.Coalesce, first.Action);

        coalescer.Observe(new Size(100, 30), start + TimeSpan.FromMilliseconds(10));
        var third = coalescer.Observe(new Size(120, 35), start + TimeSpan.FromMilliseconds(20));
        Assert.Equal(ResizeRegime.Burst, third.Regime);
        Assert.True(
            third.Action is CoalesceAction.Coalesce or CoalesceAction.SkipFrame,
            $"Unexpected action {third.Action}");

        var deadline = Assert.IsType<ResizeDecision>(coalescer.Evaluate(start + TimeSpan.FromMilliseconds(130)));
        Assert.Equal(CoalesceAction.RenderNow, deadline.Action);
        Assert.True(deadline.ForcedDeadline);
        Assert.Equal(new Size(120, 35), coalescer.ConsumeReadySize(deadline.Action));
    }

    [Fact]
    public void HostedParityInputEngineAppliesPolicySemanticAndResize()
    {
        var start = new DateTimeOffset(2026, 3, 11, 8, 40, 0, TimeSpan.Zero);
        var engine = new HostedParityInputEngine();
        var session = HostedParitySession.Create(false);

        var typed = engine.Process(
            session,
            TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('x')), start));
        Assert.Equal("x", typed.Session.InputBuffer);

        var cleared = engine.Process(
            typed.Session,
            TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('c')), start + TimeSpan.FromMilliseconds(20)));
        Assert.Empty(cleared.Session.InputBuffer);
        Assert.Contains(cleared.Session.PolicyLog, static item => item.Contains("ClearInput", StringComparison.Ordinal));

        var down = engine.Process(
            cleared.Session,
            TerminalEvent.Mouse(new MouseGesture(2, 0, TerminalMouseButton.Left, TerminalMouseKind.Down), start + TimeSpan.FromMilliseconds(40)));
        var click = engine.Process(
            down.Session,
            TerminalEvent.Mouse(new MouseGesture(2, 0, TerminalMouseButton.Left, TerminalMouseKind.Up), start + TimeSpan.FromMilliseconds(60)));
        Assert.Contains(click.Session.SemanticLog, static item => item.Contains("semantic click", StringComparison.Ordinal));

        var resizeObserved = engine.Process(
            click.Session,
            TerminalEvent.Resize(new Size(90, 28), start + TimeSpan.FromMilliseconds(80)));
        Assert.Null(resizeObserved.ResizeToApply);

        var resizeReady = engine.Tick(resizeObserved.Session, start + TimeSpan.FromMilliseconds(120));
        Assert.Equal(new Size(90, 28), resizeReady.ResizeToApply);
        Assert.Contains(resizeReady.Session.ResizeLog, static item => item.Contains("resize RenderNow", StringComparison.Ordinal));
    }
}
