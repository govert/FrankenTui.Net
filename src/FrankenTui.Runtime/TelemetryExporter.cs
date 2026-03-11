using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FrankenTui.Runtime;

public sealed record TelemetryExportReceipt(
    bool Success,
    string Protocol,
    string Endpoint,
    int EventCount,
    int? StatusCode,
    string? Error)
{
    public string ToJson() =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });
}

public interface ITelemetryExporter
{
    Task<TelemetryExportReceipt> ExportAsync(
        TelemetrySessionLog sessionLog,
        CancellationToken cancellationToken = default);
}

internal sealed class DisabledTelemetryExporter : ITelemetryExporter
{
    public Task<TelemetryExportReceipt> ExportAsync(
        TelemetrySessionLog sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);
        return Task.FromResult(
            new TelemetryExportReceipt(
                Success: true,
                Protocol: "disabled",
                Endpoint: string.Empty,
                EventCount: sessionLog.Events.Count,
                StatusCode: null,
                Error: null));
    }
}

internal sealed class OtlpBridgeTelemetryExporter : ITelemetryExporter
{
    private readonly HttpClient _httpClient;
    private readonly TelemetryConfig _config;

    public OtlpBridgeTelemetryExporter(TelemetryConfig config, HttpClient? httpClient = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<TelemetryExportReceipt> ExportAsync(
        TelemetrySessionLog sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        if (!_config.Enabled || string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            return new TelemetryExportReceipt(
                Success: true,
                Protocol: "disabled",
                Endpoint: _config.Endpoint ?? string.Empty,
                EventCount: sessionLog.Events.Count,
                StatusCode: null,
                Error: null);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _config.Endpoint);
            foreach (var header in _config.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            request.Headers.TryAddWithoutValidation("x-ftui-telemetry-bridge", "otlp");
            request.Headers.TryAddWithoutValidation(
                "x-ftui-telemetry-protocol",
                _config.Protocol == TelemetryProtocol.Grpc ? "grpc" : "http/protobuf");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = TelemetryExporterPayload.Create(_config, sessionLog);
            request.Content = new StringContent(
                payload.ToJson(),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return new TelemetryExportReceipt(
                response.IsSuccessStatusCode,
                _config.Protocol == TelemetryProtocol.Grpc ? "grpc" : "http/protobuf",
                _config.Endpoint,
                sessionLog.Events.Count,
                (int)response.StatusCode,
                response.IsSuccessStatusCode ? null : $"Exporter returned {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return new TelemetryExportReceipt(
                Success: false,
                Protocol: _config.Protocol == TelemetryProtocol.Grpc ? "grpc" : "http/protobuf",
                Endpoint: _config.Endpoint,
                EventCount: sessionLog.Events.Count,
                StatusCode: null,
                Error: exception.Message);
        }
    }
}

internal sealed record TelemetryExporterPayload(
    string SchemaVersion,
    TelemetryConfigSummary Config,
    IReadOnlyList<TelemetryEvent> Events)
{
    public static TelemetryExporterPayload Create(TelemetryConfig config, TelemetrySessionLog sessionLog) =>
        new(
            TelemetrySessionLog.SchemaVersion,
            config.ToSummary(),
            sessionLog.Events);

    public string ToJson() =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });
}
