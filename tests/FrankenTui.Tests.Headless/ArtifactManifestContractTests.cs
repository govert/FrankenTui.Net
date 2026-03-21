using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class ArtifactManifestContractTests
{
    [Fact]
    public void ArtifactManifestValidationDetectsMissingFields()
    {
        var path = ArtifactPathBuilder.For("contracts", "artifact-manifest-test.json");
        File.WriteAllText(path, """{"trace_id":"trace:test","created_at":"2026-03-20T00:00:00Z"}""");

        var entry = ArtifactManifestContract.CreateEntry(path, ArtifactClass.RunMeta);
        var validation = ArtifactManifestContract.ValidateEntry(entry);

        Assert.False(validation.Passes);
        Assert.Contains("status", validation.MissingFields);
        Assert.Contains("runtime_lane", validation.MissingFields);
    }

    [Fact]
    public void ArtifactManifestSummaryClassifiesKnownArtifacts()
    {
        var manifestPath = ArtifactPathBuilder.For("replay", "artifact-summary-manifest.json");
        File.WriteAllText(manifestPath, """{"trace_id":"trace:test","created_at":"2026-03-20T00:00:00Z","status":"ok","runtime_lane":"primary"}""");
        var textPath = ArtifactPathBuilder.For("replay", "artifact-summary.txt");
        File.WriteAllText(textPath, "summary");

        var summary = ArtifactManifestContract.BuildSummary(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["manifest"] = manifestPath,
                ["text"] = textPath
            });

        Assert.Equal(2, summary.EntryCount);
        Assert.True(summary.FailingCount >= 1);
    }
}
