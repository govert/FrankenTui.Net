// Unix PTY implementation using posix_openpt, fork/exec, and poll.
// Windows: this file is not compiled (no #if UNIX — use runtime check).

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FrankenTui.Extras;

internal sealed class UnixPtySession : IPtySession
{
    private readonly int _masterFd;
    private readonly int _childPid;
    private readonly Thread _readerThread;
    private bool _eof;
    private readonly MemoryStream _readBuffer = new();
    private readonly object _readLock = new();
    private bool _disposed;

    private UnixPtySession(int masterFd, int childPid)
    {
        _masterFd = masterFd;
        _childPid = childPid;

        _readerThread = new Thread(ReaderLoop)
        {
            Name = "ftui-pty-reader",
            IsBackground = true
        };
        _readerThread.Start();
    }

    public static unsafe UnixPtySession Spawn(PtyCaptureConfig config, string fileName, string arguments)
    {
        // Open PTY master
        int master = posix_openpt(O_RDWR | O_CLOEXEC);
        if (master < 0) throw new InvalidOperationException($"posix_openpt failed: errno={Marshal.GetLastPInvokeError()}");

        // Grant access and unlock
        if (grantpt(master) < 0) { close(master); throw new InvalidOperationException($"grantpt failed: errno={Marshal.GetLastPInvokeError()}"); }
        if (unlockpt(master) < 0) { close(master); throw new InvalidOperationException($"unlockpt failed: errno={Marshal.GetLastPInvokeError()}"); }

        // Get slave name
        byte* nameBuf = stackalloc byte[1024];
        if (ptsname_r(master, nameBuf, 1024) != 0) { close(master); throw new InvalidOperationException($"ptsname_r failed: errno={Marshal.GetLastPInvokeError()}"); }
        string slaveName = Marshal.PtrToStringUTF8((IntPtr)nameBuf) ?? "";

        // Fork
        int pid = fork();
        if (pid < 0) { close(master); throw new InvalidOperationException($"fork failed: errno={Marshal.GetLastPInvokeError()}"); }

        if (pid == 0)
        {
            // Child process
            try
            {
                // Set TERM
                string? term = config.Term ?? "xterm-256color";
                setenv("TERM", term, 1);

                // Set extra env
                foreach (var (key, value) in config.Env)
                    setenv(key, value, 1);

                // Open slave PTY
                int slave = open(slaveName, O_RDWR | O_CLOEXEC);
                if (slave < 0) _exit(1);

                // Duplicate slave to stdin/stdout/stderr
                dup2(slave, 0);
                dup2(slave, 1);
                dup2(slave, 2);

                // Close master in child
                close(master);
                if (slave > 2) close(slave);

                // Set window size
                var ws = new winsize
                {
                    ws_col = config.Cols,
                    ws_row = config.Rows,
                };
                ioctl(0, TIOCSWINSZ, &ws);

                // Execute the command
                var cmd = $"{fileName} {arguments}";
                execl("/bin/sh", "sh", "-c", cmd, (void*)0);

                // If exec returns, it failed
                _exit(1);
            }
            catch
            {
                _exit(1);
            }
        }

        // Parent: close slave, return
        return new UnixPtySession(master, pid);
    }

    private unsafe void ReaderLoop()
    {
        byte[] buf = new byte[8192];
        var pfd = new pollfd { fd = _masterFd, events = POLLIN };

        while (!_disposed)
        {
            int ret;
            unsafe { ret = poll(&pfd, 1, 100); }

            if (ret < 0) break; // error
            if (ret == 0) continue; // timeout

            int n = read(_masterFd, buf, buf.Length);
            if (n <= 0)
            {
                lock (_readLock) _eof = true;
                break;
            }

            lock (_readLock)
            {
                _readBuffer.Write(buf, 0, n);
            }
        }
    }

    public byte[] ReadAvailable()
    {
        lock (_readLock)
        {
            if (_eof && _readBuffer.Length == 0) return [];
            var data = _readBuffer.ToArray();
            _readBuffer.SetLength(0);
            return data;
        }
    }

    public byte[] ReadAvailableWithTimeout(TimeSpan timeout)
    {
        // Wait a bit for initial data
        if (timeout > TimeSpan.Zero)
            Thread.Sleep(timeout);
        return ReadAvailable();
    }

    public void SendInput(byte[] data)
    {
        if (_disposed || data.Length == 0) return;
        write(_masterFd, data, data.Length);
    }

    public PtyExitStatus WaitForExit()
    {
        int status;
        int ret;
        do
        {
            ret = waitpid(_childPid, out status, 0);
        } while (ret < 0 && Marshal.GetLastPInvokeError() == 4); // EINTR

        if (ret < 0)
            return new PtyExitStatus(-1, false, 0);

        if (WIFEXITED(status))
            return new PtyExitStatus(WEXITSTATUS(status), false, 0);
        if (WIFSIGNALED(status))
            return new PtyExitStatus(128 + WTERMSIG(status), true, WTERMSIG(status));

        return new PtyExitStatus(-1, false, 0);
    }

    public int? ChildPid => _disposed ? null : _childPid;
    public bool IsEof => _eof;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        close(_masterFd);
    }

    // ===== P/Invoke declarations =====
    private const int O_RDWR = 2;
    private const int O_CLOEXEC = 0x80000;
    private const int POLLIN = 1;

    [DllImport("libc", SetLastError = true)] private static extern int posix_openpt(int flags);
    [DllImport("libc", SetLastError = true)] private static extern int grantpt(int fd);
    [DllImport("libc", SetLastError = true)] private static extern int unlockpt(int fd);
    [DllImport("libc", SetLastError = true)] private static extern unsafe int ptsname_r(int fd, byte* buf, int len);
    [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags);
    [DllImport("libc", SetLastError = true)] private static extern int close(int fd);
    [DllImport("libc", SetLastError = true)] private static extern int read(int fd, byte[] buf, int count);
    [DllImport("libc", SetLastError = true)] private static extern int write(int fd, byte[] buf, int count);
    [DllImport("libc", SetLastError = true)] private static extern int fork();
    [DllImport("libc", SetLastError = true)] private static extern void _exit(int status);
    [DllImport("libc", SetLastError = true)] private static extern int dup2(int oldfd, int newfd);
    [DllImport("libc", SetLastError = true)] private static extern unsafe int execl(string path, string arg0, string arg1, string arg2, void* arg3);
    [DllImport("libc", SetLastError = true)] private static extern void setenv(string name, string value, int overwrite);
    [DllImport("libc", SetLastError = true)] private static extern int waitpid(int pid, out int status, int options);
    [DllImport("libc", SetLastError = true)] private static extern unsafe int ioctl(int fd, int request, void* argp);

    private unsafe struct pollfd { public int fd; public short events; public short revents; }
    [DllImport("libc", SetLastError = true)] private static extern unsafe int poll(pollfd* fds, int nfds, int timeout);

    private unsafe struct winsize { public ushort ws_row; public ushort ws_col; public ushort ws_xpixel; public ushort ws_ypixel; }
    private const int TIOCSWINSZ = 0x5414;

    private static bool WIFEXITED(int status) => (status & 0x7f) == 0;
    private static int WEXITSTATUS(int status) => (status >> 8) & 0xff;
    private static bool WIFSIGNALED(int status) => (status & 0x7f) != 0 && ((status & 0x7f) != 0x7f) && (status & 0x7f) + 1 >> 1 > 0;
    private static int WTERMSIG(int status) => status & 0x7f;
}
