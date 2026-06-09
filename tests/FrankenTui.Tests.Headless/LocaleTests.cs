// Tests for .external/frankentui/crates/ftui-runtime/src/locale.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class LocaleTests
{
    [Fact]
    public void DetectSystemLocalePrefersLcAll()
    {
        var locale = LocaleNormalization.DetectSystemLocaleFrom("fr_FR.UTF-8", "en_US.UTF-8");
        Assert.Equal("fr-FR", locale);
    }

    [Fact]
    public void DetectSystemLocaleUsesLangWhenLcAllMissing()
    {
        var locale = LocaleNormalization.DetectSystemLocaleFrom(null, "en_US.UTF-8");
        Assert.Equal("en-US", locale);
    }

    [Fact]
    public void DetectSystemLocaleDefaultsToEn()
    {
        var locale = LocaleNormalization.DetectSystemLocaleFrom(null, null);
        Assert.Equal("en", locale);
    }

    [Fact]
    public void LocaleContextSwitchingUpdatesVersion()
    {
        var ctx = new LocaleContext("en");
        var v0 = ctx.Version;
        ctx.SetLocale("en"); // Same value
        Assert.Equal(v0, ctx.Version);
        ctx.SetLocale("es");
        Assert.True(ctx.Version > v0);
        Assert.Equal("es", ctx.CurrentLocale);
    }

    [Fact]
    public void LocaleOverrideIsScoped()
    {
        var ctx = new LocaleContext("en");
        Assert.Equal("en", ctx.CurrentLocale);
        using (ctx.PushOverride("fr"))
        {
            Assert.Equal("fr", ctx.CurrentLocale);
        }
        Assert.Equal("en", ctx.CurrentLocale);
    }

    [Fact]
    public void LocaleOverrideIsLifo()
    {
        var ctx = new LocaleContext("en");
        using (ctx.PushOverride("fr"))
        {
            Assert.Equal("fr", ctx.CurrentLocale);
            using (ctx.PushOverride("es"))
            {
                Assert.Equal("es", ctx.CurrentLocale);
            }
            Assert.Equal("fr", ctx.CurrentLocale);
        }
    }

    [Fact]
    public void NormalizeLocaleHandlesCAndPosix()
    {
        var c = LocaleNormalization.NormalizeLocaleRaw("C");
        var posix = LocaleNormalization.NormalizeLocaleRaw("POSIX");
        Assert.Equal("en", c);
        Assert.Equal("en", posix);
    }

    [Fact]
    public void NormalizeLocaleStripsCodesetAndModifier()
    {
        var locale = LocaleNormalization.NormalizeLocaleRaw("en_US.UTF-8@latin");
        Assert.Equal("en-US", locale);
    }

    [Fact]
    public void LocaleOverrideDoesNotMutateBaseLocale()
    {
        var ctx = new LocaleContext("en");
        var v0 = ctx.Version;
        using (ctx.PushOverride("fr"))
        {
            Assert.Equal("en", ctx.BaseLocale);
            Assert.Equal(v0, ctx.Version);
        }
    }

    [Fact]
    public void NormalizeEmptyFallsBackToEn()
    {
        var locale = LocaleNormalization.NormalizeLocale("");
        Assert.Equal("en", locale);
    }

    [Fact]
    public void NormalizeWhitespaceOnlyFallsBackToEn()
    {
        var locale = LocaleNormalization.NormalizeLocale("   ");
        Assert.Equal("en", locale);
    }

    [Fact]
    public void SubscribeFiresOnChange()
    {
        var ctx = new LocaleContext("en");
        var fired = false;
        using (ctx.Subscribe(_ => fired = true))
        {
            ctx.SetLocale("de");
        }
        Assert.True(fired);
    }

    [Fact]
    public void DetectSystemLocaleEmptyLcAllUsesLang()
    {
        var locale = LocaleNormalization.DetectSystemLocaleFrom("", "ja_JP.UTF-8");
        Assert.Equal("ja-JP", locale);
    }
}
