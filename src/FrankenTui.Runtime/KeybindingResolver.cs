using FrankenTui.Core;

namespace FrankenTui.Runtime;

public sealed class KeybindingResolver
{
    private PendingEscape? _pendingEscape;

    public KeybindingResolver(KeybindingConfig? config = null)
    {
        Config = config ?? KeybindingConfig.Default;
    }

    public KeybindingConfig Config { get; }

    public bool AwaitingSecondEscape => _pendingEscape is not null;

    public IReadOnlyList<KeybindingDecision> Resolve(TerminalEvent terminalEvent, KeybindingState state)
    {
        ArgumentNullException.ThrowIfNull(terminalEvent);
        ArgumentNullException.ThrowIfNull(state);

        var decisions = new List<KeybindingDecision>(2);
        if (terminalEvent is not KeyTerminalEvent keyEvent)
        {
            FlushPendingInto(decisions, terminalEvent.Timestamp, state);
            return decisions;
        }

        if (_pendingEscape is { } pending &&
            !IsEscape(keyEvent.Gesture) &&
            terminalEvent.Timestamp - pending.Timestamp >= TimeSpan.Zero)
        {
            decisions.Add(ResolveEscapeAction(state, pending.Timestamp));
            _pendingEscape = null;
        }

        if (IsEscape(keyEvent.Gesture))
        {
            ResolveEscapeKey(keyEvent, state, decisions);
            return decisions;
        }

        _pendingEscape = null;

        if (IsCtrl(keyEvent.Gesture, 'c'))
        {
            decisions.Add(ResolveCtrlC(Config, state, keyEvent.Timestamp, keyEvent.Gesture));
        }
        else if (IsCtrl(keyEvent.Gesture, 'd'))
        {
            decisions.Add(new KeybindingDecision(KeybindingAction.SoftQuit, "ctrl_d", keyEvent.Timestamp, keyEvent.Gesture));
        }
        else if (IsCtrl(keyEvent.Gesture, 'q'))
        {
            decisions.Add(new KeybindingDecision(KeybindingAction.HardQuit, "ctrl_q", keyEvent.Timestamp, keyEvent.Gesture));
        }
        else
        {
            decisions.Add(new KeybindingDecision(KeybindingAction.PassThrough, "passthrough", keyEvent.Timestamp, keyEvent.Gesture));
        }

        return decisions;
    }

    public IReadOnlyList<KeybindingDecision> FlushExpired(KeybindingState state, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);

        var decisions = new List<KeybindingDecision>(1);
        FlushPendingInto(decisions, now, state);
        return decisions;
    }

    private void ResolveEscapeKey(KeyTerminalEvent keyEvent, KeybindingState state, List<KeybindingDecision> decisions)
    {
        if (Config.DisableEscapeSequences)
        {
            decisions.Add(ResolveEscapeAction(state, keyEvent.Timestamp, keyEvent.Gesture));
            return;
        }

        if (_pendingEscape is { } pending &&
            keyEvent.Timestamp - pending.Timestamp <= TimeSpan.FromMilliseconds(Config.EscSequenceTimeoutMs))
        {
            _pendingEscape = null;
            decisions.Add(new KeybindingDecision(KeybindingAction.ToggleTreeView, "escape_sequence", keyEvent.Timestamp, keyEvent.Gesture));
            return;
        }

        _pendingEscape = new PendingEscape(keyEvent.Timestamp, keyEvent.Gesture);
    }

    private void FlushPendingInto(List<KeybindingDecision> decisions, DateTimeOffset now, KeybindingState state)
    {
        if (_pendingEscape is not { } pending)
        {
            return;
        }

        var minimum = Config.DisableEscapeSequences
            ? TimeSpan.FromMilliseconds(Config.EscDebounceMs)
            : TimeSpan.FromMilliseconds(Config.EscSequenceTimeoutMs);
        if (now - pending.Timestamp < minimum)
        {
            return;
        }

        decisions.Add(ResolveEscapeAction(state, pending.Timestamp, pending.Gesture));
        _pendingEscape = null;
    }

    private static bool IsEscape(KeyGesture gesture) =>
        gesture.Key == TerminalKey.Escape && gesture.Modifiers == TerminalModifiers.None;

    private static bool IsCtrl(KeyGesture gesture, char character) =>
        gesture.IsCharacter &&
        gesture.Character is { } rune &&
        gesture.Modifiers.HasFlag(TerminalModifiers.Control) &&
        string.Equals(rune.ToString(), character.ToString(), StringComparison.OrdinalIgnoreCase);

    private static KeybindingDecision ResolveCtrlC(KeybindingConfig config, KeybindingState state, DateTimeOffset timestamp, KeyGesture gesture) =>
        state.ModalOpen
            ? new KeybindingDecision(KeybindingAction.DismissModal, "ctrl_c_modal", timestamp, gesture)
            : state.InputNonEmpty
                ? new KeybindingDecision(KeybindingAction.ClearInput, "ctrl_c_clear_input", timestamp, gesture)
                : state.TaskRunning
                    ? new KeybindingDecision(KeybindingAction.CancelTask, "ctrl_c_cancel_task", timestamp, gesture)
                    : ResolveIdleCtrlC(config, timestamp, gesture);

    private static KeybindingDecision ResolveIdleCtrlC(KeybindingConfig config, DateTimeOffset timestamp, KeyGesture gesture)
    {
        var action = config.CtrlCIdleAction switch
        {
            CtrlCIdleAction.Bell => KeybindingAction.Bell,
            CtrlCIdleAction.Noop => KeybindingAction.Noop,
            _ => KeybindingAction.Quit
        };

        return new KeybindingDecision(action, "ctrl_c_idle", timestamp, gesture);
    }

    private static KeybindingDecision ResolveEscapeAction(KeybindingState state, DateTimeOffset timestamp, KeyGesture? gesture = null)
    {
        var action = state.ModalOpen
            ? KeybindingAction.DismissModal
            : state.ViewOverlay
                ? KeybindingAction.CloseOverlay
                : state.InputNonEmpty
                    ? KeybindingAction.ClearInput
                    : state.TaskRunning
                        ? KeybindingAction.CancelTask
                        : KeybindingAction.PassThrough;

        return new KeybindingDecision(action, "escape", timestamp, gesture);
    }

    private readonly record struct PendingEscape(DateTimeOffset Timestamp, KeyGesture Gesture);
}
