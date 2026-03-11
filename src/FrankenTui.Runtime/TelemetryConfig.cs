using System.Text.Json;

namespace FrankenTui.Runtime;

public enum TelemetryProtocol
{
    HttpProtobuf,
    Grpc
}

public enum TelemetrySpanProcessor
{
    Batch,
    Simple
}

public enum TelemetryEnabledReason
{
    None,
    SdkDisabled,
    ExporterNone,
    ExplicitExporter,
    BaseEndpoint,
    HttpOverride,
    TracesEndpoint
}

public enum TelemetryEndpointSource
{
    None,
    BaseEndpoint,
    HttpOverride,
    TracesEndpoint,
    DefaultHttp,
    DefaultGrpc
}

public enum TelemetryTraceContextSource
{
    New,
    ExplicitParent
}

public enum TelemetryInstallStatus
{
    Installed,
    Disabled,
    SubscriberAlreadySet
}

public sealed record TelemetryParentContext(
    string TraceId,
    string ParentSpanId);

public sealed record TelemetryConfig(
    bool Enabled,
    bool SdkDisabled,
    string TracesExporter,
    string? Endpoint,
    TelemetryEndpointSource EndpointSource,
    TelemetryProtocol Protocol,
    IReadOnlyDictionary<string, string> Headers,
    string ServiceName,
    IReadOnlyDictionary<string, string> ResourceAttributes,
    IReadOnlyList<string> Propagators,
    TelemetrySpanProcessor SpanProcessor,
    bool Verbose,
    TelemetryEnabledReason EnabledReason,
    TelemetryParentContext? ParentContext,
    TelemetryTraceContextSource TraceContextSource,
    IReadOnlyList<string> Warnings)
{
    public static TelemetryConfig Disabled { get; } = new(
        Enabled: false,
        SdkDisabled: false,
        TracesExporter: "otlp",
        Endpoint: null,
        EndpointSource: TelemetryEndpointSource.None,
        Protocol: TelemetryProtocol.HttpProtobuf,
        Headers: new Dictionary<string, string>(StringComparer.Ordinal),
        ServiceName: "ftui-runtime",
        ResourceAttributes: new Dictionary<string, string>(StringComparer.Ordinal),
        Propagators: ["tracecontext", "baggage"],
        SpanProcessor: TelemetrySpanProcessor.Batch,
        Verbose: false,
        EnabledReason: TelemetryEnabledReason.None,
        ParentContext: null,
        TraceContextSource: TelemetryTraceContextSource.New,
        Warnings: []);

    public static TelemetryConfig FromEnvironment(IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                static entry => (string)entry.Key,
                static entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);

        var warnings = new List<string>();
        var sdkDisabled = ParseBool(Get(environment, "OTEL_SDK_DISABLED"));
        var exporterText = Get(environment, "OTEL_TRACES_EXPORTER");
        var hasExporter = !string.IsNullOrWhiteSpace(exporterText);
        var exporter = NormalizeExporter(exporterText, warnings);
        var tracesEndpoint = Trim(Get(environment, "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"));
        var httpOverride = Trim(Get(environment, "FTUI_OTEL_HTTP_ENDPOINT"));
        var baseEndpoint = Trim(Get(environment, "OTEL_EXPORTER_OTLP_ENDPOINT"));

        var enabledReason = ResolveEnabledReason(
            sdkDisabled,
            exporter,
            hasExporter,
            tracesEndpoint,
            httpOverride,
            baseEndpoint);
        var enabled = enabledReason is not (TelemetryEnabledReason.None or TelemetryEnabledReason.SdkDisabled or TelemetryEnabledReason.ExporterNone);

        var protocol = ParseProtocol(Get(environment, "OTEL_EXPORTER_OTLP_PROTOCOL"), warnings);
        var (endpoint, endpointSource) = ResolveEndpoint(enabled, protocol, tracesEndpoint, httpOverride, baseEndpoint);
        var spanProcessor = ParseSpanProcessor(Get(environment, "FTUI_OTEL_SPAN_PROCESSOR"), warnings);
        var verbose = ParseBool(Get(environment, "FTUI_TELEMETRY_VERBOSE"));
        var (parentContext, traceContextSource) = ResolveTraceContext(
            Get(environment, "OTEL_TRACE_ID"),
            Get(environment, "OTEL_PARENT_SPAN_ID"),
            warnings);

        return new TelemetryConfig(
            enabled,
            sdkDisabled,
            exporter,
            endpoint,
            endpointSource,
            protocol,
            ParseKvList(Get(environment, "OTEL_EXPORTER_OTLP_HEADERS")),
            Trim(Get(environment, "OTEL_SERVICE_NAME")) ?? "ftui-runtime",
            ParseKvList(Get(environment, "OTEL_RESOURCE_ATTRIBUTES")),
            ParseCsvList(Get(environment, "OTEL_PROPAGATORS"), ["tracecontext", "baggage"]),
            spanProcessor,
            verbose,
            enabledReason,
            parentContext,
            traceContextSource,
            warnings);
    }

    public TelemetryConfigSummary ToSummary() =>
        new(
            Enabled,
            EnabledReason.ToString().ToLowerInvariant(),
            SdkDisabled,
            TracesExporter,
            Endpoint,
            EndpointSource.ToString().ToLowerInvariant(),
            Protocol switch
            {
                TelemetryProtocol.Grpc => "grpc",
                _ => "http/protobuf"
            },
            ServiceName,
            SpanProcessor.ToString().ToLowerInvariant(),
            Verbose,
            TraceContextSource.ToString().ToLowerInvariant(),
            Headers.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray(),
            ResourceAttributes.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray(),
            Propagators,
            Warnings);

    public string ToJson() =>
        JsonSerializer.Serialize(ToSummary(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        });

    public TelemetryLayerPlan BuildLayer() =>
        new(
            Enabled,
            Endpoint,
            EndpointSource,
            Protocol,
            SpanProcessor,
            ServiceName,
            Propagators,
            ParentContext,
            Verbose);

    public ITelemetryExporter BuildExporter(HttpClient? httpClient = null) =>
        !Enabled
            ? new DisabledTelemetryExporter()
            : new OtlpBridgeTelemetryExporter(this, httpClient);

    public TelemetryInstallResult Install(TelemetryRegistry registry, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (!Enabled)
        {
            return new TelemetryInstallResult(TelemetryInstallStatus.Disabled, null, null);
        }

        var layer = BuildLayer();
        if (!registry.TryInstall(layer, BuildExporter(httpClient)))
        {
            return new TelemetryInstallResult(
                TelemetryInstallStatus.SubscriberAlreadySet,
                layer,
                "A telemetry subscriber/layer is already installed.");
        }

        return new TelemetryInstallResult(TelemetryInstallStatus.Installed, layer, null);
    }

    private static TelemetryEnabledReason ResolveEnabledReason(
        bool sdkDisabled,
        string exporter,
        bool hasExporter,
        string? tracesEndpoint,
        string? httpOverride,
        string? baseEndpoint)
    {
        if (sdkDisabled)
        {
            return TelemetryEnabledReason.SdkDisabled;
        }

        if (hasExporter && string.Equals(exporter, "none", StringComparison.Ordinal))
        {
            return TelemetryEnabledReason.ExporterNone;
        }

        if (hasExporter && string.Equals(exporter, "otlp", StringComparison.Ordinal))
        {
            return TelemetryEnabledReason.ExplicitExporter;
        }

        if (!string.IsNullOrWhiteSpace(tracesEndpoint))
        {
            return TelemetryEnabledReason.TracesEndpoint;
        }

        if (!string.IsNullOrWhiteSpace(httpOverride))
        {
            return TelemetryEnabledReason.HttpOverride;
        }

        if (!string.IsNullOrWhiteSpace(baseEndpoint))
        {
            return TelemetryEnabledReason.BaseEndpoint;
        }

        return TelemetryEnabledReason.None;
    }

    private static (string? Endpoint, TelemetryEndpointSource Source) ResolveEndpoint(
        bool enabled,
        TelemetryProtocol protocol,
        string? tracesEndpoint,
        string? httpOverride,
        string? baseEndpoint)
    {
        if (!string.IsNullOrWhiteSpace(tracesEndpoint))
        {
            return (tracesEndpoint, TelemetryEndpointSource.TracesEndpoint);
        }

        if (!string.IsNullOrWhiteSpace(httpOverride))
        {
            return (httpOverride, TelemetryEndpointSource.HttpOverride);
        }

        if (!string.IsNullOrWhiteSpace(baseEndpoint))
        {
            return (baseEndpoint, TelemetryEndpointSource.BaseEndpoint);
        }

        if (!enabled)
        {
            return (null, TelemetryEndpointSource.None);
        }

        return protocol == TelemetryProtocol.Grpc
            ? ("http://localhost:4317", TelemetryEndpointSource.DefaultGrpc)
            : ("http://localhost:4318", TelemetryEndpointSource.DefaultHttp);
    }

    private static (TelemetryParentContext? ParentContext, TelemetryTraceContextSource Source) ResolveTraceContext(
        string? traceIdText,
        string? parentSpanIdText,
        List<string> warnings)
    {
        var traceId = Trim(traceIdText);
        var parentSpanId = Trim(parentSpanIdText);
        if (string.IsNullOrWhiteSpace(traceId) || string.IsNullOrWhiteSpace(parentSpanId))
        {
            if (!string.IsNullOrWhiteSpace(traceId) || !string.IsNullOrWhiteSpace(parentSpanId))
            {
                warnings.Add("Explicit OTEL trace context requires both OTEL_TRACE_ID and OTEL_PARENT_SPAN_ID; falling back to new root trace.");
            }

            return (null, TelemetryTraceContextSource.New);
        }

        if (!IsValidHexId(traceId, 32) || !IsValidHexId(parentSpanId, 16))
        {
            warnings.Add("Invalid OTEL_TRACE_ID or OTEL_PARENT_SPAN_ID; falling back to new root trace.");
            return (null, TelemetryTraceContextSource.New);
        }

        return (new TelemetryParentContext(traceId, parentSpanId), TelemetryTraceContextSource.ExplicitParent);
    }

    private static IReadOnlyDictionary<string, string> ParseKvList(string? text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in ParseCsvList(text, []))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            values[key] = value;
        }

        return values;
    }

    private static IReadOnlyList<string> ParseCsvList(string? text, IReadOnlyList<string> fallback)
    {
        var parts = Trim(text)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static part => part.Length > 0)
            .ToArray();
        return parts is { Length: > 0 } ? parts : fallback;
    }

    private static TelemetryProtocol ParseProtocol(string? text, List<string> warnings)
    {
        var normalized = Trim(text)?.ToLowerInvariant();
        switch (normalized)
        {
            case null:
            case "":
            case "http/protobuf":
                return TelemetryProtocol.HttpProtobuf;
            case "grpc":
                return TelemetryProtocol.Grpc;
            default:
                warnings.Add($"Unsupported OTEL_EXPORTER_OTLP_PROTOCOL '{normalized}'; using http/protobuf.");
                return TelemetryProtocol.HttpProtobuf;
        }
    }

    private static TelemetrySpanProcessor ParseSpanProcessor(string? text, List<string> warnings)
    {
        var normalized = Trim(text)?.ToLowerInvariant();
        switch (normalized)
        {
            case null:
            case "":
            case "batch":
                return TelemetrySpanProcessor.Batch;
            case "simple":
                return TelemetrySpanProcessor.Simple;
            default:
                warnings.Add($"Unsupported FTUI_OTEL_SPAN_PROCESSOR '{normalized}'; using batch.");
                return TelemetrySpanProcessor.Batch;
        }
    }

    private static string NormalizeExporter(string? text, List<string> warnings)
    {
        var normalized = Trim(text)?.ToLowerInvariant();
        switch (normalized)
        {
            case null:
            case "":
            case "otlp":
                return "otlp";
            case "none":
                return "none";
            default:
                warnings.Add($"Unsupported OTEL_TRACES_EXPORTER '{normalized}'; treating it as implicit otlp.");
                return "otlp";
        }
    }

    private static bool IsValidHexId(string text, int length)
    {
        if (text.Length != length)
        {
            return false;
        }

        var allZero = true;
        foreach (var character in text)
        {
            var isDigit = character is >= '0' and <= '9';
            var isLowerHex = character is >= 'a' and <= 'f';
            if (!isDigit && !isLowerHex)
            {
                return false;
            }

            if (character != '0')
            {
                allZero = false;
            }
        }

        return !allZero;
    }

    private static string? Get(IReadOnlyDictionary<string, string?> environment, string name) =>
        environment.TryGetValue(name, out var value) ? value : null;

    private static bool ParseBool(string? value) =>
        Trim(value)?.ToLowerInvariant() is "1" or "true" or "yes" or "on";

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record TelemetryConfigSummary(
    bool Enabled,
    string EnabledReason,
    bool SdkDisabled,
    string TracesExporter,
    string? Endpoint,
    string EndpointSource,
    string Protocol,
    string ServiceName,
    string SpanProcessor,
    bool Verbose,
    string TraceContextSource,
    IReadOnlyList<string> HeaderKeys,
    IReadOnlyList<string> ResourceAttributeKeys,
    IReadOnlyList<string> Propagators,
    IReadOnlyList<string> Warnings);

