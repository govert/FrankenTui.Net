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
}
