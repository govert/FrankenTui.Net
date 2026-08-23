// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/egraph.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: The Rust module maps to the FrankenTui.Layout.Egraph namespace so its
// module-local NodeId does not collide with dep_graph::NodeId. Rust associated-data
// enum variants map to a closed Expr record hierarchy. usize counts/budgets map to
// non-negative Int32-scale storage; public returned Vec values are defensive arrays.
// Rust's capacity-based memory estimate is reproduced as a deterministic logical
// Rust-layout estimate (32-byte Expr, 24-byte Vec header), not CLR heap telemetry.
// Instant maps to Stopwatch. Rust Debug/Clone/Default map to record value behavior,
// ToString summaries, public constructors, and Default factories. Upstream has no
// production route from Flex into this experimental solver, so none is introduced.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace FrankenTui.Layout.Egraph;

/// <summary>An identifier for an equivalence class in an e-graph.</summary>
public readonly record struct Id
{
    internal Id(uint value) => Value = value;

    internal uint Value { get; }

    internal int Index => checked((int)Value);

    /// <inheritdoc />
    public override string ToString() => $"Id({Value})";
}

/// <summary>A widget node identifier used by layout-variable expressions.</summary>
public readonly record struct NodeId(uint Value);

/// <summary>The closed layout-expression language stored in an e-graph.</summary>
public abstract record Expr
{
    private Expr()
    {
    }

    /// <summary>A concrete pixel or cell value.</summary>
    public sealed record Num(ushort Value) : Expr;

    /// <summary>A reference to a widget's size.</summary>
    public sealed record Var(NodeId Node) : Expr;

    /// <summary>Addition.</summary>
    public sealed record Add(Id Left, Id Right) : Expr;

    /// <summary>Subtraction.</summary>
    public sealed record Sub(Id Left, Id Right) : Expr;

    /// <summary>Maximum.</summary>
    public sealed record Max(Id Left, Id Right) : Expr;

    /// <summary>Minimum.</summary>
    public sealed record Min(Id Left, Id Right) : Expr;

    /// <summary>Division.</summary>
    public sealed record Div(Id Left, Id Right) : Expr;

    /// <summary>Multiplication.</summary>
    public sealed record Mul(Id Left, Id Right) : Expr;

    /// <summary>Clamp a value between a minimum and maximum.</summary>
    public sealed record Clamp(Id Minimum, Id Maximum, Id Value) : Expr;

    /// <summary>A horizontal flex container.</summary>
    public sealed record HFlex : Expr
    {
        private readonly Id[] _children;
        private readonly ReadOnlyCollection<Id> _view;

        /// <summary>Create a horizontal flex expression with owned child identifiers.</summary>
        public HFlex(IEnumerable<Id> children)
        {
            ArgumentNullException.ThrowIfNull(children);
            _children = children.ToArray();
            _view = Array.AsReadOnly(_children);
        }

        /// <summary>Child expression identifiers.</summary>
        public IReadOnlyList<Id> Children => _view;

        /// <inheritdoc />
        public bool Equals(HFlex? other) =>
            other is not null && _children.AsSpan().SequenceEqual(other._children);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(typeof(HFlex));
            foreach (Id child in _children)
                hash.Add(child);
            return hash.ToHashCode();
        }
    }

    /// <summary>A vertical flex container.</summary>
    public sealed record VFlex : Expr
    {
        private readonly Id[] _children;
        private readonly ReadOnlyCollection<Id> _view;

        /// <summary>Create a vertical flex expression with owned child identifiers.</summary>
        public VFlex(IEnumerable<Id> children)
        {
            ArgumentNullException.ThrowIfNull(children);
            _children = children.ToArray();
            _view = Array.AsReadOnly(_children);
        }

        /// <summary>Child expression identifiers.</summary>
        public IReadOnlyList<Id> Children => _view;

        /// <inheritdoc />
        public bool Equals(VFlex? other) =>
            other is not null && _children.AsSpan().SequenceEqual(other._children);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(typeof(VFlex));
            foreach (Id child in _children)
                hash.Add(child);
            return hash.ToHashCode();
        }
    }

    /// <summary>Fill the remaining space in a container.</summary>
    public sealed record Fill(Id Value) : Expr;
}

