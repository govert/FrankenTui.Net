// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/egraph.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// The 64 upstream inline tests and one executable module doctest are represented
// one-for-one below. Resource observations remain evidence; they do not route the
// experimental e-graph solver into Flex because upstream has no such route.

using FrankenTui.Layout;
using FrankenTui.Layout.Egraph;
using EGraphNodeId = FrankenTui.Layout.Egraph.NodeId;

namespace FrankenTui.Tests.Headless;

public sealed class EgraphTests
{
    [Fact]
    public void ModuleExampleSimplifiesAddZero()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id b = graph.Add(new Expr.Num(0));
        Id sum = graph.Add(new Expr.Add(a, b));

        graph.ApplyRules();

        Assert.True(graph.Equiv(sum, a));
    }

    [Fact]
    public void AddNum()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));

        Assert.Equal(1, graph.NodeCount());
        Assert.Equal(new Expr.Num(42), graph.Extract(a));
    }

    [Fact]
    public void AddDedup()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id b = graph.Add(new Expr.Num(42));

        Assert.Equal(a, b);
        Assert.Equal(1, graph.NodeCount());
    }

    [Fact]
    public void MergeClasses()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(1));
        Id b = graph.Add(new Expr.Num(2));

        Assert.False(graph.Equiv(a, b));
        graph.Merge(a, b);
        Assert.True(graph.Equiv(a, b));
    }

    [Fact]
    public void MergeIdempotent()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(1));
        Assert.Equal(a, graph.Merge(a, a));
    }

    [Fact]
    public void AddZeroIdentity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id zero = graph.Add(new Expr.Num(0));
        Id sum = graph.Add(new Expr.Add(a, zero));

        graph.ApplyRules();
        Assert.True(graph.Equiv(sum, a));
    }

    [Fact]
    public void AddZeroIdentityLeft()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(50));
        Id zero = graph.Add(new Expr.Num(0));
        Id sum = graph.Add(new Expr.Add(zero, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(sum, a));
    }

    [Fact]
    public void SubZeroIdentity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id zero = graph.Add(new Expr.Num(0));
        Id difference = graph.Add(new Expr.Sub(a, zero));

        graph.ApplyRules();
        Assert.True(graph.Equiv(difference, a));
    }

    [Fact]
    public void MulOneIdentity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id one = graph.Add(new Expr.Num(1));
        Id product = graph.Add(new Expr.Mul(a, one));

        graph.ApplyRules();
        Assert.True(graph.Equiv(product, a));
    }

    [Fact]
    public void MulZeroAnnihilation()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id zero = graph.Add(new Expr.Num(0));
        Id product = graph.Add(new Expr.Mul(a, zero));

        graph.ApplyRules();
        Assert.True(graph.Equiv(product, zero));
    }

    [Fact]
    public void MaxIdempotent()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id maximum = graph.Add(new Expr.Max(a, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(maximum, a));
    }

    [Fact]
    public void MinIdempotent()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id minimum = graph.Add(new Expr.Min(a, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(minimum, a));
    }

    [Fact]
    public void ClampUnclamped()
    {
        var graph = new EGraph();
        Id zero = graph.Add(new Expr.Num(0));
        Id maximum = graph.Add(new Expr.Num(ushort.MaxValue));
        Id value = graph.Add(new Expr.Num(50));
        Id clamped = graph.Add(new Expr.Clamp(zero, maximum, value));

        graph.ApplyRules();
        Assert.True(graph.Equiv(clamped, value));
    }

    [Fact]
    public void ExtractPrefersSimpler()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id zero = graph.Add(new Expr.Num(0));
        Id sum = graph.Add(new Expr.Add(a, zero));

        graph.ApplyRules();
        Assert.Equal(new Expr.Num(100), graph.Extract(sum));
    }

    [Fact]
    public void ExtractVarOverComplex()
    {
        var graph = new EGraph();
        Id variable = graph.Add(new Expr.Var(new EGraphNodeId(0)));
        Id zero = graph.Add(new Expr.Num(0));
        Id sum = graph.Add(new Expr.Add(variable, zero));

        graph.ApplyRules();
        Assert.Equal(new Expr.Var(new EGraphNodeId(0)), graph.Extract(sum));
    }

    [Fact]
    public void CountsAfterMerge()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(1));
        Id b = graph.Add(new Expr.Num(2));

        Assert.Equal(2, graph.ClassCount());
        graph.Merge(a, b);
        Assert.Equal(1, graph.ClassCount());
        Assert.Equal(2, graph.NodeCount());
    }

    [Fact]
    public void EncodeFixedConstraint()
    {
        var graph = new EGraph();
        Id id = EGraphLayout.EncodeConstraint(graph, Constraint.Fixed(50), 200);
        Assert.Equal(new Expr.Num(50), graph.Extract(id));
    }

    [Fact]
    public void EncodePercentageConstraint()
    {
        var graph = new EGraph();
        Id id = EGraphLayout.EncodeConstraint(graph, Constraint.Percentage(50.0f), 200);
        Assert.Equal(new Expr.Num(100), graph.Extract(id));
    }

    [Fact]
    public void EncodeRatioConstraint()
    {
        var graph = new EGraph();
        Id id = EGraphLayout.EncodeConstraint(graph, Constraint.Ratio(1, 4), 200);
        Assert.Equal(new Expr.Num(50), graph.Extract(id));
    }

    [Fact]
    public void EncodeRatioZeroDenom()
    {
        var graph = new EGraph();
        Id id = EGraphLayout.EncodeConstraint(graph, Constraint.Ratio(1, 0), 200);
        Assert.Equal(new Expr.Num(0), graph.Extract(id));
    }

    [Fact]
    public void EncodeFlexHorizontal()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id b = graph.Add(new Expr.Num(200));
        Id flex = EGraphLayout.EncodeFlex(graph, [a, b], horizontal: true);
        Assert.IsType<Expr.HFlex>(graph.Extract(flex));
    }

    [Fact]
    public void EncodeFlexVertical()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id b = graph.Add(new Expr.Num(200));
        Id flex = EGraphLayout.EncodeFlex(graph, [a, b], horizontal: false);
        Assert.IsType<Expr.VFlex>(graph.Extract(flex));
    }

    [Fact]
    public void SaturationTerminates()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(10));
        Id zero = graph.Add(new Expr.Num(0));
        Id one = graph.Add(new Expr.Num(1));
        Id product = graph.Add(new Expr.Mul(a, one));
        Id sum = graph.Add(new Expr.Add(product, zero));

        int total = graph.ApplyRules();

        Assert.True(total > 0);
        Assert.True(graph.Equiv(sum, a));
    }

    [Fact]
    public void ApplyRulesReturnsZeroWhenSaturated()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(10));
        Id b = graph.Add(new Expr.Num(20));
        graph.Add(new Expr.Add(a, b));

        Assert.True(graph.ApplyRules() >= 1);
        Assert.Equal(0, graph.ApplyRules());
    }

    [Fact]
    public void ChainedIdentitySimplification()
    {
        var graph = new EGraph();
        Id x = graph.Add(new Expr.Num(42));
        Id zero = graph.Add(new Expr.Num(0));
        Id one = graph.Add(new Expr.Num(1));
        Id product = graph.Add(new Expr.Mul(x, one));
        Id difference = graph.Add(new Expr.Sub(product, zero));
        Id sum = graph.Add(new Expr.Add(difference, zero));

        graph.ApplyRules();
        Assert.True(graph.Equiv(sum, x));
    }

    [Fact]
    public void TypicalLayoutSize()
    {
        var graph = new EGraph();
        Id[] widgets = Enumerable.Range(0, 5)
            .Select(index => graph.Add(new Expr.Var(new EGraphNodeId((uint)index))))
            .ToArray();
        EGraphLayout.EncodeFlex(graph, widgets, horizontal: true);

        Assert.True(graph.NodeCount() <= 10);
    }

    [Fact]
    public void MediumLayoutSize()
    {
        var graph = new EGraph();
        Id[] widgets = Enumerable.Range(0, 100)
            .Select(index => graph.Add(new Expr.Var(new EGraphNodeId((uint)index))))
            .ToArray();
        EGraphLayout.EncodeFlex(graph, widgets, horizontal: true);

        Assert.True(graph.NodeCount() <= 200);
    }

    [Fact]
    public void SubSelfIsZero()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id zero = graph.Add(new Expr.Num(0));
        Id difference = graph.Add(new Expr.Sub(a, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(difference, zero));
    }

    [Fact]
    public void DivOneIdentity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id one = graph.Add(new Expr.Num(1));
        Id quotient = graph.Add(new Expr.Div(a, one));

        graph.ApplyRules();
        Assert.True(graph.Equiv(quotient, a));
    }

    [Fact]
    public void DivSelfIsOne()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id one = graph.Add(new Expr.Num(1));
        Id quotient = graph.Add(new Expr.Div(a, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(quotient, one));
    }

    [Fact]
    public void MaxZeroIdentity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id zero = graph.Add(new Expr.Num(0));
        Id maximum = graph.Add(new Expr.Max(a, zero));

        graph.ApplyRules();
        Assert.True(graph.Equiv(maximum, a));
    }

    [Fact]
    public void MaxZeroIdentityLeft()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(42));
        Id zero = graph.Add(new Expr.Num(0));
        Id maximum = graph.Add(new Expr.Max(zero, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(maximum, a));
    }

    [Fact]
    public void ClampEqualBounds()
    {
        var graph = new EGraph();
        Id bound = graph.Add(new Expr.Num(50));
        Id value = graph.Add(new Expr.Num(100));
        Id clamped = graph.Add(new Expr.Clamp(bound, bound, value));

        graph.ApplyRules();
        Assert.True(graph.Equiv(clamped, bound));
    }

    [Fact]
    public void ClampValEqualsMin()
    {
        var graph = new EGraph();
        Id minimum = graph.Add(new Expr.Num(10));
        Id maximum = graph.Add(new Expr.Num(100));
        Id clamped = graph.Add(new Expr.Clamp(minimum, maximum, minimum));

        graph.ApplyRules();
        Assert.True(graph.Equiv(clamped, minimum));
    }

    [Fact]
    public void AddCommutativity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(10));
        Id b = graph.Add(new Expr.Num(20));
        Id ab = graph.Add(new Expr.Add(a, b));
        Id ba = graph.Add(new Expr.Add(b, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(ab, ba));
    }

    [Fact]
    public void MaxCommutativity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(10));
        Id b = graph.Add(new Expr.Num(20));
        Id ab = graph.Add(new Expr.Max(a, b));
        Id ba = graph.Add(new Expr.Max(b, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(ab, ba));
    }

    [Fact]
    public void MinCommutativity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(10));
        Id b = graph.Add(new Expr.Num(20));
        Id ab = graph.Add(new Expr.Min(a, b));
        Id ba = graph.Add(new Expr.Min(b, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(ab, ba));
    }

    [Fact]
    public void MulCommutativity()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(3));
        Id b = graph.Add(new Expr.Num(7));
        Id ab = graph.Add(new Expr.Mul(a, b));
        Id ba = graph.Add(new Expr.Mul(b, a));

        graph.ApplyRules();
        Assert.True(graph.Equiv(ab, ba));
    }

    [Fact]
    public void SaturateBasic()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id zero = graph.Add(new Expr.Num(0));
        Id sum = graph.Add(new Expr.Add(a, zero));

        SaturationResult result = graph.Saturate(SaturationConfig.Default);

        Assert.True(result.Saturated);
        Assert.False(result.StoppedEarly);
        Assert.True(result.Rewrites > 0);
        Assert.True(graph.Equiv(sum, a));
    }

    [Fact]
    public void SaturateWithNodeBudget()
    {
        var graph = new EGraph();
        for (ushort index = 0; index < 50; index++)
        {
            Id variable = graph.Add(new Expr.Var(new EGraphNodeId(index)));
            Id number = graph.Add(new Expr.Num((ushort)(index + 1)));
            graph.Add(new Expr.Add(variable, number));
        }

        int initial = graph.NodeCount();
        var config = new SaturationConfig
        {
            NodeBudget = initial + 10,
            IterationLimit = 1_000,
            TimeLimitMicroseconds = 0,
            MemoryLimitBytes = 0,
        };

        SaturationResult result = graph.Saturate(config);

        Assert.True(result.StoppedEarly);
        Assert.Equal(GuardTriggered.NodeBudget, result.Guard);
    }

    [Fact]
    public void SaturateWithIterationLimit()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(10));
        Id b = graph.Add(new Expr.Num(20));
        graph.Add(new Expr.Add(a, b));
        var config = new SaturationConfig
        {
            NodeBudget = 10_000,
            IterationLimit = 1,
            TimeLimitMicroseconds = 0,
            MemoryLimitBytes = 0,
        };

        SaturationResult result = graph.Saturate(config);

        Assert.True(result.Iterations <= 1);
        Assert.Equal(GuardTriggered.IterationLimit, result.Guard);
    }

    [Fact]
    public void SaturateWithMemoryLimit()
    {
        var graph = new EGraph();
        for (ushort index = 0; index < 200; index++)
        {
            Id variable = graph.Add(new Expr.Var(new EGraphNodeId(index)));
            Id number = graph.Add(new Expr.Num((ushort)(index + 1)));
            graph.Add(new Expr.Add(variable, number));
        }

        var config = new SaturationConfig
        {
            NodeBudget = 100_000,
            IterationLimit = 1_000,
            TimeLimitMicroseconds = 0,
            MemoryLimitBytes = 1,
        };

        SaturationResult result = graph.Saturate(config);

        Assert.True(result.StoppedEarly);
        Assert.Equal(GuardTriggered.Memory, result.Guard);
    }

    [Fact]
    public void SaturateGuardNoneOnCompletion()
    {
        var graph = new EGraph();
        Id a = graph.Add(new Expr.Num(100));
        Id zero = graph.Add(new Expr.Num(0));
        graph.Add(new Expr.Add(a, zero));

        SaturationResult result = graph.Saturate(SaturationConfig.Default);

        Assert.True(result.Saturated);
        Assert.Equal(GuardTriggered.None, result.Guard);
        Assert.True(result.TimeMicroseconds > 0 || result.Iterations > 0);
        Assert.True(result.MemoryBytes > 0);
    }

    [Fact]
    public void SaturateResultHasTiming()
    {
        var graph = new EGraph();
        for (ushort index = 0; index < 100; index++)
        {
            Id variable = graph.Add(new Expr.Var(new EGraphNodeId(index)));
            Id zero = graph.Add(new Expr.Num(0));
            graph.Add(new Expr.Add(variable, zero));
        }

        SaturationResult result = graph.Saturate(SaturationConfig.Default);

        Assert.True(result.MemoryBytes > 0);
        Assert.True(result.NodeCount > 0);
    }

    [Fact]
    public void SaturateMemoryBounded()
    {
        var graph = new EGraph();
        for (ushort index = 0; index < 500; index++)
        {
            Id variable = graph.Add(new Expr.Var(new EGraphNodeId(index)));
            Id zero = graph.Add(new Expr.Num(0));
            graph.Add(new Expr.Add(variable, zero));
        }

        graph.Saturate(SaturationConfig.Default);

        Assert.True(graph.MemoryUsage() < 10 * 1024 * 1024);
    }

    [Fact]
    public void SolveFixedConstraints()
    {
        Constraint[] constraints = [Constraint.Fixed(50), Constraint.Fixed(100), Constraint.Fixed(50)];
        Assert.Equal(
            new ushort[] { 50, 100, 50 },
            EGraphLayout.SolveLayoutDefault(constraints, 200).ToArray());
    }

    [Fact]
    public void SolvePercentageConstraints()
    {
        Constraint[] constraints = [Constraint.Percentage(25.0f), Constraint.Percentage(75.0f)];
        Assert.Equal(
            new ushort[] { 50, 150 },
            EGraphLayout.SolveLayoutDefault(constraints, 200).ToArray());
    }

    [Fact]
    public void SolveRatioConstraints()
    {
        Constraint[] constraints = [Constraint.Ratio(1, 3), Constraint.Ratio(2, 3)];
        Assert.Equal(
            new ushort[] { 100, 200 },
            EGraphLayout.SolveLayoutDefault(constraints, 300).ToArray());
    }

    [Fact]
    public void SolveFillConstraint()
    {
        Constraint[] constraints = [Constraint.Fill];
        Assert.Equal(new ushort[] { 120 }, EGraphLayout.SolveLayoutDefault(constraints, 120).ToArray());
    }

    [Fact]
    public void SolveMixedConstraints()
    {
        Constraint[] constraints =
        [
            Constraint.Fixed(30),
            Constraint.Percentage(50.0f),
            Constraint.Ratio(1, 4),
        ];

        Assert.Equal(
            new ushort[] { 30, 100, 50 },
            EGraphLayout.SolveLayoutDefault(constraints, 200).ToArray());
    }

    [Fact]
    public void SolveEmptyConstraints()
    {
        Assert.Empty(EGraphLayout.SolveLayoutDefault([], 200));
    }

    [Fact]
    public void SolveReturnsSaturationResult()
    {
        Constraint[] constraints = [Constraint.Fixed(50)];
        (IReadOnlyList<ushort> sizes, SaturationResult result) =
            EGraphLayout.SolveLayout(constraints, 200, SaturationConfig.Default);

        Assert.Equal(new ushort[] { 50 }, sizes.ToArray());
        Assert.True(result.Saturated);
    }

    [Fact]
    public void Solve500Widgets()
    {
        Constraint[] constraints = Enumerable.Range(0, 500)
            .Select(index => Constraint.Fixed((ushort)(index % 100)))
            .ToArray();
        SaturationConfig config = SaturationConfig.Default;

        (IReadOnlyList<ushort> sizes, SaturationResult result) =
            EGraphLayout.SolveLayout(constraints, 1_000, config);

        Assert.Equal(500, sizes.Count);
        for (int index = 0; index < sizes.Count; index++)
            Assert.Equal((ushort)(index % 100), sizes[index]);
        Assert.True(!result.StoppedEarly || result.NodeCount <= config.NodeBudget + 500);
    }

    [Fact]
    public void SolveFitContentBounded()
    {
        Constraint[] constraints = [Constraint.FitContentBounded(10, 50)];
        Assert.Single(EGraphLayout.SolveLayoutDefault(constraints, 200));
    }

    [Fact]
    public void ConfigDefaultValues()
    {
        SaturationConfig config = SaturationConfig.Default;
        Assert.Equal(10_000, config.NodeBudget);
        Assert.Equal(5_000UL, config.TimeLimitMicroseconds);
        Assert.Equal(100, config.IterationLimit);
        Assert.Equal(10 * 1024 * 1024, config.MemoryLimitBytes);
    }

    [Fact]
    public void FromEnvReturnsValidConfig()
    {
        SaturationConfig config = SaturationConfig.FromEnvironment();
        Assert.True(config.NodeBudget > 0);
        Assert.True(config.IterationLimit > 0);
    }

    [Fact]
    public void RandomConstraintsNeverOomOrHang()
    {
        Constraint[][] constraintSets =
        [
            Enumerable.Range(0, 1_000).Select(index => Constraint.Fixed((ushort)index)).ToArray(),
            Enumerable.Range(0, 500).Select(_ => Constraint.Fill).ToArray(),
            Enumerable.Range(0, 200).Select(index => Constraint.Percentage(index * 0.5f)).ToArray(),
            Enumerable.Range(0, 100).Select(index => Constraint.Ratio((uint)index + 1, 100)).ToArray(),
            Enumerable.Range(0, 300).Select(index => Constraint.Min((ushort)index)).ToArray(),
            Enumerable.Range(0, 300).Select(index => Constraint.Max((ushort)index)).ToArray(),
            Enumerable.Range(0, 500)
                .Select(index => Constraint.FitContentBounded((ushort)index, (ushort)(index + 100)))
                .ToArray(),
        ];
        var config = new SaturationConfig
        {
            NodeBudget = 10_000,
            IterationLimit = 100,
            TimeLimitMicroseconds = 50_000,
            MemoryLimitBytes = 10 * 1024 * 1024,
        };

        foreach (Constraint[] constraints in constraintSets)
        {
            (IReadOnlyList<ushort> sizes, SaturationResult result) =
                EGraphLayout.SolveLayout(constraints, 1_000, config);

            Assert.Equal(constraints.Length, sizes.Count);
            Assert.True(result.MemoryBytes <= config.MemoryLimitBytes + 1024 * 1024);
        }
    }

    [Fact]
    public void Fuzz1000RandomConstraintSets()
    {
        uint seed = 42;
        uint Next()
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return seed;
        }

        var config = new SaturationConfig
        {
            NodeBudget = 10_000,
            IterationLimit = 100,
            TimeLimitMicroseconds = 50_000,
            MemoryLimitBytes = 10 * 1024 * 1024,
        };

        for (int iteration = 0; iteration < 1_000; iteration++)
        {
            int count = (int)(Next() % 50 + 1);
            ushort total = (ushort)(Next() % 500 + 1);
            var constraints = new Constraint[count];
            for (int index = 0; index < constraints.Length; index++)
            {
                constraints[index] = (Next() % 7) switch
                {
                    0 => Constraint.Fixed((ushort)(Next() % ((uint)total + 1))),
                    1 => Constraint.Percentage(Next() % 101),
                    2 => Constraint.Min((ushort)(Next() % ((uint)total + 1))),
                    3 => Constraint.Max((ushort)(Next() % ((uint)total + 1))),
                    4 => Ratio(Next),
                    5 => Constraint.Fill,
                    _ => Bounded(Next, total),
                };
            }

            (IReadOnlyList<ushort> sizes, SaturationResult result) =
                EGraphLayout.SolveLayout(constraints, total, config);

            Assert.Equal(constraints.Length, sizes.Count);
            Assert.True(result.MemoryBytes < config.MemoryLimitBytes + 2 * 1024 * 1024);
        }
    }

    [Fact]
    public void DeterministicAcrossRuns()
    {
        Constraint[] constraints =
        [
            Constraint.Fixed(30),
            Constraint.Percentage(50.0f),
            Constraint.Ratio(1, 4),
            Constraint.Fill,
            Constraint.Min(10),
            Constraint.Max(80),
        ];
        var config = new SaturationConfig
        {
            NodeBudget = 10_000,
            IterationLimit = 100,
            TimeLimitMicroseconds = 0,
            MemoryLimitBytes = 0,
        };

        (IReadOnlyList<ushort> firstSizes, SaturationResult first) =
            EGraphLayout.SolveLayout(constraints, 200, config);
        (IReadOnlyList<ushort> secondSizes, SaturationResult second) =
            EGraphLayout.SolveLayout(constraints, 200, config);

        Assert.Equal(firstSizes.ToArray(), secondSizes.ToArray());
        Assert.Equal(first.Rewrites, second.Rewrites);
        Assert.Equal(first.Iterations, second.Iterations);
        Assert.Equal(first.NodeCount, second.NodeCount);
        Assert.Equal(first.Guard, second.Guard);
    }

    [Fact]
    public void EvidenceRecordFromResult()
    {
        Constraint[] constraints = [Constraint.Fixed(50), Constraint.Percentage(50.0f)];
        (_, SaturationResult result) = EGraphLayout.SolveLayout(
            constraints,
            200,
            SaturationConfig.Default);

        EvidenceRecord record = EvidenceRecord.FromResult("test_case", 2, 200, result);

        Assert.Equal("test_case", record.TestName);
        Assert.Equal(2, record.ConstraintCount);
        Assert.Equal((ushort)200, record.TotalSpace);
        Assert.True(record.NodesAtCompletion > 0);
    }

    [Fact]
    public void EvidenceRecordToJsonl()
    {
        var result = new SaturationResult(
            Rewrites: 5,
            Iterations: 3,
            Saturated: true,
            StoppedEarly: false,
            NodeCount: 42,
            Guard: GuardTriggered.None,
            TimeMicroseconds: 1_234,
            MemoryBytes: 8_192);
        EvidenceRecord record = EvidenceRecord.FromResult("demo", 10, 200, result);

        string jsonl = record.ToJsonl();

        Assert.StartsWith("{", jsonl);
        Assert.EndsWith("}", jsonl);
        Assert.Contains("\"test\":\"demo\"", jsonl);
        Assert.Contains("\"constraints\":10", jsonl);
        Assert.Contains("\"nodes\":42", jsonl);
        Assert.Contains("\"saturated\":true", jsonl);
        Assert.Contains("\"guard\":\"None\"", jsonl);
    }

    [Fact]
    public void EvidenceRecordsForAllGuards()
    {
        foreach (GuardTriggered guard in Enum.GetValues<GuardTriggered>())
        {
            var result = new SaturationResult(
                Rewrites: 0,
                Iterations: 1,
                Saturated: guard == GuardTriggered.None,
                StoppedEarly: guard != GuardTriggered.None,
                NodeCount: 10,
                Guard: guard,
                TimeMicroseconds: 100,
                MemoryBytes: 1_024);
            EvidenceRecord record = EvidenceRecord.FromResult("guard_test", 1, 100, result);
            Assert.Contains(guard.ToString(), record.ToJsonl());
        }
    }

    [Fact]
    public void EmptyInputProducesEmptyLayout()
    {
        (IReadOnlyList<ushort> sizes, SaturationResult result) =
            EGraphLayout.SolveLayout([], 200, SaturationConfig.Default);

        Assert.Empty(sizes);
        Assert.True(result.Saturated);
    }

    [Fact]
    public void SingleConstraintNoRewritingNeeded()
    {
        (IReadOnlyList<ushort> sizes, SaturationResult result) = EGraphLayout.SolveLayout(
            [Constraint.Fixed(42)],
            200,
            SaturationConfig.Default);

        Assert.Equal(new ushort[] { 42 }, sizes.ToArray());
        Assert.True(result.Saturated);
        Assert.Equal(GuardTriggered.None, result.Guard);
    }

    [Fact]
    public void ZeroTotalSpace()
    {
        Constraint[] constraints =
        [
            Constraint.Fixed(50),
            Constraint.Percentage(50.0f),
            Constraint.Fill,
        ];

        (IReadOnlyList<ushort> sizes, _) =
            EGraphLayout.SolveLayout(constraints, 0, SaturationConfig.Default);

        Assert.Equal(3, sizes.Count);
        Assert.Equal((ushort)50, sizes[0]);
        Assert.Equal((ushort)0, sizes[1]);
    }

    private static Constraint Ratio(Func<uint> next)
    {
        uint denominator = next() % 10 + 1;
        uint numerator = next() % (denominator + 1);
        return Constraint.Ratio(numerator, denominator);
    }

    private static Constraint Bounded(Func<uint> next, ushort total)
    {
        ushort minimum = (ushort)(next() % ((uint)total + 1));
        ushort delta = (ushort)(next() % 100);
        ushort maximum = (ushort)Math.Min(ushort.MaxValue, minimum + (int)delta);
        return Constraint.FitContentBounded(minimum, maximum);
    }
}
