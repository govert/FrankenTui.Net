namespace FrankenTui.Core;

public sealed record TerminalCapabilities
{
    private static readonly string[] ModernPrograms =
    [
        "iterm.app",
        "wezterm",
        "alacritty",
        "ghostty",
        "kitty",
        "rio",
        "hyper",
        "contour"
    ];

    private static readonly string[] SyncOutputPrograms =
    [
        "alacritty",
        "ghostty",
        "kitty",
        "contour"
    ];

    public TerminalCapabilities(bool InScreen = false)
    {
        this.InScreen = InScreen;
    }

    public TerminalProfile Profile { get; init; } = TerminalProfile.Dumb;
    public bool TrueColor { get; init; }
    public bool Colors256 { get; init; }
    public bool UnicodeBoxDrawing { get; init; }
    public bool UnicodeEmoji { get; init; }
    public bool DoubleWidth { get; init; }
    public bool SyncOutput { get; init; }
    public bool Osc8Hyperlinks { get; init; }
    public bool ScrollRegion { get; init; }
    public bool InTmux { get; init; }
    public bool InScreen { get; init; }
    public bool InZellij { get; init; }
    public bool InWeztermMux { get; init; }
    public bool KittyKeyboard { get; init; }
    public bool FocusEvents { get; init; }
    public bool BracketedPaste { get; init; }
    public bool MouseSgr { get; init; }
    public bool Osc52Clipboard { get; init; }

    public string? ProfileName => Profile == TerminalProfile.Detected ? null : Profile switch
    {
        TerminalProfile.Modern => "modern",
        TerminalProfile.Xterm256Color => "xterm-256color",
        TerminalProfile.Xterm => "xterm",
        TerminalProfile.Vt100 => "vt100",
        TerminalProfile.Dumb => "dumb",
        TerminalProfile.Screen => "screen",
        TerminalProfile.Tmux => "tmux",
        TerminalProfile.Zellij => "zellij",
        TerminalProfile.WindowsConsole => "windows-console",
        TerminalProfile.Kitty => "kitty",
        TerminalProfile.LinuxConsole => "linux",
        TerminalProfile.Custom => "custom",
        _ => null
    };

    public bool InAnyMux() => InTmux || InScreen || InZellij || InWeztermMux;

    public bool UseSyncOutput() => SyncOutput && !InAnyMux();

    public bool UseScrollRegion() => ScrollRegion && !InAnyMux();

    public bool UseHyperlinks() => Osc8Hyperlinks && !InAnyMux();

    public bool NeedsPassthroughWrap() => InTmux || InScreen;

    public static TerminalCapabilities Basic() =>
        new()
        {
            Profile = TerminalProfile.Dumb,
            TrueColor = false,
            Colors256 = false,
            UnicodeBoxDrawing = false,
            UnicodeEmoji = false,
            DoubleWidth = false,
            SyncOutput = false,
            Osc8Hyperlinks = false,
            ScrollRegion = false,
            KittyKeyboard = false,
            FocusEvents = false,
            BracketedPaste = false,
            MouseSgr = false,
            Osc52Clipboard = false
        };

    public static TerminalCapabilities Modern() =>
        Basic() with
        {
            Profile = TerminalProfile.Modern,
            TrueColor = true,
            Colors256 = true,
            UnicodeBoxDrawing = true,
            UnicodeEmoji = true,
            DoubleWidth = true,
            SyncOutput = true,
            Osc8Hyperlinks = true,
            ScrollRegion = true,
            KittyKeyboard = true,
            FocusEvents = true,
            BracketedPaste = true,
            MouseSgr = true,
            Osc52Clipboard = true
        };

    public static TerminalCapabilities Xterm256Color() =>
        Modern() with
        {
            Profile = TerminalProfile.Xterm256Color,
            TrueColor = false,
            SyncOutput = false,
            Osc8Hyperlinks = false,
            KittyKeyboard = false,
            FocusEvents = false,
            Osc52Clipboard = false
        };

    public static TerminalCapabilities Xterm() =>
        Xterm256Color() with
        {
            Profile = TerminalProfile.Xterm,
            Colors256 = false,
            UnicodeEmoji = false
        };

    public static TerminalCapabilities Vt100() =>
        Basic() with
        {
            Profile = TerminalProfile.Vt100,
            ScrollRegion = true
        };

    public static TerminalCapabilities Dumb() => Basic();

    public static TerminalCapabilities Screen() =>
        Xterm256Color() with
        {
            Profile = TerminalProfile.Screen,
            InScreen = true
        };

    public static TerminalCapabilities Tmux() =>
        Xterm256Color() with
        {
            Profile = TerminalProfile.Tmux,
            InTmux = true
        };

    public static TerminalCapabilities Zellij() =>
        Modern() with
        {
            Profile = TerminalProfile.Zellij,
            SyncOutput = false,
            Osc8Hyperlinks = false,
            InZellij = true
        };

    public static TerminalCapabilities WindowsConsole() =>
        Modern() with
        {
            Profile = TerminalProfile.WindowsConsole,
            SyncOutput = false
        };

    public static TerminalCapabilities Kitty() =>
        Modern() with
        {
            Profile = TerminalProfile.Kitty
        };

    public static TerminalCapabilities LinuxConsole() =>
        Basic() with
        {
            Profile = TerminalProfile.LinuxConsole,
            ScrollRegion = true
        };

