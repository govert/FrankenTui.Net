using System.Text.Json;

namespace FrankenTui.Render;

public enum DiffStrategy
{
    Full,
    DirtyRows,
    FullRedraw,
    SignificantDirtyRows
}

public enum DiffRegime
{
    StableFrame,
    BurstyChange,
    ResizeRegime,
    DegradedTerminal
}

public sealed record DiffStrategySelection(
    int FrameIndex,
    DiffRegime Regime,
    DiffStrategy Strategy,
    double Confidence,
    int DirtyRows,
    int TotalCells,
    DiffRegime? TransitionFrom,
    string? TransitionReason);

public sealed record DiffDecisionRecord(
    int FrameIndex,
    DiffRegime Regime,
    DiffStrategy Strategy,
    double Confidence,
    int DirtyRows,
    int ChangedCells,
    int TotalCells,
    double ChangeFraction,
    double WriteLatencyMs,
    bool Fallback,
    string? TransitionReason);

public sealed record DiffRegimeTransitionRecord(
    int FrameIndex,
    DiffRegime From,
    DiffRegime To,
    string Trigger,
    double Confidence);

public sealed class DiffEvidenceLedger
{
    private const int MaxEntries = 10_000;

    private readonly List<DiffDecisionRecord> _decisions = [];
    private readonly List<DiffRegimeTransitionRecord> _transitions = [];

    public IReadOnlyList<DiffDecisionRecord> Decisions => _decisions;

    public IReadOnlyList<DiffRegimeTransitionRecord> Transitions => _transitions;

    public string ToJson() =>
        JsonSerializer.Serialize(
            new
            {
                decisions = _decisions,
                transitions = _transitions
            },
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });

    internal void RecordDecision(DiffDecisionRecord record)
    {
        if (_decisions.Count == MaxEntries)
        {
            _decisions.RemoveAt(0);
        }

        _decisions.Add(record);
    }

    internal void RecordTransition(DiffRegimeTransitionRecord record)
    {
        if (_transitions.Count == MaxEntries)
        {
            _transitions.RemoveAt(0);
        }

        _transitions.Add(record);
    }
}

public sealed class DiffStrategySelector
{
    private readonly DiffEvidenceLedger _ledger = new();

    private DiffRegime _regime = DiffRegime.StableFrame;
    private DiffRegime _previousNonDegraded = DiffRegime.StableFrame;
    private double _lastChangeFraction;
    private int _lowChangeStreak;
    private int _frameIndex;

    public DiffEvidenceLedger Ledger => _ledger;

    public DiffStrategySelection Select(
        ushort width,
        ushort height,
        int dirtyRows,
        bool resized,
        TimeSpan lastWriteLatency)
    {
        var totalCells = Math.Max(width * height, 1);
        var dirtyFraction = dirtyRows / (double)Math.Max(height, (ushort)1);
        var transitionFrom = default(DiffRegime?);
        string? transitionReason = null;

        if (resized)
        {
            if (_regime != DiffRegime.ResizeRegime)
            {
                transitionFrom = _regime;
                transitionReason = "resize";
            }

            if (_regime != DiffRegime.DegradedTerminal)
            {
                _previousNonDegraded = _regime;
            }

            _regime = DiffRegime.ResizeRegime;
        }
        else if (lastWriteLatency.TotalMilliseconds > 10)
        {
            if (_regime != DiffRegime.DegradedTerminal)
            {
                transitionFrom = _regime;
                transitionReason = $"write_latency_ms={lastWriteLatency.TotalMilliseconds:0.###}";
                _previousNonDegraded = _regime;
                _regime = DiffRegime.DegradedTerminal;
            }
        }
        else if (_regime == DiffRegime.DegradedTerminal && lastWriteLatency.TotalMilliseconds < 5)
        {
            transitionFrom = _regime;
            transitionReason = $"recover_latency_ms={lastWriteLatency.TotalMilliseconds:0.###}";
            _regime = _previousNonDegraded;
        }
        else if (_regime != DiffRegime.ResizeRegime && dirtyFraction > 0.5)
        {
            if (_regime != DiffRegime.BurstyChange)
            {
                transitionFrom = _regime;
                transitionReason = $"dirty_fraction={dirtyFraction:0.###}";
            }

            _regime = DiffRegime.BurstyChange;
        }
        else if (_regime != DiffRegime.ResizeRegime)
        {
            var nextRegime =
                _regime == DiffRegime.BurstyChange && _lowChangeStreak >= 3 ? DiffRegime.StableFrame :
                _regime == DiffRegime.StableFrame ? DiffRegime.StableFrame :
                _regime;

            if (nextRegime != _regime)
            {
                transitionFrom = _regime;
                transitionReason = $"low_change_streak={_lowChangeStreak}";
                _regime = nextRegime;
            }
        }

        var strategy = _regime switch
        {
            DiffRegime.ResizeRegime => DiffStrategy.FullRedraw,
            DiffRegime.BurstyChange => DiffStrategy.Full,
            DiffRegime.DegradedTerminal => DiffStrategy.SignificantDirtyRows,
            _ => dirtyFraction <= 0.6 ? DiffStrategy.DirtyRows : DiffStrategy.Full
        };

        var confidence = _regime switch
        {
            DiffRegime.ResizeRegime => 1.0,
            DiffRegime.BurstyChange => Math.Clamp(Math.Max(_lastChangeFraction, dirtyFraction), 0.5, 1.0),
            DiffRegime.DegradedTerminal => Math.Clamp(lastWriteLatency.TotalMilliseconds / 20.0, 0.5, 1.0),
            _ => Math.Clamp(1.0 - dirtyFraction, 0.5, 0.95)
        };

        if (transitionFrom is { } from)
        {
            _ledger.RecordTransition(
                new DiffRegimeTransitionRecord(
                    _frameIndex,
                    from,
                    _regime,
                    transitionReason ?? "state-change",
                    confidence));
        }

        return new DiffStrategySelection(
            _frameIndex,
            _regime,
            strategy,
            confidence,
            dirtyRows,
            totalCells,
            transitionFrom,
            transitionReason);
    }

    public void Observe(DiffStrategySelection selection, int changedCells, TimeSpan writeLatency)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var changeFraction = selection.TotalCells <= 0
            ? 0
            : changedCells / (double)selection.TotalCells;

        _ledger.RecordDecision(
            new DiffDecisionRecord(
                selection.FrameIndex,
                selection.Regime,
                selection.Strategy,
                selection.Confidence,
                selection.DirtyRows,
                changedCells,
                selection.TotalCells,
                changeFraction,
                writeLatency.TotalMilliseconds,
                Fallback: selection.Regime == DiffRegime.StableFrame &&
                          selection.Strategy == DiffStrategy.Full,
                selection.TransitionReason));

        _lastChangeFraction = changeFraction;
        _lowChangeStreak = changeFraction < 0.05 ? _lowChangeStreak + 1 : 0;
        if (_regime == DiffRegime.ResizeRegime)
        {
            _regime = _previousNonDegraded;
        }

        if (_regime != DiffRegime.DegradedTerminal && _regime != DiffRegime.ResizeRegime)
        {
            _previousNonDegraded = _regime;
        }

        _frameIndex++;
    }
}
