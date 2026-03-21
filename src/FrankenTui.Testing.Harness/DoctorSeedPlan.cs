using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record SeedEndpointSummary(
    string Host,
    string Port,
    string Path,
    string Endpoint);

public sealed record SeedRetryPolicySummary(
    int ConnectTimeoutSeconds,
    int RequestTimeoutSeconds,
    int MaxAttempts,
    int BaseBackoffMilliseconds,
    int PollIntervalSeconds);

public sealed record DoctorSeedPlan(
    string RunId,
    SeedEndpointSummary Endpoint,
    string ProjectKey,
    string AgentA,
    string AgentB,
    int MessageCount,
    int TimeoutSeconds,
    bool Deterministic,
    IReadOnlyList<string> Stages,
    SeedRetryPolicySummary RetryPolicy)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public static DoctorSeedPlan BuildDefault(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        var host = "127.0.0.1";
        var port = "8879";
        var path = "/mcp/";
        return new DoctorSeedPlan(
            runId,
            new SeedEndpointSummary(host, port, path, $"http://{host}:{port}{path}"),
            RepositoryPaths.FindRepositoryRoot(),
            "CrimsonHarbor",
            "AzureMeadow",
            6,
            30,
            true,
            [
                "health_check",
                "ensure_project",
                "register_agent_a",
                "register_agent_b",
                "send_messages",
                "search_messages",
                "file_reservation_paths"
            ],
            new SeedRetryPolicySummary(
                2,
                10,
                3,
                100,
                1));
    }

    public static string WriteArtifact(string runId, DoctorSeedPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(plan);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-seed-plan.json");
        File.WriteAllText(path, plan.ToJson());
        return path;
    }
}
