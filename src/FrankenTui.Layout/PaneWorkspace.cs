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
    ShrinkPrimary
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

public sealed record PaneWorkspaceState(
    PaneWorkspaceNode Root,
    string SelectedPaneId,
    PaneWorkspaceMode Mode,
    int PrimaryRatioPermille = 500,
    IReadOnlyList<PaneWorkspaceAction>? Timeline = null,
    int TimelineCursor = 0)
{
    public IReadOnlyList<PaneWorkspaceAction> Timeline { get; init; } = Timeline ?? [];

    public static PaneWorkspaceState CreateDemo() =>
        new(
            new PaneWorkspaceNode(
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
                ]),
            "editor",
            PaneWorkspaceMode.Compare);

    public IReadOnlyList<PaneWorkspaceNode> FlattenLeaves() =>
        Flatten(Root).Where(static node => node.IsLeaf).ToArray();

    public PaneWorkspaceState Apply(PaneWorkspaceAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var leaves = FlattenLeaves();
        var selectedIndex = Array.FindIndex(leaves.ToArray(), leaf => string.Equals(leaf.Id, SelectedPaneId, StringComparison.Ordinal));
        selectedIndex = selectedIndex < 0 ? 0 : selectedIndex;

        return action.Kind switch
        {
            PaneWorkspaceActionKind.SelectNext => this with
            {
                SelectedPaneId = leaves[(selectedIndex + 1) % leaves.Count].Id,
                Timeline = Append(Timeline, action),
                TimelineCursor = TimelineCursor + 1
            },
            PaneWorkspaceActionKind.SelectPrevious => this with
            {
                SelectedPaneId = leaves[(selectedIndex - 1 + leaves.Count) % leaves.Count].Id,
                Timeline = Append(Timeline, action),
                TimelineCursor = TimelineCursor + 1
            },
            PaneWorkspaceActionKind.CycleMode => this with
            {
                Mode = NextMode(Mode),
                Timeline = Append(Timeline, action),
                TimelineCursor = TimelineCursor + 1
            },
            PaneWorkspaceActionKind.GrowPrimary => this with
            {
                PrimaryRatioPermille = Math.Clamp(PrimaryRatioPermille + 50, 250, 750),
                Timeline = Append(Timeline, action),
                TimelineCursor = TimelineCursor + 1
            },
            PaneWorkspaceActionKind.ShrinkPrimary => this with
            {
                PrimaryRatioPermille = Math.Clamp(PrimaryRatioPermille - 50, 250, 750),
                Timeline = Append(Timeline, action),
                TimelineCursor = TimelineCursor + 1
            },
            _ => this
        };
    }

    public PaneWorkspaceState Replay(IEnumerable<PaneWorkspaceAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var replayed = this with { Timeline = [], TimelineCursor = 0 };
        foreach (var action in actions)
        {
            replayed = replayed.Apply(action);
        }

        return replayed;
    }

    public string SnapshotHash()
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(ToJson());
        return Convert.ToHexString(sha.ComputeHash(bytes))[..12];
    }

    public string ToJson() =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });

    public static PaneWorkspaceState FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<PaneWorkspaceState>(
                   json,
                   new JsonSerializerOptions
                   {
                       PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                   }) ??
               throw new InvalidOperationException("Could not deserialize pane workspace state.");
    }

    private static IReadOnlyList<PaneWorkspaceAction> Append(IReadOnlyList<PaneWorkspaceAction> timeline, PaneWorkspaceAction action) =>
        timeline.Concat([action]).TakeLast(24).ToArray();

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
