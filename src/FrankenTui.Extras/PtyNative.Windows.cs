// Upstream source: crates/ftui-extras/src/pty_capture.rs
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Windows uses ConPTY plus a monitor-protected buffer in place of
// portable_pty's reader channel; data/EOF wakeups preserve the upstream
// wait-for-first-chunk contract.
//
// Windows ConPTY implementation using CreatePseudoConsole API.
//
// Reader strategy: a background thread does a *blocking* ReadFile on the output
// pipe and appends to an internal buffer; ReadAvailable() just drains that buffer.
// This avoids the PeekNamedPipe polling race (a peek can report 0 bytes and the
// caller can then miss data flushed immediately afterwards).
//
// EOF detection: ConPTY (conhost) keeps the output pipe's write end open even
// after the child process exits — it is only released by ClosePseudoConsole. So a
// child exit alone never breaks the pipe. We therefore watch the child handle on a
// monitor thread; when it exits we ClosePseudoConsole, which flushes any trailing
// output and closes the write end, unblocking the reader's ReadFile with a broken
// pipe so _eof can be raised.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FrankenTui.Extras;

internal sealed class WindowsPtySession : IPtySession
{
    // CreateProcess flags. EXTENDED_STARTUPINFO_PRESENT is 0x00080000 — required
    // for CreateProcess to honour STARTUPINFOEX.lpAttributeList (and thus the
    // pseudoconsole association). CREATE_UNICODE_ENVIRONMENT is 0x00000400.
    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    // PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE (Windows 10 1809+).
    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = (IntPtr)0x20016;
    private const uint INFINITE = 0xFFFFFFFF;
    private const int STD_INPUT_HANDLE = -10, STD_OUTPUT_HANDLE = -11, STD_ERROR_HANDLE = -12;
    // SetStdHandle is process-global; this serializes the brief swap during CreateProcess.
    private static readonly object StdHandleLock = new();

    private readonly IntPtr _hPCON;
    private readonly IntPtr _pipeInputWrite;
    private readonly IntPtr _pipeOutputRead;
    private readonly IntPtr _hProcess;
    private readonly int _childPid;
    private readonly Thread _readerThread;
    private readonly Thread _monitorThread;
    private readonly MemoryStream _readBuffer = new();
    private readonly object _readLock = new();
    private int _pconClosed;
    private bool _eof;
    private bool _disposed;

    private WindowsPtySession(IntPtr hPCON, IntPtr pipeInputWrite, IntPtr pipeOutputRead, IntPtr hProcess, int childPid)
    {
        _hPCON = hPCON; _pipeInputWrite = pipeInputWrite; _pipeOutputRead = pipeOutputRead;
        _hProcess = hProcess; _childPid = childPid;
        _readerThread = new Thread(ReaderLoop) { Name = "ftui-pty-reader", IsBackground = true };
        _readerThread.Start();
        _monitorThread = new Thread(MonitorLoop) { Name = "ftui-pty-monitor", IsBackground = true };
        _monitorThread.Start();
    }