/// <summary>
/// An equivalence graph for layout expressions, with union-find equivalence classes,
/// structural hash-consing, equality saturation, and cheapest-expression extraction.
/// </summary>
public sealed class EGraph
{
    private const int LogicalExprSize = 32;
    private const int LogicalVectorSize = 24;
    private const int LogicalIdSize = 4;

    private readonly List<uint> _parents = [];
    private readonly List<byte> _ranks = [];
    private readonly List<List<Expr>> _nodes = [];
    private readonly Dictionary<Expr, Id> _memo = [];
    private int _lastApplyCount;

    /// <summary>Create a new empty e-graph.</summary>
    public EGraph()
    {
    }

    /// <summary>Create a new empty e-graph.</summary>
    public static EGraph New() => new();

    /// <summary>Add an expression, returning its canonical equivalence-class id.</summary>
    public Id Add(Expr expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expr canonical = Canonicalize(expression);
        if (_memo.TryGetValue(canonical, out Id existing))
            return Find(existing);

        var id = new Id(checked((uint)_parents.Count));
        _parents.Add(id.Value);
        _ranks.Add(0);
        _nodes.Add([canonical]);
        _memo[canonical] = id;
        return id;
    }

    /// <summary>Whether two identifiers belong to the same equivalence class.</summary>
    public bool Equiv(Id left, Id right) => Find(left) == Find(right);

    /// <summary>Merge two equivalence classes and return the winning representative.</summary>
    public Id Merge(Id left, Id right)
    {
        Id first = Find(left);
        Id second = Find(right);
        if (first == second)
            return first;

        (Id Winner, Id Loser) pair = _ranks[first.Index] >= _ranks[second.Index]
            ? (first, second)
            : (second, first);
        _parents[pair.Loser.Index] = pair.Winner.Value;
        if (_ranks[pair.Winner.Index] == _ranks[pair.Loser.Index])
            _ranks[pair.Winner.Index] = unchecked((byte)(_ranks[pair.Winner.Index] + 1));

        List<Expr> losingNodes = _nodes[pair.Loser.Index];
        _nodes[pair.Loser.Index] = [];
        _nodes[pair.Winner.Index].AddRange(losingNodes);
        return pair.Winner;
    }

    /// <summary>Find an equivalence class's canonical representative.</summary>
    public Id Find(Id id)
    {
        while (_parents[id.Index] != id.Value)
            id = new Id(_parents[id.Index]);
        return id;
    }

    /// <summary>Number of current equivalence classes.</summary>
    public int ClassCount()
    {
        int count = 0;
        for (int index = 0; index < _parents.Count; index++)
        {
            if (_parents[index] == (uint)index)
                count++;
        }

        return count;
    }

    /// <summary>Total number of stored expression nodes.</summary>
    public int NodeCount() => _nodes.Sum(static nodes => nodes.Count);

    /// <summary>Rule applications in the last saturation pass.</summary>
    public int LastApplyCount() => _lastApplyCount;

    /// <summary>Apply rewrite rules until saturation or 100 iterations.</summary>
    public int ApplyRules() => ApplyRulesWithBudget(100);

