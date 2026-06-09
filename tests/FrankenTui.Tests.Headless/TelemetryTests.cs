// Tests for .external/frankentui/crates/ftui-runtime/src/telemetry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class TelemetryTests
{
    [Fact] public void TraceIdParseValid()
    {
        var id = TraceId.Parse("abcdef1234567890abcdef1234567890");
        Assert.NotNull(id);
        Assert.Equal(16, id.AsBytes().Length);
    }

    [Fact] public void TraceIdParseTooShort()
    {
        Assert.Null(TraceId.Parse("abcdef"));
    }

    [Fact] public void TraceIdParseTooLong()
    {
        Assert.Null(TraceId.Parse("abcdef1234567890abcdef1234567890ff"));
    }

    [Fact] public void TraceIdParseUppercase()
    {
        Assert.Null(TraceId.Parse("ABCDEF1234567890ABCDEF1234567890"));
    }

    [Fact] public void TraceIdParseNonHex()
    {
        Assert.Null(TraceId.Parse("ghijklmnopqrstuvghijklmnopqrstuv"));
    }

    [Fact] public void TraceIdParseAllZeros()
    {
        Assert.Null(TraceId.Parse("00000000000000000000000000000000"));
    }

    [Fact] public void SpanIdParseValid()
    {
        var id = SpanId.Parse("abcdef1234567890");
        Assert.NotNull(id);
        Assert.Equal(8, id.AsBytes().Length);
    }

    [Fact] public void SpanIdParseTooShort()
    {
        Assert.Null(SpanId.Parse("abcdef"));
    }

    [Fact] public void SpanIdParseAllZeros()
    {
        Assert.Null(SpanId.Parse("0000000000000000"));
    }

    [Fact] public void DefaultDisabledByDefault()
    {
        var config = TelemetryConfiguration.FromEnvWith(_ => null);
        Assert.False(config.Enabled);
        Assert.Equal(EnabledReason.DefaultDisabled, config.EnabledReason);
    }

    [Fact] public void SdkDisabledDetected()
    {
        var config = TelemetryConfiguration.FromEnvWith(k => k == "OTEL_SDK_DISABLED" ? "true" : null);
        Assert.False(config.Enabled);
        Assert.Equal(EnabledReason.SdkDisabled, config.EnabledReason);
    }

    [Fact] public void ExporterNoneDetected()
    {
        var config = TelemetryConfiguration.FromEnvWith(k => k == "OTEL_TRACES_EXPORTER" ? "none" : null);
        Assert.False(config.Enabled);
        Assert.Equal(EnabledReason.ExporterNone, config.EnabledReason);
    }

    [Fact] public void ExplicitOtlpEnables()
    {
        string? Get(string k) => k switch
        {
            "OTEL_TRACES_EXPORTER" => "otlp",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.True(config.Enabled);
        Assert.Equal(EnabledReason.ExplicitOtlp, config.EnabledReason);
    }

    [Fact] public void EndpointSetEnables()
    {
        string? Get(string k) => k switch
        {
            "OTEL_EXPORTER_OTLP_ENDPOINT" => "http://collector:4318",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.True(config.Enabled);
        Assert.Equal(EnabledReason.EndpointSet, config.EnabledReason);
    }

    [Fact] public void FtuiEndpointEnables()
    {
        string? Get(string k) => k switch
        {
            "FTUI_OTEL_HTTP_ENDPOINT" => "http://localhost:9999",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.True(config.Enabled);
        Assert.Equal("http://localhost:9999", config.Endpoint);
    }

    [Fact] public void TracesEndpointHasPriority()
    {
        string? Get(string k) => k switch
        {
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT" => "http://traces:4318",
            "OTEL_EXPORTER_OTLP_ENDPOINT" => "http://base:4318",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.Equal("http://traces:4318", config.Endpoint);
        Assert.Equal(EndpointSource.TracesEndpoint, config.EndpointSource);
    }

    [Fact] public void ServiceNameParsed()
    {
        string? Get(string k) => k switch
        {
            "OTEL_EXPORTER_OTLP_ENDPOINT" => "http://collector:4318",
            "OTEL_SERVICE_NAME" => "my-service",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.Equal("my-service", config.ServiceName);
    }

    [Fact] public void ExplicitTraceContextParsed()
    {
        string? Get(string k) => k switch
        {
            "OTEL_EXPORTER_OTLP_ENDPOINT" => "http://collector:4318",
            "OTEL_TRACE_ID" => "abcdef1234567890abcdef1234567890",
            "OTEL_PARENT_SPAN_ID" => "abcdef1234567890",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.NotNull(config.TraceId);
        Assert.NotNull(config.ParentSpanId);
        Assert.Equal(TraceContextSource.Explicit, config.TraceContextSource);
    }

    [Fact] public void HeadersParsed()
    {
        string? Get(string k) => k switch
        {
            "OTEL_EXPORTER_OTLP_ENDPOINT" => "http://collector:4318",
            "OTEL_EXPORTER_OTLP_HEADERS" => "api-key=secret,trace=true",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        Assert.Equal(2, config.Headers.Count);
        Assert.Equal(("api-key", "secret"), config.Headers[0]);
        Assert.Equal(("trace", "true"), config.Headers[1]);
    }

    [Fact] public void EvidenceLedgerBuilt()
    {
        string? Get(string k) => k switch
        {
            "OTEL_EXPORTER_OTLP_ENDPOINT" => "http://collector:4318",
            "OTEL_SERVICE_NAME" => "test-svc",
            _ => null,
        };
        var config = TelemetryConfiguration.FromEnvWith(Get);
        var ledger = config.ToEvidenceLedger();
        Assert.True(ledger.Enabled);
        Assert.Equal("test-svc", ledger.ServiceName);
    }

    [Fact] public void DecisionEvidenceSimple()
    {
        var de = DecisionEvidence.Simple("rule-1", "skip");
        Assert.Equal("rule-1", de.Rule);
        Assert.Equal("skip", de.Action);
    }

    [Fact] public void DecisionEvidenceBuilder()
    {
        var de = DecisionEvidence.Simple("rule", "act")
            .WithExplanation("because")
            .WithConfidence(0.95f)
            .WithAlternatives(new List<string> { "alt1", "alt2" });
        Assert.Equal("because", de.Explanation);
        Assert.Equal(0.95f, de.Confidence);
        Assert.Equal(2, de.Alternatives.Count);
    }

    [Fact] public void TelemetryErrorToString()
    {
        Assert.Contains("already set", new TelemetryError.SubscriberAlreadySet().ToString());
        Assert.Contains("exporter", new TelemetryError.ExporterInit("bad").ToString());
        Assert.Contains("provider", new TelemetryError.ProviderSetup("fail").ToString());
    }
}
