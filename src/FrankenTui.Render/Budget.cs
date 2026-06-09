// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/budget.rs
// Render budget enforcement with graceful degradation and PID/e-process control.

namespace FrankenTui.Render;

public enum DegradationLevel{Full,SimpleBorders,NoStyling,EssentialOnly,Skeleton,SkipFrame}

public static class DegradationLevelExtensions
{
    // Returns false at SimpleBorders and above (use ASCII instead). Upstream: self < Self::SimpleBorders
    public static bool UseUnicodeBorders(this DegradationLevel l)=>l<DegradationLevel.SimpleBorders;
    // Returns false at NoStyling and above. Upstream: self < Self::NoStyling
    public static bool ApplyStyling(this DegradationLevel l)=>l<DegradationLevel.NoStyling;
    // Returns false at EssentialOnly and above. Upstream: self < Self::EssentialOnly
    public static bool RenderDecorative(this DegradationLevel l)=>l<DegradationLevel.EssentialOnly;
    // Returns false at Skeleton and above. Upstream: self < Self::Skeleton
    public static bool RenderContent(this DegradationLevel l)=>l<DegradationLevel.Skeleton;
    public static string Label(this DegradationLevel l)=>l switch{
        DegradationLevel.SimpleBorders=>"SIMPLE_BORDERS",
        DegradationLevel.NoStyling=>"NO_STYLING",
        DegradationLevel.EssentialOnly=>"ESSENTIAL_ONLY",
        DegradationLevel.Skeleton=>"SKELETON",
        DegradationLevel.SkipFrame=>"SKIP_FRAME",
        _=>"FULL"};
}

// ── PidGains / PidState ───────────────────────────────────────────────────

public sealed class PidGains
{
    public double Kp=0.5,Ki=0.05,Kd=0.2,IntegralMax=5.0;
    public static PidGains Default=>new();
}

sealed class PidState
{
    double _integral,_prevError,_lastP,_lastI,_lastD;
    public void Reset(){_integral=_prevError=_lastP=_lastI=_lastD=0;}
    public double Update(double error,PidGains g)
    {
        if(double.IsNaN(error))return 0;
        _integral=Math.Clamp(_integral+error,-g.IntegralMax,g.IntegralMax);
        double deriv=error-_prevError;_prevError=error;
        _lastP=g.Kp*error;_lastI=g.Ki*_integral;_lastD=g.Kd*deriv;
        return _lastP+_lastI+_lastD;
    }
    public double LastP=>_lastP; public double LastI=>_lastI; public double LastD=>_lastD;
    public double Integral=>_integral;
}

// ── EProcessConfig / EProcessState ────────────────────────────────────────

public sealed class EProcessConfig
{
    public double Lambda=0.5,Alpha=0.05,Beta=0.5,SigmaEmaDecay=0.9,SigmaFloorMs=1.0; public uint WarmupFrames=10;
    public static EProcessConfig Default=>new();
}

sealed class EProcessState
{
    double _e=1,_sigma=1; uint _n; double _emaSigma=0;
    public double EValue=>_e; public double SigmaMs=>_sigma; public uint FramesObserved=>_n;

    public void Update(double frameTimeMs,double targetMs,EProcessConfig c)
    {
        _n++;
        double residual=frameTimeMs-targetMs;
        _sigma=Math.Max(_sigma,1e-9);
        double z=residual/_sigma;
        double inc=Math.Exp(c.Lambda*z-c.Lambda*c.Lambda/2);
        _e=Math.Clamp(_e*Math.Max(inc,1e-12),1e-12,1e12);
        if(_n>c.WarmupFrames){
            _emaSigma=c.SigmaEmaDecay*_emaSigma+(1-c.SigmaEmaDecay)*Math.Abs(residual);
            _sigma=Math.Max(_emaSigma,c.SigmaFloorMs);
        }
    }
    public void Reset(){_e=1;_sigma=1;_n=0;_emaSigma=0;}
}

// ── BudgetController ──────────────────────────────────────────────────────

public sealed class BudgetControllerConfig
{
    public PidGains Pid=PidGains.Default; public EProcessConfig EProcess=EProcessConfig.Default;
    public double TargetFrameMs=16.667; public DegradationLevel MaxDegradation=DegradationLevel.SkipFrame;
    public int RecoveryThreshold=10; public bool Enabled=true;
    public static BudgetControllerConfig Default=>new();
}

public enum BudgetDecision{Hold,Degrade,Upgrade}
internal static class BudgetMeta{public static string AsStr(BudgetDecision d)=>d switch{BudgetDecision.Hold=>"hold",BudgetDecision.Degrade=>"degrade",BudgetDecision.Upgrade=>"upgrade",_=>""};}

public enum BudgetDecisionReason{WithinBudget,EProcessThreshold,ConsecutiveWithinBudget,PidRecommendation,DefaultHold}

public sealed class BudgetController
{
    readonly BudgetControllerConfig _c; readonly PidState _pid=new(); readonly EProcessState _ep=new();
    DegradationLevel _level=DegradationLevel.Full; int _withinBudgetStreak; BudgetDecision _lastDecision=BudgetDecision.Hold;

