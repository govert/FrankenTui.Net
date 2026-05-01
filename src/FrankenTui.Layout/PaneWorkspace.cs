using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FrankenTui.Layout;

public enum PaneWorkspaceMode
{
    Focus,
    Compare,
    Monitor,
    Compact
}

public enum PaneSplitDirection
{
    Horizontal,
    Vertical
}

public enum PaneWorkspaceActionKind
{
    SelectNext,
    SelectPrevious,
    CycleMode,
    GrowPrimary,
    ShrinkPrimary,
    Undo,
    Redo
}

public sealed record PaneWorkspaceAction(PaneWorkspaceActionKind Kind, DateTimeOffset Timestamp, string Reason);

public sealed record PaneWorkspaceNode(
    string Id,
    string Title,
    PaneSplitDirection? SplitDirection = null,
    IReadOnlyList<PaneWorkspaceNode>? Children = null)
{
    public IReadOnlyList<PaneWorkspaceNode> Children { get; init; } = Children ?? [];

    public bool IsLeaf => Children.Count == 0;
}

public sealed record PaneWorkspaceSnapshot(
    PaneWorkspaceNode Root,
    string SelectedPaneId,
    PaneWorkspaceMode Mode,
    int PrimaryRatioPermille);

public sealed record PaneWorkspaceCheckpoint(int AppliedCount, PaneWorkspaceSnapshot Snapshot);

public sealed record PaneWorkspaceReplayDiagnostics(
    int EntryCount,
    int Cursor,
    int CheckpointCount,
    int CheckpointInterval,
    bool CheckpointHit,
    int ReplayStartIndex,
    int ReplayDepth);

public sealed record PaneWorkspaceCheckpointDecision(
    int CheckpointInterval,
    long EstimatedSnapshotCostNs,
    long EstimatedReplayStepCostNs,
    long EstimatedReplayDepthNs);

public sealed record PaneWorkspaceJsonMigrationResult(
    PaneWorkspaceState State,
    string FromVersion,
    string ToVersion,
    bool MigrationApplied,
    IReadOnlyList<string> Warnings)
{
    public string Decision => MigrationApplied ? "migrated" : "current_schema";

    public string StateChecksum => State.SnapshotHash();
}

