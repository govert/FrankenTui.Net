using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

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
    string Status = "Idle");

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

public sealed class MacroRecorderWidget : IWidget
{
    public MacroRecorderState State { get; init; } = new();

    public void Render(RuntimeRenderContext context)
    {
        var macro = State.Macro;
        var lines = new List<string>
        {
            $"State: {(State.Recording ? "recording" : State.Playing ? "playing" : "idle")}",
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
            lines.AddRange(macro.Events.Take(4).Select(static item => $"{item.ScheduledMs,4}ms {item.Display}"));
        }

        new PanelWidget
        {
            Title = "Macro Recorder",
            Child = new ParagraphWidget(string.Join(Environment.NewLine, lines))
        }.Render(context);
    }
}
