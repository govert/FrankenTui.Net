using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Runtime;
using FrankenTui.Simd;
using FrankenTui.Testing.Harness;
using FrankenTui.Tty;

namespace FrankenTui.Doctor;

public static class EnvironmentDoctor
{
    public static DoctorReport CreateReport(
        TelemetryConfigSummary? telemetry = null,
        MermaidConfigSummary? mermaid = null,
        OpenTuiMigrationContractSummary? openTuiMigration = null)
    {
        var capabilities = TerminalCapabilities.Detect();
        var profile = TerminalHostMatrix.ForCurrentPlatform();
        var simd = SimdAccelerators.Capabilities;
        telemetry ??= TelemetryConfig.FromEnvironment().ToSummary();
        mermaid ??= MermaidShowcaseSurface.CreateSummary();
        openTuiMigration ??= OpenTuiMigrationContractBundle.TryLoadUpstreamReference()?.ToSummary() ??
                             OpenTuiMigrationContractSummary.Missing("Upstream OpenTUI reference contracts are unavailable in .external/frankentui.");
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
                $"Telemetry {telemetry.EnabledReason} via {telemetry.Protocol}{(telemetry.Endpoint is null ? string.Empty : $" @ {telemetry.Endpoint}")}.",
                $"Mermaid contract surface {mermaid.Status} with {mermaid.SampleCount} tracked showcase samples.",
                $"OpenTUI migration contracts {openTuiMigration.Status} with {openTuiMigration.ClauseCount} clauses and {openTuiMigration.PolicyCellCount} policy cells.",
                "True OS-level raw-mode parity beyond the current platform requires external CI or host validation.",
                "PTY-backed verification in this repo currently targets Unix hosts through the 'script' command."
            ],
            telemetry,
            mermaid,
            openTuiMigration,
            profile.EvidenceSources,
            profile.KnownDivergences,
            profile.CapabilityOverrides,
            [
                "Use the hosted parity showcase in both terminal and web modes before widening the public surface.",
                "Refresh replay, doctor, and CI artifacts together so parity evidence stays comparable.",
                "Keep OpenTUI contract validation tied to the managed upstream workspace so contract drift stays explicit."
            ]);
    }
}
