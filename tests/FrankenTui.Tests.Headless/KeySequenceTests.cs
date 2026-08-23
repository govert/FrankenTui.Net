// SPDX-License-Identifier: Apache-2.0
// Behavioral port of .external/frankentui/crates/ftui-core/src/key_sequence.rs tests.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;
using System.Text;

namespace FrankenTui.Tests.Headless;

public sealed class KeySequenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 16, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DoubleEscapeWithinTimeoutEmitsCapturedSequence()
    {
        var interpreter = new KeySequenceInterpreter();

        var first = interpreter.Feed(Escape(Start));
        var second = interpreter.Feed(Escape(Start + TimeSpan.FromMilliseconds(100)));

        Assert.IsType<KeySequenceAction.Pending>(first);
        var sequence = Assert.IsType<KeySequenceAction.EmitSequence>(second);
        Assert.Equal(KeySequenceKind.DoubleEscape, sequence.Kind);
        Assert.Equal(2, sequence.Keys.Count);
        Assert.All(sequence.Keys, key => Assert.Equal(TerminalKey.Escape, key.Gesture.Key));
        Assert.True(sequence.IsSequence);
        Assert.False(interpreter.HasPending);
    }

    [Fact]
    public void SingleEscapeFlushesAtTimeoutBoundary()
    {
        var interpreter = new KeySequenceInterpreter();
        interpreter.Feed(Escape(Start));

        Assert.Null(interpreter.CheckTimeout(Start + TimeSpan.FromMilliseconds(249)));
        var actions = Assert.Single(interpreter.CheckTimeout(Start + TimeSpan.FromMilliseconds(250))!);

        Assert.IsType<KeySequenceAction.Emit>(actions);
        Assert.False(interpreter.HasPending);
    }

    [Fact]
    public void EscapeThenDifferentKeyMatchesUpstreamNoMatchDegradation()
    {
        var interpreter = new KeySequenceInterpreter();
        interpreter.Feed(Escape(Start));

        var action = interpreter.Feed(Character('a', Start + TimeSpan.FromMilliseconds(50)));

        var emitted = Assert.IsType<KeySequenceAction.Emit>(action);
        Assert.Equal(new Rune('a'), emitted.Key.Gesture.Character);
        Assert.False(interpreter.HasPending);
        Assert.Empty(interpreter.Flush());
    }

    [Fact]
    public void RegularAndModifiedEscapeKeysPassThrough()
    {
        var interpreter = new KeySequenceInterpreter();

        Assert.IsType<KeySequenceAction.Emit>(interpreter.Feed(Character('x', Start)));
        Assert.IsType<KeySequenceAction.Emit>(interpreter.Feed(
            new KeyTerminalEvent(
                new KeyGesture(TerminalKey.Escape, TerminalModifiers.Control),
                Start + TimeSpan.FromMilliseconds(1))));
        Assert.False(interpreter.HasPending);
    }

    [Fact]
    public void CustomTimeoutAndRemainingTimeUseFirstKeyCoordinate()
    {
        var interpreter = new KeySequenceInterpreter(KeySequenceConfig.WithTimeout(TimeSpan.FromMilliseconds(100)));
        interpreter.Feed(Escape(Start));

        Assert.Equal(TimeSpan.FromMilliseconds(50), interpreter.TimeUntilTimeout(Start + TimeSpan.FromMilliseconds(50)));
        Assert.Null(interpreter.CheckTimeout(Start + TimeSpan.FromMilliseconds(99)));
        Assert.NotNull(interpreter.CheckTimeout(Start + TimeSpan.FromMilliseconds(100)));
        Assert.Null(interpreter.TimeUntilTimeout(Start + TimeSpan.FromMilliseconds(101)));
    }

    [Fact]
    public void ClockRegressionSaturatesInsteadOfExpiring()
    {
        var interpreter = new KeySequenceInterpreter();
        interpreter.Feed(Escape(Start));

        Assert.Equal(KeySequenceConfig.DefaultSequenceTimeout, interpreter.TimeUntilTimeout(Start - TimeSpan.FromSeconds(1)));
        Assert.Null(interpreter.CheckTimeout(Start - TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void DisabledDoubleEscapePassesBothKeysThrough()
    {
        var interpreter = new KeySequenceInterpreter(new KeySequenceConfig { DetectDoubleEscape = false });

        Assert.IsType<KeySequenceAction.Emit>(interpreter.Feed(Escape(Start)));
        Assert.IsType<KeySequenceAction.Emit>(interpreter.Feed(Escape(Start + TimeSpan.FromMilliseconds(50))));
        Assert.False(interpreter.HasPending);
    }

    [Fact]
    public void ResetAndFlushClearPendingState()
    {
        var interpreter = new KeySequenceInterpreter();
        interpreter.Feed(Escape(Start));

        Assert.Single(interpreter.Flush());
        Assert.False(interpreter.HasPending);
        Assert.Empty(interpreter.Flush());

        interpreter.Feed(Escape(Start));
        interpreter.Reset();
        Assert.False(interpreter.HasPending);
        Assert.Empty(interpreter.Flush());
    }

    [Fact]
    public void ConfigCanBeReplacedAndRejectsNegativeDuration()
    {
        var interpreter = new KeySequenceInterpreter();
        var replacement = KeySequenceConfig.WithTimeout(TimeSpan.FromMilliseconds(500));

        interpreter.SetConfig(replacement);

        Assert.Same(replacement, interpreter.Config);
        Assert.Equal(TimeSpan.FromMilliseconds(500), interpreter.Config.SequenceTimeout);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            KeySequenceConfig.WithTimeout(TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void TripleEscapeProducesSequenceThenNewPendingSequence()
    {
        var interpreter = new KeySequenceInterpreter();

        Assert.IsType<KeySequenceAction.Pending>(interpreter.Feed(Escape(Start)));
        Assert.IsType<KeySequenceAction.EmitSequence>(
            interpreter.Feed(Escape(Start + TimeSpan.FromMilliseconds(50))));
        Assert.IsType<KeySequenceAction.Pending>(
            interpreter.Feed(Escape(Start + TimeSpan.FromMilliseconds(100))));
        Assert.True(interpreter.HasPending);
    }

    [Fact]
    public void ActionAndKindHelpersExposeVariantMeaning()
    {
        KeySequenceAction pending = KeySequenceAction.Pending.Instance;
        KeySequenceAction emitted = new KeySequenceAction.Emit(Escape(Start));

        Assert.True(pending.IsPending);
        Assert.False(pending.IsSequence);
        Assert.False(emitted.IsPending);
        Assert.False(emitted.IsSequence);
        Assert.Equal("Esc Esc", KeySequenceKind.DoubleEscape.Name());
    }

    private static KeyTerminalEvent Escape(DateTimeOffset timestamp) =>
        new(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), timestamp);

    private static KeyTerminalEvent Character(char value, DateTimeOffset timestamp) =>
        new(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(value)), timestamp);
}