    public static WindowsPtySession Spawn(PtyCaptureConfig config, string fileName, string arguments)
    {
        if (!CreatePipe(out var pipeOutR, out var pipeOutW, IntPtr.Zero, 0))
            throw new InvalidOperationException($"CreatePipe failed for output: {Marshal.GetLastWin32Error()}");
        if (!CreatePipe(out var pipeInR, out var pipeInW, IntPtr.Zero, 0))
        { CloseHandle(pipeOutR); CloseHandle(pipeOutW); throw new InvalidOperationException($"CreatePipe failed for input: {Marshal.GetLastWin32Error()}"); }

        IntPtr hPCON = IntPtr.Zero;
        try
        {
            var size = new COORD { X = (short)config.Cols, Y = (short)config.Rows };
            int hr = CreatePseudoConsole(size, pipeInR, pipeOutW, 0, out hPCON);
            if (hr < 0)
                throw new InvalidOperationException($"CreatePseudoConsole failed: 0x{hr:X8}");

            // ConPTY duplicated the PTY-side handles into conhost; release the
            // parent's copies. Closing pipeOutW here is what lets the output read
            // end eventually observe a broken pipe.
            CloseHandle(pipeInR); pipeInR = IntPtr.Zero;
            CloseHandle(pipeOutW); pipeOutW = IntPtr.Zero;

            nuint attrSize = 0;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attrSize);
            var attrList = Marshal.AllocHGlobal((int)attrSize);
            try
            {
                if (!InitializeProcThreadAttributeList(attrList, 1, 0, ref attrSize))
                    throw new InvalidOperationException($"InitializeProcThreadAttributeList failed: {Marshal.GetLastWin32Error()}");
                if (!UpdateProcThreadAttribute(attrList, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, hPCON, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                    throw new InvalidOperationException($"UpdateProcThreadAttribute failed: {Marshal.GetLastWin32Error()}");

                var env = MakeEnv(config);
                var siEx = new STARTUPINFOEX { StartupInfo = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFOEX>() }, lpAttributeList = attrList };

                // Crucial: route the child's stdio through the pseudoconsole.
                //
                // A process attached via PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE only gets
                // the pseudoconsole's CONIN$/CONOUT$ as its standard handles when the
                // creator has no usable standard handles to hand down. If the parent's
                // std handles are redirected (a test host, a service, a GUI app, or any
                // process whose stdout is piped), the new console session adopts THOSE
                // instead, and the child's output never reaches conhost's screen buffer
                // — so we capture conhost's setup/teardown frames but no program output.
                //
                // The Microsoft ConPTY samples don't hit this because their parent owns a
                // real console. To be correct regardless of how we were launched, blank
                // the parent's std handles for the duration of CreateProcess so the child
                // is forced onto the pseudoconsole, then restore them. SetStdHandle is
                // process-global, so serialize the (microsecond) window across spawns.
                PROCESS_INFORMATION pi;
                bool ok;
                lock (StdHandleLock)
                {
                    var savedIn = GetStdHandle(STD_INPUT_HANDLE);
                    var savedOut = GetStdHandle(STD_OUTPUT_HANDLE);
                    var savedErr = GetStdHandle(STD_ERROR_HANDLE);
                    SetStdHandle(STD_INPUT_HANDLE, IntPtr.Zero);
                    SetStdHandle(STD_OUTPUT_HANDLE, IntPtr.Zero);
                    SetStdHandle(STD_ERROR_HANDLE, IntPtr.Zero);
                    try
                    {
                        ok = CreateProcess(null, $"\"{fileName}\" {arguments}", IntPtr.Zero, IntPtr.Zero, false,
                            EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT, env, null, ref siEx, out pi);
                    }
                    finally
                    {
                        SetStdHandle(STD_INPUT_HANDLE, savedIn);
                        SetStdHandle(STD_OUTPUT_HANDLE, savedOut);
                        SetStdHandle(STD_ERROR_HANDLE, savedErr);
                    }
                }
                if (!ok)
                    throw new InvalidOperationException($"CreateProcess failed: {Marshal.GetLastWin32Error()}");

                CloseHandle(pi.hThread);
                // Keep pi.hProcess for exit-code retrieval and child-exit monitoring.
                return new WindowsPtySession(hPCON, pipeInW, pipeOutR, pi.hProcess, pi.dwProcessId);
            }
            finally { DeleteProcThreadAttributeList(attrList); Marshal.FreeHGlobal(attrList); }
        }
        catch
        {
            if (hPCON != IntPtr.Zero) ClosePseudoConsole(hPCON);
            if (pipeInR != IntPtr.Zero) CloseHandle(pipeInR);
            if (pipeOutW != IntPtr.Zero) CloseHandle(pipeOutW);
            CloseHandle(pipeInW); CloseHandle(pipeOutR);
            throw;
        }
    }

    private static byte[] MakeEnv(PtyCaptureConfig config)
    {
        var vars = new List<string>();
        foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
            vars.Add($"{de.Key}={de.Value}");
        vars.Add($"TERM={config.Term ?? "xterm-256color"}");
        foreach (var (k, v) in config.Env) vars.Add($"{k}={v}");
        var sb = new StringBuilder();
        foreach (var v in vars) { sb.Append(v); sb.Append('\0'); }
        sb.Append('\0');
        return Encoding.Unicode.GetBytes(sb.ToString());
    }

    private unsafe void ReaderLoop()
    {
        byte[] buf = new byte[8192];
        while (true)
        {
            int n;
            fixed (byte* p = buf)
            {
                if (ReadFile(_pipeOutputRead, p, buf.Length, out n, IntPtr.Zero) == 0)
                {
                    // Broken pipe = ConPTY write end closed (child gone + PTY closed). Any
                    // other error also terminates the reader.
                    break;
                }
            }
            if (n <= 0) break; // 0 bytes => write end closed.
            lock (_readLock)
            {
                _readBuffer.Write(buf, 0, n);
                Monitor.PulseAll(_readLock);
            }
        }
        lock (_readLock)
        {
            _eof = true;
            Monitor.PulseAll(_readLock);
        }
    }

