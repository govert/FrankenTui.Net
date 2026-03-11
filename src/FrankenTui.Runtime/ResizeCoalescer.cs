using System.Text.Json;
using FrankenTui.Core;

namespace FrankenTui.Runtime;

public enum CoalesceAction
{
    RenderNow,
    Coalesce,
    SkipFrame
}

public enum ResizeRegime
{
    Steady,
    Burst
}

public sealed record CoalescerConfig(
    int SteadyDelayMs,
    int BurstDelayMs,
    int HardDeadlineMs,
    double BurstEnterRate,
    double BurstExitRate,
    int CooldownFrames,
    int RateWindowSize,
    bool EnableLogging)
{
    public static CoalescerConfig Default { get; } = new(
        16,
        40,
        100,
        10.0,
        5.0,
        3,
        8,
        EnableLogging: true);
}

public sealed class ResizeCoalescer
{
    private readonly Queue<DateTimeOffset> _recentEvents = [];
    private PendingResize? _pending;
    private ResizeRegime _regime = ResizeRegime.Steady;
    private int _cooldownRemaining;

    public ResizeCoalescer(CoalescerConfig? config = null)
    {
        Config = config ?? CoalescerConfig.Default;
    }

    public CoalescerConfig Config { get; }

    public ResizeDecisionLog DecisionLog { get; } = new();

    public ResizeRegime Regime => _regime;

    public ResizeDecision Observe(Size size, DateTimeOffset timestamp)
    {
        _pending = _pending is null
            ? new PendingResize(size, timestamp)
            : _pending.Value with { LatestSize = size, LastEventTimestamp = timestamp };
        TrackRate(timestamp);

        var rate = CalculateRate();
        UpdateRegime(rate);
        var action = SelectAction(timestamp, rate, out var forcedDeadline);
        var decision = CreateDecision(size, timestamp, rate, action, forcedDeadline);
        DecisionLog.Record(decision);
        return decision;
    }

    public ResizeDecision? Evaluate(DateTimeOffset now)
    {
        if (_pending is not { } pending)
        {
            return null;
        }

        var rate = CalculateRate();
        UpdateRegime(rate);
        var action = SelectAction(now, rate, out var forcedDeadline);
        if (action == CoalesceAction.Coalesce)
        {
            return null;
        }

        var decision = CreateDecision(pending.LatestSize, now, rate, action, forcedDeadline);
        DecisionLog.Record(decision);
        return decision;
    }

    public Size? ConsumeReadySize(CoalesceAction action)
    {
        if (action != CoalesceAction.RenderNow || _pending is not { } pending)
        {
            return null;
        }

        _pending = null;
        if (_cooldownRemaining > 0)
        {
            _cooldownRemaining--;
        }

        return pending.LatestSize;
    }

    private void TrackRate(DateTimeOffset timestamp)
    {
        _recentEvents.Enqueue(timestamp);
        while (_recentEvents.Count > Config.RateWindowSize)
        {
            _recentEvents.Dequeue();
        }
    }

    private double CalculateRate()
    {
        if (_recentEvents.Count <= 1)
        {
            return 0;
        }

        var first = _recentEvents.Peek();
        var last = _recentEvents.Last();
        var seconds = Math.Max((last - first).TotalSeconds, 0.001);
        return (_recentEvents.Count - 1) / seconds;
    }

    private void UpdateRegime(double rate)
    {
        if (rate >= Config.BurstEnterRate)
        {
            _regime = ResizeRegime.Burst;
            _cooldownRemaining = Config.CooldownFrames;
            return;
        }

        if (_regime == ResizeRegime.Burst &&
            rate < Config.BurstExitRate &&
            _cooldownRemaining <= 0)
        {
            _regime = ResizeRegime.Steady;
        }
    }

    private CoalesceAction SelectAction(DateTimeOffset now, double rate, out bool forcedDeadline)
    {
        forcedDeadline = false;
        if (_pending is not { } pending)
        {
            return CoalesceAction.Coalesce;
        }

        var pendingAge = now - pending.FirstEventTimestamp;
        if (pendingAge.TotalMilliseconds >= Config.HardDeadlineMs)
        {
            forcedDeadline = true;
            return CoalesceAction.RenderNow;
        }

        var lastAge = now - pending.LastEventTimestamp;
        var targetDelay = _regime == ResizeRegime.Burst ? Config.BurstDelayMs : Config.SteadyDelayMs;
        if (lastAge.TotalMilliseconds >= targetDelay)
        {
            return CoalesceAction.RenderNow;
        }

        return _regime == ResizeRegime.Burst && rate >= Config.BurstEnterRate
            ? CoalesceAction.SkipFrame
            : CoalesceAction.Coalesce;
    }

    private ResizeDecision CreateDecision(
        Size size,
        DateTimeOffset timestamp,
        double rate,
        CoalesceAction action,
        bool forcedDeadline)
    {
        var pendingSince = _pending?.FirstEventTimestamp ?? timestamp;
        var coalesceMs = Math.Max((timestamp - pendingSince).TotalMilliseconds, 0);
        return new ResizeDecision(
            size,
            action,
            _regime,
            timestamp,
            rate,
            coalesceMs,
            forcedDeadline,
            forcedDeadline ? "hard_deadline" : _regime == ResizeRegime.Burst ? "burst_window" : "steady_window");
    }

    private readonly record struct PendingResize(Size LatestSize, DateTimeOffset FirstEventTimestamp, DateTimeOffset LastEventTimestamp)
    {
        public PendingResize(Size latestSize, DateTimeOffset timestamp)
            : this(latestSize, timestamp, timestamp)
        {
        }
    }
}

public sealed class ResizeDecisionLog
{
    private readonly List<ResizeDecision> _entries = [];

    public IReadOnlyList<ResizeDecision> Entries => _entries;

    public void Record(ResizeDecision decision) => _entries.Add(decision);

    public ResizeDecisionSummary Summarize() =>
        new(
            _entries.Count,
            _entries.Count(static item => item.Action == CoalesceAction.RenderNow),
            _entries.Count(static item => item.Action == CoalesceAction.Coalesce),
            _entries.Count(static item => item.Action == CoalesceAction.SkipFrame),
            _entries.Count(static item => item.ForcedDeadline));

    public string ToJson() =>
        JsonSerializer.Serialize(
            new
            {
                summary = Summarize(),
                entries = _entries
            },
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });
}

public sealed record ResizeDecision(
    Size Size,
    CoalesceAction Action,
    ResizeRegime Regime,
    DateTimeOffset Timestamp,
    double EventRate,
    double CoalesceMs,
    bool ForcedDeadline,
    string Reason);

public sealed record ResizeDecisionSummary(
    int TotalDecisions,
    int RenderNowCount,
    int CoalesceCount,
    int SkipFrameCount,
    int ForcedDeadlineCount);
