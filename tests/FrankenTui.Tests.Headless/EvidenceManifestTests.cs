using System.Text.Json;
using FrankenTui.Extras;
using FrankenTui.Runtime;
using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class EvidenceManifestTests
{
    [Fact]
    public void HostedParityManifestMatchesUpstreamContractShape()
    {
        var samplePath = UpstreamReferencePaths.EvidenceManifestContractSample();
        var specPath = UpstreamReferencePaths.EvidenceManifestSpec();

        Assert.True(File.Exists(samplePath), $"Missing upstream evidence sample at {samplePath}.");
        Assert.True(File.Exists(specPath), $"Missing upstream evidence spec at {specPath}.");

        var session = HostedParitySession.ForFrame(false, 3, HostedParityScenarioId.Tooling);
        var evidence = RenderHarness.CaptureHostedParity(
            "vrf357-contract",
            HostedParitySurface.Create(session),
            64,
            18,
            options: HostedParitySurface.CreateWebOptions(session));
        var replay = new ReplayTape<string>();
        replay.Add(0, "tooling", [], evidence.Terminal.Text, evidence.Json);
        var benchmarkSuite = new BenchmarkSuiteResult(
        [
            new BenchmarkMeasurement("frame_render", "Frame render", "frame_render", 1000, 1200, 1400, 10)
        ]);

        var result = EvidenceManifestBuilder.WriteHostedParityManifest(
            "vrf357-contract",
            evidence,
            evidence.WriteArtifacts(),
            replay,
            benchmarkSuite);

        var errors = result.Manifest.Validate();
        Assert.Empty(errors);
        Assert.True(File.Exists(result.ArtifactPaths["manifest"]));

        using var actualDocument = JsonDocument.Parse(result.Manifest.ToJson());
        using var sampleDocument = JsonDocument.Parse(File.ReadAllText(samplePath));
        var actualNames = actualDocument.RootElement.EnumerateObject().Select(static property => property.Name).OrderBy(static name => name).ToArray();
        var sampleNames = sampleDocument.RootElement.EnumerateObject().Select(static property => property.Name).OrderBy(static name => name).ToArray();

        Assert.Equal(sampleNames, actualNames);
        Assert.Equal("evidence-manifest-v1", actualDocument.RootElement.GetProperty("schema_version").GetString());
        Assert.Equal(
            UpstreamReferencePaths.BasisCommit,
            actualDocument.RootElement
                .GetProperty("source_fingerprint")
                .GetProperty("parser_versions")
                .GetProperty("frankentui_basis")
                .GetString());
        Assert.NotEmpty(actualDocument.RootElement.GetProperty("stages").EnumerateArray());
        Assert.Contains("Validation Rules", File.ReadAllText(specPath));

        var roundTrip = EvidenceManifest.FromJson(result.Manifest.ToJson());
        Assert.Equal(result.Manifest.SchemaVersion, roundTrip.SchemaVersion);
        Assert.Equal(result.Manifest.SourceFingerprint.SourceHash, roundTrip.SourceFingerprint.SourceHash);
    }
}
