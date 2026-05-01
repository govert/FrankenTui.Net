using System.Text.Json;
using FrankenTui.Extras;
using FrankenTui.Web;
using FrankenTui.Widgets;

namespace FrankenTui.Showcase.Wasm;

public enum ShowcaseRunnerPanePhase
{
    PointerDown,
    PointerMove,
    PointerUp,
    PointerCancel,
    PointerLeave,
    NativeTouchGesture,
    Blur,
    VisibilityHidden,
    LostPointerCapture,
    ContextLost,
    RenderStalled
}

public enum ShowcaseRunnerPaneCommand
{
    None,
    Acquire,
    Release
}

public enum ShowcaseRunnerPaneOutcome
{
    SemanticForwarded,
    CaptureStateUpdated,
    Ignored
}

public sealed record ShowcaseRunnerStepResult(
    bool Running,
    bool Rendered,
    int EventsProcessed,
    ulong FrameIndex,
    WebFrame Frame);

public sealed record ShowcaseRunnerPaneDispatch(
    ShowcaseRunnerPanePhase Phase,
    ulong Sequence,
    uint? PointerId,
    ShowcaseRunnerPaneCommand Command,
    ShowcaseRunnerPaneOutcome Outcome,
    string Reason,
    uint? CommandPointerId = null)
{
    public bool Accepted => Outcome != ShowcaseRunnerPaneOutcome.Ignored;

    public string ToLogLine()
    {
        var pointer = PointerId?.ToString() ?? "-";
        var commandPointer = CommandPointerId?.ToString() ?? "-";
        return $"pane_pointer phase={FormatPhase(Phase)} seq={Sequence} pointer={pointer} command={FormatCommand(Command)} command_pointer={commandPointer} outcome={FormatOutcome(Outcome, Reason)}";
    }

    private static string FormatPhase(ShowcaseRunnerPanePhase phase) => phase switch
    {
        ShowcaseRunnerPanePhase.PointerDown => "pointer_down",
        ShowcaseRunnerPanePhase.PointerMove => "pointer_move",
        ShowcaseRunnerPanePhase.PointerUp => "pointer_up",
        ShowcaseRunnerPanePhase.PointerCancel => "pointer_cancel",
        ShowcaseRunnerPanePhase.PointerLeave => "pointer_leave",
        ShowcaseRunnerPanePhase.NativeTouchGesture => "native_touch_gesture",
        ShowcaseRunnerPanePhase.Blur => "blur",
        ShowcaseRunnerPanePhase.VisibilityHidden => "visibility_hidden",
        ShowcaseRunnerPanePhase.LostPointerCapture => "lost_pointer_capture",
        ShowcaseRunnerPanePhase.ContextLost => "context_lost",
        ShowcaseRunnerPanePhase.RenderStalled => "render_stalled",
        _ => "unknown"
    };

    private static string FormatCommand(ShowcaseRunnerPaneCommand command) => command switch
    {
        ShowcaseRunnerPaneCommand.Acquire => "acquire",
        ShowcaseRunnerPaneCommand.Release => "release",
        _ => "none"
    };

    private static string FormatOutcome(ShowcaseRunnerPaneOutcome outcome, string reason) => outcome switch
    {
        ShowcaseRunnerPaneOutcome.Ignored => $"ignored:{reason}",
        ShowcaseRunnerPaneOutcome.CaptureStateUpdated => "capture_state_updated",
        _ => "semantic_forwarded"
    };
}

public sealed class ShowcaseRunnerCore
{
    private readonly List<string> _logs = [];
    private HostedParityScenarioId _scenarioId;
    private bool _inlineMode;
    private ushort _width;
    private ushort _height;
    private string _language;
    private WidgetFlowDirection _flowDirection;
    private ulong _frameIndex;
    private ulong _paneSequence;
    private uint? _activePointerId;
    private bool _captureAcquired;
    private bool _running = true;

    public ShowcaseRunnerCore(
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        ushort width = 64,
        ushort height = 18,
        bool inlineMode = false,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight)
    {
        _scenarioId = scenarioId;
        _width = ClampDimension(width);
        _height = ClampDimension(height);
        _inlineMode = inlineMode;
        _language = string.IsNullOrWhiteSpace(language) ? "en-US" : language;
        _flowDirection = flowDirection;
    }

    public ulong FrameIndex => _frameIndex;

    public uint? ActivePointerId => _activePointerId;

    public bool IsRunning => _running;

    public WebFrame RenderCurrent() =>
        ShowcasePage.RenderScenario(
            _scenarioId,
            checked((int)Math.Min(_frameIndex, int.MaxValue)),
            _inlineMode,
            _width,
            _height,
            _language,
            _flowDirection);

    public ShowcaseRunnerStepResult Step()
    {
        if (!_running)
        {
            return new ShowcaseRunnerStepResult(false, false, 0, _frameIndex, RenderCurrent());
        }

        _frameIndex++;
        return new ShowcaseRunnerStepResult(true, true, 0, _frameIndex, RenderCurrent());
    }

