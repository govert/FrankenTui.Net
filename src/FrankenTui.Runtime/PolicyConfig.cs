// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/policy_config.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
//
// Policy-as-data configuration for FrankenTUI decision controllers.
// Captures all tunable parameters across the decision stack.
//
// DIVERGENCE: The managed cascade port names upstream CascadeConfig as
// DegradationCascadeConfig and uses RuntimeConformalConfig internally.

using FrankenTui.Render;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrankenTui.Runtime;

// ── DegradationLevel enum (from ftui_render::budget, now in FrankenTui.Render.Budget) ──

/// <summary>Degradation level for the cascade system.</summary>

public sealed class ConformalPolicyConfig
{
    public double Alpha { get; set; } = 0.05;
    public int MinSamples { get; set; } = 20;
    public int WindowSize { get; set; } = 256;
    public double QDefault { get; set; } = 10_000.0;
}

public sealed class FrameGuardPolicyConfig
{
    public double FallbackBudgetUs { get; set; } = 16_000.0;
    public int TimeSeriesWindow { get; set; } = 512;
    public int NonconformityWindow { get; set; } = 256;
}

public sealed class CascadePolicyConfig
{
    public uint RecoveryThreshold { get; set; } = 10;
    public DegradationLevel MaxDegradation { get; set; } = DegradationLevel.SkipFrame;
    public DegradationLevel MinTriggerLevel { get; set; } = DegradationLevel.SimpleBorders;
    public DegradationLevel DegradationFloor { get; set; } = DegradationLevel.SimpleBorders;
}

public sealed class PidPolicyConfig
{
    public double Kp { get; set; } = 0.5;
    public double Ki { get; set; } = 0.05;
    public double Kd { get; set; } = 0.2;
    public double IntegralMax { get; set; } = 5.0;
}

public sealed class EProcessBudgetPolicyConfig
{
    public double Lambda { get; set; } = 0.5;
    public double Alpha { get; set; } = 0.05;
    public double Beta { get; set; } = 0.5;
    public double SigmaEmaDecay { get; set; } = 0.9;
    public double SigmaFloorMs { get; set; } = 1.0;
    public uint WarmupFrames { get; set; } = 10;
}

public sealed class BocpdPolicyConfig
{
    public double MuSteadyMs { get; set; } = 200.0;
    public double MuBurstMs { get; set; } = 20.0;
    public double HazardLambda { get; set; } = 50.0;
    public int MaxRunLength { get; set; } = 100;
    public double SteadyThreshold { get; set; } = 0.3;
    public double BurstThreshold { get; set; } = 0.7;
    public double BurstPrior { get; set; } = 0.2;
    public double MinObservationMs { get; set; } = 1.0;
    public double MaxObservationMs { get; set; } = 10_000.0;
    public bool EnableLogging { get; set; }
}

public sealed class EProcessThrottlePolicyConfig
{
    public double Alpha { get; set; } = 0.05;
    public double Mu0 { get; set; } = 0.1;
    public double InitialLambda { get; set; } = 0.5;
    public double GrapaEta { get; set; } = 0.1;
    public ulong HardDeadlineMs { get; set; } = 500;
    public ulong MinObservationsBetween { get; set; } = 8;
    public int RateWindowSize { get; set; } = 64;
    public bool EnableLogging { get; set; }
}

public sealed class VoiPolicyConfig
{
    public double Alpha { get; set; } = 0.05;
    public double PriorAlpha { get; set; } = 1.0;
    public double PriorBeta { get; set; } = 1.0;
    public double Mu0 { get; set; } = 0.05;
    public double Lambda { get; set; } = 0.5;
    public double ValueScale { get; set; } = 1.0;
    public double BoundaryWeight { get; set; } = 1.0;
    public double SampleCost { get; set; } = 0.01;
    public ulong MinIntervalMs { get; set; }
    public ulong MaxIntervalMs { get; set; } = 250;
    public ulong MinIntervalEvents { get; set; }
    public ulong MaxIntervalEvents { get; set; } = 20;
    public bool EnableLogging { get; set; }
    public int MaxLogEntries { get; set; } = 2048;
}

