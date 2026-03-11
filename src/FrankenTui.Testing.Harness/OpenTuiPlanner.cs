using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record OpenTuiPlannerRow(
    string ConstructSignature,
    string CategoryId,
    string HandlingClass,
    string PlannerStrategy,
    string FallbackBehavior,
    string RiskLevel);

public sealed record OpenTuiCertificationRow(
    string ConstructSignature,
    string HandlingClass,
    string RiskLevel,
    IReadOnlyList<string> SemanticClauseLinks,
    IReadOnlyList<string> CertificationEvidence,
    string UserMessaging);

public sealed record OpenTuiPlannerFinding(
    string ConstructSignature,
    string Status,
    string Detail,
    string HandlingClass,
    string RiskLevel);

public sealed record OpenTuiPlannerReport(
    string RunId,
    string BasisCommit,
    IReadOnlyList<OpenTuiPlannerRow> PlannerRows,
    IReadOnlyList<OpenTuiCertificationRow> CertificationRows,
    IReadOnlyList<OpenTuiPlannerFinding> Findings,
    IReadOnlyList<string> ExecutionTrace)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class OpenTuiPlanner
{
    public static OpenTuiPlannerReport Build(
        string runId,
        OpenTuiContractSet contractSet,
        EvidenceManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(contractSet);
        ArgumentNullException.ThrowIfNull(manifest);

        var plannerRows = contractSet.Migration.TransformationPolicy.PolicyCells
            .Select(static cell => new OpenTuiPlannerRow(
                cell.ConstructSignature,
                cell.CategoryId,
                cell.HandlingClass,
                cell.PlannerStrategy,
                cell.FallbackBehavior,
                cell.RiskLevel))
            .OrderBy(static row => row.ConstructSignature, StringComparer.Ordinal)
            .ToArray();
        var certificationRows = contractSet.Migration.TransformationPolicy.PolicyCells
            .Select(static cell => new OpenTuiCertificationRow(
                cell.ConstructSignature,
                cell.HandlingClass,
                cell.RiskLevel,
                cell.SemanticClauseLinks,
                cell.CertificationEvidence,
                cell.UserMessaging))
            .OrderBy(static row => row.ConstructSignature, StringComparer.Ordinal)
            .ToArray();

        var coveredClaims = manifest.CertificationVerdict.SemanticClauseCoverage.Covered.ToHashSet(StringComparer.Ordinal);
        var findings = new List<OpenTuiPlannerFinding>(plannerRows.Length);
        var trace = new List<string>(plannerRows.Length + 2)
        {
            $"planner_rows:{plannerRows.Length}",
            $"cert_rows:{certificationRows.Length}"
        };

        foreach (var cell in contractSet.Migration.TransformationPolicy.PolicyCells.OrderBy(static item => item.ConstructSignature, StringComparer.Ordinal))
        {
            var missingClause = cell.SemanticClauseLinks.FirstOrDefault(clauseId => !coveredClaims.Contains(clauseId));
            var status = cell.HandlingClass switch
            {
                "unsupported" => "blocked",
                _ when missingClause is not null => "review",
                _ => "ready"
            };
            var detail = status switch
            {
                "blocked" => $"Construct '{cell.ConstructSignature}' is explicitly unsupported and must use fallback '{cell.FallbackBehavior}'.",
                "review" => $"Missing semantic evidence for clause '{missingClause}'.",
                _ => $"Planner strategy '{cell.PlannerStrategy}' is ready with fallback '{cell.FallbackBehavior}'."
            };

            findings.Add(new OpenTuiPlannerFinding(
                cell.ConstructSignature,
                status,
                detail,
                cell.HandlingClass,
                cell.RiskLevel));
            trace.Add($"finding:{cell.ConstructSignature}:{status}");
        }

        return new OpenTuiPlannerReport(
            runId,
            UpstreamReferencePaths.BasisCommit,
            plannerRows,
            certificationRows,
            findings,
            trace);
    }

    public static IReadOnlyDictionary<string, string> WriteArtifacts(string runId, OpenTuiPlannerReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(report);

        var artifacts = new Dictionary<string, string>(StringComparer.Ordinal);

        var plannerPath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-planner-report.json");
        File.WriteAllText(plannerPath, report.ToJson());
        artifacts["opentui_planner_report"] = plannerPath;

        var findingsPath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-planner-findings.json");
        File.WriteAllText(findingsPath, JsonSerializer.Serialize(report.Findings, HarnessJson.IndentedSnakeCase));
        artifacts["opentui_planner_findings"] = findingsPath;

        return artifacts;
    }
}
