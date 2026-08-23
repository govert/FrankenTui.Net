using System.Runtime.InteropServices;
using System.Text.Json;

namespace FrankenTui.Runtime;

public enum TelemetryEventCategory
{
    RuntimePhase,
    RenderPipeline,
    Decision,
    Input
}

public enum TelemetryFieldSensitivity
{
    None,
    SoftRedacted,
    HardRedacted
}

public sealed record TelemetryField(
    string Key,
    string Value,
    TelemetryFieldSensitivity Sensitivity = TelemetryFieldSensitivity.None);

public sealed record TelemetryDecisionEvidence(
    string Rule,
    string InputsSummary,
    string Action,
    double? Confidence,
    IReadOnlyList<string> Alternatives,
    string Explanation);

public sealed record TelemetryEvent(
    string Name,
    string Category,
    int StepIndex,
    IReadOnlyList<TelemetryField> Fields,
    TelemetryDecisionEvidence? Evidence = null);

public sealed class TelemetrySessionLog
{
    private readonly List<TelemetryEvent> _events = [];

    public TelemetrySessionLog(TelemetryConfig? config = null)
    {
        Config = config ?? TelemetryConfig.Disabled;
    }

    public const string SchemaVersion = "1.0.0";

    public TelemetryConfig Config { get; }

    public IReadOnlyList<TelemetryEvent> Events => _events;

    public void Record(
        string name,
        TelemetryEventCategory category,
        int stepIndex,
        IEnumerable<TelemetryField> fields,
        TelemetryDecisionEvidence? evidence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fields);

        var orderedFields = BuildCommonFields()
            .Concat(fields)
            .OrderBy(static field => field.Key, StringComparer.Ordinal)
            .ToArray();

