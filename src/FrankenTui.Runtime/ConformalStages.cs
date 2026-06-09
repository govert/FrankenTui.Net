// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/conformal_stages.rs
// Multi-stage conformal prediction for render pipeline timing.

namespace FrankenTui.Runtime;

public enum RenderStage{Layout,Diff,Present}
public sealed class StageObservation{public double LayoutUs,DiffUs,PresentUs;public double Get(RenderStage s)=>s switch{RenderStage.Layout=>LayoutUs,RenderStage.Diff=>DiffUs,RenderStage.Present=>PresentUs,_=>0};public double TotalUs=>LayoutUs+DiffUs+PresentUs;}

public sealed class StageAlert{public RenderStage Stage;public bool IsAlert;public double Observed,Threshold,EValue;public int CalibrationCount;}

public sealed class FrameResult
{
    public StageAlert[] Stages=new StageAlert[3];
    public bool AnyAlert=>Stages.Any(s=>s.IsAlert);
    public List<RenderStage> AlertingStages=>Stages.Where(s=>s.IsAlert).Select(s=>s.Stage).ToList();
    public StageAlert Stage(RenderStage s)=>Stages[(int)s];
}

public sealed class StagedConfig{public double Alpha=0.05,Lambda=0.5;public int MaxCalibration=500,MinCalibration=10;public static StagedConfig Default=>new();}

public sealed class StagedConformalPredictor
{
    const double EMin=1e-12,EMax=1e12;
    StagedConfig _c; (Queue<double> cal,double mean,double m2,ulong n,double e)[] _s=new(Queue<double> cal,double mean,double m2,ulong n,double e)[3]{ (new(),0,0,0,1),(new(),0,0,0,1),(new(),0,0,0,1) };

    public StagedConformalPredictor(StagedConfig? c=null){_c=c??StagedConfig.Default;}
    public void Calibrate(RenderStage stage,double value){int i=(int)stage;ref var s=ref _s[i];s.n++;double d=value-s.mean;s.mean+=d/s.n;s.m2+=d*(value-s.mean);double res=Math.Abs(value-s.mean);s.cal.Enqueue(res);while(s.cal.Count>_c.MaxCalibration)s.cal.Dequeue();}
    public StageAlert Observe(RenderStage stage,double value){
        int i=(int)stage;ref var s=ref _s[i];
        double res=value-s.mean,z=res/Math.Max(Math.Sqrt(s.m2/Math.Max(1,s.n-1)),1e-9);
        double exp=Math.Clamp(_c.Lambda*z-_c.Lambda*_c.Lambda/2,-700,700);
        s.e=Math.Clamp(s.e*Math.Exp(exp),EMin,EMax);
        var sorted=s.cal.OrderBy(x=>x).ToList();double thresh=sorted.Count>=_c.MinCalibration?sorted[Math.Min((int)Math.Ceiling((1-_c.Alpha)*(sorted.Count+1))-1,sorted.Count-1)]:double.MaxValue;
        return new StageAlert{Stage=stage,IsAlert=Math.Abs(res)>thresh,Observed=value,Threshold=thresh,EValue=s.e,CalibrationCount=s.cal.Count};
    }
    public FrameResult ObserveFrame(StageObservation obs)=>new FrameResult{Stages=new[]{Observe(RenderStage.Layout,obs.LayoutUs),Observe(RenderStage.Diff,obs.DiffUs),Observe(RenderStage.Present,obs.PresentUs)}};
}
