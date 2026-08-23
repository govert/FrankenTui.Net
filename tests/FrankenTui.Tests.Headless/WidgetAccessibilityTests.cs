// Tests ported from the Accessible implementations in:
// block.rs, list.rs, input.rs, paragraph.rs, progress.rs, scrollbar.rs,
// spinner.rs, table.rs, and tabs.rs.
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: The public FrankenTui.Widgets accessibility DTOs are tested as an
// explicit compatibility projection; canonical assertions use FrankenTui.A11y.

using System.Text;
using FrankenTui.Core;
using FrankenTui.Widgets;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Tests.Headless;

public sealed class WidgetAccessibilityTests
{
    private static readonly Rect Area = new(3, 5, 40, 7);

    [Fact]
    public void AllNineUpstreamWidgetsImplementCanonicalAndLegacyContracts()
    {
        // Independent known-answer value from upstream's little-endian FNV-1a algorithm.
        Assert.Equal(3_567_723_274_982_965_756UL, WidgetDrawing.A11yNodeId(Area));

        object[] widgets =
        [
            new Block(),
            new ListWidget([]),
            new TextInput(),
            new Paragraph(TextContent.Raw("")),
            new ProgressBar(),
            new Scrollbar(ScrollbarOrientation.VerticalRight),
            new Spinner(),
            new Table([], []),
            new Tabs([]),
        ];

        Assert.Equal(9, widgets.Length);
        foreach (object widget in widgets)
        {
            CanonicalA11y.IAccessible canonical =
                Assert.IsAssignableFrom<CanonicalA11y.IAccessible>(widget);
            IAccessible legacy = Assert.IsAssignableFrom<IAccessible>(widget);

            List<CanonicalA11y.A11yNodeInfo> canonicalNodes = canonical.AccessibilityNodes(Area);
            List<A11yNodeInfo> legacyNodes = legacy.AccessibilityNodes(Area);
            Assert.NotEmpty(canonicalNodes);
            Assert.Equal(canonicalNodes.Count, legacyNodes.Count);
            Assert.Equal(WidgetDrawing.A11yNodeId(Area), canonicalNodes[0].Id);

            for (int index = 0; index < canonicalNodes.Count; index++)
                AssertLegacyProjection(canonicalNodes[index], legacyNodes[index]);

            Assert.All(canonicalNodes, node =>
            {
                Assert.Equal(Area, node.Bounds);
                Assert.Null(node.LiveRegion);
                Assert.Null(node.Shortcut);
            });
        }
    }

    [Fact]
    public void BlockUsesGroupRoleAndOptionalTitleName()
    {
        CanonicalA11y.A11yNodeInfo untitled = CanonicalNode(new Block());
        Assert.Equal(CanonicalA11y.A11yRole.Group, untitled.Role);
        Assert.Null(untitled.Name);

        CanonicalA11y.A11yNodeInfo titled = CanonicalNode(new Block().Title("Settings"));
        Assert.Equal("Settings", titled.Name);
    }

    [Fact]
    public void ListBuildsNamedParentAndSourceOrderedItemChildren()
    {
        var list = new ListWidget(
        [
            ListItem.New("First\nignored"),
            ListItem.New(""),
            ListItem.New("Third"),
        ]).WithBlock(new Block().Title("Results"));

        List<CanonicalA11y.A11yNodeInfo> nodes = CanonicalNodes(list);
        Assert.Equal(4, nodes.Count);
        CanonicalA11y.A11yNodeInfo root = nodes[0];
        Assert.Equal(CanonicalA11y.A11yRole.List, root.Role);
        Assert.Equal("Results", root.Name);
        Assert.Equal("3 items", root.Description);
        Assert.Equal(nodes.Skip(1).Select(node => node.Id), root.Children);

        Assert.Equal("First", nodes[1].Name);
        Assert.Null(nodes[2].Name);
        Assert.Equal("Third", nodes[3].Name);
        Assert.All(nodes.Skip(1), node =>
        {
            Assert.Equal(CanonicalA11y.A11yRole.ListItem, node.Role);
            Assert.Equal(root.Id, node.Parent);
        });
    }

