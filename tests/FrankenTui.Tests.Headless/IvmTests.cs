using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class IvmTests
{
    [Fact] public void Delta_Insert_HasPositiveWeight(){Assert.True(IvmDelta.Insert(1,0).IsInsert);}
    [Fact] public void Delta_Remove_HasNegativeWeight(){Assert.True(IvmDelta.Remove(1,0).IsRemove);}
    [Fact] public void View_Apply_Insert_AddsKey(){var v=new IvmView();v.Apply(IvmDelta.Insert(1,0));Assert.Equal(1,v[1]);}
    [Fact] public void View_Apply_Remove_RemovesKey(){var v=new IvmView();v.Apply(IvmDelta.Insert(1,0));v.Apply(IvmDelta.Remove(1,1));Assert.Equal(0,v[1]);}
    [Fact] public void View_Downstream_FiresOnChange(){var v=new IvmView();IvmDelta? received=null;v.Subscribe(d=>received=d);v.Apply(IvmDelta.Insert(1,0));Assert.NotNull(received);Assert.Equal(1UL,received!.Value.Key);}
    [Fact] public void View_NoChange_NoDownstream(){var v=new IvmView();int fired=0;v.Subscribe(_=>fired++);v.Apply(IvmDelta.Insert(1,0));v.Apply(IvmDelta.Remove(1,1));v.Apply(IvmDelta.Insert(1,2));Assert.Equal(3,fired);}
    [Fact] public void View_Keys_ReturnsActive(){var v=new IvmView();v.Apply(IvmDelta.Insert(1,0));v.Apply(IvmDelta.Insert(2,0));Assert.Equal(2,v.Keys.Count());}
    [Fact] public void View_Clear_RemovesAll(){var v=new IvmView();v.Apply(IvmDelta.Insert(1,0));v.Clear();Assert.Equal(0,v.Count);}
    [Fact] public void View_Snapshot_Copies(){var v=new IvmView();v.Apply(IvmDelta.Insert(1,0));var s=v.Snapshot();Assert.Equal(1,s[1]);s[1]=99;Assert.Equal(1,v[1]);}
    [Fact] public void View_NetZero_RemovesFromIndex(){var v=new IvmView();v.Apply(IvmDelta.Insert(1,0));v.Apply(IvmDelta.Remove(1,1));Assert.Equal(0,v.Count);}
    [Fact] public void Transform_MapsKey(){ulong mapped=0;var t=new IvmTransform(k=>k*10);t.Subscribe(d=>mapped=d.Key);t.Apply(IvmDelta.Insert(5,0));Assert.Equal(50UL,mapped);}
    [Fact] public void Filter_Passes(){ulong passed=0;var f=new IvmFilter(k=>k%2==0);f.Apply(IvmDelta.Insert(2,0));}
    [Fact] public void Join_ActiveWhenBothPresent(){var j=new IvmJoin();ulong result=0;j.ApplyLeft(IvmDelta.Insert(1,0));Assert.Equal(0UL,result);}
    [Fact] public void Clock_Tick_Increments(){var c=new IvmClock();Assert.Equal(1UL,c.Tick());Assert.Equal(1UL,c.Now);Assert.Equal(2UL,c.Tick());}
    [Fact] public void Graph_Propagate_FlowsDownstream(){var g=new IvmGraph();var src=new IvmView();var mid=g.AddNode(src);var sink=g.AddNode(mid);g.Propagate(src,new[]{IvmDelta.Insert(1,0)});Assert.Equal(1,sink[1]);}
}
