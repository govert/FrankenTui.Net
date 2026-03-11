using System.Text.Json;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum MacroRecorderMode
{
    Idle,
    Recording,
    Ready,
    Playing,
    Error
}

public sealed record MacroRecordedEvent(long ScheduledMs, string EventType, string Display);

public sealed record MacroDefinition(
    string Id,
    long CreatedTs,
    IReadOnlyList<MacroRecordedEvent> Events,
    string Description)
{
    public string ToJson() => JsonSerializer.Serialize(
        this,
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        });
}

public sealed record MacroRecorderState(
    bool Recording = false,
    bool Playing = false,
    bool Loop = false,
    double Speed = 1.0,
    MacroDefinition? Macro = null,
    string Status = "Idle",
    MacroRecorderMode Mode = MacroRecorderMode.Idle,
    int CurrentEventIndex = 0,
    long PlaybackElapsedMs = 0,
    long LastDriftMs = 0,
    DateTimeOffset? LastTick = null,
    string? Error = null);

public static class MacroRecorder
{
    public static MacroDefinition FromEvents(string id, IReadOnlyList<TerminalEvent> events, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (events.Count == 0)
        {
            return new MacroDefinition(id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), [], description);
        }

        var origin = events[0].Timestamp;
        var recorded = events
            .Select(terminalEvent => new MacroRecordedEvent(
                (long)(terminalEvent.Timestamp - origin).TotalMilliseconds,
                terminalEvent.GetType().Name,
                Describe(terminalEvent)))
            .ToArray();
        return new MacroDefinition(id, origin.ToUnixTimeMilliseconds(), recorded, description);
    }

    public static IReadOnlyList<MacroRecordedEvent> ReplayPlan(MacroDefinition macro, int tickMs = 16)
    {
        ArgumentNullException.ThrowIfNull(macro);

        return macro.Events
            .Select(recorded => recorded with
            {
                ScheduledMs = (long)Math.Round(recorded.ScheduledMs / (double)tickMs) * tickMs
            })
            .ToArray();
    }

    private static string Describe(TerminalEvent terminalEvent) =>
        terminalEvent switch
        {
            KeyTerminalEvent keyEvent when keyEvent.Gesture.IsCharacter && keyEvent.Gesture.Character is { } rune => $"key:{rune}",
            KeyTerminalEvent keyEvent => $"key:{keyEvent.Gesture.Key}",
            MouseTerminalEvent mouseEvent => $"mouse:{mouseEvent.Gesture.Kind}:{mouseEvent.Gesture.Column},{mouseEvent.Gesture.Row}",
            ResizeTerminalEvent resizeEvent => $"resize:{resizeEvent.Size.Width}x{resizeEvent.Size.Height}",
            PasteTerminalEvent pasteEvent => $"paste:{pasteEvent.Text}",
            FocusTerminalEvent focusEvent => focusEvent.Focused ? "focus:gained" : "focus:lost",
            HoverTerminalEvent hoverEvent => $"hover:{hoverEvent.Column},{hoverEvent.Row}",
            _ => terminalEvent.GetType().Name
        };
}

public sealed record MacroTickResult(
    MacroRecorderState State,
    IReadOnlyList<TerminalEvent> EmittedEvents);

public static class MacroRecorderController
{
    public static MacroRecorderState ToggleRecording(
        MacroRecorderState state,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.Recording)
        {
            return state with
            {
                Recording = true,
                Playing = false,
                Mode = MacroRecorderMode.Recording,
                Macro = new MacroDefinition(
                    state.Macro?.Id ?? "macro-001",
                    now.ToUnixTimeMilliseconds(),
                    [],
                    state.Macro?.Description ?? "Hosted parity scenario"),
                CurrentEventIndex = 0,
                PlaybackElapsedMs = 0,
                LastDriftMs = 0,
                LastTick = now,
                Error = null,
                Status = "Recording... (Esc to stop)"
            };
        }

