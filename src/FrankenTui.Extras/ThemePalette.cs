// Upstream source: .external/frankentui/crates/ftui-extras/src/theme.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of the core theme system: ColorToken enum, ThemePalette,
// accent_gradient(), and the Cyberpunk Aurora default palette.

using FrankenTui.Render;

namespace FrankenTui.Extras;

/// <summary>Semantic color tokens that resolve against the current theme palette.
/// Port of <c>pub enum ColorToken</c> in theme.rs.</summary>
public enum ColorToken
{
    // ── Backgrounds ──
    BgDeep, BgBase, BgSurface, BgOverlay, BgHighlight,
    // ── Foregrounds ──
    FgPrimary, FgSecondary, FgMuted, FgDisabled,
    // ── Accents ──
    AccentPrimary, AccentSecondary, AccentSuccess, AccentWarning, AccentError, AccentInfo, AccentLink,
    // ── Accent slots 0..11 (for gradients/sparklines/palette cycling) ──
    AccentSlot0, AccentSlot1, AccentSlot2, AccentSlot3, AccentSlot4, AccentSlot5,
    AccentSlot6, AccentSlot7, AccentSlot8, AccentSlot9, AccentSlot10, AccentSlot11,
}

/// <summary>Per-theme color palette.
/// Port of the <c>ThemePalette</c> and per-theme-ctor pattern in theme.rs.</summary>
public sealed class ThemePalette
{
    // Backgrounds
    public PackedRgba BgDeep      { get; init; }
    public PackedRgba BgBase      { get; init; }
    public PackedRgba BgSurface   { get; init; }
    public PackedRgba BgOverlay   { get; init; }
    public PackedRgba BgHighlight { get; init; }
    // Foregrounds
    public PackedRgba FgPrimary   { get; init; }
    public PackedRgba FgSecondary { get; init; }
    public PackedRgba FgMuted     { get; init; }
    public PackedRgba FgDisabled  { get; init; }
    // Accents
    public PackedRgba AccentPrimary   { get; init; }
    public PackedRgba AccentSecondary { get; init; }
    public PackedRgba AccentSuccess   { get; init; }
    public PackedRgba AccentWarning   { get; init; }
    public PackedRgba AccentError     { get; init; }
    public PackedRgba AccentInfo      { get; init; }
    public PackedRgba AccentLink      { get; init; }
    // Accent slots (0..11) for gradient / sparkline / palette cycling
    public PackedRgba[] AccentSlots { get; init; } = Array.Empty<PackedRgba>();

    /// <summary>Resolve a ColorToken against this palette.
    /// Port of <c>impl ColorToken::resolve_in(&self, &ThemePalette) -> PackedRgba</c>.</summary>
    public PackedRgba Resolve(ColorToken token) => token switch
    {
        ColorToken.BgDeep      => BgDeep,
        ColorToken.BgBase      => BgBase,
        ColorToken.BgSurface   => BgSurface,
        ColorToken.BgOverlay   => BgOverlay,
        ColorToken.BgHighlight => BgHighlight,
        ColorToken.FgPrimary   => FgPrimary,
        ColorToken.FgSecondary => FgSecondary,
        ColorToken.FgMuted     => FgMuted,
        ColorToken.FgDisabled  => FgDisabled,
        ColorToken.AccentPrimary   => AccentPrimary,
        ColorToken.AccentSecondary => AccentSecondary,
        ColorToken.AccentSuccess   => AccentSuccess,
        ColorToken.AccentWarning   => AccentWarning,
        ColorToken.AccentError     => AccentError,
        ColorToken.AccentInfo      => AccentInfo,
        ColorToken.AccentLink      => AccentLink,
        ColorToken.AccentSlot0     => AccentSlots.Length > 0  ? AccentSlots[0]  : AccentPrimary,
        ColorToken.AccentSlot1     => AccentSlots.Length > 1  ? AccentSlots[1]  : AccentPrimary,
        ColorToken.AccentSlot2     => AccentSlots.Length > 2  ? AccentSlots[2]  : AccentPrimary,
        ColorToken.AccentSlot3     => AccentSlots.Length > 3  ? AccentSlots[3]  : AccentPrimary,
        ColorToken.AccentSlot4     => AccentSlots.Length > 4  ? AccentSlots[4]  : AccentPrimary,
        ColorToken.AccentSlot5     => AccentSlots.Length > 5  ? AccentSlots[5]  : AccentPrimary,
        ColorToken.AccentSlot6     => AccentSlots.Length > 6  ? AccentSlots[6]  : AccentPrimary,
        ColorToken.AccentSlot7     => AccentSlots.Length > 7  ? AccentSlots[7]  : AccentPrimary,
        ColorToken.AccentSlot8     => AccentSlots.Length > 8  ? AccentSlots[8]  : AccentPrimary,
        ColorToken.AccentSlot9     => AccentSlots.Length > 9  ? AccentSlots[9]  : AccentPrimary,
        ColorToken.AccentSlot10    => AccentSlots.Length > 10 ? AccentSlots[10] : AccentPrimary,
        ColorToken.AccentSlot11    => AccentSlots.Length > 11 ? AccentSlots[11] : AccentPrimary,
        _ => PackedRgba.White,
    };

