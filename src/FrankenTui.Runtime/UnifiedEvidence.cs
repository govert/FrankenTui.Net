// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/unified_evidence.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Unified Evidence Ledger for all Bayesian decision points.
// Every adaptive controller emits decisions through this common schema.
// Fixed-capacity ring buffer with zero per-decision allocation on hot path.

using System.Text;

namespace FrankenTui.Runtime;

// ── DecisionDomain ────────────────────────────────────────────────────────

/// <summary>Domain of a Bayesian decision point. Covers all 7 adaptive controllers.</summary>
public enum DecisionDomain
{
    DiffStrategy,
    ResizeCoalescing,
    FrameBudget,
    Degradation,
    VoiSampling,
    HintRanking,
    PaletteScoring,
}

public static class DecisionDomainMeta
{
    public static string AsStr(DecisionDomain d) => d switch
    {
        DecisionDomain.DiffStrategy => "diff_strategy",
        DecisionDomain.ResizeCoalescing => "resize_coalescing",
        DecisionDomain.FrameBudget => "frame_budget",
        DecisionDomain.Degradation => "degradation",
        DecisionDomain.VoiSampling => "voi_sampling",
        DecisionDomain.HintRanking => "hint_ranking",
        DecisionDomain.PaletteScoring => "palette_scoring",
        _ => throw new ArgumentOutOfRangeException(nameof(d)),
    };

    public static readonly DecisionDomain[] All = Enum.GetValues<DecisionDomain>();
}

// ── EvidenceTerm ──────────────────────────────────────────────────────────

/// <summary>
/// A single piece of evidence contributing to a Bayesian decision.
/// Bayes factor > 1 supports the chosen action; &lt;1 opposes it.
/// </summary>
public sealed record EvidenceTerm(string Label, double BayesFactor)
{
    /// <summary>Log Bayes factor (natural log).</summary>
    public double LogBf => Math.Log(BayesFactor);
}

// ── EvidenceEntry ─────────────────────────────────────────────────────────

/// <summary>
/// Unified evidence record for any Bayesian decision point.
/// Fixed-size: the top-3 evidence array avoids heap allocation.
/// </summary>
public sealed class EvidenceEntry
{
    public ulong DecisionId { get; set; }
    public ulong TimestampNs { get; set; }
    public DecisionDomain Domain { get; set; }
    public double LogPosterior { get; set; }
    public EvidenceTerm?[] TopEvidence { get; set; } = new EvidenceTerm?[3];
    public string Action { get; set; } = "";
    public double LossAvoided { get; set; }
    public (double Lower, double Upper) ConfidenceInterval { get; set; }

    /// <summary>Posterior probability derived from log-odds.</summary>
    public double PosteriorProbability()
    {
        if (LogPosterior >= 0.0)
            return 1.0 / (1.0 + Math.Exp(-LogPosterior));
        var expLp = Math.Exp(LogPosterior);
        return expLp / (1.0 + expLp);
    }

    /// <summary>Number of evidence terms present.</summary>
    public int EvidenceCount => TopEvidence.Count(t => t != null);

    /// <summary>Combined log Bayes factor (sum of individual log-BFs).</summary>
    public double CombinedLogBf => TopEvidence.Where(t => t != null).Sum(t => t!.LogBf);

    /// <summary>Format as a JSONL line (no trailing newline).</summary>
    public string ToJsonl()
    {
        var sb = new StringBuilder(256);
        sb.Append("{\"schema\":\"ftui-evidence-v2\"");
        sb.AppendFormat(",\"id\":{0}", DecisionId);
        sb.AppendFormat(",\"ts_ns\":{0}", TimestampNs);
        sb.AppendFormat(",\"domain\":\"{0}\"", DecisionDomainMeta.AsStr(Domain));
        sb.AppendFormat(",\"log_posterior\":{0:F6}", LogPosterior);

        sb.Append(",\"evidence\":[");
        bool first = true;
        foreach (var term in TopEvidence)
        {
            if (term == null) continue;
            if (!first) sb.Append(',');
            first = false;
            sb.AppendFormat("{{\"label\":\"{0}\",\"bf\":{1:F6}}}", term.Label, term.BayesFactor);
        }
        sb.Append(']');

        sb.AppendFormat(",\"action\":\"{0}\"", Action);
        sb.AppendFormat(",\"loss_avoided\":{0:F6}", LossAvoided);
        sb.AppendFormat(",\"ci\":[{0:F6},{1:F6}]", ConfidenceInterval.Lower, ConfidenceInterval.Upper);
        sb.Append('}');
        return sb.ToString();
    }
}

// ── EvidenceEntryBuilder ──────────────────────────────────────────────────

/// <summary>
/// Builder for constructing EvidenceEntry values.
/// Handles automatic selection of top-3 evidence terms by |log(BF)|.
/// </summary>
public sealed class EvidenceEntryBuilder
{
    private readonly DecisionDomain _domain;
    private readonly ulong _decisionId;
    private readonly ulong _timestampNs;
    private double _logPosterior;
    private readonly List<EvidenceTerm> _evidence = new();
    private string _action = "";
    private double _lossAvoided;
    private (double, double) _confidenceInterval = (0.0, 1.0);

    public EvidenceEntryBuilder(DecisionDomain domain, ulong decisionId, ulong timestampNs)
    {
        _domain = domain;
        _decisionId = decisionId;
        _timestampNs = timestampNs;
    }

