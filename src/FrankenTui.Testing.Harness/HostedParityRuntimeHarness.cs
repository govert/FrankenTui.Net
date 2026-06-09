// SPDX-License-Identifier: Apache-2.0
// Headless capture harness for HostedParitySession scenarios.
// Produces ReplayTape, RuntimeTrace, DiffEvidenceLedger, and artifact files
// mirroring what a real hosted-parity runtime session would generate.

using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Web;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Testing.Harness;

/// <summary>
/// Result of a headless hosted-parity capture run.
/// Holds all artifacts produced by simulating one initial frame plus
/// zero or more input events through a <see cref="HostedParitySession"/>.
/// </summary>
public sealed class HostedParityCapture
{
    internal HostedParityCapture(
        HostedParityEvidence evidence,
        ReplayTape<object> replayTape,
        RuntimeTrace<object> trace,
        DiffEvidenceLedger diffEvidence,
        IReadOnlyList<TerminalEvent> events,
        string terminalTranscript)
    {
        Evidence = evidence;
        ReplayTape = replayTape;
        Trace = trace;
        DiffEvidence = diffEvidence;
        Events = events;
        TerminalTranscript = terminalTranscript;
    }

    /// <summary>Terminal + web snapshot evidence.</summary>
    public HostedParityEvidence Evidence { get; }

    /// <summary>Replay tape with one entry per rendered frame.</summary>
    public ReplayTape<object> ReplayTape { get; }

    /// <summary>Runtime trace with one entry per rendered frame.</summary>
    public RuntimeTrace<object> Trace { get; }

    /// <summary>Diff evidence ledger recording one decision per frame.</summary>
    public DiffEvidenceLedger DiffEvidence { get; }

    /// <summary>The input events that were replayed (may be empty).</summary>
    public IReadOnlyList<TerminalEvent> Events { get; }

    /// <summary>Plain-text terminal transcript of the final frame.</summary>
    public string TerminalTranscript { get; }

    /// <summary>
    /// Write all capture artifacts to the artifacts directory and return a
    /// dictionary mapping artifact keys to their absolute file paths.
    /// </summary>
    public IReadOnlyDictionary<string, string> WriteArtifacts(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var paths = new Dictionary<string, string>(StringComparer.Ordinal);

        var replayTapePath = ArtifactPathBuilder.For("replay", $"{name}-replay-tape.json");
        File.WriteAllText(replayTapePath, ReplayTape.ToJson());
        paths["replay_tape"] = replayTapePath;

        var tracePath = ArtifactPathBuilder.For("replay", $"{name}-runtime-trace.json");
        File.WriteAllText(tracePath, Trace.ToJson());
        paths["runtime_trace"] = tracePath;

        var diffPath = ArtifactPathBuilder.For("replay", $"{name}-diff-evidence.json");
        File.WriteAllText(diffPath, System.Text.Json.JsonSerializer.Serialize(DiffEvidence.Decisions));
        paths["diff_evidence"] = diffPath;

        var eventScriptPath = ArtifactPathBuilder.For("replay", $"{name}-event-script.json");
        File.WriteAllText(eventScriptPath, System.Text.Json.JsonSerializer.Serialize(Events.Select(static e => e.GetType().Name).ToArray()));
        paths["event_script"] = eventScriptPath;

        var transcriptPath = ArtifactPathBuilder.For("replay", $"{name}-transcript.txt");
        File.WriteAllText(transcriptPath, TerminalTranscript);
        paths["terminal_transcript"] = transcriptPath;

        return paths;
    }
}

/// <summary>
/// Static factory for headless hosted-parity runtime captures.
/// </summary>
public static class HostedParityRuntimeHarness
{
    /// <summary>
    /// Capture a headless hosted-parity session.
    /// Renders an initial frame then replays each supplied event,
    /// recording one replay-tape entry, one trace entry, and one diff-evidence
    /// decision for every frame rendered (events.Count + 1 total).
    /// </summary>
    public static Task<HostedParityCapture> CaptureAsync(
        string name,
        HostedParityScenarioId scenarioId,
        int width,
        int height,
        IReadOnlyList<TerminalEvent> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(events);

        var capture = RunCapture(name, scenarioId, (ushort)width, (ushort)height, events);
        return Task.FromResult(capture);
    }

    private static HostedParityCapture RunCapture(
        string name,
        HostedParityScenarioId scenarioId,
        ushort width,
        ushort height,
        IReadOnlyList<TerminalEvent> events)
    {
        var replayTape = new ReplayTape<object>();
        var trace = new RuntimeTrace<object>();
        var diffLedger = new DiffEvidenceLedger();

        var session = HostedParitySession.Create(inlineMode: false, scenarioId);

        // Render initial frame (step 0).
        var buffer = RenderFrame(session, width, height);
        var screenText = HeadlessBufferView.ScreenString(buffer);
        RecordFrame(replayTape, trace, diffLedger, 0, (object)session, screenText);

        // Replay each event (steps 1..N).
        for (var i = 0; i < events.Count; i++)
        {
            session = session.Advance(events[i]);
            buffer = RenderFrame(session, width, height);
            screenText = HeadlessBufferView.ScreenString(buffer);
            RecordFrame(replayTape, trace, diffLedger, i + 1, (object)session, screenText);
        }

        // Build evidence.
        var widget = HostedParitySurface.Create(session);
        var view = new WidgetView(widget, width, height);
        var webOptions = HostedParitySurface.CreateWebOptions(session);
        var terminal = RenderHarness.Render(view, width, height);
        var web = WebHost.Render(view, new Size(width, height), options: webOptions);
        var evidence = HostedParityEvidence.Create(name, terminal, web);
        var transcript = terminal.Text;

        return new HostedParityCapture(evidence, replayTape, trace, diffLedger, events, transcript);
    }

    private static RenderBuffer RenderFrame(HostedParitySession session, ushort width, ushort height)
    {
        var buffer = new RenderBuffer(width, height);
        var widget = HostedParitySurface.Create(session);
        var pool = new GraphemePool();
        var frame = new Frame(width, height, pool);
        frame.BufferOverride = buffer;
        widget.Render(new Rect(0, 0, width, height), frame);
        return buffer;
    }

    private static void RecordFrame(
        ReplayTape<object> replayTape,
        RuntimeTrace<object> trace,
        DiffEvidenceLedger diffLedger,
        int stepIndex,
        object message,
        string screenText)
    {
        replayTape.Add(stepIndex, message, [], screenText, string.Empty);
        trace.Record(stepIndex, message, [], screenText, string.Empty);
        diffLedger.RecordDecision(new DiffDecisionRecord(DiffStrategy.Full, DiffRegime.StableFrame, stepIndex));
    }

    /// <summary>Adapter: renders an <see cref="IWidget"/> as an <see cref="IRuntimeView"/>.</summary>
    private sealed class WidgetView(FrankenTui.Widgets.IWidget widget, ushort width, ushort height) : IRuntimeView
    {
        public void Render(RuntimeRenderContext context)
        {
            var pool = new GraphemePool();
            var frame = new Frame(width, height, pool);
            frame.BufferOverride = context.Buffer;
            widget.Render(context.Bounds, frame);
        }
    }
}
