using FrankenTui.Extras;
using FrankenTui.Runtime;
using FrankenTui.Testing.Harness;

namespace FrankenTui.Doctor;

public sealed record DoctorReport(
    string OperatingSystem,
    string RuntimeVersion,
    string Term,
    string HostProfile,
    string HostValidationStatus,
    bool SimdSupported,
    bool SimdEnabled,
    string SimdSummary,
    bool SupportsHyperlinks,
    bool SupportsSyncOutput,
    bool InMux,
    IReadOnlyList<string> Notes,
    TelemetryConfigSummary? Telemetry = null,
    MermaidConfigSummary? Mermaid = null,
    OpenTuiMigrationContractSummary? OpenTuiMigration = null,
    IReadOnlyList<string>? HostEvidenceSources = null,
    IReadOnlyList<string>? KnownHostDivergences = null,
    IReadOnlyList<string>? CapabilityOverrides = null,
    IReadOnlyList<string>? Recommendations = null,
    IReadOnlyDictionary<string, string>? ArtifactPaths = null);
