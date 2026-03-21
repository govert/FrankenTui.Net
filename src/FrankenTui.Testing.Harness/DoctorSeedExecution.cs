using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorSeedStageResult(
    string Stage,
    string Status,
    long ElapsedMilliseconds,
    long RemainingMilliseconds,
    string? Detail = null);

public sealed record DoctorSeedExecution(
    string RunId,
    string Status,
    string Mode,
    string Endpoint,
    string ProjectKey,
    string AgentA,
    string AgentB,
    int MessageCount,
    string? LogPath,
    IReadOnlyList<DoctorSeedStageResult> Stages)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    private sealed class CounterState
    {
        public long Value { get; set; }
    }

    public static DoctorSeedExecution Simulate(DoctorSeedPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var remaining = plan.TimeoutSeconds * 1000L;
        var stages = new List<DoctorSeedStageResult>(plan.Stages.Count);
        foreach (var (stage, index) in plan.Stages.Select((value, idx) => (value, idx)))
        {
            var elapsed = stage switch
            {
                "health_check" => 250L,
                "send_messages" => 600L,
                _ => 120L
            };
            remaining = Math.Max(remaining - elapsed, 0);
            stages.Add(
                new DoctorSeedStageResult(
                    stage,
                    "ok",
                    elapsed,
                    remaining,
                    index == plan.Stages.Count - 1 ? "seed_complete" : null));
        }

        return new DoctorSeedExecution(
            plan.RunId,
            "ok",
            "simulated",
            plan.Endpoint.Endpoint,
            plan.ProjectKey,
            plan.AgentA,
            plan.AgentB,
            plan.MessageCount,
            null,
            stages);
    }

    public static DoctorSeedExecution Disabled(DoctorSeedPlan plan) =>
        new(
            plan.RunId,
            "skipped",
            "off",
            plan.Endpoint.Endpoint,
            plan.ProjectKey,
            plan.AgentA,
            plan.AgentB,
            plan.MessageCount,
            null,
            []);

    public static async Task<DoctorSeedExecution> ExecuteAsync(
        DoctorSeedPlan plan,
        HttpClient? httpClient = null,
        string? authBearer = null,
        string? logPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        using var ownedClient = httpClient is null
            ? new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(plan.RetryPolicy.RequestTimeoutSeconds)
            }
            : null;
        var client = httpClient ?? ownedClient!;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(plan.TimeoutSeconds);
        var stageResults = new List<DoctorSeedStageResult>(plan.Stages.Count);
        var counter = new CounterState();

        try
        {
            await WaitForServerAsync(client, plan, authBearer, deadline, counter, logPath, cancellationToken).ConfigureAwait(false);
            stageResults.Add(CreateStageResult("health_check", "ok", deadline, 0, "server_ready"));

            await RunStageAsync(client, plan, "ensure_project", JsonSerializer.SerializeToElement(
                new Dictionary<string, object?>
                {
                    ["human_key"] = plan.ProjectKey
                }, HarnessJson.SnakeCase), authBearer, deadline, counter, logPath, stageResults, cancellationToken).ConfigureAwait(false);

            await RunStageAsync(client, plan, "register_agent", JsonSerializer.SerializeToElement(
                new Dictionary<string, object?>
                {
                    ["project_key"] = plan.ProjectKey,
                    ["program"] = "FrankenTui.Doctor",
                    ["model"] = "gpt-5.4",
                    ["name"] = plan.AgentA,
                    ["task_description"] = "doctor seed sender"
                }, HarnessJson.SnakeCase), authBearer, deadline, counter, logPath, stageResults, cancellationToken, logicalStageName: "register_agent_a").ConfigureAwait(false);

            await RunStageAsync(client, plan, "register_agent", JsonSerializer.SerializeToElement(
                new Dictionary<string, object?>
                {
                    ["project_key"] = plan.ProjectKey,
                    ["program"] = "FrankenTui.Doctor",
                    ["model"] = "gpt-5.4",
                    ["name"] = plan.AgentB,
                    ["task_description"] = "doctor seed receiver"
                }, HarnessJson.SnakeCase), authBearer, deadline, counter, logPath, stageResults, cancellationToken, logicalStageName: "register_agent_b").ConfigureAwait(false);

            var messageStarted = Stopwatch.GetTimestamp();
            for (var index = 0; index < plan.MessageCount; index++)
            {
                var fromAgent = index % 2 == 0 ? plan.AgentA : plan.AgentB;
                var toAgent = index % 2 == 0 ? plan.AgentB : plan.AgentA;
                await CallToolAsync(
                    client,
                    "send_message",
                    JsonSerializer.SerializeToElement(
                        new Dictionary<string, object?>
                        {
                            ["project_key"] = plan.ProjectKey,
                            ["sender_name"] = fromAgent,
                            ["to"] = new[] { toAgent },
                            ["subject"] = $"FrankenTui.Net doctor seed message {index + 1}",
                            ["body_md"] = $"Seeded by doctor run {plan.RunId}. Iteration {index + 1}."
                        }, HarnessJson.SnakeCase),
                    authBearer,
                    plan,
                    deadline,
                    counter,
                    logPath,
                    cancellationToken).ConfigureAwait(false);
            }

            stageResults.Add(CreateStageResult(
                "send_messages",
                "ok",
                deadline,
                ElapsedMilliseconds(messageStarted),
                $"{plan.MessageCount} messages"));

            await RunStageAsync(client, plan, "search_messages", JsonSerializer.SerializeToElement(
                new Dictionary<string, object?>
                {
                    ["project_key"] = plan.ProjectKey,
                    ["query"] = "FrankenTui.Net",
                    ["limit"] = 20
                }, HarnessJson.SnakeCase), authBearer, deadline, counter, logPath, stageResults, cancellationToken).ConfigureAwait(false);

            var reservationStarted = Stopwatch.GetTimestamp();
            try
            {
                await CallToolAsync(
                    client,
                    "file_reservation_paths",
                    JsonSerializer.SerializeToElement(
                        new Dictionary<string, object?>
                        {
                            ["project_key"] = plan.ProjectKey,
                            ["agent_name"] = plan.AgentA,
                            ["paths"] = new[] { "artifacts/replay/*" },
                            ["ttl_seconds"] = 300,
                            ["exclusive"] = false,
                            ["reason"] = "doctor-seed"
                        }, HarnessJson.SnakeCase),
                    authBearer,
                    plan,
                    deadline,
                    counter,
                    logPath,
                    cancellationToken).ConfigureAwait(false);

                stageResults.Add(CreateStageResult("file_reservation_paths", "ok", deadline, ElapsedMilliseconds(reservationStarted), "reservation_checked"));
            }
            catch (Exception error)
            {
                await AppendLogLineAsync(logPath, $"event=seed_stage_warning stage=file_reservation_paths reason={error.Message}", cancellationToken).ConfigureAwait(false);
                stageResults.Add(CreateStageResult("file_reservation_paths", "warning", deadline, ElapsedMilliseconds(reservationStarted), error.Message));
            }

            stageResults[^1] = stageResults[^1] with { Detail = "seed_complete" };

            return new DoctorSeedExecution(
                plan.RunId,
                "ok",
                "actual",
                plan.Endpoint.Endpoint,
                plan.ProjectKey,
                plan.AgentA,
                plan.AgentB,
                plan.MessageCount,
                logPath,
                stageResults);
        }
        catch (Exception error)
        {
            if (stageResults.Count == 0 || !string.Equals(stageResults[^1].Status, "failed", StringComparison.Ordinal))
            {
                stageResults.Add(CreateStageResult("seed", "failed", deadline, 0, error.Message));
            }

            return new DoctorSeedExecution(
                plan.RunId,
                "failed",
                "actual",
                plan.Endpoint.Endpoint,
                plan.ProjectKey,
                plan.AgentA,
                plan.AgentB,
                plan.MessageCount,
                logPath,
                stageResults);
        }
    }

    private static async Task WaitForServerAsync(
        HttpClient client,
        DoctorSeedPlan plan,
        string? authBearer,
        DateTimeOffset deadline,
        CounterState counter,
        string? logPath,
        CancellationToken cancellationToken)
    {
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await CallToolOnceAsync(client, "health_check", JsonSerializer.SerializeToElement(new { }), authBearer, plan, deadline, counter, logPath, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(plan.RetryPolicy.PollIntervalSeconds), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException($"Timed out waiting for server at {plan.Endpoint.Endpoint}");
    }

    private static async Task RunStageAsync(
        HttpClient client,
        DoctorSeedPlan plan,
        string method,
        JsonElement arguments,
        string? authBearer,
        DateTimeOffset deadline,
        CounterState counter,
        string? logPath,
        List<DoctorSeedStageResult> stages,
        CancellationToken cancellationToken,
        string? logicalStageName = null)
    {
        var started = Stopwatch.GetTimestamp();
        var stageName = logicalStageName ?? method;
        try
        {
            await CallToolAsync(client, method, arguments, authBearer, plan, deadline, counter, logPath, cancellationToken).ConfigureAwait(false);
            stages.Add(CreateStageResult(stageName, "ok", deadline, ElapsedMilliseconds(started)));
        }
        catch (Exception error)
        {
            stages.Add(CreateStageResult(stageName, "failed", deadline, ElapsedMilliseconds(started), error.Message));
            throw;
        }
    }

    private static async Task<JsonDocument> CallToolAsync(
        HttpClient client,
        string method,
        JsonElement arguments,
        string? authBearer,
        DoctorSeedPlan plan,
        DateTimeOffset deadline,
        CounterState counter,
        string? logPath,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= plan.RetryPolicy.MaxAttempts; attempt++)
        {
            try
            {
                return await CallToolOnceAsync(client, method, arguments, authBearer, plan, deadline, counter, logPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                lastError = error;
                if (attempt == plan.RetryPolicy.MaxAttempts || DateTimeOffset.UtcNow >= deadline)
                {
                    break;
                }

                await AppendLogLineAsync(logPath, $"event=rpc_retry_scheduled stage={method} attempt={attempt} reason={error.Message}", cancellationToken).ConfigureAwait(false);
                var backoff = TimeSpan.FromMilliseconds(plan.RetryPolicy.BaseBackoffMilliseconds * (1 << (attempt - 1)));
                await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastError ?? new InvalidOperationException($"Seed call failed for {method}.");
    }

    private static async Task<JsonDocument> CallToolOnceAsync(
        HttpClient client,
        string method,
        JsonElement arguments,
        string? authBearer,
        DoctorSeedPlan plan,
        DateTimeOffset deadline,
        CounterState counter,
        string? logPath,
        CancellationToken cancellationToken)
    {
        counter.Value++;
        using var request = new HttpRequestMessage(HttpMethod.Post, plan.Endpoint.Endpoint);
        request.Headers.Add("Accept", "application/json");
        if (!string.IsNullOrWhiteSpace(authBearer))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authBearer);
        }

        request.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            id = counter.Value,
            method = "tools/call",
            @params = new
            {
                name = method,
                arguments
            }
        }, options: HarnessJson.SnakeCase);

        await AppendLogLineAsync(logPath, $"event=rpc_request stage={method} id={counter.Value}", cancellationToken).ConfigureAwait(false);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var remaining = deadline - DateTimeOffset.UtcNow;
        timeoutCts.CancelAfter(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
        using var response = await client.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
        await AppendLogLineAsync(logPath, $"event=rpc_response stage={method} id={counter.Value} payload={payload}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException($"RPC empty response for {method}");
        }

        var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("jsonrpc", out _))
        {
            throw new InvalidOperationException($"RPC non-JSON-RPC response for {method}: {payload}");
        }

        if (document.RootElement.TryGetProperty("error", out var rpcError))
        {
            throw new InvalidOperationException($"RPC error for {method}: {rpcError.GetRawText()}");
        }

        if (document.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("isError", out var isError) &&
            isError.ValueKind == JsonValueKind.True)
        {
            throw new InvalidOperationException($"MCP tool error for {method}: {payload}");
        }

        return document;
    }

    private static DoctorSeedStageResult CreateStageResult(string stage, string status, DateTimeOffset deadline, long elapsedMilliseconds, string? detail = null) =>
        new(
            stage,
            status,
            elapsedMilliseconds,
            Math.Max((long)(deadline - DateTimeOffset.UtcNow).TotalMilliseconds, 0),
            detail);

    private static long ElapsedMilliseconds(long started) =>
        (long)Math.Round((Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency);

    private static async Task AppendLogLineAsync(string? logPath, string line, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logPath))
        {
            return;
        }

        await File.AppendAllTextAsync(logPath, $"[{DateTimeOffset.UtcNow:O}] {line}{Environment.NewLine}", cancellationToken).ConfigureAwait(false);
    }

    public static string WriteArtifact(string runId, DoctorSeedExecution execution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(execution);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-seed-execution.json");
        File.WriteAllText(path, execution.ToJson());
        return path;
    }
}
