// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/conformal_predictor.rs
// Conformal prediction for render pipeline timing with Mondrian bucket calibration.

using FrankenTui.Render;

namespace FrankenTui.Runtime;

public sealed class ConformalConfig
{
    public double Alpha=0.05,QDefault=10000; public int MinSamples=20,WindowSize=256;
    public static ConformalConfig Default=>new();
}

public enum ModeBucket{Inline,InlineAuto,AltScreen}
public enum DiffBucket{Full,DirtyRows,FullRedraw}

public readonly record struct BucketKey(ModeBucket Mode,DiffBucket Diff,byte SizeBucket)
{
    public static BucketKey FromContext(ScreenMode mode,DiffStrategy diff,ushort cols,ushort rows)
    {
        var mb=mode switch{ScreenMode.Inline=>ModeBucket.Inline,ScreenMode.AltScreen=>ModeBucket.AltScreen,_=>ModeBucket.Inline};
        var db=diff switch{DiffStrategy.Full=>DiffBucket.Full,DiffStrategy.DirtyRows=>DiffBucket.DirtyRows,DiffStrategy.FullRedraw=>DiffBucket.FullRedraw,_=>DiffBucket.Full};
        byte sb=(byte)((cols*rows)switch{<2000=>0,<8000=>1,<32000=>2,_=>3});
        return new(mb,db,sb);
    }
    public override string ToString()=>$"{Mode}:{Diff}:{SizeBucket}";
}

public sealed class ConformalPrediction
{
    public double UpperUs,YHat,BudgetUs,Quantile,Confidence; public bool Risk; public BucketKey Bucket; public int SampleCount,FallbackLevel,WindowSize; public ulong ResetCount;
    public string ToJsonl()=>$"{{\"upper_us\":{UpperUs:F1},\"risk\":{Risk.ToString().ToLowerInvariant()},\"confidence\":{Confidence:F4},\"sample_count\":{SampleCount},\"quantile\":{Quantile:F1},\"fallback_level\":{FallbackLevel}}}";
}

public sealed class ConformalPredictor
{
    ConformalConfig _c; readonly Dictionary<BucketKey,Queue<double>> _buckets=new(); ulong _resetCount;

    public ConformalPredictor(ConformalConfig? c=null){_c=c??ConformalConfig.Default;}
    public ConformalConfig Config=>_c;
    public int BucketSamples(BucketKey key)=>_buckets.GetValueOrDefault(key)?.Count??0;
    public void ResetAll(){_buckets.Clear();_resetCount++;}
    public void ResetBucket(BucketKey key){if(_buckets.TryGetValue(key,out var q)){q.Clear();_resetCount++;}}

    public void Observe(BucketKey key,double yHatUs,double observedUs)
    {
        double r=observedUs-yHatUs; if(!double.IsFinite(r))return;
        if(!_buckets.TryGetValue(key,out var q)){q=new Queue<double>();_buckets[key]=q;}
        q.Enqueue(r); while(q.Count>_c.WindowSize)q.Dequeue();
    }

    public ConformalPrediction Predict(BucketKey key,double yHatUs,double budgetUs)
    {
        var(quantile,sampleCount,fallback)=QuantileFor(key);
        double upper=yHatUs+Math.Max(0,quantile); bool risk=upper>budgetUs;
        return new ConformalPrediction{UpperUs=upper,Risk=risk,Confidence=1-_c.Alpha,Bucket=key,SampleCount=sampleCount,Quantile=quantile,FallbackLevel=fallback,WindowSize=_c.WindowSize,ResetCount=_resetCount,YHat=yHatUs,BudgetUs=budgetUs};
    }

    (double quantile,int samples,int fallback) QuantileFor(BucketKey key)
    {
        int minS=Math.Max(1,_c.MinSamples);
        var exact=CollectExact(key); if(exact.Count>=minS)return(Quantile(exact,_c.Alpha),exact.Count,0);
        var md=CollectModeDiff(key.Mode,key.Diff); if(md.Count>=minS)return(Quantile(md,_c.Alpha),md.Count,1);
        var mo=CollectMode(key.Mode); if(mo.Count>=minS)return(Quantile(mo,_c.Alpha),mo.Count,2);
        var all=CollectAll(); if(all.Count>0)return(Quantile(all,_c.Alpha),all.Count,3);
        return(_c.QDefault,0,3);
    }

    List<double> CollectExact(BucketKey key)=>_buckets.TryGetValue(key,out var q)?q.ToList():new();
    List<double> CollectModeDiff(ModeBucket m,DiffBucket d){var r=new List<double>();foreach(var(k,v)in _buckets)if(k.Mode==m&&k.Diff==d)r.AddRange(v);return r;}
    List<double> CollectMode(ModeBucket m){var r=new List<double>();foreach(var(k,v)in _buckets)if(k.Mode==m)r.AddRange(v);return r;}
    List<double> CollectAll(){var r=new List<double>();foreach(var v in _buckets.Values)r.AddRange(v);return r;}

    static double Quantile(List<double> values,double alpha)
    {
        var s=values.OrderBy(x=>x).ToList(); int n=s.Count;
        double t=(1-alpha)*(n+1); int idx=Math.Min((int)Math.Ceiling(t)-1,n-1); if(idx<0)idx=0;
        return s[idx];
    }
}
