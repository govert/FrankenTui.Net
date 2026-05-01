using System.Globalization;
using System.Text.Json;

namespace FrankenTui.Demo.Showcase;

public enum ShowcaseVfxGoldenOutcome
{
    Pass,
    Missing,
    Mismatch
}

public sealed record ShowcaseVfxGoldenResult(
    ShowcaseVfxGoldenOutcome Outcome,
    int? MismatchIndex = null,
    ulong? Expected = null,
    ulong? Actual = null);

public static class ShowcaseVfxGoldenRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string ScenarioName(ShowcaseVfxHarnessOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var effect = ShowcaseVfxEffects.NormalizeOrDefault(options.Effect);
        var seed = options.Seed?.ToString(CultureInfo.InvariantCulture) ?? "none";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"vfx_{effect}_{options.Columns}x{options.Rows}_{options.TickMilliseconds}ms_seed{seed}");
    }

    public static IReadOnlyList<ulong> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return [];
        }

        return JsonSerializer.Deserialize<ulong[]>(File.ReadAllText(path)) ?? [];
    }

    public static void Save(string path, IReadOnlyList<ulong> hashes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(hashes);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(hashes, JsonOptions));
    }

    public static ShowcaseVfxGoldenResult VerifyOrUpdate(string path, IReadOnlyList<ulong> actual, bool update)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(actual);

        if (update)
        {
            Save(path, actual);
            return new ShowcaseVfxGoldenResult(ShowcaseVfxGoldenOutcome.Pass);
        }

        return Verify(actual, Load(path));
    }

    public static ShowcaseVfxGoldenResult Verify(IReadOnlyList<ulong> actual, IReadOnlyList<ulong> expected)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);
        if (expected.Count == 0)
        {
            return new ShowcaseVfxGoldenResult(ShowcaseVfxGoldenOutcome.Missing);
        }

        var count = Math.Max(actual.Count, expected.Count);
        for (var index = 0; index < count; index++)
        {
            var actualValue = index < actual.Count ? actual[index] : (ulong?)null;
            var expectedValue = index < expected.Count ? expected[index] : (ulong?)null;
            if (actualValue != expectedValue)
            {
                return new ShowcaseVfxGoldenResult(
                    ShowcaseVfxGoldenOutcome.Mismatch,
                    index,
                    expectedValue,
                    actualValue);
            }
        }

        return new ShowcaseVfxGoldenResult(ShowcaseVfxGoldenOutcome.Pass);
    }

    public static IReadOnlyList<ulong> ExtractFrameHashesFromJsonl(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return [];
        }

        var hashes = new List<ulong>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.TryGetProperty("event", out var eventElement) &&
                string.Equals(eventElement.GetString(), "vfx_frame", StringComparison.Ordinal) &&
                root.TryGetProperty("hash", out var hashElement) &&
                hashElement.TryGetUInt64(out var hash))
            {
                hashes.Add(hash);
            }
        }

        return hashes;
    }
}
