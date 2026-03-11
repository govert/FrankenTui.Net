using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using System.Diagnostics;
using System.Globalization;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Runtime;

public sealed class AppRuntime<TModel, TMessage>
{
    private readonly ITerminalBackend _backend;
    private readonly DiffStrategySelector _diffSelector = new();
    private RenderBuffer _current;
    private RenderBuffer _next;
    private TimeSpan _lastPresentLatency;
    private bool _resizePending;
    private int _stepIndex;

    public AppRuntime(
        ITerminalBackend backend,
        Size size,
        Theme? theme = null,
        RuntimeExecutionPolicy? policy = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        var effectiveSize = size.IsEmpty ? new Size(1, 1) : size;
        Size = effectiveSize;
        _current = new RenderBuffer(effectiveSize.Width, effectiveSize.Height);
        _next = new RenderBuffer(effectiveSize.Width, effectiveSize.Height);
        Theme = theme ?? Theme.DefaultTheme;
        Policy = policy ?? RuntimeExecutionPolicy.Default;
        Telemetry = new TelemetrySessionLog(Policy.Telemetry ?? TelemetryConfig.Disabled);
    }

    public Size Size { get; private set; }

    public Theme Theme { get; }

    public RuntimeExecutionPolicy Policy { get; }

    public TelemetrySessionLog Telemetry { get; }

    public RuntimeTrace<TMessage> Trace { get; } = new();

    public ReplayTape<TMessage> Replay { get; } = new();

    public DiffEvidenceLedger DiffEvidence => _diffSelector.Ledger;

    public TimeSpan LastPresentLatency => _lastPresentLatency;

    public async ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        IAppProgram<TModel, TMessage> program,
        TModel model,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var updateStopwatch = Stopwatch.StartNew();
        var update = program.Update(model, message);
        updateStopwatch.Stop();
        var presentation = await RenderAsync(program.BuildView(update.Model), cancellationToken).ConfigureAwait(false);
        var emitted = CollectMessages(update.Commands, update.Subscriptions);
        var screenText = HeadlessBufferView.ScreenString(_current);
        RuntimeTraceEntry<TMessage>? traceEntry = null;
        if (Policy.CaptureTrace)
        {
            Trace.Record(_stepIndex, message, emitted, screenText, presentation.Output);
            traceEntry = Trace.Entries[^1];
        }

        if (Policy.CaptureReplayTape)
        {
            Replay.Add(_stepIndex, message, emitted, screenText, presentation.Output);
        }

        if (Policy.EmitTelemetry)
        {
            Telemetry.Record(
                "ftui.program.update",
                TelemetryEventCategory.RuntimePhase,
                _stepIndex,
                [
                    new TelemetryField("cmd_count", update.Commands.Messages.Count.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("duration_us", ToMicroseconds(updateStopwatch.Elapsed).ToString(CultureInfo.InvariantCulture)),
                    TelemetryRedactor.TypeField("model_type", typeof(TModel), Telemetry.Config.Verbose),
                    TelemetryRedactor.TypeField("msg_type", message?.GetType(), Telemetry.Config.Verbose),
                    new TelemetryField("subscription_count", update.Subscriptions.Count.ToString(CultureInfo.InvariantCulture))
                ]);
        }

        _stepIndex++;
        return new RuntimeStepResult<TModel, TMessage>(
            update.Model,
            presentation,
            screenText,
            emitted,
            traceEntry);
    }

