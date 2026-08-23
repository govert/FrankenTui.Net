// Tests ported from .external/frankentui/crates/ftui-a11y/tests/a11y_tests.rs
// and .external/frankentui/crates/ftui-a11y/src/preferences.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust Display checks call the managed ToDisplayString extensions.

using System.Text.Json;
using FrankenTui.A11y;
using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class A11yTests
{
    [Fact]
    public void NodeContractHasUpstreamDefaultsAndBuilderFields()
    {
        Rect bounds = new(1, 2, 30, 4);
        A11yNodeInfo minimal = A11yNodeInfo.New(42, A11yRole.Button, bounds);

        Assert.Equal(42UL, minimal.Id);
        Assert.Equal(A11yRole.Button, minimal.Role);
        Assert.Equal(bounds, minimal.Bounds);
        Assert.Null(minimal.Name);
        Assert.Null(minimal.Description);
        Assert.Empty(minimal.Children);
        Assert.Null(minimal.Parent);
        Assert.Null(minimal.Shortcut);
        Assert.Equal(new A11yState(), minimal.State);
        Assert.Null(minimal.LiveRegion);

        var state = new A11yState
        {
            Focused = true,
            Checked = true,
            ValueNow = 50,
            ValueText = "50%",
        };
        A11yNodeInfo full = minimal
            .WithName("Submit")
            .WithDescription("Submit the form")
            .WithShortcut("Ctrl+Enter")
            .WithLiveRegion(LiveRegion.Assertive)
            .WithChildren([100, 101])
            .WithParent(7)
            .WithState(state);

        Assert.Equal("Submit", full.Name);
        Assert.Equal("Submit the form", full.Description);
        Assert.Equal("Ctrl+Enter", full.Shortcut);
        Assert.Equal(LiveRegion.Assertive, full.LiveRegion);
        Assert.Equal([100UL, 101UL], full.Children);
        Assert.Equal(7UL, full.Parent);
        Assert.Equal(state, full.State);
        Assert.Null(minimal.Name); // builder calls preserve immutable snapshots
    }

    [Fact]
    public void StateDefaultIsAllUnset()
    {
        var state = new A11yState();

        Assert.False(state.Focused);
        Assert.False(state.Disabled);
        Assert.Null(state.Checked);
        Assert.Null(state.Expanded);
        Assert.False(state.Selected);
        Assert.False(state.Readonly);
        Assert.False(state.Required);
        Assert.False(state.Busy);
        Assert.Null(state.ValueNow);
        Assert.Null(state.ValueMin);
        Assert.Null(state.ValueMax);
        Assert.Null(state.ValueText);
    }

    [Fact]
    public void RolesExposeExactInteractiveAndDisplayContracts()
    {
        A11yRole[] interactive =
        [
            A11yRole.Button,
            A11yRole.TextInput,
            A11yRole.Checkbox,
            A11yRole.RadioButton,
            A11yRole.Slider,
            A11yRole.Tab,
            A11yRole.MenuItem,
        ];
        Assert.All(interactive, role => Assert.True(role.IsInteractive()));
        Assert.False(A11yRole.Window.IsInteractive());
        Assert.False(A11yRole.Dialog.IsInteractive());
        Assert.False(A11yRole.Label.IsInteractive());
        Assert.False(A11yRole.List.IsInteractive());
        Assert.False(A11yRole.Table.IsInteractive());
        Assert.False(A11yRole.ProgressBar.IsInteractive());
        Assert.False(A11yRole.Presentation.IsInteractive());

        string[] displays = Enum.GetValues<A11yRole>()
            .Select(role => role.ToDisplayString())
            .ToArray();
        Assert.Equal(23, displays.Length);
        Assert.Equal("textInput", A11yRole.TextInput.ToDisplayString());
        Assert.Equal("radioButton", A11yRole.RadioButton.ToDisplayString());
        Assert.Equal("scrollBar", A11yRole.ScrollBar.ToDisplayString());
        Assert.All(displays, display => Assert.NotEmpty(display));
        Assert.Equal("polite", LiveRegion.Polite.ToDisplayString());
        Assert.Equal("assertive", LiveRegion.Assertive.ToDisplayString());
    }

    [Fact]
    public void BuilderSupportsEmptyCapacityInsertAndReplacement()
    {
        Assert.True(A11yTreeBuilder.New().Build().IsEmpty);
        Assert.True(A11yTreeBuilder.WithCapacity(32).Build().IsEmpty);
        Assert.True(new A11yTreeBuilder().Build().IsEmpty);

        var builder = new A11yTreeBuilder();
        builder.AddNode(Node(1, A11yRole.Label, "old"));
        builder.AddNode(Node(1, A11yRole.Button, "new"));
        builder.SetRoot(1);
        builder.SetFocused(1);
        A11yTree tree = builder.Build();

        Assert.Equal(1, tree.NodeCount);
        Assert.Equal(A11yRole.Button, tree.Node(1)!.Role);
        Assert.Equal("new", tree.Root!.Name);
        Assert.Equal(1UL, tree.RootId);
        Assert.Equal(1UL, tree.FocusedId);
        Assert.Equal("new", tree.Focused!.Name);

        builder.AddNode(Node(1, A11yRole.Label, "later"));
        Assert.Equal("new", tree.Node(1)!.Name); // Build() freezes by copying.
    }

    [Fact]
    public void TreeChildrenAncestorsAndNodesFollowStoredRelationships()
    {
        A11yTree tree = BuildSampleTree();

        Assert.Equal([2UL, 3UL], tree.ChildrenOf(1).Select(node => node.Id));
        Assert.Equal([4UL], tree.ChildrenOf(2).Select(node => node.Id));
        Assert.Empty(tree.ChildrenOf(4));
        Assert.Empty(tree.ChildrenOf(999));
        Assert.Equal([4UL, 2UL, 1UL], tree.Ancestors(4));
        Assert.Equal([1UL], tree.Ancestors(1));
        Assert.Empty(tree.Ancestors(999));
        Assert.Equal([1UL, 2UL, 3UL, 4UL], tree.Nodes().Select(node => node.Id).Order());
    }

    [Fact]
    public void TreeHandlesDanglingRootAndFocusReferences()
    {
        var builder = new A11yTreeBuilder();
        builder.AddNode(Node(1, A11yRole.Window));
        builder.SetRoot(998);
        builder.SetFocused(999);
        A11yTree tree = builder.Build();

        Assert.Equal(998UL, tree.RootId);
        Assert.Null(tree.Root);
        Assert.Equal(999UL, tree.FocusedId);
        Assert.Null(tree.Focused);
    }

    [Fact]
    public void AncestorsProtectAgainstCycles()
    {
        var cycle = new A11yTreeBuilder();
        cycle.AddNode(Node(1, A11yRole.Group).WithParent(2).WithChildren([2]));
        cycle.AddNode(Node(2, A11yRole.Group).WithParent(1).WithChildren([1]));
        cycle.SetRoot(1);
        Assert.Equal([1UL, 2UL], cycle.Build().Ancestors(1));

        var selfCycle = new A11yTreeBuilder();
        selfCycle.AddNode(Node(1, A11yRole.Window).WithParent(1));
        selfCycle.SetRoot(1);
        Assert.Equal([1UL], selfCycle.Build().Ancestors(1));
    }

    [Fact]
    public void DiffIdenticalTreesIsEmptyAndFocusTransitionsAreTyped()
    {
        A11yTree before = BuildSampleTree();
        A11yTree same = BuildSampleTree();
        Assert.True(same.Diff(before).IsEmpty);

        A11yTree gained = BuildSampleTree(focused: 2);
        Assert.Equal((null, 2UL), gained.Diff(before).FocusChanged);

        A11yTree moved = BuildSampleTree(focused: 3);
        Assert.Equal((2UL, 3UL), moved.Diff(gained).FocusChanged);

        Assert.Equal((3UL, null), before.Diff(moved).FocusChanged);
    }

    [Fact]
    public void DiffOrdersAddedRemovedAndChangedNodeIds()
    {
        A11yTree before = BuildTree(
            Node(30, A11yRole.Label, "old-30"),
            Node(10, A11yRole.Label, "old-10"),
            Node(50, A11yRole.Label, "removed"));
        A11yTree after = BuildTree(
            Node(30, A11yRole.Label, "new-30"),
            Node(10, A11yRole.Label, "new-10"),
            Node(40, A11yRole.Label, "added-40"),
            Node(20, A11yRole.Label, "added-20"));

        A11yTreeDiff diff = after.Diff(before);
        Assert.Equal([20UL, 40UL], diff.Added);
        Assert.Equal([50UL], diff.Removed);
        Assert.Equal([10UL, 30UL], diff.Changed.Select(change => change.Id));
    }

    [Fact]
    public void DiffDetectsEveryNodePropertyAndStateField()
    {
        A11yNodeInfo old = A11yNodeInfo.New(1, A11yRole.Checkbox, new Rect(0, 0, 3, 1))
            .WithName("Old")
            .WithDescription("Old description")
            .WithShortcut("Ctrl+O")
            .WithChildren([2])
            .WithParent(9)
            .WithLiveRegion(LiveRegion.Polite)
            .WithState(new A11yState
            {
                Checked = false,
                Expanded = false,
                ValueNow = 1,
                ValueMin = 0,
                ValueMax = 10,
                ValueText = "one",
            });
        A11yNodeInfo current = A11yNodeInfo.New(1, A11yRole.Slider, new Rect(1, 2, 4, 1))
            .WithName("New")
            .WithDescription("New description")
            .WithShortcut("Ctrl+N")
            .WithChildren([3])
            .WithParent(8)
            .WithLiveRegion(LiveRegion.Assertive)
            .WithState(new A11yState
            {
                Focused = true,
                Disabled = true,
                Checked = true,
                Expanded = true,
                Selected = true,
                Readonly = true,
                Required = true,
                Busy = true,
                ValueNow = 2,
                ValueMin = -1,
                ValueMax = 20,
                ValueText = "two",
            });

        A11yTreeDiff diff = BuildTree(current).Diff(BuildTree(old));
        IReadOnlyList<A11yChange> changes = Assert.Single(diff.Changed).Changes;

        Assert.Contains(changes, change => change is A11yChange.NameChanged("Old", "New"));
        Assert.Contains(changes, change => change is A11yChange.RoleChanged(A11yRole.Checkbox, A11yRole.Slider));
        Assert.Contains(changes, change => change is A11yChange.BoundsChanged);
        Assert.Contains(changes, change => change is A11yChange.ChildrenChanged);
        Assert.Contains(changes, change => change is A11yChange.LiveRegionChanged(LiveRegion.Polite, LiveRegion.Assertive));
        Assert.Contains(changes, change => change is A11yChange.DescriptionChanged("Old description", "New description"));
        Assert.Contains(changes, change => change is A11yChange.ShortcutChanged("Ctrl+O", "Ctrl+N"));
        Assert.Contains(changes, change => change is A11yChange.ParentChanged(9, 8));

        string[] stateFields = changes
            .OfType<A11yChange.StateChanged>()
            .Select(change => change.Field)
            .ToArray();
        Assert.Equal(
            [
                "focused", "disabled", "checked", "expanded", "selected", "readonly",
                "required", "busy", "value_now", "value_min", "value_max", "value_text",
            ],
            stateFields);
        Assert.Contains(changes, change => change is A11yChange.StateChanged("focused", "true"));
        Assert.Contains(changes, change => change is A11yChange.StateChanged("checked", "Some(true)"));
        Assert.Contains(changes, change => change is A11yChange.StateChanged("value_now", "Some(2.0)"));
    }

    [Fact]
    public void MirrorUsesChildOrderPresentationFilteringAndBounds()
    {
        var builder = new A11yTreeBuilder();
        builder.AddNode(Node(10, A11yRole.Window, "App").WithChildren([30, 40, 20]));
        builder.AddNode(Node(20, A11yRole.Button, "Cancel").WithParent(10));
        builder.AddNode(Node(30, A11yRole.Button, "Save").WithParent(10));
        builder.AddNode(Node(40, A11yRole.Presentation).WithParent(10));
        builder.SetRoot(10);
        builder.SetFocused(30);

        ScreenReaderMirror mirror = builder.Build().ScreenReaderMirror(new ScreenReaderPolicy(2, 8, 80));

        Assert.Equal(["window: App", "  button: Save. focused"], mirror.Lines);
        Assert.Equal(1, mirror.OmittedNodes);
        Assert.Equal("window: App\n  button: Save. focused", mirror.Text());
        Assert.Equal(
            mirror,
            builder.Build().ScreenReaderMirror(new ScreenReaderPolicy(2, 8, 80)));
    }

    [Fact]
    public void MirrorAppendsDisconnectedNodesBySortedId()
    {
        var builder = new A11yTreeBuilder();
        builder.AddNode(Node(100, A11yRole.Window));
        builder.AddNode(Node(3, A11yRole.Label, "Three"));
        builder.AddNode(Node(2, A11yRole.Label, "Two"));
        builder.SetRoot(100);

        Assert.Equal(
            ["window", "label: Two", "label: Three"],
            builder.Build().ScreenReaderMirror(ScreenReaderPolicy.Default).Lines);
    }

    [Fact]
    public void MirrorNormalizesStateTextAndLimitsUnicodeScalars()
    {
        A11yNodeInfo node = Node(1, A11yRole.TextInput, "  Search\tfield  ")
            .WithDescription("Search\nfield")
            .WithShortcut(" Ctrl+F ")
            .WithLiveRegion(LiveRegion.Polite)
            .WithState(new A11yState
            {
                Focused = true,
                Disabled = true,
                Required = true,
                ValueText = "  ready  now ",
            });
        A11yTree tree = BuildTree(node);

        string full = Assert.Single(tree.ScreenReaderMirror(ScreenReaderPolicy.Default).Lines);
        Assert.Equal(
            "textInput: Search field. focused, disabled, required, value ready now. shortcut Ctrl+F. live polite",
            full);
        string limited = Assert.Single(tree.ScreenReaderMirror(new ScreenReaderPolicy(1, 8, 5)).Lines);
        Assert.Equal("textI", limited);

        A11yTree emoji = BuildTree(Node(1, A11yRole.Label, "😀😀"));
        Assert.Equal("label:", Assert.Single(
            emoji.ScreenReaderMirror(new ScreenReaderPolicy(1, 8, 6)).Lines));
    }

    [Fact]
    public void AnnouncementsIncludeFocusThenLiveContentChanges()
    {
        A11yTree before = BuildTree(
            Node(1, A11yRole.Button, "Save"),
            Node(2, A11yRole.Label, "Loading").WithLiveRegion(LiveRegion.Polite));
        var afterBuilder = new A11yTreeBuilder();
        afterBuilder.AddNode(Node(1, A11yRole.Button, "Save"));
        afterBuilder.AddNode(Node(2, A11yRole.Label, "Loaded").WithLiveRegion(LiveRegion.Polite));
        afterBuilder.SetRoot(1);
        afterBuilder.SetFocused(1);

        ScreenReaderAnnouncements batch = afterBuilder.Build()
            .ScreenReaderAnnouncementsSince(before, ScreenReaderPolicy.Default);

        Assert.Equal(0, batch.DroppedCount);
        Assert.Collection(
            batch.Announcements,
            announcement =>
            {
                Assert.Equal(1UL, announcement.NodeId);
                Assert.Equal(AnnouncementReason.FocusChanged, announcement.Reason);
                Assert.Equal("button: Save. focused", announcement.Text);
            },
            announcement =>
            {
                Assert.Equal(2UL, announcement.NodeId);
                Assert.Equal(AnnouncementReason.LiveContentChanged, announcement.Reason);
                Assert.Equal("label: Loaded", announcement.Text);
            });
    }

    [Fact]
    public void AnnouncementsAreBoundedAndPrioritizeAssertiveNodeIds()
    {
        var after = new A11yTreeBuilder();
        after.AddNode(Node(1, A11yRole.Window));
        foreach ((ulong id, LiveRegion region, string name) in new[]
        {
            (30UL, LiveRegion.Polite, "Announcement 30 with extra detail"),
            (10UL, LiveRegion.Polite, "Announcement 10 with extra detail"),
            (20UL, LiveRegion.Assertive, "Announcement 20 with extra detail"),
            (40UL, LiveRegion.Assertive, "Announcement 40 with extra detail"),
        })
        {
            after.AddNode(Node(id, A11yRole.Label, name).WithLiveRegion(region));
        }
        after.SetRoot(1);

        ScreenReaderAnnouncements batch = after.Build().ScreenReaderAnnouncementsSince(
            A11yTree.Empty(),
            new ScreenReaderPolicy(128, 3, 18));

        Assert.Equal([20UL, 40UL, 10UL], batch.Announcements.Select(item => item.NodeId!.Value));
        Assert.Equal(1, batch.DroppedCount);
        Assert.All(batch.Announcements, item => Assert.True(item.Text.EnumerateRunes().Count() <= 18));
    }

    [Fact]
    public void AnnouncementsSkipEmptyLiveRegionsAndPresentationNodes()
    {
        var after = new A11yTreeBuilder();
        after.AddNode(Node(1, A11yRole.Presentation, "Decorative")
            .WithLiveRegion(LiveRegion.Assertive));
        after.AddNode(Node(2, A11yRole.Label).WithLiveRegion(LiveRegion.Polite));
        after.SetRoot(1);
        after.SetFocused(1);

        ScreenReaderAnnouncements batch = after.Build()
            .ScreenReaderAnnouncementsSince(A11yTree.Empty(), ScreenReaderPolicy.Default);
        Assert.Empty(batch.Announcements);
        Assert.Equal(0, batch.DroppedCount);
    }

    [Fact]
    public void LiveRegionPolicyChangeTakesReasonPrecedence()
    {
        A11yTree before = BuildTree(
            Node(1, A11yRole.Label, "Loading").WithLiveRegion(LiveRegion.Polite));
        A11yTree after = BuildTree(
            Node(1, A11yRole.Label, "Loaded").WithLiveRegion(LiveRegion.Assertive));

        ScreenReaderAnnouncement announcement = Assert.Single(
            after.ScreenReaderAnnouncementsSince(before, ScreenReaderPolicy.Default).Announcements);
        Assert.Equal(AnnouncementReason.LiveRegionChanged, announcement.Reason);
        Assert.Equal(LiveRegion.Assertive, announcement.Urgency);
    }

    [Fact]
    public void AccessibleInterfaceSupportsPrimaryAndInternalNodes()
    {
        IAccessible button = new FakeButton(100, "Submit");
        A11yNodeInfo buttonNode = Assert.Single(
            button.AccessibilityNodes(new Rect(5, 10, 12, 1)));
        Assert.Equal(100UL, buttonNode.Id);
        Assert.Equal(A11yRole.Button, buttonNode.Role);
        Assert.Equal("Submit", buttonNode.Name);
        Assert.Equal(new Rect(5, 10, 12, 1), buttonNode.Bounds);

        IAccessible list = new FakeList(200, [(201, "Alpha"), (202, "Beta"), (203, "Gamma")]);
        List<A11yNodeInfo> nodes = list.AccessibilityNodes(new Rect(0, 0, 30, 10));
        Assert.Equal(4, nodes.Count);
        Assert.Equal(A11yRole.List, nodes[0].Role);
        Assert.Equal([201UL, 202UL, 203UL], nodes[0].Children);
        Assert.All(nodes.Skip(1), node =>
        {
            Assert.Equal(A11yRole.ListItem, node.Role);
            Assert.Equal(200UL, node.Parent);
        });

        var builder = new A11yTreeBuilder();
        foreach (A11yNodeInfo node in button.AccessibilityNodes(new Rect(0, 0, 5, 1)))
            builder.AddNode(node);
        builder.SetRoot(100);
        builder.SetFocused(100);
        Assert.Equal("Submit", builder.Build().Focused!.Name);
    }

    [Fact]
    public void LegacySnapshotCallersRemainSourceCompatible()
    {
        AccessibilitySnapshot snapshot = new AccessibilitySnapshot()
            .Add("button", "Save")
            .Add("status", "Ready", "No pending changes");

        Assert.Equal(
            [
                new AccessibleNode("button", "Save"),
                new AccessibleNode("status", "Ready", "No pending changes"),
            ],
            snapshot.Nodes);
    }

    [Fact]
    public void PreferencesConstructionBuildersAndDatasetAreDeterministic()
    {
        AccessibilityPreferences none = AccessibilityPreferences.None();
        Assert.Equal(default, none);
        Assert.False(none.Any);

        AccessibilityPreferences all = AccessibilityPreferences.All();
        Assert.True(all.Any);
        Assert.True(all.ReducedMotion);
        Assert.True(all.HighContrast);
        Assert.True(all.ForcedColors);

        AccessibilityPreferences media = AccessibilityPreferences.FromMediaQueries(true, false, true);
        Assert.True(media.ReducedMotion);
        Assert.False(media.HighContrast);
        Assert.True(media.ForcedColors);
        Assert.Equal(
            [
                ("data-ftui-reduced-motion", true),
                ("data-ftui-high-contrast", false),
                ("data-ftui-forced-colors", true),
            ],
            media.WebDataset());

        AccessibilityPreferences composed = AccessibilityPreferences.None()
            .WithReducedMotion(true)
            .WithHighContrast(true)
            .WithForcedColors(false);
        Assert.Equal(new AccessibilityPreferences(true, true, false), composed);
    }

    [Fact]
    public void MotionProfilesScaleDurationsAndRemainOrthogonalToContrast()
    {
        MotionProfile full = AccessibilityPreferences.None().MotionProfile();
        Assert.Equal(MotionProfile.Full, full);
        Assert.Equal(250U, full.ScaledDurationMs(250));
        Assert.Equal(0U, full.ScaledDurationMs(0));
        Assert.Equal(1_000_000U, full.ScaledDurationMs(1_000_000));

        MotionProfile reduced = AccessibilityPreferences.None()
            .WithReducedMotion(true)
            .MotionProfile();
        Assert.True(reduced.ReducedMotion);
        Assert.Equal(0, reduced.AnimationScaleBps);
        Assert.True(reduced.AnnouncementCoalescing);
        Assert.True(reduced.SuppressTransient);
        Assert.Equal(0U, reduced.ScaledDurationMs(uint.MaxValue));

        Assert.Equal(
            MotionProfile.Full,
            AccessibilityPreferences.None()
                .WithHighContrast(true)
                .WithForcedColors(true)
                .MotionProfile());
    }

    [Fact]
    public void ContrastProfilesImplementAaAaaAndForcedColorRules()
    {
        ContrastProfile baseline = AccessibilityPreferences.None().ContrastProfile();
        Assert.Equal(ContrastProfile.Default, baseline);
        Assert.Equal(ContrastProfile.WcagAaX100, baseline.MinContrastRatioX100);
        Assert.False(baseline.DropDim);

        ContrastProfile high = AccessibilityPreferences.None()
            .WithHighContrast(true)
            .ContrastProfile();
        Assert.True(high.HighContrast);
        Assert.Equal(ContrastProfile.WcagAaaX100, high.MinContrastRatioX100);
        Assert.True(high.DropDim);
        Assert.False(high.SystemColorsOnly);

        ContrastProfile forced = AccessibilityPreferences.None()
            .WithForcedColors(true)
            .ContrastProfile();
        Assert.True(forced.HighContrast);
        Assert.True(forced.ForcedColors);
        Assert.True(forced.DropDim);
        Assert.True(forced.SystemColorsOnly);

        Assert.Equal(
            ContrastProfile.Default,
            AccessibilityPreferences.None().WithReducedMotion(true).ContrastProfile());
    }

    [Fact]
    public void PreferenceJsonlIsExactDeterministicAndJsonSafe()
    {
        AccessibilityPreferences preferences = AccessibilityPreferences.All();
        string line = preferences.ToJsonl("corr-1");
        Assert.Equal(line, preferences.ToJsonl("corr-1"));
        Assert.Equal(
            "{\"event\":\"a11y_preferences\",\"correlation_id\":\"corr-1\"," +
            "\"reduced_motion\":true,\"high_contrast\":true,\"forced_colors\":true," +
            "\"animation_scale_bps\":0,\"announcement_coalescing\":true," +
            "\"suppress_transient\":true,\"min_contrast_ratio_x100\":700," +
            "\"drop_dim\":true,\"system_colors_only\":true}",
            line);
        using JsonDocument _ = JsonDocument.Parse(line);
        Assert.DoesNotContain('\n', line);

        string escaped = AccessibilityPreferences.None().ToJsonl("a\"b\\c\nd\u0001");
        Assert.Contains("\"correlation_id\":\"a\\\"b\\\\c\\nd\\u0001\"", escaped);
        using JsonDocument __ = JsonDocument.Parse(escaped);
    }

    [Fact]
    public void FullMotionPassesAnnouncementsThrough()
    {
        ScreenReaderAnnouncements batch = Batch(
            3,
            Announcement(1, LiveRegion.Polite, AnnouncementReason.FocusChanged, "button: ok"),
            Announcement(2, LiveRegion.Assertive, AnnouncementReason.LiveContentChanged, "progress 10%"));

        MotionFilteredAnnouncements output = MotionProfile.Full.FilterAnnouncements(batch);
        Assert.Equal(batch.Announcements, output.Announcements);
        Assert.Equal(3, output.DroppedCount);
        Assert.Equal(0, output.CoalescedCount);
        Assert.Equal(0, output.DowngradedCount);
    }

    [Fact]
    public void ReducedMotionCoalescesLatestAndDowngradesAssertiveChurn()
    {
        MotionProfile motion = AccessibilityPreferences.All().MotionProfile();
        ScreenReaderAnnouncements batch = Batch(
            0,
            Announcement(5, LiveRegion.Assertive, AnnouncementReason.LiveContentChanged, "progress 10%"),
            Announcement(5, LiveRegion.Assertive, AnnouncementReason.LiveContentChanged, "progress 50%"),
            Announcement(5, LiveRegion.Assertive, AnnouncementReason.LiveContentChanged, "progress 90%"));

        MotionFilteredAnnouncements output = motion.FilterAnnouncements(batch);
        ScreenReaderAnnouncement kept = Assert.Single(output.Announcements);
        Assert.Equal("progress 90%", kept.Text);
        Assert.Equal(LiveRegion.Polite, kept.Urgency);
        Assert.Equal(2, output.CoalescedCount);
        Assert.Equal(1, output.DowngradedCount);
    }

    [Fact]
    public void ReducedMotionPreservesIntentionalAnnouncementsAndDistinctNodes()
    {
        MotionProfile motion = AccessibilityPreferences.All().MotionProfile();
        ScreenReaderAnnouncements batch = Batch(
            2,
            Announcement(1, LiveRegion.Assertive, AnnouncementReason.FocusChanged, "focus"),
            Announcement(2, LiveRegion.Assertive, AnnouncementReason.LiveRegionChanged, "region"),
            Announcement(10, LiveRegion.Polite, AnnouncementReason.LiveContentChanged, "a1"),
            Announcement(11, LiveRegion.Polite, AnnouncementReason.LiveContentChanged, "b1"),
            Announcement(10, LiveRegion.Assertive, AnnouncementReason.LiveContentChanged, "a2"));

        MotionFilteredAnnouncements output = motion.FilterAnnouncements(batch);
        Assert.Equal(["focus", "region", "b1", "a2"], output.Announcements.Select(item => item.Text));
        Assert.Equal(LiveRegion.Assertive, output.Announcements[0].Urgency);
        Assert.Equal(LiveRegion.Assertive, output.Announcements[1].Urgency);
        Assert.Equal(LiveRegion.Polite, output.Announcements[3].Urgency);
        Assert.Equal(1, output.CoalescedCount);
        Assert.Equal(1, output.DowngradedCount);
        Assert.Equal(2, output.DroppedCount);

        MotionFilteredAnnouncements repeated = motion.FilterAnnouncements(batch);
        Assert.Equal(output, repeated);
    }

    [Fact]
    public void PreferencesFilterRealProgressTreeTransitions()
    {
        A11yTree previous = ProgressTree(10, LiveRegion.Assertive);
        A11yTree current = ProgressTree(55, LiveRegion.Assertive);
        ScreenReaderAnnouncements plain = current.ScreenReaderAnnouncementsSince(
            previous,
            ScreenReaderPolicy.Default);
        ScreenReaderAnnouncement raw = Assert.Single(plain.Announcements);
        Assert.Equal(LiveRegion.Assertive, raw.Urgency);
        Assert.Equal(AnnouncementReason.LiveContentChanged, raw.Reason);

        MotionFilteredAnnouncements reduced = AccessibilityPreferences.None()
            .WithReducedMotion(true)
            .MotionProfile()
            .FilterAnnouncements(plain);
        Assert.Equal(LiveRegion.Polite, Assert.Single(reduced.Announcements).Urgency);
        Assert.Equal(1, reduced.DowngradedCount);

        MotionFilteredAnnouncements contrastOnly = AccessibilityPreferences.None()
            .WithHighContrast(true)
            .MotionProfile()
            .FilterAnnouncements(plain);
        Assert.Equal(raw, Assert.Single(contrastOnly.Announcements));
    }

    private static A11yNodeInfo Node(ulong id, A11yRole role, string? name = null)
    {
        A11yNodeInfo node = A11yNodeInfo.New(id, role, new Rect(0, 0, 10, 1));
        return name is null ? node : node.WithName(name);
    }

    private static A11yTree BuildTree(params A11yNodeInfo[] nodes)
    {
        var builder = new A11yTreeBuilder();
        foreach (A11yNodeInfo node in nodes)
            builder.AddNode(node);
        if (nodes.Length != 0)
            builder.SetRoot(nodes[0].Id);
        return builder.Build();
    }

    private static A11yTree BuildSampleTree(ulong? focused = null)
    {
        var builder = new A11yTreeBuilder();
        builder.AddNode(Node(1, A11yRole.Window, "App").WithChildren([2, 3]));
        builder.AddNode(Node(2, A11yRole.Group, "Group").WithParent(1).WithChildren([4]));
        builder.AddNode(Node(3, A11yRole.Button, "OK").WithParent(1));
        builder.AddNode(Node(4, A11yRole.Label, "Nested").WithParent(2));
        builder.SetRoot(1);
        builder.SetFocused(focused);
        return builder.Build();
    }

    private static A11yTree ProgressTree(byte percent, LiveRegion region)
    {
        var builder = new A11yTreeBuilder();
        builder.AddNode(Node(1, A11yRole.Window).WithChildren([2]));
        builder.AddNode(
            Node(2, A11yRole.ProgressBar, "Download")
                .WithParent(1)
                .WithState(new A11yState
                {
                    ValueNow = percent,
                    ValueText = $"{percent}%",
                })
                .WithLiveRegion(region));
        builder.SetRoot(1);
        return builder.Build();
    }

    private static ScreenReaderAnnouncement Announcement(
        ulong nodeId,
        LiveRegion urgency,
        AnnouncementReason reason,
        string text) =>
        new(nodeId, urgency, reason, text);

    private static ScreenReaderAnnouncements Batch(
        int droppedCount,
        params ScreenReaderAnnouncement[] announcements) =>
        new(Array.AsReadOnly(announcements), droppedCount);

    private sealed record FakeButton(ulong Id, string Label) : IAccessible
    {
        public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
            [A11yNodeInfo.New(Id, A11yRole.Button, area).WithName(Label)];
    }

    private sealed record FakeList(
        ulong Id,
        IReadOnlyList<(ulong Id, string Label)> Items) : IAccessible
    {
        public List<A11yNodeInfo> AccessibilityNodes(Rect area)
        {
            ulong[] childIds = Items.Select(item => item.Id).ToArray();
            var nodes = new List<A11yNodeInfo>
            {
                A11yNodeInfo.New(Id, A11yRole.List, area)
                    .WithName($"{Items.Count} items")
                    .WithChildren(childIds),
            };
            nodes.AddRange(Items.Select(item =>
                A11yNodeInfo.New(item.Id, A11yRole.ListItem, area)
                    .WithName(item.Label)
                    .WithParent(Id)));
            return nodes;
        }
    }
}
