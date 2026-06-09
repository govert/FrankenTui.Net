// Tests for .external/frankentui/crates/ftui-runtime/src/conformal_alert.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class ConformalAlertTests
{
    static AlertConfig TestConfig() => new() { Alpha=0.05, MinCalibration=5, MaxCalibration=100, Lambda=0.5, AdaptiveLambda=false, EnableLogging=true, Hysteresis=1.0, AlertCooldown=0 };

    [Fact] public void InitialState(){var a=new ConformalAlert(TestConfig());Assert.InRange(a.EValue,0.99,1.01);Assert.Equal(0,a.CalibrationCount);Assert.InRange(a.Mean,-0.01,0.01);}
    [Fact] public void CalibrationUpdatesStats(){var a=new ConformalAlert(TestConfig());a.Calibrate(10);a.Calibrate(20);a.Calibrate(30);Assert.Equal(3,a.CalibrationCount);Assert.InRange(a.Mean,19.9,20.1);}
    [Fact] public void CalibrationWindowEnforced(){var c=TestConfig();c.MaxCalibration=5;var a=new ConformalAlert(c);for(int i=1;i<=10;i++)a.Calibrate(i);Assert.Equal(5,a.CalibrationCount);}
    [Fact] public void ConformalThresholdIncreases(){var a=new ConformalAlert(TestConfig());for(int i=1;i<=20;i++)a.Calibrate(i);var t=a.Threshold;Assert.True(t>0);Assert.True(t<double.MaxValue);}
    [Fact] public void ExtremeValueTriggersAlert(){var c=TestConfig();c.AlertCooldown=0;var a=new ConformalAlert(c);a.Calibrate(50);a.Calibrate(50);a.Calibrate(50);a.Calibrate(50);a.Calibrate(50);Assert.True(a.Observe(500).Evidence.ConformalAlert);}
    [Fact] public void NormalValueNoAlert(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{45.0,50,55,45,55,50})a.Calibrate(v);Assert.False(a.Observe(48).IsAlert);}
    [Fact] public void InsufficientCalibration(){var c=TestConfig();var a=new ConformalAlert(c);a.Calibrate(50);a.Calibrate(51);var d=a.Observe(1000);Assert.False(d.IsAlert);Assert.Equal(AlertReason.InsufficientCalibration,d.Evidence.Reason);}
    [Fact] public void CooldownPreventsRapidAlerts(){var c=TestConfig();c.AlertCooldown=5;c.Hysteresis=0.1;var a=new ConformalAlert(c);for(int i=0;i<5;i++)a.Calibrate(50);bool triggered=false;for(int i=0;i<10;i++)if(a.Observe(200).IsAlert){triggered=true;break;}Assert.True(triggered);var d=a.Observe(200);Assert.Equal(AlertReason.InCooldown,d.Evidence.Reason);}
    [Fact] public void EvidenceContainsAllFields(){var c=TestConfig();var a=new ConformalAlert(c);foreach(var v in new[]{45.0,50,55,48,52})a.Calibrate(v);var e=a.Observe(75).Evidence;Assert.Equal(1UL,e.ObservationIdx);Assert.InRange(e.Value,74.9,75.1);Assert.True(e.EValue>0);Assert.True(e.ConformalScore>=0&&e.ConformalScore<=1);}
    [Fact] public void LogsCapturedWhenEnabled(){var c=TestConfig();c.EnableLogging=true;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,50,50,50,50})a.Calibrate(v);a.Observe(60);a.Observe(70);a.Observe(80);Assert.Equal(3,a.Logs.Count);a.ClearLogs();Assert.Empty(a.Logs);}
    [Fact] public void LogsNotCapturedWhenDisabled(){var c=TestConfig();c.EnableLogging=false;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,50,50,50,50})a.Calibrate(v);a.Observe(60);Assert.Empty(a.Logs);}
    [Fact] public void StatsReflectState(){var c=TestConfig();c.AlertCooldown=0;c.Hysteresis=0.1;var a=new ConformalAlert(c);foreach(var v in new[]{45.0,50,55,48,52})a.Calibrate(v);for(int i=0;i<5;i++)a.Observe(50);for(int i=0;i<5;i++)a.Observe(200);var s=a.Stats();Assert.Equal(10UL,s.TotalObservations);Assert.Equal(5,s.CalibrationSamples);}
    [Fact] public void EValueGrowsOnExtreme(){var c=TestConfig();c.Hysteresis=1e10;var a=new ConformalAlert(c);foreach(var v in new[]{49.0,50,51,50,49.5,50.5})a.Calibrate(v);var eb=a.EValue;var d=a.Observe(100);Assert.True(d.Evidence.EValue>eb);}
    [Fact] public void EValueStaysPositive(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{45.0,50,55,50,45,55})a.Calibrate(v);for(int i=0;i<100;i++){a.Observe(50);Assert.True(a.EValue>0);}}

    [Fact] public void EValueCeilingPreventsOverflow(){var c=TestConfig();c.Hysteresis=double.MaxValue;c.AlertCooldown=0;var a=new ConformalAlert(c);for(int i=0;i<10;i++)a.Calibrate(0);var d=a.Observe(1e100);Assert.True(double.IsFinite(d.Evidence.EValue));}
    [Fact] public void EValueFloorPreventsUnderflow(){var c=TestConfig();c.Hysteresis=double.MaxValue;var a=new ConformalAlert(c);for(int i=0;i<10;i++)a.Calibrate(1e100);var d=a.Observe(0);Assert.True(d.Evidence.EValue>=1e-12);}
    [Fact] public void NaNCalibration(){var a=new ConformalAlert(TestConfig());a.Calibrate(double.NaN);Assert.Equal(1,a.CalibrationCount);}
    [Fact] public void InfinityCalibration(){var a=new ConformalAlert(TestConfig());a.Calibrate(double.PositiveInfinity);Assert.Equal(1,a.CalibrationCount);}
    [Fact] public void NegInfinityObservation(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);Assert.True(a.Observe(double.NegativeInfinity).Evidence.ConformalAlert);}
    [Fact] public void AlphaOne(){var c=TestConfig();c.Alpha=1;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);Assert.True(a.Threshold>=0&&a.Threshold<double.MaxValue);}
    [Fact] public void HysteresisZero(){var c=TestConfig();c.Hysteresis=0;c.AlertCooldown=0;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,50,50,50,50})a.Calibrate(v);Assert.True(a.Observe(51).Evidence.EProcessAlert);}
    [Fact] public void MaxCalibrationZero(){var c=TestConfig();c.MaxCalibration=0;var a=new ConformalAlert(c);a.Calibrate(50);Assert.Equal(0,a.CalibrationCount);}
    [Fact] public void MinCalibrationZero(){var c=TestConfig();c.MinCalibration=0;c.AlertCooldown=0;var a=new ConformalAlert(c);a.Calibrate(50);Assert.NotEqual(AlertReason.InsufficientCalibration,a.Observe(55).Evidence.Reason);}
    [Fact] public void AdaptiveLambdaGrapa(){var c=TestConfig();c.AdaptiveLambda=true;c.GrapaEta=0.5;c.Hysteresis=1e10;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);var lb=a.Stats().CurrentLambda;a.Observe(100);Assert.True(Math.Abs(a.Stats().CurrentLambda-lb)>1e-10);}
    [Fact] public void EmptyCalibration(){var a=new ConformalAlert(TestConfig());Assert.Equal(double.MaxValue,a.Threshold);}
    [Fact] public void SingleCalibrationValue(){var a=new ConformalAlert(TestConfig());a.Calibrate(50);Assert.True(a.Threshold>=0&&a.Threshold<double.MaxValue);}
    [Fact] public void EdgeAlphaVerySmall(){var c=TestConfig();c.Alpha=1e-10;c.Hysteresis=1;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);Assert.False(a.Observe(52).Evidence.EProcessAlert);}
    [Fact] public void EdgeLambdaClampedZero(){var c=TestConfig();c.Lambda=0;c.AdaptiveLambda=false;Assert.True(new ConformalAlert(c).Stats().CurrentLambda>0);}
    [Fact] public void EdgeLambdaClampedOne(){var c=TestConfig();c.Lambda=1;c.AdaptiveLambda=false;Assert.True(new ConformalAlert(c).Stats().CurrentLambda<1);}
    [Fact] public void EdgeSigma0Zero(){var c=TestConfig();c.Sigma0=0;c.AdaptiveLambda=false;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);Assert.True(double.IsFinite(a.Observe(55).Evidence.EValue));}
    [Fact] public void AllSameCalibration(){var a=new ConformalAlert(TestConfig());for(int i=0;i<10;i++)a.Calibrate(50);Assert.True(a.Std<0.1);Assert.True(a.Observe(51).Evidence.ConformalAlert);}
    [Fact] public void Deterministic(){var c=TestConfig();var run=()=>{var a=new ConformalAlert(c);foreach(var v in new[]{50.0,51,49,52,48})a.Calibrate(v);var ds=new List<bool>();foreach(var v in new[]{55.0,45,100,50})ds.Add(a.Observe(v).IsAlert);return(ds,a.EValue,a.Threshold);};var(r1,e1,t1)=run();var(r2,e2,t2)=run();Assert.Equal(r1,r2);Assert.InRange(e1-e2,-1e-10,1e-10);Assert.InRange(t1-t2,-1e-10,1e-10);}
    [Fact] public void ResetClearsEProcess(){var c=TestConfig();c.Hysteresis=1e10;var a=new ConformalAlert(c);foreach(var v in new[]{45.0,50,55,48,52})a.Calibrate(v);a.Observe(200);a.ResetEProcess();Assert.InRange(a.EValue,0.99,1.01);}
    [Fact] public void ClearCalibrationResetsAll(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{50.0,50,50,50,50})a.Calibrate(v);a.Observe(75);a.ClearCalibration();Assert.Equal(0,a.CalibrationCount);Assert.InRange(a.Mean,-0.01,0.01);}
    [Fact] public void NaN_Observation(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);var d=a.Observe(double.NaN);Assert.False(d.Evidence.ConformalAlert);}
    [Fact] public void InfinityObservation(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);var d=a.Observe(double.PositiveInfinity);Assert.True(d.Evidence.ConformalAlert);Assert.True(double.IsFinite(d.Evidence.EValue)||d.Evidence.EValue<=1e12);}
    [Fact] public void AlphaVerySmall(){var c=TestConfig();c.Alpha=1e-10;c.Hysteresis=1.0;var a=new ConformalAlert(c);foreach(var v in new[]{50.0,51,49,50,50})a.Calibrate(v);Assert.False(a.Observe(52).Evidence.EProcessAlert);}
    [Fact] public void LambdaClamped(){var c=TestConfig();c.Lambda=0;c.AdaptiveLambda=false;Assert.True(new ConformalAlert(c).Stats().CurrentLambda>0);c.Lambda=1;Assert.True(new ConformalAlert(c).Stats().CurrentLambda<1);}
    [Fact] public void EvidenceSummaryFormat(){var a=new ConformalAlert(TestConfig());foreach(var v in new[]{50.0,50,50,50,50})a.Calibrate(v);var s=a.Observe(75).EvidenceSummary();Assert.Contains("obs=",s);Assert.Contains("E=",s);}
}