    public async ValueTask<PresentResult> RenderAsync(
        IRuntimeView view,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<uint, string>? links = null)
    {
        ArgumentNullException.ThrowIfNull(view);

        var frameStopwatch = Stopwatch.StartNew();
        _next.Clear();
        var viewStopwatch = Stopwatch.StartNew();
        view.Render(new RuntimeRenderContext(_next, Rect.FromSize(_next.Width, _next.Height), Theme));
        viewStopwatch.Stop();
        var resized = _resizePending || _current.Width != _next.Width || _current.Height != _next.Height;
        var dirtyRows = resized ? _next.Height : BufferDiff.CountDirtyRows(_current, _next);
        var diffStopwatch = Stopwatch.StartNew();
        var selection = _diffSelector.Select(_next.Width, _next.Height, dirtyRows, resized, _lastPresentLatency);
        var diff = selection.Strategy switch
        {
            DiffStrategy.Full => BufferDiff.ComputeFull(_current, _next),
            DiffStrategy.FullRedraw => BufferDiff.Full(_next.Width, _next.Height),
            DiffStrategy.SignificantDirtyRows => BufferDiff.ComputeSignificantDirty(_current, _next),
            _ => BufferDiff.ComputeDirty(_current, _next)
        };
        diffStopwatch.Stop();

        var presentStopwatch = Stopwatch.StartNew();
        var result = await _backend.PresentAsync(_next, diff, links, cancellationToken).ConfigureAwait(false);
        presentStopwatch.Stop();
        frameStopwatch.Stop();

        _lastPresentLatency = presentStopwatch.Elapsed;
        _diffSelector.Observe(selection, diff.Count, _lastPresentLatency);
        _current.CopyFrom(_next);
        _resizePending = false;

        if (Policy.EmitTelemetry)
        {
            Telemetry.Record(
                "ftui.render.diff",
                TelemetryEventCategory.RenderPipeline,
                _stepIndex,
                [
                    new TelemetryField("changes_count", diff.Count.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("duration_us", ToMicroseconds(diffStopwatch.Elapsed).ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("rows_skipped", Math.Max(_next.Height - dirtyRows, 0).ToString(CultureInfo.InvariantCulture))
                ]);
            Telemetry.Record(
                "ftui.render.present",
                TelemetryEventCategory.RenderPipeline,
                _stepIndex,
                [
                    new TelemetryField("bytes_written", result.Output.Length.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("duration_us", ToMicroseconds(presentStopwatch.Elapsed).ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("runs_count", diff.Runs().Count.ToString(CultureInfo.InvariantCulture))
                ]);
            Telemetry.Record(
                "ftui.render.frame",
                TelemetryEventCategory.RenderPipeline,
                _stepIndex,
                [
                    new TelemetryField("duration_us", ToMicroseconds(frameStopwatch.Elapsed).ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("height", _next.Height.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("width", _next.Width.ToString(CultureInfo.InvariantCulture))
                ]);

            if (selection.TransitionReason is not null || selection.Regime is not DiffRegime.StableFrame)
            {
                Telemetry.Record(
                    "ftui.decision.fallback",
                    TelemetryEventCategory.Decision,
                    _stepIndex,
                    [
                        new TelemetryField("capability", "diff_strategy"),
                        new TelemetryField("fallback_to", selection.Strategy.ToString().ToLowerInvariant()),
                        new TelemetryField("reason", selection.TransitionReason ?? selection.Regime.ToString().ToLowerInvariant())
                    ],
                    new TelemetryDecisionEvidence(
                        "diff-regime-selector",
                        $"dirty_rows={selection.DirtyRows};total_cells={selection.TotalCells}",
                        selection.Strategy.ToString().ToLowerInvariant(),
                        selection.Confidence,
                        [DiffStrategy.DirtyRows.ToString().ToLowerInvariant(), DiffStrategy.Full.ToString().ToLowerInvariant()],
                        $"Diff regime {selection.Regime.ToString().ToLowerInvariant()} selected {selection.Strategy.ToString().ToLowerInvariant()}."));
            }
        }

        return result;
    }

    public async ValueTask ResizeAsync(Size size, CancellationToken cancellationToken = default)
    {
        var effectiveSize = size.IsEmpty ? new Size(1, 1) : size;
        if (effectiveSize == Size)
        {
            return;
        }

        await _backend.ResizeAsync(effectiveSize, cancellationToken).ConfigureAwait(false);
        Size = effectiveSize;
        _current = new RenderBuffer(effectiveSize.Width, effectiveSize.Height);
        _next = new RenderBuffer(effectiveSize.Width, effectiveSize.Height);
        _resizePending = true;

        if (Policy.EmitTelemetry)
        {
            Telemetry.Record(
                "ftui.decision.resize",
                TelemetryEventCategory.Decision,
                _stepIndex,
                [
                    new TelemetryField("coalesced", "false"),
                    new TelemetryField("height", effectiveSize.Height.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("same_size", "false"),
                    new TelemetryField("strategy", "immediate"),
                    new TelemetryField("width", effectiveSize.Width.ToString(CultureInfo.InvariantCulture))
                ],
                new TelemetryDecisionEvidence(
                    "runtime.resize",
                    $"size={effectiveSize.Width}x{effectiveSize.Height}",
                    "resize",
                    1.0,
                    ["ignore"],
                    "Resize applied directly through the runtime backend."));
        }
    }

    private IReadOnlyList<TMessage> CollectMessages(
        AppCommand<TMessage> commands,
        IReadOnlyList<Subscription<TMessage>> subscriptions)
    {
        var messages = new List<TMessage>(commands.Messages);
        foreach (var subscription in subscriptions)
        {
            messages.AddRange(subscription.Invoke());
        }

        return messages;
    }

    private static long ToMicroseconds(TimeSpan duration) =>
        (long)Math.Round(duration.TotalMilliseconds * 1000.0, MidpointRounding.AwayFromZero);
}