    [Fact]
    public void TextInputProtectsMaskedValuesAndReportsFocusAndDisabledState()
    {
        var password = new TextInput()
            .WithValue("do-not-expose")
            .WithMask('*')
            .WithFocused(true)
            .WithMaxLength(0);
        CanonicalA11y.A11yNodeInfo passwordNode = CanonicalNode(password);

        Assert.Equal(CanonicalA11y.A11yRole.TextInput, passwordNode.Role);
        Assert.Equal("password field", passwordNode.Name);
        Assert.Equal("password input", passwordNode.Description);
        Assert.DoesNotContain("do-not-expose", passwordNode.Name);
        Assert.True(passwordNode.State.Focused);
        Assert.True(passwordNode.State.Disabled);

        CanonicalA11y.A11yNodeInfo placeholder = CanonicalNode(
            new TextInput().WithPlaceholder("Search"));
        Assert.Equal("Search", placeholder.Name);
        Assert.Null(placeholder.Description);

        CanonicalA11y.A11yNodeInfo unnamed = CanonicalNode(new TextInput());
        Assert.Null(unnamed.Name);
    }

    [Fact]
    public void ParagraphUsesPlainTextAndMovesContentToDescriptionWhenTitled()
    {
        var paragraph = new Paragraph(TextContent.Raw("First line\nSecond line"));
        CanonicalA11y.A11yNodeInfo plain = CanonicalNode(paragraph);
        Assert.Equal(CanonicalA11y.A11yRole.Label, plain.Role);
        Assert.Equal("First line Second line", plain.Name);
        Assert.Null(plain.Description);

        CanonicalA11y.A11yNodeInfo titled = CanonicalNode(
            new Paragraph(TextContent.Raw("First line\nSecond line"))
                .Block(new Block().Title("Body")));
        Assert.Equal("Body", titled.Name);
        Assert.Equal("First line Second line", titled.Description);
    }

    [Fact]
    public void ParagraphTruncationCountsUnicodeScalarsAndPreservesGraphemes()
    {
        string content = string.Concat(Enumerable.Repeat("😀", 201));
        CanonicalA11y.A11yNodeInfo node = CanonicalNode(
            new Paragraph(TextContent.Raw(content)));

        Assert.NotNull(node.Name);
        Assert.EndsWith("...", node.Name);
        Assert.Equal(200, RuneCount(node.Name!));
        Assert.DoesNotContain("�", node.Name);
    }

    [Fact]
    public void ProgressReportsRustRoundedPercentageAndUnitIntervalState()
    {
        CanonicalA11y.A11yNodeInfo node = CanonicalNode(new ProgressBar().Ratio(0.125));
        Assert.Equal(CanonicalA11y.A11yRole.ProgressBar, node.Role);
        Assert.Equal("13%", node.Name);
        Assert.Equal(0.125, node.State.ValueNow);
        Assert.Equal(0.0, node.State.ValueMin);
        Assert.Equal(1.0, node.State.ValueMax);
        Assert.Equal("13%", node.State.ValueText);

        CanonicalA11y.A11yNodeInfo labeled = CanonicalNode(
            new ProgressBar().Ratio(0.125).Label("Download"));
        Assert.Equal("Download", labeled.Name);
        Assert.Equal("13%", labeled.State.ValueText);
    }

    [Theory]
    [InlineData(ScrollbarOrientation.VerticalRight, "vertical scrollbar")]
    [InlineData(ScrollbarOrientation.VerticalLeft, "vertical scrollbar")]
    [InlineData(ScrollbarOrientation.HorizontalBottom, "horizontal scrollbar")]
    [InlineData(ScrollbarOrientation.HorizontalTop, "horizontal scrollbar")]
    public void ScrollbarReportsOrientationWithoutClaimingFocus(
        ScrollbarOrientation orientation,
        string name)
    {
        CanonicalA11y.A11yNodeInfo node = CanonicalNode(new Scrollbar(orientation));
        Assert.Equal(CanonicalA11y.A11yRole.ScrollBar, node.Role);
        Assert.Equal(name, node.Name);
        Assert.False(node.State.Focused);
        Assert.False(node.State.Disabled);
    }