public sealed class EvidencePolicyConfig
{
    public int LedgerCapacity { get; set; } = 1024;
    public bool SinkEnabled { get; set; }
    public string? SinkFile { get; set; }
    public bool FlushOnWrite { get; set; } = true;
}

// ── Top-level PolicyConfig ────────────────────────────────────────────────

/// <summary>
/// Top-level policy configuration for the FrankenTUI decision stack.
/// Groups every tunable parameter into a single struct.
/// </summary>
public sealed class PolicyConfig
{
    private const string StandalonePolicyToml = "ftui-policy.toml";
    private const string StandalonePolicyJson = "ftui-policy.json";
    private const string CargoManifestName = "Cargo.toml";

    private static readonly JsonSerializerOptions PolicyJsonOptions = CreatePolicyJsonOptions();

    public ConformalPolicyConfig Conformal { get; set; } = new();
    public FrameGuardPolicyConfig FrameGuard { get; set; } = new();
    public CascadePolicyConfig Cascade { get; set; } = new();
    public PidPolicyConfig Pid { get; set; } = new();
    public EProcessBudgetPolicyConfig EProcessBudget { get; set; } = new();
    public BocpdPolicyConfig Bocpd { get; set; } = new();
    public EProcessThrottlePolicyConfig EProcessThrottle { get; set; } = new();
    public VoiPolicyConfig Voi { get; set; } = new();
    public EvidencePolicyConfig Evidence { get; set; } = new();

    public static PolicyConfig Default => new();

    public static Result<PolicyConfig, PolicyConfigError> FromTomlString(string content)
    {
        try
        {
            PolicyTomlDocument document = PolicyTomlReader.Parse(content, cargoMetadata: false);
            return ValidateLoaded(document.Policy);
        }
        catch (PolicyTomlException error)
        {
            return Result<PolicyConfig, PolicyConfigError>.Err(
                new PolicyConfigError.Toml(error.Message));
        }
    }

    public static Result<PolicyConfig, PolicyConfigError> FromTomlStr(string content) =>
        FromTomlString(content);

    public static Result<PolicyConfig, PolicyConfigError> FromTomlFile(string path) =>
        ReadPolicyFile(path, FromTomlString);

    public static Result<PolicyConfig, PolicyConfigError> FromJsonString(string content)
    {
        try
        {
            PolicyConfig? policy = JsonSerializer.Deserialize<PolicyConfig>(
                content,
                PolicyJsonOptions);
            if (policy is null)
            {
                return Result<PolicyConfig, PolicyConfigError>.Err(
                    new PolicyConfigError.Json("policy config cannot be null"));
            }
            string? missingSection = FindMissingSection(policy);
            if (missingSection is not null)
            {
                return Result<PolicyConfig, PolicyConfigError>.Err(
                    new PolicyConfigError.Json($"policy section cannot be null: {missingSection}"));
            }
            return ValidateLoaded(policy);
        }
        catch (JsonException error)
        {
            return Result<PolicyConfig, PolicyConfigError>.Err(
                new PolicyConfigError.Json(error.Message));
        }
        catch (NotSupportedException error)
        {
            return Result<PolicyConfig, PolicyConfigError>.Err(
                new PolicyConfigError.Json(error.Message));
        }
    }

    public static Result<PolicyConfig, PolicyConfigError> FromJsonStr(string content) =>
        FromJsonString(content);

    public static Result<PolicyConfig, PolicyConfigError> FromJsonFile(string path) =>
        ReadPolicyFile(path, FromJsonString);

    public static Result<PolicyConfig, PolicyConfigError> FromCargoTomlString(string content)
    {
        try
        {
            PolicyTomlDocument document = PolicyTomlReader.Parse(content, cargoMetadata: true);
            if (!document.MetadataSectionFound)
            {
                return Result<PolicyConfig, PolicyConfigError>.Err(
                    new PolicyConfigError.MissingMetadataSection("[package.metadata.ftui]"));
            }
            return ValidateLoaded(document.Policy);
        }
        catch (PolicyTomlException error)
        {
            return Result<PolicyConfig, PolicyConfigError>.Err(
                new PolicyConfigError.Toml(error.Message));
        }
    }

