namespace FrankenTui.Extras;

public abstract record HintContext
{
    private HintContext()
    {
    }

    public sealed record Global : HintContext;

    public sealed record Widget(string Name) : HintContext;

    public sealed record Mode(string Name) : HintContext;
}

public sealed record HintStats(
    double Alpha,
    double Beta,
    double Cost,
    uint StaticPriority,
    ulong Observations)
{
    public double ExpectedUtility => Alpha / (Alpha + Beta);

    public double Variance
    {
        get
        {
            var sum = Alpha + Beta;
            return Alpha * Beta / (sum * sum * (sum + 1.0));
        }
    }

    public double ValueOfInformation => Math.Sqrt(Variance);
}

public sealed record HintEntry(
    int Id,
    string Label,
    double Cost,
    HintContext Context,
    HintStats Stats);

public sealed record HintRankingEvidence(
    int Id,
    string Label,
    double ExpectedUtility,
    double Cost,
    double NetValue,
    double ValueOfInformation,
    int Rank);

public sealed record HintRankerConfig(
    double PriorAlpha = 1.0,
    double PriorBeta = 1.0,
    double Lambda = 0.01,
    double Hysteresis = 0.02,
    double ValueOfInformationWeight = 0.1);

public sealed class HintRanker
{
    private readonly HintRankerConfig _config;
    private readonly List<HintEntry> _hints = [];
    private IReadOnlyList<int> _lastOrdering = [];
    private string? _lastContext;

    public HintRanker(HintRankerConfig? config = null)
    {
        _config = config ?? new HintRankerConfig();
    }

    public int Register(string label, double costColumns, HintContext context, uint staticPriority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(context);

        var id = _hints.Count;
        _hints.Add(new HintEntry(
            id,
            label,
            costColumns,
            context,
            new HintStats(
                _config.PriorAlpha,
                _config.PriorBeta,
                costColumns,
                staticPriority,
                Observations: 0)));
        return id;
    }

    public void RecordUsage(int hintId)
    {
        if (hintId < 0 || hintId >= _hints.Count)
        {
            return;
        }

        var hint = _hints[hintId];
        _hints[hintId] = hint with
        {
            Stats = hint.Stats with
            {
                Alpha = hint.Stats.Alpha + 1.0,
                Observations = hint.Stats.Observations + 1
            }
        };
    }

    public void RecordShownNotUsed(int hintId)
    {
        if (hintId < 0 || hintId >= _hints.Count)
        {
            return;
        }

        var hint = _hints[hintId];
        _hints[hintId] = hint with
        {
            Stats = hint.Stats with
            {
                Beta = hint.Stats.Beta + 1.0,
                Observations = hint.Stats.Observations + 1
            }
        };
    }

    public (IReadOnlyList<int> Ordering, IReadOnlyList<HintRankingEvidence> Ledger) Rank(string? contextKey = null)
    {
        var candidates = _hints
            .Where(hint => MatchesContext(hint.Context, contextKey))
            .Select(hint =>
            {
                var value = hint.Stats.Observations == 0
                    ? -hint.Stats.StaticPriority
                    : NetValue(hint);
                return (hint.Id, Value: value);
            })
            .OrderByDescending(static candidate => candidate.Value)
            .ThenBy(static candidate => candidate.Id)
            .ToArray();

        var newOrdering = candidates.Select(static candidate => candidate.Id).ToArray();
        var ordering = string.Equals(_lastContext, contextKey, StringComparison.Ordinal) && _lastOrdering.Count > 0
            ? ApplyHysteresis(newOrdering, candidates)
            : newOrdering;

        var ledger = ordering
            .Select((id, rank) =>
            {
                var hint = _hints[id];
                return new HintRankingEvidence(
                    hint.Id,
                    hint.Label,
                    hint.Stats.ExpectedUtility,
                    hint.Cost,
                    NetValue(hint),
                    hint.Stats.ValueOfInformation,
                    rank);
            })
            .ToArray();

        _lastOrdering = ordering;
        _lastContext = contextKey;
        return (ordering, ledger);
    }

    private double NetValue(HintEntry hint) =>
        hint.Stats.ExpectedUtility +
        _config.ValueOfInformationWeight * hint.Stats.ValueOfInformation -
        _config.Lambda * hint.Cost;

    private IReadOnlyList<int> ApplyHysteresis(
        IReadOnlyList<int> newOrdering,
        IReadOnlyList<(int Id, double Value)> scores)
    {
        var scoreMap = scores.ToDictionary(static score => score.Id, static score => score.Value);
        var result = _lastOrdering.Where(newOrdering.Contains).ToList();
        foreach (var id in newOrdering)
        {
            if (!result.Contains(id))
            {
                result.Add(id);
            }
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            for (var index = 0; index < result.Count - 1; index++)
            {
                var left = result[index];
                var right = result[index + 1];
                var leftScore = scoreMap.GetValueOrDefault(left, double.NegativeInfinity);
                var rightScore = scoreMap.GetValueOrDefault(right, double.NegativeInfinity);
                if (rightScore > leftScore + _config.Hysteresis)
                {
                    result[index] = right;
                    result[index + 1] = left;
                    changed = true;
                }
            }
        }

        return result;
    }

    private static bool MatchesContext(HintContext context, string? contextKey) =>
        context switch
        {
            HintContext.Global => true,
            HintContext.Widget widget when contextKey is not null => string.Equals(widget.Name, contextKey, StringComparison.Ordinal),
            HintContext.Mode mode when contextKey is not null => string.Equals(mode.Name, contextKey, StringComparison.Ordinal),
            _ => contextKey is null
        };
}
