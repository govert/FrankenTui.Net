// SPDX-License-Identifier: Apache-2.0
// Port of evidence_sink.rs, rough_path.rs, asciicast.rs

namespace FrankenTui.Runtime;

// ── evidence_sink ─────────────────────────────────────────────────────────
public enum EvidenceSinkDestination{Stdout,File}
public sealed class EvidenceSinkConfig
{
    public bool Enabled; public EvidenceSinkDestination Destination=EvidenceSinkDestination.Stdout; public string? FilePath; public bool FlushOnWrite=true; public ulong MaxBytes=50*1024*1024;
    public static EvidenceSinkConfig EnabledStdout()=>new(){Enabled=true};
}

public sealed class EvidenceSink : IDisposable
{
    readonly EvidenceSinkConfig _c; readonly object _lk=new(); StreamWriter? _w; ulong _bytes;
    public EvidenceSink(EvidenceSinkConfig c){_c=c;if(c.Enabled){if(c.Destination==EvidenceSinkDestination.Stdout)_w=new StreamWriter(Console.OpenStandardOutput());else if(c.FilePath!=null)_w=new StreamWriter(c.FilePath,true);}}
    public void WriteJsonl(string line){lock(_lk){if(_w==null||_bytes>=_c.MaxBytes)return;_w.WriteLine(line);_bytes+=(ulong)line.Length+1;if(_c.FlushOnWrite)_w.Flush();}}
    public void Dispose(){lock(_lk){_w?.Dispose();_w=null;}}
}

// ── rough_path ────────────────────────────────────────────────────────────
public sealed class RoughPathSignature
{
    readonly double[] _sig; public RoughPathSignature(double[] sig){_sig=sig;} public double[] Values=>_sig; public int Length=>_sig.Length;
    public static RoughPathSignature Compute(double[] values,int depth=3)
    {
        var sig=new double[depth]; double acc=0;for(int i=0;i<Math.Min(depth,values.Length);i++){acc+=values[i];sig[i]=acc/(i+1);}return new(sig);
    }
    public double Distance(RoughPathSignature other){double d=0;for(int i=0;i<Math.Min(_sig.Length,other._sig.Length);i++){double diff=_sig[i]-other._sig[i];d+=diff*diff;}return Math.Sqrt(d);}
}

// ── asciicast ─────────────────────────────────────────────────────────────
public sealed class AsciicastRecorder
{
    readonly List<(double,string)> _frames=new();double _start;
    public AsciicastRecorder(){_start=DateTime.UtcNow.TimeOfDay.TotalSeconds;}
    public void Record(string content){_frames.Add((DateTime.UtcNow.TimeOfDay.TotalSeconds-_start,content));}
    public string ToAsciicast(int width=80,int height=24)
    {
        var sb=new System.Text.StringBuilder();sb.AppendLine($"{{\"version\":2,\"width\":{width},\"height\":{height}}}");
        foreach(var(t,c)in _frames){sb.AppendLine($"[{t:F6},\"o\",\"{Escape(c)}\"]");}return sb.ToString();
    }
    static string Escape(string s)=>s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","\\r");
}