    public static TerminalCapabilities FromProfile(TerminalProfile profile) => profile switch
    {
        TerminalProfile.Modern => Modern(),
        TerminalProfile.Xterm256Color => Xterm256Color(),
        TerminalProfile.Xterm => Xterm(),
        TerminalProfile.Vt100 => Vt100(),
        TerminalProfile.Dumb => Dumb(),
        TerminalProfile.Screen => Screen(),
        TerminalProfile.Tmux => Tmux(),
        TerminalProfile.Zellij => Zellij(),
        TerminalProfile.WindowsConsole => WindowsConsole(),
        TerminalProfile.Kitty => Kitty(),
        TerminalProfile.LinuxConsole => LinuxConsole(),
        TerminalProfile.Custom => Basic() with { Profile = TerminalProfile.Custom },
        TerminalProfile.Detected => Detect(),
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown terminal profile.")
    };

    public static TerminalCapabilities Detect() =>
        Detect(Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => (string)entry.Key,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase));

    public static TerminalCapabilities Detect(IReadOnlyDictionary<string, string?> environment)
    {
        if (TryGet(environment, "FTUI_TEST_PROFILE", out var profileName) &&
            EnumProfile(profileName) is { } forcedProfile)
        {
            return FromProfile(forcedProfile);
        }

        var term = Get(environment, "TERM");
        var termProgram = Get(environment, "TERM_PROGRAM");
        var colorTerm = Get(environment, "COLORTERM");
        var noColor = environment.ContainsKey("NO_COLOR");
        var inTmux = environment.ContainsKey("TMUX");
        var inScreen = environment.ContainsKey("STY");
        var inZellij = environment.ContainsKey("ZELLIJ");
        var inWeztermMux =
            environment.ContainsKey("WEZTERM_UNIX_SOCKET") ||
            environment.ContainsKey("WEZTERM_PANE") ||
            environment.ContainsKey("WEZTERM_EXECUTABLE");
        var kittyWindow = environment.ContainsKey("KITTY_WINDOW_ID");
        var wtSession = environment.ContainsKey("WT_SESSION");

        if (string.IsNullOrWhiteSpace(term) || term.Equals("dumb", StringComparison.OrdinalIgnoreCase))
        {
            return Dumb() with { Profile = TerminalProfile.Detected };
        }

        var normalizedTerm = term.ToLowerInvariant();
        var normalizedProgram = termProgram.ToLowerInvariant();
        var isKitty = kittyWindow || normalizedTerm.Contains("kitty");
        var isModern = isKitty || ModernPrograms.Contains(normalizedProgram);
        var trueColor = !noColor && (colorTerm.Contains("truecolor", StringComparison.OrdinalIgnoreCase) || isModern);
        var colors256 = !noColor && (trueColor || normalizedTerm.Contains("256color"));
        var emoji = normalizedTerm is not "vt100" && normalizedTerm is not "linux";
        var profile =
            inTmux ? TerminalProfile.Tmux :
            inScreen ? TerminalProfile.Screen :
            inZellij ? TerminalProfile.Zellij :
            isKitty ? TerminalProfile.Kitty :
            wtSession ? TerminalProfile.WindowsConsole :
            normalizedTerm.Contains("xterm-256color") ? TerminalProfile.Xterm256Color :
            normalizedTerm.Contains("xterm") ? TerminalProfile.Xterm :
            normalizedTerm.Contains("vt100") ? TerminalProfile.Vt100 :
            normalizedTerm.Contains("linux") ? TerminalProfile.LinuxConsole :
            isModern ? TerminalProfile.Modern :
            TerminalProfile.Detected;

        return Basic() with
        {
            Profile = profile,
            TrueColor = trueColor,
            Colors256 = colors256,
            UnicodeBoxDrawing = !normalizedTerm.Contains("dumb"),
            UnicodeEmoji = emoji,
            DoubleWidth = !normalizedTerm.Contains("vt100") && !normalizedTerm.Contains("dumb"),
            SyncOutput = SyncOutputPrograms.Contains(normalizedProgram) || isKitty,
            Osc8Hyperlinks = !noColor && (isModern || isKitty),
            ScrollRegion = true,
            InTmux = inTmux,
            InScreen = inScreen,
            InZellij = inZellij,
            InWeztermMux = inWeztermMux,
            KittyKeyboard = isKitty || normalizedProgram is "wezterm" or "iterm.app" or "alacritty" or "ghostty",
            FocusEvents = isModern || isKitty,
            BracketedPaste = true,
            MouseSgr = !normalizedTerm.Contains("vt100"),
            Osc52Clipboard = !noColor && !inTmux && !inScreen && !inZellij && (isModern || isKitty)
        };
    }

    private static string Get(IReadOnlyDictionary<string, string?> environment, string key) =>
        TryGet(environment, key, out var value) ? value ?? string.Empty : string.Empty;

    private static bool TryGet(IReadOnlyDictionary<string, string?> environment, string key, out string? value)
    {
        value = null;
        foreach (var pair in environment)
        {
            if (!pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = pair.Value;
            return true;
        }

        return false;
    }

    private static TerminalProfile? EnumProfile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return name.Trim().ToLowerInvariant() switch
        {
            "modern" => TerminalProfile.Modern,
            "xterm-256color" or "xterm256color" => TerminalProfile.Xterm256Color,
            "xterm" => TerminalProfile.Xterm,
            "vt100" => TerminalProfile.Vt100,
            "dumb" => TerminalProfile.Dumb,
            "screen" or "screen-256color" => TerminalProfile.Screen,
            "tmux" or "tmux-256color" => TerminalProfile.Tmux,
            "zellij" => TerminalProfile.Zellij,
            "windows-console" or "windows" or "conhost" => TerminalProfile.WindowsConsole,
            "kitty" or "xterm-kitty" => TerminalProfile.Kitty,
            "linux" or "linux-console" => TerminalProfile.LinuxConsole,
            "custom" => TerminalProfile.Custom,
            "detected" or "auto" => TerminalProfile.Detected,
            _ => null
        };
    }
}