        _events.Add(
            new TelemetryEvent(
                name,
                category.ToString().ToLowerInvariant(),
                stepIndex,
                orderedFields,
                evidence));
    }

    public string ToJson() =>
        JsonSerializer.Serialize(
            new
            {
                schema_version = SchemaVersion,
                config = Config.ToSummary(),
                event_count = _events.Count,
                events = _events
            },
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });

    public void RecordMacro(
        int stepIndex,
        string macroId,
        int eventCount,
        long driftMs)
    {
        Record(
            "ftui.input.macro",
            TelemetryEventCategory.Input,
            stepIndex,
            [
                new TelemetryField("drift_ms", driftMs.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new TelemetryField("event_count", eventCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new TelemetryField("macro_id", macroId),
                new TelemetryField("state", "playing")
            ]);
    }

    private IReadOnlyList<TelemetryField> BuildCommonFields() =>
        [
            new TelemetryField("ftui.schema_version", SchemaVersion),
            new TelemetryField("host.arch", RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()),
            new TelemetryField("process.pid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new TelemetryField("service.name", Config.ServiceName),
            new TelemetryField("service.version", typeof(TelemetrySessionLog).Assembly.GetName().Version?.ToString() ?? "0.0.0"),
            new TelemetryField("telemetry.sdk", "ftui-telemetry")
        ];
}

public static class TelemetryRedactor
{
    public static string RedactUserInput(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : "[redacted:user-input]";

    public static string RedactPath(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : "[redacted:path]";

    public static string RedactEnvironment(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.StartsWith("OTEL_", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("FTUI_", StringComparison.OrdinalIgnoreCase))
        {
            return value ?? string.Empty;
        }

        return "[redacted:environment]";
    }

    public static TelemetryField CustomField(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var effectiveKey = key.StartsWith("app.", StringComparison.OrdinalIgnoreCase) ||
                           key.StartsWith("custom.", StringComparison.OrdinalIgnoreCase)
            ? key
            : $"app.{key}";

        return new TelemetryField(
            effectiveKey,
            value ?? string.Empty);
    }

    public static TelemetryField TypeField(string key, Type? type, bool verbose) =>
        verbose
            ? new TelemetryField(key, type?.Name ?? "null")
            : new TelemetryField(key, "[redacted:type]", TelemetryFieldSensitivity.SoftRedacted);

    public static TelemetryField TextField(string key, string? value) =>
        new(key, RedactUserInput(value), TelemetryFieldSensitivity.HardRedacted);

    public static string RedactArbitraryText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append('x');
            }
            else if (ch is '\n' or '\r' or '\t' or ' ')
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}

// ── Schema-meta wrapper types ──────────────────────────────────────────────
// Port of .external/frankentui/crates/ftui-runtime/src/telemetry_schema.rs
// constants and ALL_TARGETS / ALL_EVENTS / ALL_METRICS arrays. Exposed as the
// TelemetrySchemaMeta / TelemetryTarget / TelemetryEventNames / TelemetryMetric
// wrappers the test suite (the upstream spec) expects.

/// <summary>
/// Schema-version and manifest meta for the runtime telemetry contract.
/// Mirrors upstream <c>SCHEMA_VERSION</c> and the schema-manifest constants.
/// </summary>
public static class TelemetrySchemaMeta
{
    /// <summary>Schema version for forward compatibility. Upstream <c>SCHEMA_VERSION</c>.</summary>
    public const string SchemaVersion = "1.0.0";
}

/// <summary>Registered tracing targets. Upstream <c>ALL_TARGETS</c>.</summary>
public static class TelemetryTarget
{
    public const string Runtime = "ftui.runtime";
    public const string Effect = "ftui.effect";
    public const string Process = "ftui.process";
    public const string Resize = "ftui.decision.resize";
    public const string Voi = "ftui.voi";
    public const string Bocpd = "ftui.bocpd";
    public const string EProcess = "ftui.eprocess";

    public static IReadOnlyList<string> All { get; } =
    [
        Runtime, Effect, Process, Resize, Voi, Bocpd, EProcess,
    ];
}

/// <summary>Canonical structured event names. Upstream <c>event::*</c> and <c>ALL_EVENTS</c>.</summary>
public static class TelemetryEventNames
{
    public const string RuntimeStartup = "runtime.startup";
    public const string EffectQueueShutdown = "effect_queue.shutdown";
    public const string SpawnExecutorShutdown = "spawn_executor.shutdown";
    public const string SubscriptionStopAll = "subscription.stop_all";
    public const string SubscriptionStop = "subscription.stop";
    public const string EffectCommand = "effect.command";
    public const string EffectSubscription = "effect.subscription";
    public const string QueueDrop = "effect_queue.drop";
    public const string EffectTimeout = "effect.timeout";
    public const string EffectPanic = "effect.panic";

    public static IReadOnlyList<string> All { get; } =
    [
        RuntimeStartup, EffectQueueShutdown, SpawnExecutorShutdown,
        SubscriptionStopAll, SubscriptionStop, EffectCommand,
        EffectSubscription, QueueDrop, EffectTimeout, EffectPanic,
    ];
}

/// <summary>Counter/gauge metric names. Upstream <c>metric::*</c> and <c>ALL_METRICS</c>.</summary>
public static class TelemetryMetric
{
    public const string EffectsCommandTotal = "effects_command_total";
    public const string EffectsSubscriptionTotal = "effects_subscription_total";
    public const string EffectsExecutedTotal = "effects_executed_total";
    public const string EffectsQueueEnqueued = "effects_queue_enqueued";
    public const string EffectsQueueProcessed = "effects_queue_processed";
    public const string EffectsQueueDropped = "effects_queue_dropped";
    public const string EffectsQueueHighWater = "effects_queue_high_water";
    public const string EffectsQueueInFlight = "effects_queue_in_flight";

    public static IReadOnlyList<string> All { get; } =
    [
        EffectsCommandTotal, EffectsSubscriptionTotal, EffectsExecutedTotal,
        EffectsQueueEnqueued, EffectsQueueProcessed, EffectsQueueDropped,
        EffectsQueueHighWater, EffectsQueueInFlight,
    ];
}