    public EvidenceEntryBuilder LogPosterior(double value) { _logPosterior = value; return this; }
    public EvidenceEntryBuilder Evidence(string label, double bayesFactor) { _evidence.Add(new EvidenceTerm(label, bayesFactor)); return this; }
    public EvidenceEntryBuilder Action(string action) { _action = action; return this; }
    public EvidenceEntryBuilder LossAvoided(double value) { _lossAvoided = value; return this; }
    public EvidenceEntryBuilder ConfidenceInterval(double lower, double upper) { _confidenceInterval = (lower, upper); return this; }

    /// <summary>Build the entry, selecting top-3 evidence terms by |log(BF)|.</summary>
    public EvidenceEntry Build()
    {
        _evidence.Sort((a, b) => Math.Abs(b.LogBf).CompareTo(Math.Abs(a.LogBf)));
        var top = new EvidenceTerm?[3];
        for (int i = 0; i < Math.Min(3, _evidence.Count); i++)
            top[i] = _evidence[i];
        return new EvidenceEntry
        {
            DecisionId = _decisionId,
            TimestampNs = _timestampNs,
            Domain = _domain,
            LogPosterior = _logPosterior,
            TopEvidence = top,
            Action = _action,
            LossAvoided = _lossAvoided,
            ConfidenceInterval = _confidenceInterval,
        };
    }
}

// ── UnifiedEvidenceLedger ─────────────────────────────────────────────────

/// <summary>
/// Fixed-capacity ring buffer storing EvidenceEntry records from all
/// decision domains. Pre-allocates all storage so Record never allocates.
/// </summary>
public sealed class UnifiedEvidenceLedger
{
    private readonly EvidenceEntry?[] _entries;
    private int _head;
    private int _count;
    private readonly int _capacity;
    private ulong _nextId;
    private readonly ulong[] _domainCounts = new ulong[7];

    public UnifiedEvidenceLedger(int capacity)
    {
        _capacity = Math.Max(1, capacity);
        _entries = new EvidenceEntry?[_capacity];
    }

    /// <summary>Record an evidence entry. Returns the assigned decision_id.</summary>
    public ulong Record(EvidenceEntry entry)
    {
        var id = _nextId++;
        entry.DecisionId = id;
        _domainCounts[(int)entry.Domain]++;
        _entries[_head] = entry;
        _head = (_head + 1) % _capacity;
        if (_count < _capacity) _count++;
        return id;
    }

    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public ulong TotalRecorded => _nextId;
    public ulong DomainCount(DecisionDomain d) => _domainCounts[(int)d];

    /// <summary>Iterate stored entries in insertion order (oldest first).</summary>
    public IEnumerable<EvidenceEntry> Entries()
    {
        var start = _count < _capacity ? 0 : _head;
        for (int i = 0; i < _count; i++)
        {
            var idx = (start + i) % _capacity;
            if (_entries[idx] != null)
                yield return _entries[idx]!;
        }
    }

    public IEnumerable<EvidenceEntry> EntriesForDomain(DecisionDomain domain) =>
        Entries().Where(e => e.Domain == domain);

    public EvidenceEntry? LastEntry()
    {
        if (_count == 0) return null;
        var idx = _head == 0 ? _capacity - 1 : _head - 1;
        return _entries[idx];
    }

    public EvidenceEntry? LastEntryForDomain(DecisionDomain domain)
    {
        var start = _head == 0 ? _capacity - 1 : _head - 1;
        for (int i = 0; i < _count; i++)
        {
            var idx = (start + _capacity - i) % _capacity;
            if (_entries[idx] is { } entry && entry.Domain == domain)
                return entry;
        }
        return null;
    }

    public string ExportJsonl()
    {
        var sb = new StringBuilder();
        foreach (var e in Entries())
        {
            sb.Append(e.ToJsonl());
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public void Clear()
    {
        Array.Clear(_entries);
        _head = 0;
        _count = 0;
    }

    public LedgerSummary Summary()
    {
        var perDomain = new (ulong Count, double SumLoss, double SumPosterior)[7];
        foreach (var e in Entries())
        {
            int i = (int)e.Domain;
            perDomain[i].Count++;
            perDomain[i].SumLoss += e.LossAvoided;
            perDomain[i].SumPosterior += e.PosteriorProbability();
        }
        var domains = new List<DomainSummary>();
        for (int i = 0; i < 7; i++)
        {
            if (perDomain[i].Count == 0) continue;
            domains.Add(new DomainSummary(
                (DecisionDomain)i,
                perDomain[i].Count,
                perDomain[i].SumLoss / perDomain[i].Count,
                perDomain[i].SumPosterior / perDomain[i].Count));
        }
        return new LedgerSummary(_nextId, (ulong)_count, domains);
    }
}

// ── Summary types ─────────────────────────────────────────────────────────

public sealed record LedgerSummary(ulong TotalDecisions, ulong StoredDecisions, List<DomainSummary> Domains);
public sealed record DomainSummary(DecisionDomain Domain, ulong DecisionCount, double MeanLossAvoided, double MeanPosterior);

// ── EmitsEvidence interface ───────────────────────────────────────────────

/// <summary>Interface for decision-making components that emit unified evidence.</summary>
public interface IEmitsEvidence
{
    EvidenceEntry ToEvidenceEntry(ulong timestampNs);
    DecisionDomain EvidenceDomain { get; }
}
