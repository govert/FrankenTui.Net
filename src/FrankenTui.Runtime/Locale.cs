// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/locale.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Locale context provider for runtime-wide internationalization.
//
// LocaleContext owns the current locale and exposes scoped overrides
// for widget subtrees. Locale changes are versioned so the runtime can
// trigger re-renders when the active locale changes.
//
// DIVERGENCE: ftui_i18n::catalog::Locale is string in C# (no dedicated type).

namespace FrankenTui.Runtime;

/// <summary>
/// Runtime locale context with scoped overrides.
/// </summary>
public sealed class LocaleContext
{
    private static readonly ThreadLocal<LocaleContext> _global = new(() => LocaleContext.System());

    private readonly Observable<string> _current;
    private readonly List<string> _overrides = new();

    /// <summary>Create a new locale context with the provided locale.</summary>
    public LocaleContext(string locale)
    {
        _current = new Observable<string>(LocaleNormalization.NormalizeLocale(locale));
    }

    /// <summary>Create a locale context initialized from system locale detection.</summary>
    public static LocaleContext System() => new(LocaleDetection.DetectSystemLocale());

    /// <summary>Access the global locale context (thread-local).</summary>
    public static LocaleContext Global => _global.Value!;

    /// <summary>Get the active locale, honoring any scoped override.</summary>
    public string CurrentLocale
    {
        get
        {
            if (_overrides.Count > 0)
                return _overrides[^1];
            return _current.Get();
        }
    }

    /// <summary>Get the base locale without considering overrides.</summary>
    public string BaseLocale => _current.Get();

    /// <summary>Set the base locale.</summary>
    public void SetLocale(string locale)
    {
        _current.Set(LocaleNormalization.NormalizeLocale(locale));
    }

    /// <summary>Subscribe to base locale changes.</summary>
    public Subscription Subscribe(Action<string> callback) => _current.Subscribe(callback);

    /// <summary>
    /// Push a scoped locale override. Dropping the guard restores the prior locale.
    /// </summary>
    public LocaleOverride PushOverride(string locale)
    {
        var normalized = LocaleNormalization.NormalizeLocale(locale);
        _overrides.Add(normalized);
        return new LocaleOverride(_overrides, normalized);
    }

    /// <summary>Current version counter for the base locale.</summary>
    public ulong Version => _current.Version;
}

/// <summary>RAII guard for scoped locale overrides.</summary>
public sealed class LocaleOverride : IDisposable
{
    private readonly List<string> _stack;
    private readonly string _locale;

    internal LocaleOverride(List<string> stack, string locale)
    {
        _stack = stack;
        _locale = locale;
    }

    /// <summary>Clear the override, restoring the prior locale.</summary>
    public void Dispose()
    {
        var popped = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        System.Diagnostics.Debug.Assert(popped == _locale, "Locale override LIFO violation");
    }
}

// ── Static helpers ───────────────────────────────────────────────────────

/// <summary>
/// Detect the system locale from environment variables.
/// Preference order: LC_ALL, then LANG. Falls back to "en".
/// </summary>
public static class LocaleDetection
{
    public static string DetectSystemLocale()
    {
        var lcAll = Environment.GetEnvironmentVariable("LC_ALL");
        var lang = Environment.GetEnvironmentVariable("LANG");
        return LocaleNormalization.DetectSystemLocaleFrom(lcAll, lang);
    }

    /// <summary>Convenience: set the global locale.</summary>
    public static void SetGlobal(string locale) => LocaleContext.Global.SetLocale(locale);

    /// <summary>Convenience: get the global locale.</summary>
    public static string CurrentGlobal => LocaleContext.Global.CurrentLocale;
}

// ── Internal normalization ───────────────────────────────────────────────

internal static class LocaleNormalization
{
    internal static string NormalizeLocale(string locale)
    {
        var raw = NormalizeLocaleRaw(locale);
        return raw ?? "en";
    }

    internal static string? NormalizeLocaleRaw(string raw)
    {
        raw = (raw ?? "").Trim();
        if (raw.Length == 0) return null;

        // Strip @modifiers and .codesets
        int at = raw.IndexOf('@');
        if (at >= 0) raw = raw[..at];

        int dot = raw.IndexOf('.');
        if (dot >= 0) raw = raw[..dot];

        raw = raw.Trim();
        if (raw.Length == 0) return null;

        // Normalize underscores to hyphens
        raw = raw.Replace('_', '-');

        // "C" or "POSIX" → "en"
        if (string.Equals(raw, "C", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "POSIX", StringComparison.OrdinalIgnoreCase))
            return "en";

        return raw;
    }

    internal static string DetectSystemLocaleFrom(string? lcAll, string? lang)
    {
        return NormalizeLocaleRaw(lcAll ?? "") ??
               NormalizeLocaleRaw(lang ?? "") ??
               "en";
    }
}