    public void Resize(ushort width, ushort height)
    {
        _width = ClampDimension(width);
        _height = ClampDimension(height);
    }

    public bool PushEncodedInput(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (TryGetStringProperty(document.RootElement, "scenario", out var scenarioName) &&
                Enum.TryParse<HostedParityScenarioId>(scenarioName, ignoreCase: true, out var scenario))
            {
                _scenarioId = scenario;
                return true;
            }

            if (TryGetStringProperty(document.RootElement, "kind", out var kind) &&
                string.Equals(kind, "quit", StringComparison.OrdinalIgnoreCase))
            {
                _running = false;
                return true;
            }

            return !string.IsNullOrWhiteSpace(kind);
        }
    }

    public ShowcaseRunnerPaneDispatch PanePointerDownAt(uint pointerId)
    {
        if (_activePointerId is not null && _activePointerId != pointerId)
        {
            return Record(
                ShowcaseRunnerPanePhase.PointerDown,
                pointerId,
                ShowcaseRunnerPaneCommand.None,
                ShowcaseRunnerPaneOutcome.Ignored,
                "active_pointer_already_in_progress");
        }

        _activePointerId = pointerId;
        _captureAcquired = false;
        return Record(
            ShowcaseRunnerPanePhase.PointerDown,
            pointerId,
            ShowcaseRunnerPaneCommand.Acquire,
            ShowcaseRunnerPaneOutcome.SemanticForwarded,
            "accepted",
            pointerId);
    }

    public ShowcaseRunnerPaneDispatch PanePointerCaptureAcquired(uint pointerId)
    {
        if (_activePointerId != pointerId)
        {
            return Record(
                ShowcaseRunnerPanePhase.PointerDown,
                pointerId,
                ShowcaseRunnerPaneCommand.None,
                ShowcaseRunnerPaneOutcome.Ignored,
                "pointer_mismatch");
        }

        _captureAcquired = true;
        return Record(
            ShowcaseRunnerPanePhase.PointerDown,
            pointerId,
            ShowcaseRunnerPaneCommand.None,
            ShowcaseRunnerPaneOutcome.CaptureStateUpdated,
            "capture_acquired");
    }

    public ShowcaseRunnerPaneDispatch PaneTouchPointerDownAt(uint pointerId, byte activeTouchPoints)
    {
        if (activeTouchPoints > 1)
        {
            return InterruptActive(
                ShowcaseRunnerPanePhase.NativeTouchGesture,
                "native_touch_gesture",
                pointerId,
                releaseOnlyWhenCaptured: true);
        }

        return PanePointerDownAt(pointerId);
    }

    public ShowcaseRunnerPaneDispatch PaneContextLost() =>
        InterruptActive(ShowcaseRunnerPanePhase.ContextLost, "context_lost", null, releaseOnlyWhenCaptured: true);

    public ShowcaseRunnerPaneDispatch PaneRenderStalled() =>
        InterruptActive(ShowcaseRunnerPanePhase.RenderStalled, "render_stalled", null, releaseOnlyWhenCaptured: true);

    public IReadOnlyList<string> TakeLogs()
    {
        var logs = _logs.ToArray();
        _logs.Clear();
        return logs;
    }

    private ShowcaseRunnerPaneDispatch InterruptActive(
        ShowcaseRunnerPanePhase phase,
        string reason,
        uint? signalPointerId,
        bool releaseOnlyWhenCaptured)
    {
        if (_activePointerId is null)
        {
            return Record(
                phase,
                signalPointerId,
                ShowcaseRunnerPaneCommand.None,
                ShowcaseRunnerPaneOutcome.Ignored,
                "no_active_pointer");
        }

        var releasedPointer = _activePointerId;
        var command = _captureAcquired || !releaseOnlyWhenCaptured
            ? ShowcaseRunnerPaneCommand.Release
            : ShowcaseRunnerPaneCommand.None;
        _activePointerId = null;
        _captureAcquired = false;
        return Record(
            phase,
            releasedPointer,
            command,
            ShowcaseRunnerPaneOutcome.SemanticForwarded,
            reason,
            command == ShowcaseRunnerPaneCommand.Release ? releasedPointer : null);
    }

    private ShowcaseRunnerPaneDispatch Record(
        ShowcaseRunnerPanePhase phase,
        uint? pointerId,
        ShowcaseRunnerPaneCommand command,
        ShowcaseRunnerPaneOutcome outcome,
        string reason,
        uint? commandPointerId = null)
    {
        var dispatch = new ShowcaseRunnerPaneDispatch(
            phase,
            ++_paneSequence,
            pointerId,
            command,
            outcome,
            reason,
            commandPointerId);
        _logs.Add(dispatch.ToLogLine());
        return dispatch;
    }

    private static ushort ClampDimension(ushort value) => value == 0 ? (ushort)1 : value;

    private static bool TryGetStringProperty(JsonElement element, string name, out string value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString() ?? string.Empty;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