        return state with
        {
            Recording = false,
            Mode = state.Macro?.Events.Count > 0 ? MacroRecorderMode.Ready : MacroRecorderMode.Idle,
            Status = state.Macro?.Events.Count > 0
                ? $"Macro ready ({state.Macro.Events.Count} events)"
                : "Idle",
            LastTick = now
        };
    }

    public static MacroRecorderState TogglePlay(MacroRecorderState state, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Macro is null || state.Macro.Events.Count == 0)
        {
            return state with
            {
                Mode = MacroRecorderMode.Error,
                Error = "No macro loaded.",
                Status = "No macro loaded."
            };
        }

        if (state.Playing)
        {
            return state with
            {
                Playing = false,
                Mode = MacroRecorderMode.Ready,
                Status = "Playback paused.",
                LastTick = now
            };
        }

        return state with
        {
            Recording = false,
            Playing = true,
            Mode = MacroRecorderMode.Playing,
            CurrentEventIndex = 0,
            PlaybackElapsedMs = 0,
            LastDriftMs = 0,
            LastTick = now,
            Error = null,
            Status = $"Playing {state.Macro.Id} at {state.Speed:0.##}x"
        };
    }

    public static MacroRecorderState ToggleLoop(MacroRecorderState state) =>
        state with
        {
            Loop = !state.Loop,
            Status = state.Loop ? "Loop off." : "Loop on."
        };

    public static MacroRecorderState AdjustSpeed(MacroRecorderState state, double delta)
    {
        var speed = Math.Clamp(state.Speed + delta, 0.25, 4.0);
        return state with
        {
            Speed = speed,
            Status = $"Speed {speed:0.##}x"
        };
    }

    public static MacroRecorderState Stop(MacroRecorderState state, string status = "Idle") =>
        state with
        {
            Recording = false,
            Playing = false,
            Mode = state.Macro is null ? MacroRecorderMode.Idle : MacroRecorderMode.Ready,
            CurrentEventIndex = 0,
            PlaybackElapsedMs = 0,
            LastDriftMs = 0,
            Error = null,
            Status = status
        };

    public static MacroRecorderState Capture(MacroRecorderState state, TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(terminalEvent);

        if (!state.Recording || state.Macro is null)
        {
            return state;
        }

        var origin = DateTimeOffset.FromUnixTimeMilliseconds(state.Macro.CreatedTs);
        var recorded = new MacroRecordedEvent(
            (long)(terminalEvent.Timestamp - origin).TotalMilliseconds,
            terminalEvent.GetType().Name,
            DescribeForReplay(terminalEvent));
        return state with
        {
            Macro = state.Macro with
            {
                Events = state.Macro.Events.Concat([recorded]).ToArray()
            },
            Status = $"Recording... ({state.Macro.Events.Count + 1} events)"
        };
    }

    public static MacroTickResult Tick(MacroRecorderState state, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.Playing || state.Macro is null)
        {
            return new MacroTickResult(state with { LastTick = now }, []);
        }

        var lastTick = state.LastTick ?? now;
        var deltaMs = Math.Max((long)Math.Round((now - lastTick).TotalMilliseconds * state.Speed), 0);
        var elapsed = state.PlaybackElapsedMs + deltaMs;
        var due = new List<TerminalEvent>();
        var index = state.CurrentEventIndex;
        while (index < state.Macro.Events.Count && state.Macro.Events[index].ScheduledMs <= elapsed)
        {
            var scheduled = state.Macro.Events[index];
            due.Add(ReplayEvent(scheduled, now));
            index++;
        }

        if (index >= state.Macro.Events.Count)
        {
            if (state.Loop && state.Macro.Events.Count > 0)
            {
                return new MacroTickResult(
                    state with
                    {
                        CurrentEventIndex = 0,
                        PlaybackElapsedMs = 0,
                        LastDriftMs = elapsed - state.Macro.Events[^1].ScheduledMs,
                        LastTick = now,
                        Status = $"Looping {state.Macro.Id}"
                    },
                    due);
            }

            return new MacroTickResult(
                state with
                {
                    Playing = false,
                    Mode = MacroRecorderMode.Ready,
                    CurrentEventIndex = 0,
                    PlaybackElapsedMs = elapsed,
                    LastDriftMs = state.Macro.Events.Count == 0 ? 0 : elapsed - state.Macro.Events[^1].ScheduledMs,
                    LastTick = now,
                    Status = $"Playback finished ({state.Macro.Events.Count} events)"
                },
                due);
        }

        return new MacroTickResult(
            state with
            {
                CurrentEventIndex = index,
                PlaybackElapsedMs = elapsed,
                LastDriftMs = due.Count == 0 ? state.LastDriftMs : elapsed - state.Macro.Events[index - 1].ScheduledMs,
                LastTick = now,
                Status = $"Playing {state.Macro.Id} ({index}/{state.Macro.Events.Count})"
            },
            due);
    }

    private static string DescribeForReplay(TerminalEvent terminalEvent) =>
        terminalEvent switch
        {
            KeyTerminalEvent keyEvent when keyEvent.Gesture.IsCharacter && keyEvent.Gesture.Character is { } rune => $"char:{rune}",
            KeyTerminalEvent keyEvent => $"key:{keyEvent.Gesture.Key}",
            ResizeTerminalEvent resizeEvent => $"resize:{resizeEvent.Size.Width}x{resizeEvent.Size.Height}",
            PasteTerminalEvent pasteEvent => $"paste:{pasteEvent.Text}",
            _ => terminalEvent.GetType().Name
        };

    private static TerminalEvent ReplayEvent(MacroRecordedEvent recorded, DateTimeOffset timestamp)
    {
        if (recorded.Display.StartsWith("char:", StringComparison.Ordinal))
        {
            var value = recorded.Display["char:".Length..];
            var rune = string.IsNullOrEmpty(value) ? new Rune('?') : value.EnumerateRunes().First();
            return TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, rune), timestamp);
        }

        if (recorded.Display.StartsWith("key:", StringComparison.Ordinal) &&
            Enum.TryParse<TerminalKey>(recorded.Display["key:".Length..], ignoreCase: true, out var key))
        {
            return TerminalEvent.Key(new KeyGesture(key, TerminalModifiers.None), timestamp);
        }

        if (recorded.Display.StartsWith("resize:", StringComparison.Ordinal))
        {
            var dims = recorded.Display["resize:".Length..].Split('x', 2);
            if (dims.Length == 2 &&
                ushort.TryParse(dims[0], out var width) &&
                ushort.TryParse(dims[1], out var height))
            {
                return TerminalEvent.Resize(new Size(width, height), timestamp);
            }
        }

        if (recorded.Display.StartsWith("paste:", StringComparison.Ordinal))
        {
            return TerminalEvent.Paste(recorded.Display["paste:".Length..], timestamp);
        }

        return TerminalEvent.Key(new KeyGesture(TerminalKey.Unknown, TerminalModifiers.None), timestamp);
    }
}

public sealed class MacroRecorderWidget : IWidget
{
    public MacroRecorderState State { get; init; } = new();

    public void Render(RuntimeRenderContext context)
    {
        var macro = State.Macro;
        var lines = new List<string>
        {
            $"State: {State.Mode.ToString().ToLowerInvariant()}",
            $"Loop: {(State.Loop ? "on" : "off")}  Speed: {State.Speed:0.##}x",
            $"Status: {State.Status}"
        };

        if (macro is null)
        {
            lines.Add("No macro loaded.");
        }
        else
        {
            lines.Add($"Macro: {macro.Id} ({macro.Events.Count} events)");
            lines.Add($"Cursor: {State.CurrentEventIndex}/{macro.Events.Count}  Drift: {State.LastDriftMs}ms");
            lines.AddRange(macro.Events.Take(4).Select(static item => $"{item.ScheduledMs,4}ms {item.Display}"));
        }

        if (!string.IsNullOrWhiteSpace(State.Error))
        {
            lines.Add($"Error: {State.Error}");
        }

        new PanelWidget
        {
            Title = "Macro Recorder",
            Child = new ParagraphWidget(string.Join(Environment.NewLine, lines))
        }.Render(context);
    }
}
