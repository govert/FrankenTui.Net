// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/slo.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// SLO schema, breach detection, and safe-mode enforcement.

namespace FrankenTui.Runtime;

public enum MetricType{Latency,Memory,ErrorRate}
public enum BreachSeverity{None,Noise,Breach,AbsoluteBreach}
public abstract record SafeModeDecision{public sealed record Normal: SafeModeDecision; public sealed record Triggered(string Reason): SafeModeDecision;}

public sealed class MetricSlo
{
    public MetricType MetricType; public double? MaxValue,MaxRatio; public bool SafeModeTrigger;
}

public sealed class SloSchema
{
    public double RegressionThreshold=0.10,NoiseTolerance=0.05,SafeModeErrorRate=0.10; public int SafeModeBreachCount=3;
    public Dictionary<string,MetricSlo> Metrics=new();
    public static SloSchema Default=>new();
}

public abstract record SloSchemaError
{
    public sealed record InvalidThreshold(string Field,double Value):SloSchemaError;
    public sealed record MissingField(string Field):SloSchemaError;
    public sealed record ParseError(string Field,string Reason):SloSchemaError;
    public sealed record UnknownMetricType(string Type):SloSchemaError;
    public sealed record DuplicateMetric(string Name):SloSchemaError;
    public sealed record MalformedStructure(string Message):SloSchemaError;
    public override string ToString()=>this switch{InvalidThreshold i=>$"invalid threshold for '{i.Field}': {i.Value}",MissingField m=>$"missing required field: '{m.Field}'",ParseError p=>$"parse error for '{p.Field}': {p.Reason}",UnknownMetricType u=>$"unknown metric type: '{u.Type}'",DuplicateMetric d=>$"duplicate metric: '{d.Name}'",MalformedStructure ms=>$"malformed structure: {ms.Message}",_=>""};
}

public sealed record BreachResult(string MetricName,MetricType MetricType,double Baseline,double Current,double Ratio,BreachSeverity Severity,bool SafeModeTrigger);

public static class SloEngine
{
    public static Result<SloSchema,List<SloSchemaError>> ParseSloYaml(string yaml)
    {
        var schema=SloSchema.Default; var errors=new List<SloSchemaError>(); bool inMetrics=false;
        var curName=""; var curSlo=new MetricSlo(); var seen=new HashSet<string>();
        foreach(var(raw,_)in yaml.Split('\n').Select((l,i)=>(l,i)))
        {
            var t=raw.Trim(); if(t==""||t.StartsWith('#'))continue;
            if(t.Contains('\t')){errors.Add(new SloSchemaError.MalformedStructure("tabs not allowed"));continue;}
            if(TryPrefix(t,"regression_threshold:",out var v))ParseThresh(v,"regression_threshold",ref schema.RegressionThreshold,errors);
            else if(TryPrefix(t,"noise_tolerance:",out v))ParseThresh(v,"noise_tolerance",ref schema.NoiseTolerance,errors);
            else if(TryPrefix(t,"safe_mode_breach_count:",out v)){if(int.TryParse(v,out int n)&&n>0)schema.SafeModeBreachCount=n;else errors.Add(new SloSchemaError.ParseError("safe_mode_breach_count",v));}
            else if(TryPrefix(t,"safe_mode_error_rate:",out v))ParseThresh(v,"safe_mode_error_rate",ref schema.SafeModeErrorRate,errors);
            else if(t=="metrics:")inMetrics=true;
            else if(inMetrics&&t.EndsWith(':')&&!t.StartsWith("max_")&&!t.StartsWith("metric_type")&&!t.StartsWith("safe_mode")){
                if(curName!="")schema.Metrics[curName]=new MetricSlo{ MetricType=curSlo.MetricType,MaxValue=curSlo.MaxValue,MaxRatio=curSlo.MaxRatio,SafeModeTrigger=curSlo.SafeModeTrigger};
                curName=t.TrimEnd(':'); if(!seen.Add(curName))errors.Add(new SloSchemaError.DuplicateMetric(curName));
                curSlo=new MetricSlo();
            }
            else if(TryPrefix(t,"metric_type:",out v))curSlo.MetricType=v switch{"latency"=>MetricType.Latency,"memory"=>MetricType.Memory,"error_rate"=>MetricType.ErrorRate,_=>throw new Exception()};
            else if(TryPrefix(t,"max_value:",out v)){if(double.TryParse(v,out double d))curSlo.MaxValue=d;}
            else if(TryPrefix(t,"max_ratio:",out v)){if(double.TryParse(v,out double d))curSlo.MaxRatio=d;}
            else if(TryPrefix(t,"safe_mode_trigger:",out v))curSlo.SafeModeTrigger=v=="true";
        }
        if(curName!="")schema.Metrics[curName]=curSlo;
        if(schema.NoiseTolerance>=schema.RegressionThreshold)errors.Add(new SloSchemaError.InvalidThreshold("noise_tolerance",schema.NoiseTolerance));
        return errors.Count==0?Result<SloSchema,List<SloSchemaError>>.Ok(schema):Result<SloSchema,List<SloSchemaError>>.Err(errors);
    }