    [Fact]
    public void SpinnerReportsBusyProgressAndSourceEquivalentNames()
    {
        CanonicalA11y.A11yNodeInfo defaultNode = CanonicalNode(new Spinner());
        Assert.Equal(CanonicalA11y.A11yRole.ProgressBar, defaultNode.Role);
        Assert.Equal("Loading...", defaultNode.Name);
        Assert.True(defaultNode.State.Busy);

        CanonicalA11y.A11yNodeInfo labeled = CanonicalNode(
            new Spinner().WithLabel("dependencies"));
        Assert.Equal("Loading: dependencies", labeled.Name);
        Assert.True(labeled.State.Busy);
    }

    [Fact]
    public void TableReportsTitleAndStructuralDimensions()
    {
        var table = new Table(
        [
            Row.New(new[] { "A", "B" }),
            Row.New(new[] { "C", "D" }),
        ],
        [TableConstraint.Fixed(5), TableConstraint.Fill])
            .Block(new Block().Title("Metrics"));

        CanonicalA11y.A11yNodeInfo node = CanonicalNode(table);
        Assert.Equal(CanonicalA11y.A11yRole.Table, node.Role);
        Assert.Equal("Metrics", node.Name);
        Assert.Equal("2 rows, 2 columns", node.Description);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void TabsExposeGroupAndTabTreeWithStableSourceOrder()
    {
        var tabs = new Tabs([Tab.New("Home"), Tab.New("Logs"), Tab.New("About")]);
        List<CanonicalA11y.A11yNodeInfo> nodes = CanonicalNodes(tabs);

        Assert.Equal(4, nodes.Count);
        CanonicalA11y.A11yNodeInfo group = nodes[0];
        Assert.Equal(CanonicalA11y.A11yRole.Group, group.Role);
        Assert.Equal("3 tabs", group.Name);
        Assert.Equal(nodes.Skip(1).Select(node => node.Id), group.Children);
        Assert.Equal(new[] { "Home", "Logs", "About" }, nodes.Skip(1).Select(node => node.Name));
        Assert.All(nodes.Skip(1), node =>
        {
            Assert.Equal(CanonicalA11y.A11yRole.Tab, node.Role);
            Assert.Equal(group.Id, node.Parent);
        });
    }

    private static CanonicalA11y.A11yNodeInfo CanonicalNode(object widget) =>
        Assert.Single(CanonicalNodes(widget));

    private static List<CanonicalA11y.A11yNodeInfo> CanonicalNodes(object widget) =>
        Assert.IsAssignableFrom<CanonicalA11y.IAccessible>(widget).AccessibilityNodes(Area);

    private static int RuneCount(string value)
    {
        int count = 0;
        foreach (Rune _ in value.EnumerateRunes()) count++;
        return count;
    }

    private static void AssertLegacyProjection(
        CanonicalA11y.A11yNodeInfo canonical,
        A11yNodeInfo legacy)
    {
        Assert.Equal(canonical.Id, legacy.Id);
        Assert.Equal(canonical.Role.ToString(), legacy.Role.ToString());
        Assert.Equal(canonical.Bounds, legacy.Area);
        Assert.Equal(canonical.Name, legacy.Name);
        Assert.Equal(canonical.Description, legacy.Description);
        Assert.Equal(canonical.Children, legacy.Children);
        Assert.Equal(canonical.Parent, legacy.Parent);
        Assert.Equal(canonical.Shortcut, legacy.Shortcut);
        Assert.Equal(canonical.LiveRegion, legacy.LiveRegion);
        Assert.Equal(canonical.State.Focused, legacy.State.Focused);
        Assert.Equal(canonical.State.Disabled, legacy.State.Disabled);
        Assert.Equal(canonical.State.Checked, legacy.State.CheckedValue);
        Assert.Equal(canonical.State.Expanded, legacy.State.ExpandedValue);
        Assert.Equal(canonical.State.Selected, legacy.State.Selected);
        Assert.Equal(canonical.State.Readonly, legacy.State.Readonly);
        Assert.Equal(canonical.State.Required, legacy.State.Required);
        Assert.Equal(canonical.State.Busy, legacy.State.Busy);
        Assert.Equal(canonical.State.ValueNow, legacy.State.ValueNow);
        Assert.Equal(canonical.State.ValueMin, legacy.State.ValueMin);
        Assert.Equal(canonical.State.ValueMax, legacy.State.ValueMax);
        Assert.Equal(canonical.State.ValueText, legacy.State.ValueText);
        Assert.False(legacy.State.Hidden);
    }
}
