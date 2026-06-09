// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/flake_detector.rs
// Anytime-valid flake detector using e-process statistics.

namespace FrankenTui.Runtime;

public sealed class FlakeConfig
{
    public double Alpha=0.05,Lambda=0.5,Sigma=1.0; public int VarianceWindow=50,MinObservations=3; public bool EnableLogging; public double? ThresholdOverride;
    public static FlakeConfig Default=>new();
    public static FlakeConfig Create(double alpha)=>new(){Alpha=Math.Clamp(alpha,1e-10,0.5)};
    public FlakeConfig WithLambda(double l){Lambda=Math.Clamp(l,0.01,2.0);return this;}
    public FlakeConfig WithSigma(double s){Sigma=Math.Max(s,1e-9);return this;}
    public FlakeConfig WithVarianceWindow(int w){VarianceWindow=w;return this;}
    public FlakeConfig WithMinObservations(int m){MinObservations=Math.Max(m,1);return this;}
    public FlakeConfig WithLogging(bool e){EnableLogging=e;return this;}
    public double Threshold=>ThresholdOverride??1.0/Alpha;
}

public sealed record FlakeDecision(bool IsFlaky,double EValue,double Threshold,int ObservationCount,double VarianceEstimate,bool WarmedUp)
{
    public bool ShouldFail=>IsFlaky&&WarmedUp;
}

public sealed record EvidenceLog(int ObservationIdx,double Residual,double EIncrement,double ECumulative,double Variance,bool Decision)
{
    public string ToJsonl()=>$"{{\"idx\":{ObservationIdx},\"residual\":{Residual:F6},\"e_inc\":{EIncrement:F6},\"e_cum\":{ECumulative:F6},\"var\":{Variance:F6},\"decision\":{Decision.ToString().ToLowerInvariant()}}}";
}

public sealed class FlakeDetector
{
    const double EMin=1e-100,EMax=1e100,SMin=1e-9;
    FlakeConfig _c; double _e=1; int _n; Queue<double> _vw=new(); List<EvidenceLog> _log=new(); int? _firstFlaky; double _maxE=1;

    public FlakeDetector(FlakeConfig c){_c=c;}

    public FlakeDecision Observe(double residual)
    {
        if(double.IsNaN(residual))return new(false,_e,_c.Threshold,_n,Math.Pow(CurrentSigma,2),_n>=_c.MinObservations);
        _n++;UpdateVariance(residual);double s=CurrentSigma,l=_c.Lambda;
        double exp=l*residual-(l*l*s*s)/2.0,ei=Math.Clamp(Math.Exp(exp),EMin,EMax);
        _e=Math.Clamp(_e*ei,EMin,EMax);_maxE=Math.Max(_maxE,_e);
        bool isFlaky=_e>_c.Threshold,warmed=_n>=_c.MinObservations,dec=isFlaky&&warmed;
        if(dec&&_firstFlaky==null)_firstFlaky=_n;
        if(_c.EnableLogging)_log.Add(new EvidenceLog(_n,residual,ei,_e,s*s,dec));
        return new(isFlaky,_e,_c.Threshold,_n,s*s,warmed);
    }

    public FlakeDecision ObserveBatch(double[] residuals)
    {
        FlakeDecision d=new(false,_e,_c.Threshold,_n,Math.Pow(CurrentSigma,2),false);
        foreach(var r in residuals){d=Observe(r);if(d.ShouldFail)break;}return d;
    }

    public void Reset(){_e=1;_n=0;_vw.Clear();_log.Clear();_firstFlaky=null;_maxE=1;}
    public double EValue=>_e; public int ObservationCount=>_n; public bool IsWarmedUp=>_n>=_c.MinObservations;
    public IReadOnlyList<EvidenceLog> EvidenceLog=>_log; public string EvidenceToJsonl()=>string.Join("\n",_log.Select(l=>l.ToJsonl()));
    public FlakeConfig Config=>_c;

    public double CurrentSigma{
        get{
            if(_c.VarianceWindow==0||_vw.Count<2)return Math.Max(_c.Sigma,SMin);
            double mean=_vw.Average(),sumSq=0;foreach(var x in _vw){double d=x-mean;sumSq+=d*d;}
            return Math.Max(Math.Sqrt(sumSq/(_vw.Count-1)),SMin);
        }
    }

    void UpdateVariance(double r){if(_c.VarianceWindow==0)return;if(_vw.Count>=_c.VarianceWindow)_vw.Dequeue();_vw.Enqueue(r);}

    public FlakeSummary Summary=>new(_n,_e,_e>_c.Threshold,_firstFlaky,_maxE,_c.Threshold);
}
public sealed record FlakeSummary(int TotalObservations,double FinalEValue,bool IsFlaky,int? FirstFlakyAt,double MaxEValue,double Threshold);
