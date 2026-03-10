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
    IReadOnlyList<string>? HostEvidenceSources = null,
    IReadOnlyList<string>? KnownHostDivergences = null,
    IReadOnlyList<string>? CapabilityOverrides = null,
    IReadOnlyList<string>? Recommendations = null,
    IReadOnlyDictionary<string, string>? ArtifactPaths = null);
