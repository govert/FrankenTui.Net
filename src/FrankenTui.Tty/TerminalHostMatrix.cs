using FrankenTui.Core;

namespace FrankenTui.Tty;

public static class TerminalHostMatrix
{
    public static IReadOnlyList<TerminalHostProfile> Profiles { get; } =
    [
        new("linux", "unix-tty", TerminalCapabilities.Modern(), "Primary PTY-backed validation target in this workspace."),
        new("macos", "unix-tty", TerminalCapabilities.Modern() with { Profile = TerminalProfile.Custom }, "Contract-compatible Unix host profile; requires external CI for validation."),
        new("windows", "conpty", TerminalCapabilities.WindowsConsole(), "Windows host profile; implementation present but not executable in this Linux workspace.")
    ];

    public static TerminalHostProfile ForCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return Profiles.First(static profile => profile.Platform == "windows");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Profiles.First(static profile => profile.Platform == "macos");
        }

        return Profiles.First(static profile => profile.Platform == "linux");
    }
}