    /// <summary>Apply rewrite rules with an explicit iteration budget.</summary>
    public int ApplyRulesWithBudget(int maxIterations)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxIterations);
        int total = 0;
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            int applied = ApplyRulesOnce();
            total = checked(total + applied);
            _lastApplyCount = total;
            if (applied == 0)
                break;
        }

        return total;
    }

    /// <summary>Extract the lowest-cost expression in an equivalence class.</summary>
    public Expr Extract(Id id)
    {
        Id canonical = Find(id);
        List<Expr> expressions = _nodes[canonical.Index];
        if (expressions.Count == 0)
            throw new InvalidOperationException("An e-class must contain at least one expression.");

        Expr best = expressions[0];
        uint bestCost = Cost(best);
        for (int index = 1; index < expressions.Count; index++)
        {
            uint candidateCost = Cost(expressions[index]);
            if (candidateCost < bestCost)
            {
                best = expressions[index];
                bestCost = candidateCost;
            }
        }

        return best;
    }

    /// <summary>Run equality saturation with explicit resource bounds.</summary>
    public SaturationResult Saturate(SaturationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var stopwatch = Stopwatch.StartNew();
        int rewrites = 0;
        int iterations = 0;

        SaturationResult Result(bool saturated, GuardTriggered guard) => new(
            rewrites,
            iterations,
            saturated,
            guard != GuardTriggered.None,
            NodeCount(),
            guard,
            ElapsedMicroseconds(stopwatch),
            MemoryUsage());

        for (int iteration = 0; iteration < config.IterationLimit; iteration++)
        {
            if (NodeCount() >= config.NodeBudget)
                return Result(false, GuardTriggered.NodeBudget);

            if (config.TimeLimitMicroseconds > 0 &&
                ElapsedMicroseconds(stopwatch) >= config.TimeLimitMicroseconds)
            {
                return Result(false, GuardTriggered.Timeout);
            }

            if (config.MemoryLimitBytes > 0 && MemoryUsage() >= config.MemoryLimitBytes)
                return Result(false, GuardTriggered.Memory);

            int applied = ApplyRulesOnce();
            rewrites = checked(rewrites + applied);
            iterations++;
            if (applied == 0)
            {
                _lastApplyCount = rewrites;
                return Result(true, GuardTriggered.None);
            }
        }

        _lastApplyCount = rewrites;
        return Result(false, GuardTriggered.IterationLimit);
    }

    /// <summary>
    /// Return the deterministic Rust-layout-equivalent retained-capacity estimate.
    /// </summary>
    public int MemoryUsage()
    {
        long parents = (long)_parents.Capacity * sizeof(uint);
        long ranks = _ranks.Capacity;
        long nodes = _nodes.Sum(nodes => (long)nodes.Capacity * LogicalExprSize);
        long nodeVectors = (long)_nodes.Capacity * LogicalVectorSize;
        long memo = (long)_memo.EnsureCapacity(0) * (LogicalExprSize + LogicalIdSize);
        long total = parents + ranks + nodes + nodeVectors + memo;
        return total >= int.MaxValue ? int.MaxValue : (int)total;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"EGraph {{ classes: {ClassCount()}, nodes: {NodeCount()}, last_apply_count: {_lastApplyCount} }}";

    private int ApplyRulesOnce()
    {
        var merges = new List<(Id Left, Id Right)>();
        var newNodes = new List<(Id ClassId, Expr Expression)>();
        int classCount = _parents.Count;

        for (int classIndex = 0; classIndex < classCount; classIndex++)
        {
            var classId = new Id((uint)classIndex);
            if (Find(classId) != classId)
                continue;

            Expr[] expressions = _nodes[classIndex].ToArray();
            foreach (Expr expression in expressions)
            {
                if (RewriteSimplify(expression) is { } target &&
                    Find(classId) != Find(target))
                {
                    merges.Add((classId, target));
                }

                RewriteConstruct(expression, classId, newNodes);
            }
        }

        foreach ((Id classId, Expr expression) in newNodes)
        {
            Id newId = Add(expression);
            if (Find(classId) != Find(newId))
                merges.Add((classId, newId));
        }

        merges.Sort((left, right) =>
        {
            (uint A, uint B) leftKey = (Find(left.Left).Value, Find(left.Right).Value);
            (uint A, uint B) rightKey = (Find(right.Left).Value, Find(right.Right).Value);
            int first = leftKey.A.CompareTo(rightKey.A);
            return first != 0 ? first : leftKey.B.CompareTo(rightKey.B);
        });

        var unique = new List<(Id Left, Id Right)>(merges.Count);
        (Id Left, Id Right)? previous = null;
        foreach ((Id left, Id right) in merges)
        {
            (Id Left, Id Right) canonical = (Find(left), Find(right));
            if (previous is { } seen &&
                seen.Left == canonical.Left &&
                seen.Right == canonical.Right)
            {
                continue;
            }

            unique.Add((left, right));
            previous = canonical;
        }

        int count = unique.Count(pair => Find(pair.Left) != Find(pair.Right));
        foreach ((Id left, Id right) in unique)
            Merge(left, right);
        return count;
    }

    private Id? RewriteSimplify(Expr expression) => expression switch
    {
        Expr.Add(var left, var right) when IsZero(right) => left,
        Expr.Add(var left, var right) when IsZero(left) => right,
        Expr.Sub(var left, var right) when IsZero(right) => left,
        Expr.Sub(var left, var right) when Find(left) == Find(right) => FindNum(0),
        Expr.Mul(var left, var right) when IsOne(right) => left,
        Expr.Mul(var left, var right) when IsOne(left) => right,
        Expr.Mul(var left, _) when IsZero(left) => left,
        Expr.Mul(_, var right) when IsZero(right) => right,
        Expr.Max(var left, var right) when Find(left) == Find(right) => left,
        Expr.Max(var left, var right) when IsZero(right) => left,
        Expr.Max(var left, var right) when IsZero(left) => right,
        Expr.Min(var left, var right) when Find(left) == Find(right) => left,
        Expr.Clamp(var min, var max, var value) when IsZero(min) && IsMaximum(max) => value,
        Expr.Clamp(var min, var max, _) when Find(min) == Find(max) => min,
        Expr.Clamp(var min, _, var value) when Find(value) == Find(min) => min,
        Expr.Clamp(_, var max, var value) when Find(value) == Find(max) => max,
        Expr.Div(var left, var right) when IsOne(right) => left,
        Expr.Div(var left, var right) when Find(left) == Find(right) && !IsZero(left) => FindNum(1),
        _ => null,
    };

    private void RewriteConstruct(
        Expr expression,
        Id classId,
        ICollection<(Id ClassId, Expr Expression)> output)
    {
        switch (expression)
        {
            case Expr.Add(var left, var right) when Find(left) != Find(right):
                output.Add((classId, new Expr.Add(right, left)));
                break;
            case Expr.Max(var left, var right) when Find(left) != Find(right):
                output.Add((classId, new Expr.Max(right, left)));
                break;
            case Expr.Min(var left, var right) when Find(left) != Find(right):
                output.Add((classId, new Expr.Min(right, left)));
                break;
            case Expr.Mul(var left, var right) when Find(left) != Find(right):
                output.Add((classId, new Expr.Mul(right, left)));
                break;
        }
    }

    private Id? FindNum(ushort value) =>
        _memo.TryGetValue(new Expr.Num(value), out Id id) ? Find(id) : null;

    private bool IsZero(Id id) => ContainsNumber(id, 0);

    private bool IsOne(Id id) => ContainsNumber(id, 1);

    private bool IsMaximum(Id id) => ContainsNumber(id, ushort.MaxValue);

    private bool ContainsNumber(Id id, ushort value)
    {
        Id canonical = Find(id);
        return _nodes[canonical.Index].Any(expression => expression is Expr.Num number && number.Value == value);
    }

    private Expr Canonicalize(Expr expression) => expression switch
    {
        Expr.Num number => number,
        Expr.Var variable => variable,
        Expr.Add(var left, var right) => new Expr.Add(Find(left), Find(right)),
        Expr.Sub(var left, var right) => new Expr.Sub(Find(left), Find(right)),
        Expr.Max(var left, var right) => new Expr.Max(Find(left), Find(right)),
        Expr.Min(var left, var right) => new Expr.Min(Find(left), Find(right)),
        Expr.Div(var left, var right) => new Expr.Div(Find(left), Find(right)),
        Expr.Mul(var left, var right) => new Expr.Mul(Find(left), Find(right)),
        Expr.Clamp(var min, var max, var value) => new Expr.Clamp(Find(min), Find(max), Find(value)),
        Expr.HFlex horizontal => new Expr.HFlex(horizontal.Children.Select(Find)),
        Expr.VFlex vertical => new Expr.VFlex(vertical.Children.Select(Find)),
        Expr.Fill fill => new Expr.Fill(Find(fill.Value)),
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unknown expression kind."),
    };

    private static uint Cost(Expr expression) => expression switch
    {
        Expr.Num => 1,
        Expr.Var => 2,
        Expr.Add or Expr.Sub or Expr.Max or Expr.Min => 3,
        Expr.Mul or Expr.Div => 4,
        Expr.Fill => 3,
        Expr.Clamp => 5,
        Expr.HFlex horizontal => checked(10U + (uint)horizontal.Children.Count),
        Expr.VFlex vertical => checked(10U + (uint)vertical.Children.Count),
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unknown expression kind."),
    };

    private static ulong ElapsedMicroseconds(Stopwatch stopwatch) =>
        (ulong)((UInt128)(ulong)stopwatch.ElapsedTicks * 1_000_000UL / (ulong)Stopwatch.Frequency);
}

