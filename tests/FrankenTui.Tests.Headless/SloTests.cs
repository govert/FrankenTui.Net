// Tests for .external/frankentui/crates/ftui-runtime/src/slo.rs
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class SloTests
{
    [Fact] public void ParseMinimalValidYaml(){var r=SloEngine.ParseSloYaml("regression_threshold: 0.10\nnoise_tolerance: 0.05\nmetrics:\n  render_p99:\n    metric_type: latency\n    max_value: 4000.0\n    max_ratio: 1.25\n    safe_mode_trigger: true\n");var s=r.Unwrap();Assert.Single(s.Metrics);var m=s.Metrics["render_p99"];Assert.Equal(MetricType.Latency,m.MetricType);Assert.Equal(4000,m.MaxValue);Assert.True(m.SafeModeTrigger);}
    [Fact] public void ParseEmptyUsesDefaults(){var s=SloEngine.ParseSloYaml("").Unwrap();Assert.InRange(s.RegressionThreshold,0.09,0.11);Assert.InRange(s.NoiseTolerance,0.04,0.06);Assert.Equal(3,s.SafeModeBreachCount);}
    [Fact] public void RejectInvalidThreshold(){var e=SloEngine.ParseSloYaml("regression_threshold: 1.5\nnoise_tolerance: 0.05\n").UnwrapErr();Assert.Contains(e,err=>err.ToString().Contains("regression_threshold"));}
    [Fact] public void RejectNoiseGteRegression(){var e=SloEngine.ParseSloYaml("regression_threshold: 0.05\nnoise_tolerance: 0.10\n").UnwrapErr();Assert.Contains(e,err=>err is SloSchemaError.InvalidThreshold{Field:"noise_tolerance"});}
    [Fact] public void BreachAbsoluteThreshold(){var s=SloSchema.Default;s.Metrics["p99"]=new MetricSlo{MetricType=MetricType.Latency,MaxValue=500,MaxRatio=1.15};Assert.Equal(BreachSeverity.AbsoluteBreach,SloEngine.CheckBreach("p99",400,520,s).Severity);}
    [Fact] public void WithinSloNoBreach(){var s=SloSchema.Default;s.Metrics["p99"]=new MetricSlo{MetricType=MetricType.Latency,MaxValue=500,MaxRatio=1.15};Assert.Equal(BreachSeverity.None,SloEngine.CheckBreach("p99",400,404,s).Severity);}
    [Fact] public void SafeModeTriggeredByFlag(){var b=new List<BreachResult>{new("critical",MetricType.Latency,200,600,3,BreachSeverity.Breach,true)};Assert.IsType<SafeModeDecision.Triggered>(SloEngine.CheckSafeMode(b,SloSchema.Default));}
    [Fact] public void SafeModeTriggeredByBreachCount(){var s=SloSchema.Default;s.SafeModeBreachCount=2;var b=new List<BreachResult>{new("a",MetricType.Latency,100,200,2,BreachSeverity.Breach,false),new("b",MetricType.Memory,1000,3000,3,BreachSeverity.AbsoluteBreach,false)};Assert.IsType<SafeModeDecision.Triggered>(SloEngine.CheckSafeMode(b,s));}
    [Fact] public void SafeModeNotTriggered(){var b=new List<BreachResult>{new("ok",MetricType.Latency,100,115,1.15,BreachSeverity.Breach,false)};Assert.IsType<SafeModeDecision.Normal>(SloEngine.CheckSafeMode(b,SloSchema.Default));}
    [Fact] public void ZeroBaselineNoPanic(){var r=SloEngine.CheckBreach("zero",0,5,SloSchema.Default);Assert.InRange(r.Ratio,0.99,1.01);}
    [Fact] public void ImprovementNotFlagged(){Assert.Equal(BreachSeverity.None,SloEngine.CheckBreach("ok",200,150,SloSchema.Default).Severity);}
    [Fact] public void RunSloCheckBatch(){var s=SloSchema.Default;s.Metrics["p99"]=new MetricSlo{MetricType=MetricType.Latency,MaxValue=500,MaxRatio=1.15};var(breaches,dec)=SloEngine.RunSloCheck(s,new List<(string,double,double)>{("p99",400,404)});Assert.Single(breaches);Assert.IsType<SafeModeDecision.Normal>(dec);}
}
