using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record OpenTuiConfidenceModel(
    string ModelId,
    string SchemaVersion,
    string ModelVersion,
    OpenTuiDecisionSpace DecisionSpace,
    IReadOnlyDictionary<string, double> LossMatrix,
    IReadOnlyDictionary<string, double> PriorConfig,
    IReadOnlyList<OpenTuiLikelihoodSource> LikelihoodSources,
    OpenTuiDecisionBoundaries DecisionBoundaries,
    OpenTuiCalibrationSettings Calibration,
    IReadOnlyList<OpenTuiFallbackTrigger> FallbackTriggers)
{
    public static OpenTuiConfidenceModel FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<OpenTuiConfidenceModel>(json, HarnessJson.SnakeCase) ??
               throw new InvalidOperationException("Could not deserialize OpenTUI confidence model.");
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!string.Equals(ModelId, "opentui-migration-confidence-model", StringComparison.Ordinal))
        {
            errors.Add("Confidence model id must be opentui-migration-confidence-model.");
        }

        if (!string.Equals(SchemaVersion, "confidence-model-v1", StringComparison.Ordinal))
        {
            errors.Add("Confidence model schema version must be confidence-model-v1.");
        }

        if (LikelihoodSources.Count == 0)
        {
            errors.Add("Confidence model must define likelihood sources.");
        }

        var weightSum = LikelihoodSources.Sum(static source => source.Weight);
        if (Math.Abs(weightSum - 1.0) > 0.0001)
        {
            errors.Add("Likelihood source weights must sum to 1.0.");
        }

        return errors;
    }
}

public sealed record OpenTuiDecisionSpace(
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> States);

public sealed record OpenTuiLikelihoodSource(
    string SourceId,
    double Weight,
    string Description);

public sealed record OpenTuiDecisionBoundaries(
    double AutoApproveThreshold,
    double HumanReviewLower,
    double HumanReviewUpper,
    double RejectThreshold,
    double HardRejectThreshold,
    double RollbackTrigger);

public sealed record OpenTuiCalibrationSettings(
    double CredibleIntervalWidth,
    double ConformalCoverageTarget,
    int MinCalibrationSamples,
    double RecalibrationTriggerDrift);

public sealed record OpenTuiFallbackTrigger(
    string TriggerId,
    string Condition,
    string Action);

public sealed record OpenTuiLicensingProvenanceContract(
    string ContractId,
    string SchemaVersion,
    string ContractVersion,
    OpenTuiLicensingPolicy LicensingPolicy,
    OpenTuiProvenanceChainPolicy ProvenanceChainPolicy,
    IReadOnlyList<string> IpArtifactStatuses,
    IReadOnlyDictionary<string, string> FailSafeDefaults,
    OpenTuiAttributionTemplate AttributionTemplate,
    IReadOnlyList<OpenTuiRiskFlag> RiskFlags)
{
    public static OpenTuiLicensingProvenanceContract FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<OpenTuiLicensingProvenanceContract>(json, HarnessJson.SnakeCase) ??
               throw new InvalidOperationException("Could not deserialize OpenTUI licensing/provenance contract.");
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!string.Equals(ContractId, "opentui-licensing-provenance-guardrails", StringComparison.Ordinal))
        {
            errors.Add("Licensing/provenance contract id must be opentui-licensing-provenance-guardrails.");
        }

        if (!string.Equals(SchemaVersion, "licensing-provenance-v1", StringComparison.Ordinal))
        {
            errors.Add("Licensing/provenance schema version must be licensing-provenance-v1.");
        }

        foreach (var requiredStage in ProvenanceChainPolicy.RequiredStages)
        {
            if (requiredStage.Length == 0)
            {
                errors.Add("Provenance required stages must be non-empty.");
            }
        }

        if (AttributionTemplate.RequiredFields.Count == 0)
        {
            errors.Add("Attribution template must require fields.");
        }

        return errors;
    }
}

public sealed record OpenTuiLicensingPolicy(
    IReadOnlyList<string> AllowedLicenseClasses,
    IReadOnlyList<string> BlockedLicenseClasses,
    string CopyleftBoundaryAction,
    string MissingLicenseAction,
    string AmbiguousLicenseAction,
    IReadOnlyList<OpenTuiLicenseClassDefinition> LicenseClassDefinitions);

public sealed record OpenTuiLicenseClassDefinition(
    string ClassId,
    string Description,
    string RiskLevel);

