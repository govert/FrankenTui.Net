// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/alpha_investing.rs
// Alpha-Investing: sequential FDR control for multiple simultaneous alerts.

namespace FrankenTui.Runtime;

public sealed class AlphaInvestingConfig
{
    public double InitialWealth=0.5,RewardFactor=1.0,DefaultAlpha=0.05,MaxAlpha=0.5,MinAlpha=1e-10; public int MaxTests=int.MaxValue;
    public static AlphaInvestingConfig Default=>new();
}

public enum TestOutcome{NotInvested,NotRejected,Rejected}

public sealed record TestRecord(int TestIndex,double PValue,double AlphaInvested,double WealthBefore,double WealthAfter,TestOutcome Outcome);

public sealed class AlphaInvestor
{
    AlphaInvestingConfig _c; double _w; int _n,_disc; List<TestRecord> _log=new();

    public AlphaInvestor(AlphaInvestingConfig c){_c=c;_w=c.InitialWealth;}
    public static AlphaInvestor WithDefaults()=>new(AlphaInvestingConfig.Default);
    public double Wealth=>_w; public int TestsRun=>_n; public int Discoveries=>_disc;
    public double DiscoveryRate=>_n>0?(double)_disc/_n:0;
    public IReadOnlyList<TestRecord> Log=>_log;

    public TestOutcome Test(double pValue)=>TestWithInvestment(pValue,null);
    public TestOutcome TestWithInvestment(double pValue,double? customAlpha)
    {
        _n++;
        if(_n>_c.MaxTests||_w<_c.MinAlpha)return LogAndReturn(pValue,0,TestOutcome.NotInvested);
        double alpha=Math.Min(Math.Max(customAlpha??_c.DefaultAlpha,_c.MinAlpha),_w*0.5);
        double wb=_w; _w-=alpha;
        bool rejected=pValue<=alpha;
        if(rejected){_disc++;_w+=alpha*_c.RewardFactor;return LogAndReturn(pValue,alpha,TestOutcome.Rejected);}
        return LogAndReturn(pValue,alpha,TestOutcome.NotRejected);
    }

    public List<TestOutcome> TestBatch(double[] pValues){var r=new List<TestOutcome>();foreach(var p in pValues)r.Add(Test(p));return r;}
    public void Reset(){_w=_c.InitialWealth;_n=0;_disc=0;_log.Clear();}

    TestOutcome LogAndReturn(double p,double alpha,TestOutcome o){_log.Add(new(_n,p,alpha,_w+alpha,_w,o));return o;}
}
