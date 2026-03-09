namespace FrankenTui.Doctor;

public sealed record DoctorReport(
    string OperatingSystem,
    string RuntimeVersion,
    string Term,
    string HostProfile,
    bool SupportsHyperlinks,
    bool SupportsSyncOutput,
    bool InMux,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string>? Recommendations = null,
    IReadOnlyDictionary<string, string>? ArtifactPaths = null);
