using FrankenTui.Core;
using FrankenTui.Simd;
using FrankenTui.Tty;

namespace FrankenTui.Doctor;

public static class EnvironmentDoctor
{
    public static DoctorReport CreateReport()
    {
        var capabilities = TerminalCapabilities.Detect();
        var profile = TerminalHostMatrix.ForCurrentPlatform();
        var simd = SimdAccelerators.Capabilities;
        return new DoctorReport(
            OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux",
            Environment.Version.ToString(),
            Environment.GetEnvironmentVariable("TERM") ?? string.Empty,
            profile.Host,
            profile.ValidationStatus,
            simd.IsSupported,
            SimdAccelerators.IsEnabled,
            simd.Summary,
            capabilities.UseHyperlinks(),
            capabilities.UseSyncOutput(),
            capabilities.InAnyMux(),
            [
                profile.Notes,
                $"SIMD acceleration {(SimdAccelerators.IsEnabled ? "enabled" : simd.IsSupported ? "available but disabled" : "unavailable")} with {simd.Summary}.",
                "True OS-level raw-mode parity beyond the current platform requires external CI or host validation.",
                "PTY-backed verification in this repo currently targets Unix hosts through the 'script' command."
            ],
            profile.EvidenceSources,
            profile.KnownDivergences,
            profile.CapabilityOverrides,
            [
                "Use the hosted parity showcase in both terminal and web modes before widening the public surface.",
                "Refresh replay, doctor, and CI artifacts together so parity evidence stays comparable."
            ]);
    }
}