    private void MonitorLoop()
    {
        // Wait for the child to exit, then tear down the pseudoconsole so the
        // reader's blocking ReadFile unblocks (after trailing output is flushed).
        WaitForSingleObject(_hProcess, INFINITE);
        CloseConsoleOnce();
    }

    private void CloseConsoleOnce()
    {
        if (Interlocked.Exchange(ref _pconClosed, 1) == 0)
            ClosePseudoConsole(_hPCON);
    }

    public byte[] ReadAvailable()
    {
        lock (_readLock)
            return DrainReadBufferLocked();
    }

    public byte[] ReadAvailableWithTimeout(TimeSpan timeout)
    {
        lock (_readLock)
        {
            if (_readBuffer.Length == 0 && !_eof && timeout > TimeSpan.Zero)
            {
                var elapsed = Stopwatch.StartNew();
                var remaining = timeout;

                while (_readBuffer.Length == 0 && !_eof && remaining > TimeSpan.Zero)
                {
                    if (!Monitor.Wait(_readLock, remaining))
                        break;

                    remaining = timeout - elapsed.Elapsed;
                }
            }

            return DrainReadBufferLocked();
        }
    }

    private byte[] DrainReadBufferLocked()
    {
        if (_readBuffer.Length == 0) return [];
        var data = _readBuffer.ToArray();
        _readBuffer.SetLength(0);
        return data;
    }

    public void SendInput(byte[] data)
    {
        if (_disposed || data.Length == 0) return;
        unsafe { fixed (byte* p = data) WriteFile(_pipeInputWrite, p, data.Length, out _, IntPtr.Zero); }
    }

    public PtyExitStatus WaitForExit()
    {
        WaitForSingleObject(_hProcess, INFINITE);
        int code = GetExitCodeProcess(_hProcess, out uint exit) ? unchecked((int)exit) : -1;
        return new PtyExitStatus(code, false, 0);
    }

    public int? ChildPid => _disposed ? null : _childPid;
    public bool IsEof { get { lock (_readLock) return _eof; } }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Closing the pseudoconsole releases conhost's output-write end, unblocking
        // the reader's ReadFile so the thread can exit before we close its handle.
        CloseConsoleOnce();
        _readerThread.Join(TimeSpan.FromMilliseconds(500));
        CloseHandle(_pipeInputWrite);
        CloseHandle(_pipeOutputRead);
        CloseHandle(_hProcess);
    }

    [DllImport("kernel32.dll", SetLastError = true)] private static extern int CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern int CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPCON);
    [DllImport("kernel32.dll")] private static extern void ClosePseudoConsole(IntPtr hPCON);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern int ResizePseudoConsole(IntPtr hPCON, COORD size);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CreatePipe(out IntPtr hR, out IntPtr hW, IntPtr a, uint s);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool InitializeProcThreadAttributeList(IntPtr l, uint c, uint f, ref nuint s);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UpdateProcThreadAttribute(IntPtr l, uint f, IntPtr a, IntPtr v, IntPtr s, IntPtr p, IntPtr r);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteProcThreadAttributeList(IntPtr l);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CreateProcess(string? app, string cmd, IntPtr pa, IntPtr ta, [MarshalAs(UnmanagedType.Bool)] bool inh, uint f, byte[]? env, string? dir, ref STARTUPINFOEX si, out PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern unsafe int ReadFile(IntPtr h, byte* b, int c, out int r, IntPtr o);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern unsafe int WriteFile(IntPtr h, byte* b, int c, out int r, IntPtr o);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr h, uint ms);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetExitCodeProcess(IntPtr h, out uint code);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetStdHandle(int n);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetStdHandle(int n, IntPtr h);

    private struct COORD { public short X; public short Y; }
    private struct STARTUPINFO { public int cb; public string? lpReserved; public string? lpDesktop; public string? lpTitle; public uint dwX; public uint dwY; public uint dwXSize; public uint dwYSize; public uint dwXCountChars; public uint dwYCountChars; public uint dwFillAttribute; public uint dwFlags; public short wShowWindow; public short cbReserved2; public IntPtr lpReserved2; public IntPtr hStdInput; public IntPtr hStdOutput; public IntPtr hStdError; }
    private struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }
    private struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }
}
