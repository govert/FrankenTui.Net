// Tests ported from .external/frankentui/crates/ftui-runtime/src/policy_config.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Temporary directories use BCL APIs instead of Rust tempfile.

using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class PolicyConfigLoadingTests
{
    [Fact]
    public void TomlLoadsEveryPolicySectionAndPreservesPartialDefaults()
    {
        const string toml = """
            [conformal]
            alpha = 0.01
            min_samples = 30
            window_size = 300
            q_default = 12345.0

            [frame_guard]
            fallback_budget_us = 12000.0
            time_series_window = 400
            nonconformity_window = 200

            [cascade]
            recovery_threshold = 24
            max_degradation = "skip_frame"
            min_trigger_level = "NoStyling"
            degradation_floor = "simple_borders"

            [pid]
            kp = 0.7
            ki = 0.08
            kd = 0.3
            integral_max = 6.0

            [eprocess_budget]
            lambda = 0.4
            alpha = 0.02
            beta = 0.6
            sigma_ema_decay = 0.8
            sigma_floor_ms = 1.5
            warmup_frames = 12

            [bocpd]
            mu_steady_ms = 180.0
            mu_burst_ms = 18.0
            hazard_lambda = 40.0
            max_run_length = 80
            steady_threshold = 0.25
            burst_threshold = 0.75
            burst_prior = 0.3
            min_observation_ms = 2.0
            max_observation_ms = 9000.0
            enable_logging = true

            [eprocess_throttle]
            alpha = 0.03
            mu_0 = 0.2
            initial_lambda = 0.4
            grapa_eta = 0.2
            hard_deadline_ms = 600
            min_observations_between = 9
            rate_window_size = 72
            enable_logging = true

            [voi]
            alpha = 0.04
            prior_alpha = 1.2
            prior_beta = 1.3
            mu_0 = 0.06
            lambda = 0.6
            value_scale = 1.4
            boundary_weight = 1.5
            sample_cost = 0.02
            min_interval_ms = 2
            max_interval_ms = 300
            min_interval_events = 3
            max_interval_events = 30
            enable_logging = true
            max_log_entries = 4096

            [evidence]
            ledger_capacity = 2048
            sink_enabled = true
            sink_file = 'evidence#capture.jsonl' # hash belongs to the literal string
            flush_on_write = false
            """;

        Result<PolicyConfig, PolicyConfigError> result = PolicyConfig.FromTomlString(toml);

        Assert.True(result.IsOk, result.IsOk ? "" : result.UnwrapErr().ToString());
        PolicyConfig policy = result.Unwrap();
        Assert.Equal((0.01, 30, 300, 12_345.0),
            (policy.Conformal.Alpha, policy.Conformal.MinSamples,
                policy.Conformal.WindowSize, policy.Conformal.QDefault));
        Assert.Equal((12_000.0, 400, 200),
            (policy.FrameGuard.FallbackBudgetUs, policy.FrameGuard.TimeSeriesWindow,
                policy.FrameGuard.NonconformityWindow));
        Assert.Equal(24U, policy.Cascade.RecoveryThreshold);
        Assert.Equal(DegradationLevel.SkipFrame, policy.Cascade.MaxDegradation);
        Assert.Equal(DegradationLevel.NoStyling, policy.Cascade.MinTriggerLevel);
        Assert.Equal(DegradationLevel.SimpleBorders, policy.Cascade.DegradationFloor);
        Assert.Equal(0.7, policy.Pid.Kp);
        Assert.Equal(1.5, policy.EProcessBudget.SigmaFloorMs);
        Assert.Equal(12U, policy.EProcessBudget.WarmupFrames);
        Assert.Equal(80, policy.Bocpd.MaxRunLength);
        Assert.True(policy.Bocpd.EnableLogging);
        Assert.Equal(600UL, policy.EProcessThrottle.HardDeadlineMs);
        Assert.Equal(72, policy.EProcessThrottle.RateWindowSize);
        Assert.Equal(4096, policy.Voi.MaxLogEntries);
        Assert.Equal(2048, policy.Evidence.LedgerCapacity);
        Assert.Equal("evidence#capture.jsonl", policy.Evidence.SinkFile);
        Assert.False(policy.Evidence.FlushOnWrite);
    }

    [Fact]
    public void TomlAndJsonValidateLoadedValues()
    {
        Result<PolicyConfig, PolicyConfigError> toml = PolicyConfig.FromTomlString("""
            [conformal]
            alpha = nan
            q_default = inf

            [frame_guard]
            fallback_budget_us = inf
            """);
        PolicyConfigError.Validation tomlError =
            Assert.IsType<PolicyConfigError.Validation>(toml.UnwrapErr());
        Assert.Contains(tomlError.Errors, error =>
            error.Contains("conformal.alpha must be finite", StringComparison.Ordinal));
        Assert.Contains(tomlError.Errors, error =>
            error.Contains("conformal.q_default must be finite", StringComparison.Ordinal));
        Assert.Contains(tomlError.Errors, error =>
            error.Contains("frame_guard.fallback_budget_us must be finite", StringComparison.Ordinal));

        Result<PolicyConfig, PolicyConfigError> json =
            PolicyConfig.FromJsonString("""{"conformal":{"alpha":1.2}}""");
        Assert.IsType<PolicyConfigError.Validation>(json.UnwrapErr());
    }

    [Fact]
    public void JsonUsesSnakeCasePartialDefaultsAndDegradationNames()
    {
        Result<PolicyConfig, PolicyConfigError> result = PolicyConfig.FromJsonString("""
            {
              "conformal": { "alpha": 0.02 },
              "cascade": {
                "recovery_threshold": 19,
                "max_degradation": "SkipFrame",
                "min_trigger_level": "no_styling"
              },
              "evidence": { "sink_file": "capture.jsonl" }
            }
            """);

        Assert.True(result.IsOk, result.IsOk ? "" : result.UnwrapErr().ToString());
        PolicyConfig policy = result.Unwrap();
        Assert.Equal(0.02, policy.Conformal.Alpha);
        Assert.Equal(20, policy.Conformal.MinSamples);
        Assert.Equal(19U, policy.Cascade.RecoveryThreshold);
        Assert.Equal(DegradationLevel.SkipFrame, policy.Cascade.MaxDegradation);
        Assert.Equal(DegradationLevel.NoStyling, policy.Cascade.MinTriggerLevel);
        Assert.Equal(0.5, policy.Pid.Kp);
        Assert.Equal("capture.jsonl", policy.Evidence.SinkFile);
    }

    [Fact]
    public void SyntaxAndEnumFailuresKeepTheirParserErrorKinds()
    {
        Result<PolicyConfig, PolicyConfigError> badToml =
            PolicyConfig.FromTomlString("[cascade]\nmax_degradation = \"turbo\"");
        PolicyConfigError.Toml tomlError =
            Assert.IsType<PolicyConfigError.Toml>(badToml.UnwrapErr());
        Assert.Contains("unknown degradation level: turbo", tomlError.Message, StringComparison.Ordinal);

        Result<PolicyConfig, PolicyConfigError> duplicate =
            PolicyConfig.FromTomlString("[conformal]\nalpha = 0.1\nalpha = 0.2");
        Assert.IsType<PolicyConfigError.Toml>(duplicate.UnwrapErr());

        Result<PolicyConfig, PolicyConfigError> badJson =
            PolicyConfig.FromJsonString("""{"cascade":{"max_degradation":"turbo"}}""");
        PolicyConfigError.Json jsonError =
            Assert.IsType<PolicyConfigError.Json>(badJson.UnwrapErr());
        Assert.Contains("unknown degradation level: turbo", jsonError.Message, StringComparison.Ordinal);

        Result<PolicyConfig, PolicyConfigError> nullSection =
            PolicyConfig.FromJsonStr("""{"conformal":null}""");
        PolicyConfigError.Json nullError =
            Assert.IsType<PolicyConfigError.Json>(nullSection.UnwrapErr());
        Assert.Contains("conformal", nullError.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CargoMetadataExtractsPolicyAndIgnoresOtherCargoShapes()
    {
        Result<PolicyConfig, PolicyConfigError> result = PolicyConfig.FromCargoTomlString("""
            [workspace]
            members = [
              "crates/demo",
            ]

            [package]
            name = "demo"
            version = "0.1.0"

            [package.metadata.ftui.conformal]
            alpha = 0.01

            [package.metadata.ftui.cascade]
            recovery_threshold = 24
            """);

        Assert.True(result.IsOk, result.IsOk ? "" : result.UnwrapErr().ToString());
        PolicyConfig policy = result.Unwrap();
        Assert.Equal(0.01, policy.Conformal.Alpha);
        Assert.Equal(24U, policy.Cascade.RecoveryThreshold);
        Assert.Equal(0.5, policy.Pid.Kp);

        Result<PolicyConfig, PolicyConfigError> missing = PolicyConfig.FromCargoTomlString("""
            [package]
            name = "demo"
            version = "0.1.0"
            """);
        PolicyConfigError.MissingMetadataSection error =
            Assert.IsType<PolicyConfigError.MissingMetadataSection>(missing.UnwrapErr());
        Assert.Equal("[package.metadata.ftui]", error.Section);
    }

    [Fact]
    public void EmptyCargoMetadataSectionProducesDefaultPolicy()
    {
        Result<PolicyConfig, PolicyConfigError> result =
            PolicyConfig.FromCargoTomlString("[package.metadata.ftui]");

        Assert.True(result.IsOk);
        Assert.Empty(result.Unwrap().Validate());
    }

    [Fact]
    public void DiscoveryUsesTomlThenJsonThenCargoPrecedence()
    {
        WithTempDirectory(directory =>
        {
            string toml = Path.Combine(directory, "ftui-policy.toml");
            string json = Path.Combine(directory, "ftui-policy.json");
            string cargo = Path.Combine(directory, "Cargo.toml");
            File.WriteAllText(toml, "[conformal]\nalpha = 0.03");
            File.WriteAllText(json, """{"conformal":{"alpha":0.04}}""");
            File.WriteAllText(cargo,
                "[package.metadata.ftui.conformal]\nalpha = 0.05");

            Assert.Equal(0.03, PolicyConfig.DiscoverInDirectory(directory).Unwrap().Conformal.Alpha);
            File.Delete(toml);
            Assert.Equal(0.04, PolicyConfig.DiscoverInDirectory(directory).Unwrap().Conformal.Alpha);
            File.Delete(json);
            Assert.Equal(0.05, PolicyConfig.DiscoverInDirectory(directory).Unwrap().Conformal.Alpha);
        });
    }

    [Fact]
    public void MissingFilesAndDiscoveryReturnIoErrors()
    {
        WithTempDirectory(directory =>
        {
            Result<PolicyConfig, PolicyConfigError> missingFile =
                PolicyConfig.FromTomlFile(Path.Combine(directory, "missing.toml"));
            Assert.IsType<PolicyConfigError.Io>(missingFile.UnwrapErr());

            PolicyConfigError.Io discovery = Assert.IsType<PolicyConfigError.Io>(
                PolicyConfig.DiscoverInDirectory(directory).UnwrapErr());
            Assert.Contains("ftui-policy.toml", discovery.Message, StringComparison.Ordinal);
            Assert.Contains("ftui-policy.json", discovery.Message, StringComparison.Ordinal);
            Assert.Contains("Cargo.toml", discovery.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void LoadErrorDisplayUsesStablePrefixes()
    {
        Assert.StartsWith("I/O error: ", new PolicyConfigError.Io("broken").ToString());
        Assert.StartsWith("TOML parse error: ", new PolicyConfigError.Toml("broken").ToString());
        Assert.StartsWith("JSON parse error: ", new PolicyConfigError.Json("broken").ToString());
        Assert.Equal(
            "missing metadata section: [package.metadata.ftui]",
            new PolicyConfigError.MissingMetadataSection("[package.metadata.ftui]").ToString());
    }

    private static void WithTempDirectory(Action<string> action)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"frankentui-policy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            action(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
