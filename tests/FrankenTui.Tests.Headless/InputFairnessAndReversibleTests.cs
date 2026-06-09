// Tests for input_fairness.rs and reversible.rs
using System.Diagnostics;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class InputFairnessTests
{
    [Fact] public void DefaultConfigEnabled(){Assert.True(FairnessConfig.Default.Enabled);}
    [Fact] public void DisabledConfig(){Assert.False(FairnessConfig.Disabled().Enabled);}
    [Fact] public void DefaultDecisionAllowsProcessing(){var g=new InputFairnessGuard();Assert.True(g.CheckFairness(Stopwatch.GetTimestamp()).ShouldProcess);}
    [Fact] public void EventProcessingUpdatesStats(){var g=new InputFairnessGuard();long t=Stopwatch.GetTimestamp();g.EventProcessed(EventType.Input,TimeSpan.FromMilliseconds(10),t);g.EventProcessed(EventType.Resize,TimeSpan.FromMilliseconds(5),t);Assert.Equal(2UL,g.Stats.EventsProcessed);Assert.Equal(1UL,g.Stats.InputEvents);}
    [Fact] public void JainIndexPerfectFairness(){var g=new InputFairnessGuard();long t=Stopwatch.GetTimestamp();g.EventProcessed(EventType.Input,TimeSpan.FromMilliseconds(10),t);g.EventProcessed(EventType.Resize,TimeSpan.FromMilliseconds(10),t);Assert.InRange(g.JainIdx,0.99,1.01);}
    [Fact] public void JainIndexUnfair(){var g=new InputFairnessGuard();long t=Stopwatch.GetTimestamp();g.EventProcessed(EventType.Input,TimeSpan.FromMilliseconds(1),t);g.EventProcessed(EventType.Resize,TimeSpan.FromMilliseconds(100),t);Assert.True(g.JainIdx<0.6);}
    [Fact] public void ResetClearsState(){var g=new InputFairnessGuard();long t=Stopwatch.GetTimestamp();g.InputArrived(t);g.EventProcessed(EventType.Resize,TimeSpan.FromMilliseconds(10),t);g.CheckFairness(t);g.Reset();Assert.False(g.HasPendingInput);Assert.Equal(0U,g.ResizeDominanceCount);}
}

public class ReversibleTests
{
    [Fact] public void AddForwardBackward(){ulong x=10;var op=new AddOp(5);op.Forward(ref x);Assert.Equal(15UL,x);op.Backward(ref x);Assert.Equal(10UL,x);}
    [Fact] public void XorIsSelfInverse(){ulong x=0xFF;var op=new XorOp(0x0F);op.Forward(ref x);Assert.Equal(0xF0UL,x);op.Backward(ref x);Assert.Equal(0xFFUL,x);}
    [Fact] public void MulF64Roundtrip(){double x=4.0;var op=new MulOp(2.5);op.Forward(ref x);Assert.InRange(x,9.9,10.1);op.Backward(ref x);Assert.InRange(x,3.9,4.1);}
    [Fact] public void SetRoundtrip(){string x="hello";var op=new SetOp<string>("hello","world");op.Forward(ref x);Assert.Equal("world",x);op.Backward(ref x);Assert.Equal("hello",x);}
    [Fact] public void PushRoundtrip(){var v=new List<uint>{1,2};var op=new PushOp<uint>(3);op.Forward(ref v);Assert.Equal(new List<uint>{1,2,3},v);op.Backward(ref v);Assert.Equal(new List<uint>{1,2},v);}
    [Fact] public void InsertRoundtrip(){var v=new List<int>{1,3,4};var op=new InsertOp<int>(1,2);op.Forward(ref v);Assert.Equal(new List<int>{1,2,3,4},v);op.Backward(ref v);Assert.Equal(new List<int>{1,3,4},v);}
    [Fact] public void RemoveCaptureRoundtrip(){var v=new List<int>{10,20,30};var op=RemoveOp<int>.Capturing(v,1);op.Forward(ref v);Assert.Equal(new List<int>{10,30},v);op.Backward(ref v);Assert.Equal(new List<int>{10,20,30},v);}
    [Fact] public void SequenceForwardBackward(){ulong x=0;var seq=new Sequence<ulong>(new List<IReversible<ulong>>{new AddOp(10),new AddOp(20),new AddOp(30)});seq.Forward(ref x);Assert.Equal(60UL,x);seq.Backward(ref x);Assert.Equal(0UL,x);}
    [Fact] public void SequenceEmpty(){ulong x=42;var seq=Sequence<ulong>.Empty;seq.Forward(ref x);Assert.Equal(42UL,x);seq.Backward(ref x);Assert.Equal(42UL,x);}
    [Fact] public void JournalApplyUndoRedo(){ulong s=0;var j=new Journal<ulong>();j.Apply(new AddOp(10),ref s);j.Apply(new AddOp(20),ref s);Assert.Equal(30UL,s);Assert.True(j.Undo(ref s));Assert.Equal(10UL,s);Assert.True(j.Redo(ref s));Assert.Equal(30UL,s);}
    [Fact] public void JournalClear(){ulong s=0;var j=new Journal<ulong>();j.Apply(new AddOp(10),ref s);j.Clear();Assert.Equal(0,j.UndoCount);}
}