    public static Result<PolicyConfig, PolicyConfigError> FromCargoTomlStr(string content) =>
        FromCargoTomlString(content);

    public static Result<PolicyConfig, PolicyConfigError> FromCargoTomlFile(string path) =>
        ReadPolicyFile(path, FromCargoTomlString);

    /// <summary>
    /// Discover a policy using upstream precedence: standalone TOML, standalone
    /// JSON, then Cargo metadata.
    /// </summary>
    public static Result<PolicyConfig, PolicyConfigError> DiscoverInDirectory(string directory)
    {
        string standaloneToml = Path.Combine(directory, StandalonePolicyToml);
        if (File.Exists(standaloneToml))
            return FromTomlFile(standaloneToml);

        string standaloneJson = Path.Combine(directory, StandalonePolicyJson);
        if (File.Exists(standaloneJson))
            return FromJsonFile(standaloneJson);

        string cargoManifest = Path.Combine(directory, CargoManifestName);
        if (File.Exists(cargoManifest))
            return FromCargoTomlFile(cargoManifest);

        return Result<PolicyConfig, PolicyConfigError>.Err(new PolicyConfigError.Io(
            $"no policy config found in {directory} (expected {StandalonePolicyToml}, {StandalonePolicyJson}, or {CargoManifestName})"));
    }

    public static Result<PolicyConfig, PolicyConfigError> DiscoverInDir(string directory) =>
        DiscoverInDirectory(directory);

    private static Result<PolicyConfig, PolicyConfigError> ReadPolicyFile(
        string path,
        Func<string, Result<PolicyConfig, PolicyConfigError>> parse)
    {
        try
        {
            return parse(File.ReadAllText(path));
        }
        catch (Exception error) when (
            error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return Result<PolicyConfig, PolicyConfigError>.Err(
                new PolicyConfigError.Io(error.Message));
        }
    }

    private static Result<PolicyConfig, PolicyConfigError> ValidateLoaded(PolicyConfig policy)
    {
        List<string> errors = policy.Validate();
        return errors.Count == 0
            ? Result<PolicyConfig, PolicyConfigError>.Ok(policy)
            : Result<PolicyConfig, PolicyConfigError>.Err(
                new PolicyConfigError.Validation(errors));
    }

    private static string? FindMissingSection(PolicyConfig policy)
    {
        if (policy.Conformal is null) return "conformal";
        if (policy.FrameGuard is null) return "frame_guard";
        if (policy.Cascade is null) return "cascade";
        if (policy.Pid is null) return "pid";
        if (policy.EProcessBudget is null) return "eprocess_budget";
        if (policy.Bocpd is null) return "bocpd";
        if (policy.EProcessThrottle is null) return "eprocess_throttle";
        if (policy.Voi is null) return "voi";
        if (policy.Evidence is null) return "evidence";
        return null;
    }

