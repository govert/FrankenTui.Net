// Differential contract for crates/ftui-render/src/link_registry.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class LinkRegistryTests
{
    [Fact]
    public void FreshRegistryReservesZeroAndIsEmpty()
    {
        var registry = new LinkRegistry();

        Assert.Equal(0, registry.Count);
        Assert.True(registry.IsEmpty);
        Assert.Null(registry.Get(0));
        Assert.False(registry.Contains(0));
    }

    [Fact]
    public void RegisterAndGetRoundTrip()
    {
        var registry = new LinkRegistry();

        var id = registry.Register("https://example.com");

        Assert.NotEqual(0U, id);
        Assert.Equal("https://example.com", registry.Get(id));
        Assert.True(registry.Contains(id));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void RegistrationDeduplicatesOrdinalUrls()
    {
        var registry = new LinkRegistry();

        Assert.Equal(registry.Register("https://example.com/A"), registry.Register("https://example.com/A"));
        Assert.NotEqual(registry.Register("https://example.com/a"), registry.Register("https://example.com/A"));
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void IdsAreSequentialWithoutFreeSlots()
    {
        var registry = new LinkRegistry();

        Assert.Equal(Enumerable.Range(1, 100).Select(static value => (uint)value),
            Enumerable.Range(1, 100).Select(value => registry.Register($"https://example.com/{value}")));
    }

    [Fact]
    public void UnregisterReusesIdsInLifoOrder()
    {
        var registry = new LinkRegistry();
        var first = registry.Register("a");
        registry.Register("b");
        var third = registry.Register("c");
        registry.Unregister(first);
        registry.Unregister(third);

        Assert.Equal(third, registry.Register("d"));
        Assert.Equal(first, registry.Register("e"));
    }

    [Fact]
    public void RemoveCompatibilityNameMatchesUnregister()
    {
        var registry = new LinkRegistry();
        var id = registry.Register("https://example.com");

        registry.Remove(id);

        Assert.Null(registry.Get(id));
        Assert.True(registry.IsEmpty);
    }

    [Theory]
    [InlineData(0U)]
    [InlineData(1_000_000U)]
    [InlineData(uint.MaxValue)]
    public void InvalidUnregisterIsSafe(uint id)
    {
        var registry = new LinkRegistry();

        registry.Unregister(id);
        registry.Unregister(id);

        Assert.True(registry.IsEmpty);
    }

    [Fact]
    public void DoubleUnregisterDoesNotDuplicateFreeSlot()
    {
        var registry = new LinkRegistry();
        var id = registry.Register("a");
        registry.Unregister(id);
        registry.Unregister(id);

        Assert.Equal(id, registry.Register("b"));
        Assert.NotEqual(id, registry.Register("c"));
    }

    [Fact]
    public void UnregisterDoesNotAffectOtherUrls()
    {
        var registry = new LinkRegistry();
        var first = registry.Register("a");
        var second = registry.Register("b");
        var third = registry.Register("c");

        registry.Unregister(second);

        Assert.Equal("a", registry.Get(first));
        Assert.Null(registry.Get(second));
        Assert.Equal("c", registry.Get(third));
    }

    [Fact]
    public void ReRegisterAfterUnregisterUsesAFreeId()
    {
        var registry = new LinkRegistry();
        var old = registry.Register("old");
        registry.Unregister(old);

        var replacement = registry.Register("old");

        Assert.Equal(old, replacement);
        Assert.Equal("old", registry.Get(replacement));
    }

    [Fact]
    public void CountTracksMixedOperations()
    {
        var registry = new LinkRegistry();
        var ids = Enumerable.Range(0, 10)
            .Select(value => registry.Register($"url-{value}"))
            .ToArray();
        registry.Unregister(ids[1]);
        registry.Unregister(ids[5]);
        registry.Register("url-0");

        Assert.Equal(8, registry.Count);
        Assert.False(registry.IsEmpty);
    }

    [Fact]
    public void ClearResetsSlotsLookupAndFreeList()
    {
        var registry = new LinkRegistry();
        var first = registry.Register("a");
        var second = registry.Register("b");
        registry.Unregister(first);

        registry.Clear();
        registry.Clear();

        Assert.True(registry.IsEmpty);
        Assert.Null(registry.Get(first));
        Assert.Null(registry.Get(second));
        Assert.Equal(1U, registry.Register("after-clear"));
    }

    [Fact]
    public void EmptyUrlIsValid()
    {
        var registry = new LinkRegistry();

        var id = registry.Register(string.Empty);

        Assert.Equal(1U, id);
        Assert.Equal(string.Empty, registry.Get(id));
    }

    [Theory]
    [InlineData("https://example.com/path?q=a%20b#fragment")]
    [InlineData("mailto:user+tag@example.com")]
    [InlineData("https://例え.テスト/路径")]
    [InlineData("file:///tmp/a-b_c.txt")]
    public void SafeSpecialAndUnicodeUrlsRoundTrip(string url)
    {
        var registry = new LinkRegistry();

        Assert.Equal(url, registry.Get(registry.Register(url)));
    }

    [Theory]
    [InlineData("https://example.com/\nattack")]
    [InlineData("https://example.com/\u001b]8;;attack")]
    [InlineData("https://example.com/\u0085attack")]
    public void ControlCharactersAreRejected(string url)
    {
        var registry = new LinkRegistry();

        Assert.Equal(0U, registry.Register(url));
        Assert.True(registry.IsEmpty);
    }

    [Fact]
    public void Utf8ByteLimitMatchesRustStrLength()
    {
        var registry = new LinkRegistry();
        var exactAscii = new string('a', 4096);
        var exactMultibyte = string.Concat(Enumerable.Repeat("é", 2048));
        var tooManyMultibyteBytes = string.Concat(Enumerable.Repeat("é", 2049));

        Assert.NotEqual(0U, registry.Register(exactAscii));
        Assert.NotEqual(0U, registry.Register(exactMultibyte));
        Assert.Equal(0U, registry.Register(new string('a', 4097)));
        Assert.Equal(0U, registry.Register(tooManyMultibyteBytes));
    }

    [Fact]
    public void MalformedUtf16IsTypedAsUnrepresentableRustInput()
    {
        var registry = new LinkRegistry();

        foreach (var url in new[] { "\uD800", "\uDC00", "a\uD800b" })
        {
            Assert.Equal(0U, registry.Register(url));
        }
    }

    [Fact]
    public void NullCannotRepresentRustStr()
    {
        var registry = new LinkRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void CloneIsIndependent()
    {
        var registry = new LinkRegistry();
        var first = registry.Register("a");
        registry.Register("b");
        var third = registry.Register("c");
        registry.Unregister(first);
        registry.Unregister(third);

        var clone = registry.Clone();
        Assert.Equal(third, clone.Register("clone-c"));
        Assert.Equal(first, clone.Register("clone-a"));

        Assert.Null(registry.Get(third));
        Assert.Equal(third, registry.Register("original-c"));
        Assert.NotEqual("clone-c", registry.Get(third));
    }

    [Fact]
    public void MemoryEstimateIsPositiveAndGrowsWithContent()
    {
        var registry = new LinkRegistry();
        var empty = registry.EstimateMemory();

        registry.Register(new string('x', 1000));
        var populated = registry.EstimateMemory();

        Assert.True(empty > 0);
        Assert.True(populated > empty);
    }

    [Fact]
    public void DeterministicPropertySequencesPreserveRegistryInvariants()
    {
        var random = new Random(0x5EED);
        for (var run = 0; run < 200; run++)
        {
            var registry = new LinkRegistry();
            var expected = new Dictionary<string, uint>(StringComparer.Ordinal);
            for (var operation = 0; operation < 100; operation++)
            {
                var url = $"https://example.com/{random.Next(0, 25)}";
                if (random.Next(0, 3) != 0)
                {
                    var id = registry.Register(url);
                    if (expected.TryGetValue(url, out var prior))
                    {
                        Assert.Equal(prior, id);
                    }
                    else
                    {
                        expected[url] = id;
                    }
                }
                else if (expected.Remove(url, out var id))
                {
                    registry.Unregister(id);
                }

                Assert.Equal(expected.Count, registry.Count);
                Assert.All(expected, pair => Assert.Equal(pair.Key, registry.Get(pair.Value)));
            }
        }
    }
}
