// Upstream source: crates/ftui-widgets/src/stateful.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of StateKey, Stateful trait, VersionedState, MigrationChain,
// MigrationError, ErasedMigration, RestoreResult, and StateMigration.
//
// Note: state.rs is just a re-export of stateful.rs and is not ported separately.

using System;
using System.Text;

namespace FrankenTui.Widgets;

// =============================================================================
// StateKey
// =============================================================================

/// <summary>
/// Unique identifier for a widget's persisted state.
/// A <see cref="StateKey"/> is the <c>(widget_type, instance_id)</c> pair
/// that maps a widget instance to its stored state blob.
/// </summary>
public sealed class StateKey : IEquatable<StateKey>
{
    /// <summary>The widget type name (e.g., "ScrollView", "TreeView").</summary>
    public string WidgetType { get; }
    /// <summary>Instance-unique identifier within a widget tree.</summary>
    public string InstanceId { get; }

    public StateKey(string widgetType, string instanceId)
    {
        WidgetType = widgetType ?? throw new ArgumentNullException(nameof(widgetType));
        InstanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
    }

    /// <summary>Build a state key from a path of widget-tree segments.</summary>
    public static StateKey FromPath(params string[] path)
    {
        if (path.Length == 0)
            throw new ArgumentException("StateKey.FromPath requires a non-empty path", nameof(path));
        return new StateKey(path[^1], string.Join("/", path));
    }

    /// <summary>Canonical string representation: "widget_type::instance_id".</summary>
    public string Canonical() => $"{WidgetType}::{InstanceId}";

    public override string ToString() => Canonical();

    public bool Equals(StateKey? other)
    {
        if (other is null) return false;
        return WidgetType == other.WidgetType && InstanceId == other.InstanceId;
    }

    public override bool Equals(object? obj) => obj is StateKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(WidgetType, InstanceId);
}

// =============================================================================
// IStateful trait
// =============================================================================

/// <summary>
/// Opt-in interface for widgets with persistable state.
/// Implementing this signals that a widget's user-facing state can be
/// serialized, stored, and later restored.
/// </summary>
public interface IStateful<TState> where TState : new()
{
    /// <summary>Unique key identifying this widget instance.</summary>
    StateKey StateKey { get; }

    /// <summary>Extract current state for persistence.</summary>
    TState SaveState();

    /// <summary>Restore state from persistence.</summary>
    void RestoreState(TState state);

    /// <summary>State schema version for forward-compatible migrations.</summary>
    static virtual uint StateVersion() => 1;
}

// =============================================================================
// VersionedState
// =============================================================================

/// <summary>
/// Version-tagged wrapper for serialized widget state.
/// </summary>
public sealed class VersionedState<T> where T : new()
{
    /// <summary>Schema version.</summary>
    public uint Version { get; set; }
    /// <summary>The actual state payload.</summary>
    public T Data { get; set; }

    public VersionedState(uint version, T data)
    {
        Version = version;
        Data = data;
    }

    /// <summary>Pack a widget's state into a versioned envelope.</summary>
    public static VersionedState<T> Pack<W>(W widget) where W : IStateful<T>
        => new(W.StateVersion(), widget.SaveState());

    /// <summary>
    /// Attempt to unpack, returning null if the version does not match.
    /// </summary>
    public T? Unpack<W>() where W : IStateful<T>
        => Version == W.StateVersion() ? Data : default;

    /// <summary>
    /// Unpack with fallback: returns the stored data if versions match,
    /// otherwise returns a new default instance.
    /// </summary>
    public T UnpackOrDefault<W>() where W : IStateful<T>
        => Version == W.StateVersion() ? Data : new T();
}

// =============================================================================
// MigrationError
// =============================================================================

/// <summary>Exception wrapper for migration errors (required since .NET can only throw Exception-derived types).</summary>
public sealed class MigrationException : Exception
{
    public MigrationError Error { get; }
    public MigrationException(MigrationError error) : base(error.ToString()) { Error = error; }
}

/// <summary>Error that can occur during state migration.</summary>
public abstract record MigrationError
{
    private MigrationError() { }

    /// <summary>No migration path exists from source to target version.</summary>
    public sealed record NoPathFound(uint From, uint To) : MigrationError;
    /// <summary>A migration function returned an error.</summary>
    public sealed record MigrationFailed(uint From, uint To, string Message) : MigrationError;
    /// <summary>Version numbers are invalid (e.g., target less than source).</summary>
    public sealed record InvalidVersionRange(uint From, uint To) : MigrationError;

    public override string ToString() => this switch
    {
        NoPathFound n => $"no migration path from version {n.From} to {n.To}",
        MigrationFailed f => $"migration from {f.From} to {f.To} failed: {f.Message}",
        InvalidVersionRange i => $"invalid version range: {i.From} to {i.To}",
        _ => "unknown migration error",
    };
}