    // ── Built-in palettes ───────────────────────────────────────────────

    /// <summary>Cyberpunk Aurora (default dark theme).
    /// Port of the CyberpunkAurora constructor in theme.rs.</summary>
    public static ThemePalette CyberpunkAurora { get; } = new()
    {
        BgDeep       = PackedRgba.Rgb(7,  10, 18),
        BgBase       = PackedRgba.Rgb(14, 18, 30),
        BgSurface    = PackedRgba.Rgb(22, 28, 44),
        BgOverlay    = PackedRgba.Rgb(28, 36, 56),
        BgHighlight  = PackedRgba.Rgb(36, 48, 72),
        FgPrimary    = PackedRgba.Rgb(220, 225, 240),
        FgSecondary  = PackedRgba.Rgb(160, 170, 195),
        FgMuted      = PackedRgba.Rgb(90,  100, 125),
        FgDisabled   = PackedRgba.Rgb(55,  60,  75),
        AccentPrimary   = PackedRgba.Rgb(80,  180, 255),
        AccentSecondary = PackedRgba.Rgb(110, 140, 220),
        AccentSuccess   = PackedRgba.Rgb(80,  220, 100),
        AccentWarning   = PackedRgba.Rgb(255, 170, 50),
        AccentError     = PackedRgba.Rgb(255, 70,  70),
        AccentInfo      = PackedRgba.Rgb(100, 180, 255),
        AccentLink      = PackedRgba.Rgb(100, 160, 255),
        AccentSlots = new[]
        {
            PackedRgba.Rgb(255, 100, 80),   // red/crimson
            PackedRgba.Rgb(255, 160, 40),   // orange/amber
            PackedRgba.Rgb(255, 220, 30),   // gold/yellow
            PackedRgba.Rgb(80,  240, 80),   // lime/green
            PackedRgba.Rgb(40,  210, 190),  // teal
            PackedRgba.Rgb(60,  160, 255),  // blue/azure
            PackedRgba.Rgb(130, 100, 255),  // indigo/violet
            PackedRgba.Rgb(220, 80,  220),  // magenta/fuchsia
            PackedRgba.Rgb(255, 120, 160),  // pink/rose
            PackedRgba.Rgb(180, 200, 60),   // chartreuse
            PackedRgba.Rgb(80,  220, 180),  // aquamarine
            PackedRgba.Rgb(220, 140, 60),   // terra cotta
        },
    };

    /// <summary>Current global palette. Port of <c>fn current_palette() -> &ThemePalette</c>.</summary>
    public static ThemePalette CurrentPalette { get; set; } = CyberpunkAurora;
}