    private static JsonSerializerOptions CreatePolicyJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        options.Converters.Add(new DegradationLevelJsonConverter());
        return options;
    }

    /// <summary>Validate all parameters are within acceptable ranges.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        bool ca = ValidateFinite(errors, "conformal.alpha", Conformal.Alpha);
        _ = ValidateFinite(errors, "conformal.q_default", Conformal.QDefault);
        bool fgfb = ValidateFinite(errors, "frame_guard.fallback_budget_us", FrameGuard.FallbackBudgetUs);
        bool pk = ValidateFinite(errors, "pid.kp", Pid.Kp);
        _ = ValidateFinite(errors, "pid.ki", Pid.Ki);
        _ = ValidateFinite(errors, "pid.kd", Pid.Kd);
        bool pim = ValidateFinite(errors, "pid.integral_max", Pid.IntegralMax);
        _ = ValidateFinite(errors, "eprocess_budget.lambda", EProcessBudget.Lambda);
        bool eba = ValidateFinite(errors, "eprocess_budget.alpha", EProcessBudget.Alpha);
        _ = ValidateFinite(errors, "eprocess_budget.beta", EProcessBudget.Beta);
        _ = ValidateFinite(errors, "eprocess_budget.sigma_ema_decay", EProcessBudget.SigmaEmaDecay);
        _ = ValidateFinite(errors, "eprocess_budget.sigma_floor_ms", EProcessBudget.SigmaFloorMs);
        _ = ValidateFinite(errors, "bocpd.mu_steady_ms", Bocpd.MuSteadyMs);
        _ = ValidateFinite(errors, "bocpd.mu_burst_ms", Bocpd.MuBurstMs);
        bool bhl = ValidateFinite(errors, "bocpd.hazard_lambda", Bocpd.HazardLambda);
        _ = ValidateFinite(errors, "bocpd.steady_threshold", Bocpd.SteadyThreshold);
        _ = ValidateFinite(errors, "bocpd.burst_threshold", Bocpd.BurstThreshold);
        _ = ValidateFinite(errors, "bocpd.burst_prior", Bocpd.BurstPrior);
        _ = ValidateFinite(errors, "bocpd.min_observation_ms", Bocpd.MinObservationMs);
        _ = ValidateFinite(errors, "bocpd.max_observation_ms", Bocpd.MaxObservationMs);
        bool eta = ValidateFinite(errors, "eprocess_throttle.alpha", EProcessThrottle.Alpha);
        _ = ValidateFinite(errors, "eprocess_throttle.mu_0", EProcessThrottle.Mu0);
        _ = ValidateFinite(errors, "eprocess_throttle.initial_lambda", EProcessThrottle.InitialLambda);
        _ = ValidateFinite(errors, "eprocess_throttle.grapa_eta", EProcessThrottle.GrapaEta);
        bool va = ValidateFinite(errors, "voi.alpha", Voi.Alpha);
        _ = ValidateFinite(errors, "voi.prior_alpha", Voi.PriorAlpha);
        _ = ValidateFinite(errors, "voi.prior_beta", Voi.PriorBeta);
        _ = ValidateFinite(errors, "voi.mu_0", Voi.Mu0);
        _ = ValidateFinite(errors, "voi.lambda", Voi.Lambda);
        _ = ValidateFinite(errors, "voi.value_scale", Voi.ValueScale);
        _ = ValidateFinite(errors, "voi.boundary_weight", Voi.BoundaryWeight);
        bool vsc = ValidateFinite(errors, "voi.sample_cost", Voi.SampleCost);

        if (ca && (Conformal.Alpha <= 0.0 || Conformal.Alpha >= 1.0))
            errors.Add($"conformal.alpha must be in (0, 1), got {Conformal.Alpha}");
        if (Conformal.MinSamples == 0)
            errors.Add("conformal.min_samples must be > 0");
        if ((int)Cascade.MinTriggerLevel > (int)Cascade.MaxDegradation)
            errors.Add($"cascade.min_trigger_level ({Cascade.MinTriggerLevel}) cannot be strictly greater than cascade.max_degradation ({Cascade.MaxDegradation})");
        if (Conformal.WindowSize == 0)
            errors.Add("conformal.window_size must be > 0");
        if (fgfb && FrameGuard.FallbackBudgetUs <= 0.0)
            errors.Add($"frame_guard.fallback_budget_us must be > 0, got {FrameGuard.FallbackBudgetUs}");
        if (pk && Pid.Kp < 0.0)
            errors.Add($"pid.kp must be >= 0, got {Pid.Kp}");
        if (pim && Pid.IntegralMax <= 0.0)
            errors.Add($"pid.integral_max must be > 0, got {Pid.IntegralMax}");
        if (eba && (EProcessBudget.Alpha <= 0.0 || EProcessBudget.Alpha >= 1.0))
            errors.Add($"eprocess_budget.alpha must be in (0, 1), got {EProcessBudget.Alpha}");
        if (bhl && Bocpd.HazardLambda <= 0.0)
            errors.Add($"bocpd.hazard_lambda must be > 0, got {Bocpd.HazardLambda}");
        if (Bocpd.MaxRunLength == 0)
            errors.Add("bocpd.max_run_length must be > 0");
        if (eta && (EProcessThrottle.Alpha <= 0.0 || EProcessThrottle.Alpha >= 1.0))
            errors.Add($"eprocess_throttle.alpha must be in (0, 1), got {EProcessThrottle.Alpha}");
        if (va && (Voi.Alpha <= 0.0 || Voi.Alpha >= 1.0))
            errors.Add($"voi.alpha must be in (0, 1), got {Voi.Alpha}");
        if (vsc && Voi.SampleCost < 0.0)
            errors.Add($"voi.sample_cost must be >= 0, got {Voi.SampleCost}");
        if (Evidence.LedgerCapacity == 0)
            errors.Add("evidence.ledger_capacity must be > 0");

        return errors;
    }

    private static bool ValidateFinite(List<string> errors, string field, double value)
    {
        if (double.IsFinite(value)) return true;
        errors.Add($"{field} must be finite, got {value}");
        return false;
    }

    /// <summary>Format as a JSONL line for structured logging.</summary>
    public string ToJsonl()
    {
        return $"{{\"schema\":\"policy-config-v1\",\"conformal_alpha\":{Conformal.Alpha},\"conformal_min_samples\":{Conformal.MinSamples},\"cascade_recovery_threshold\":{Cascade.RecoveryThreshold},\"pid_kp\":{Pid.Kp},\"bocpd_hazard_lambda\":{Bocpd.HazardLambda},\"voi_alpha\":{Voi.Alpha},\"evidence_ledger_capacity\":{Evidence.LedgerCapacity}}}";
    }

    public ConformalConfig ToConformalConfig() => new()
    {
        Alpha = Conformal.Alpha,
        MinSamples = Conformal.MinSamples,
        WindowSize = Conformal.WindowSize,
        QDefault = Conformal.QDefault,
    };

    public ConformalFrameGuardConfig ToFrameGuardConfig() => new(
        TimeSpan.FromMicroseconds(FrameGuard.FallbackBudgetUs),
        new RuntimeConformalConfig(
            Conformal.Alpha,
            Conformal.MinSamples,
            Conformal.WindowSize,
            Conformal.QDefault),
        FrameGuard.TimeSeriesWindow,
        FrameGuard.NonconformityWindow);

    // DIVERGENCE: CascadeConfig is named DegradationCascadeConfig in the
    // managed runtime to avoid ambiguity with the render budget controller.
    public DegradationCascadeConfig ToCascadeConfig() => new(
        ToFrameGuardConfig(),
        checked((int)Cascade.RecoveryThreshold),
        ToRuntimeDegradation(Cascade.MaxDegradation),
        ToRuntimeDegradation(Cascade.MinTriggerLevel),
        ToRuntimeDegradation(Cascade.DegradationFloor));

    public FrankenTui.Render.PidGains ToPidGains() => new()
    {
        Kp = Pid.Kp,
        Ki = Pid.Ki,
        Kd = Pid.Kd,
        IntegralMax = Pid.IntegralMax,
    };

    public FrankenTui.Render.EProcessConfig ToEProcessBudgetConfig() => new()
    {
        Lambda = EProcessBudget.Lambda,
        Alpha = EProcessBudget.Alpha,
        Beta = EProcessBudget.Beta,
        SigmaEmaDecay = EProcessBudget.SigmaEmaDecay,
        SigmaFloorMs = EProcessBudget.SigmaFloorMs,
        WarmupFrames = EProcessBudget.WarmupFrames,
    };

    public BocpdConfig ToBocpdConfig() => new()
    {
        MuSteadyMs = Bocpd.MuSteadyMs,
        MuBurstMs = Bocpd.MuBurstMs,
        HazardLambda = Bocpd.HazardLambda,
        MaxRunLength = Bocpd.MaxRunLength,
        SteadyThreshold = Bocpd.SteadyThreshold,
        BurstThreshold = Bocpd.BurstThreshold,
        BurstPrior = Bocpd.BurstPrior,
        MinObservationMs = Bocpd.MinObservationMs,
        MaxObservationMs = Bocpd.MaxObservationMs,
        EnableLogging = Bocpd.EnableLogging,
    };

    public ThrottleConfig ToThrottleConfig() => new()
    {
        Alpha = EProcessThrottle.Alpha,
        Mu0 = EProcessThrottle.Mu0,
        InitialLambda = EProcessThrottle.InitialLambda,
        GrapaEta = EProcessThrottle.GrapaEta,
        HardDeadlineMs = EProcessThrottle.HardDeadlineMs,
        MinObservationsBetween = EProcessThrottle.MinObservationsBetween,
        RateWindowSize = EProcessThrottle.RateWindowSize,
        EnableLogging = EProcessThrottle.EnableLogging,
    };

    public VoiConfig ToVoiConfig() => new()
    {
        Alpha = Voi.Alpha,
        PriorAlpha = Voi.PriorAlpha,
        PriorBeta = Voi.PriorBeta,
        Mu0 = Voi.Mu0,
        Lambda = Voi.Lambda,
        ValueScale = Voi.ValueScale,
        BoundaryWeight = Voi.BoundaryWeight,
        SampleCost = Voi.SampleCost,
        MinIntervalMs = Voi.MinIntervalMs,
        MaxIntervalMs = Voi.MaxIntervalMs,
        MinIntervalEvents = Voi.MinIntervalEvents,
        MaxIntervalEvents = Voi.MaxIntervalEvents,
        EnableLogging = Voi.EnableLogging,
        MaxLogEntries = Voi.MaxLogEntries,
    };

    public EvidenceSinkConfig ToEvidenceSinkConfig() => new()
    {
        Enabled = Evidence.SinkEnabled,
        Destination = Evidence.SinkFile is null
            ? EvidenceSinkDestination.Stdout
            : EvidenceSinkDestination.File,
        FilePath = Evidence.SinkFile,
        FlushOnWrite = Evidence.FlushOnWrite,
        MaxBytes = 50UL * 1024 * 1024,
    };

    private static RuntimeDegradationLevel ToRuntimeDegradation(DegradationLevel level) =>
        level switch
        {
            DegradationLevel.Full => RuntimeDegradationLevel.Full,
            DegradationLevel.SimpleBorders => RuntimeDegradationLevel.SimpleBorders,
            DegradationLevel.NoStyling => RuntimeDegradationLevel.NoStyling,
            DegradationLevel.EssentialOnly => RuntimeDegradationLevel.EssentialOnly,
            DegradationLevel.Skeleton => RuntimeDegradationLevel.Skeleton,
            DegradationLevel.SkipFrame => RuntimeDegradationLevel.SkipFrame,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
        };
}