public sealed record PaneWorkspaceState(
    PaneWorkspaceNode Root,
    string SelectedPaneId,
    PaneWorkspaceMode Mode,
    int PrimaryRatioPermille = 500,
    IReadOnlyList<PaneWorkspaceAction>? Timeline = null,
    int TimelineCursor = 0,
    PaneWorkspaceSnapshot? Baseline = null,
    IReadOnlyList<PaneWorkspaceCheckpoint>? Checkpoints = null,
    int CheckpointInterval = 16)
{
    private const int MaxTimelineEntries = 24;
    private const int DefaultCheckpointInterval = 16;
    public const string CurrentJsonSchemaVersion = "pane-workspace-state-v1";

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public IReadOnlyList<PaneWorkspaceAction> Timeline { get; init; } = Timeline ?? [];

    public PaneWorkspaceSnapshot Baseline { get; init; } = Baseline ?? new PaneWorkspaceSnapshot(Root, SelectedPaneId, Mode, PrimaryRatioPermille);

    public IReadOnlyList<PaneWorkspaceCheckpoint> Checkpoints { get; init; } = Checkpoints ?? [];

    public static PaneWorkspaceState CreateDemo()
    {
        var root = new PaneWorkspaceNode(
            "root",
            "Workspace",
            PaneSplitDirection.Horizontal,
            [
                new PaneWorkspaceNode(
                    "editor-shell",
                    "Editor Shell",
                    PaneSplitDirection.Vertical,
                    [
                        new PaneWorkspaceNode("files", "Files"),
                        new PaneWorkspaceNode("editor", "Editor")
                    ]),
                new PaneWorkspaceNode(
                    "observer-shell",
                    "Observer Shell",
                    PaneSplitDirection.Vertical,
                    [
                        new PaneWorkspaceNode("logs", "Logs"),
                        new PaneWorkspaceNode("hud", "HUD")
                    ])
            ]);
        var baseline = new PaneWorkspaceSnapshot(root, "editor", PaneWorkspaceMode.Compare, 500);
        return FromSnapshot(baseline, baseline, [], 0, [], DefaultCheckpointInterval);
    }

    public IReadOnlyList<PaneWorkspaceNode> FlattenLeaves() =>
        Flatten(Root).Where(static node => node.IsLeaf).ToArray();

    public PaneWorkspaceReplayDiagnostics ReplayDiagnostics()
    {
        var checkpoint = Checkpoints.LastOrDefault(candidate => candidate.AppliedCount <= TimelineCursor);
        var replayStartIndex = checkpoint?.AppliedCount ?? 0;
        return new PaneWorkspaceReplayDiagnostics(
            Timeline.Count,
            TimelineCursor,
            Checkpoints.Count,
            CheckpointInterval,
            checkpoint is not null,
            replayStartIndex,
            Math.Max(TimelineCursor - replayStartIndex, 0));
    }

    public static PaneWorkspaceCheckpointDecision CheckpointDecision(long snapshotCostNs, long replayStepCostNs)
    {
        if (snapshotCostNs <= 0 || replayStepCostNs <= 0)
        {
            return new PaneWorkspaceCheckpointDecision(
                DefaultCheckpointInterval,
                snapshotCostNs,
                replayStepCostNs,
                Math.Max(replayStepCostNs, 0) * DefaultCheckpointInterval / 2);
        }

        var ratio = Math.Max((snapshotCostNs * 2L) / replayStepCostNs, 1L);
        var interval = (int)Math.Max(1, Math.Floor(Math.Sqrt(ratio)));
        return new PaneWorkspaceCheckpointDecision(
            interval,
            snapshotCostNs,
            replayStepCostNs,
            replayStepCostNs * interval / 2);
    }

    public PaneWorkspaceState Apply(PaneWorkspaceAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (action.Kind == PaneWorkspaceActionKind.Undo)
        {
            return Restore(Timeline, Checkpoints, Math.Max(TimelineCursor - 1, 0), Baseline, CheckpointInterval);
        }

        if (action.Kind == PaneWorkspaceActionKind.Redo)
        {
            return Restore(Timeline, Checkpoints, Math.Min(TimelineCursor + 1, Timeline.Count), Baseline, CheckpointInterval);
        }

        var activeTimeline = Timeline.Take(TimelineCursor).ToArray();
        var combinedTimeline = activeTimeline.Concat([action]).ToArray();
        var dropCount = Math.Max(combinedTimeline.Length - MaxTimelineEntries, 0);
        var baseline = dropCount == 0
            ? Baseline
            : ReplaySnapshot(Baseline, combinedTimeline.Take(dropCount));
        var retainedTimeline = combinedTimeline.Skip(dropCount).ToArray();
        var current = ReplaySnapshot(baseline, retainedTimeline);
        var checkpoints = BuildCheckpoints(baseline, retainedTimeline, CheckpointInterval);
        return FromSnapshot(current, baseline, retainedTimeline, retainedTimeline.Length, checkpoints, CheckpointInterval);
    }

    public PaneWorkspaceState Replay(IEnumerable<PaneWorkspaceAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var state = FromSnapshot(Baseline, Baseline, [], 0, [], CheckpointInterval);
        foreach (var action in actions)
        {
            state = state.Apply(action);
        }

        return state;
    }

    public PaneWorkspaceSnapshot ToSnapshot() => new(Root, SelectedPaneId, Mode, PrimaryRatioPermille);

    public string SnapshotHash()
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(ToJson());
        return Convert.ToHexString(sha.ComputeHash(bytes))[..12];
    }

    public string ToJson() =>
        JsonSerializer.Serialize(
            this,
            CanonicalJsonOptions);

    public string ToCanonicalJson()
    {
        var sourceIssues = Validate();
        if (sourceIssues.Count > 0)
        {
            throw new InvalidOperationException($"Pane workspace snapshot invalid: {string.Join("; ", sourceIssues)}");
        }

        var restored = Restore(
            Timeline,
            Checkpoints.Count == 0
                ? BuildCheckpoints(Baseline, Timeline, CheckpointInterval)
                : Checkpoints,
            TimelineCursor,
            Baseline,
            CheckpointInterval);
        var issues = restored.Validate();
        if (issues.Count > 0)
        {
            throw new InvalidOperationException($"Pane workspace snapshot invalid: {string.Join("; ", issues)}");
        }

        return restored.ToJson();
    }

    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();
        var nodes = Flatten(Root).ToArray();
        if (nodes.Length == 0)
        {
            issues.Add("root tree must contain at least one pane");
        }

        if (nodes.Any(static node => string.IsNullOrWhiteSpace(node.Id)))
        {
            issues.Add("pane ids must be non-empty");
        }

        if (nodes.GroupBy(static node => node.Id, StringComparer.Ordinal).Any(static group => group.Count() > 1))
        {
            issues.Add("pane ids must be unique");
        }

        var leaves = nodes.Where(static node => node.IsLeaf).ToArray();
        if (leaves.Length == 0)
        {
            issues.Add("root tree must contain at least one leaf pane");
        }

        if (leaves.Length > 0 && !leaves.Any(leaf => string.Equals(leaf.Id, SelectedPaneId, StringComparison.Ordinal)))
        {
            issues.Add($"selected pane '{SelectedPaneId}' was not found in leaf panes");
        }

        if (PrimaryRatioPermille is < 250 or > 750)
        {
            issues.Add("primary ratio must be between 250 and 750 permille");
        }

        if (TimelineCursor < 0 || TimelineCursor > Timeline.Count)
        {
            issues.Add($"timeline cursor {TimelineCursor} out of range for {Timeline.Count} entries");
        }

        if (CheckpointInterval <= 0)
        {
            issues.Add("checkpoint interval must be positive");
        }

        if (Checkpoints.Any(checkpoint => checkpoint.AppliedCount < 0 || checkpoint.AppliedCount > Timeline.Count))
        {
            issues.Add("checkpoint applied count is outside the timeline");
        }

        return issues;
    }

    public static PaneWorkspaceState FromJson(string json)
    {
        return DecodeJson(json).State;
    }

    public static PaneWorkspaceJsonMigrationResult DecodeJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var state = JsonSerializer.Deserialize<PaneWorkspaceState>(
                        json,
                        CanonicalJsonOptions) ??
                    throw new InvalidOperationException("Could not deserialize pane workspace state.");

        var sourceIssues = state.Validate();
        if (sourceIssues.Count > 0)
        {
            throw new InvalidOperationException($"Pane workspace snapshot invalid: {string.Join("; ", sourceIssues)}");
        }

        var restored = Restore(
            state.Timeline,
            state.Checkpoints.Count == 0
                ? BuildCheckpoints(state.Baseline, state.Timeline, state.CheckpointInterval)
                : state.Checkpoints,
            state.TimelineCursor,
            state.Baseline,
            state.CheckpointInterval);
        var issues = restored.Validate();
        if (issues.Count > 0)
        {
            throw new InvalidOperationException($"Pane workspace snapshot invalid: {string.Join("; ", issues)}");
        }

        return new PaneWorkspaceJsonMigrationResult(
            restored,
            CurrentJsonSchemaVersion,
            CurrentJsonSchemaVersion,
            MigrationApplied: false,
            Warnings: []);
    }

    private static PaneWorkspaceState Restore(
        IReadOnlyList<PaneWorkspaceAction> timeline,
        IReadOnlyList<PaneWorkspaceCheckpoint> checkpoints,
        int cursor,
        PaneWorkspaceSnapshot baseline,
        int checkpointInterval)
    {
        var effectiveCursor = Math.Clamp(cursor, 0, timeline.Count);
        var replayCheckpoint = checkpoints
            .Where(checkpoint => checkpoint.AppliedCount <= effectiveCursor)
            .LastOrDefault();
        var replayStartIndex = replayCheckpoint?.AppliedCount ?? 0;
        var replayBaseline = replayCheckpoint?.Snapshot ?? baseline;
        var current = ReplaySnapshot(replayBaseline, timeline.Skip(replayStartIndex).Take(effectiveCursor - replayStartIndex));
        return FromSnapshot(current, baseline, timeline, effectiveCursor, checkpoints, checkpointInterval);
    }

    private static PaneWorkspaceSnapshot ReplaySnapshot(
        PaneWorkspaceSnapshot baseline,
        IEnumerable<PaneWorkspaceAction> actions)
    {
        var snapshot = baseline;
        foreach (var action in actions)
        {
            snapshot = ApplyToSnapshot(snapshot, action);
        }

        return snapshot;
    }

    private static IReadOnlyList<PaneWorkspaceCheckpoint> BuildCheckpoints(
        PaneWorkspaceSnapshot baseline,
        IReadOnlyList<PaneWorkspaceAction> timeline,
        int checkpointInterval)
    {
        if (checkpointInterval <= 0 || timeline.Count == 0)
        {
            return [];
        }

        var checkpoints = new List<PaneWorkspaceCheckpoint>();
        var snapshot = baseline;
        for (var index = 0; index < timeline.Count; index++)
        {
            snapshot = ApplyToSnapshot(snapshot, timeline[index]);
            if ((index + 1) % checkpointInterval == 0)
            {
                checkpoints.Add(new PaneWorkspaceCheckpoint(index + 1, snapshot));
            }
        }

        return checkpoints;
    }

    private static PaneWorkspaceSnapshot ApplyToSnapshot(PaneWorkspaceSnapshot snapshot, PaneWorkspaceAction action)
    {
        var leaves = Flatten(snapshot.Root).Where(static node => node.IsLeaf).ToArray();
        var selectedIndex = Array.FindIndex(leaves, leaf => string.Equals(leaf.Id, snapshot.SelectedPaneId, StringComparison.Ordinal));
        selectedIndex = selectedIndex < 0 ? 0 : selectedIndex;

        return action.Kind switch
        {
            PaneWorkspaceActionKind.SelectNext => snapshot with
            {
                SelectedPaneId = leaves[(selectedIndex + 1) % leaves.Length].Id
            },
            PaneWorkspaceActionKind.SelectPrevious => snapshot with
            {
                SelectedPaneId = leaves[(selectedIndex - 1 + leaves.Length) % leaves.Length].Id
            },
            PaneWorkspaceActionKind.CycleMode => snapshot with
            {
                Mode = NextMode(snapshot.Mode)
            },
            PaneWorkspaceActionKind.GrowPrimary => snapshot with
            {
                PrimaryRatioPermille = Math.Clamp(snapshot.PrimaryRatioPermille + 50, 250, 750)
            },
            PaneWorkspaceActionKind.ShrinkPrimary => snapshot with
            {
                PrimaryRatioPermille = Math.Clamp(snapshot.PrimaryRatioPermille - 50, 250, 750)
            },
            _ => snapshot
        };
    }

    private static PaneWorkspaceState FromSnapshot(
        PaneWorkspaceSnapshot snapshot,
        PaneWorkspaceSnapshot baseline,
        IReadOnlyList<PaneWorkspaceAction> timeline,
        int timelineCursor,
        IReadOnlyList<PaneWorkspaceCheckpoint> checkpoints,
        int checkpointInterval) =>
        new(
            snapshot.Root,
            snapshot.SelectedPaneId,
            snapshot.Mode,
            snapshot.PrimaryRatioPermille,
            timeline,
            timelineCursor,
            baseline,
            checkpoints,
            checkpointInterval);

    private static PaneWorkspaceMode NextMode(PaneWorkspaceMode mode) => mode switch
    {
        PaneWorkspaceMode.Focus => PaneWorkspaceMode.Compare,
        PaneWorkspaceMode.Compare => PaneWorkspaceMode.Monitor,
        PaneWorkspaceMode.Monitor => PaneWorkspaceMode.Compact,
        _ => PaneWorkspaceMode.Focus
    };

    private static IEnumerable<PaneWorkspaceNode> Flatten(PaneWorkspaceNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var nested in Flatten(child))
            {
                yield return nested;
            }
        }
    }
}
