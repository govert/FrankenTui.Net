// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/conformal_alert.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Conformal alert threshold calibration with anytime-valid e-process control.

namespace FrankenTui.Runtime;

static class ConformalConstants { public const double EMin=1e-12, EMax=1e12, Epsilon=1e-10, FallbackThreshold=double.MaxValue; }

public sealed class AlertConfig
{
    public double Alpha=0.05; public int MinCalibration=10; public int MaxCalibration=500;
    public double Lambda=0.5; public double Mu0; public double Sigma0=1.0;
    public bool AdaptiveLambda=true; public double GrapaEta=0.1; public bool EnableLogging;
    public double Hysteresis=1.1; public ulong AlertCooldown=5;
    public static AlertConfig Default=>new();
}

struct CalibrationStats
{
    public ulong N; public double Mean, M2;
    public void Update(double v){if(double.IsNaN(v))return;N++; double d=v-Mean;Mean+=d/N; M2+=d*(v-Mean);}
    public double Variance=>N<2?1.0:Math.Max(M2/(N-1),ConformalConstants.Epsilon);
    public double Std=>Math.Sqrt(Variance);
}

public enum AlertReason{Normal,ConformalExceeded,EProcessExceeded,BothExceeded,InCooldown,InsufficientCalibration,InvalidObservation}

public sealed class AlertEvidence
{
    public ulong ObservationIdx; public double Value, Residual, ZScore, ConformalThreshold, ConformalScore, EValue, EThreshold, Lambda;
    public bool ConformalAlert, EProcessAlert, IsAlert; public AlertReason Reason;
    public string Summary()=>$"obs={ObservationIdx} val={Value:F2} res={Residual:F2} z={ZScore:F2} q={ConformalThreshold:F2} conf_p={ConformalScore:F3} E={EValue:F2}/{EThreshold:F2} alert={IsAlert}";
}

public sealed class AlertDecision{public bool IsAlert;public AlertEvidence Evidence=new();public ulong ObservationsSinceAlert;public string EvidenceSummary()=>Evidence.Summary();}

public sealed class AlertStats
{
    public ulong TotalObservations,TotalAlerts,ConformalAlerts,EProcessAlerts,BothAlerts; public int CalibrationSamples;
    public double CurrentEValue,CurrentThreshold,CurrentLambda,CalibrationMean,CalibrationStd,EmpiricalFpr;
}

public sealed class ConformalAlert
{
    AlertConfig _c; List<double> _cal=new(); CalibrationStats _s; double _e=1,_et,_l; ulong _n,_since,_alerts,_ca,_ea,_ba; bool _cool; List<AlertEvidence> _logs=new();

    public ConformalAlert(AlertConfig c)
    {
        _c=c; _et=(1.0/c.Alpha)*c.Hysteresis; _l=Math.Clamp(c.Lambda,ConformalConstants.Epsilon,1.0-ConformalConstants.Epsilon);
    }

    public void Calibrate(double v){_s.Update(v); _cal.Add(Math.Abs(v-_s.Mean)); while(_cal.Count>_c.MaxCalibration)_cal.RemoveAt(0);}
    public AlertDecision Observe(double v)
    {
        _n++;_since++;
        if(double.IsNaN(v))return NoAlert(v,AlertReason.InvalidObservation);
        if(_cool&&_since<=_c.AlertCooldown)return NoAlert(v,AlertReason.InCooldown);
        _cool=false;
        if(_cal.Count<_c.MinCalibration)return NoAlert(v,AlertReason.InsufficientCalibration);
        double res=v-_s.Mean, ares=Math.Abs(res), z=res/Math.Max(_s.Std,ConformalConstants.Epsilon);
        double thresh=ConformalThreshold(), cscore=ConformalScore(ares); bool ca=ares>thresh;
        double zc=z-_c.Mu0, exp=Math.Clamp(_l*zc-_l*_l*_c.Sigma0*_c.Sigma0/2.0,-700,700);
        _e=Math.Clamp(_e*Math.Exp(exp),ConformalConstants.EMin,ConformalConstants.EMax);
        bool ea=_e>_et;
        if(_c.AdaptiveLambda){double d=1+_l*zc; if(Math.Abs(d)>ConformalConstants.Epsilon) _l=Math.Clamp(_l+_c.GrapaEta*zc/d,ConformalConstants.Epsilon,1.0-ConformalConstants.Epsilon);}
        bool alert=ca||ea;
        AlertReason r=(ca&&ea)?AlertReason.BothExceeded:(ca?AlertReason.ConformalExceeded:(ea?AlertReason.EProcessExceeded:AlertReason.Normal));
        var ev=new AlertEvidence{ObservationIdx=_n,Value=v,Residual=res,ZScore=z,ConformalThreshold=thresh,ConformalScore=cscore,EValue=_e,EThreshold=_et,Lambda=_l,ConformalAlert=ca,EProcessAlert=ea,IsAlert=alert,Reason=r};
        if(_c.EnableLogging)_logs.Add(ev);
        if(alert){_alerts++;_since=0;_cool=true;_e=1.0; switch(r){case AlertReason.ConformalExceeded:_ca++;break; case AlertReason.EProcessExceeded:_ea++;break; case AlertReason.BothExceeded:_ba++;break;}}
        return new AlertDecision{IsAlert=alert,Evidence=ev,ObservationsSinceAlert=_since};
    }

    double ConformalThreshold()
    {
        if(_cal.Count==0)return ConformalConstants.FallbackThreshold;
        int n=_cal.Count; double t=(1-_c.Alpha)*(n+1); int idx=Math.Min((int)Math.Ceiling(t)-1,n-1); if(idx<0)idx=0;
        var s= new List<double>(_cal); s.Sort(); return s[idx];
    }

    double ConformalScore(double ares)
    {
        if(_cal.Count==0)return 1.0; int n=_cal.Count, geq=0;
        foreach(var r in _cal)if(r>=ares)geq++; return (geq+1.0)/(n+1);
    }

    AlertDecision NoAlert(double v,AlertReason r)
    {
        var ev=new AlertEvidence{ObservationIdx=_n,Value=v,ConformalThreshold=ConformalConstants.FallbackThreshold,ConformalScore=1.0,EValue=_e,EThreshold=_et,Lambda=_l,Reason=r};
        return new AlertDecision{Evidence=ev,ObservationsSinceAlert=_since};
    }

    public void ResetEProcess(){_e=1.0;_since=0;_cool=false;}
    public void ClearCalibration(){_cal.Clear();_s=default;ResetEProcess();}
    public AlertStats Stats()
    {
        double fpr=_n>0?(double)_alerts/_n:0;
        return new AlertStats{TotalObservations=_n,CalibrationSamples=_cal.Count,TotalAlerts=_alerts,ConformalAlerts=_ca,EProcessAlerts=_ea,BothAlerts=_ba,CurrentEValue=_e,CurrentThreshold=ConformalThreshold(),CurrentLambda=_l,CalibrationMean=_s.Mean,CalibrationStd=_s.Std,EmpiricalFpr=fpr};
    }
    public IReadOnlyList<AlertEvidence> Logs=>_logs;
    public void ClearLogs()=>_logs.Clear();
    public double EValue=>_e;
    public double Threshold=>ConformalThreshold();
    public double Mean=>_s.Mean;
    public double Std=>_s.Std;
    public int CalibrationCount=>_cal.Count;
    public double Alpha=>_c.Alpha;
}
