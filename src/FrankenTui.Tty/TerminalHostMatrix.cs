using FrankenTui.Core;

namespace FrankenTui.Tty;

public static class TerminalHostMatrix
{
    public static IReadOnlyList<TerminalHostProfile> Profiles { get; } =
    [
        new(
            "linux",
            "unix-tty",
            TerminalCapabilities.Modern(),
            "Primary PTY-backed validation target in this workspace.",
            "validated-local",
            ["headless", "pty", "doctor", "ci-linux"],
            [
                "Raw-mode ownership still flows through the managed backend boundary rather than native termios capture.",
                "Terminal evidence is transcript-based rather than byte-perfect TTY state inspection."
            ],
            ["Prefer modern ANSI features; degrade cleanly when TERM metadata is sparse."]),
        new(
            "macos",
            "unix-tty",
            TerminalCapabilities.Modern() with { Profile = TerminalProfile.Custom },
            "Contract-compatible Unix host profile; requires external CI for validation.",
            "validated-external",
            ["headless", "design-contract", "pending-ci-macos"],
            [
                "Current repo does not execute native macOS PTY runs from the primary Linux workspace.",
                "Capability overrides follow the shared Unix policy until native evidence widens."
            ],
            ["Assume modern ANSI support; verify mux behavior before enabling host-specific upgrades."]),
        new(
            "windows",
            "conpty",
            TerminalCapabilities.WindowsConsole(),
            "Windows host profile; implementation present but not executable in this Linux workspace.",
            "validated-ci",
            ["headless", "ci-windows"],
            [
                "No local ConPTY execution evidence is produced from this Linux workspace.",
                "PTY transcript assertions are currently Unix-only."
            ],
            ["Prefer Windows console capability set and avoid Unix-only toggles unless explicitly supported."])
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

    public static TerminalHostProfile ForPlatform(string platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);

        return Profiles.First(
            profile => string.Equals(profile.Platform, platform, StringComparison.OrdinalIgnoreCase));
    }
}
