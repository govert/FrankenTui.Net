// SPDX-License-Identifier: Apache-2.0
// Tests ported from ftui-core/src/animation/timeline.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class TimelineTests
{
    private static readonly TimeSpan Ms100 = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Ms200 = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan Ms250 = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Ms300 = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Ms500 = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Sec1 = TimeSpan.FromSeconds(1);

    [Fact]
    public void EmptyTimelineHasCompleteProgress()
    {
        var timeline = new Timeline();
        Assert.Equal(1.0f, timeline.Progress);
        Assert.Equal(0, timeline.EventCount);
    }

    [Fact]
    public void SequentialEventsRunAtTheirOffsets()
    {
        var timeline = new Timeline()
            .Add(TimeSpan.Zero, new Fade(Ms200))
            .Add(Ms200, new Fade(Ms200))
            .Add(TimeSpan.FromMilliseconds(400), new Fade(Ms200))
            .SetDuration(TimeSpan.FromMilliseconds(600));
        timeline.Play();
        timeline.Tick(Ms100);
        AssertClose(0.5f, timeline.EventValueAt(0)!.Value);
        Assert.Equal(0.0f, timeline.EventValueAt(1));
        timeline.Tick(Ms200);
        Assert.True(timeline.EventValueAt(0) > 0.99f);
        AssertClose(0.5f, timeline.EventValueAt(1)!.Value);
        timeline.Tick(Ms300);
        Assert.True(timeline.IsComplete);
        Assert.Equal(1.0f, timeline.Progress);
    }

    [Fact]
    public void OverlappingEventsRunTogether()
    {
        var timeline = new Timeline()
            .Add(TimeSpan.Zero, new Fade(Ms500))
            .Add(Ms200, new Fade(Ms500))
            .SetDuration(TimeSpan.FromMilliseconds(700));
        timeline.Play();
        timeline.Tick(Ms300);
        AssertClose(0.6f, timeline.EventValueAt(0)!.Value, 0.02f);
        AssertClose(0.2f, timeline.EventValueAt(1)!.Value, 0.02f);
    }

    [Fact]
    public void LabeledEventsCanBeSought()
    {
        var timeline = new Timeline()
            .AddLabeled("intro", TimeSpan.Zero, new Fade(Ms500))
            .AddLabeled("main", Ms500, new Fade(Ms500))
            .SetDuration(Sec1);
        timeline.Play();
        Assert.True(timeline.SeekLabel("main"));
        Assert.True(timeline.EventValue("intro") > 0.99f);
        Assert.Equal(0.0f, timeline.EventValue("main"));
        Assert.False(timeline.SeekLabel("nonexistent"));
    }

    [Fact]
    public void FiniteLoopCountMeansAdditionalPlaythroughs()
    {
        var timeline = OneEventTimeline().SetLoopCount(LoopCount.Times(2));
        timeline.Play();
        timeline.Tick(Ms100);
        Assert.False(timeline.IsComplete);
        Assert.Equal(PlaybackState.Playing, timeline.State);
        timeline.Tick(Ms100);
        Assert.False(timeline.IsComplete);
        timeline.Tick(Ms100);
        Assert.True(timeline.IsComplete);
    }

    [Fact]
    public void InfiniteLoopNeverFinishes()
    {
        var timeline = OneEventTimeline().SetLoopCount(LoopCount.Infinite);
        timeline.Play();
        for (var index = 0; index < 100; index++)
        {
            timeline.Tick(Ms100);
        }

        Assert.False(timeline.IsComplete);
        Assert.Equal(PlaybackState.Playing, timeline.State);
    }

    [Fact]
    public void PauseAndResumeControlAdvancement()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Ms250);
        timeline.Pause();
        var pausedAt = timeline.CurrentTime;
        timeline.Tick(Ms500);
        Assert.Equal(pausedAt, timeline.CurrentTime);
        timeline.Resume();
        Assert.Equal(PlaybackState.Playing, timeline.State);
        timeline.Tick(Ms250);
        Assert.True(timeline.CurrentTime > pausedAt);
    }

    [Fact]
    public void SeekClampsToDuration()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Seek(Sec1);
        Assert.Equal(Ms500, timeline.CurrentTime);
    }

    [Fact]
    public void SeekResetsAndReticksAnimations()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Ms500);
        AssertClose(0.5f, timeline.EventValueAt(0)!.Value, 0.02f);
        timeline.Seek(Ms250);
        AssertClose(0.25f, timeline.EventValueAt(0)!.Value, 0.02f);
    }

    [Fact]
    public void StopResetsEverything()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Ms500);
        timeline.Stop();
        Assert.Equal(PlaybackState.Idle, timeline.State);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
        Assert.Equal(0.0f, timeline.EventValueAt(0));
    }

    [Fact]
    public void PlayRestartsFromBeginning()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Sec1);
        timeline.Play();
        Assert.Equal(PlaybackState.Playing, timeline.State);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
        Assert.Equal(0.0f, timeline.EventValueAt(0));
    }

    [Fact]
    public void ThenChainsAtSameMaximumOffset()
    {
        var timeline = new Timeline().Add(Ms100, new Fade(Ms100)).Then(new Fade(Ms100));
        Assert.Equal(2, timeline.EventCount);
        Assert.Equal(Ms100, timeline.Duration);
    }

    [Fact]
    public void ProgressTracksTime()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        Assert.Equal(0.0f, timeline.Progress);
        timeline.Tick(Ms250);
        AssertClose(0.25f, timeline.Progress, 0.02f);
        timeline.Tick(Ms250);
        AssertClose(0.5f, timeline.Progress, 0.02f);
    }

    [Fact]
    public void AnimationValueMatchesProgress()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Ms500);
        Assert.Equal(timeline.Progress, timeline.Value);
    }

    [Fact]
    public void AnimationResetReturnsToIdle()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Sec1);
        timeline.Reset();
        Assert.Equal(PlaybackState.Idle, timeline.State);
        Assert.False(timeline.IsComplete);
    }

    [Fact]
    public void DebugFormatNamesTimelineAndEventCount()
    {
        var text = OneEventTimeline().ToString();
        Assert.Contains("Timeline", text, StringComparison.Ordinal);
        Assert.Contains("event_count", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LoopOncePlaysExactlyOnce()
    {
        var timeline = OneEventTimeline().SetLoopCount(LoopCount.Once);
        timeline.Play();
        timeline.Tick(Ms100);
        Assert.True(timeline.IsComplete);
    }

    [Fact]
    public void MissingLabelReturnsNull() => Assert.Null(OneEventTimeline().EventValue("nope"));

    [Fact]
    public void OutOfBoundsIndexReturnsNull()
    {
        Assert.Null(OneEventTimeline().EventValueAt(5));
        Assert.Null(OneEventTimeline().EventValueAt(-1));
    }

    [Fact]
    public void IdleTimelineValueIsZero()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        Assert.Equal(0.0f, timeline.Value);
        Assert.Equal(PlaybackState.Idle, timeline.State);
    }

    [Fact]
    public void OvershootIsZeroWhilePlaying()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms250);
        Assert.Equal(TimeSpan.Zero, timeline.Overshoot);
    }

    [Fact]
    public void SeekToZeroResetsAnimations()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms250);
        timeline.Seek(TimeSpan.Zero);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
        Assert.Equal(0.0f, timeline.EventValueAt(0));
    }

    [Fact]
    public void DefaultStateMatchesNewTimeline()
    {
        var timeline = new Timeline();
        Assert.Equal(0, timeline.EventCount);
        Assert.Equal(PlaybackState.Idle, timeline.State);
        Assert.Equal(1.0f, timeline.Progress);
    }

    [Fact]
    public void ZeroDurationClampsToOneManagedTick()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms100)).SetDuration(TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromTicks(1), timeline.Duration);
    }

    [Fact]
    public void ThenOnEmptyUsesZeroOffsetAndMinimumDuration()
    {
        var timeline = new Timeline().Then(new Fade(Ms100));
        Assert.Equal(1, timeline.EventCount);
        Assert.Equal(TimeSpan.FromTicks(1), timeline.Duration);
    }

    [Fact]
    public void PauseWhenNotPlayingIsNoOp()
    {
        var timeline = OneEventTimeline();
        timeline.Pause();
        Assert.Equal(PlaybackState.Idle, timeline.State);
        timeline.Play();
        timeline.Tick(Ms100);
        timeline.Pause();
        Assert.Equal(PlaybackState.Finished, timeline.State);
    }

    [Fact]
    public void ResumeWhenNotPausedIsNoOp()
    {
        var timeline = OneEventTimeline();
        timeline.Resume();
        Assert.Equal(PlaybackState.Idle, timeline.State);
        timeline.Play();
        timeline.Resume();
        Assert.Equal(PlaybackState.Playing, timeline.State);
    }

    [Fact]
    public void SeekFromIdleTransitionsToPaused()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Seek(Ms250);
        Assert.Equal(PlaybackState.Paused, timeline.State);
        Assert.Equal(Ms250, timeline.CurrentTime);
    }

    [Fact]
    public void SeekFromFinishedTransitionsToPaused()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms500);
        timeline.Seek(Ms250);
        Assert.Equal(PlaybackState.Paused, timeline.State);
        Assert.Equal(Ms250, timeline.CurrentTime);
    }

    [Fact]
    public void SeekFromPlayingStaysPlaying()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms100);
        timeline.Seek(Ms300);
        Assert.Equal(PlaybackState.Playing, timeline.State);
    }

    [Fact]
    public void FinishedOnceTimelineHasZeroOvershoot()
    {
        var timeline = OneEventTimeline();
        timeline.Play();
        timeline.Tick(Ms100);
        Assert.True(timeline.IsComplete);
        Assert.Equal(TimeSpan.Zero, timeline.Overshoot);
    }

    [Fact]
    public void TickWhenIdleDoesNotAdvance()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Tick(Ms250);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
        Assert.Equal(PlaybackState.Idle, timeline.State);
    }

    [Fact]
    public void TickWhenPausedDoesNotAdvance()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Sec1)).SetDuration(Sec1);
        timeline.Play();
        timeline.Tick(Ms250);
        timeline.Pause();
        var pausedAt = timeline.CurrentTime;
        timeline.Tick(Ms500);
        Assert.Equal(pausedAt, timeline.CurrentTime);
    }

    [Fact]
    public void TickWhenFinishedDoesNotAdvance()
    {
        var timeline = OneEventTimeline();
        timeline.Play();
        timeline.Tick(Ms100);
        var finishedAt = timeline.CurrentTime;
        timeline.Tick(Ms500);
        Assert.Equal(finishedAt, timeline.CurrentTime);
    }

    [Fact]
    public void MultipleEventsAtSameOffsetAllAdvance()
    {
        var timeline = new Timeline()
            .Add(TimeSpan.Zero, new Fade(Ms200))
            .Add(TimeSpan.Zero, new Fade(Ms200))
            .Add(TimeSpan.Zero, new Fade(Ms200))
            .SetDuration(Ms200);
        timeline.Play();
        timeline.Tick(Ms100);
        for (var index = 0; index < 3; index++)
        {
            AssertClose(0.5f, timeline.EventValueAt(index)!.Value, 0.02f);
        }
    }

    [Fact]
    public void AutoDurationUsesMaximumOffset()
    {
        var timeline = new Timeline()
            .Add(Ms100, new Fade(Ms100))
            .Add(Ms500, new Fade(Ms100))
            .Add(Ms300, new Fade(Ms100));
        Assert.Equal(Ms500, timeline.Duration);
    }

    [Fact]
    public void ExplicitDurationOverridesAutoDuration()
    {
        var timeline = new Timeline()
            .Add(Ms100, new Fade(Ms100))
            .Add(Ms500, new Fade(Ms100))
            .SetDuration(Sec1);
        Assert.Equal(Sec1, timeline.Duration);
    }

    [Fact]
    public void SeekLabelOnEmptyReturnsFalse() => Assert.False(new Timeline().SeekLabel("foo"));

    [Fact]
    public void EventValueAtOnEmptyReturnsNull() => Assert.Null(new Timeline().EventValueAt(0));

    [Fact]
    public void LoopTimesZeroPlaysOnce()
    {
        var timeline = OneEventTimeline().SetLoopCount(LoopCount.Times(0));
        timeline.Play();
        timeline.Tick(Ms100);
        Assert.True(timeline.IsComplete);
    }

    [Fact]
    public void LoopCountHasValueEquality()
    {
        Assert.Equal(LoopCount.Once, LoopCount.Once);
        Assert.Equal(LoopCount.Times(5), LoopCount.Times(5));
        Assert.NotEqual(LoopCount.Times(5), LoopCount.Times(3));
        Assert.Equal(LoopCount.Infinite, LoopCount.Infinite);
        Assert.NotEqual(LoopCount.Once, LoopCount.Infinite);
    }

    [Fact]
    public void PlaybackStateHasValueEquality()
    {
        Assert.Equal(PlaybackState.Idle, PlaybackState.Idle);
        Assert.NotEqual(PlaybackState.Idle, PlaybackState.Playing);
    }

    [Fact]
    public void LoopCountCopiesByValue()
    {
        var first = LoopCount.Times(3);
        var second = first;
        Assert.Equal(first, second);
    }

    [Fact]
    public void PlaybackStateCopiesByValue()
    {
        var first = PlaybackState.Paused;
        var second = first;
        Assert.Equal(first, second);
    }

    [Fact]
    public void PlayAfterStopResets()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms250);
        timeline.Stop();
        timeline.Play();
        Assert.Equal(PlaybackState.Playing, timeline.State);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
    }

    [Fact]
    public void SeekToEndThenTickFinishes()
    {
        var timeline = OneEventTimeline();
        timeline.Play();
        timeline.Seek(Ms100);
        timeline.Resume();
        timeline.Tick(TimeSpan.FromTicks(1));
        Assert.True(timeline.IsComplete);
    }

    [Fact]
    public void ProgressIsClampedToZeroOne()
    {
        var timeline = OneEventTimeline();
        Assert.InRange(timeline.Progress, 0.0f, 1.0f);
        timeline.Play();
        timeline.Tick(Ms100);
        Assert.InRange(timeline.Progress, 0.0f, 1.0f);
    }

    [Fact]
    public void IsCompleteFalseWhilePlaying()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms250);
        Assert.False(timeline.IsComplete);
    }

    [Fact]
    public void ResetFromFinished()
    {
        var timeline = OneEventTimeline();
        timeline.Play();
        timeline.Tick(Ms100);
        timeline.Reset();
        Assert.Equal(PlaybackState.Idle, timeline.State);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
        Assert.False(timeline.IsComplete);
    }

    [Fact]
    public void ResetFromPaused()
    {
        var timeline = new Timeline().Add(TimeSpan.Zero, new Fade(Ms500)).SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(Ms250);
        timeline.Pause();
        timeline.Reset();
        Assert.Equal(PlaybackState.Idle, timeline.State);
        Assert.Equal(TimeSpan.Zero, timeline.CurrentTime);
    }

    [Fact]
    public void LabeledEventValueTracksAnimation()
    {
        var timeline = new Timeline()
            .AddLabeled("fade", TimeSpan.Zero, new Fade(Ms200))
            .SetDuration(Ms200);
        timeline.Play();
        timeline.Tick(Ms100);
        AssertClose(0.5f, timeline.EventValue("fade")!.Value, 0.02f);
    }

    [Fact]
    public void EventsAreSortedByOffsetOnInsert()
    {
        var timeline = new Timeline()
            .Add(Ms500, new Fade(Ms100))
            .Add(Ms100, new Fade(Ms100))
            .Add(Ms300, new Fade(Ms100))
            .SetDuration(Ms500);
        timeline.Play();
        timeline.Tick(TimeSpan.FromMilliseconds(150));
        AssertClose(0.5f, timeline.EventValueAt(0)!.Value, 0.02f);
        Assert.InRange(timeline.EventValueAt(1)!.Value, 0.0f, 0.01f);
    }

    [Fact]
    public void DebugFormatIncludesSchedulerFields()
    {
        var text = new Timeline()
            .AddLabeled("intro", TimeSpan.Zero, new Fade(Ms100))
            .SetDuration(Ms100)
            .ToString();
        Assert.Contains("event_count", text, StringComparison.Ordinal);
        Assert.Contains("total_duration", text, StringComparison.Ordinal);
        Assert.Contains("state", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FiniteLoopForwardsOvershootIntoNextPlaythrough()
    {
        var timeline = OneEventTimeline().SetLoopCount(LoopCount.Times(1));
        timeline.Play();
        timeline.Tick(TimeSpan.FromMilliseconds(150));
        Assert.False(timeline.IsComplete);
        Assert.Equal(Ms50(), timeline.CurrentTime);
        AssertClose(0.5f, timeline.EventValueAt(0)!.Value, 0.02f);
    }

    [Fact]
    public void NegativeOffsetsDurationsSeeksAndTicksAreRejected()
    {
        var negative = TimeSpan.FromTicks(-1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timeline().Add(negative, new Fade(Ms100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timeline().SetDuration(negative));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timeline().Seek(negative));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Timeline().Tick(negative));
    }

    private static Timeline OneEventTimeline() =>
        new Timeline().Add(TimeSpan.Zero, new Fade(Ms100)).SetDuration(Ms100);

    private static TimeSpan Ms50() => TimeSpan.FromMilliseconds(50);

    private static void AssertClose(float expected, float actual, float tolerance = 0.01f) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
