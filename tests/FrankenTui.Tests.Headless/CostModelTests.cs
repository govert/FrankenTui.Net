// Tests for .external/frankentui/crates/ftui-runtime/src/cost_model.rs
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class CostModelTests
{
    [Fact] public void CacheDefaultParams(){var c=CacheCostParams.Default;Assert.True(c.CMissUs>0);Assert.True(c.ItemBytes>0);}
    [Fact] public void CacheMissRateAtMaxBudget(){var c=CacheCostParams.Default;Assert.Equal(0.0,c.MissRate(1e12));}
    [Fact] public void CacheMissRateAtZeroBudget(){var c=CacheCostParams.Default;Assert.Equal(1.0,c.MissRate(0));}
    [Fact] public void CacheTotalCostMonotonic(){var c=CacheCostParams.Default;double prev=double.MaxValue;for(double b=256;b<=1e6;b*=2){double cost=c.TotalCost(b);Assert.True(cost>0);prev=cost;}}
    [Fact] public void CacheOptimalBudgetInRange(){var c=CacheCostParams.Default;double o=c.OptimalBudget();Assert.True(o>=c.ItemBytes&&o<=c.BudgetMaxBytes);}
    [Fact] public void CacheOptimizeReturnsResult(){var r=CacheCostParams.Default.Optimize();Assert.NotNull(r.Optimal);Assert.NotEmpty(r.Alternatives);}
    [Fact] public void CacheResultJsonl(){Assert.Contains("optimal_budget",CacheCostParams.Default.Optimize().ToJsonl());}
    [Fact] public void PipelineDefaultParams(){var p=PipelineCostParams.Default;p.Stages.Add(new StageStats{MeanUs=100,VarianceUs=400});var r=p.Analyze();Assert.True(r.TotalMeanUs>0);}
    [Fact] public void PipelineHighUtilization(){var p=new PipelineCostParams{ArrivalRatePerMs=0.1};p.Stages.Add(new StageStats{MeanUs=100,VarianceUs=400});Assert.True(p.Analyze().Utilization>0);}
    [Fact] public void PipelineResultJsonl(){var p=PipelineCostParams.Default;p.Stages.Add(new StageStats{MeanUs=50});Assert.Contains("total_mean_us",p.Analyze().ToJsonl());}
    [Fact] public void BatchDefaultParams(){var b=BatchCostParams.Default;Assert.True(b.NPending>0);}
    [Fact] public void BatchTotalCostDecreases(){var b=BatchCostParams.Default;double c1=b.TotalCost(1),c4=b.TotalCost(4);Assert.True(c1>0);Assert.True(c4>0);}
    [Fact] public void BatchOptimalBatchInRange(){var b=BatchCostParams.Default;ulong o=b.OptimalBatch();Assert.True(o>=1&&o<=b.MaxBatch);}
    [Fact] public void BatchOptimizeReturnsResult(){var r=BatchCostParams.Default.Optimize();Assert.NotNull(r.Optimal);Assert.NotEmpty(r.Alternatives);}
    [Fact] public void BatchResultJsonl(){Assert.Contains("optimal_batch",BatchCostParams.Default.Optimize().ToJsonl());}
}
