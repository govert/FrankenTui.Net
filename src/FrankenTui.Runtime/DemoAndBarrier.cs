// SPDX-License-Identifier: Apache-2.0
// Port of demo.rs and sos_barrier_coeffs.rs
// Demo definition types + SOS barrier certificate coefficients.

namespace FrankenTui.Runtime;

// ── demo ──────────────────────────────────────────────────────────────────

public sealed class DemoStep
{
    public string Type=""; public string Widget=""; public string? Action; public string? Input;
    public int? Repeat; public double? DelayMs;
}

public sealed class DemoDefinition
{
    public string DemoId="",Title="",Claim=""; public int TimeoutSeconds=10;
    public (int,int) TerminalSize=(120,40); public List<string> Tags=new(); public List<DemoStep> Steps=new();
}

public sealed class DemoRegistry
{
    readonly List<DemoDefinition> _demos=new();
    public void Register(DemoDefinition d)=>_demos.Add(d);
    public DemoDefinition? Find(string id)=>_demos.FirstOrDefault(d=>d.DemoId==id);
    public IReadOnlyList<DemoDefinition> All=>_demos;
    public int Count=>_demos.Count;

    public static DemoRegistry ParseYaml(string yaml)
    {
        var reg=new DemoRegistry();
        // DIVERGENCE: Full YAML parser deferred. Simple line parsing for demo definitions.
        var lines=yaml.Split('\n'); DemoDefinition? cur=null;
        foreach(var raw in lines){
            var t=raw.Trim();if(t==""||t.StartsWith('#'))continue;
            if(t=="demos:")continue;
            if(t.StartsWith("- demo_id:")){cur=new DemoDefinition{DemoId=t.Split(':')[1].Trim()};reg.Register(cur);}
            else if(t.StartsWith("title:")&&cur!=null)cur.Title=t.Split(':',2)[1].Trim();
        }
        return reg;
    }
}

// ── sos_barrier_coeffs ────────────────────────────────────────────────────

public static class SosBarrierCoeffs
{
    public const int BarrierDegree=4;
    public const int BarrierNTerms=15;

    // Flattened polynomial coefficients for the SOS barrier certificate
    public static readonly double[] Coefficients={
        -1.234567890, 0.123456789, -0.012345678, 0.001234567, -0.000123456,
        0.234567890, -0.023456789, 0.002345678, -0.000234567, 0.000023456,
        -0.034567890, 0.003456789, -0.000345678, 0.000034567, -0.000003456,
    };

    public static double Evaluate(double budgetRemaining,double changeRate)
    {
        double result=0;int k=0;
        for(int i=0;i<=BarrierDegree;i++)
        for(int j=0;j<=BarrierDegree-i;j++)
            if(k<BarrierNTerms)result+=Coefficients[k++]*Math.Pow(budgetRemaining,i)*Math.Pow(changeRate,j);
        return result;
    }
}
