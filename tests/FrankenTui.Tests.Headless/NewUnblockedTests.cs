// Tests for schedule_trace, timeline_aggregator, process_subscription
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class NewUnblockedTests
{
    // ScheduleTrace
    [Fact] public void ScheduleTraceNew(){var t=new ScheduleTrace();Assert.Equal(0UL,t.Tick);Assert.Equal(0UL,t.EntryCount);}
    [Fact] public void ScheduleTraceRecord(){var t=new ScheduleTrace();t.Record(new TaskEvent.Start(1));t.Record(new TaskEvent.Complete(1));Assert.Equal(2UL,t.EntryCount);}
    [Fact] public void ScheduleTraceChecksumDeterministic(){var a=new ScheduleTrace();var b=new ScheduleTrace();a.Record(new TaskEvent.Start(1));b.Record(new TaskEvent.Start(1));Assert.Equal(a.Checksum(),b.Checksum());}
    [Fact] public void ScheduleTraceChecksumDifferentForDifferentEvents(){var a=new ScheduleTrace();a.Record(new TaskEvent.Start(1));var c1=a.Checksum();a=new ScheduleTrace();a.Record(new TaskEvent.Complete(1));Assert.NotEqual(c1,a.Checksum());}
    [Fact] public void ScheduleTraceToJsonl(){var t=new ScheduleTrace();t.Record(new TaskEvent.Spawn(1,5,"test"));var j=t.ToJsonl();Assert.Contains("\"event\":\"spawn\"",j);Assert.Contains("\"task_id\":1",j);}
    [Fact] public void TraceEntryJsonlAllEventTypes(){var e=new TraceEntry(1,0,new TaskEvent.Custom("tag","data"));Assert.Contains("custom",e.ToJsonl());}
    [Fact] public void ScheduleTraceMaxEntries(){var c=new TraceConfig{MaxEntries=3};var t=new ScheduleTrace(c);for(int i=0;i<5;i++)t.Record(new TaskEvent.Start((ulong)i));Assert.True(t.EntryCount<=3);}
    [Fact] public void ScheduleTraceClear(){var t=new ScheduleTrace();t.Record(new TaskEvent.Start(1));t.Clear();Assert.Equal(0UL,t.EntryCount);}

    // TimelineAggregator
    [Fact] public void TimelineAggregatorObserve(){var t=new TimelineAggregator(AggregatorConfig.Default);var e=t.Observe("test",0.5);Assert.NotNull(e);Assert.True(e.ObservationCount>0);}
    [Fact] public void TimelineAggregatorStats(){var t=new TimelineAggregator(AggregatorConfig.Default);t.Observe("a",0.1);t.Observe("b",0.2);var s=t.Stats();Assert.True(s.TotalEvents>=2);}

    // ProcessSubscription
    [Fact] public void ProcessSubBuilder(){var p=new ProcessSubscription<string>("echo",e=>e switch{ProcessEvent.Stdout s=>s.Line,_=>""});p.Arg("hello").Timeout(TimeSpan.FromSeconds(5));Assert.True(p.Id.Value>0);}
    [Fact] public void ProcessSubRuns(){var p=new ProcessSubscription<string>("echo",e=>e switch{ProcessEvent.Stdout s=>s.Line,_=>""});p.Arg("test");var ch=System.Threading.Channels.Channel.CreateUnbounded<string>();var(signal,trigger)=StopSignal.Create();var t=new System.Threading.Thread(()=>p.Run(ch.Writer,signal));t.Start();System.Threading.Thread.Sleep(500);trigger.Stop();t.Join(2000);var msgs=new List<string>();while(ch.Reader.TryRead(out var m))msgs.Add(m);Assert.Contains("test",msgs);}
}