// ── PolicyConfigError ─────────────────────────────────────────────────────

public abstract record PolicyConfigError
{
    public sealed record Io(string Message) : PolicyConfigError
    {
        public override string ToString() => $"I/O error: {Message}";
    }

    public sealed record MissingMetadataSection(string Section) : PolicyConfigError
    {
        public override string ToString() => $"missing metadata section: {Section}";
    }

    public sealed record Toml(string Message) : PolicyConfigError
    {
        public override string ToString() => $"TOML parse error: {Message}";
    }

    public sealed record Json(string Message) : PolicyConfigError
    {
        public override string ToString() => $"JSON parse error: {Message}";
    }

    public sealed record Validation(List<string> Errors) : PolicyConfigError
    {
        public override string ToString() => $"validation errors: {string.Join("; ", Errors)}";
    }

    public override string ToString() => this switch
    {
        Io io => $"I/O error: {io.Message}",
        MissingMetadataSection mms => $"missing metadata section: {mms.Section}",
        Toml toml => $"TOML parse error: {toml.Message}",
        Json json => $"JSON parse error: {json.Message}",
        Validation v => $"validation errors: {string.Join("; ", v.Errors)}",
        _ => "",
    };
}

internal sealed record PolicyTomlDocument(PolicyConfig Policy, bool MetadataSectionFound);

