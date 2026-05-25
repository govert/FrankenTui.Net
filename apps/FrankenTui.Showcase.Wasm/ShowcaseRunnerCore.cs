using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
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

public sealed record ShowcaseRunnerPatchStats(
    int DirtyCells,
    int PatchCount,
    long BytesUploaded);

public sealed record ShowcaseRunnerFlatPatchBatch(
    IReadOnlyList<uint> Cells,
    IReadOnlyList<uint> Spans);

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
    private int? _screenNumber;
    private ShowcaseDemoState? _screenState;
    private readonly List<TerminalEvent> _pendingScreenEvents = [];
    private DateTimeOffset _clock = DateTimeOffset.UnixEpoch;
    private bool _clockDirty;
    private ShowcaseRunnerPatchStats? _patchStats;
    private uint[] _flatCells = [];
    private uint[] _flatSpans = [];
    private ulong _paneWorkspaceGeneration;
    private ulong _paneSavedWorkspaceGeneration;
    private string? _paneWorkspaceSnapshot;
    private int _pendingEventsProcessed;
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
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight,
        int? screenNumber = null)
    {
        _scenarioId = scenarioId;
        _screenNumber = screenNumber is { } screen ? ClampScreenNumber(screen) : null;
        _width = ClampDimension(width);
        _height = ClampDimension(height);
        _inlineMode = inlineMode;
        _language = string.IsNullOrWhiteSpace(language) ? "en-US" : language;
        _flowDirection = flowDirection;
        _screenState = _screenNumber is { } initialScreen ? CreateScreenState(initialScreen) : null;
    }

    public ulong FrameIndex => _frameIndex;

    public uint? ActivePointerId => _activePointerId;

    public int? ScreenNumber => _screenState?.CurrentScreenNumber ?? _screenNumber;

    public bool IsRunning => _running;

    public WebFrame RenderCurrent() =>
        _screenState is { } screenState
            ? RenderScreenState(screenState)
            : ShowcasePage.RenderScenario(
                _scenarioId,
                checked((int)Math.Min(_frameIndex, int.MaxValue)),
                _inlineMode,
                _width,
                _height,
                _language,
                _flowDirection);

    public ShowcaseRunnerStepResult Step()
    {
        var eventsProcessed = _pendingEventsProcessed;
        ApplyPendingScreenEvents();
        ApplyPendingClockTick();
        _pendingEventsProcessed = 0;
        if (!_running)
        {
            return new ShowcaseRunnerStepResult(false, false, eventsProcessed, _frameIndex, RenderCurrent());
        }

        _frameIndex++;
        var frame = RenderCurrent();
        _patchStats = ComputePatchStats(frame);
        return new ShowcaseRunnerStepResult(true, true, eventsProcessed, _frameIndex, frame);
    }

    public void Resize(ushort width, ushort height)
    {
        _width = ClampDimension(width);
        _height = ClampDimension(height);
        _pendingEventsProcessed++;
        if (_screenState is { } screenState)
        {
            _screenState = screenState with { Viewport = new Size(_width, _height) };
            _screenNumber = _screenState.CurrentScreenNumber;
        }
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
                _screenNumber = null;
                _screenState = null;
                _pendingScreenEvents.Clear();
                _pendingEventsProcessed++;
                return true;
            }

            if (TryGetIntProperty(document.RootElement, "screen", out var screenNumber))
            {
                SelectScreen(screenNumber);
                _pendingEventsProcessed++;
                return true;
            }

            if (TryGetStringProperty(document.RootElement, "screen", out var screenText) &&
                int.TryParse(screenText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedScreen))
            {
                SelectScreen(parsedScreen);
                _pendingEventsProcessed++;
                return true;
            }

            if (TryGetStringProperty(document.RootElement, "kind", out var kind) &&
                string.Equals(kind, "quit", StringComparison.OrdinalIgnoreCase))
            {
                _running = false;
                _pendingEventsProcessed++;
                return true;
            }

            if (TryHandleScreenKeyInput(document.RootElement, kind) ||
                TryHandleScreenMouseInput(document.RootElement, kind))
            {
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

    public ShowcaseRunnerPaneDispatch PanePointerLeave(uint pointerId)
    {
        if (_activePointerId is null)
            return Record(ShowcaseRunnerPanePhase.PointerLeave, pointerId, ShowcaseRunnerPaneCommand.None, ShowcaseRunnerPaneOutcome.Ignored, "no_active_pointer");
        if (_captureAcquired)
            return Record(ShowcaseRunnerPanePhase.PointerLeave, pointerId, ShowcaseRunnerPaneCommand.None, ShowcaseRunnerPaneOutcome.SemanticForwarded, "pointer_leave_after_capture");
        _activePointerId = null;
        _captureAcquired = false;
        return Record(ShowcaseRunnerPanePhase.PointerLeave, pointerId, ShowcaseRunnerPaneCommand.None, ShowcaseRunnerPaneOutcome.SemanticForwarded, "pointer_left_before_capture");
    }

    public ShowcaseRunnerPaneDispatch PaneBlur() =>
        InterruptActive(ShowcaseRunnerPanePhase.Blur, "blur", null, releaseOnlyWhenCaptured: true);

    public ShowcaseRunnerPaneDispatch PaneVisibilityHidden() =>
        InterruptActive(ShowcaseRunnerPanePhase.VisibilityHidden, "visibility_hidden", null, releaseOnlyWhenCaptured: true);

    public ShowcaseRunnerPaneDispatch PaneLostPointerCapture(uint pointerId) =>
        InterruptActive(ShowcaseRunnerPanePhase.LostPointerCapture, "lost_pointer_capture", pointerId, releaseOnlyWhenCaptured: false);

    public string PatchHash() => ComputeFnv1a64(RenderCurrent().Text);

    public ShowcaseRunnerPatchStats? PatchStats() => _patchStats;

    public ShowcaseRunnerFlatPatchBatch TakeFlatPatches()
    {
        PrepareFlatPatches();
        return new ShowcaseRunnerFlatPatchBatch(_flatCells.ToArray(), _flatSpans.ToArray());
    }

    public void PrepareFlatPatches()
    {
        var frame = RenderCurrent();
        (_flatCells, _flatSpans) = BuildFlatPatchBuffers(frame);
        _patchStats = new ShowcaseRunnerPatchStats(
            _flatCells.Length / 4, _flatSpans.Length / 2,
            checked((long)_flatCells.Length * sizeof(uint) + (long)_flatSpans.Length * sizeof(uint)));
    }

    public int FlatCellsPtr => _flatCells.Length == 0 ? 0 : Math.Abs(RuntimeHelpers.GetHashCode(_flatCells));
    public int FlatCellsLen => _flatCells.Length;
    public int FlatSpansPtr => _flatSpans.Length == 0 ? 0 : Math.Abs(RuntimeHelpers.GetHashCode(_flatSpans));
    public int FlatSpansLen => _flatSpans.Length;

    public void AdvanceTime(double deltaMilliseconds)
    {
        if (!double.IsFinite(deltaMilliseconds) || deltaMilliseconds < 0) return;
        _clock = _clock.AddMilliseconds(deltaMilliseconds);
        _clockDirty = true;
    }

    public void SetTime(double timestampNanoseconds)
    {
        if (!double.IsFinite(timestampNanoseconds) || timestampNanoseconds < 0) return;
        var ms = Math.Min(timestampNanoseconds / 1_000_000.0, TimeSpan.MaxValue.TotalMilliseconds);
        _clock = DateTimeOffset.UnixEpoch.AddMilliseconds(ms);
        _clockDirty = true;
    }

    public ulong PaneWorkspaceGeneration() => _paneWorkspaceGeneration;
    public ulong PaneSavedWorkspaceGeneration() => _paneSavedWorkspaceGeneration;
    public bool PaneWorkspaceDirty() => _paneWorkspaceGeneration != _paneSavedWorkspaceGeneration;

    public bool PaneMarkWorkspaceSaved(ulong generation)
    {
        if (generation > _paneWorkspaceGeneration) return false;
        _paneSavedWorkspaceGeneration = generation;
        return true;
    }

    public string? PaneExportWorkspaceSnapshot() => _paneWorkspaceSnapshot;

    public bool PaneImportWorkspaceSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        _paneWorkspaceSnapshot = json;
        _paneWorkspaceGeneration++;
        return true;
    }

    public bool PaneApplyLayoutMode(int mode, ulong primaryPaneId)
    {
        if (mode is < 0 or > 3 || primaryPaneId == 0) return false;
        _paneWorkspaceGeneration++;
        return true;
    }

    public void Destroy() { }

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
        var command = releaseOnlyWhenCaptured && _captureAcquired
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

    private WebFrame RenderScreenState(ShowcaseDemoState state)
    {
        var options = new WebRenderOptions(
            "FrankenTui Showcase",
            _language,
            _flowDirection == WidgetFlowDirection.RightToLeft ? "rtl" : "ltr",
            $"FrankenTui showcase screen {state.CurrentScreenNumber}",
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["showcase"] = "screen",
                ["screen-number"] = state.CurrentScreenNumber.ToString(CultureInfo.InvariantCulture),
                ["screen-mode"] = _inlineMode ? "inline" : "alt"
            });
        return WebHost.Render(ShowcaseSurface.Create(state), new Size(_width, _height), options: options);
    }

    private ShowcaseDemoState CreateScreenState(int screenNumber) =>
        ShowcaseDemoState.Create(_inlineMode, new Size(_width, _height), ClampScreenNumber(screenNumber), _language, _flowDirection);

    private void SelectScreen(int screenNumber)
    {
        _screenNumber = ClampScreenNumber(screenNumber);
        _screenState = CreateScreenState(_screenNumber.Value);
        _pendingScreenEvents.Clear();
    }

    private void ApplyPendingScreenEvents()
    {
        if (_screenState is null || _pendingScreenEvents.Count == 0)
        {
            _pendingScreenEvents.Clear();
            return;
        }
        foreach (var terminalEvent in _pendingScreenEvents)
            _screenState = _screenState.ApplyInput(Envelope(terminalEvent, terminalEvent.Timestamp), RuntimeFrameStats.Empty);
        _screenNumber = _screenState.CurrentScreenNumber;
        _pendingScreenEvents.Clear();
    }

    private void ApplyPendingClockTick()
    {
        if (!_clockDirty) return;
        if (_screenState is { } state)
        {
            _screenState = state.ApplyTick(_clock, RuntimeFrameStats.Empty);
            _screenNumber = _screenState.CurrentScreenNumber;
        }
        _clockDirty = false;
    }

    private static int ClampScreenNumber(int value) => Math.Clamp(value, 1, 45);

    private static bool TryGetIntProperty(JsonElement element, string name, out int value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Number &&
                property.Value.TryGetInt32(out value))
            {
                return true;
            }
        }
        value = 0;
        return false;
    }

    private static RuntimeInputEnvelope Envelope(TerminalEvent terminalEvent, DateTimeOffset timestamp) =>
        new(terminalEvent, terminalEvent, [], [], null, null, QuitRequested: false, HasWork: true, "web-runner", timestamp);

    private static string ComputeFnv1a64(string text)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= prime;
        }
        return $"fnv1a64:{hash:x16}";
    }

    private static ShowcaseRunnerPatchStats ComputePatchStats(WebFrame frame)
    {
        var dirtyCells = frame.Rows.Sum(static row => row.Length);
        var patchCount = frame.Rows.Count(static row => row.Length > 0);
        var bytesUploaded = checked((long)dirtyCells * 4 * sizeof(uint) + (long)patchCount * 2 * sizeof(uint));
        return new ShowcaseRunnerPatchStats(dirtyCells, patchCount, bytesUploaded);
    }

    private static (uint[] Cells, uint[] Spans) BuildFlatPatchBuffers(WebFrame frame)
    {
        var cells = new List<uint>();
        var spans = new List<uint>();
        foreach (var row in frame.Rows)
        {
            if (row.Length == 0) continue;
            var start = checked((uint)(cells.Count / 4));
            foreach (var ch in row)
            {
                cells.Add(ch); cells.Add(0); cells.Add(0); cells.Add(0);
            }
            spans.Add(start);
            spans.Add(checked((uint)row.Length));
        }
        return (cells.ToArray(), spans.ToArray());
    }

    private bool TryHandleScreenKeyInput(JsonElement element, string kind)
    {
        if (_screenState is null || !string.Equals(kind, "key", StringComparison.OrdinalIgnoreCase))
            return false;
        if (TryGetStringProperty(element, "phase", out var phase) &&
            string.Equals(phase, "up", StringComparison.OrdinalIgnoreCase))
        {
            var accepted = HasAnyProperty(element, "code", "key", "raw_key", "raw_code");
            if (accepted) _pendingEventsProcessed++;
            return accepted;
        }
        if (!TryParseKeyGesture(element, out var gesture))
            return false;
        QueueScreenTerminalEvent(TerminalEvent.Key(gesture));
        return true;
    }

    private bool TryHandleScreenMouseInput(JsonElement element, string kind)
    {
        if (_screenState is null ||
            (!string.Equals(kind, "mouse", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(kind, "wheel", StringComparison.OrdinalIgnoreCase)))
            return false;
        if (!TryParseMouseGesture(element, kind, out var gesture))
            return false;
        QueueScreenTerminalEvent(TerminalEvent.Mouse(gesture));
        return true;
    }

    private void QueueScreenTerminalEvent(TerminalEvent terminalEvent)
    {
        _pendingScreenEvents.Add(terminalEvent);
        _pendingEventsProcessed++;
    }

    private static bool TryParseKeyGesture(JsonElement element, out KeyGesture gesture)
    {
        gesture = default;
        if (!TryGetStringProperty(element, "key", out var key) &&
            !TryGetStringProperty(element, "code", out key) &&
            !TryGetStringProperty(element, "raw_key", out key) &&
            !TryGetStringProperty(element, "raw_code", out key))
            return false;
        _ = TryGetIntProperty(element, "mods", out var modifiers);
        var mods = (TerminalModifiers)(Math.Clamp(modifiers, 0, byte.MaxValue) & 0b111);
        var normalized = key.Trim();
        if (normalized.Length == 0) return false;
        gesture = normalized switch
        {
            "Enter" or "NumpadEnter" => new KeyGesture(TerminalKey.Enter, mods),
            "Escape" or "Esc" => new KeyGesture(TerminalKey.Escape, mods),
            "Backspace" => new KeyGesture(TerminalKey.Backspace, mods),
            "Tab" => new KeyGesture(TerminalKey.Tab, mods),
            "BackTab" => new KeyGesture(TerminalKey.Tab, mods | TerminalModifiers.Shift),
            "Delete" => new KeyGesture(TerminalKey.Delete, mods),
            "Insert" => new KeyGesture(TerminalKey.Insert, mods),
            "Home" => new KeyGesture(TerminalKey.Home, mods),
            "End" => new KeyGesture(TerminalKey.End, mods),
            "PageUp" => new KeyGesture(TerminalKey.PageUp, mods),
            "PageDown" => new KeyGesture(TerminalKey.PageDown, mods),
            "Up" or "ArrowUp" => new KeyGesture(TerminalKey.Up, mods),
            "Down" or "ArrowDown" => new KeyGesture(TerminalKey.Down, mods),
            "Left" or "ArrowLeft" => new KeyGesture(TerminalKey.Left, mods),
            "Right" or "ArrowRight" => new KeyGesture(TerminalKey.Right, mods),
            "Space" or "Spacebar" => new KeyGesture(TerminalKey.Character, mods, new Rune(' ')),
            _ => CreateCharacterGesture(normalized, mods)
        };
        return true;
    }

    private static KeyGesture CreateCharacterGesture(string value, TerminalModifiers mods)
    {
        if (value.Length == 4 && value.StartsWith("Key", StringComparison.OrdinalIgnoreCase))
        {
            var ch = value[3];
            ch = mods.HasFlag(TerminalModifiers.Shift) ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch);
            return new KeyGesture(TerminalKey.Character, mods, new Rune(ch));
        }
        if (value.Length == 6 && value.StartsWith("Digit", StringComparison.OrdinalIgnoreCase))
            return new KeyGesture(TerminalKey.Character, mods, new Rune(value[5]));
        var enumerator = value.EnumerateRunes();
        if (enumerator.MoveNext())
        {
            var rune = enumerator.Current;
            if (!enumerator.MoveNext())
                return new KeyGesture(TerminalKey.Character, mods, rune);
        }
        return new KeyGesture(TerminalKey.Character, mods, new Rune('\0'));
    }

    private static bool TryParseMouseGesture(JsonElement element, string kind, out MouseGesture gesture)
    {
        gesture = default;
        _ = TryGetIntProperty(element, "x", out var xValue);
        _ = TryGetIntProperty(element, "y", out var yValue);
        var column = (ushort)Math.Clamp(xValue, 0, ushort.MaxValue);
        var row = (ushort)Math.Clamp(yValue, 0, ushort.MaxValue);
        _ = TryGetIntProperty(element, "mods", out var modifierBits);
        var mods = (TerminalModifiers)(Math.Clamp(modifierBits, 0, byte.MaxValue) & 0b111);
        if (string.Equals(kind, "wheel", StringComparison.OrdinalIgnoreCase))
        {
            _ = TryGetIntProperty(element, "dy", out var dy);
            _ = TryGetIntProperty(element, "dx", out var dx);
            if (dy == 0) dy = dx;
            if (dy == 0) return false;
            var wheelButton = dy < 0 ? TerminalMouseButton.WheelUp : TerminalMouseButton.WheelDown;
            gesture = new MouseGesture(column, row, wheelButton, TerminalMouseKind.Scroll, mods);
            return true;
        }
        var phase = TryGetStringProperty(element, "phase", out var phaseValue) ? phaseValue : "down";
        _ = TryGetIntProperty(element, "button", out var buttonValue);
        var button = buttonValue switch { 1 => TerminalMouseButton.Middle, 2 => TerminalMouseButton.Right, _ => TerminalMouseButton.Left };
        var mouseKind = phase.ToLowerInvariant() switch
        {
            "down" => TerminalMouseKind.Down,
            "up" => TerminalMouseKind.Up,
            "drag" => TerminalMouseKind.Drag,
            "move" => TerminalMouseKind.Move,
            _ => (TerminalMouseKind?)null
        };
        if (mouseKind is null) return false;
        gesture = new MouseGesture(column, row, mouseKind == TerminalMouseKind.Move ? TerminalMouseButton.None : button, mouseKind.Value, mods);
        return true;
    }

    private static bool HasAnyProperty(JsonElement element, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
            foreach (var name in names)
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
        return false;
    }
}
