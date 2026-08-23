// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/state_persistence.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust Result values map to typed .NET exceptions. File replacement
// uses File.Move(overwrite: true), the closest portable managed atomic-rename API.

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrankenTui.Runtime;

/// <summary>Base error for state-storage operations.</summary>
public abstract class StorageException : Exception
{
    protected StorageException(string message, Exception? inner = null) : base(message, inner) { }
}

public sealed class StorageIoException : StorageException
{
    public StorageIoException(Exception inner) : base($"I/O error: {inner.Message}", inner) { }
}

public sealed class StorageSerializationException : StorageException
{
    public StorageSerializationException(string message, Exception? inner = null)
        : base($"serialization error: {message}", inner) { }
}

public sealed class StorageCorruptionException : StorageException
{
    public StorageCorruptionException(string message) : base($"storage corruption: {message}") { }
}

public sealed class StorageUnavailableException : StorageException
{
    public StorageUnavailableException(string message) : base($"storage unavailable: {message}") { }
}

/// <summary>A serialized state entry with schema-version metadata.</summary>
public sealed record StoredEntry(string Key, uint Version, byte[] Data)
{
    public StoredEntry CloneEntry() => new(Key, Version, (byte[])Data.Clone());
}

/// <summary>Thread-safe pluggable storage for serialized widget state.</summary>
public interface IStorageBackend
{
    string Name { get; }
    IReadOnlyDictionary<string, StoredEntry> LoadAll();
    void SaveAll(IReadOnlyDictionary<string, StoredEntry> entries);
    void Clear();
    bool IsAvailable => true;
}

/// <summary>Ephemeral storage used by tests and applications without disk persistence.</summary>
public sealed class MemoryStorage : IStorageBackend
{
    private readonly object _gate = new();
    private Dictionary<string, StoredEntry> _data;

    public MemoryStorage() : this(new Dictionary<string, StoredEntry>(StringComparer.Ordinal)) { }

    public MemoryStorage(IReadOnlyDictionary<string, StoredEntry> entries)
    {
        _data = CloneEntries(entries);
    }

    public static MemoryStorage WithEntries(IReadOnlyDictionary<string, StoredEntry> entries) => new(entries);

    public string Name => nameof(MemoryStorage);
    public bool IsAvailable => true;

    public IReadOnlyDictionary<string, StoredEntry> LoadAll()
    {
        lock (_gate) return ReadOnly(CloneEntries(_data));
    }

    public void SaveAll(IReadOnlyDictionary<string, StoredEntry> entries)
    {
        lock (_gate) _data = CloneEntries(entries);
    }

    public void Clear()
    {
        lock (_gate) _data.Clear();
    }

    public override string ToString()
    {
        lock (_gate) return $"MemoryStorage {{ entries = {_data.Count} }}";
    }

    internal static Dictionary<string, StoredEntry> CloneEntries(
        IReadOnlyDictionary<string, StoredEntry> entries)
    {
        var clone = new Dictionary<string, StoredEntry>(entries.Count, StringComparer.Ordinal);
        foreach (var (key, entry) in entries) clone[key] = entry.CloneEntry();
        return clone;
    }

    internal static IReadOnlyDictionary<string, StoredEntry> ReadOnly(
        Dictionary<string, StoredEntry> entries) =>
        new ReadOnlyDictionary<string, StoredEntry>(entries);
}