internal sealed class PolicyTomlException(string message) : Exception(message);

/// <summary>
/// Schema-directed TOML adapter for PolicyConfig. The upstream schema contains
/// only scalar fields; parsing those fields directly keeps the managed runtime
/// dependency-free while preserving partial-default and Cargo nesting semantics.
/// </summary>
internal static class PolicyTomlReader
{
    private const string CargoPrefix = "package.metadata.ftui";

    public static PolicyTomlDocument Parse(string content, bool cargoMetadata)
    {
        if (content is null)
            throw new PolicyTomlException("input cannot be null");

        var policy = new PolicyConfig();
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        string currentTable = "";
        bool metadataFound = false;
        string[] lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = StripComment(lines[lineIndex]).Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("[", StringComparison.Ordinal))
            {
                if (line.StartsWith("[[", StringComparison.Ordinal) || !line.EndsWith(']'))
                    throw Error(lineIndex, "invalid table header");
                currentTable = NormalizePath(line[1..^1], lineIndex);
                if (cargoMetadata &&
                    (currentTable == CargoPrefix ||
                     currentTable.StartsWith(CargoPrefix + ".", StringComparison.Ordinal)))
                {
                    metadataFound = true;
                }
                continue;
            }

            bool relevantCargoTable = !cargoMetadata ||
                currentTable == CargoPrefix ||
                currentTable.StartsWith(CargoPrefix + ".", StringComparison.Ordinal);
            if (cargoMetadata && !relevantCargoTable &&
                !line.StartsWith(CargoPrefix + ".", StringComparison.Ordinal))
            {
                // Cargo.toml is a larger document. Its non-policy tables are
                // intentionally left to Cargo; only metadata.ftui is projected.
                continue;
            }