/// <summary>Resource bounds for an equality-saturation run.</summary>
public sealed record SaturationConfig
{
    /// <summary>Maximum expression nodes before stopping.</summary>
    public int NodeBudget { get; set; } = 10_000;

    /// <summary>Maximum rewrite iterations.</summary>
    public int IterationLimit { get; set; } = 100;

    /// <summary>Maximum elapsed microseconds; zero is unlimited.</summary>
    public ulong TimeLimitMicroseconds { get; set; } = 5_000;

    /// <summary>Maximum logical memory bytes; zero is unlimited.</summary>
    public int MemoryLimitBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Create a fresh default configuration.</summary>
    public static SaturationConfig Default => new();

    /// <summary>Create a configuration from the four upstream environment variables.</summary>
    public static SaturationConfig FromEnvironment()
    {
        var config = new SaturationConfig();

        if (TryReadNonNegativeInt("FRANKENTUI_EGRAPH_NODE_BUDGET", out int nodeBudget))
            config.NodeBudget = nodeBudget;
        if (ulong.TryParse(
                Environment.GetEnvironmentVariable("FRANKENTUI_EGRAPH_TIMEOUT_MS"),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out ulong milliseconds))
        {
            config.TimeLimitMicroseconds = unchecked(milliseconds * 1_000UL);
        }
        if (TryReadNonNegativeInt("FRANKENTUI_EGRAPH_MAX_ITERS", out int iterations))
            config.IterationLimit = iterations;
        if (TryReadNonNegativeInt("FRANKENTUI_EGRAPH_MEMORY_MB", out int megabytes))
            config.MemoryLimitBytes = unchecked(megabytes * 1024 * 1024);

        return config;
    }

