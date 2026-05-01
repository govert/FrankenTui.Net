using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
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
    private RuntimeFrameStats _frameStats = RuntimeFrameStats.Empty;
    private HashSet<string> _activeSubscriptionKeys = [];
    private readonly RuntimeLoadGovernor _loadGovernor;
    private readonly RuntimeDegradationCascade _degradationCascade;
    private readonly bool _cascadeRenderGateEnabled;
    private DiffStrategy _lastDiffStrategy = DiffStrategy.Full;

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
        _loadGovernor = new RuntimeLoadGovernor(Policy.EffectiveLoadGovernor);
        _degradationCascade = new RuntimeDegradationCascade(Policy.EffectiveDegradationCascade);
        _cascadeRenderGateEnabled = Policy.PolicyConfig is not null;
    }

    public Size Size { get; private set; }

    public Theme Theme { get; }

    public RuntimeExecutionPolicy Policy { get; }

    public TelemetrySessionLog Telemetry { get; }

    public RuntimeTrace<TMessage> Trace { get; } = new();

    public ReplayTape<TMessage> Replay { get; } = new();

    public DiffEvidenceLedger DiffEvidence => _diffSelector.Ledger;

    public TimeSpan LastPresentLatency => _lastPresentLatency;

    public int CurrentStepIndex => _stepIndex;

    public RuntimeFrameStats FrameStats => _frameStats;

    public QueueTelemetry QueueTelemetry => EffectSystem.SnapshotQueueTelemetry();

    public RuntimeDynamics RuntimeDynamics => EffectSystem.SnapshotRuntimeDynamics();

    public async ValueTask<RuntimeStepResult<TModel, TMessage>> DispatchAsync(
        IAppProgram<TModel, TMessage> program,
        TModel model,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var updateStopwatch = Stopwatch.StartNew();
        var update = program.Update(model, message);
        updateStopwatch.Stop();
        var buildViewStopwatch = Stopwatch.StartNew();
        var view = program.BuildView(update.Model);
        buildViewStopwatch.Stop();
        var presentation = await RenderAsync(view, cancellationToken).ConfigureAwait(false);
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
            var dynamics = EffectSystem.SnapshotRuntimeDynamics();
            var queue = EffectSystem.SnapshotQueueTelemetry();
            Telemetry.Record(
                "ftui.program.update",
                TelemetryEventCategory.RuntimePhase,
                _stepIndex,
                [
                    new TelemetryField("cmd_count", update.Commands.Messages.Count.ToString(CultureInfo.InvariantCulture)),
                    TelemetryRedactor.TypeField("cmd_type", typeof(TMessage), Telemetry.Config.Verbose),
                    new TelemetryField("duration_us", ToMicroseconds(updateStopwatch.Elapsed).ToString(CultureInfo.InvariantCulture)),
                    TelemetryRedactor.TypeField("model_type", typeof(TModel), Telemetry.Config.Verbose),
                    TelemetryRedactor.TypeField("msg_type", message?.GetType(), Telemetry.Config.Verbose),
                    new TelemetryField("subscription_count", update.Subscriptions.Count.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("command_effects_total", dynamics.CommandEffects.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("command_cancellations_total", dynamics.CommandCancellations.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("command_failures_total", dynamics.CommandFailures.ToString(CultureInfo.InvariantCulture))
                ]);
            Telemetry.Record(
                "ftui.program.view",
                TelemetryEventCategory.RuntimePhase,
                _stepIndex,
                [
                    new TelemetryField("duration_us", ToMicroseconds(buildViewStopwatch.Elapsed).ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("widget_count", "1")
                ]);
            Telemetry.Record(
                "ftui.program.subscriptions",
                TelemetryEventCategory.RuntimePhase,
                _stepIndex,
                [
                    new TelemetryField("active_count", update.Subscriptions.Count.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("started", dynamics.SubscriptionStarts.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("stopped", dynamics.SubscriptionStops.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("subscription_effects_total", dynamics.SubscriptionEffects.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("subscription_cancellations_total", dynamics.SubscriptionCancellations.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("subscription_failures_total", dynamics.SubscriptionFailures.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("subscription_messages_total", dynamics.SubscriptionMessages.ToString(CultureInfo.InvariantCulture))
                ]);
            Telemetry.Record(
                "ftui.effect.queue",
                TelemetryEventCategory.RuntimePhase,
                _stepIndex,
                [
                    new TelemetryField("enqueued_total", queue.Enqueued.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("processed_total", queue.Processed.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("dropped_total", queue.Dropped.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("high_water", queue.HighWater.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("in_flight", queue.InFlight.ToString(CultureInfo.InvariantCulture))
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
        var cascadeKey = RuntimeConformalBucketKey.FromContext(
            inlineMode: false,
            inlineAuto: false,
            _lastDiffStrategy,
            (ushort)Math.Clamp((int)Size.Width, 0, (int)ushort.MaxValue),
            (ushort)Math.Clamp((int)Size.Height, 0, (int)ushort.MaxValue));
        var cascadeEvidence = _degradationCascade.PreRender(
            Policy.EffectiveLoadGovernor.EffectiveBudgetController.TargetFrameTime,
            cascadeKey);

        var renderLevel = _cascadeRenderGateEnabled ? cascadeEvidence.LevelAfter : RuntimeDegradationLevel.Full;
        if (renderLevel == RuntimeDegradationLevel.SkipFrame)
        {
            frameStopwatch.Stop();
            var skipped = new PresentResult(string.Empty, 0, 0, 0, UsedSyncOutput: false, Truncated: false);
            var skippedLoadDecision = _loadGovernor.Observe(TimeSpan.Zero);
            _frameStats = CreateFrameStats(
                skipped,
                frameStopwatch.Elapsed,
                presentDuration: TimeSpan.Zero,
                diffDuration: TimeSpan.Zero,
                dirtyRows: 0,
                skippedLoadDecision,
                cascadeEvidence,
                cascadeKey);
            RecordRenderTelemetry(
                skipped,
                frameStopwatch.Elapsed,
                presentDuration: TimeSpan.Zero,
                diffDuration: TimeSpan.Zero,
                dirtyRows: 0,
                skippedLoadDecision,
                cascadeEvidence,
                cascadeKey,
                selection: null);
            return skipped;
        }

        _next.Clear();
        var viewStopwatch = Stopwatch.StartNew();
        view.Render(new RuntimeRenderContext(
            _next,
            Rect.FromSize(_next.Width, _next.Height),
            Theme,
                renderLevel));
        viewStopwatch.Stop();
        var resized = _resizePending || _current.Width != _next.Width || _current.Height != _next.Height;
        var dirtyRows = resized ? _next.Height : BufferDiff.CountDirtyRows(_current, _next);
        var diffStopwatch = Stopwatch.StartNew();
        var selection = _diffSelector.Select(_next.Width, _next.Height, dirtyRows, resized, _lastPresentLatency);
        var certifiedHint = CreateDiffSkipHint(selection, resized, dirtyRows);
        var diff = selection.Strategy switch
        {
            DiffStrategy.Full => BufferDiff.ComputeFull(_current, _next),
            DiffStrategy.FullRedraw => BufferDiff.Full(_next.Width, _next.Height),
            DiffStrategy.SignificantDirtyRows => BufferDiff.ComputeSignificantDirty(_current, _next),
            _ => BufferDiff.ComputeCertified(_current, _next, certifiedHint)
        };
        diffStopwatch.Stop();

        var presentStopwatch = Stopwatch.StartNew();
        var result = await _backend.PresentAsync(_next, diff, links, cancellationToken).ConfigureAwait(false);
        presentStopwatch.Stop();
        frameStopwatch.Stop();

        _degradationCascade.Observe(frameStopwatch.Elapsed, cascadeKey);
        var loadDecision = _loadGovernor.Observe(frameStopwatch.Elapsed);
        _lastPresentLatency = presentStopwatch.Elapsed;
        _diffSelector.Observe(selection, diff.Count, _lastPresentLatency);
        _lastDiffStrategy = selection.Strategy;
        _current.CopyFrom(_next);
        _resizePending = false;
        _frameStats = CreateFrameStats(
            result,
            frameStopwatch.Elapsed,
            presentStopwatch.Elapsed,
            diffStopwatch.Elapsed,
            dirtyRows,
            loadDecision,
            cascadeEvidence,
            cascadeKey);

        RecordRenderTelemetry(
            result,
            frameStopwatch.Elapsed,
            presentStopwatch.Elapsed,
            diffStopwatch.Elapsed,
            dirtyRows,
            loadDecision,
            cascadeEvidence,
            cascadeKey,
            selection);

        return result;
    }

    private RuntimeFrameStats CreateFrameStats(
        PresentResult result,
        TimeSpan frameDuration,
        TimeSpan presentDuration,
        TimeSpan diffDuration,
        int dirtyRows,
        LoadGovernorDecision loadDecision,
        RuntimeCascadeEvidence cascadeEvidence,
        RuntimeConformalBucketKey cascadeKey)
    {
        var effectiveLevel = _cascadeRenderGateEnabled
            ? MoreDegraded(loadDecision.LevelAfter, cascadeEvidence.LevelAfter)
            : loadDecision.LevelAfter;
        return new RuntimeFrameStats(
            _stepIndex,
            result.ChangedCells,
            result.RunCount,
            result.ByteCount,
            frameDuration.TotalMilliseconds,
            presentDuration.TotalMilliseconds,
            diffDuration.TotalMilliseconds,
            dirtyRows,
            effectiveLevel.Label(),
            result.UsedSyncOutput,
            result.Truncated,
            LoadGovernorAction: loadDecision.Action,
            LoadGovernorReason: loadDecision.Reason,
            LoadGovernorPidOutput: loadDecision.PidOutput,
            LoadGovernorPidP: loadDecision.PidP,
            LoadGovernorPidI: loadDecision.PidI,
            LoadGovernorPidD: loadDecision.PidD,
            LoadGovernorEProcessValue: loadDecision.EProcessValue,
            LoadGovernorEProcessSigmaMs: loadDecision.EProcessSigmaMs,
            LoadGovernorFramesObserved: loadDecision.FramesObserved,
            LoadGovernorFramesSinceChange: loadDecision.FramesSinceChange,
            LoadGovernorPidGateThreshold: loadDecision.PidGateThreshold,
            LoadGovernorPidGateMargin: loadDecision.PidGateMargin,
            LoadGovernorEvidenceThreshold: loadDecision.EvidenceThreshold,
            LoadGovernorEvidenceMargin: loadDecision.EvidenceMargin,
            LoadGovernorEProcessInWarmup: loadDecision.EProcessInWarmup,
            LoadGovernorTransitionSeq: loadDecision.TransitionSeq,
            LoadGovernorTransitionCorrelationId: loadDecision.TransitionCorrelationId,
            CascadeDecision: RuntimeCascadeEvidence.DecisionLabel(cascadeEvidence.Decision),
            CascadeLevelBefore: cascadeEvidence.LevelBefore.Label(),
            CascadeLevelAfter: cascadeEvidence.LevelAfter.Label(),
            CascadeGuardState: RuntimeP99Prediction.StateLabel(cascadeEvidence.GuardState),
            ConformalBucketKey: cascadeKey.ToString(),
            ConformalUpperMicroseconds: cascadeEvidence.Prediction.UpperMicroseconds,
            ConformalBudgetMicroseconds: cascadeEvidence.BudgetMicroseconds,
            ConformalCalibrationSize: cascadeEvidence.Prediction.CalibrationSize,
            ConformalFallbackLevel: cascadeEvidence.Prediction.FallbackLevel,
            ConformalIntervalWidthMicroseconds: cascadeEvidence.Prediction.IntervalWidthMicroseconds,
            CascadeRecoveryStreak: cascadeEvidence.RecoveryStreak,
            CascadeRecoveryThreshold: cascadeEvidence.RecoveryThreshold);
    }

    private void RecordRenderTelemetry(
        PresentResult result,
        TimeSpan frameDuration,
        TimeSpan presentDuration,
        TimeSpan diffDuration,
        int dirtyRows,
        LoadGovernorDecision loadDecision,
        RuntimeCascadeEvidence cascadeEvidence,
        RuntimeConformalBucketKey cascadeKey,
        DiffStrategySelection? selection)
    {
        if (!Policy.EmitTelemetry)
        {
            return;
        }

        Telemetry.Record(
            "ftui.render.diff",
            TelemetryEventCategory.RenderPipeline,
            _stepIndex,
            [
                new TelemetryField("changes_count", result.ChangedCells.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("duration_us", ToMicroseconds(diffDuration).ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("rows_skipped", Math.Max(_next.Height - dirtyRows, 0).ToString(CultureInfo.InvariantCulture))
            ]);
        Telemetry.Record(
            "ftui.render.present",
            TelemetryEventCategory.RenderPipeline,
            _stepIndex,
            [
                new TelemetryField("bytes_written", result.Output.Length.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("duration_us", ToMicroseconds(presentDuration).ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("runs_count", result.RunCount.ToString(CultureInfo.InvariantCulture))
            ]);
        Telemetry.Record(
            "ftui.render.flush",
            TelemetryEventCategory.RenderPipeline,
            _stepIndex,
            [
                new TelemetryField("duration_us", ToMicroseconds(presentDuration).ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("sync_mode", result.UsedSyncOutput ? "true" : "false")
            ]);
        Telemetry.Record(
            "ftui.render.frame",
            TelemetryEventCategory.RenderPipeline,
            _stepIndex,
            [
                new TelemetryField("duration_us", ToMicroseconds(frameDuration).ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("height", _next.Height.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("width", _next.Width.ToString(CultureInfo.InvariantCulture))
            ]);
        Telemetry.Record(
            "ftui.decision.degradation",
            TelemetryEventCategory.Decision,
            _stepIndex,
            [
                new TelemetryField("action", loadDecision.Action),
                new TelemetryField("reason", loadDecision.Reason),
                new TelemetryField("level_before", loadDecision.LevelBefore.Label()),
                new TelemetryField(
                    "level_after",
                    (_cascadeRenderGateEnabled
                        ? MoreDegraded(loadDecision.LevelAfter, cascadeEvidence.LevelAfter)
                        : loadDecision.LevelAfter).Label()),
                new TelemetryField("frame_duration_ms", loadDecision.FrameDurationMs.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("target_frame_ms", loadDecision.TargetFrameMs.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("normalized_error", RuntimeLoadGovernor.FormatNormalizedError(loadDecision.NormalizedError)),
                new TelemetryField("pid_output", loadDecision.PidOutput.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("pid_p", loadDecision.PidP.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("pid_i", loadDecision.PidI.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("pid_d", loadDecision.PidD.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("e_value", loadDecision.EProcessValue.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("eprocess_sigma_ms", loadDecision.EProcessSigmaMs.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("frames_observed", loadDecision.FramesObserved.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("frames_since_change", loadDecision.FramesSinceChange.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("pid_gate_threshold", loadDecision.PidGateThreshold.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("pid_gate_margin", loadDecision.PidGateMargin.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("evidence_threshold", loadDecision.EvidenceThreshold.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("evidence_margin", loadDecision.EvidenceMargin.ToString("0.###", CultureInfo.InvariantCulture)),
                new TelemetryField("in_warmup", loadDecision.EProcessInWarmup ? "true" : "false"),
                new TelemetryField("transition_seq", loadDecision.TransitionSeq.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("transition_correlation_id", loadDecision.TransitionCorrelationId.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("cascade_decision", RuntimeCascadeEvidence.DecisionLabel(cascadeEvidence.Decision)),
                new TelemetryField("cascade_level_before", cascadeEvidence.LevelBefore.Label()),
                new TelemetryField("cascade_level_after", cascadeEvidence.LevelAfter.Label()),
                new TelemetryField("cascade_guard_state", RuntimeP99Prediction.StateLabel(cascadeEvidence.GuardState)),
                new TelemetryField("conformal_bucket", cascadeKey.ToString()),
                new TelemetryField("conformal_upper_us", cascadeEvidence.Prediction.UpperMicroseconds.ToString("0.#", CultureInfo.InvariantCulture)),
                new TelemetryField("conformal_budget_us", cascadeEvidence.BudgetMicroseconds.ToString("0.#", CultureInfo.InvariantCulture)),
                new TelemetryField("conformal_calibration_size", cascadeEvidence.Prediction.CalibrationSize.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("conformal_fallback_level", cascadeEvidence.Prediction.FallbackLevel.ToString(CultureInfo.InvariantCulture)),
                new TelemetryField("conformal_interval_width_us", cascadeEvidence.Prediction.IntervalWidthMicroseconds.ToString("0.#", CultureInfo.InvariantCulture))
            ],
            new TelemetryDecisionEvidence(
                "runtime.load_governor",
                $"frame_ms={loadDecision.FrameDurationMs:0.###};target_ms={loadDecision.TargetFrameMs:0.###}",
                loadDecision.Action,
                Math.Clamp(Math.Abs(loadDecision.NormalizedError), 0, 1),
                ["stay", "degrade", "upgrade"],
                $"Load governor {loadDecision.Action} with reason {loadDecision.Reason}."));

        if (selection is not null &&
            (selection.TransitionReason is not null || selection.Regime is not DiffRegime.StableFrame))
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
            Telemetry.Record(
                "ftui.reflow.apply",
                TelemetryEventCategory.RenderPipeline,
                _stepIndex,
                [
                    new TelemetryField("debounce_ms", "0"),
                    new TelemetryField("height", effectiveSize.Height.ToString(CultureInfo.InvariantCulture)),
                    new TelemetryField("latency_ms", "0"),
                    new TelemetryField("rate_hz", "0"),
                    new TelemetryField("width", effectiveSize.Width.ToString(CultureInfo.InvariantCulture))
                ]);
        }
    }

    private IReadOnlyList<TMessage> CollectMessages(
        AppCommand<TMessage> commands,
        IReadOnlyList<Subscription<TMessage>> subscriptions)
    {
        var reconcileStopwatch = Stopwatch.StartNew();
        var commandMessages = EffectSystem.TraceCommandEffect(
            commands.EffectKind,
            () => commands.Messages.ToArray());
        var currentKeys = subscriptions.Select(static item => item.Key).ToHashSet(StringComparer.Ordinal);
        var started = currentKeys.Except(_activeSubscriptionKeys, StringComparer.Ordinal).Count();
        var stopped = _activeSubscriptionKeys.Except(currentKeys, StringComparer.Ordinal).Count();
        EffectSystem.RecordSubscriptionStart(started);
        EffectSystem.RecordSubscriptionStop(stopped);
        var messages = new List<TMessage>(commandMessages);
        foreach (var subscription in subscriptions)
        {
            messages.AddRange(EffectSystem.TraceSubscriptionEffect(subscription));
        }
        reconcileStopwatch.Stop();
        EffectSystem.RecordReconcile(reconcileStopwatch.Elapsed);
        _activeSubscriptionKeys = currentKeys;

        return messages;
    }

    private static long ToMicroseconds(TimeSpan duration) =>
        (long)Math.Round(duration.TotalMilliseconds * 1000.0, MidpointRounding.AwayFromZero);

    private static RuntimeDegradationLevel MoreDegraded(
        RuntimeDegradationLevel left,
        RuntimeDegradationLevel right) =>
        left >= right ? left : right;

    private DiffSkipHint CreateDiffSkipHint(DiffStrategySelection selection, bool resized, int dirtyRows)
    {
        if (resized || selection.Strategy is DiffStrategy.Full or DiffStrategy.FullRedraw)
        {
            return DiffSkipHint.FullDiff;
        }

        if (dirtyRows == 0)
        {
            return DiffSkipHint.SkipDiff;
        }

        return DiffSkipHint.NarrowToRows(BufferDiff.CollectDirtyRows(_current, _next));
    }
}