            int assignment = FindUnquoted(line, '=');
            if (assignment <= 0)
                throw Error(lineIndex, "expected key = value");

            string key = NormalizePath(line[..assignment], lineIndex);
            string value = line[(assignment + 1)..].Trim();
            if (value.Length == 0)
                throw Error(lineIndex, "missing value");

            string fullPath = currentTable.Length == 0 ? key : currentTable + "." + key;
            string policyPath;
            if (cargoMetadata)
            {
                if (fullPath == CargoPrefix)
                {
                    metadataFound = true;
                    continue;
                }
                if (!fullPath.StartsWith(CargoPrefix + ".", StringComparison.Ordinal))
                    continue;
                metadataFound = true;
                policyPath = fullPath[(CargoPrefix.Length + 1)..];
            }
            else
            {
                policyPath = fullPath;
            }

            if (!TryResolveProperty(policy, policyPath, out object? owner, out PropertyInfo? property))
                continue; // Serde's default behavior ignores unknown fields.

            string canonicalPath = propertyPath(owner!, property!);
            if (!assigned.Add(canonicalPath))
                throw Error(lineIndex, $"duplicate key: {policyPath}");

            try
            {
                property!.SetValue(owner, ParseScalar(value, property.PropertyType));
            }
            catch (PolicyTomlException error)
            {
                throw Error(lineIndex, $"{policyPath}: {error.Message}");
            }
            catch (Exception error) when (error is FormatException or OverflowException)
            {
                throw Error(lineIndex, $"{policyPath}: {error.Message}");
            }
        }

        return new PolicyTomlDocument(policy, metadataFound);

        static string propertyPath(object owner, PropertyInfo property) =>
            owner.GetType().Name + "." + property.Name;
    }

    private static bool TryResolveProperty(
        PolicyConfig policy,
        string path,
        out object? owner,
        out PropertyInfo? property)
    {
        string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        owner = null;
        property = null;
        if (parts.Length != 2)
            return false;

        PropertyInfo? section = typeof(PolicyConfig).GetProperty(
            SnakeToPascal(parts[0]),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        owner = section?.GetValue(policy);
        if (owner is null)
            return false;

        property = owner.GetType().GetProperty(
            SnakeToPascal(parts[1]),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.CanWrite == true;
    }

    private static object? ParseScalar(string raw, Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        string numeric = raw.Replace("_", "", StringComparison.Ordinal);

        if (type == typeof(string))
            return ParseString(raw);
        if (type == typeof(bool))
        {
            if (raw == "true") return true;
            if (raw == "false") return false;
            throw new PolicyTomlException($"expected boolean, got {raw}");
        }
        if (type == typeof(double))
        {
            return numeric switch
            {
                "inf" or "+inf" => double.PositiveInfinity,
                "-inf" => double.NegativeInfinity,
                "nan" or "+nan" => double.NaN,
                "-nan" => -double.NaN,
                _ when double.TryParse(
                    numeric,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value) => value,
                _ => throw new PolicyTomlException($"expected number, got {raw}"),
            };
        }
        if (type == typeof(int))
        {
            if (int.TryParse(numeric, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
                return value;
            throw new PolicyTomlException($"expected integer, got {raw}");
        }
        if (type == typeof(uint))
        {
            if (uint.TryParse(numeric, NumberStyles.None, CultureInfo.InvariantCulture, out uint value))
                return value;
            throw new PolicyTomlException($"expected unsigned integer, got {raw}");
        }
        if (type == typeof(ulong))
        {
            if (ulong.TryParse(numeric, NumberStyles.None, CultureInfo.InvariantCulture, out ulong value))
                return value;
            throw new PolicyTomlException($"expected unsigned integer, got {raw}");
        }
        if (type == typeof(DegradationLevel))
            return DegradationLevelText.Parse(ParseString(raw));

        throw new PolicyTomlException($"unsupported policy value type {type.Name}");
    }

    private static string ParseString(string raw)
    {
        if (raw.Length >= 2 && raw[0] == '\'' && raw[^1] == '\'')
            return raw[1..^1];
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            try
            {
                return JsonSerializer.Deserialize<string>(raw)
                    ?? throw new PolicyTomlException("string cannot be null");
            }
            catch (JsonException error)
            {
                throw new PolicyTomlException(error.Message);
            }
        }
        throw new PolicyTomlException($"expected quoted string, got {raw}");
    }

    private static string StripComment(string line)
    {
        bool basic = false;
        bool literal = false;
        bool escaped = false;
        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];
            if (basic)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') basic = false;
            }
            else if (literal)
            {
                if (character == '\'') literal = false;
            }
            else if (character == '"') basic = true;
            else if (character == '\'') literal = true;
            else if (character == '#') return line[..index];
        }
        return line;
    }

    private static int FindUnquoted(string text, char target)
    {
        bool basic = false;
        bool literal = false;
        bool escaped = false;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (basic)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') basic = false;
            }
            else if (literal)
            {
                if (character == '\'') literal = false;
            }
            else if (character == '"') basic = true;
            else if (character == '\'') literal = true;
            else if (character == target) return index;
        }
        return -1;
    }

    private static string NormalizePath(string raw, int lineIndex)
    {
        string[] parts = raw.Trim().Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts.Any(part => part.Length == 0))
            throw Error(lineIndex, "invalid dotted key");
        foreach (string part in parts)
        {
            if (part.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '_' or '-')))
                throw Error(lineIndex, $"unsupported key syntax: {raw.Trim()}");
        }
        return string.Join('.', parts);
    }

    private static string SnakeToPascal(string value) => string.Concat(
        value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

    private static PolicyTomlException Error(int zeroBasedLine, string message) =>
        new($"line {zeroBasedLine + 1}: {message}");
}

