// Tests for flake_detector, flat_combine, alpha_investing, lens, conformal_stages
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class SmallModuleTests
{
    // FlakeDetector
    [Fact] public void FlakeDefaultConfig(){var c=FlakeConfig.Default;Assert.InRange(c.Alpha,0.04,0.06);Assert.Equal(50,c.VarianceWindow);}
    [Fact] public void FlakeEProcessNonNegative(){var d=new FlakeDetector(FlakeConfig.Default);foreach(var r in new[]{-5.0,-2.0,0.0,2.0,5.0})Assert.True(d.Observe(r).EValue>0);}
    [Fact] public void FlakeStableRunNoFalsePositives(){var d=new FlakeDetector(FlakeConfig.Create(0.05).WithMinObservations(3));for(int i=0;i<50;i++)Assert.False(d.Observe(0).ShouldFail);}
    [Fact] public void FlakeReset(){var d=new FlakeDetector(FlakeConfig.Default);d.Observe(1);d.Observe(2);d.Reset();Assert.Equal(0,d.ObservationCount);Assert.InRange(d.EValue,0.99,1.01);}
    [Fact] public void FlakeEvidenceLog(){var d=new FlakeDetector(FlakeConfig.Default.WithLogging(true).WithMinObservations(1));d.Observe(0.5);d.Observe(1.0);Assert.Equal(2,d.EvidenceLog.Count);}
    [Fact] public void FlakeBatchObserve(){var d=new FlakeDetector(FlakeConfig.Default.WithMinObservations(1));Assert.Equal(5,d.ObserveBatch(new[]{0.1,0.2,0.3,0.4,0.5}).ObservationCount);}
    [Fact] public void FlakeConfigBuilder(){var c=FlakeConfig.Create(0.01).WithLambda(0.3).WithSigma(2).WithMinObservations(5).WithLogging(true);Assert.InRange(c.Alpha,0.009,0.011);Assert.InRange(c.Lambda,0.29,0.31);Assert.InRange(c.Threshold,99,101);}

    // FlatCombiner
    [Fact] public void FlatNewCreatesEmpty(){var f=new FlatCombiner<List<int>>(new());Assert.Equal(0,f.PendingCount);Assert.Equal(0UL,f.Generation);}
    [Fact] public void FlatExecuteAppliesDirectly(){var f=new FlatCombiner<List<int>>(new List<int>{10});Assert.Equal(15,f.Execute(s=>{s[0]+=5;return s[0];}));}
    [Fact] public void FlatSubmitAndCombine(){var f=new FlatCombiner<List<int>>(new List<int>{0});f.Submit(s=>s[0]+=10);f.Submit(s=>s[0]+=20);Assert.Equal(2,f.Combine());Assert.Equal(30,f.WithState(s=>s[0]));}
    [Fact] public void FlatCombineEmptyReturnsZero(){Assert.Equal(0,new FlatCombiner<List<int>>(new()).Combine());}
    [Fact] public void FlatStatsTrackBatches(){var f=new FlatCombiner<List<int>>(new List<int>{0});f.Submit(s=>s[0]+=1);f.Submit(s=>s[0]+=1);f.Combine();Assert.Equal(1UL,f.Stats.CombinePasses);Assert.Equal(2UL,f.Stats.TotalOps);}

    // AlphaInvesting
    [Fact] public void AlphaDefaultConfig(){var a=AlphaInvestor.WithDefaults();Assert.InRange(a.Wealth,0.49,0.51);Assert.Equal(0,a.TestsRun);}
    [Fact] public void AlphaTestRejectsLowPValue(){var a=AlphaInvestor.WithDefaults();Assert.Equal(TestOutcome.Rejected,a.Test(0.001));}
    [Fact] public void AlphaTestNotRejectsHighPValue(){var a=AlphaInvestor.WithDefaults();Assert.Equal(TestOutcome.NotRejected,a.Test(0.5));}
    [Fact] public void AlphaWealthDecreases(){var a=AlphaInvestor.WithDefaults();a.Test(0.5);a.Test(0.5);Assert.True(a.Wealth<0.5);}
    [Fact] public void AlphaReset(){var a=AlphaInvestor.WithDefaults();a.Test(0.1);a.Reset();Assert.Equal(0,a.TestsRun);Assert.InRange(a.Wealth,0.49,0.51);}

    // Lens
    [Fact] public void LensGetSet(){var l=new Lens<List<int>,int>(s=>s[0],(s,v)=>{s[0]=v;return s;});var s=new List<int>{42};Assert.Equal(42,l.Get(s));Assert.Equal(99,l.Set(s,99)[0]);}
    [Fact] public void LensAtKey(){var d=new Dictionary<string,int>{{"x",5}};var l=LensList.AtKey<string,int>("x");Assert.Equal(5,l.Get(d));Assert.Equal(10,l.Set(d,10)["x"]);}

    // ConformalStages
    [Fact] public void ConformalStageObservation(){var o=new StageObservation{LayoutUs=1, DiffUs=2, PresentUs=3};Assert.Equal(6,o.TotalUs);Assert.Equal(2,o.Get(RenderStage.Diff));}
    [Fact] public void ConformalPredictorCalibrate(){var p=new StagedConformalPredictor();for(int i=0;i<20;i++)p.Calibrate(RenderStage.Layout,100);Assert.True(p.Observe(RenderStage.Layout,100).CalibrationCount>0);}
    [Fact] public void ConformalFrameResult(){var p=new StagedConformalPredictor();var r=p.ObserveFrame(new StageObservation{LayoutUs=100,DiffUs=50,PresentUs=200});Assert.Equal(3,r.Stages.Length);}
}
