// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/cost_model.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Formal cost models for caches, pipeline scheduling, and batching.

namespace FrankenTui.Runtime;

// ─── Cache Cost Model ─────────────────────────────────────────────────────

public sealed class CacheCostParams
{
    public double CMissUs=100, CMemPerByte=0.001, ItemBytes=256, WorkingSetN=500, ZipfAlpha=1.5, BudgetMaxBytes=1024*1024;
    public static CacheCostParams Default=>new();

    public double MissRate(double budgetBytes)
    {
        double maxItems=budgetBytes/ItemBytes,cacheItems=Math.Min(maxItems,WorkingSetN);
        if(cacheItems<=0||WorkingSetN<=0)return 1.0;
        return 1.0-Math.Pow(cacheItems/WorkingSetN,1.0/ZipfAlpha);
    }

    public double TotalCost(double budgetBytes)=>CMissUs*MissRate(budgetBytes)+CMemPerByte*budgetBytes;

    public CacheCostPoint Evaluate(double budgetBytes)=>new(budgetBytes,MissRate(budgetBytes),TotalCost(budgetBytes));

    public double OptimalBudget()
    {
        double term=Math.Pow(CMissUs/(ZipfAlpha*CMemPerByte*WorkingSetN),ZipfAlpha/(1.0+ZipfAlpha));
        double optimal=ItemBytes*WorkingSetN*term;
        return Math.Clamp(optimal,ItemBytes,BudgetMaxBytes);
    }

    public CacheCostResult Optimize()
    {
        double opt=OptimalBudget(); var optimal=Evaluate(opt);
        var alternatives=new List<CacheCostPoint>(); double[] mults={0.25,0.5,2,4};
        foreach(var m in mults){double b=Math.Clamp(opt*m,ItemBytes,BudgetMaxBytes);alternatives.Add(Evaluate(b));}
        return new CacheCostResult(optimal,alternatives);
    }
}

public sealed record CacheCostPoint(double BudgetBytes,double MissRate,double TotalCostUs);
public sealed record CacheCostResult(CacheCostPoint Optimal,List<CacheCostPoint> Alternatives)
{
    public string ToJsonl()=>$"{{\"optimal_budget\":{Optimal.BudgetBytes},\"miss_rate\":{Optimal.MissRate:F6},\"total_cost_us\":{Optimal.TotalCostUs:F3}}}";
}

// ─── Pipeline Scheduling Model ────────────────────────────────────────────

public sealed class StageStats{public double MeanUs,VarianceUs;public double SecondMoment=>VarianceUs+MeanUs*MeanUs;}

public sealed class PipelineCostParams
{
    public double ArrivalRatePerMs=0.0167,TotalBudgetUs=16666; public List<StageStats> Stages=new();
    public static PipelineCostParams Default=>new();

    public PipelineCostResult Analyze()
    {
        double eS=0,vS=0,eS2=0;
        foreach(var s in Stages){eS+=s.MeanUs;vS+=s.VarianceUs;eS2+=s.SecondMoment;}
        double rho=eS*ArrivalRatePerMs;
        double eT=rho>=1?double.PositiveInfinity:eS+(ArrivalRatePerMs*eS2)/(2.0*(1.0-rho));
        double latencyBudgetUsed=eT/TotalBudgetUs;
        var breakdown=Stages.Select((s,i)=>new StageBreakdown(i,s.MeanUs,s.VarianceUs,s.MeanUs/eS*100)).ToList();
        return new PipelineCostResult(eS,vS,rho,eT,latencyBudgetUsed,breakdown);
    }
}

public sealed record StageBreakdown(int Stage,double MeanUs,double VarianceUs,double PercentOfTotal);
public sealed record PipelineCostResult(double TotalMeanUs,double TotalVarianceUs,double Utilization,double MeanSojournUs,double LatencyBudgetUsed,List<StageBreakdown> Breakdown)
{
    public string ToJsonl()=>$"{{\"total_mean_us\":{TotalMeanUs:F3},\"utilization\":{Utilization:F4},\"sojourn_us\":{MeanSojournUs:F3},\"budget_used\":{LatencyBudgetUsed:F4}}}";
}

// ─── Patch Batching Model ─────────────────────────────────────────────────

public sealed class BatchCostParams
{
    public double COverheadUs=50,CPerPatchUs=10,CLatencyUs=100; public ulong NPending=20,MaxBatch=16;
    public static BatchCostParams Default=>new();

    public double TotalCost(ulong batchSize)
    {
        ulong k=Math.Max(1,Math.Min(batchSize,NPending));
        ulong batches=(NPending+k-1)/k;
        return batches*(COverheadUs+k*CPerPatchUs)+(k-1)*CLatencyUs;
    }

    public BatchCostPoint Evaluate(ulong batchSize)
    {
        ulong k=Math.Max(1,Math.Min(batchSize,NPending));
        return new BatchCostPoint(k,TotalCost(k));
    }

    public ulong OptimalBatch()
    {
        double kStar=Math.Sqrt(NPending*COverheadUs/CLatencyUs);
        return Math.Clamp((ulong)Math.Round(kStar),1,MaxBatch);
    }

    public BatchCostResult Optimize()
    {
        ulong opt=OptimalBatch(); var optimal=Evaluate(opt);
        var alternatives=new List<BatchCostPoint>();
        for(ulong k=1;k<=Math.Min(5,MaxBatch);k++){if(k!=opt)alternatives.Add(Evaluate(k));}
        return new BatchCostResult(optimal,alternatives);
    }
}

public sealed record BatchCostPoint(ulong BatchSize,double TotalCostUs);
public sealed record BatchCostResult(BatchCostPoint Optimal,List<BatchCostPoint> Alternatives)
{
    public string ToJsonl()=>$"{{\"optimal_batch\":{Optimal.BatchSize},\"cost_us\":{Optimal.TotalCostUs:F3}}}";
}