/// <summary>Foreground/text color tokens.
/// Port of <c>pub mod fg</c> in theme.rs.</summary>
public static class Fg
{
    public static ColorToken Primary   => ColorToken.FgPrimary;
    public static ColorToken Secondary => ColorToken.FgSecondary;
    public static ColorToken Muted     => ColorToken.FgMuted;
    public static ColorToken Disabled  => ColorToken.FgDisabled;
}

/// <summary>Background color tokens.
/// Port of <c>pub mod bg</c> in theme.rs.</summary>
public static class Bg
{
    public static ColorToken Deep      => ColorToken.BgDeep;
    public static ColorToken Base      => ColorToken.BgBase;
    public static ColorToken Surface   => ColorToken.BgSurface;
    public static ColorToken Overlay   => ColorToken.BgOverlay;
    public static ColorToken Highlight => ColorToken.BgHighlight;
}

/// <summary>Alpha / surface color tokens.
/// Port of <c>pub mod alpha</c> in theme.rs.</summary>
public static class Alpha
{
    public static ColorToken Surface => ColorToken.BgSurface;
}

/// <summary>Accent color tokens.
/// Port of <c>pub mod accent</c> in theme.rs.</summary>
public static class AccentPalette
{
    public static ColorToken Primary    => ColorToken.AccentPrimary;
    public static ColorToken Secondary  => ColorToken.AccentSecondary;
    public static ColorToken Success    => ColorToken.AccentSuccess;
    public static ColorToken Warning    => ColorToken.AccentWarning;
    public static ColorToken Error      => ColorToken.AccentError;
    public static ColorToken Info       => ColorToken.AccentInfo;
    public static ColorToken Link       => ColorToken.AccentLink;
    public static ColorToken Accent1    => ColorToken.AccentSlot0;
    public static ColorToken Accent2    => ColorToken.AccentSlot1;
    public static ColorToken Accent3    => ColorToken.AccentSlot2;
    public static ColorToken Accent4    => ColorToken.AccentSlot3;
    public static ColorToken Accent5    => ColorToken.AccentSlot4;
    public static ColorToken Accent6    => ColorToken.AccentSlot5;
    public static ColorToken Accent7    => ColorToken.AccentSlot6;
    public static ColorToken Accent8    => ColorToken.AccentSlot7;
    public static ColorToken Accent9    => ColorToken.AccentSlot8;
}

/// <summary>Theme system helpers.
/// Port of theme.rs functions.</summary>
public static class ThemeSystem
{
    /// <summary>Map a normalized value (0..1) through the accent gradient.
    /// Port of <c>pub fn accent_gradient(t: f64) -> PackedRgba</c>.</summary>
    public static PackedRgba AccentGradient(double t)
    {
        var slots = ThemePalette.CurrentPalette.AccentSlots;
        t = ((t % 1.0) + 1.0) % 1.0; // rem_euclid equivalent
        t = Math.Clamp(t, 0.0, 1.0);
        if (slots.Length == 0) return ThemePalette.CurrentPalette.Resolve(ColorToken.AccentPrimary);
        if (slots.Length == 1) return slots[0];

        int maxIdx = slots.Length - 1;
        double pos = t * maxIdx;
        int idx = Math.Min((int)Math.Floor(pos), maxIdx);
        double frac = pos - idx;

        var a = slots[idx];
        var b = slots[Math.Min(idx + 1, maxIdx)];

        byte r = (byte)Math.Round(a.R + (b.R - a.R) * frac);
        byte g = (byte)Math.Round(a.G + (b.G - a.G) * frac);
        byte bl = (byte)Math.Round(a.B + (b.B - a.B) * frac);
        return PackedRgba.Rgb(r, g, bl);
    }
}

/// <summary>Built-in theme identifiers. Port of ThemeId enum.</summary>
public enum ThemeId
{
    CyberpunkAurora, Darcula, Solar, Nord, HighContrast,
    Monokai, SolarizedDark, GruvboxDark, OneDark, TokyoNight,
    Dracula, KanagawaWave, EverforestDark, Synthwave84, Palenight
}
