// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/validation_pipeline.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Expected-cost validation ordering with Bayesian online learning.

namespace FrankenTui.Runtime;

public sealed class PipelineConfig
{
    public double PriorAlpha=1,PriorBeta=1,Gamma=0.3; public TimeSpan CMin=TimeSpan.FromTicks(10); // 1μs
    public static PipelineConfig Default=>new();
}

public sealed class ValidatorStats
{
    public int Id; public string Name=""; public double Alpha,Beta; public TimeSpan CostEma; public ulong Observations,Failures;
    public double FailureProb{get{double s=Alpha+Beta;return s>0?Alpha/s:0.5;}}
    public double Score(TimeSpan cMin){var c=CostEma>=cMin?CostEma:cMin;return FailureProb/c.TotalSeconds;}
    public double Variance{get{double s=Alpha+Beta;return s>0?Alpha*Beta/(s*s*(s+1)):1.0/12.0;}}
    public double ConfidenceWidth{get{return 2*1.96*Math.Sqrt(Variance);}}
}

public sealed record LedgerEntry(int Id,string Name,double P,TimeSpan C,double Score,int Rank);
public sealed record ValidationOutcome(int Id,bool Passed,TimeSpan Duration);
public sealed record PipelineResult(bool AllPassed,List<ValidationOutcome> Outcomes,TimeSpan TotalCost,List<int> Ordering,List<LedgerEntry> Ledger,int Skipped);
public sealed record PipelineSummary(int ValidatorCount,ulong TotalRuns,List<int> OptimalOrdering,double ExpectedCostSecs,double NaturalCostSecs,double ImprovementFraction,List<LedgerEntry> Ledger);

public sealed class ValidationPipeline
{
    PipelineConfig _c; List<ValidatorStats> _vs=new(); ulong _runs;

    public ValidationPipeline(PipelineConfig? c=null){_c=c??PipelineConfig.Default;}

    public int Register(string name,TimeSpan initialCost)
    {
        int id=_vs.Count;
        _vs.Add(new ValidatorStats{Id=id,Name=name,Alpha=_c.PriorAlpha,Beta=_c.PriorBeta,CostEma=initialCost<_c.CMin?_c.CMin:initialCost});
        return id;
    }

    public (List<int> ordering,List<LedgerEntry> ledger) ComputeOrdering()
    {
        if(_vs.Count==0)return(new(),new());
        var scored=_vs.Select(v=>(v.Id,v.Score(_c.CMin))).OrderByDescending(x=>x.Item2).ThenBy(x=>x.Id).ToList();
        var ordering=scored.Select(x=>x.Id).ToList();
        var ledger=scored.Select((x,rank)=>{var v=_vs[x.Id];return new LedgerEntry(x.Id,v.Name,v.FailureProb,v.CostEma,x.Item2,rank);}).ToList();
        return(ordering,ledger);
    }

    public double ExpectedCost(List<int> ordering)
    {
        double surv=1,tot=0;
        foreach(var id in ordering){var v=_vs[id];double c=Math.Max(v.CostEma.Ticks,_c.CMin.Ticks)*1e-7;tot+=c*surv;surv*=1-v.FailureProb;}
        return tot;
    }

    public void Update(ValidationOutcome o)
    {
        if(o.Id<0||o.Id>=_vs.Count)return;
        var v=_vs[o.Id];v.Observations++;
        if(o.Passed)v.Beta+=1;else{v.Alpha+=1;v.Failures++;}
        double oldNs=v.CostEma.Ticks*100.0,newNs=o.Duration.Ticks*100.0;
        double up=_c.Gamma*newNs+(1-_c.Gamma)*oldNs;
        v.CostEma=TimeSpan.FromTicks((long)(Math.Max(up,_c.CMin.Ticks*100.0)/100));
    }

    public void UpdateBatch(PipelineResult r){_runs++;foreach(var o in r.Outcomes)Update(o);}
    public PipelineResult Run(Func<int,(bool,TimeSpan)> validate)
    {
        var(ordering,ledger)=ComputeOrdering();var outcomes=new List<ValidationOutcome>();var total=TimeSpan.Zero;bool allPassed=true;
        foreach(var id in ordering){var(p,d)=validate(id);total+=d;outcomes.Add(new(id,p,d));if(!p){allPassed=false;break;}}
        return new(allPassed,outcomes,total,ordering,ledger,ordering.Count-outcomes.Count);
    }

    public ValidatorStats? Stats(int id)=>id>=0&&id<_vs.Count?_vs[id]:null;
    public IReadOnlyList<ValidatorStats> AllStats=>_vs;
    public ulong TotalRuns=>_runs;
    public int ValidatorCount=>_vs.Count;

    public PipelineSummary Summary()
    {
        var(ordering,ledger)=ComputeOrdering();
        double exp=ExpectedCost(ordering);
        var natural=Enumerable.Range(0,_vs.Count).ToList();
        double natCost=ExpectedCost(natural),imp=natCost>0?1-exp/natCost:0;
        return new(_vs.Count,_runs,ordering,exp,natCost,imp,ledger);
    }
}
