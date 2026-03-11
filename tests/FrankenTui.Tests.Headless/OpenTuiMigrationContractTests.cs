using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class OpenTuiMigrationContractTests
{
    [Fact]
    public void OpenTuiContractBundleLoadsAndValidatesUpstreamReference()
    {
        var bundle = OpenTuiMigrationContractBundle.LoadUpstreamReference();
        var summary = bundle.ToSummary();

        Assert.Empty(bundle.Validate());
        Assert.Equal("ready", summary.Status);
        Assert.True(summary.CoverageComplete);
        Assert.True(summary.ClauseTraceability);
        Assert.True(summary.ClauseCount > 0);
        Assert.True(summary.PolicyCellCount > 0);
    }

    [Fact]
    public void OpenTuiContractGateAcceptsCoveredManifest()
    {
        var contractSet = OpenTuiContractSet.LoadUpstreamReference();
        var manifest = CreateManifest(contractSet.Migration.SemanticContract.Clauses.Select(static clause => clause.ClauseId).ToArray());

        var report = OpenTuiContractGate.Evaluate("gate-test", contractSet, manifest);

        Assert.Equal("accept", report.Verdict);
        Assert.DoesNotContain(report.ClauseResults, static result => result.Status == "fail");
        Assert.Contains("load_contracts", report.StagesPassed);
        Assert.Contains("validate_manifest", report.StagesPassed);
    }

    [Fact]
    public void OpenTuiContractGateRejectsCriticalClauseFailure()
    {
        var contractSet = OpenTuiContractSet.LoadUpstreamReference();
        var manifest = CreateManifest(["EO-001"]);

        var report = OpenTuiContractGate.Evaluate("gate-fail", contractSet, manifest);

        Assert.Equal("reject", report.Verdict);
        Assert.Contains("critical_clause_failure", report.RiskFlags);
        Assert.Contains(report.ClauseResults, static result => result.ClauseId == "ST-001" && result.Status == "fail");
    }

    private static EvidenceManifest CreateManifest(IReadOnlyList<string> coveredClaims)
    {
        var stages = coveredClaims
            .Select((claimId, index) => new EvidenceStage(
                $"stage-{index}",
                index,
                $"run:test:stage:{index}",
                claimId,
                $"evidence:{index}",
                "policy:test",
                "trace:test",
                "2026-03-11T00:00:00Z",
                "2026-03-11T00:00:00Z",
                "ok",
                index == 0 ? "sha256:seed" : $"sha256:out-{index - 1}",
                $"sha256:out-{index}",
                [],
                null))
            .ToArray();

        return new EvidenceManifest(
            "opentui-migration-evidence-manifest",
            "evidence-manifest-v1",
            "2026-03-11",
            "gate-test",
            new EvidenceSourceFingerprint(
                "https://example.invalid/repo",
                "deadbeef",
                "/tmp/repo",
                "sha256:source",
                [],
                new Dictionary<string, string>(StringComparer.Ordinal)),
            stages,
            new EvidenceGeneratedCodeFingerprint("sha256:code", ".NET SDK 10.0.103", "dotnet build"),
            new EvidenceCertificationVerdict(
                "accept",
                0.98,
                12,
                0,
                0,
                new EvidenceSemanticClauseCoverage(coveredClaims, []),
                new EvidenceBenchmarkSummary(0.5, 1.0, 1200),
                []),
            new EvidenceDeterminismAttestation(2, true, false));
    }
}
