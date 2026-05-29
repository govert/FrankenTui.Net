using System.Diagnostics;

namespace FrankenTui.Extras;

public sealed class PtyCaptureConfig
{
    public ushort Cols { get; set; } = 80; public ushort Rows { get; set; } = 24;
    public string? Term { get; set; } = "xterm-256color";
    public List<(string Key, string Value)> Env { get; } = [];
    public PtyCaptureConfig WithSize(ushort c, ushort r) { Cols = c; Rows = r; return this; }
    public PtyCaptureConfig WithTerm(string t) { Term = t; return this; }
    public PtyCaptureConfig WithEnv(string k, string v) { Env.Add((k, v)); return this; }
}

public sealed class PtyCapture : IDisposable
{
    private readonly IPtySession? _nativeSession;
    private readonly Process? _process;
    private readonly StreamWriter? _stdinWriter;
    private readonly MemoryStream _buf = new();
    private readonly object _bufLock = new();
    private bool _eof;
    private bool _disposed;

    public static PtyCapture Spawn(PtyCaptureConfig config, string fileName, string arguments)
    {
        if (PtyNative.IsAvailable)
        {
            try { return new PtyCapture(PtyNative.Spawn(config, fileName, arguments)); }
            catch { }
        }
        return SpawnProcess(config, fileName, arguments);
    }

    private static PtyCapture SpawnProcess(PtyCaptureConfig c, string fn, string args)
    {
        var psi = new ProcessStartInfo { FileName = fn, Arguments = args, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        if (c.Term is not null) psi.EnvironmentVariables["TERM"] = c.Term;
        foreach (var (k, v) in c.Env) psi.EnvironmentVariables[k] = v;
        var p = new Process { StartInfo = psi }; p.Start();
        return new PtyCapture(p, new StreamWriter(p.StandardInput.BaseStream) { AutoFlush = true });
    }

    private PtyCapture(IPtySession s) { _nativeSession = s; }
    private PtyCapture(Process p, StreamWriter w)
    {
        _process = p; _stdinWriter = w;
        byte[] ob = new byte[8192], eb = new byte[8192];
        var o = p.StandardOutput.BaseStream; var e = p.StandardError.BaseStream;
        int oDone = 0, eDone = 0;
        var oThread = new Thread(() => { try { while (true) { int n = o.Read(ob, 0, ob.Length); if (n <= 0) break; lock (_bufLock) _buf.Write(ob, 0, n); } } finally { Interlocked.Exchange(ref oDone, 1); } }) { IsBackground = true };
        var eThread = new Thread(() => { try { while (true) { int n = e.Read(eb, 0, eb.Length); if (n <= 0) break; lock (_bufLock) _buf.Write(eb, 0, n); } } finally { Interlocked.Exchange(ref eDone, 1); } }) { IsBackground = true };
        oThread.Start(); eThread.Start();
        var monitor = new Thread(() =>
        {
            while (oDone == 0 || eDone == 0) { Thread.Sleep(50); }
            lock (_bufLock) _eof = true;
        }) { IsBackground = true };
        monitor.Start();
    }

    public byte[] ReadAvailable() => ReadAvailableWithTimeout(TimeSpan.Zero);
    public byte[] ReadAvailableWithTimeout(TimeSpan timeout)
    {
        if (_nativeSession is not null) return _nativeSession.ReadAvailableWithTimeout(timeout);
        if (timeout > TimeSpan.Zero) Thread.Sleep(timeout);
        lock (_bufLock) { var d = _buf.ToArray(); _buf.SetLength(0); return d; }
    }

    public void SendInput(byte[] d) { if (_disposed || d.Length == 0) return; if (_nativeSession is not null) { _nativeSession.SendInput(d); return; } _stdinWriter?.BaseStream.Write(d, 0, d.Length); _stdinWriter?.Flush(); }
    public int Wait() { if (_nativeSession is not null) return _nativeSession.WaitForExit().ExitCode; _process?.WaitForExit(); return _process?.ExitCode ?? -1; }
    public int? ChildPid => _nativeSession?.ChildPid ?? _process?.Id;
    public bool IsEof => _nativeSession?.IsEof ?? _eof;
    public void Dispose() { if (_disposed) return; _disposed = true; _nativeSession?.Dispose(); if (_process is not null) { try { _stdinWriter?.Dispose(); } catch { } try { if (!_process.HasExited) _process.Kill(); } catch { } _process.Dispose(); } }
}