    private static bool TryReadNonNegativeInt(string name, out int value)
    {
        bool parsed = int.TryParse(
            Environment.GetEnvironmentVariable(name),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value);
        return parsed && value >= 0;
    }
}

/// <summary>The resource guard that stopped equality saturation.</summary>
public enum GuardTriggered
{
    /// <summary>No guard fired.</summary>
    None,
    /// <summary>The node budget was reached.</summary>
    NodeBudget,
    /// <summary>The wall-clock time limit was reached.</summary>
    Timeout,
    /// <summary>The logical memory limit was reached.</summary>
    Memory,
    /// <summary>The rewrite iteration limit was reached.</summary>
    IterationLimit,
}

/// <summary>Result of an equality-saturation run.</summary>
public sealed record SaturationResult(
    int Rewrites,
    int Iterations,
    bool Saturated,
    bool StoppedEarly,
    int NodeCount,
    GuardTriggered Guard,
    ulong TimeMicroseconds,
    int MemoryBytes);

/// <summary>Module-level encoding and solving functions from upstream egraph.rs.</summary>
public static class EGraphLayout
{
    /// <summary>Encode one layout constraint as an e-graph expression.</summary>
    public static Id EncodeConstraint(
        EGraph graph,
        FrankenTui.Layout.Constraint constraint,
        ushort total)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(constraint);

