// Tests for .external/frankentui/crates/ftui-runtime/src/eprocess_throttle.rs
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class EProcessThrottleTests
{
    static ThrottleConfig TestConfig() => new() { Alpha=0.05, Mu0=0.1, InitialLambda=0.5, GrapaEta=0.1, HardDeadlineMs=500, MinObservationsBetween=4, RateWindowSize=32, EnableLogging=true };

    [Fact] public void InitialState(){var t=new EProcessThrottle(TestConfig());Assert.InRange(t.Wealth,0.99,1.01);Assert.Equal(0UL,t.ObservationCount);Assert.True(t.Lambda>0);Assert.InRange(t.Threshold,19.9,20.1);}
    [Fact] public void Mu0Clamped(){var c=TestConfig();c.Mu0=0;Assert.True(new EProcessThrottle(c).Wealth>=0);c.Mu0=1;Assert.True(new EProcessThrottle(c).Wealth>=0);}
    [Fact] public void ObserveWealthIncreasesOnMatches(){var t=new EProcessThrottle(TestConfig());Assert.True(t.Observe(true).Wealth>1.0);}
    [Fact] public void ObserveWealthDecreasesOnMisses(){var t=new EProcessThrottle(TestConfig());t.Observe(true);var w=t.Wealth;for(int i=0;i<10;i++)t.Observe(false);Assert.True(t.Wealth<w);}
    [Fact] public void WealthStaysPositive(){var t=new EProcessThrottle(TestConfig());for(int i=0;i<1000;i++)t.Observe(false);Assert.True(t.Wealth>0);}
    [Fact] public void ThresholdTriggersRecompute(){var t=new EProcessThrottle(TestConfig());for(int i=0;i<1000;i++)if(t.Observe(true).ShouldRecompute)return;Assert.True(false,"Should have triggered");}
    [Fact] public void HardDeadlineForcesRecompute(){var c=TestConfig();c.HardDeadlineMs=10;c.MinObservationsBetween=0;c.Mu0=0.99;var t=new EProcessThrottle(c);t.Observe(false);System.Threading.Thread.Sleep(20);Assert.True(t.Observe(false).ShouldRecompute);}
    [Fact] public void MinObsBlocksRapidRecompute(){var c=TestConfig();c.MinObservationsBetween=100;c.HardDeadlineMs=100000;var t=new EProcessThrottle(c);for(int i=0;i<50;i++)t.Observe(true);Assert.Equal(0UL,t.Stats().TotalRecomputes);}
    [Fact] public void EmpiricalMatchRate(){var t=new EProcessThrottle(TestConfig());for(int i=0;i<5;i++)t.Observe(true);for(int i=0;i<5;i++)t.Observe(false);Assert.InRange(t.EmpiricalMatchRate(),0.4,0.6);}
    [Fact] public void ResetResetsWealth(){var t=new EProcessThrottle(TestConfig());for(int i=0;i<100;i++)t.Observe(true);var l=t.Lambda;t.Reset();Assert.InRange(t.Wealth,0.99,1.01);Assert.InRange(t.Lambda-l,-0.1,0.1);}
    [Fact] public void SetMu0Resets(){var t=new EProcessThrottle(TestConfig());t.SetMu0(0.2);Assert.InRange(t.Wealth,0.99,1.01);}
    [Fact] public void StatsAccurate(){var c=TestConfig();c.EnableLogging=false;var t=new EProcessThrottle(c);for(int i=0;i<50;i++)t.Observe(i%2==0);var s=t.Stats();Assert.Equal(50UL,s.TotalObservations);Assert.True(s.CurrentWealth>=0);}
    [Fact] public void LogsCapturedWhenEnabled(){var t=new EProcessThrottle(TestConfig());t.Observe(true);t.Observe(false);Assert.Equal(2,t.Logs.Count);t.ClearLogs();Assert.Empty(t.Logs);}
    [Fact] public void DecisionJsonlFormat(){var j=new EProcessThrottle(TestConfig()).Observe(true).ToJsonl();Assert.Contains("eprocess-throttle-v1",j);Assert.Contains("should_recompute",j);}
    [Fact] public void ObservationCountIncrements(){var t=new EProcessThrottle(TestConfig());t.Observe(true);t.Observe(false);Assert.Equal(2UL,t.ObservationCount);}
    [Fact] public void ForcedRecomputesCounted(){var c=TestConfig();c.HardDeadlineMs=10;c.MinObservationsBetween=0;c.Mu0=0.99;var t=new EProcessThrottle(c);t.Observe(false);System.Threading.Thread.Sleep(20);t.Observe(false);Assert.True(t.Stats().ForcedRecomputes>0);}
}