internal static class DegradationLevelText
{
    public static DegradationLevel Parse(string value) => value switch
    {
        "full" or "Full" => DegradationLevel.Full,
        "simple_borders" or "SimpleBorders" => DegradationLevel.SimpleBorders,
        "no_styling" or "NoStyling" => DegradationLevel.NoStyling,
        "essential_only" or "EssentialOnly" => DegradationLevel.EssentialOnly,
        "skeleton" or "Skeleton" => DegradationLevel.Skeleton,
        "skip_frame" or "SkipFrame" => DegradationLevel.SkipFrame,
        _ => throw new PolicyTomlException($"unknown degradation level: {value}"),
    };

    public static string Format(DegradationLevel value) => value switch
    {
        DegradationLevel.Full => "full",
        DegradationLevel.SimpleBorders => "simple_borders",
        DegradationLevel.NoStyling => "no_styling",
        DegradationLevel.EssentialOnly => "essential_only",
        DegradationLevel.Skeleton => "skeleton",
        DegradationLevel.SkipFrame => "skip_frame",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}

internal sealed class DegradationLevelJsonConverter : JsonConverter<DegradationLevel>
{
    public override DegradationLevel Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("degradation level must be a string");
        string value = reader.GetString() ?? "";
        try
        {
            return DegradationLevelText.Parse(value);
        }
        catch (PolicyTomlException error)
        {
            throw new JsonException(error.Message);
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        DegradationLevel value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(DegradationLevelText.Format(value));
}
