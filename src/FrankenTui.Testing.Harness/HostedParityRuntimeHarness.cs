using System.Text.Json;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Widgets;

namespace FrankenTui.Testing.Harness;

public static class HostedParityRuntimeHarness
{
    public static async Task<HostedParityRuntimeCapture> CaptureAsync(
        string name,
        HostedParityScenarioId scenarioId,
        ushort width = 72,
        ushort height = 18,
        bool inlineMode = false,
        IReadOnlyList<TerminalEvent>? events = null,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight,
        Theme? theme = null,
        RuntimeExecutionPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var effectiveSize = new Size(width, height);
        var capabilities = inlineMode ? TerminalCapabilities.Tmux() : TerminalCapabilities.Modern();
        var eventScript = TerminalEventCoalescer.Coalesce(events ?? HostedParitySession.DefaultScript()).ToArray();

        var backend = new MemoryTerminalBackend(effectiveSize, capabilities, inlineMode ? "memory-inline" : "memory");
        await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await backend.ConfigureSessionAsync(
            new TerminalSessionConfiguration
            {
                InlineMode = inlineMode
            },
            cancellationToken).ConfigureAwait(false);

        var runtime = new AppRuntime<HostedParitySession, HostedParityRuntimeMessage>(
            backend,
            effectiveSize,
            theme,
            policy);
        var session = new AppSession<HostedParitySession, HostedParityRuntimeMessage>(
            runtime,
            new HostedParityRuntimeProgram(inlineMode, scenarioId, language, flowDirection));
        var controller = new RuntimeInputController<HostedParitySession, HostedParityRuntimeMessage>(
            static model => model.CreateKeybindingState(),
            static (_, input) => [HostedParityRuntimeMessage.FromInput(input)],
            static (_, terminalEvent) => HostedParityInputEngine.Translate(terminalEvent));

        await session.DispatchAsync(HostedParityRuntimeMessage.Initial(), cancellationToken).ConfigureAwait(false);
        foreach (var terminalEvent in eventScript)
        {
            await controller.ProcessAsync(session, terminalEvent, cancellationToken).ConfigureAwait(false);
        }

        await controller.TickAsync(
            session,
            eventScript.Length == 0
                ? DateTimeOffset.UtcNow
                : eventScript[^1].Timestamp + TimeSpan.FromMilliseconds(1000),
            cancellationToken).ConfigureAwait(false);

        var finalSession = session.Model;
        if (runtime.Policy.EmitTelemetry &&
            finalSession.Macro.Macro is { } macro)
        {
            runtime.Telemetry.RecordMacro(
                finalSession.StepCount,
                macro.Id,
                macro.Events.Count,
                finalSession.Macro.LastDriftMs);
        }

        var finalSize = runtime.Size;
        var evidence = RenderHarness.CaptureHostedParity(
            name,
            HostedParitySurface.Create(finalSession),
            finalSize.Width,
            finalSize.Height,
            theme,
            HostedParitySurface.CreateWebOptions(finalSession));

        return new HostedParityRuntimeCapture(
            name,
            evidence,
            runtime.Replay,
            runtime.Trace,
            runtime.Telemetry,
            runtime.DiffEvidence,
            runtime.FrameStats,
            eventScript,
            backend.DrainOutput());
    }

    private sealed class HostedParityRuntimeProgram : IAppProgram<HostedParitySession, HostedParityRuntimeMessage>
    {
        private readonly bool _inlineMode;
        private readonly HostedParityScenarioId _scenarioId;
        private readonly string _language;
        private readonly WidgetFlowDirection _flowDirection;

        public HostedParityRuntimeProgram(
            bool inlineMode,
            HostedParityScenarioId scenarioId,
            string language,
            WidgetFlowDirection flowDirection)
        {
            _inlineMode = inlineMode;
            _scenarioId = scenarioId;
            _language = language;
            _flowDirection = flowDirection;
        }

        public HostedParitySession Initialize() =>
            HostedParitySession.Create(_inlineMode, _scenarioId, _language, _flowDirection);

        public UpdateResult<HostedParitySession, HostedParityRuntimeMessage> Update(
            HostedParitySession model,
            HostedParityRuntimeMessage message) =>
            message.Input is null
                ? UpdateResult<HostedParitySession, HostedParityRuntimeMessage>.FromModel(model)
                : UpdateResult<HostedParitySession, HostedParityRuntimeMessage>.FromModel(model.Advance(message.Input));

