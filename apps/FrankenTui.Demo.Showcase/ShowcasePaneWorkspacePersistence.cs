using FrankenTui.Layout;
using System.Text.Json;

namespace FrankenTui.Demo.Showcase;

public sealed record ShowcasePaneWorkspaceLoadResult(
    PaneWorkspaceState Workspace,
    bool Loaded,
    string? Error = null,
    string? InvalidSnapshotPath = null,
    string SchemaVersion = ShowcasePaneWorkspacePersistence.CurrentSchemaVersion,
    bool MigrationApplied = false,
    string? MigrationFromVersion = null);

public sealed record ShowcasePaneWorkspaceSaveResult(
    string? Path,
    bool Saved,
    string? SnapshotHash = null,
    string? Error = null,
    string SchemaVersion = ShowcasePaneWorkspacePersistence.CurrentSchemaVersion);

public static class ShowcasePaneWorkspacePersistence
{
    public const string CurrentSchemaVersion = "showcase-pane-workspace-v2";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static ShowcasePaneWorkspaceLoadResult Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new ShowcasePaneWorkspaceLoadResult(PaneWorkspaceState.CreateDemo(), Loaded: false);
        }

        try
        {
            return LoadJson(File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            var invalidPath = PreserveInvalidSnapshot(path);
            return new ShowcasePaneWorkspaceLoadResult(
                PaneWorkspaceState.CreateDemo(),
                Loaded: false,
                Error: exception.GetType().Name,
                InvalidSnapshotPath: invalidPath);
        }
    }

    public static ShowcasePaneWorkspaceSaveResult Save(string? path, PaneWorkspaceState workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (string.IsNullOrWhiteSpace(path))
        {
            return new ShowcasePaneWorkspaceSaveResult(null, Saved: false, SnapshotHash: workspace.SnapshotHash());
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = fullPath + ".tmp";
            File.WriteAllText(tempPath, ToJson(workspace));
            File.Move(tempPath, fullPath, overwrite: true);
            return new ShowcasePaneWorkspaceSaveResult(fullPath, Saved: true, SnapshotHash: workspace.SnapshotHash());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return new ShowcasePaneWorkspaceSaveResult(path, Saved: false, SnapshotHash: workspace.SnapshotHash(), Error: exception.GetType().Name);
        }
    }

    private static string PreserveInvalidSnapshot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var invalidPath = fullPath + ".invalid";
        File.Copy(fullPath, invalidPath, overwrite: true);
        return invalidPath;
    }

    internal static ShowcasePaneWorkspaceLoadResult LoadJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Pane workspace snapshot must be a JSON object.");
        }

        if (document.RootElement.TryGetProperty("schema_version", out var schemaElement) &&
            schemaElement.ValueKind == JsonValueKind.String)
        {
            var schemaVersion = schemaElement.GetString() ?? string.Empty;
            if (!string.Equals(schemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported pane workspace schema {schemaVersion}.");
            }

            if (!document.RootElement.TryGetProperty("workspace", out var workspaceElement))
            {
                throw new JsonException("Pane workspace envelope is missing workspace.");
            }

            return new ShowcasePaneWorkspaceLoadResult(
                PaneWorkspaceState.DecodeJson(workspaceElement.GetRawText()).State,
                Loaded: true,
                SchemaVersion: schemaVersion);
        }

        return new ShowcasePaneWorkspaceLoadResult(
            PaneWorkspaceState.DecodeJson(json).State,
            Loaded: true,
            SchemaVersion: CurrentSchemaVersion,
            MigrationApplied: true,
            MigrationFromVersion: "raw-pane-workspace-v1");
    }

    internal static string ToJson(PaneWorkspaceState workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var canonicalWorkspace = PaneWorkspaceState.DecodeJson(workspace.ToCanonicalJson()).State;
        return JsonSerializer.Serialize(
            new
            {
                schema_version = CurrentSchemaVersion,
                workspace = canonicalWorkspace
            },
            JsonOptions);
    }
}
