using FrankenTui.Core;

namespace FrankenTui.Runtime;

public enum CtrlCIdleAction
{
    Quit,
    Noop,
    Bell
}

public enum KeybindingAction
{
    PassThrough,
    DismissModal,
    ClearInput,
    CancelTask,
    CloseOverlay,
    ToggleTreeView,
    Quit,
    SoftQuit,
    HardQuit,
    Noop,
    Bell
}

public sealed record KeybindingState(
    bool InputNonEmpty = false,
    bool TaskRunning = false,
    bool ModalOpen = false,
    bool ViewOverlay = false);

public sealed record KeybindingConfig(
    CtrlCIdleAction CtrlCIdleAction,
    int EscSequenceTimeoutMs,
    int EscDebounceMs,
    bool DisableEscapeSequences)
{
    public static KeybindingConfig Default { get; } = new(
        CtrlCIdleAction.Quit,
        250,
        50,
        DisableEscapeSequences: false);

    public static KeybindingConfig FromEnvironment(IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                static entry => (string)entry.Key,
                static entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);

        var idleAction = TryParseIdleAction(Get(environment, "FTUI_CTRL_C_IDLE_ACTION"), out var parsedIdle)
            ? parsedIdle
            : CtrlCIdleAction.Quit;
        var escTimeout = ParseInt(Get(environment, "FTUI_ESC_SEQ_TIMEOUT_MS"), 250, 150, 400);
        var debounce = ParseInt(Get(environment, "FTUI_ESC_DEBOUNCE_MS"), 50, 0, 100);
        var disableSequences = bool.TryParse(Get(environment, "FTUI_DISABLE_ESC_SEQ"), out var parsedDisable) && parsedDisable;

        return new KeybindingConfig(idleAction, escTimeout, debounce, disableSequences);
    }

    private static string? Get(IReadOnlyDictionary<string, string?> environment, string name) =>
        environment.TryGetValue(name, out var value) ? value : null;

    private static int ParseInt(string? text, int fallback, int min, int max)
    {
        if (!int.TryParse(text, out var value))
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }

    private static bool TryParseIdleAction(string? text, out CtrlCIdleAction action)
    {
        action = CtrlCIdleAction.Quit;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Enum.TryParse(text, ignoreCase: true, out action);
    }
}

public sealed record KeybindingDecision(
    KeybindingAction Action,
    string Reason,
    DateTimeOffset Timestamp,
    KeyGesture? Gesture = null);