    public BudgetController(BudgetControllerConfig? c=null){_c=c??BudgetControllerConfig.Default;}

    public BudgetDecision Update(TimeSpan frameTime)
    {
        double frameMs=frameTime.TotalMilliseconds,targetMs=_c.TargetFrameMs;
        _ep.Update(frameMs,targetMs,_c.EProcess);
        double error=frameMs-targetMs;
        double pidOut=_pid.Update(error,_c.Pid);
        bool eprocessAlert=_ep.EValue>1.0/_c.EProcess.Alpha;

        var decision=BudgetDecision.Hold;
        if(eprocessAlert&&_level!=_c.MaxDegradation){decision=BudgetDecision.Degrade;_level=NextDegradation(_level);_withinBudgetStreak=0;}
        else if(frameMs<=targetMs){
            _withinBudgetStreak++;
            if(_withinBudgetStreak>=_c.RecoveryThreshold&&_level!=DegradationLevel.Full){decision=BudgetDecision.Upgrade;_level=PrevDegradation(_level);_withinBudgetStreak=0;}
        }
        else if(error>0){_withinBudgetStreak=0;if(pidOut>0.5&&_level!=_c.MaxDegradation){decision=BudgetDecision.Degrade;_level=NextDegradation(_level);}}
        _lastDecision=decision;
        return decision;
    }

    public DegradationLevel Level=>_level; public double EValue=>_ep.EValue;
    public double EProcessSigmaMs=>_ep.SigmaMs; public double PidIntegral=>_pid.Integral;
    public uint FramesObserved=>_ep.FramesObserved; public BudgetDecision LastDecision=>_lastDecision;
    public BudgetControllerConfig Config=>_c;

    public void Reset(){_pid.Reset();_ep.Reset();_level=DegradationLevel.Full;_withinBudgetStreak=0;_lastDecision=BudgetDecision.Hold;}

    static DegradationLevel NextDegradation(DegradationLevel l)=>l switch{
        DegradationLevel.Full=>DegradationLevel.SimpleBorders,DegradationLevel.SimpleBorders=>DegradationLevel.NoStyling,
        DegradationLevel.NoStyling=>DegradationLevel.EssentialOnly,DegradationLevel.EssentialOnly=>DegradationLevel.Skeleton,
        DegradationLevel.Skeleton=>DegradationLevel.SkipFrame,_=>DegradationLevel.SkipFrame,
    };
    static DegradationLevel PrevDegradation(DegradationLevel l)=>l switch{
        DegradationLevel.SkipFrame=>DegradationLevel.Skeleton,DegradationLevel.Skeleton=>DegradationLevel.EssentialOnly,
        DegradationLevel.EssentialOnly=>DegradationLevel.NoStyling,DegradationLevel.NoStyling=>DegradationLevel.SimpleBorders,
        DegradationLevel.SimpleBorders=>DegradationLevel.Full,_=>DegradationLevel.Full,
    };
}

// ── RenderBudget ──────────────────────────────────────────────────────────

public sealed class FrameBudgetConfig
{
    public TimeSpan Total=TimeSpan.FromMilliseconds(16.667); public DegradationLevel MaxDegradation=DegradationLevel.SkipFrame;
    public int RecoveryThreshold=10; public bool Enabled=true;
    public static FrameBudgetConfig Default=>new();
}

public sealed class RenderBudget
{
    readonly FrameBudgetConfig _c; readonly BudgetController _controller;
    DegradationLevel _degradation=DegradationLevel.Full; TimeSpan _used; int _framesSinceDegrade;

    public RenderBudget(FrameBudgetConfig? c=null){_c=c??FrameBudgetConfig.Default;_controller=new BudgetController(new BudgetControllerConfig{TargetFrameMs=_c.Total.TotalMilliseconds,MaxDegradation=_c.MaxDegradation,RecoveryThreshold=_c.RecoveryThreshold});}

    public void NextFrame(){_used=TimeSpan.Zero;}
    public void RecordFrameTime(TimeSpan t){_controller.Update(t);}
    public void Degrade(){_degradation=NextDegradation(_degradation);_framesSinceDegrade=0;}
    public bool Exhausted=>_used>=_c.Total||_degradation==DegradationLevel.SkipFrame;
    public DegradationLevel DegradationLevel=>_degradation;
    public TimeSpan Total=>_c.Total;
    public void SetDegradation(DegradationLevel l){_degradation=l;}
    public BudgetController Controller=>_controller;

    static DegradationLevel NextDegradation(DegradationLevel l)=>l switch{
        DegradationLevel.Full=>DegradationLevel.SimpleBorders,DegradationLevel.SimpleBorders=>DegradationLevel.NoStyling,
        DegradationLevel.NoStyling=>DegradationLevel.EssentialOnly,DegradationLevel.EssentialOnly=>DegradationLevel.Skeleton,
        DegradationLevel.Skeleton=>DegradationLevel.SkipFrame,_=>DegradationLevel.SkipFrame,
    };
}
