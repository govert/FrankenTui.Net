// Ported from upstream FrankenTUI:
//   crates/ftui-widgets/src/modal/stack.rs
//   crates/ftui-widgets/src/focus/manager.rs
// Source basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Managed realization: tracing spans use ActivitySource; discrete tracing events
// use DiagnosticListener so production observers can capture the same fields.

using System.Diagnostics;

namespace FrankenTui.Widgets.Modal;

/// <summary>
/// Observable modal and modal-focus telemetry corresponding to upstream's
/// optional <c>tracing</c> instrumentation.
/// </summary>
public static class ModalTelemetry
{
    public const string ActivitySourceName = "FrankenTui.Widgets.Modal";
    public const string DiagnosticListenerName = "FrankenTui.Widgets.Modal.Events";

    public const string ModalRenderActivityName = "modal.render";
    public const string FocusChangeEventName = "focus.change";
    public const string FocusTrapPushEventName = "focus.trap_push";
    public const string FocusTrapPopEventName = "focus.trap_pop";

    /// <summary>The activity source used for modal render spans.</summary>
    public static ActivitySource Activities { get; } = new(ActivitySourceName);

    /// <summary>The diagnostic listener used for discrete modal/focus events.</summary>
    public static DiagnosticListener Events { get; } = new(DiagnosticListenerName);

    internal static Activity? StartRender(
        string modalType,
        bool focusTrapped,
        bool backdropActive,
        out long startTimestamp)
    {
        if (!Activities.HasListeners())
        {
            startTimestamp = 0;
            return null;
        }

        startTimestamp = Stopwatch.GetTimestamp();
        var tags = new ActivityTagsCollection
        {
            ["modal_type"] = modalType,
            ["focus_trapped"] = focusTrapped,
            ["backdrop_active"] = backdropActive,
        };

        return Activities.StartActivity(
            ModalRenderActivityName,
            ActivityKind.Internal,
            default(ActivityContext),
            tags);
    }

    internal static void CompleteRender(Activity? activity, long startTimestamp)
    {
        if (activity is null)
            return;

        long durationMicroseconds = Stopwatch.GetElapsedTime(startTimestamp).Ticks / 10;
        activity.SetTag("render_duration_us", durationMicroseconds);
    }

    internal static void WriteEvent(
        string name,
        params (string Name, object? Value)[] fields)
    {
        if (!Events.IsEnabled(name))
            return;

        var payload = new Dictionary<string, object?>(fields.Length, StringComparer.Ordinal);
        foreach (var field in fields)
            payload[field.Name] = field.Value;

        Events.Write(name, payload);
    }
}
