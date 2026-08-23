// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/responsive.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust's T: Clone contract has no direct unconstrained C# equivalent.
// Builder copies preserve slot independence, while values follow normal C# value/
// reference semantics. ResolveCloned clones ICloneable values and otherwise returns
// the resolved value. Explicit slot state is tracked separately so null remains a
// valid explicit base value.

namespace FrankenTui.Layout;

/// <summary>
/// A breakpoint-aware value whose unset tiers inherit from the nearest smaller tier.
/// </summary>
public sealed class Responsive<T> : IEquatable<Responsive<T>>, ICloneable
{
    private readonly Slot[] _values;

    public Responsive(T baseValue)
    {
        _values = new Slot[5];
        _values[0] = new Slot(true, baseValue);
    }

    public Responsive()
    {
        _values = new Slot[5];
        _values[0] = new Slot(true, default);
    }

    private Responsive(Slot[] values) => _values = values;

    public static Responsive<T> New(T baseValue) => new(baseValue);

    public static Responsive<T> Default => new();

    /// <summary>Return a copy with an explicit value at <paramref name="breakpoint"/>.</summary>
    public Responsive<T> At(Breakpoint breakpoint, T value)
    {
        Responsive<T> result = Clone();
        result.Set(breakpoint, value);
        return result;
    }

    public void Set(Breakpoint breakpoint, T value) =>
        _values[breakpoint.Index()] = new Slot(true, value);

    public void Clear(Breakpoint breakpoint)
    {
        if (breakpoint != Breakpoint.Xs)
            _values[breakpoint.Index()] = default;
    }

    public T Resolve(Breakpoint breakpoint)
    {
        for (int index = breakpoint.Index(); index >= 0; index--)
        {
            if (_values[index].IsSet)
                return _values[index].Value!;
        }

        // The constructor always sets Xs; this is a defensive invariant check.
        throw new InvalidOperationException("Xs always has a value.");
    }

    public T ResolveCloned(Breakpoint breakpoint)
    {
        T value = Resolve(breakpoint);
        return value is ICloneable cloneable ? (T)cloneable.Clone() : value;
    }

    public bool HasExplicit(Breakpoint breakpoint) => _values[breakpoint.Index()].IsSet;

    public IEnumerable<(Breakpoint Breakpoint, T Value)> ExplicitValues()
    {
        foreach (Breakpoint breakpoint in BreakpointExtensions.All)
        {
            Slot slot = _values[breakpoint.Index()];
            if (slot.IsSet)
                yield return (breakpoint, slot.Value!);
        }
    }

    public Responsive<TOutput> Map<TOutput>(Func<T, TOutput> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        Responsive<TOutput>? result = null;
        foreach ((Breakpoint breakpoint, T value) in ExplicitValues())
        {
            TOutput mapped = map(value);
            if (breakpoint == Breakpoint.Xs)
                result = new Responsive<TOutput>(mapped);
            else
                result!.Set(breakpoint, mapped);
        }

        return result ?? throw new InvalidOperationException("Xs always has a value.");
    }

    public Responsive<T> Clone() => new((Slot[])_values.Clone());

    object ICloneable.Clone() => Clone();

    public bool Equals(Responsive<T>? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int index = 0; index < _values.Length; index++)
        {
            Slot left = _values[index];
            Slot right = other._values[index];
            if (left.IsSet != right.IsSet) return false;
            if (left.IsSet && !comparer.Equals(left.Value!, right.Value!)) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is Responsive<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (Slot slot in _values)
        {
            hash.Add(slot.IsSet);
            if (slot.IsSet) hash.Add(slot.Value!, EqualityComparer<T>.Default);
        }
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"Responsive({string.Join(", ", ExplicitValues().Select(static item => $"{item.Breakpoint.Label()}={item.Value}"))})";

    private readonly record struct Slot(bool IsSet, T? Value);
}
