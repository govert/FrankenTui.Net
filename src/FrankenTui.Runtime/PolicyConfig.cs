// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/policy_config.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Policy-as-data configuration for FrankenTUI decision controllers.
// Captures all tunable parameters across the decision stack.
//
// DIVERGENCE: to_*_config() bridge methods are stubbed — they depend on
// upstream config types (ConformalConfig, CascadeConfig, BocpdConfig,
// VoiConfig, ThrottleConfig, PidGains, EProcessConfig, EvidenceSinkConfig)
// which live in Phases G, H, and I (not yet ported).

using FrankenTui.Render;

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

    // DIVERGENCE: to_*_config() bridge methods deferred — depend on upstream
    // config types from Phases G, H, I (ConformalConfig, CascadeConfig,
    // BocpdConfig, VoiConfig, ThrottleConfig, PidGains, EProcessConfig,
    // EvidenceSinkConfig). These are simple field-copy bridges; stubbed here.
    // public ConformalConfig ToConformalConfig() => ...;
    // public ConformalFrameGuardConfig ToFrameGuardConfig() => ...;
    // public CascadeConfig ToCascadeConfig() => ...;
    // public PidGains ToPidGains() => ...;
    // public EProcessConfig ToEProcessBudgetConfig() => ...;
    // public BocpdConfig ToBocpdConfig() => ...;
    // public ThrottleConfig ToThrottleConfig() => ...;
    // public VoiConfig ToVoiConfig() => ...;
    // public EvidenceSinkConfig ToEvidenceSinkConfig() => ...;
}

// ── PolicyConfigError ─────────────────────────────────────────────────────

public abstract record PolicyConfigError
{
    public sealed record Io(string Message) : PolicyConfigError;
    public sealed record MissingMetadataSection(string Section) : PolicyConfigError;
    public sealed record Validation(List<string> Errors) : PolicyConfigError;

    public override string ToString() => this switch
    {
        Io io => $"I/O error: {io.Message}",
        MissingMetadataSection mms => $"missing metadata section: {mms.Section}",
        Validation v => $"validation errors: {string.Join("; ", v.Errors)}",
        _ => "",
    };
}
