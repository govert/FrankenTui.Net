// Behavioral port tests for frankentui 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source denominator: 46 tests in crates/ftui-render/src/frame_guardrails.rs.

using System.Reflection;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class FrameGuardrailsTests
{
    [Fact]
    public void MemoryConfigsExposeSourceDefaultsPresetsAndNormalization()
    {
        var defaults = MemoryBudgetConfig.Default;
        Assert.Equal(8UL * 1024 * 1024, defaults.SoftLimitBytes);
        Assert.Equal(16UL * 1024 * 1024, defaults.HardLimitBytes);
        Assert.Equal(32UL * 1024 * 1024, defaults.EmergencyLimitBytes);
        Assert.Equal(defaults, new MemoryBudgetConfig());

        var small = MemoryBudgetConfig.Small();
        var large = MemoryBudgetConfig.Large();
        Assert.True(small.SoftLimitBytes < defaults.SoftLimitBytes);
        Assert.True(large.SoftLimitBytes > defaults.SoftLimitBytes);

        var zero = default(MemoryBudgetConfig).Normalized();
        Assert.Equal(defaults, zero);

        var inverted = new MemoryBudgetConfig(
            32UL * 1024 * 1024,
            8UL * 1024 * 1024,
            16UL * 1024 * 1024).Normalized();
        Assert.Equal(inverted.SoftLimitBytes, inverted.HardLimitBytes);
        Assert.Equal(inverted.HardLimitBytes, inverted.EmergencyLimitBytes);

        var budget = new MemoryBudget(default);
        Assert.Equal(defaults, budget.Config);
    }

    [Fact]
    public void MemoryThresholdsAreInclusiveAndRecommendSourceLevels()
    {
        var below = new MemoryBudget(MemoryBudgetConfig.Default);
        Assert.Null(below.Check(1024));
        Assert.Equal(1024UL, below.CurrentBytes);
        Assert.InRange(below.UsageFraction, double.Epsilon, 0.001);

        var cases = new[]
        {
            (Bytes: 8UL * 1024 * 1024, Severity: AlertSeverity.Warning,
                Level: DegradationLevel.SimpleBorders),
            (Bytes: 16UL * 1024 * 1024, Severity: AlertSeverity.Critical,
                Level: DegradationLevel.Skeleton),
            (Bytes: 32UL * 1024 * 1024, Severity: AlertSeverity.Emergency,
                Level: DegradationLevel.SkipFrame),
        };

        foreach (var expected in cases)
        {
            var budget = new MemoryBudget(MemoryBudgetConfig.Default);
            var alert = Assert.IsType<GuardrailAlert>(budget.Check(expected.Bytes));
            Assert.Equal(GuardrailKind.Memory, alert.Kind);
            Assert.Equal(expected.Severity, alert.Severity);
            Assert.Equal(expected.Level, alert.RecommendedLevel);
        }
    }

    [Fact]
    public void MemoryTracksCurrentPeakViolationsFractionAndReset()
    {
        var config = new MemoryBudgetConfig(100, 200, 300);
        var budget = new MemoryBudget(config);

        Assert.Null(budget.Check(50));
        budget.Check(150);
        budget.Check(150);
        budget.Check(250);

        Assert.Equal(250UL, budget.CurrentBytes);
        Assert.Equal(250UL, budget.PeakBytes);
        Assert.Equal(2.5, budget.UsageFraction);
        Assert.Equal((uint)2, budget.SoftViolations);
        Assert.Equal((uint)1, budget.HardViolations);

        budget.Check(125);
        Assert.Equal(250UL, budget.PeakBytes);
        Assert.Equal(125UL, budget.CurrentBytes);

        budget.Reset();
        Assert.Equal(0UL, budget.CurrentBytes);
        Assert.Equal(0UL, budget.PeakBytes);
        Assert.Equal((uint)0, budget.SoftViolations);
        Assert.Equal((uint)0, budget.HardViolations);
        Assert.Equal(config, budget.Config);
    }

    [Fact]
    public void MemoryCloneCopiesTrackingStateIndependently()
    {
        var original = new MemoryBudget(new MemoryBudgetConfig(100, 200, 300));
        original.Check(150);
        var clone = original.Clone();

        original.Reset();

        Assert.Equal(0UL, original.PeakBytes);
        Assert.Equal(150UL, clone.PeakBytes);
        Assert.Equal((uint)1, clone.SoftViolations);
    }

    [Fact]
    public void QueueConfigsExposeSourceDefaultsPresetsAndDefaultPolicy()
    {
        var defaults = QueueConfig.Default;
        Assert.Equal(new QueueConfig(), defaults);
        Assert.Equal((uint)3, defaults.WarnDepth);
        Assert.Equal((uint)8, defaults.MaxDepth);
        Assert.Equal((uint)16, defaults.EmergencyDepth);
        Assert.Equal(QueueDropPolicy.DropOldest, defaults.DropPolicy);
        Assert.Equal(QueueDropPolicy.DropOldest, default(QueueDropPolicy));

        var strict = QueueConfig.Strict();
        Assert.Equal(QueueDropPolicy.Backpressure, strict.DropPolicy);
        Assert.True(strict.MaxDepth < defaults.MaxDepth);

        var relaxed = QueueConfig.Relaxed();
        Assert.Equal(QueueDropPolicy.DropOldest, relaxed.DropPolicy);
        Assert.True(relaxed.MaxDepth > defaults.MaxDepth);
    }

    [Fact]
    public void QueueBelowAndAtWarningDepthDoNotRequestActions()
    {
        var queue = new QueueGuardrails(QueueConfig.Default);

        var below = queue.Check(1);
        Assert.Null(below.Alert);
        Assert.IsType<QueueAction.None>(below.Action);

        var warning = queue.Check(3);
        Assert.Equal(AlertSeverity.Warning, warning.Alert!.Value.Severity);
        Assert.Equal(DegradationLevel.SimpleBorders, warning.Alert.Value.RecommendedLevel);
        Assert.IsType<QueueAction.None>(warning.Action);
    }

    [Fact]
    public void QueueCriticalDepthAppliesEachConfiguredPolicy()
    {
        var oldest = new QueueGuardrails(new QueueConfig(3, 8, 16, QueueDropPolicy.DropOldest));
        var oldestResult = oldest.Check(8);
        Assert.Equal(AlertSeverity.Critical, oldestResult.Alert!.Value.Severity);
        Assert.Equal((uint)5, Assert.IsType<QueueAction.DropOldest>(oldestResult.Action).Count);
        Assert.Equal(5UL, oldest.TotalDrops);

        var newest = new QueueGuardrails(new QueueConfig(3, 8, 16, QueueDropPolicy.DropNewest));
        var newestResult = newest.Check(8);
        Assert.Equal((uint)5, Assert.IsType<QueueAction.DropNewest>(newestResult.Action).Count);
        Assert.Equal(5UL, newest.TotalDrops);

        var backpressure = new QueueGuardrails(
            new QueueConfig(3, 8, 16, QueueDropPolicy.Backpressure));
        var backpressureResult = backpressure.Check(8);
        Assert.IsType<QueueAction.Backpressure>(backpressureResult.Action);
        Assert.Equal(1UL, backpressure.TotalBackpressureEvents);
        Assert.Equal(0UL, backpressure.TotalDrops);
    }

    [Fact]
    public void QueueEmergencyDepthKeepsOnlyOneFrameForDropPolicies()
    {
        var oldest = new QueueGuardrails(QueueConfig.Default);
        var oldestResult = oldest.Check(16);

        Assert.Equal(AlertSeverity.Emergency, oldestResult.Alert!.Value.Severity);
        Assert.Equal(DegradationLevel.SkipFrame, oldestResult.Alert.Value.RecommendedLevel);
        Assert.Equal((uint)15, Assert.IsType<QueueAction.DropOldest>(oldestResult.Action).Count);
        Assert.Equal(15UL, oldest.TotalDrops);

        var newest = new QueueGuardrails(
            QueueConfig.Default with { DropPolicy = QueueDropPolicy.DropNewest });
        var newestResult = newest.Check(16);
        Assert.Equal((uint)15, Assert.IsType<QueueAction.DropNewest>(newestResult.Action).Count);
    }

    [Fact]
    public void QueueTracksPeakCountersConfigAndResetAndClone()
    {
        var config = QueueConfig.Relaxed();
        var queue = new QueueGuardrails(config);
        queue.Check(2);
        queue.Check(16);
        queue.Check(1);

        Assert.Equal((uint)1, queue.CurrentDepth);
        Assert.Equal((uint)16, queue.PeakDepth);
        Assert.Equal(8UL, queue.TotalDrops);
        Assert.Equal(config, queue.Config);

        var clone = queue.Clone();
        queue.Reset();
        Assert.Equal((uint)0, queue.CurrentDepth);
        Assert.Equal((uint)0, queue.PeakDepth);
        Assert.Equal(0UL, queue.TotalDrops);
        Assert.Equal((uint)16, clone.PeakDepth);
        Assert.Equal(8UL, clone.TotalDrops);
    }

    [Fact]
    public void QueueActionDropClassificationMatchesTaggedVariants()
    {
        Assert.False(new QueueAction.None().DropsFrames());
        Assert.True(new QueueAction.DropOldest(3).DropsFrames());
        Assert.True(new QueueAction.DropNewest(1).DropsFrames());
        Assert.False(new QueueAction.Backpressure().DropsFrames());
        Assert.Equal(new QueueAction.DropOldest(3), new QueueAction.DropOldest(3));
    }

    [Fact]
    public void UnifiedGuardrailsReturnClearVerdictForHealthyFrame()
    {
        var guardrails = new FrameGuardrails(GuardrailsConfig.Default);

        var verdict = guardrails.CheckFrame(1024, 0);

        Assert.True(verdict.IsClear());
        Assert.False(verdict.ShouldDegrade());
        Assert.False(verdict.ShouldDropFrame());
        Assert.Null(verdict.MaxSeverity());
        Assert.Equal(DegradationLevel.Full, verdict.RecommendedLevel);
        Assert.IsType<QueueAction.None>(verdict.QueueAction);
    }

    [Fact]
    public void UnifiedGuardrailsCombineAlertsInStableOrderAndChooseMostAggressiveLevel()
    {
        var memoryOnly = new FrameGuardrails(GuardrailsConfig.Default)
            .CheckFrame(8UL * 1024 * 1024, 0);
        Assert.Single(memoryOnly.Alerts);
        Assert.Equal(GuardrailKind.Memory, memoryOnly.Alerts[0].Kind);
        Assert.True(memoryOnly.ShouldDegrade());
        Assert.False(memoryOnly.ShouldDropFrame());

        var queueOnly = new FrameGuardrails(GuardrailsConfig.Default)
            .CheckFrame(0, 8);
        Assert.Single(queueOnly.Alerts);
        Assert.Equal(GuardrailKind.QueueDepth, queueOnly.Alerts[0].Kind);

        var config = new GuardrailsConfig(
            new MemoryBudgetConfig(100, 200, 300),
            new QueueConfig(1, 2, 3, QueueDropPolicy.DropOldest));
        var guardrails = new FrameGuardrails(config);

        var degraded = guardrails.CheckFrame(150, 2);
        Assert.Collection(
            degraded.Alerts,
            alert => Assert.Equal(GuardrailKind.Memory, alert.Kind),
            alert => Assert.Equal(GuardrailKind.QueueDepth, alert.Kind));
        Assert.Equal(DegradationLevel.EssentialOnly, degraded.RecommendedLevel);
        Assert.Equal(AlertSeverity.Critical, degraded.MaxSeverity());
        Assert.True(degraded.ShouldDegrade());
        Assert.False(degraded.ShouldDropFrame());

        var emergency = guardrails.CheckFrame(300, 3);
        Assert.Equal(2, emergency.Alerts.Count);
        Assert.Equal(DegradationLevel.SkipFrame, emergency.RecommendedLevel);
        Assert.True(emergency.ShouldDropFrame());
        Assert.False(emergency.ShouldDegrade());
        Assert.Equal((uint)2, Assert.IsType<QueueAction.DropOldest>(emergency.QueueAction).Count);
    }

    [Fact]
    public void UnifiedCountersAlertRateSnapshotAndResetRemainConsistent()
    {
        var guardrails = new FrameGuardrails(new GuardrailsConfig(
            new MemoryBudgetConfig(100, 200, 300),
            QueueConfig.Default));

        guardrails.CheckFrame(50, 0);
        guardrails.CheckFrame(150, 0);
        guardrails.CheckFrame(50, 0);
        guardrails.CheckFrame(150, 0);

        Assert.Equal(4UL, guardrails.FramesChecked);
        Assert.Equal(2UL, guardrails.FramesWithAlerts);
        Assert.Equal(0.5, guardrails.AlertRate);
        Assert.Equal(150UL, guardrails.Memory.PeakBytes);
        Assert.Equal((uint)0, guardrails.Queue.PeakDepth);

        var snapshot = guardrails.Snapshot();
        Assert.Equal(150UL, snapshot.MemoryBytes);
        Assert.Equal(150UL, snapshot.MemoryPeakBytes);
        Assert.Equal(4UL, snapshot.FramesChecked);
        Assert.Equal(2UL, snapshot.FramesWithAlerts);

        guardrails.Reset();
        Assert.Equal(0UL, guardrails.FramesChecked);
        Assert.Equal(0UL, guardrails.FramesWithAlerts);
        Assert.Equal(0.0, guardrails.AlertRate);
        Assert.Equal(0UL, guardrails.Memory.PeakBytes);
        Assert.Equal((uint)0, guardrails.Queue.PeakDepth);
    }

    [Fact]
    public void SnapshotJsonlMatchesSourceSchemaOrderAndInvariantFormatting()
    {
        var guardrails = new FrameGuardrails(GuardrailsConfig.Default);
        guardrails.CheckFrame(1024, 1);

        Assert.Equal(
            "{\"memory_bytes\":1024,\"memory_peak\":1024,\"memory_frac\":0.0001,"
            + "\"mem_soft_violations\":0,\"mem_hard_violations\":0,"
            + "\"queue_depth\":1,\"queue_peak\":1,\"queue_drops\":0,"
            + "\"queue_backpressure\":0,\"frames_checked\":1,\"frames_alerted\":0}",
            guardrails.Snapshot().ToJsonl());
    }

    [Fact]
    public void VerdictIsMutableLikeSourcePublicFieldsAndCloneOwnsItsAlertList()
    {
        var verdict = new GuardrailVerdict(
        [
            new GuardrailAlert(
                GuardrailKind.Memory,
                AlertSeverity.Warning,
                DegradationLevel.SimpleBorders),
            new GuardrailAlert(
                GuardrailKind.QueueDepth,
                AlertSeverity.Critical,
                DegradationLevel.EssentialOnly),
        ],
            new QueueAction.None(),
            DegradationLevel.EssentialOnly);
        var clone = verdict.Clone();

        Assert.Equal(AlertSeverity.Critical, verdict.MaxSeverity());
        Assert.True(verdict.ShouldDegrade());
        verdict.Alerts.Clear();
        verdict.RecommendedLevel = DegradationLevel.Full;

        Assert.True(verdict.IsClear());
        Assert.Null(verdict.MaxSeverity());
        Assert.Equal(2, clone.Alerts.Count);
        Assert.Equal(AlertSeverity.Critical, clone.MaxSeverity());
    }

    [Fact]
    public void SeverityAndDegradationEnumsPreserveRequiredOrdering()
    {
        Assert.True(AlertSeverity.Warning < AlertSeverity.Critical);
        Assert.True(AlertSeverity.Critical < AlertSeverity.Emergency);
        Assert.True(DegradationLevel.Full < DegradationLevel.SimpleBorders);
        Assert.True(DegradationLevel.EssentialOnly < DegradationLevel.Skeleton);
        Assert.True(DegradationLevel.Skeleton < DegradationLevel.SkipFrame);
    }

    [Fact]
    public void BufferMemoryUtilityUsesSourceCellSizeAndHandlesZeroAndLargeDimensions()
    {
        Assert.Equal(16UL, FrameGuardrails.CellSizeBytes);
        Assert.Equal(80UL * 24 * 16, FrameGuardrails.BufferMemoryBytes(80, 24));
        Assert.Equal(0UL, FrameGuardrails.BufferMemoryBytes(0, 24));
        Assert.Equal(0UL, FrameGuardrails.BufferMemoryBytes(80, 0));
        Assert.Equal(0UL, FrameGuardrails.BufferMemoryBytes(0, 0));
        Assert.Equal(480_000UL, FrameGuardrails.BufferMemoryBytes(300, 100));
    }

    [Fact]
    public void GuardrailsAreDeterministicAndClonesAdvanceIndependently()
    {
        static (string[] Verdicts, GuardrailSnapshot Snapshot) Run()
        {
            var config = GuardrailsConfig.Default;
            var guardrails = new FrameGuardrails(config.Clone());
            var inputs = new[]
            {
                (Memory: 1024UL, Queue: 0U),
                (Memory: 8UL * 1024 * 1024, Queue: 3U),
                (Memory: 20UL * 1024 * 1024, Queue: 10U),
            };
            var verdicts = inputs.Select(input =>
            {
                var verdict = guardrails.CheckFrame(input.Memory, input.Queue);
                return $"{verdict.RecommendedLevel}:{verdict.Alerts.Count}:{verdict.QueueAction}";
            }).ToArray();
            return (verdicts, guardrails.Snapshot());
        }

        var first = Run();
        var second = Run();
        Assert.Equal(first.Verdicts, second.Verdicts);
        Assert.Equal(first.Snapshot, second.Snapshot);

        var original = new FrameGuardrails(GuardrailsConfig.Default);
        original.CheckFrame(8UL * 1024 * 1024, 3);
        var clone = original.Clone();
        original.Reset();
        clone.CheckFrame(0, 0);
        Assert.Equal(0UL, original.FramesChecked);
        Assert.Equal(2UL, clone.FramesChecked);
        Assert.Equal(1UL, clone.FramesWithAlerts);
    }

    [Fact]
    public void RenderAndRuntimeGuardrailsConfigsRemainDistinctPublicContracts()
    {
        Assert.Equal("FrankenTui.Render", typeof(GuardrailsConfig).Namespace);
        Assert.Equal("FrankenTui.Runtime", typeof(FrankenTui.Runtime.GuardrailsConfig).Namespace);
        Assert.True(FrankenTui.Runtime.GuardrailsConfig.Default.MaxTotalCells > 0);
        Assert.True(FrankenTui.Runtime.GuardrailsConfig.Default.MaxQueueDepth > 0);
    }

    [Fact]
    public void PublicTypeVariantAndDataDenominatorsRemainClosed()
    {
        Type[] domainTypes =
        [
            typeof(GuardrailKind),
            typeof(AlertSeverity),
            typeof(GuardrailAlert),
            typeof(MemoryBudgetConfig),
            typeof(MemoryBudget),
            typeof(QueueDropPolicy),
            typeof(QueueConfig),
            typeof(QueueGuardrails),
            typeof(QueueAction),
            typeof(GuardrailsConfig),
            typeof(GuardrailVerdict),
            typeof(FrameGuardrails),
            typeof(GuardrailSnapshot),
        ];
        Assert.Equal(13, domainTypes.Distinct().Count());

        Assert.Equal(
            new[] { "Memory", "QueueDepth" },
            Enum.GetNames<GuardrailKind>());
        Assert.Equal(
            new[] { "Warning", "Critical", "Emergency" },
            Enum.GetNames<AlertSeverity>());
        Assert.Equal(
            new[] { "DropOldest", "DropNewest", "Backpressure" },
            Enum.GetNames<QueueDropPolicy>());
        Assert.Equal(
            new[] { "Backpressure", "DropNewest", "DropOldest", "None" },
            typeof(QueueAction).GetNestedTypes(BindingFlags.Public)
                .Select(type => type.Name)
                .Order(StringComparer.Ordinal));

        Type[] publicDataTypes =
        [
            typeof(GuardrailAlert),
            typeof(MemoryBudgetConfig),
            typeof(QueueConfig),
            typeof(GuardrailsConfig),
            typeof(GuardrailVerdict),
            typeof(GuardrailSnapshot),
        ];
        Assert.Equal(
            26,
            publicDataTypes.Sum(type => type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length));
    }
}
