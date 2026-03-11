using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public sealed class TelemetryContractTests
{
    [Fact]
    public void TelemetryConfigResolvesPrecedenceAndExplicitParentContext()
    {
        var config = TelemetryConfig.FromEnvironment(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://base.invalid:4318",
                ["FTUI_OTEL_HTTP_ENDPOINT"] = "http://override.invalid:4318",
                ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = "http://traces.invalid:4318/v1/traces",
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
                ["OTEL_TRACE_ID"] = "0123456789abcdef0123456789abcdef",
                ["OTEL_PARENT_SPAN_ID"] = "0123456789abcdef",
                ["FTUI_OTEL_SPAN_PROCESSOR"] = "simple",
                ["FTUI_TELEMETRY_VERBOSE"] = "true"
            });

        Assert.True(config.Enabled);
        Assert.Equal(TelemetryEnabledReason.TracesEndpoint, config.EnabledReason);
        Assert.Equal("http://traces.invalid:4318/v1/traces", config.Endpoint);
        Assert.Equal(TelemetryEndpointSource.TracesEndpoint, config.EndpointSource);
        Assert.Equal(TelemetryProtocol.Grpc, config.Protocol);
        Assert.Equal(TelemetrySpanProcessor.Simple, config.SpanProcessor);
        Assert.True(config.Verbose);
        Assert.Equal(TelemetryTraceContextSource.ExplicitParent, config.TraceContextSource);
        Assert.NotNull(config.ParentContext);
    }

    [Fact]
    public void TelemetryRedactionHidesSensitiveFieldsByDefault()
    {
        var config = TelemetryConfig.FromEnvironment(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
            });
        var log = new TelemetrySessionLog(config);
        log.Record(
            "ftui.input.event",
            TelemetryEventCategory.Input,
            0,
            [
                TelemetryRedactor.TextField("event_type", "secret password"),
                TelemetryRedactor.TypeField("msg_type", typeof(string), verbose: false),
                TelemetryRedactor.CustomField("action", "refresh")
            ]);

        var json = log.ToJson();

        Assert.DoesNotContain("secret password", json);
        Assert.Contains("[redacted:user-input]", json);
        Assert.Contains("[redacted:type]", json);
        Assert.Contains("\"app.action\"", json);
    }

    [Fact]
    public void TelemetryInstallHonorsNoClobberingRule()
    {
        var config = TelemetryConfig.FromEnvironment(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
            });
        var registry = new TelemetryRegistry();

        var first = config.Install(registry);
        var second = config.Install(registry);

        Assert.Equal(TelemetryInstallStatus.Installed, first.Status);
        Assert.Equal(TelemetryInstallStatus.SubscriberAlreadySet, second.Status);
        Assert.NotNull(first.Layer);
    }

    [Fact]
    public async Task RuntimeEmitsTelemetryEventsWhenEnabled()
    {
        var simulator = Ui.CreateSimulator<int, string>(
            40,
            10,
            theme: null,
            policy: RuntimeExecutionPolicy.Default with
            {
                EmitTelemetry = true,
                Telemetry = TelemetryConfig.FromEnvironment(
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
                    })
            });

        await simulator.DispatchAsync(new ReplayProgram(), 0, "emit");

        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.program.update");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.render.frame");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.render.present");
    }

    private sealed class ReplayProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(message == "emit" ? model + 1 : model);

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"Telemetry {model}");
    }
}