public sealed record OpenTuiProvenanceChainPolicy(
    IReadOnlyList<string> RequiredStages,
    string HashAlgorithm,
    bool ChainMustBeUnbroken,
    IReadOnlyList<string> EachStageMustRecord,
    bool AttributionRequired);

public sealed record OpenTuiAttributionTemplate(
    string Format,
    IReadOnlyList<string> RequiredFields,
    IReadOnlyList<string> OptionalFields);

public sealed record OpenTuiRiskFlag(
    string FlagId,
    string Severity,
    string Description);

public sealed record OpenTuiContractSet(
    OpenTuiMigrationContractBundle Migration,
    OpenTuiConfidenceModel Confidence,
    OpenTuiLicensingProvenanceContract Licensing)
{
    public static OpenTuiContractSet LoadUpstreamReference() =>
        new(
            OpenTuiMigrationContractBundle.LoadUpstreamReference(),
            OpenTuiConfidenceModel.FromJson(File.ReadAllText(UpstreamReferencePaths.OpenTuiConfidenceModel())),
            OpenTuiLicensingProvenanceContract.FromJson(File.ReadAllText(UpstreamReferencePaths.OpenTuiLicensingContract())));

    public static OpenTuiContractSet? TryLoadUpstreamReference()
    {
        try
        {
            return LoadUpstreamReference();
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(Migration.Validate());
        errors.AddRange(Confidence.Validate());
        errors.AddRange(Licensing.Validate());
        return errors;
    }

    public IReadOnlyDictionary<string, string> WriteArtifacts(string runId)
    {
        var artifacts = new Dictionary<string, string>(Migration.WriteArtifacts(runId), StringComparer.Ordinal);

        var confidencePath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-confidence-model.json");
        File.WriteAllText(confidencePath, JsonSerializer.Serialize(Confidence, HarnessJson.IndentedSnakeCase));
        artifacts["opentui_confidence_model"] = confidencePath;

        var licensingPath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-licensing-provenance.json");
        File.WriteAllText(licensingPath, JsonSerializer.Serialize(Licensing, HarnessJson.IndentedSnakeCase));
        artifacts["opentui_licensing_provenance"] = licensingPath;

        return artifacts;
    }
}

public sealed record OpenTuiGateClauseResult(
    string ClauseId,
    string Status,
    string Detail);

public sealed record OpenTuiContractGateReport(
    string RunId,
    string Verdict,
    IReadOnlyList<string> StagesPassed,
    IReadOnlyList<string> StagesFailed,
    IReadOnlyList<OpenTuiGateClauseResult> ClauseResults,
    IReadOnlyList<string> RiskFlags,
    IReadOnlyList<string> ExecutionTrace)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class OpenTuiContractGate
{
    public static OpenTuiContractGateReport Evaluate(
        string runId,
        OpenTuiContractSet contractSet,
        EvidenceManifest manifest,
        SharedSampleComparisonReport? sampleComparison = null,
        OpenTuiPlannerReport? plannerReport = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(contractSet);
        ArgumentNullException.ThrowIfNull(manifest);

        var stagesPassed = new List<string>();
        var stagesFailed = new List<string>();
        var trace = new List<string>();
        var clauseResults = new List<OpenTuiGateClauseResult>();
        var riskFlags = new List<string>();
        var contractErrors = contractSet.Validate();

        if (contractErrors.Count == 0)
        {
            stagesPassed.Add("load_contracts");
            trace.Add("load_contracts:ok");
        }
        else
        {
            stagesFailed.Add("load_contracts");
            trace.Add($"load_contracts:error:{string.Join(" | ", contractErrors)}");
        }

        var manifestErrors = manifest.Validate();
        if (manifestErrors.Count == 0)
        {
            stagesPassed.Add("validate_manifest");
            trace.Add("validate_manifest:ok");
        }
        else
        {
            stagesFailed.Add("validate_manifest");
            trace.Add($"validate_manifest:error:{string.Join(" | ", manifestErrors)}");
        }

        var covered = manifest.CertificationVerdict.SemanticClauseCoverage.Covered.ToHashSet(StringComparer.Ordinal);
        foreach (var clause in contractSet.Migration.SemanticContract.Clauses)
        {
            var status = covered.Contains(clause.ClauseId) ? "pass" : "fail";
            var detail = covered.Contains(clause.ClauseId)
                ? "Covered by evidence manifest."
                : "Missing from evidence manifest clause coverage.";

            if ((clause.ClauseId is "VT-001" or "VT-002") && sampleComparison is not null)
            {
                status = sampleComparison.IsMatch ? "pass" : "fail";
                detail = sampleComparison.IsMatch
                    ? "Shared sample comparison matches upstream reference."
                    : sampleComparison.DifferenceSummary();
            }

            clauseResults.Add(new OpenTuiGateClauseResult(clause.ClauseId, status, detail));
            trace.Add($"clause:{clause.ClauseId}:{status}");
        }

        if (clauseResults.All(static result => string.Equals(result.Status, "pass", StringComparison.Ordinal)))
        {
            stagesPassed.Add("clause_coverage");
        }
        else
        {
            stagesFailed.Add("clause_coverage");
        }

        if (plannerReport is null)
        {
            stagesFailed.Add("planner_findings");
            trace.Add("planner_findings:missing");
        }
        else
        {
            var actionable = plannerReport.Findings.Count(static finding =>
                string.Equals(finding.Status, "ready", StringComparison.Ordinal) ||
                string.Equals(finding.Status, "review", StringComparison.Ordinal) ||
                string.Equals(finding.Status, "blocked", StringComparison.Ordinal));
            if (actionable == plannerReport.Findings.Count)
            {
                stagesPassed.Add("planner_findings");
                trace.Add($"planner_findings:ok:{plannerReport.Findings.Count}");
            }
            else
            {
                stagesFailed.Add("planner_findings");
                trace.Add($"planner_findings:error:{plannerReport.Findings.Count}");
            }

            if (plannerReport.Findings.Any(static finding => string.Equals(finding.Status, "review", StringComparison.Ordinal)))
            {
                riskFlags.Add("planner_review_required");
            }
        }

        var confidence = ComputeConfidence(contractSet.Confidence, manifest, clauseResults);
        trace.Add($"confidence:{confidence:0.000}");
        if (confidence < contractSet.Confidence.DecisionBoundaries.HumanReviewLower)
        {
            riskFlags.Add("confidence_below_review_threshold");
        }

        if (manifest.CertificationVerdict.TestFailCount > 0)
        {
            riskFlags.Add("manifest_test_failures");
        }

        var criticalFailure = contractSet.Migration.SemanticContract.Clauses
            .Where(static clause => string.Equals(clause.Severity, "critical", StringComparison.Ordinal))
            .Any(clause => clauseResults.Any(result =>
                string.Equals(result.ClauseId, clause.ClauseId, StringComparison.Ordinal) &&
                !string.Equals(result.Status, "pass", StringComparison.Ordinal)));

        if (criticalFailure)
        {
            riskFlags.Add("critical_clause_failure");
        }

        var verdict = criticalFailure || manifestErrors.Count > 0 || contractErrors.Count > 0
            ? "reject"
            : confidence >= contractSet.Confidence.DecisionBoundaries.AutoApproveThreshold &&
              string.Equals(manifest.CertificationVerdict.Verdict, "accept", StringComparison.OrdinalIgnoreCase)
                ? "accept"
                : "hold";

        return new OpenTuiContractGateReport(
            runId,
            verdict,
            stagesPassed,
            stagesFailed,
            clauseResults,
            riskFlags,
            trace);
    }

    public static IReadOnlyDictionary<string, string> WriteArtifacts(string runId, OpenTuiContractGateReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(report);

        var path = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-contract-gate.json");
        File.WriteAllText(path, report.ToJson());
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["opentui_contract_gate"] = path
        };
    }

    private static double ComputeConfidence(
        OpenTuiConfidenceModel confidenceModel,
        EvidenceManifest manifest,
        IReadOnlyList<OpenTuiGateClauseResult> clauseResults)
    {
        var clausePassRate = clauseResults.Count == 0
            ? 0
            : clauseResults.Count(static result => string.Equals(result.Status, "pass", StringComparison.Ordinal)) / (double)clauseResults.Count;
        var benchmarkFactor = manifest.CertificationVerdict.BenchmarkSummary.LatencyP99Ms <= 0
            ? 1.0
            : Math.Min(1.0, 3.0 / Math.Max(manifest.CertificationVerdict.BenchmarkSummary.LatencyP99Ms, 0.001));
        var determinismFactor = manifest.DeterminismAttestation.DivergenceDetected ? 0.2 : 1.0;
        var posterior = clausePassRate * 0.7 + benchmarkFactor * 0.2 + determinismFactor * 0.1;
        return Math.Clamp(posterior, 0, 1);
    }
}
