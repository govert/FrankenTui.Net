// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/policy_registry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Thread-safe registry of named PolicyConfig instances with lock-free
// reads and atomic hot-swap.

using System.Collections.Concurrent;

namespace FrankenTui.Runtime;

/// <summary>Default policy name, matching PolicyConfig.Default.</summary>
public static class PolicyRegistryConstants
{
    public const string StandardPolicy = "standard";
}

/// <summary>Record emitted when the active policy changes.</summary>
public sealed record PolicySwitchEvent(string OldName, string NewName, ulong SwitchId)
{
    /// <summary>Serialize as a single-line JSONL string.</summary>
    public string ToJsonl()
    {
        var oldEscaped = OldName.Replace("\"", "\\\"");
        var newEscaped = NewName.Replace("\"", "\\\"");
        return $$"""{"schema":"policy-switch-v1","switch_id":{{SwitchId}},"old":"{{oldEscaped}}","new":"{{newEscaped}}"}""";
    }
}

/// <summary>Errors from PolicyRegistry operations.</summary>
public abstract record PolicyRegistryError
{
    public sealed record NotFound(string Name) : PolicyRegistryError;
    public sealed record StandardPolicyProtected : PolicyRegistryError;
    public sealed record ValidationFailed(List<string> Errors) : PolicyRegistryError;

    public override string ToString() => this switch
    {
        NotFound n => $"policy not found: {n.Name}",
        StandardPolicyProtected => "cannot remove standard policy",
        ValidationFailed vf => $"policy validation failed: {string.Join("; ", vf.Errors)}",
        _ => "",
    };
}

// ── Internal snapshot ─────────────────────────────────────────────────────

internal sealed class ActivePolicy(string name, PolicyConfig config)
{
    public string Name { get; } = name;
    public PolicyConfig Config { get; } = config;
}

// ── PolicyRegistry ────────────────────────────────────────────────────────

/// <summary>
/// Thread-safe registry of named PolicyConfig instances.
/// Reads are lock-free (volatile reference). Writes take a lock.
/// </summary>
public sealed class PolicyRegistry
{
    private readonly ConcurrentDictionary<string, PolicyConfig> _policies = new();
    private volatile ActivePolicy _active;
    private long _switchCount;

    public PolicyRegistry()
    {
        var standard = PolicyConfig.Default;
        _policies[PolicyRegistryConstants.StandardPolicy] = standard;
        _active = new ActivePolicy(PolicyRegistryConstants.StandardPolicy, standard);
    }

    /// <summary>Get the currently active policy config (lock-free).</summary>
    public PolicyConfig ActiveConfig => _active.Config;

    /// <summary>Get the name of the currently active policy (lock-free).</summary>
    public string ActiveName => _active.Name;

    /// <summary>Register a named policy. Validates before accepting.</summary>
    public void Register(string name, PolicyConfig config)
    {
        if (name == PolicyRegistryConstants.StandardPolicy)
            throw new InvalidOperationException("Cannot overwrite standard policy");

        var errors = config.Validate();
        if (errors.Count > 0)
            throw new ArgumentException($"Validation failed: {string.Join("; ", errors)}");

        _policies[name] = config;
    }

    /// <summary>Remove a named policy. Cannot remove standard or active policy.</summary>
    public void Remove(string name)
    {
        if (name == PolicyRegistryConstants.StandardPolicy)
            throw new InvalidOperationException("Cannot remove standard policy");
        if (ActiveName == name)
            throw new InvalidOperationException($"Cannot remove active policy: {name}");

        if (!_policies.TryRemove(name, out _))
            throw new KeyNotFoundException($"Policy not found: {name}");
    }

    /// <summary>Switch the active policy to the named policy.</summary>
    public PolicySwitchEvent SetActive(string name)
    {
        if (!_policies.TryGetValue(name, out var config))
            throw new KeyNotFoundException($"Policy not found: {name}");

        var oldName = ActiveName;
        var switchId = (ulong)Interlocked.Increment(ref _switchCount) - 1;

        _active = new ActivePolicy(name, config);

        return new PolicySwitchEvent(oldName, name, switchId);
    }

    /// <summary>List all registered policy names.</summary>
    public List<string> List()
    {
        var names = _policies.Keys.ToList();
        names.Sort();
        return names;
    }

    /// <summary>Get a specific named policy config, if it exists.</summary>
    public PolicyConfig? Get(string name)
    {
        _policies.TryGetValue(name, out var config);
        return config;
    }

    /// <summary>Total number of policy switches performed.</summary>
    public ulong SwitchCount => (ulong)Interlocked.Read(ref _switchCount);

    public override string ToString() =>
        $"PolicyRegistry {{ active = {ActiveName}, policies = [{string.Join(", ", List())}], switch_count = {SwitchCount} }}";
}