public sealed record TelemetryLayerPlan(
    bool Enabled,
    string? Endpoint,
    TelemetryEndpointSource EndpointSource,
    TelemetryProtocol Protocol,
    TelemetrySpanProcessor SpanProcessor,
    string ServiceName,
    IReadOnlyList<string> Propagators,
    TelemetryParentContext? ParentContext,
    bool Verbose);

public sealed record TelemetryInstallResult(
    TelemetryInstallStatus Status,
    TelemetryLayerPlan? Layer,
    string? Error)
{
    public bool Success => Status is TelemetryInstallStatus.Installed or TelemetryInstallStatus.Disabled;
}

public sealed class TelemetryRegistry
{
    public TelemetryLayerPlan? InstalledLayer { get; private set; }
    public ITelemetryExporter? Exporter { get; private set; }

    public bool TryInstall(TelemetryLayerPlan layer, ITelemetryExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(exporter);

        if (InstalledLayer is not null)
        {
            return false;
        }

        InstalledLayer = layer;
        Exporter = exporter;
        return true;
    }

    public Task<TelemetryExportReceipt> ExportAsync(
        TelemetrySessionLog sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        if (Exporter is null || InstalledLayer is null)
        {
            return Task.FromResult(
                new TelemetryExportReceipt(
                    Success: false,
                    Protocol: "uninstalled",
                    Endpoint: string.Empty,
                    EventCount: sessionLog.Events.Count,
                    StatusCode: null,
                    Error: "No telemetry exporter is installed."));
        }

        return Exporter.ExportAsync(sessionLog, cancellationToken);
    }
}
