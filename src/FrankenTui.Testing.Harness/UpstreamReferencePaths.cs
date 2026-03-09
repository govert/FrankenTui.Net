namespace FrankenTui.Testing.Harness;

public static class UpstreamReferencePaths
{
    public const string BasisCommit = "7a91089366bd4644e086d5a422cb76b052e3de17";

    public static string FindUpstreamRoot()
    {
        var root = RepositoryPaths.FindRepositoryRoot();
        var upstream = Path.Combine(root, ".external", "frankentui");
        if (!Directory.Exists(upstream))
        {
            throw new DirectoryNotFoundException(
                $"Expected upstream reference workspace at '{upstream}'. Recreate it using docs/EXTERNALS.md.");
        }

        return upstream;
    }

    public static string BenchmarkBaseline() =>
        Path.Combine(FindUpstreamRoot(), "tests", "baseline.json");

    public static string EvidenceManifestSpec() =>
        Path.Combine(FindUpstreamRoot(), "docs", "spec", "opentui-evidence-manifest.md");

    public static string EvidenceManifestContractSample() =>
        Path.Combine(FindUpstreamRoot(), "crates", "doctor_frankentui", "contracts", "opentui_evidence_manifest_v1.json");

    public static string RuntimeDeterministicReplayTest() =>
        Path.Combine(FindUpstreamRoot(), "crates", "ftui-runtime", "tests", "deterministic_replay.rs");

    public static string WebStepProgramTest() =>
        Path.Combine(FindUpstreamRoot(), "crates", "ftui-web", "tests", "wasm_step_program.rs");
}