/// <summary>JSON file storage with base64 payloads and write-then-rename replacement.</summary>
public sealed class FileStorage : IStorageBackend
{
    private const uint FormatVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public FileStorage(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path);
    }

    public string Path { get; }
    public string Name => nameof(FileStorage);

    public static FileStorage DefaultForApp(string appName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        var root = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        if (string.IsNullOrWhiteSpace(root))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            root = string.IsNullOrWhiteSpace(home)
                ? Environment.CurrentDirectory
                : System.IO.Path.Combine(home, ".local", "state");
        }

        return new FileStorage(System.IO.Path.Combine(root, "ftui", appName, "state.json"));
    }

    public IReadOnlyDictionary<string, StoredEntry> LoadAll()
    {
        if (!File.Exists(Path))
            return MemoryStorage.ReadOnly(new Dictionary<string, StoredEntry>(StringComparer.Ordinal));

        try
        {
            using var stream = File.OpenRead(Path);
            var file = JsonSerializer.Deserialize<StateFile>(stream, JsonOptions)
                ?? throw new StorageSerializationException("state file contained JSON null");
            if (file.FormatVersion != FormatVersion)
                return MemoryStorage.ReadOnly(new Dictionary<string, StoredEntry>(StringComparer.Ordinal));

            var result = new Dictionary<string, StoredEntry>(StringComparer.Ordinal);
            foreach (var (key, entry) in file.Entries)
            {
                try
                {
                    result[key] = new StoredEntry(key, entry.Version, Convert.FromBase64String(entry.DataBase64));
                }
                catch (FormatException)
                {
                    // Source contract: one corrupt payload does not discard valid siblings.
                }
            }

            return MemoryStorage.ReadOnly(result);
        }
        catch (StorageException) { throw; }
        catch (JsonException ex)
        {
            throw new StorageSerializationException($"failed to parse state file: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new StorageIoException(ex);
        }
    }

    public void SaveAll(IReadOnlyDictionary<string, StoredEntry> entries)
    {
        var parent = System.IO.Path.GetDirectoryName(Path);
        var tempPath = System.IO.Path.ChangeExtension(Path, "json.tmp");
        try
        {
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            var fileEntries = new Dictionary<string, FileEntry>(StringComparer.Ordinal);
            foreach (var (key, entry) in entries)
                fileEntries[key] = new FileEntry(entry.Version, Convert.ToBase64String(entry.Data));

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, new StateFile(FormatVersion, fileEntries), JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, Path, overwrite: true);
        }
        catch (JsonException ex)
        {
            throw new StorageSerializationException($"failed to serialize state: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new StorageIoException(ex);
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new StorageIoException(ex);
        }
    }

    public bool IsAvailable
    {
        get
        {
            var parent = System.IO.Path.GetDirectoryName(Path);
            if (string.IsNullOrEmpty(parent)) return false;
            var probe = System.IO.Path.Combine(parent, ".ftui_test_write");
            try
            {
                Directory.CreateDirectory(parent);
                File.WriteAllBytes(probe, "test"u8.ToArray());
                File.Delete(probe);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public override string ToString() => $"FileStorage {{ path = {Path} }}";

    private sealed record StateFile(
        [property: JsonPropertyName("format_version")] uint FormatVersion,
        [property: JsonPropertyName("entries")] Dictionary<string, FileEntry> Entries);

    private sealed record FileEntry(
        [property: JsonPropertyName("version")] uint Version,
        [property: JsonPropertyName("data_base64")] string DataBase64);
}

/// <summary>Central cached registry delegating persistence to an <see cref="IStorageBackend"/>.</summary>
public sealed class StateRegistry
{
    private readonly IStorageBackend _backend;
    private readonly object _gate = new();
    private Dictionary<string, StoredEntry> _cache = new(StringComparer.Ordinal);
    private bool _dirty;
    private long _generation;

    public StateRegistry(IStorageBackend backend) =>
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));

    public static StateRegistry InMemory() => new(new MemoryStorage());
    public static StateRegistry WithFile(string path) => new(new FileStorage(path));

    public int Load()
    {
        var entries = _backend.LoadAll();
        lock (_gate)
        {
            _cache = MemoryStorage.CloneEntries(entries);
            _dirty = false;
            _generation++;
            return _cache.Count;
        }
    }

    public bool Flush()
    {
        Dictionary<string, StoredEntry> snapshot;
        long generation;
        lock (_gate)
        {
            if (!_dirty) return false;
            snapshot = MemoryStorage.CloneEntries(_cache);
            generation = _generation;
        }

        // Deliberately outside _gate: backends may re-enter this registry.
        _backend.SaveAll(MemoryStorage.ReadOnly(snapshot));

        lock (_gate)
        {
            _dirty = _generation != generation;
            return true;
        }
    }

    public StoredEntry? Get(string key)
    {
        lock (_gate) return _cache.TryGetValue(key, out var entry) ? entry.CloneEntry() : null;
    }

    public void Set(string key, uint version, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(data);
        lock (_gate)
        {
            _cache[key] = new StoredEntry(key, version, (byte[])data.Clone());
            _generation++;
            _dirty = true;
        }
    }

    public StoredEntry? Remove(string key)
    {
        lock (_gate)
        {
            if (!_cache.Remove(key, out var entry)) return null;
            _generation++;
            _dirty = true;
            return entry.CloneEntry();
        }
    }

    public void Clear()
    {
        _backend.Clear();
        lock (_gate)
        {
            _cache.Clear();
            _generation++;
            _dirty = false;
        }
    }

    public int Count { get { lock (_gate) return _cache.Count; } }
    public bool IsEmpty => Count == 0;
    public bool IsDirty { get { lock (_gate) return _dirty; } }
    public string BackendName => _backend.Name;
    public bool IsAvailable => _backend.IsAvailable;

    public IReadOnlyList<string> Keys
    {
        get { lock (_gate) return _cache.Keys.ToArray(); }
    }

    public RegistryStats Stats
    {
        get
        {
            lock (_gate)
                return new RegistryStats(_cache.Count, _cache.Values.Sum(entry => entry.Data.Length), _dirty, _backend.Name);
        }
    }

    public override string ToString() =>
        $"StateRegistry {{ backend = {BackendName}, entries = {Count}, dirty = {IsDirty} }}";
}

public sealed record RegistryStats(int EntryCount = 0, int TotalBytes = 0, bool Dirty = false, string Backend = "");
