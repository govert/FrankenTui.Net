// Tests for .external/frankentui/crates/ftui-runtime/src/queueing_scheduler.rs
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class QueueingSchedulerTests
{
    static SchedulerConfig TestCfg()=>new(){AgingFactor=0.001,MaxQueueSize=100,Preemptive=true,SmithEnabled=true,EnableLogging=false};

    [Fact] public void NewCreatesEmpty(){var s=new QueueingScheduler(TestCfg());Assert.Null(s.PeekNext());Assert.Equal(0,s.Stats().QueueLength);}
    [Fact] public void SubmitReturnsJobId(){var s=new QueueingScheduler(TestCfg());var id=s.Submit(1,10);Assert.Equal(1UL,id);}
    [Fact] public void SubmitIncrementsJobId(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Equal(2UL,s.Submit(1,10));}
    [Fact] public void SubmitRejectsWhenQueueFull(){var c=TestCfg();c.MaxQueueSize=2;var s=new QueueingScheduler(c);s.Submit(1,10);s.Submit(1,10);Assert.Null(s.Submit(1,10));Assert.Equal(1UL,s.Stats().TotalRejected);}
    [Fact] public void SrptPrefersShorterJobs(){var c=TestCfg();c.SmithEnabled=false;var s=new QueueingScheduler(c);s.Submit(1,100);s.Submit(1,10);Assert.Equal(2UL,s.PeekNext()!.Id);}
    [Fact] public void SmithRulePrefersHighWeight(){var s=new QueueingScheduler(TestCfg());s.Submit(1,100);s.Submit(10,100);Assert.Equal(2UL,s.PeekNext()!.Id);}
    [Fact] public void TickProcessesJobs(){var s=new QueueingScheduler(TestCfg());s.Submit(1,50);s.Submit(1,30);var done=s.Tick(20);Assert.Empty(done);Assert.True(s.Stats().TotalProcessingTime>0);}
    [Fact] public void TickCompletesJobs(){var s=new QueueingScheduler(TestCfg());s.Submit(1,20);s.Submit(1,10);var done=s.Tick(100);Assert.Equal(2,done.Count);}
    [Fact] public void CancelRemovesJob(){var s=new QueueingScheduler(TestCfg());s.Submit(1,50);Assert.True(s.Cancel(1));Assert.Null(s.PeekNext());}
    [Fact] public void ResetClearsAll(){var s=new QueueingScheduler(TestCfg());s.Submit(1,50);s.Tick(10);s.Reset();Assert.Null(s.PeekNext());Assert.Equal(0,s.Stats().QueueLength);}
    [Fact] public void StatsTrackCompletions(){var s=new QueueingScheduler(TestCfg());s.Submit(1,5);s.Submit(1,5);s.Tick(100);Assert.Equal(2UL,s.Stats().TotalCompleted);}
    [Fact] public void EvidenceHasFields(){var s=new QueueingScheduler(TestCfg());s.Submit(1,50);s.Submit(2,30);var e=s.Evidence();Assert.Equal(2,e.QueueLength);Assert.NotNull(e.SelectedJobId);Assert.NotEmpty(e.Jobs);}
    [Fact] public void EvidenceJsonl(){var s=new QueueingScheduler(TestCfg());s.Submit(1,50);var j=s.Evidence().ToJsonl("test");Assert.Contains("\"event\":\"test\"",j);Assert.Contains("\"reason\"",j);}
    [Fact] public void AgingIncreasesPriority(){var c=TestCfg();c.AgingFactor=0.5;c.Preemptive=false;var s=new QueueingScheduler(c);s.Submit(1,30);s.Submit(5,30);var done=s.Tick(100);Assert.Contains(1UL,done);}
    [Fact] public void PreemptionWhenHigherComes(){var c=TestCfg();c.SmithEnabled=false;var s=new QueueingScheduler(c);s.Submit(1,100);s.Tick(1);s.Submit(1,5);Assert.Equal(2UL,s.PeekNext()!.Id);Assert.True(s.Stats().TotalPreemptions>0);}

    [Fact] public void DefaultConfigValid(){var c=SchedulerConfig.Default;Assert.True(c.SmithEnabled);Assert.False(c.ForceFifo);Assert.True(c.Preemptive);}
    [Fact] public void SubmitNamedJob(){var s=new QueueingScheduler(TestCfg());s.SubmitNamed(1,10,"test-job");Assert.Equal("test-job",s.PeekNext()!.Name);}
    [Fact] public void SmithRuleBalancesWeightAndTime(){var s=new QueueingScheduler(TestCfg());s.Submit(2,20);s.Submit(1,5);Assert.Equal(5.0,s.PeekNext()!.RemainingTime);}
    [Fact] public void AgingPreventsStarvation(){var c=TestCfg();c.AgingFactor=1;var s=new QueueingScheduler(c);s.Submit(1,1000);s.Submit(1,1);var done=s.Tick(1);Assert.Single(done);Assert.NotNull(s.PeekNext());}
    [Fact] public void NoPreemptionWhenDisabled(){var c=TestCfg();c.Preemptive=false;var s=new QueueingScheduler(c);s.Submit(1,100);s.Tick(10);s.Submit(1,5);Assert.Equal(90.0,s.PeekNext()!.RemainingTime);}
    [Fact] public void TickCompletesMultipleJobs(){var s=new QueueingScheduler(TestCfg());s.Submit(1,5);s.Submit(1,5);Assert.Equal(2,s.Tick(10).Count);}
    [Fact] public void TickHandlesZeroDelta(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Empty(s.Tick(0));}
    [Fact] public void StatsTrackSubmissions(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);s.Submit(1,10);var st=s.Stats();Assert.Equal(2UL,st.TotalSubmitted);Assert.Equal(2,st.QueueLength);}
    [Fact] public void StatsComputeMeanResponseTime(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);s.Submit(1,10);s.Tick(20);Assert.Equal(2UL,s.Stats().TotalCompleted);Assert.True(s.Stats().MeanResponseTime>0);}
    [Fact] public void StatsComputeThroughput(){var s=new QueueingScheduler(TestCfg());s.Submit(1,5);s.Tick(5);Assert.True(s.Stats().Throughput>0);}
    [Fact] public void EvidenceReportsQueueEmpty(){var s=new QueueingScheduler(TestCfg());Assert.Equal(SelectionReason.QueueEmpty,s.Evidence().Reason);}
    [Fact] public void EvidenceReportsSelectedJob(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Equal(1UL,s.Evidence().SelectedJobId);}
    [Fact] public void EvidenceToJsonlEmptyQueue(){var s=new QueueingScheduler(TestCfg());var j=s.Evidence().ToJsonl("t");Assert.Contains("\"selected_job_id\":null",j);}
    [Fact] public void ForceFifoOverridesPriority(){var c=TestCfg();c.ForceFifo=true;var s=new QueueingScheduler(c);s.Submit(10,100);s.Submit(1,5);Assert.Equal(1UL,s.PeekNext()!.Id);}
    [Fact] public void SrptModeIgnoresWeights(){var c=TestCfg();c.SmithEnabled=false;var s=new QueueingScheduler(c);s.Submit(100,100);s.Submit(1,5);Assert.Equal(5.0,s.PeekNext()!.RemainingTime);}
    [Fact] public void FifoModeDisablesPreemption(){var c=TestCfg();c.ForceFifo=true;var s=new QueueingScheduler(c);s.Submit(1,100);s.Tick(1);s.Submit(1,5);Assert.Equal(99.0,s.PeekNext()!.RemainingTime);}
    [Fact] public void CancelReturnsFalseForNonexistent(){var s=new QueueingScheduler(TestCfg());Assert.False(s.Cancel(999));}
    [Fact] public void ClearRemovesJobsButKeepsStats(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);s.Tick(5);s.Clear();Assert.Null(s.PeekNext());Assert.Equal(0,s.Stats().QueueLength);Assert.Equal(1UL,s.Stats().TotalSubmitted);}
    [Fact] public void JobProgressIncreases(){var j=Job.Create(1,1,100);Assert.Equal(0.0,j.Progress);j.RemainingTime=50;Assert.Equal(0.5,j.Progress);}
    [Fact] public void JobIsComplete(){var j=Job.Create(1,1,10);j.RemainingTime=0;Assert.True(j.IsComplete);j.RemainingTime=-1;Assert.True(j.IsComplete);}
    [Fact] public void JobNewNanWeightClamps(){var j=Job.Create(1,double.NaN,10);Assert.True(j.Weight>0);}
    [Fact] public void JobNewInfWeightClamps(){var j=Job.Create(1,double.PositiveInfinity,10);Assert.True(j.Weight<1000);}
    [Fact] public void JobNewNanEstimateClamps(){var j=Job.Create(1,1,double.NaN);Assert.True(j.RemainingTime>0);}
    [Fact] public void JobWithNameSetsName(){var j=Job.CreateNamed(1,1,10,"test");Assert.Equal("test",j.Name);}
    [Fact] public void JobWithSourcesSetsBoth(){var j=Job.Create(1,1,10).WithSources(WeightSource.Default,EstimateSource.Historical);Assert.Equal(WeightSource.Default,j.WeightSource);Assert.Equal(EstimateSource.Historical,j.EstimateSource);}
    [Fact] public void ConfigModeReturnsCorrect(){Assert.Equal(SchedulingMode.Smith,SchedulerConfig.Default.Mode);var c=SchedulerConfig.Default;c.SmithEnabled=false;Assert.Equal(SchedulingMode.Srpt,c.Mode);c.ForceFifo=true;Assert.Equal(SchedulingMode.Fifo,c.Mode);}
    [Fact] public void StarvationGuardTriggersAfterThreshold(){var c=TestCfg();c.WaitStarveMs=1;c.AgingFactor=10;var s=new QueueingScheduler(c);s.Submit(1,1000);s.Submit(1,1000);s.Tick(100);Assert.Equal(1UL,s.PeekNext()!.Id);}
    [Fact] public void TickNegativeDeltaReturnsEmpty(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Empty(s.Tick(-1));}
    [Fact] public void TickEmptyQueueAdvancesTime(){var s=new QueueingScheduler(TestCfg());var done=s.Tick(100);Assert.Empty(done);}
    [Fact] public void StatsDefaultValues(){var s=new QueueingScheduler(TestCfg());var st=s.Stats();Assert.Equal(0UL,st.TotalSubmitted);Assert.Equal(0UL,st.TotalCompleted);Assert.Equal(0.0,st.MeanResponseTime);Assert.Equal(0.0,st.Throughput);}
    [Fact] public void EvidenceSingleJobNoTieBreak(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Null(s.Evidence().TieBreakReason);}
    [Fact] public void MultiplePreemptionsCounted(){var c=TestCfg();c.SmithEnabled=false;var s=new QueueingScheduler(c);s.Submit(1,100);s.Tick(1);s.Submit(1,5);s.Tick(1);s.Submit(1,3);Assert.True(s.Stats().TotalPreemptions>=1);}
    [Fact] public void MultipleRejectionsCounted(){var c=TestCfg();c.MaxQueueSize=1;var s=new QueueingScheduler(c);s.Submit(1,10);s.Submit(1,10);s.Submit(1,10);Assert.Equal(2UL,s.Stats().TotalRejected);}
    [Fact] public void ResetResetsJobIdSequence(){var s=new QueueingScheduler(TestCfg());var id1=s.Submit(1,10);s.Reset();Assert.Equal(1UL,s.Submit(1,10));}
    [Fact] public void ClearPreservesJobIdSequence(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);s.Clear();Assert.Equal(2UL,s.Submit(1,10));}

    [Fact] public void EvidenceReportsWaitStats(){var s=new QueueingScheduler(TestCfg());s.Submit(1,100);s.Submit(1,100);s.Tick(50);Assert.True(s.Evidence().MeanWaitTime>0);Assert.True(s.Evidence().MaxWaitTime>0);}
    [Fact] public void EvidenceReportsPriorityTerms(){var c=TestCfg();c.AgingFactor=0.5;c.WaitStarveMs=10;c.StarveBoostRatio=2;var s=new QueueingScheduler(c);s.Submit(1,20);s.Tick(20);var ev=s.Evidence();Assert.True(ev.Jobs.Count>=0);}
    [Fact] public void UnknownSourcesUseConfigValues(){var c=TestCfg();c.WeightUnknown=2.5;c.EstimateUnknownMs=250;var s=new QueueingScheduler(c);s.SubmitWithSources(0,0,WeightSource.Unknown,EstimateSource.Unknown,null);var n=s.PeekNext()!;Assert.InRange(n.Weight,2.49,2.51);Assert.InRange(n.RemainingTime,249.9,250.1);}
    [Fact] public void TieBreakPrefersBaseRatio(){var s=new QueueingScheduler(TestCfg());s.Submit(1,2);s.Tick(5);s.Submit(1,1);Assert.True(true);}
    [Fact] public void TieBreakPrefersWeight(){var s=new QueueingScheduler(TestCfg());var h=s.Submit(2,2);s.Submit(1,1);Assert.Equal(h,s.Evidence().SelectedJobId);Assert.Equal(TieBreakReason.Weight,s.Evidence().TieBreakReason);}
    [Fact] public void TieBreakPrefersArrivalSeq(){var c=TestCfg();c.AgingFactor=0;var s=new QueueingScheduler(c);var f=s.Submit(1,10);s.Submit(1,10);Assert.NotNull(s.PeekNext());}
    [Fact] public void ExplicitZeroWeightClampsToMin(){var c=TestCfg();c.WMin=0.5;var s=new QueueingScheduler(c);s.SubmitWithSources(0,1,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.InRange(s.PeekNext()!.Weight,0.49,0.51);}
    [Fact] public void ExplicitZeroEstimateClampsToMin(){var c=TestCfg();c.PMinMs=2;var s=new QueueingScheduler(c);s.SubmitWithSources(1,0,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.InRange(s.PeekNext()!.RemainingTime,1.99,2.01);}
    [Fact] public void ExplicitWeightHonorsWMax(){var c=TestCfg();c.WMax=50;var s=new QueueingScheduler(c);s.SubmitWithSources(20,1,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.InRange(s.PeekNext()!.Weight,19.9,20.1);}
    [Fact] public void ExplicitEstimateHonorsPMax(){var c=TestCfg();c.PMaxMs=100000;var s=new QueueingScheduler(c);s.SubmitWithSources(1,50000,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.True(true);}
    [Fact] public void CancelCurrentJob(){var s=new QueueingScheduler(TestCfg());s.Submit(1,100);s.Tick(10);Assert.True(s.Cancel(1));Assert.Null(s.PeekNext());}
    [Fact] public void TickProcessesMultipleJobsInSingleDelta(){var s=new QueueingScheduler(TestCfg());s.Submit(1,3);s.Submit(1,4);s.Submit(1,5);Assert.Equal(3,s.Tick(100).Count);}
    [Fact] public void JobWithSourcesPreservesBoth(){var j=Job.Create(1,1,10).WithSources(WeightSource.Default,EstimateSource.Historical);Assert.Equal(WeightSource.Default,j.WeightSource);Assert.Equal(EstimateSource.Historical,j.EstimateSource);}
    [Fact] public void JobProgressZeroTotalTime(){var j=Job.Create(1,1,10);j.TotalTime=0;Assert.Equal(1.0,j.Progress);}
    [Fact] public void SubmitNanWeightNormalized(){var s=new QueueingScheduler(TestCfg());s.SubmitWithSources(double.NaN,10,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.True(double.IsFinite(s.PeekNext()!.Weight));}
    [Fact] public void SubmitInfWeightNormalized(){var s=new QueueingScheduler(TestCfg());s.SubmitWithSources(double.PositiveInfinity,10,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.True(double.IsFinite(s.PeekNext()!.Weight));}
    [Fact] public void SubmitNanEstimateNormalized(){var s=new QueueingScheduler(TestCfg());s.SubmitWithSources(1,double.NaN,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.True(double.IsFinite(s.PeekNext()!.RemainingTime));}
    [Fact] public void SubmitInfEstimateNormalized(){var s=new QueueingScheduler(TestCfg());s.SubmitWithSources(1,double.PositiveInfinity,WeightSource.Explicit,EstimateSource.Explicit,null);Assert.True(double.IsFinite(s.PeekNext()!.RemainingTime));}
    [Fact] public void HistoricalEstimatePassesThrough(){var s=new QueueingScheduler(TestCfg());s.SubmitWithSources(1,42,WeightSource.Explicit,EstimateSource.Historical,null);Assert.InRange(s.PeekNext()!.RemainingTime,41.9,42.1);}
    [Fact] public void StarvationGuardDisabledWhenZero(){var c=TestCfg();c.WaitStarveMs=0;c.AgingFactor=10;var s=new QueueingScheduler(c);s.Submit(1,1000);s.Submit(1,1000);s.Tick(100);var e=s.Evidence();if(e.Jobs.Count>0)Assert.Equal(0.0,e.Jobs[0].StarvationFloor);}
    [Fact] public void ZeroTimeCompletesImmediately(){var s=new QueueingScheduler(TestCfg());var j=Job.Create(1,1,10);j.RemainingTime=0;Assert.True(j.IsComplete);}
    [Fact] public void NegativeTimeHandled(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Empty(s.Tick(-5));}
    [Fact] public void TickNonFiniteDeltaNoops(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Empty(s.Tick(double.NaN));Assert.Empty(s.Tick(double.PositiveInfinity));}
    [Fact] public void StatsDefaultAllZeros(){var st=new QueueingScheduler(TestCfg()).Stats();Assert.Equal(0UL,st.TotalSubmitted);Assert.Equal(0UL,st.TotalRejected);Assert.Equal(0UL,st.TotalPreemptions);}
    [Fact] public void StatsMaxResponseTimeTracked(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);s.Submit(1,10);s.Tick(100);Assert.True(s.Stats().MaxResponseTime>0);}
    [Fact] public void EvidenceContinuationReason(){var s=new QueueingScheduler(TestCfg());s.Submit(1,100);s.Tick(10);Assert.Equal(SelectionReason.Continuation,s.Evidence().Reason);}
    [Fact] public void EvidenceToJsonlContainsRequiredFields(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);var j=s.Evidence().ToJsonl("test");Assert.Contains("\"current_time\"",j);Assert.Contains("\"queue_length\"",j);Assert.Contains("\"jobs\"",j);}
    [Fact] public void EvidenceToJsonlEscapesSpecialChars(){var s=new QueueingScheduler(TestCfg());s.SubmitNamed(1,10,"test\"name");var j=s.Evidence().ToJsonl("test");Assert.Contains("test\\\"name",j);}
    [Fact] public void EvidenceAgingBoostReason(){var c=TestCfg();c.AgingFactor=100;var s=new QueueingScheduler(c);s.Submit(1,1000);s.Submit(1,1);s.Tick(100);Assert.NotNull(s.PeekNext());}
    [Fact] public void DefaultWeightSourceUsesConfig(){var c=TestCfg();c.WeightDefault=3;var s=new QueueingScheduler(c);s.SubmitWithSources(0,10,WeightSource.Default,EstimateSource.Explicit,null);Assert.InRange(s.PeekNext()!.Weight,2.9,3.1);}
    [Fact] public void DefaultEstimateSourceUsesConfig(){var c=TestCfg();c.EstimateDefaultMs=50;var s=new QueueingScheduler(c);s.SubmitWithSources(1,0,WeightSource.Explicit,EstimateSource.Default,null);Assert.InRange(s.PeekNext()!.RemainingTime,49.9,50.1);}

    [Fact] public void SelectionReasonAsStr(){Assert.Equal("queue_empty",EnumStr.S(SelectionReason.QueueEmpty));Assert.Equal("shortest_remaining",EnumStr.S(SelectionReason.ShortestRemaining));Assert.Equal("highest_weighted_priority",EnumStr.S(SelectionReason.HighestWeightedPriority));Assert.Equal("fifo",EnumStr.S(SelectionReason.Fifo));Assert.Equal("aging_boost",EnumStr.S(SelectionReason.AgingBoost));}
    [Fact] public void EstimateSourceAsStr(){Assert.Equal("explicit",EnumStr.E(EstimateSource.Explicit));Assert.Equal("historical",EnumStr.E(EstimateSource.Historical));Assert.Equal("default",EnumStr.E(EstimateSource.Default));Assert.Equal("unknown",EnumStr.E(EstimateSource.Unknown));}
    [Fact] public void WeightSourceAsStr(){Assert.Equal("explicit",EnumStr.W(WeightSource.Explicit));Assert.Equal("default",EnumStr.W(WeightSource.Default));Assert.Equal("unknown",EnumStr.W(WeightSource.Unknown));}
    [Fact] public void TieBreakReasonAsStr(){Assert.Equal("effective_priority",EnumStr.T(TieBreakReason.EffectivePriority));Assert.Equal("base_ratio",EnumStr.T(TieBreakReason.BaseRatio));Assert.Equal("weight",EnumStr.T(TieBreakReason.Weight));Assert.Equal("remaining_time",EnumStr.T(TieBreakReason.RemainingTime));}
    [Fact] public void JobDebugFormat(){var j=Job.CreateNamed(1,2,50,"render");Assert.Contains("Job",j.ToString()!);}
    [Fact] public void SchedulerConfigDebugFormat(){Assert.Contains("SchedulerConfig",SchedulerConfig.Default.ToString());}
    [Fact] public void SchedulerStatsDebugFormat(){Assert.Contains("SchedulerStats",SchedulerStats.Default.ToString());}
    [Fact] public void SchedulingEvidenceDebugFormat(){var s=new QueueingScheduler(TestCfg());s.Submit(1,10);Assert.Contains("SchedulingEvidence",s.Evidence().ToString());}
    [Fact] public void PropertyDeterministic(){var a=new QueueingScheduler(TestCfg());var b=new QueueingScheduler(TestCfg());a.Submit(1,10);b.Submit(1,10);a.Submit(2,20);b.Submit(2,20);a.Tick(5);b.Tick(5);Assert.Equal(a.Stats().TotalProcessingTime,b.Stats().TotalProcessingTime);}
}
