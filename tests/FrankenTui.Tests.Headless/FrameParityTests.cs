// Differential contract for crates/ftui-render/src/frame.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class FrameParityTests
{
    private sealed class LinkRouteModel : IModel<string>
    {
        public Program<string>? Program { get; set; }
        public uint RegisteredId { get; private set; }

        public Cmd<string> Init() => new Cmd<string>.Tick(TimeSpan.FromMilliseconds(1));

        public Cmd<string> Update(string message) => Cmd<string>.NoneCmd;

        public void View(Frame frame)
        {
            RegisteredId = frame.RegisterLink("https://example.com/runtime-route");
            Program!.Quit();
        }
    }

    [Fact]
    public void HitIdAndSourceRegionPreserveFullPayloadDomains()
    {
        Assert.Equal(uint.MaxValue, HitId.New(uint.MaxValue).Id);
        Assert.Equal(default, HitId.New(0));
        Assert.Equal(HitRegion.None, default);

        var custom = HitRegion.Custom(255);
        Assert.True(custom.IsCustom);
        Assert.Equal((byte)255, custom.CustomTag);
        Assert.Equal(HitRegionKind.Custom, custom.ToCompatibilityKind());
    }

    [Theory]
    [InlineData(HitRegionKind.None, 0)]
    [InlineData(HitRegionKind.Content, 0)]
    [InlineData(HitRegionKind.Border, 0)]
    [InlineData(HitRegionKind.Scrollbar, 0)]
    [InlineData(HitRegionKind.Handle, 0)]
    [InlineData(HitRegionKind.Button, 0)]
    [InlineData(HitRegionKind.Link, 0)]
    [InlineData(HitRegionKind.Custom, 0)]
    [InlineData(HitRegionKind.DialogInput, 1)]
    [InlineData(HitRegionKind.ModalBackdrop, 1)]
    [InlineData(HitRegionKind.ModalContent, 2)]
    public void CompatibilityRegionsHaveExplicitSourceProjection(HitRegionKind kind, byte tag)
    {
        var source = HitRegion.FromCompatibilityKind(kind);

        Assert.Equal(tag, source.CustomTag);
        Assert.Equal(kind is >= HitRegionKind.Custom ? HitRegionKind.Custom : kind, source.Kind);
    }

    [Fact]
    public void HitCellAndResultPreserveSourceAndCompatibilityCoordinates()
    {
        var cell = HitCell.Create(HitId.New(42), HitRegion.Custom(99), 7, new HitOwner(123));

        Assert.False(cell.IsEmpty);
        Assert.Equal(HitRegionKind.Custom, cell.Region);
        Assert.Equal(HitRegion.Custom(99), cell.SourceRegion);
        Assert.Equal(123UL, cell.Owner?.Value);

        var result = HitTestResult.New(cell.WidgetId!.Value, cell.SourceRegion, cell.Data, cell.Owner);
        Assert.Equal((HitId.New(42), HitRegion.Custom(99), new HitData(7)), result.IntoSourceTuple());
        Assert.Equal((HitId.New(42), HitRegionKind.Custom, new HitData(7)), result.IntoTuple());
        Assert.True(default(HitCell).IsEmpty);
    }

    [Fact]
    public void GridCreationAndOutOfBoundsQueriesMatchSource()
    {
        var grid = new HitGrid(10, 5);

        Assert.Equal((ushort)10, grid.Width);
        Assert.Equal((ushort)5, grid.Height);
        Assert.NotNull(grid.Get(0, 0));
        Assert.True(grid.Get(0, 0)!.Value.IsEmpty);
        Assert.Null(grid.Get(10, 0));
        Assert.Null(grid.Get(0, 5));
        Assert.Null(grid.HitTest(0, 0));
        Assert.False(grid.TryMutate(10, 0, static cell => cell));

        var zero = new HitGrid(0, 0);
        Assert.Null(zero.Get(0, 0));
        zero.Register(new Rect(0, 0, 1, 1), HitId.New(1), HitRegion.Content, 0);
        Assert.Null(zero.HitTestSource(0, 0));
    }

    [Fact]
    public void GridRegistrationClipsOverwritesAndPreservesOwner()
    {
        var grid = new HitGrid(10, 5);
        grid.Register(new Rect(8, 3, 10, 10), HitId.New(1), HitRegion.Custom(7), 99, new HitOwner(55));

        Assert.Null(grid.HitTestSource(7, 3));
        Assert.Equal((HitId.New(1), HitRegion.Custom(7), new HitData(99)), grid.HitTestSource(9, 4));
        Assert.Equal(55UL, grid.HitTestDetailed(9, 4)?.Owner?.Value);

        grid.Register(new Rect(9, 4, 1, 1), HitId.New(2), HitRegion.Button, 3);
        Assert.Equal(HitId.New(2), grid.HitTestSource(9, 4)?.Item1);
        Assert.Equal(HitId.New(1), grid.HitTestSource(8, 4)?.Item1);
    }

    [Fact]
    public void GridZeroSizedAndPastBoundsRegistrationsAreNoOps()
    {
        var grid = new HitGrid(4, 4);

        grid.Register(new Rect(1, 1, 0, 2), HitId.New(1), HitRegion.Content, 0);
        grid.Register(new Rect(1, 1, 2, 0), HitId.New(1), HitRegion.Content, 0);
        grid.Register(new Rect(10, 10, 2, 2), HitId.New(1), HitRegion.Content, 0);

        Assert.All(Enumerable.Range(0, 4), y =>
            Assert.All(Enumerable.Range(0, 4), x => Assert.Null(grid.HitTestSource((ushort)x, (ushort)y))));
    }

    [Fact]
    public void HitsInReturnsOneEntryPerPopulatedCellLikeSource()
    {
        var grid = new HitGrid(4, 4);
        grid.Register(new Rect(1, 1, 2, 2), HitId.New(1), HitRegion.Content, 8);

        Assert.Equal(4, grid.HitsInSource(new Rect(0, 0, 4, 4)).Count);
        Assert.Equal(2, grid.HitsIn(new Rect(1, 1, 1, 2)).Count);
        Assert.Empty(grid.HitsInSource(new Rect(0, 0, 0, 4)));
        Assert.Empty(grid.HitsInSource(new Rect(10, 10, 2, 2)));
    }

    [Fact]
    public void GridMutationCloneAndClearAreIndependent()
    {
        var grid = new HitGrid(2, 1);
        grid.Register(new Rect(0, 0, 1, 1), HitId.New(1), HitRegion.Content, 5);
        Assert.True(grid.TryMutate(0, 0, cell => cell with { Data = new HitData(9) }));

        var clone = grid.Clone();
        clone.Register(new Rect(1, 0, 1, 1), HitId.New(2), HitRegion.Button, 0);
        grid.Clear();

        Assert.Null(grid.HitTest(0, 0));
        Assert.Equal(9UL, clone.HitTest(0, 0)?.Item3.Value);
        Assert.Equal(HitId.New(2), clone.HitTest(1, 0)?.Item1);
    }

    [Fact]
    public void FrameCreationClampsDimensionsAndUsesSourceDefaults()
    {
        var pool = new GraphemePool();
        var frame = new Frame(0, 0, pool);

        Assert.Equal((ushort)1, frame.Width);
        Assert.Equal((ushort)1, frame.Height);
        Assert.Equal(new Rect(0, 0, 1, 1), frame.Bounds);
        Assert.Null(frame.HitGrid);
        Assert.Null(frame.CursorPosition);
        Assert.True(frame.CursorVisible);
        Assert.Equal(DegradationLevel.Full, frame.Degradation);
        Assert.Null(frame.Links);
        Assert.Null(frame.Arena);
    }

    [Fact]
    public void WithHitGridUsesClampedBufferDimensions()
    {
        var frame = Frame.WithHitGrid(0, 0, new GraphemePool());

        Assert.Equal((ushort)1, frame.HitGrid?.Width);
        Assert.True(frame.RegisterHitRegion(new Rect(0, 0, 1, 1), HitId.New(7)));
        Assert.Equal(HitId.New(7), frame.HitTestSource(0, 0)?.Item1);
    }

    [Fact]
    public void HitTestingCanBeEnabledAndRegistrationReportsRouteAvailability()
    {
        var frame = new Frame(4, 2, new GraphemePool());

        Assert.False(frame.RegisterHitRegion(new Rect(0, 0, 1, 1), HitId.New(1)));
        frame.EnableHitTesting();
        frame.EnableHitTesting();
        Assert.True(frame.RegisterHitRegion(new Rect(0, 0, 1, 1), HitId.New(1)));
        Assert.Equal((HitId.New(1), HitRegion.Content, HitData.Zero), frame.HitTestSource(0, 0));
    }

    [Fact]
    public void FrameRegistrationRespectsNestedScissor()
    {
        var frame = Frame.WithHitGrid(10, 10, new GraphemePool());
        frame.Buffer.PushScissor(new Rect(2, 2, 3, 3));

        frame.RegisterHit(new Rect(0, 0, 10, 10), HitId.New(1), HitRegion.Content, 0);

        Assert.Null(frame.HitTestSource(1, 2));
        Assert.NotNull(frame.HitTestSource(2, 2));
        Assert.NotNull(frame.HitTestSource(4, 4));
        Assert.Null(frame.HitTestSource(5, 4));
    }

    [Fact]
    public void NestedHitOwnersRestoreAfterReturnAndException()
    {
        var frame = Frame.WithHitGrid(3, 1, new GraphemePool());

        frame.WithHitOwner(10, outer =>
        {
            outer.RegisterHit(new Rect(0, 0, 1, 1), HitId.New(1), HitRegion.Content, 0);
            outer.WithHitOwner(20, inner =>
                inner.RegisterHit(new Rect(1, 0, 1, 1), HitId.New(2), HitRegion.Content, 0));
            outer.RegisterHit(new Rect(2, 0, 1, 1), HitId.New(3), HitRegion.Content, 0);
        });

        Assert.Equal(10UL, frame.HitTestDetailed(0, 0)?.Owner?.Value);
        Assert.Equal(20UL, frame.HitTestDetailed(1, 0)?.Owner?.Value);
        Assert.Equal(10UL, frame.HitTestDetailed(2, 0)?.Owner?.Value);

        Assert.Throws<InvalidOperationException>(() => frame.WithHitOwner(30, _ =>
            throw new InvalidOperationException("boom")));
        frame.RegisterHit(new Rect(0, 0, 1, 1), HitId.New(4), HitRegion.Content, 0);
        Assert.Null(frame.HitTestDetailed(0, 0)?.Owner);
    }

    [Fact]
    public void WidgetSignalDefaultsCloneAndProvenanceMatchSource()
    {
        var signal = WidgetSignal.New(99);

        Assert.Equal(99UL, signal.WidgetId);
        Assert.False(signal.Essential);
        Assert.Equal(0.5f, signal.Priority);
        Assert.Equal(1U, signal.AreaCells);
        Assert.Equal(5f, signal.CostEstimateUs);
        Assert.Equal(5f, signal.RecentCostUs);
        Assert.Equal(CostEstimateSource.FixedDefault, signal.EstimateSource);
        Assert.Equal(CostEstimateSource.FixedDefault, default);

        var clone = signal.Clone();
        clone.Priority = 1f;
        clone.EstimateSource = CostEstimateSource.Measured;
        Assert.Equal(0.5f, signal.Priority);
        Assert.Equal(CostEstimateSource.FixedDefault, signal.EstimateSource);
    }

    [Fact]
    public void WidgetBudgetsAllowAllOrOnlySortedUniqueIdsAndAlwaysEssential()
    {
        var all = new WidgetBudget();
        Assert.True(all.Allows(123, false));

        var only = WidgetBudget.AllowOnly(new ulong[] { 9, 2, 9, 4 });
        Assert.True(only.Allows(2, false));
        Assert.True(only.Allows(9, false));
        Assert.False(only.Allows(3, false));
        Assert.True(only.Allows(3, true));
        Assert.False(WidgetBudget.AllowOnly([]).Allows(1, false));
        Assert.True(only.Clone().Allows(4, false));
    }

    [Fact]
    public void FrameWidgetBudgetAndSignalLifecycleMatchSource()
    {
        var frame = new Frame(2, 1, new GraphemePool());
        frame.SetWidgetBudget(WidgetBudget.AllowOnly([2]));
        Assert.False(frame.ShouldRenderWidget(1, false));
        Assert.True(frame.ShouldRenderWidget(2, false));
        Assert.True(frame.ShouldRenderWidget(1, true));

        var signal = WidgetSignal.New(2);
        signal.Priority = 0.9f;
        frame.RegisterWidgetSignal(signal);
        frame.RegisterWidgetSignal(3, false, 10);
        Assert.Equal(2, frame.WidgetSignals.Count);

        var taken = frame.TakeWidgetSignals();
        Assert.Equal(2, taken.Count);
        Assert.Empty(frame.WidgetSignals);
    }

    [Fact]
    public void FrameClearResetsBufferHitGridCursorAndSignals()
    {
        var frame = Frame.WithHitGrid(2, 1, new GraphemePool());
        frame.Buffer.Set(0, 0, Cell.FromChar('X'));
        frame.RegisterHitRegion(new Rect(0, 0, 1, 1), HitId.New(1));
        frame.SetCursor((1, 0));
        frame.SetCursorVisible(false);
        frame.RegisterWidgetSignal(WidgetSignal.New(1));

        frame.Clear();

        Assert.True(frame.Buffer.Get(0, 0)!.Value.IsEmpty);
        Assert.Null(frame.HitTest(0, 0));
        Assert.Null(frame.CursorPosition);
        Assert.False(frame.CursorVisible);
        Assert.Empty(frame.WidgetSignals);
    }

    [Fact]
    public void LinkRegistryIsOptionalAndExternalLikeSource()
    {
        var frame = new Frame(1, 1, new GraphemePool());
        Assert.Equal(0U, frame.RegisterLink("https://example.com"));

        var links = new LinkRegistry();
        frame.SetLinks(links);
        var id = frame.RegisterLink("https://example.com");
        Assert.NotEqual(0U, id);
        Assert.Equal("https://example.com", links.Get(id));

        var other = Frame.WithLinks(1, 1, new GraphemePool(), links);
        Assert.Equal(id, other.RegisterLink("https://example.com"));
    }

    [Fact]
    public void ProgramRoutesItsPersistentLinkRegistryIntoEachFrame()
    {
        var writer = new TerminalWriter(
            new StringWriter(),
            new ScreenMode.AltScreen(),
            UiAnchor.Bottom,
            TerminalCapabilities.Modern());
        var model = new LinkRouteModel();
        var program = new Program<string>(model, writer);
        model.Program = program;

        program.Run();

        Assert.NotEqual(0U, model.RegisteredId);
        Assert.Equal("https://example.com/runtime-route", writer.Links.Get(model.RegisteredId));
    }

    [Fact]
    public void FromBufferAndOverrideAttachTheSharedPool()
    {
        var local = new FrankenTui.Render.Buffer(3, 1);
        local.SetText(0, 0, "e\u0301", Cell.Empty);
        var pool = new GraphemePool();

        var frame = Frame.FromBuffer(local, pool);

        Assert.Same(local, frame.Buffer);
        Assert.Equal("e\u0301", frame.Buffer.ResolveText(frame.Buffer.Get(0, 0)!.Value));
        Assert.True(pool.Count > 0);
    }

    [Fact]
    public void InternUsesAutomaticOrExplicitWidths()
    {
        var pool = new GraphemePool();
        var frame = new Frame(4, 1, pool);

        var automatic = frame.Intern("😀");
        var explicitWidth = frame.InternWithWidth("ab", 1);

        Assert.Equal("😀", pool.Get(automatic));
        Assert.Equal((byte)2, automatic.Width);
        Assert.Equal("ab", pool.Get(explicitWidth));
        Assert.Equal((byte)1, explicitWidth.Width);
    }

    [Fact]
    public void DegradationPropagatesIntoBuffer()
    {
        var frame = new Frame(1, 1, new GraphemePool());

        frame.SetDegradation(DegradationLevel.EssentialOnly);

        Assert.Equal(DegradationLevel.EssentialOnly, frame.Degradation);
        Assert.Equal(DegradationLevel.EssentialOnly, frame.Buffer.Degradation);
        Assert.Equal(DegradationLevel.EssentialOnly, frame.Buffer.Clone().Degradation);
    }

    [Fact]
    public void FrameDrawingDelegatesToTheSourceShapedDrawingContract()
    {
        var frame = new Frame(5, 4, new GraphemePool());
        var hash = Cell.FromChar('#');
        frame.DrawHorizontalLine(0, 0, 3, hash);
        frame.DrawVerticalLine(4, 0, 3, hash);
        frame.DrawRectFilled(new Rect(1, 1, 2, 2), Cell.FromChar('x'));
        frame.PaintArea(new Rect(1, 1, 1, 1), PackedRgba.Red, PackedRgba.Blue);

        Assert.Equal('#', frame.Buffer.Get(2, 0)?.Content.AsRune()?.Value);
        Assert.Equal('#', frame.Buffer.Get(4, 2)?.Content.AsRune()?.Value);
        Assert.Equal('x', frame.Buffer.Get(2, 2)?.Content.AsRune()?.Value);
        Assert.Equal(PackedRgba.Red, frame.Buffer.Get(1, 1)?.Foreground);
        Assert.Equal(PackedRgba.Blue, frame.Buffer.Get(1, 1)?.Background);
    }

    [Fact]
    public void FrameTextInternsComplexAndWideGraphemesAndSetsContinuation()
    {
        var pool = new GraphemePool();
        var frame = new Frame(8, 1, pool);

        var end = frame.PrintText(0, 0, "A😀e\u0301", Cell.Empty);

        Assert.Equal((ushort)4, end);
        Assert.Equal('A', frame.Buffer.Get(0, 0)?.Content.AsRune()?.Value);
        Assert.Equal("😀", frame.Buffer.ResolveText(frame.Buffer.Get(1, 0)!.Value));
        Assert.True(frame.Buffer.Get(2, 0)!.Value.IsContinuation);
        Assert.Equal("e\u0301", frame.Buffer.ResolveText(frame.Buffer.Get(3, 0)!.Value));
    }

    [Fact]
    public void FrameTextClippingDoesNotStartWideGraphemePastBoundary()
    {
        var frame = new Frame(4, 1, new GraphemePool());

        Assert.Equal((ushort)1, frame.PrintTextClipped(0, 0, "A😀", Cell.Empty, 2));
        Assert.True(frame.Buffer.Get(1, 0)!.Value.IsEmpty);
        Assert.Equal((ushort)4, frame.PrintText(4, 0, "x", Cell.Empty));
        Assert.Equal((ushort)0, frame.PrintText(0, 0, string.Empty, Cell.Empty));
    }

    [Fact]
    public void ArenaCanBeAttachedAndCleared()
    {
        var frame = new Frame(1, 1, new GraphemePool());
        var arena = new FrameArena();

        frame.SetArena(arena);
        Assert.Same(arena, frame.Arena);
        frame.ClearArena();
        Assert.Null(frame.Arena);
    }
}