        public IRuntimeView BuildView(HostedParitySession model) =>
            HostedParitySurface.Create(model);
    }

    public sealed record HostedParityRuntimeMessage(string Label, RuntimeInputEnvelope? Input)
    {
        public static HostedParityRuntimeMessage Initial() => new("init", null);

        public static HostedParityRuntimeMessage FromInput(RuntimeInputEnvelope input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return new HostedParityRuntimeMessage(input.Label, input);
        }
    }
}

public sealed record HostedParityRuntimeCapture(
    string Name,
    HostedParityEvidence Evidence,
    ReplayTape<HostedParityRuntimeHarness.HostedParityRuntimeMessage> ReplayTape,
    RuntimeTrace<HostedParityRuntimeHarness.HostedParityRuntimeMessage> Trace,
    TelemetrySessionLog Telemetry,
    DiffEvidenceLedger DiffEvidence,
    RuntimeFrameStats FrameStats,
    IReadOnlyList<TerminalEvent> Events,
    string TerminalTranscript)
{
    public IReadOnlyDictionary<string, string> WriteArtifacts(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var artifacts = new Dictionary<string, string>(Evidence.WriteArtifacts(prefix), StringComparer.Ordinal);

        var replayPath = ArtifactPathBuilder.For("replay", $"{prefix}-runtime-replay.json");
        File.WriteAllText(replayPath, ReplayTape.ToJson());
        artifacts["replay_tape"] = replayPath;

        var tracePath = ArtifactPathBuilder.For("replay", $"{prefix}-runtime-trace.json");
        File.WriteAllText(tracePath, Trace.ToJson());
        artifacts["runtime_trace"] = tracePath;

        var telemetryPath = ArtifactPathBuilder.For("replay", $"{prefix}-telemetry.json");
        File.WriteAllText(telemetryPath, Telemetry.ToJson());
        artifacts["telemetry"] = telemetryPath;

        var diffPath = ArtifactPathBuilder.For("replay", $"{prefix}-diff-evidence.json");
        File.WriteAllText(diffPath, DiffEvidence.ToJson());
        artifacts["diff_evidence"] = diffPath;

        var transcriptPath = ArtifactPathBuilder.For("replay", $"{prefix}-terminal-transcript.txt");
        File.WriteAllText(transcriptPath, TerminalTranscript);
        artifacts["terminal_transcript"] = transcriptPath;

        var eventsPath = ArtifactPathBuilder.For("replay", $"{prefix}-event-script.json");
        File.WriteAllText(eventsPath, SerializeEvents(Events));
        artifacts["event_script"] = eventsPath;

        return artifacts;
    }

    private static string SerializeEvents(IReadOnlyList<TerminalEvent> events) =>
        JsonSerializer.Serialize(
            events.Select(static terminalEvent => terminalEvent switch
            {
                KeyTerminalEvent keyEvent => (object)new
                {
                    type = "key",
                    key = keyEvent.Gesture.Key.ToString(),
                    text = keyEvent.Gesture.Character?.ToString(),
                    modifiers = keyEvent.Gesture.Modifiers.ToString()
                },
                MouseTerminalEvent mouseEvent => new
                {
                    type = "mouse",
                    kind = mouseEvent.Gesture.Kind.ToString(),
                    mouseEvent.Gesture.Column,
                    mouseEvent.Gesture.Row,
                    button = mouseEvent.Gesture.Button.ToString()
                },
                HoverTerminalEvent hoverEvent => new
                {
                    type = "hover",
                    hoverEvent.Column,
                    hoverEvent.Row,
                    hoverEvent.Stable
                },
                PasteTerminalEvent pasteEvent => new
                {
                    type = "paste",
                    pasteEvent.Text
                },
                FocusTerminalEvent focusEvent => new
                {
                    type = "focus",
                    focusEvent.Focused
                },
                ResizeTerminalEvent resizeEvent => new
                {
                    type = "resize",
                    width = resizeEvent.Size.Width,
                    height = resizeEvent.Size.Height
                },
                _ => new
                {
                    type = terminalEvent.GetType().Name
                }
            }),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });
}
