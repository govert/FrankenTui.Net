using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record OpenTuiSemanticEquivalenceContract(
    string ContractId,
    string SchemaVersion,
    string ContractVersion,
    IReadOnlyDictionary<string, string> EquivalenceAxes,
    OpenTuiVisualTolerancePolicy VisualTolerancePolicy,
    OpenTuiImprovementEnvelope ImprovementEnvelope,
    IReadOnlyList<OpenTuiTieBreakerRule> DeterministicTieBreakers,
    IReadOnlyList<OpenTuiSemanticClause> Clauses,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ValidatorClauseMap)
{
    public static OpenTuiSemanticEquivalenceContract FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<OpenTuiSemanticEquivalenceContract>(json, HarnessJson.SnakeCase) ??
               throw new InvalidOperationException("Could not deserialize OpenTUI semantic equivalence contract.");
    }

    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!string.Equals(ContractId, "opentui-migration-semantic-equivalence", StringComparison.Ordinal))
        {
            errors.Add("Semantic contract id must be opentui-migration-semantic-equivalence.");
        }

        if (!string.Equals(SchemaVersion, "sem-eq-contract-v1", StringComparison.Ordinal))
        {
            errors.Add("Semantic contract schema version must be sem-eq-contract-v1.");
        }

        foreach (var axis in new[] { "state_transition", "event_ordering", "side_effect_observability" })
        {
            if (!EquivalenceAxes.ContainsKey(axis))
            {
                errors.Add($"Semantic equivalence axis '{axis}' is missing.");
            }
        }

        var clauseIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var clause in Clauses)
        {
            if (!clauseIds.Add(clause.ClauseId))
            {
                errors.Add($"Semantic clause '{clause.ClauseId}' must be unique.");
            }
        }

        var priorities = DeterministicTieBreakers.Select(static rule => rule.Priority).ToArray();
        if (!priorities.SequenceEqual(priorities.OrderBy(static priority => priority)))
        {
            errors.Add("Deterministic tie breakers must be sorted by priority.");
        }

        foreach (var mapping in ValidatorClauseMap)
        {
            foreach (var clauseId in mapping.Value)
            {
                if (!clauseIds.Contains(clauseId))
                {
                    errors.Add($"Validator '{mapping.Key}' references unknown clause '{clauseId}'.");
                }
            }
        }

        return errors;
    }
}

public sealed record OpenTuiVisualTolerancePolicy(
    IReadOnlyList<string> StrictClasses,
    string StrictPolicy,
    IReadOnlyList<string> PerceptualClasses,
    string PerceptualPolicy,
    double MaxPerceptualDelta);

public sealed record OpenTuiImprovementEnvelope(
    IReadOnlyList<string> AllowedDimensions,
    IReadOnlyList<string> ForbiddenRewrites,
    IReadOnlyList<string> RequiredSafeguards);

public sealed record OpenTuiTieBreakerRule(
    int Priority,
    string RuleId,
    string Description);

public sealed record OpenTuiSemanticClause(
    string ClauseId,
    string Title,
    string Category,
    string Requirement,
    string Severity);

