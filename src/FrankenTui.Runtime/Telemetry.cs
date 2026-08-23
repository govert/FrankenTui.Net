// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/telemetry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Optional OpenTelemetry telemetry integration.
// Provides TelemetryConfig parsed from environment variables,
// TraceId/SpanId validation, and evidence ledger support.
//
// DIVERGENCE: install() and build_layer() are stubbed — they depend on
// OpenTelemetry SDK packages (opentelemetry, opentelemetry_sdk, otlp)
// which are not available in .NET. The data types and env-var parsing
// are fully ported.

namespace FrankenTui.Runtime;

// ── Enums ─────────────────────────────────────────────────────────────────

/// <summary>Reason why telemetry is enabled or disabled.</summary>
public enum EnabledReason
{
    SdkDisabled,
    ExporterNone,
    ExplicitOtlp,
    EndpointSet,
    FtuiEndpointSet,
    DefaultDisabled,
}

/// <summary>Source of the OTLP endpoint configuration.</summary>
public enum EndpointSource
{
    TracesEndpoint,
    FtuiOverride,
    BaseEndpoint,
    ProtocolDefault,
    None,
}

/// <summary>OTLP transport protocol.</summary>
public enum OtlpProtocol
{
    HttpProtobuf,
}

/// <summary>Span processor mode.</summary>
public enum SpanProcessorKind
{
    Batch,
    Simple,
}

/// <summary>Source of trace context.</summary>
public enum TraceContextSource
{
    Explicit,
    New,
    Disabled,
}

// ── TraceId / SpanId ──────────────────────────────────────────────────────

/// <summary>Validated 128-bit trace ID (32 hex chars).</summary>
public sealed class TraceId
{
    private readonly byte[] _bytes;

    private TraceId(byte[] bytes) { _bytes = bytes; }