        return constraint switch
        {
            FrankenTui.Layout.Constraint.FixedValue fixedValue =>
                graph.Add(new Expr.Num(fixedValue.Value)),
            FrankenTui.Layout.Constraint.MinValue minimum => EncodeClamp(
                graph,
                minimum.Value,
                total,
                minimum.Value),
            FrankenTui.Layout.Constraint.MaxValue maximum => EncodeMaximum(
                graph,
                maximum.Value),
            FrankenTui.Layout.Constraint.PercentageValue percentage => graph.Add(
                new Expr.Num(FloatToUInt16Saturating(
                    percentage.Value / 100.0f * total))),
            FrankenTui.Layout.Constraint.RatioValue ratio => graph.Add(
                new Expr.Num(EvaluateRatio(total, ratio.Numerator, ratio.Denominator))),
            FrankenTui.Layout.Constraint.FillValue or
            FrankenTui.Layout.Constraint.FitContentValue or
            FrankenTui.Layout.Constraint.FitMinValue => graph.Add(
                new Expr.Fill(graph.Add(new Expr.Num(total)))),
            FrankenTui.Layout.Constraint.FitContentBoundedValue bounded => EncodeClamp(
                graph,
                bounded.Minimum,
                bounded.Maximum,
                total),
            _ => throw new ArgumentOutOfRangeException(nameof(constraint), constraint, "Unknown constraint kind."),
        };
    }

    /// <summary>Encode a horizontal or vertical flex container.</summary>
    public static Id EncodeFlex(EGraph graph, IReadOnlyList<Id> children, bool horizontal)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(children);
        return graph.Add(horizontal ? new Expr.HFlex(children) : new Expr.VFlex(children));
    }

    /// <summary>Solve constraints through bounded equality saturation.</summary>
    public static (IReadOnlyList<ushort> Sizes, SaturationResult Result) SolveLayout(
        IReadOnlyList<FrankenTui.Layout.Constraint> constraints,
        ushort total,
        SaturationConfig config)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(config);
        var graph = new EGraph();
        Id[] ids = constraints.Select(constraint => EncodeConstraint(graph, constraint, total)).ToArray();
        SaturationResult result = graph.Saturate(config);
        ushort[] sizes = ids.Select(id => graph.Extract(id) is Expr.Num number ? number.Value : total).ToArray();
        return (sizes, result);
    }

    /// <summary>Solve constraints with a fresh default configuration.</summary>
    public static IReadOnlyList<ushort> SolveLayoutDefault(
        IReadOnlyList<FrankenTui.Layout.Constraint> constraints,
        ushort total) =>
        SolveLayout(constraints, total, SaturationConfig.Default).Sizes;

    private static Id EncodeClamp(EGraph graph, ushort minimum, ushort maximum, ushort value)
    {
        Id min = graph.Add(new Expr.Num(minimum));
        Id max = graph.Add(new Expr.Num(maximum));
        Id val = graph.Add(new Expr.Num(value));
        return graph.Add(new Expr.Clamp(min, max, val));
    }

    private static Id EncodeMaximum(EGraph graph, ushort maximum)
    {
        // Preserve upstream insertion order: max is allocated before zero.
        Id max = graph.Add(new Expr.Num(maximum));
        Id zero = graph.Add(new Expr.Num(0));
        return graph.Add(new Expr.Clamp(zero, max, max));
    }

    private static ushort FloatToUInt16Saturating(float value)
    {
        if (float.IsNaN(value) || value <= 0.0f)
            return 0;
        if (value >= ushort.MaxValue)
            return ushort.MaxValue;
        return (ushort)value;
    }

    private static ushort EvaluateRatio(ushort total, uint numerator, uint denominator)
    {
        ulong product = (ulong)total * numerator;
        if (product > uint.MaxValue || denominator == 0)
            return 0;
        return unchecked((ushort)((uint)product / denominator));
    }
}

/// <summary>A structured JSONL evidence record for a saturation run.</summary>
public sealed record EvidenceRecord(
    string TestName,
    int ConstraintCount,
    ushort TotalSpace,
    int NodesAtCompletion,
    int Iterations,
    ulong TimeMicroseconds,
    int MemoryBytes,
    GuardTriggered GuardTriggered,
    bool Saturated,
    int Rewrites)
{
    /// <summary>Create an evidence record from a solve result.</summary>
    public static EvidenceRecord FromResult(
        string testName,
        int constraintCount,
        ushort total,
        SaturationResult result)
    {
        ArgumentNullException.ThrowIfNull(testName);
        ArgumentNullException.ThrowIfNull(result);
        return new EvidenceRecord(
            testName,
            constraintCount,
            total,
            result.NodeCount,
            result.Iterations,
            result.TimeMicroseconds,
            result.MemoryBytes,
            result.Guard,
            result.Saturated,
            result.Rewrites);
    }

    /// <summary>Serialize this evidence record to one source-compatible JSONL line.</summary>
    public string ToJsonl() => FormattableString.Invariant(
        $"{{\"test\":\"{TestName}\",\"constraints\":{ConstraintCount},\"total\":{TotalSpace},\"nodes\":{NodesAtCompletion},\"iterations\":{Iterations},\"time_us\":{TimeMicroseconds},\"memory_bytes\":{MemoryBytes},\"guard\":\"{GuardTriggered}\",\"saturated\":{Saturated.ToString().ToLowerInvariant()},\"rewrites\":{Rewrites}}}");
}
