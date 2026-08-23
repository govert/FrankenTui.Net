// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/animation/group.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Globalization;

namespace FrankenTui.Core;

/// <summary>A collection of named animations with shared lifecycle control.</summary>
/// <remarks>
/// Labels are unique and replacement preserves insertion order. The group value is the
/// arithmetic mean of member values. An empty group has value zero and is complete.
/// </remarks>
public sealed class AnimationGroup : IAnimation
{
    private readonly List<GroupMember> _members = [];

    /// <summary>Number of animations in the group.</summary>
    public int Count => _members.Count;

    /// <summary>Whether the group contains no animations.</summary>
    public bool IsEmpty => _members.Count == 0;

    /// <summary>Whether every animation in the group has completed.</summary>
    public bool AllComplete
    {
        get
        {
            foreach (var member in _members)
            {
                if (!member.Animation.IsComplete)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Average progress across all animations, or zero for an empty group.</summary>
    public float OverallProgress
    {
        get
        {
            if (_members.Count == 0)
            {
                return 0.0f;
            }

            var sum = 0.0f;
            foreach (var member in _members)
            {
                sum += member.Animation.Value;
            }

            return sum / _members.Count;
        }
    }

    /// <inheritdoc />
    public bool IsComplete => AllComplete;

    /// <inheritdoc />
    public float Value => OverallProgress;

    /// <summary>Add a named animation and return this group for fluent construction.</summary>
    /// <remarks>If the label exists, its animation is replaced in place.</remarks>
    public AnimationGroup Add(string label, IAnimation animation)
    {
        Insert(label, animation);
        return this;
    }

    /// <summary>Insert or replace a named animation.</summary>
    public void Insert(string label, IAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(animation);

        foreach (var member in _members)
        {
            if (string.Equals(member.Label, label, StringComparison.Ordinal))
            {
                member.Animation = animation;
                return;
            }
        }

        _members.Add(new GroupMember(label, animation));
    }

    /// <summary>Remove a named animation.</summary>
    /// <returns><see langword="true"/> when a member was found and removed.</returns>
    public bool Remove(string label)
    {
        ArgumentNullException.ThrowIfNull(label);

        for (var index = 0; index < _members.Count; index++)
        {
            if (!string.Equals(_members[index].Label, label, StringComparison.Ordinal))
            {
                continue;
            }

            _members.RemoveAt(index);
            return true;
        }

        return false;
    }

    /// <summary>Reset all animations to their initial state.</summary>
    public void StartAll() => Reset();

    /// <summary>Reset all animations, matching the upstream cancel semantics.</summary>
    public void CancelAll() => StartAll();

    /// <summary>Get a named animation, or <see langword="null"/> when absent.</summary>
    public IAnimation? Get(string label)
    {
        ArgumentNullException.ThrowIfNull(label);

        foreach (var member in _members)
        {
            if (string.Equals(member.Label, label, StringComparison.Ordinal))
            {
                return member.Animation;
            }
        }

        return null;
    }

    /// <summary>Get an animation by insertion index, or <see langword="null"/> when absent.</summary>
    public IAnimation? GetAt(int index) =>
        index >= 0 && index < _members.Count ? _members[index].Animation : null;

    /// <summary>Enumerate label-animation pairs in insertion order.</summary>
    public IEnumerable<(string Label, IAnimation Animation)> Iter()
    {
        foreach (var member in _members)
        {
            yield return (member.Label, member.Animation);
        }
    }

    /// <summary>Enumerate labels in insertion order.</summary>
    public IEnumerable<string> Labels()
    {
        foreach (var member in _members)
        {
            yield return member.Label;
        }
    }

    /// <inheritdoc />
    public void Tick(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Animation delta cannot be negative.");
        }

        foreach (var member in _members)
        {
            if (!member.Animation.IsComplete)
            {
                member.Animation.Tick(delta);
            }
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        foreach (var member in _members)
        {
            member.Animation.Reset();
        }
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"AnimationGroup {{ count: {Count}, progress: {OverallProgress}, complete: {AllComplete.ToString().ToLowerInvariant()} }}");

    private sealed class GroupMember(string label, IAnimation animation)
    {
        public string Label { get; } = label;

        public IAnimation Animation { get; set; } = animation;
    }
}
