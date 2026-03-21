namespace FrankenTui.Testing.Harness;

public enum SuitePartition
{
    Canonical,
    Challenge,
    NegativeControl
}

public readonly record struct ViewportSpec(ushort Width, ushort Height)
{
    public static ViewportSpec Standard => new(80, 24);

    public static ViewportSpec Medium => new(120, 40);

    public static ViewportSpec Large => new(200, 60);

    public static ViewportSpec Tiny => new(40, 10);

    public static ViewportSpec Ultrawide => new(320, 24);

    public int CellCount => Width * Height;

    public string Label => $"{Width}x{Height}";
}

public enum TransitionPattern
{
    SparseUpdate,
    ModerateUpdate,
    LargeInvalidation,
    ResizeChurn,
    SubscriptionChurn,
    Cancellation,
    DegradedIo,
    Timeout,
    ArtifactHeavy,
    InputStorm,
    Mixed
}

public sealed record ReproducibilityRules(
    long Seed,
    int TimeStepMs,
    bool DeterministicTime,
    int FixtureVersion,
    bool RequireHostMatch,
    int MinSamples,
    int TimeoutSeconds)
{
    public static ReproducibilityRules Deterministic() => new(42, 16, true, 1, false, 8, 30);

    public static ReproducibilityRules Challenge() => new(42, 0, false, 1, true, 4, 60);
}

public sealed record FixtureSpec(
    string Id,
    string Name,
    FixtureFamily Family,
    SuitePartition Partition,
    ViewportSpec Viewport,
    TransitionPattern Pattern,
    string Description,
    ReproducibilityRules Reproducibility,
    IReadOnlyList<string> Tags)
{
    public string ArtifactPath(string stem, string extension) =>
        $"{stem.Trim()}/{Family.ToString().ToLowerInvariant()}/{Partition.ToString().ToLowerInvariant()}/{Id}.{extension.TrimStart('.')}";
}

public sealed class FixtureRegistry
{
    public FixtureRegistry(IReadOnlyList<FixtureSpec> fixtures)
    {
        Fixtures = fixtures ?? throw new ArgumentNullException(nameof(fixtures));
    }

    public IReadOnlyList<FixtureSpec> Fixtures { get; }

    public static FixtureRegistry Canonical() =>
        new(
        [
            new FixtureSpec(
                "render_sparse_80x24",
                "Render Sparse 80x24",
                FixtureFamily.Render,
                SuitePartition.Canonical,
                ViewportSpec.Standard,
                TransitionPattern.SparseUpdate,
                "Representative dirty-row presenter workload with deterministic output.",
                ReproducibilityRules.Deterministic(),
                ["render", "dirty-rows", "presenter"]),
            new FixtureSpec(
                "runtime_dispatch_120x40",
                "Runtime Dispatch 120x40",
                FixtureFamily.Runtime,
                SuitePartition.Canonical,
                ViewportSpec.Medium,
                TransitionPattern.SubscriptionChurn,
                "Small runtime dispatch workload with command and subscription emission.",
                ReproducibilityRules.Deterministic(),
                ["runtime", "dispatch", "effects"]),
            new FixtureSpec(
                "doctor_manifest_80x24",
                "Doctor Artifact Manifest 80x24",
                FixtureFamily.Doctor,
                SuitePartition.Canonical,
                ViewportSpec.Standard,
                TransitionPattern.ArtifactHeavy,
                "Doctor-side evidence summary validation against local artifact contracts.",
                ReproducibilityRules.Deterministic(),
                ["doctor", "artifacts", "manifest"]),
            new FixtureSpec(
                "render_negative_control_80x24",
                "Render Negative Control 80x24",
                FixtureFamily.Render,
                SuitePartition.NegativeControl,
                ViewportSpec.Standard,
                TransitionPattern.SparseUpdate,
                "Baseline workload expected to remain stable across replay runs.",
                ReproducibilityRules.Deterministic(),
                ["render", "negative-control"]),
            new FixtureSpec(
                "render_challenge_320x24",
                "Render Challenge 320x24",
                FixtureFamily.Challenge,
                SuitePartition.Challenge,
                ViewportSpec.Ultrawide,
                TransitionPattern.LargeInvalidation,
                "Challenge workload that stresses large invalidation and presenter throughput.",
                ReproducibilityRules.Challenge(),
                ["render", "challenge", "ultrawide"])
        ]);

    public IReadOnlyList<FixtureSpec> ByFamily(FixtureFamily family) =>
        Fixtures.Where(candidate => candidate.Family == family).ToArray();

    public IReadOnlyList<FixtureSpec> ByPartition(SuitePartition partition) =>
        Fixtures.Where(candidate => candidate.Partition == partition).ToArray();

    public FixtureSpec? Find(string id) =>
        Fixtures.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
}
