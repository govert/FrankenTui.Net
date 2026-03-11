using FrankenTui.Runtime;
using FrankenTui.Widgets;
using System.Net;
using System.Net.Http;
using System.Text;

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
    public async Task TelemetryExporterSendsDeterministicBridgePayload()
    {
        var config = TelemetryConfig.FromEnvironment(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318/v1/traces",
                ["OTEL_EXPORTER_OTLP_HEADERS"] = "authorization=Bearer test-token"
            });
        var handler = new CaptureHandler();
        using var httpClient = new HttpClient(handler);
        var registry = new TelemetryRegistry();
        var install = config.Install(registry, httpClient);
        var log = new TelemetrySessionLog(config);
        log.Record(
            "ftui.program.init",
            TelemetryEventCategory.RuntimePhase,
            0,
            [new TelemetryField("cmd_count", "0")]);

        var receipt = await registry.ExportAsync(log);

        Assert.Equal(TelemetryInstallStatus.Installed, install.Status);
        Assert.True(receipt.Success);
        Assert.Equal("http://collector.invalid:4318/v1/traces", handler.RequestUri);
        Assert.Equal("otlp", handler.BridgeHeader);
        Assert.Equal("Bearer test-token", handler.AuthorizationHeader);
        Assert.Contains("\"schema_version\": \"1.0.0\"", handler.Body);
        Assert.Contains("\"name\": \"ftui.program.init\"", handler.Body);
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

        var session = simulator.CreateSession(new ReplayProgram());
        await session.DispatchAsync("emit");

        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.program.init");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.program.update");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.program.view");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.program.subscriptions");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.render.frame");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.render.present");
        Assert.Contains(simulator.Runtime.Telemetry.Events, static item => item.Name == "ftui.render.flush");
    }

    [Fact]
    public void TelemetrySessionLogCanRecordMacroPlaybackEvidence()
    {
        var log = new TelemetrySessionLog(
            TelemetryConfig.FromEnvironment(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318"
                }));

        log.RecordMacro(4, "macro-001", 3, 12);

        var item = Assert.Single(log.Events, static item => item.Name == "ftui.input.macro");
        Assert.Contains(item.Fields, static field => field.Key == "macro_id" && field.Value == "macro-001");
        Assert.Contains(item.Fields, static field => field.Key == "event_count" && field.Value == "3");
    }

    [Fact]
    public void TelemetryArbitraryTextRedactionNeverLeaksLettersOrDigits()
    {
        var random = new Random(4242);
        for (var iteration = 0; iteration < 128; iteration++)
        {
            var builder = new StringBuilder(64);
            for (var index = 0; index < 64; index++)
            {
                builder.Append(index % 5 == 0
                    ? ' '
                    : (char)("abcdefghijklmnopqrstuvwxyz0123456789"[random.Next(36)]));
            }

            var redacted = TelemetryRedactor.RedactArbitraryText(builder.ToString());
            foreach (var ch in redacted)
            {
                Assert.True(ch is 'x' or ' ', $"Unexpected unredacted character {ch}.");
            }
        }
    }

    private sealed class ReplayProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            UpdateResult<int, string>.FromModel(message == "emit" ? model + 1 : model);

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"Telemetry {model}");
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }

        public string? BridgeHeader { get; private set; }

        public string? AuthorizationHeader { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            BridgeHeader = request.Headers.TryGetValues("x-ftui-telemetry-bridge", out var bridge)
                ? bridge.SingleOrDefault()
                : null;
            AuthorizationHeader = request.Headers.TryGetValues("authorization", out var auth)
                ? auth.SingleOrDefault()
                : null;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }
}
