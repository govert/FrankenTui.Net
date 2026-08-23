// SPDX-License-Identifier: Apache-2.0
// Tests ported from crates/ftui-runtime/src/state_persistence.rs at 15cc6543.

using System.Collections.Concurrent;
using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class StatePersistenceTests
{
    [Fact]
    public void MemoryStorageBasicOperations()
    {
        var storage = new MemoryStorage();
        Assert.Empty(storage.LoadAll());
        storage.SaveAll(Entries(("key1", 1, "hello"u8.ToArray())));
        var loaded = storage.LoadAll();
        Assert.Equal("hello"u8.ToArray(), loaded["key1"].Data);
        storage.Clear();
        Assert.Empty(storage.LoadAll());
    }

    [Fact]
    public void MemoryStorageWithEntries()
    {
        var storage = MemoryStorage.WithEntries(Entries(("test", 2, new byte[] { 1, 2, 3 })));
        Assert.Equal((uint)2, storage.LoadAll()["test"].Version);
    }

    [Fact]
    public void MemoryStorageCopiesInputAndOutputPayloads()
    {
        var bytes = new byte[] { 1 };
        var storage = new MemoryStorage(Entries(("k", 1, bytes)));
        bytes[0] = 9;
        var first = storage.LoadAll()["k"];
        first.Data[0] = 7;
        Assert.Equal(1, storage.LoadAll()["k"].Data[0]);
    }

    [Fact]
    public void MemoryStorageSaveReplacesAll()
    {
        var storage = new MemoryStorage();
        storage.SaveAll(Entries(("old", 1, [])));
        storage.SaveAll(Entries(("new", 2, [])));
        var loaded = storage.LoadAll();
        Assert.Single(loaded);
        Assert.True(loaded.ContainsKey("new"));
    }

    [Fact]
    public void MemoryStorageMetadata()
    {
        var storage = new MemoryStorage(Entries(("a", 1, [])));
        Assert.Equal("MemoryStorage", storage.Name);
        Assert.True(storage.IsAvailable);
        Assert.Contains("entries = 1", storage.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RegistryBasicOperations()
    {
        var registry = StateRegistry.InMemory();
        Assert.True(registry.IsEmpty);
        Assert.False(registry.IsDirty);
        registry.Set("widget::1", 1, "data"u8.ToArray());
        Assert.Equal(1, registry.Count);
        Assert.True(registry.IsDirty);
        Assert.Equal("data"u8.ToArray(), registry.Get("widget::1")!.Data);
        Assert.Null(registry.Get("widget::99"));
        Assert.True(registry.Flush());
        Assert.False(registry.IsDirty);
        Assert.False(registry.Flush());
        Assert.Equal("data"u8.ToArray(), registry.Remove("widget::1")!.Data);
        Assert.True(registry.IsEmpty);
        Assert.True(registry.IsDirty);
    }

    [Fact]
    public void RegistryLoadReplacesCacheAndClearsDirty()
    {
        var storage = new MemoryStorage(Entries(("backend", 5, "old"u8.ToArray())));
        var registry = new StateRegistry(storage);
        registry.Set("local", 1, "new"u8.ToArray());
        Assert.Equal(1, registry.Load());
        Assert.Null(registry.Get("local"));
        Assert.Equal((uint)5, registry.Get("backend")!.Version);
        Assert.False(registry.IsDirty);
    }

    [Fact]
    public void RegistryFlushPersistsToBackend()
    {
        var storage = new MemoryStorage();
        var registry = new StateRegistry(storage);
        registry.Set("widget::foo", 3, "bar"u8.ToArray());
        Assert.True(registry.Flush());
        registry.Remove("widget::foo");
        Assert.Equal(1, registry.Load());
        Assert.Equal("bar"u8.ToArray(), registry.Get("widget::foo")!.Data);
    }

    [Fact]
    public void RegistryClearClearsCacheAndBackend()
    {
        var registry = StateRegistry.InMemory();
        registry.Set("a", 1, []);
        registry.Set("b", 1, []);
        registry.Flush();
        registry.Clear();
        Assert.True(registry.IsEmpty);
        Assert.False(registry.IsDirty);
        Assert.Equal(0, registry.Load());
    }

    [Fact]
    public void RegistryKeysAndStats()
    {
        var registry = StateRegistry.InMemory();
        registry.Set("widget::b", 1, new byte[] { 6, 7, 8 });
        registry.Set("widget::a", 1, new byte[] { 1, 2, 3, 4, 5 });
        Assert.Equal(new[] { "widget::a", "widget::b" }, registry.Keys.Order());
        Assert.Equal(new RegistryStats(2, 8, true, "MemoryStorage"), registry.Stats);
    }

    [Fact]
    public void RegistryMetadataAndDefaults()
    {
        var registry = StateRegistry.InMemory();
        Assert.Equal("MemoryStorage", registry.BackendName);
        Assert.True(registry.IsAvailable);
        Assert.Equal(new RegistryStats(), new RegistryStats());
        Assert.Equal(new RegistryStats(0, 0, false, "MemoryStorage"), registry.Stats);
        Assert.Contains("StateRegistry", registry.ToString(), StringComparison.Ordinal);
        Assert.Contains("dirty = False", registry.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrySetOverwritesAndRemoveMissingDoesNotDirty()
    {
        var registry = StateRegistry.InMemory();
        Assert.Null(registry.Remove("missing"));
        Assert.False(registry.IsDirty);
        registry.Set("k", 1, "first"u8.ToArray());
        registry.Set("k", 2, "second"u8.ToArray());
        Assert.Equal(1, registry.Count);
        Assert.Equal((uint)2, registry.Get("k")!.Version);
        Assert.Equal("second"u8.ToArray(), registry.Get("k")!.Data);
    }

    [Fact]
    public async Task RegistrySupportsConcurrentAccess()
    {
        var registry = StateRegistry.InMemory();
        await Task.WhenAll(Enumerable.Range(0, 64).Select(i => Task.Run(() =>
            registry.Set($"k{i}", 1, BitConverter.GetBytes(i)))));
        Assert.Equal(64, registry.Count);
        Assert.All(Enumerable.Range(0, 64), i => Assert.NotNull(registry.Get($"k{i}")));
    }

    [Fact]
    public async Task FlushDoesNotHoldCacheLockDuringBackendSave()
    {
        StateRegistry? registry = null;
        var backend = new ReentrantBackend(() => registry!);
        registry = new StateRegistry(backend);
        registry.Set("widget::foo", 1, "bar"u8.ToArray());
        var flush = Task.Run(registry.Flush);
        var completed = await Task.WhenAny(flush, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.NotNull(backend);
        Assert.Same(flush, completed);
        Assert.True(await flush);
        Assert.True(registry.IsDirty);
        Assert.Equal("late"u8.ToArray(), registry.Get("backend::late")!.Data);
        Assert.True(registry.Flush());
        Assert.False(registry.IsDirty);
        Assert.True(backend.Saved.ContainsKey("backend::late"));
    }

    [Fact]
    public void StoredEntryIsDefensivelyClonedByRegistry()
    {
        var registry = StateRegistry.InMemory();
        var bytes = new byte[] { 1, 2, 3 };
        registry.Set("test", 7, bytes);
        bytes[0] = 8;
        var entry = registry.Get("test")!;
        entry.Data[1] = 9;
        Assert.Equal(new byte[] { 1, 2, 3 }, registry.Get("test")!.Data);
        Assert.Contains("StoredEntry", entry.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void FileStorageRoundTripsAndCreatesParentDirectories()
    {
        using var temp = new TempDirectory();
        var path = System.IO.Path.Combine(temp.Path, "nested", "dirs", "state.json");
        var storage = new FileStorage(path);
        storage.SaveAll(Entries(("widget::test", 3, "hello world"u8.ToArray())));
        Assert.True(File.Exists(path));
        var loaded = storage.LoadAll();
        Assert.Equal((uint)3, loaded["widget::test"].Version);
        Assert.Equal("hello world"u8.ToArray(), loaded["widget::test"].Data);
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(1, json.RootElement.GetProperty("format_version").GetInt32());
    }

    [Fact]
    public void FileStorageNonexistentAndUnknownFormatAreEmpty()
    {
        using var temp = new TempDirectory();
        var path = System.IO.Path.Combine(temp.Path, "state.json");
        var storage = new FileStorage(path);
        Assert.Empty(storage.LoadAll());
        File.WriteAllText(path, "{\"format_version\":2,\"entries\":{}}");
        Assert.Empty(storage.LoadAll());
    }

    [Fact]
    public void FileStorageSkipsOneCorruptBase64Entry()
    {
        using var temp = new TempDirectory();
        var path = System.IO.Path.Combine(temp.Path, "state.json");
        File.WriteAllText(path,
            "{\"format_version\":1,\"entries\":{" +
            "\"bad\":{\"version\":1,\"data_base64\":\"!!invalid!!\"}," +
            "\"good\":{\"version\":1,\"data_base64\":\"aGVsbG8=\"}}}");
        var loaded = new FileStorage(path).LoadAll();
        Assert.Single(loaded);
        Assert.Equal("hello"u8.ToArray(), loaded["good"].Data);
    }

    [Fact]
    public void FileStorageInvalidJsonIsTypedSerializationError()
    {
        using var temp = new TempDirectory();
        var path = System.IO.Path.Combine(temp.Path, "state.json");
        File.WriteAllText(path, "{");
        var error = Assert.Throws<StorageSerializationException>(() => new FileStorage(path).LoadAll());
        Assert.Contains("serialization error", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<JsonException>(error.InnerException);
    }

    [Fact]
    public void FileStorageClearAndAvailability()
    {
        using var temp = new TempDirectory();
        var path = System.IO.Path.Combine(temp.Path, "state.json");
        var storage = new FileStorage(path);
        Assert.True(storage.IsAvailable);
        storage.SaveAll(Entries(("k", 1, [])));
        storage.Clear();
        Assert.False(File.Exists(path));
        Assert.Contains("FileStorage", storage.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultForAppHonorsXdgStateHome()
    {
        using var temp = new TempDirectory();
        var prior = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        try
        {
            Environment.SetEnvironmentVariable("XDG_STATE_HOME", temp.Path);
            var storage = FileStorage.DefaultForApp("sample");
            Assert.Equal(System.IO.Path.Combine(temp.Path, "ftui", "sample", "state.json"), storage.Path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_STATE_HOME", prior);
        }
    }

    [Fact]
    public void ProgramManualPersistenceUsesConfiguredRegistry()
    {
        var registry = StateRegistry.InMemory();
        var config = ProgramConfig.Default;
        config.Persistence = PersistenceConfig.Default.WithRegistry(registry);
        var writer = new TerminalWriter(
            new StringWriter(), new ScreenMode.Inline(4), UiAnchor.Bottom, TerminalCapabilities.Basic());
        var program = new Program<string>(new NoopModel(), writer, config);
        Assert.True(program.HasPersistence);
        Assert.Same(registry, program.StateRegistry);
        registry.Set("widget::x", 1, "x"u8.ToArray());
        Assert.True(program.TriggerSave());
        registry.Remove("widget::x");
        Assert.Equal(1, program.TriggerLoad());
        Assert.NotNull(registry.Get("widget::x"));
    }

    private static IReadOnlyDictionary<string, StoredEntry> Entries(
        params (string Key, uint Version, byte[] Data)[] values) =>
        values.ToDictionary(v => v.Key, v => new StoredEntry(v.Key, v.Version, v.Data), StringComparer.Ordinal);

    private sealed class ReentrantBackend(Func<StateRegistry> registry) : IStorageBackend
    {
        private int _injected;
        public ConcurrentDictionary<string, StoredEntry> Saved { get; } = new(StringComparer.Ordinal);
        public string Name => nameof(ReentrantBackend);
        public IReadOnlyDictionary<string, StoredEntry> LoadAll() => new Dictionary<string, StoredEntry>(Saved);
        public void SaveAll(IReadOnlyDictionary<string, StoredEntry> entries)
        {
            Saved.Clear();
            foreach (var pair in entries) Saved[pair.Key] = pair.Value.CloneEntry();
            if (Interlocked.Exchange(ref _injected, 1) == 0)
                registry().Set("backend::late", 2, "late"u8.ToArray());
        }
        public void Clear() => Saved.Clear();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ftui-state-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }
        public string Path { get; }
        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class NoopModel : IModel<string>
    {
        public Cmd<string> Update(string message) => Cmd<string>.NoneCmd;
        public void View(FrankenTui.Render.Frame frame) { }
    }
}
