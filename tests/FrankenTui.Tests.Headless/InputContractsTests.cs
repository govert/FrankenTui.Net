// Ported regression coverage for .external/frankentui/crates/ftui-core/src/input_parser.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c;
// commits c63f7dd4, 56170378, 5304e051, and edd35dd2.

using System.Text;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class InputContractsTests
{
    [Fact]
    public void TerminalInputParserConsumesDcsResponsesWithoutInjectingKeys()
    {
        var parser = new TerminalInputParser();

        var stTerminated = parser.Parse("\u001bP1+r524742=8/8/8\u001b\\a");
        var stKey = Assert.IsType<KeyTerminalEvent>(Assert.Single(stTerminated));
        Assert.Equal(new Rune('a'), stKey.Gesture.Character);

        var belTerminated = parser.Parse("\u001bPignored\u0007b");
        var belKey = Assert.IsType<KeyTerminalEvent>(Assert.Single(belTerminated));
        Assert.Equal(new Rune('b'), belKey.Gesture.Character);

        Assert.Empty(parser.Parse("\u001bPunterminated-response"));
        Assert.Empty(parser.Parse("\u001bPunterminated-response\u001b"));
    }

    [Fact]
    public void TerminalInputParserKeepsSiblingIntroducersAsAltInput()
    {
        var parser = new TerminalInputParser();

        var events = parser.Parse("\u001b^");

        var key = Assert.IsType<KeyTerminalEvent>(Assert.Single(events));
        Assert.Equal(new Rune('^'), key.Gesture.Character);
        Assert.Equal(TerminalModifiers.Alt, key.Gesture.Modifiers);
    }

    [Fact]
    public void TerminalInputParserMarksAltPrefixedAsciiAndUnicode()
    {
        var parser = new TerminalInputParser();

        var events = parser.Parse("\u001bx\u001bé");

        Assert.Collection(
            events,
            first =>
            {
                var key = Assert.IsType<KeyTerminalEvent>(first);
                Assert.Equal(new Rune('x'), key.Gesture.Character);
                Assert.Equal(TerminalModifiers.Alt, key.Gesture.Modifiers);
            },
            second =>
            {
                var key = Assert.IsType<KeyTerminalEvent>(second);
                Assert.Equal(new Rune('é'), key.Gesture.Character);
                Assert.Equal(TerminalModifiers.Alt, key.Gesture.Modifiers);
            });
    }

    [Fact]
    public void TerminalInputParserIgnoresStrayPasteEndAndReprocessesCsiControl()
    {
        var parser = new TerminalInputParser();

        Assert.Empty(parser.Parse("\u001b[201~"));

        var events = parser.Parse("\u001b[\r");
        var key = Assert.IsType<KeyTerminalEvent>(Assert.Single(events));
        Assert.Equal(TerminalKey.Enter, key.Gesture.Key);
        Assert.Equal(TerminalModifiers.None, key.Gesture.Modifiers);
    }

    // Source: .external/frankentui/crates/ftui-core/src/input_parser.rs at
    // 15cc6543f76b814394c590f9e7719dedd6684e4c (incremental_matches_bulk,
    // utf8_4byte_emoji, legacy_key_with_kitty_event_type_subparam_decodes_release_and_mods,
    // bracketed_paste).
    [Fact]
    public void TerminalInputParserStreamsUtf8CsiAndPasteAcrossChunks()
    {
        var parser = new TerminalInputParser();

        Assert.Empty(parser.Parse(new byte[] { 0xF0, 0x9F }));
        var unicode = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse(new byte[] { 0x98, 0x80 })));
        Assert.Equal(new Rune(0x1F600), unicode.Gesture.Character);

        Assert.Empty(parser.Parse("\u001b["));
        var arrow = Assert.IsType<KeyTerminalEvent>(Assert.Single(parser.Parse("1;5:3A")));
        Assert.Equal(TerminalKey.Up, arrow.Gesture.Key);
        Assert.Equal(TerminalModifiers.Control, arrow.Gesture.Modifiers);
        Assert.Equal(TerminalKeyEventKind.Release, arrow.Kind);

        Assert.Empty(parser.Parse("\u001b[200~abc"));
        var paste = Assert.IsType<PasteTerminalEvent>(Assert.Single(
            parser.Parse("def\u001b[201~")));
        Assert.Equal("abcdef", paste.Text);
    }

    // Source: input_parser.rs at 15cc6543 (kitty_keyboard_with_modifiers_and_kind,
    // kitty_keyboard_f_keys, kitty_keyboard_reserved_keycode_ignored, ctrl_space_is_null).
    [Fact]
    public void TerminalInputParserPreservesKittyKindsExtendedKeysAndNull()
    {
        var parser = new TerminalInputParser();

        var repeat = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse("\u001b[97;5:2u")));
        Assert.Equal(new Rune('a'), repeat.Gesture.Character);
        Assert.Equal(TerminalModifiers.Control, repeat.Gesture.Modifiers);
        Assert.Equal(TerminalKeyEventKind.Repeat, repeat.Kind);

        var release = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse("\u001b[97;9:3u")));
        Assert.Equal(TerminalModifiers.Super, release.Gesture.Modifiers);
        Assert.Equal(TerminalKeyEventKind.Release, release.Kind);

        var f24 = Assert.IsType<KeyTerminalEvent>(Assert.Single(parser.Parse("\u001b[57387u")));
        Assert.Equal(TerminalKey.F24, f24.Gesture.Key);
        Assert.Empty(parser.Parse("\u001b[57360u"));

        var nullKey = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse(new byte[] { 0x00 })));
        Assert.Equal(TerminalKey.Null, nullKey.Gesture.Key);
        Assert.Equal(TerminalModifiers.None, nullKey.Gesture.Modifiers);
    }

    // Source: input_parser.rs at 15cc6543 (csi_ignore_handles_final_bytes,
    // dos_protection_csi, oversized_osc_transitions_to_ignore, osc52_*).
    [Fact]
    public void TerminalInputParserBoundsControlStringsAndRecovers()
    {
        var parser = new TerminalInputParser();

        var afterExactCsiBoundary = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse("\u001b[" + new string('0', 256) + "@w")));
        Assert.Equal(new Rune('w'), afterExactCsiBoundary.Gesture.Character);

        var afterCsi = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse("\u001b[" + new string('1', 257) + "Ax")));
        Assert.Equal(new Rune('x'), afterCsi.Gesture.Character);

        var afterExactOscBoundary = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse("\u001b]" + new string('a', 102_400) + "\u0007y")));
        Assert.Equal(new Rune('y'), afterExactOscBoundary.Gesture.Character);

        var afterOsc = Assert.IsType<KeyTerminalEvent>(Assert.Single(
            parser.Parse("\u001b]" + new string('a', 102_401) + "\u0007z")));
        Assert.Equal(new Rune('z'), afterOsc.Gesture.Character);

        Assert.Empty(parser.Parse("\u001b]52;c;Y2xpcGJvYXJk"));
        var afterSt = Assert.IsType<KeyTerminalEvent>(Assert.Single(parser.Parse("\u001b\\q")));
        Assert.Equal(new Rune('q'), afterSt.Gesture.Character);

        var boundedPaste = Assert.IsType<PasteTerminalEvent>(Assert.Single(
            parser.Parse("\u001b[200~" + new string('p', 1_048_596) + "\u001b[201~")));
        Assert.Equal(1_048_576, boundedPaste.Text.Length);
    }

    // Source: input_parser.rs at 15cc6543 (mouse_x10_when_enabled,
    // mouse_x10_malformed_packet_ignored, x10_mouse_scroll_codes_not_misread_as_button_events).
    [Fact]
    public void TerminalInputParserGatesAndStreamsX10MousePackets()
    {
        var disabled = new TerminalInputParser();
        Assert.DoesNotContain(
            disabled.Parse(new byte[] { 0x1B, (byte)'[', (byte)'M', 32, 42, 52 }),
            item => item is MouseTerminalEvent);

        var parser = new TerminalInputParser();
        parser.SetExpectX10Mouse(true);
        Assert.Empty(parser.Parse(new byte[] { 0x1B, (byte)'[', (byte)'M', 32 }));
        var mouse = Assert.IsType<MouseTerminalEvent>(Assert.Single(
            parser.Parse(new byte[] { 42, 52 })));
        Assert.Equal((ushort)9, mouse.Gesture.Column);
        Assert.Equal((ushort)19, mouse.Gesture.Row);
        Assert.Equal(TerminalMouseButton.Left, mouse.Gesture.Button);
        Assert.Equal(TerminalMouseKind.Down, mouse.Gesture.Kind);

        Assert.Empty(parser.Parse(new byte[] { 0x1B, (byte)'[', (byte)'M', 31, 42, 52 }));
    }

    // Source: input_parser.rs at 15cc6543 (timeout, truncated_utf8_lead_bytes).
    [Fact]
    public void TerminalInputParserResolvesTimedOutEscapeAndUtf8States()
    {
        var parser = new TerminalInputParser();

        Assert.Empty(parser.Parse(new byte[] { 0x1B }));
        Assert.True(parser.HasPendingTimeoutState);
        var escape = Assert.IsType<KeyTerminalEvent>(parser.Timeout());
        Assert.Equal(TerminalKey.Escape, escape.Gesture.Key);

        Assert.Empty(parser.Parse(new byte[] { 0xF0, 0x9F }));
        Assert.True(parser.HasPendingTimeoutState);
        var replacement = Assert.IsType<KeyTerminalEvent>(parser.Timeout());
        Assert.Equal(Rune.ReplacementChar, replacement.Gesture.Character);
        Assert.False(parser.HasPendingTimeoutState);
    }

    // Source: input_parser.rs at 15cc6543 (random_bytes_never_panic,
    // incremental_matches_bulk, deterministic_output). This is a fixed-seed managed
    // sample, not a claim that the complete upstream proptest corpus was duplicated.
    [Fact]
    public void TerminalInputParserIsDeterministicAcrossRandomChunkBoundaries()
    {
        var random = new Random(0x15CC6543);
        var stamp = DateTimeOffset.UnixEpoch;

        for (int sample = 0; sample < 128; sample++)
        {
            var bytes = new byte[random.Next(0, 129)];
            random.NextBytes(bytes);

            var bulk = new TerminalInputParser().Parse(bytes, stamp);
            var incrementalParser = new TerminalInputParser();
            var incremental = new List<TerminalEvent>();
            foreach (byte value in bytes)
                incremental.AddRange(incrementalParser.Parse(new byte[] { value }, stamp));

            Assert.Equal(bulk, TerminalEventCoalescer.Coalesce(incremental));
            Assert.Equal(bulk, new TerminalInputParser().Parse(bytes, stamp));
        }
    }

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