public sealed record OpenTuiTransformationPolicyMatrix(
    string PolicyId,
    string SchemaVersion,
    string PolicyVersion,
    IReadOnlyList<OpenTuiPolicyCategory> Categories,
    IReadOnlyList<OpenTuiConstructCatalogEntry> ConstructCatalog,
    IReadOnlyList<OpenTuiPolicyCell> PolicyCells,
    OpenTuiPlannerProjection PlannerProjection,
    OpenTuiCertificationProjection CertificationProjection)
{
    public static OpenTuiTransformationPolicyMatrix FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<OpenTuiTransformationPolicyMatrix>(json, HarnessJson.SnakeCase) ??
               throw new InvalidOperationException("Could not deserialize OpenTUI transformation policy matrix.");
    }

    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public IReadOnlyList<string> Validate(OpenTuiSemanticEquivalenceContract semanticContract)
    {
        ArgumentNullException.ThrowIfNull(semanticContract);

        var errors = new List<string>();
        if (!string.Equals(PolicyId, "opentui-transform-policy-matrix", StringComparison.Ordinal))
        {
            errors.Add("Transformation policy id must be opentui-transform-policy-matrix.");
        }

        if (!string.Equals(SchemaVersion, "transform-policy-v1", StringComparison.Ordinal))
        {
            errors.Add("Transformation policy schema version must be transform-policy-v1.");
        }

        var categoryIds = Categories.Select(static category => category.CategoryId).ToHashSet(StringComparer.Ordinal);
        var constructIds = ConstructCatalog.Select(static entry => entry.ConstructSignature).ToArray();
        var cellIds = PolicyCells.Select(static cell => cell.ConstructSignature).ToArray();
        var semanticClauseIds = semanticContract.Clauses.Select(static clause => clause.ClauseId).ToHashSet(StringComparer.Ordinal);
        var requiredRiskLevels = CertificationProjection.RequiredRiskLevels.ToHashSet(StringComparer.Ordinal);

        foreach (var construct in ConstructCatalog)
        {
            if (!categoryIds.Contains(construct.CategoryId))
            {
                errors.Add($"Construct '{construct.ConstructSignature}' references unknown category '{construct.CategoryId}'.");
            }
        }

        foreach (var missingConstruct in constructIds.Except(cellIds, StringComparer.Ordinal))
        {
            errors.Add($"Construct '{missingConstruct}' is missing from policy cells.");
        }

        foreach (var extraCell in cellIds.Except(constructIds, StringComparer.Ordinal))
        {
            errors.Add($"Policy cell '{extraCell}' is not present in the construct catalog.");
        }

        if (!cellIds.SequenceEqual(cellIds.OrderBy(static cell => cell, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            errors.Add("Policy cells must be sorted by construct_signature.");
        }

        foreach (var cell in PolicyCells)
        {
            if (!requiredRiskLevels.Contains(cell.RiskLevel))
            {
                errors.Add($"Policy cell '{cell.ConstructSignature}' uses unsupported risk level '{cell.RiskLevel}'.");
            }

            foreach (var clauseId in cell.SemanticClauseLinks)
            {
                if (!semanticClauseIds.Contains(clauseId))
                {
                    errors.Add($"Policy cell '{cell.ConstructSignature}' references unknown clause '{clauseId}'.");
                }
            }

            foreach (var field in PlannerProjection.RequiredFields)
            {
                if (!HasField(cell, field))
                {
                    errors.Add($"Planner projection field '{field}' is missing for '{cell.ConstructSignature}'.");
                }
            }

            foreach (var field in CertificationProjection.RequiredFields)
            {
                if (!HasField(cell, field))
                {
                    errors.Add($"Certification projection field '{field}' is missing for '{cell.ConstructSignature}'.");
                }
            }

            if (CertificationProjection.RequiresClauseTraceability && cell.SemanticClauseLinks.Count == 0)
            {
                errors.Add($"Policy cell '{cell.ConstructSignature}' must include semantic clause traceability.");
            }
        }

        return errors;
    }

    private static bool HasField(OpenTuiPolicyCell cell, string field) =>
        field switch
        {
            "construct_signature" => !string.IsNullOrWhiteSpace(cell.ConstructSignature),
            "category_id" => !string.IsNullOrWhiteSpace(cell.CategoryId),
            "handling_class" => !string.IsNullOrWhiteSpace(cell.HandlingClass),
            "planner_strategy" => !string.IsNullOrWhiteSpace(cell.PlannerStrategy),
            "fallback_behavior" => !string.IsNullOrWhiteSpace(cell.FallbackBehavior),
            "risk_level" => !string.IsNullOrWhiteSpace(cell.RiskLevel),
            "semantic_clause_links" => cell.SemanticClauseLinks.Count > 0,
            "certification_evidence" => cell.CertificationEvidence.Count > 0,
            "user_messaging" => !string.IsNullOrWhiteSpace(cell.UserMessaging),
            _ => false
        };
}

public sealed record OpenTuiPolicyCategory(
    string CategoryId,
    string Description);

public sealed record OpenTuiConstructCatalogEntry(
    string ConstructSignature,
    string CategoryId,
    string Summary);

public sealed record OpenTuiPolicyCell(
    string ConstructSignature,
    string HandlingClass,
    string Rationale,
    string RiskLevel,
    string FallbackBehavior,
    string UserMessaging,
    string PlannerStrategy,
    IReadOnlyList<string> SemanticClauseLinks,
    IReadOnlyList<string> CertificationEvidence)
{
    public string CategoryId =>
        ConstructSignature.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() switch
        {
            "term" => "terminal_capability",
            var category when !string.IsNullOrWhiteSpace(category) => category,
            _ => string.Empty
        };
}

public sealed record OpenTuiPlannerProjection(
    IReadOnlyList<string> RequiredFields,
    string DeterministicSortKey);

public sealed record OpenTuiCertificationProjection(
    IReadOnlyList<string> RequiredFields,
    IReadOnlyList<string> RequiredRiskLevels,
    bool RequiresClauseTraceability);

public sealed record OpenTuiMigrationContractBundle(
    OpenTuiSemanticEquivalenceContract SemanticContract,
    OpenTuiTransformationPolicyMatrix TransformationPolicy)
{
    public static OpenTuiMigrationContractBundle LoadUpstreamReference()
    {
        var semanticPath = UpstreamReferencePaths.OpenTuiSemanticContract();
        var policyPath = UpstreamReferencePaths.OpenTuiTransformationPolicy();
        return new OpenTuiMigrationContractBundle(
            OpenTuiSemanticEquivalenceContract.FromJson(File.ReadAllText(semanticPath)),
            OpenTuiTransformationPolicyMatrix.FromJson(File.ReadAllText(policyPath)));
    }

    public static OpenTuiMigrationContractBundle? TryLoadUpstreamReference()
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

    public OpenTuiMigrationContractSummary ToSummary()
    {
        var issues = Validate();
        var constructIds = TransformationPolicy.ConstructCatalog.Select(static entry => entry.ConstructSignature).ToHashSet(StringComparer.Ordinal);
        var cellIds = TransformationPolicy.PolicyCells.Select(static cell => cell.ConstructSignature).ToHashSet(StringComparer.Ordinal);
        return new OpenTuiMigrationContractSummary(
            issues.Count == 0 ? "ready" : "invalid",
            UpstreamReferencePaths.BasisCommit,
            SemanticContract.Clauses.Count,
            TransformationPolicy.PolicyCells.Count,
            constructIds.SetEquals(cellIds),
            TransformationPolicy.PolicyCells.All(static cell => cell.SemanticClauseLinks.Count > 0),
            issues);
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        errors.AddRange(SemanticContract.Validate());
        errors.AddRange(TransformationPolicy.Validate(SemanticContract));
        return errors;
    }

    public IReadOnlyDictionary<string, string> WriteArtifacts(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        var artifacts = new Dictionary<string, string>(StringComparer.Ordinal);

        var semanticPath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-semantic-equivalence.json");
        File.WriteAllText(semanticPath, SemanticContract.ToJson());
        artifacts["opentui_semantic_contract"] = semanticPath;

        var policyPath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-transformation-policy.json");
        File.WriteAllText(policyPath, TransformationPolicy.ToJson());
        artifacts["opentui_transformation_policy"] = policyPath;

        var summaryPath = ArtifactPathBuilder.For("contracts", $"{runId}-opentui-contract-summary.json");
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(ToSummary(), HarnessJson.IndentedSnakeCase));
        artifacts["opentui_contract_summary"] = summaryPath;

        return artifacts;
    }
}

public sealed record OpenTuiMigrationContractSummary(
    string Status,
    string? BasisCommit,
    int ClauseCount,
    int PolicyCellCount,
    bool CoverageComplete,
    bool ClauseTraceability,
    IReadOnlyList<string> Issues)
{
    public static OpenTuiMigrationContractSummary Missing(string reason) =>
        new(
            "missing",
            null,
            0,
            0,
            false,
            false,
            [reason]);
}