// =============================================================================
// IErasedMigration
// =============================================================================

/// <summary>A type-erased migration step for use in migration chains.</summary>
public interface IErasedMigration<T>
{
    /// <summary>Source version.</summary>
    uint FromVersion { get; }
    /// <summary>Target version (must equal from_version + 1).</summary>
    uint ToVersion { get; }
    /// <summary>Perform migration on boxed state, returning boxed result.</summary>
    object MigrateErased(object oldState);
}

// =============================================================================
// MigrationChain
// =============================================================================

/// <summary>A chain of migrations that can upgrade state through multiple versions.</summary>
public sealed class MigrationChain<T>
{
    private readonly Dictionary<uint, IErasedMigration<T>> _migrations = [];

    /// <summary>Register a migration step.</summary>
    public void Register(IErasedMigration<T> migration)
    {
        if (migration.ToVersion != migration.FromVersion + 1)
            throw new ArgumentException(
                $"migration must increment version by exactly 1 (got {migration.FromVersion} -> {migration.ToVersion})");
        if (_migrations.ContainsKey(migration.FromVersion))
            throw new ArgumentException($"migration for version {migration.FromVersion} already registered");
        _migrations[migration.FromVersion] = migration;
    }

    /// <summary>Check if a migration path exists.</summary>
    public bool HasPath(uint fromVersion, uint toVersion)
    {
        if (fromVersion >= toVersion) return fromVersion == toVersion;
        uint current = fromVersion;
        while (current < toVersion)
        {
            if (!_migrations.ContainsKey(current)) return false;
            current++;
        }
        return true;
    }

    /// <summary>Attempt to migrate state.</summary>
    public object Migrate(object state, uint fromVersion, uint toVersion)
    {
        if (fromVersion > toVersion)
            throw new MigrationException(new MigrationError.InvalidVersionRange(fromVersion, toVersion));

        if (fromVersion == toVersion) return state;

        uint currentVersion = fromVersion;
        while (currentVersion < toVersion)
        {
            if (!_migrations.TryGetValue(currentVersion, out var migration))
                throw new MigrationException(new MigrationError.NoPathFound(currentVersion, toVersion));

            state = migration.MigrateErased(state);
            currentVersion++;
        }
        return state;
    }
}

// =============================================================================
// RestoreResult
// =============================================================================

/// <summary>Result of attempting state restoration with migration.</summary>
public abstract record RestoreResult<T>
{
    private RestoreResult() { }

    /// <summary>State was restored directly (versions matched).</summary>
    public sealed record Direct(T State) : RestoreResult<T>;
    /// <summary>State was successfully migrated from an older version.</summary>
    public sealed record Migrated(T State, uint FromVersion) : RestoreResult<T>;
    /// <summary>Migration failed; falling back to default state.</summary>
    public sealed record DefaultFallback(MigrationError Error, T Default) : RestoreResult<T>;

    /// <summary>Extract the state value, regardless of how it was obtained.</summary>
    public T IntoState() => this switch
    {
        Direct(var s) => s,
        Migrated(var s, _) => s,
        DefaultFallback(_, var d) => d,
        _ => throw new InvalidOperationException("unknown RestoreResult variant"),
    };

    /// <summary>Returns true if the state was migrated.</summary>
    public bool WasMigrated => this is Migrated;

    /// <summary>Returns true if we fell back to default.</summary>
    public bool IsFallback => this is DefaultFallback;
}

// Extension method for VersionedState unpack with migration
public static class VersionedStateExtensions
{
    /// <summary>
    /// Attempt to unpack with migration support.
    /// If the stored version doesn't match the current version, attempts to
    /// migrate through the provided chain. Falls back to default on failure.
    /// </summary>
    public static RestoreResult<T> UnpackWithMigration<T, W>(this VersionedState<T> vs, MigrationChain<T> chain)
        where W : IStateful<T> where T : new()
    {
        uint currentVersion = W.StateVersion();

        if (vs.Version == currentVersion)
            return new RestoreResult<T>.Direct(vs.Data);

        // Try migration
        try
        {
            var migrated = chain.Migrate(vs.Data!, vs.Version, currentVersion);
            if (migrated is T state)
                return new RestoreResult<T>.Migrated(state, vs.Version);
            return new RestoreResult<T>.DefaultFallback(
                new MigrationError.MigrationFailed(vs.Version, currentVersion, "type mismatch after migration"),
                new T());
        }
        catch (MigrationException mex)
        {
            return new RestoreResult<T>.DefaultFallback(mex.Error, new T());
        }
    }
}
