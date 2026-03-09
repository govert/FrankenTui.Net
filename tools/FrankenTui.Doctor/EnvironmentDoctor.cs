using FrankenTui.Core;
using FrankenTui.Tty;

namespace FrankenTui.Doctor;

public static class EnvironmentDoctor
{
    public static DoctorReport CreateReport()
    {
        var capabilities = TerminalCapabilities.Detect();
        var profile = TerminalHostMatrix.ForCurrentPlatform();
        return new DoctorReport(
            OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux",
            Environment.Version.ToString(),
            Environment.GetEnvironmentVariable("TERM") ?? string.Empty,
            profile.Host,
            capabilities.UseHyperlinks(),
            capabilities.UseSyncOutput(),
            capabilities.InAnyMux(),
            [
                "True OS-level raw-mode parity beyond the current platform requires external CI or host validation.",
                "PTY-backed verification in this repo currently targets Unix hosts through the 'script' command."
            ]);
    }
}