    /// <summary>Parse a 32-char lowercase hex string into a trace ID.</summary>
    public static TraceId? Parse(string s)
    {
        if (s.Length != 32 || !s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
            return null;
        var bytes = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            if (!byte.TryParse(s.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                return null;
        }
        if (bytes.All(b => b == 0)) return null;
        return new TraceId(bytes);
    }

    /// <summary>Get the raw bytes.</summary>
    public byte[] AsBytes() => (byte[])_bytes.Clone();
}

/// <summary>Validated 64-bit span ID (16 hex chars).</summary>
public sealed class SpanId
{
    private readonly byte[] _bytes;

    private SpanId(byte[] bytes) { _bytes = bytes; }

    /// <summary>Parse a 16-char lowercase hex string into a span ID.</summary>
    public static SpanId? Parse(string s)
    {
        if (s.Length != 16 || !s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
            return null;
        var bytes = new byte[8];
        for (int i = 0; i < 8; i++)
        {
            if (!byte.TryParse(s.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                return null;
        }
        if (bytes.All(b => b == 0)) return null;
        return new SpanId(bytes);
    }

    /// <summary>Get the raw bytes.</summary>
    public byte[] AsBytes() => (byte[])_bytes.Clone();
}

// ── TelemetryError ────────────────────────────────────────────────────────

public abstract record TelemetryError
{
    public sealed record SubscriberAlreadySet : TelemetryError
    {
        public override string ToString() => "A global tracing subscriber is already set. Use build_layer() instead.";
    }
    public sealed record ExporterInit(string Message) : TelemetryError
    {
        public override string ToString() => $"Failed to initialize OTLP exporter: {Message}";
    }
    public sealed record ProviderSetup(string Message) : TelemetryError
    {
        public override string ToString() => $"Failed to set up tracer provider: {Message}";
    }

    public override string ToString() => this switch
    {
        SubscriberAlreadySet => "A global tracing subscriber is already set. Use build_layer() instead.",
        ExporterInit e => $"Failed to initialize OTLP exporter: {e.Message}",
        ProviderSetup p => $"Failed to set up tracer provider: {p.Message}",
        _ => "",
    };
}

// ── TelemetryConfig ───────────────────────────────────────────────────────

/// <summary>
/// Telemetry configuration parsed from environment variables.
/// </summary>
public sealed class TelemetryConfiguration
{
    public bool Enabled { get; set; }
    public EnabledReason EnabledReason { get; set; }
    public string? Endpoint { get; set; }
    public EndpointSource EndpointSource { get; set; }
    public OtlpProtocol Protocol { get; set; } = OtlpProtocol.HttpProtobuf;
    public SpanProcessorKind Processor { get; set; } = SpanProcessorKind.Batch;
    public string? ServiceName { get; set; }
    public List<(string, string)> ResourceAttributes { get; set; } = new();
    public TraceId? TraceId { get; set; }
    public SpanId? ParentSpanId { get; set; }
    public TraceContextSource TraceContextSource { get; set; }
    public List<(string, string)> Headers { get; set; } = new();

    /// <summary>Parse configuration from environment variables.</summary>
    public static TelemetryConfiguration FromEnv()
    {
        return FromEnvWith(Environment.GetEnvironmentVariable);
    }

    /// <summary>Parse configuration with a custom env-var getter (testable).</summary>
    public static TelemetryConfiguration FromEnvWith(Func<string, string?> get)
    {
        var config = new TelemetryConfiguration();

        // Check if explicitly disabled
        var sdkDisabled = get("OTEL_SDK_DISABLED");
        if (string.Equals(sdkDisabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            config.Enabled = false;
            config.EnabledReason = EnabledReason.SdkDisabled;
            config.TraceContextSource = TraceContextSource.Disabled;
            return config;
        }

        // Check exporter
        var tracesExporter = get("OTEL_TRACES_EXPORTER");
        if (string.Equals(tracesExporter, "none", StringComparison.OrdinalIgnoreCase))
        {
            config.Enabled = false;
            config.EnabledReason = EnabledReason.ExporterNone;
            config.TraceContextSource = TraceContextSource.Disabled;
            return config;
        }

        // Determine protocol (HTTP/protobuf only)
        var otelProtocol = get("OTEL_EXPORTER_OTLP_PROTOCOL");
        config.Protocol = OtlpProtocol.HttpProtobuf; // Only HTTP supported

        // Determine endpoint
        var tracesEndpoint = get("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
        var baseEndpoint = get("OTEL_EXPORTER_OTLP_ENDPOINT");
        var ftuiEndpoint = get("FTUI_OTEL_HTTP_ENDPOINT");

        if (!string.IsNullOrWhiteSpace(tracesEndpoint))
        {
            config.Endpoint = tracesEndpoint.Trim();
            config.EndpointSource = EndpointSource.TracesEndpoint;
            config.Enabled = true;
            config.EnabledReason = EnabledReason.ExplicitOtlp;
        }
        else if (!string.IsNullOrWhiteSpace(ftuiEndpoint))
        {
            config.Endpoint = ftuiEndpoint.Trim();
            config.EndpointSource = EndpointSource.FtuiOverride;
            config.Enabled = true;
            config.EnabledReason = EnabledReason.FtuiEndpointSet;
        }
        else if (!string.IsNullOrWhiteSpace(baseEndpoint))
        {
            config.Endpoint = baseEndpoint.Trim();
            config.EndpointSource = EndpointSource.BaseEndpoint;
            config.Enabled = true;
            config.EnabledReason = EnabledReason.EndpointSet;
        }
        else if (string.Equals(tracesExporter, "otlp", StringComparison.OrdinalIgnoreCase))
        {
            config.Endpoint = "http://localhost:4318";
            config.EndpointSource = EndpointSource.ProtocolDefault;
            config.Enabled = true;
            config.EnabledReason = EnabledReason.ExplicitOtlp;
        }
        else
        {
            config.Enabled = false;
            config.EnabledReason = EnabledReason.DefaultDisabled;
            config.TraceContextSource = TraceContextSource.Disabled;
            return config;
        }

        // Service name
        config.ServiceName = get("OTEL_SERVICE_NAME");

        // Trace context (explicit parent)
        var traceId = get("OTEL_TRACE_ID");
        var parentSpanId = get("OTEL_PARENT_SPAN_ID");
        if (!string.IsNullOrWhiteSpace(traceId) && !string.IsNullOrWhiteSpace(parentSpanId))
        {
            config.TraceId = TraceId.Parse(traceId.Trim());
            config.ParentSpanId = SpanId.Parse(parentSpanId.Trim());
            config.TraceContextSource = config.TraceId != null && config.ParentSpanId != null
                ? TraceContextSource.Explicit : TraceContextSource.New;
        }
        else
        {
            config.TraceContextSource = TraceContextSource.New;
        }

        // Headers
        var headers = get("OTEL_EXPORTER_OTLP_HEADERS");
        if (!string.IsNullOrWhiteSpace(headers))
        {
            foreach (var pair in headers.Split(','))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2)
                    config.Headers.Add((kv[0].Trim(), kv[1].Trim()));
            }
        }

        // Processor mode
        var processorEnv = get("OTEL_BSP_SCHEDULE_DELAY");
        config.Processor = SpanProcessorKind.Batch; // default

        return config;
    }

    /// <summary>Check if telemetry is enabled.</summary>
    public bool IsEnabled => Enabled;

    // DIVERGENCE: install() and build_layer() depend on OpenTelemetry SDK
    // packages not available in .NET. These are deferred.
    // public TelemetryGuard Install() => ...;
    // public (OtlpLayer, SdkTracerProvider) BuildLayer() => ...;

    /// <summary>Build an evidence ledger summary of this configuration.</summary>
    public EvidenceLedger ToEvidenceLedger()
    {
        return new EvidenceLedger
        {
            Enabled = Enabled,
            EnabledReason = EnabledReason.ToString(),
            Endpoint = Endpoint,
            EndpointSource = EndpointSource.ToString(),
            Protocol = Protocol.ToString(),
            ServiceName = ServiceName,
            TraceContextSource = TraceContextSource.ToString(),
        };
    }
}

// ── EvidenceLedger ────────────────────────────────────────────────────────

/// <summary>Evidence ledger record for telemetry configuration.</summary>
public sealed class EvidenceLedger
{
    public bool Enabled { get; init; }
    public string EnabledReason { get; init; } = "";
    public string? Endpoint { get; init; }
    public string EndpointSource { get; init; } = "";
    public string Protocol { get; init; } = "";
    public string? ServiceName { get; init; }
    public string TraceContextSource { get; init; } = "";
}

// ── DecisionEvidence ──────────────────────────────────────────────────────

/// <summary>Structured evidence for a telemetry decision.</summary>
public sealed class DecisionEvidence
{
    public string Rule { get; init; } = "";
    public string Action { get; init; } = "";
    public string? Explanation { get; set; }
    public float? Confidence { get; set; }
    public List<string> Alternatives { get; set; } = new();

    public static DecisionEvidence Simple(string rule, string action) => new()
    {
        Rule = rule,
        Action = action,
    };

    public DecisionEvidence WithExplanation(string explanation)
    {
        Explanation = explanation;
        return this;
    }

    public DecisionEvidence WithConfidence(float confidence)
    {
        Confidence = confidence;
        return this;
    }

    public DecisionEvidence WithAlternatives(List<string> alternatives)
    {
        Alternatives = alternatives;
        return this;
    }
}