    static bool TryPrefix(string line,string prefix,out string value){value="";if(line.StartsWith(prefix)){value=line[prefix.Length..].Trim();return true;}return false;}
    static void ParseThresh(string v,string field,ref double target,List<SloSchemaError> errors){if(double.TryParse(v,out double d)&&d>=0&&d<=1)target=d;else errors.Add(new SloSchemaError.InvalidThreshold(field,d));}

    public static BreachResult CheckBreach(string metricName,double baseline,double current,SloSchema schema)
    {
        double ratio=baseline>0?current/baseline:1.0;
        var slo=schema.Metrics.GetValueOrDefault(metricName);
        var mt=slo?.MetricType??MetricType.Latency; bool smt=slo?.SafeModeTrigger??false;
        if(slo is {}s){if(s.MaxValue.HasValue&&current>s.MaxValue.Value)return new(metricName,mt,baseline,current,ratio,BreachSeverity.AbsoluteBreach,smt);if(s.MaxRatio.HasValue&&ratio>s.MaxRatio.Value)return new(metricName,mt,baseline,current,ratio,BreachSeverity.Breach,smt);}
        double cp=ratio-1.0;
        BreachSeverity sev=cp>schema.RegressionThreshold?BreachSeverity.Breach:cp>schema.NoiseTolerance?BreachSeverity.Noise:BreachSeverity.None;
        return new(metricName,mt,baseline,current,ratio,sev,smt);
    }

    public static SafeModeDecision CheckSafeMode(List<BreachResult> breaches,SloSchema schema)
    {
        foreach(var b in breaches)if(b.SafeModeTrigger&&(b.Severity==BreachSeverity.Breach||b.Severity==BreachSeverity.AbsoluteBreach))return new SafeModeDecision.Triggered($"metric '{b.MetricName}' breached with safe_mode_trigger=true (ratio={b.Ratio:F3})");
        foreach(var b in breaches)if(b.MetricType==MetricType.ErrorRate&&b.Current>schema.SafeModeErrorRate)return new SafeModeDecision.Triggered($"error rate '{b.MetricName}' at {b.Current:F3} exceeds safe_mode_error_rate {schema.SafeModeErrorRate:F3}");
        int bc=breaches.Count(b=>b.Severity==BreachSeverity.Breach||b.Severity==BreachSeverity.AbsoluteBreach);
        if(bc>=schema.SafeModeBreachCount)return new SafeModeDecision.Triggered($"{bc} simultaneous breaches (threshold: {schema.SafeModeBreachCount})");
        return new SafeModeDecision.Normal();
    }

    public static (List<BreachResult>,SafeModeDecision) RunSloCheck(SloSchema schema,List<(string,double,double)> observations)
    {
        var breaches=observations.Select(o=>CheckBreach(o.Item1,o.Item2,o.Item3,schema)).ToList();
        return(breaches,CheckSafeMode(breaches,schema));
    }
}

public readonly struct Result<T,E>{readonly T? _ok;readonly E? _err;readonly bool _isOk;Result(T ok){_ok=ok;_err=default;_isOk=true;}Result(E err){_err=err;_ok=default;_isOk=false;}public static Result<T,E> Ok(T v)=>new(v);public static Result<T,E> Err(E e)=>new(e);public bool IsOk=>_isOk;public T Unwrap()=>_isOk?_ok!:throw new InvalidOperationException();public E UnwrapErr()=>!_isOk?_err!:throw new InvalidOperationException();}
