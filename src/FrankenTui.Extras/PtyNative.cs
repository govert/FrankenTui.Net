// Native PTY support using platform-specific APIs.
// Shared types and factory for PtyNative backend.

using System.Runtime.InteropServices;

namespace FrankenTui.Extras;

public readonly record struct PtyExitStatus(int ExitCode, bool Signaled, int Signal)
{
    public bool Success => ExitCode == 0 && !Signaled;
}

public interface IPtySession : IDisposable
{
    byte[] ReadAvailable();
    byte[] ReadAvailableWithTimeout(TimeSpan timeout);
    void SendInput(byte[] data);
    PtyExitStatus WaitForExit();
    int? ChildPid { get; }
    bool IsEof { get; }
}

public static class PtyNative
{
    public static IPtySession Spawn(PtyCaptureConfig config, string fileName, string arguments)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return UnixPtySession.Spawn(config, fileName, arguments);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try { return WindowsPtySession.Spawn(config, fileName, arguments); }
            catch (Exception ex) { throw new PlatformNotSupportedException($"Windows ConPTY not available: {ex.Message}", ex); }
        }
        throw new PlatformNotSupportedException("PTY not supported on this platform");
    }

    public static bool IsAvailable =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
