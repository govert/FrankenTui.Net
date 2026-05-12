using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FrankenTui.Runtime;

public enum RuntimeEstimateSource
{
    Explicit,
    Historical,
    Default,
    Unknown
}

public enum RuntimeWeightSource
{
    Explicit,
    Default,
    Unknown
}

public enum RuntimeSelectionReason
{
    QueueEmpty,
    ShortestRemaining,
    HighestWeightedPriority,
    Fifo,
    AgingBoost,
    Continuation
}

public enum RuntimeTieBreakReason
{
    EffectivePriority,
    BaseRatio,
    Weight,
    RemainingTime,
    ArrivalSeq,
    JobId,
    Continuation
}

public static class RuntimeQueueingSchedulerLabels
{
    public static string Label(this RuntimeEstimateSource source) =>
        source switch
        {
            RuntimeEstimateSource.Explicit => "explicit",
            RuntimeEstimateSource.Historical => "historical",
            RuntimeEstimateSource.Default => "default",
            RuntimeEstimateSource.Unknown => "unknown",
            _ => "unknown"
        };

    public static string Label(this RuntimeWeightSource source) =>
        source switch
        {
            RuntimeWeightSource.Explicit => "explicit",
            RuntimeWeightSource.Default => "default",
            RuntimeWeightSource.Unknown => "unknown",
            _ => "unknown"
        };

    public static string Label(this RuntimeSelectionReason reason) =>
        reason switch
        {
            RuntimeSelectionReason.QueueEmpty => "queue_empty",
            RuntimeSelectionReason.ShortestRemaining => "shortest_remaining",
            RuntimeSelectionReason.HighestWeightedPriority => "highest_weighted_priority",
            RuntimeSelectionReason.Fifo => "fifo",
            RuntimeSelectionReason.AgingBoost => "aging_boost",
            RuntimeSelectionReason.Continuation => "continuation",
            _ => "unknown"
        };

    public static string Label(this RuntimeTieBreakReason reason) =>
        reason switch
        {
            RuntimeTieBreakReason.EffectivePriority => "effective_priority",
            RuntimeTieBreakReason.BaseRatio => "base_ratio",
            RuntimeTieBreakReason.Weight => "weight",
            RuntimeTieBreakReason.RemainingTime => "remaining_time",
            RuntimeTieBreakReason.ArrivalSeq => "arrival_seq",
            RuntimeTieBreakReason.JobId => "job_id",
            RuntimeTieBreakReason.Continuation => "continuation",
            _ => "unknown"
        };
}

public sealed record RuntimeQueueJob(
    ulong Id,
    double Weight,
    double RemainingTime,
    double TotalTime,
    double ArrivalTime,
    ulong ArrivalSeq,
    RuntimeEstimateSource EstimateSource,
    RuntimeWeightSource WeightSource,
    string? Name = null)
{
    public double Progress => TotalTime <= 0 ? 1.0 : 1.0 - Math.Clamp(RemainingTime / TotalTime, 0.0, 1.0);

    public bool IsComplete => RemainingTime <= 0.0;

    public static RuntimeQueueJob Create(ulong id, double weight, double estimatedTime, string? name = null) =>
        new(
            id,
            NormalizeInitialWeight(weight),
            NormalizeInitialTime(estimatedTime),
            NormalizeInitialTime(estimatedTime),
            0,
            0,
            RuntimeEstimateSource.Explicit,
            RuntimeWeightSource.Explicit,
            name);

    private static double NormalizeInitialWeight(double value) =>
        double.IsNaN(value) ? 1e-6 : double.IsPositiveInfinity(value) ? 100.0 : double.IsNegativeInfinity(value) ? 1e-6 : Math.Clamp(value, 1e-6, 100.0);

    private static double NormalizeInitialTime(double value) =>
        double.IsNaN(value) ? 5_000.0 : double.IsPositiveInfinity(value) ? 5_000.0 : double.IsNegativeInfinity(value) ? 0.05 : Math.Clamp(value, 0.05, 5_000.0);
}

public sealed record RuntimeQueueJobEvidence(
    ulong JobId,
    string? Name,
    double EstimateMilliseconds,
    double Weight,
    double Ratio,
    double AgingReward,
    double StarvationFloor,
    double AgeMilliseconds,
    double EffectivePriority,
    double ObjectiveLossProxy,
    RuntimeEstimateSource EstimateSource,
    RuntimeWeightSource WeightSource);

public sealed record RuntimeSchedulingEvidence(
    double CurrentTime,
    ulong? SelectedJobId,
    int QueueLength,
    double MeanWaitTime,
    double MaxWaitTime,
    RuntimeSelectionReason Reason,
    RuntimeTieBreakReason? TieBreakReason,
    IReadOnlyList<RuntimeQueueJobEvidence> Jobs)
{
    public string ToJsonl(string eventName)
    {
        var builder = new StringBuilder(256 + Jobs.Count * 128);
        builder.Append("{\"event\":").Append(JsonSerializer.Serialize(eventName));
        builder.Append(",\"current_time\":").Append(CurrentTime.ToString("0.000000", CultureInfo.InvariantCulture));
        builder.Append(",\"selected_job_id\":").Append(SelectedJobId?.ToString(CultureInfo.InvariantCulture) ?? "null");
        builder.Append(",\"queue_length\":").Append(QueueLength.ToString(CultureInfo.InvariantCulture));
        builder.Append(",\"mean_wait_time\":").Append(MeanWaitTime.ToString("0.000000", CultureInfo.InvariantCulture));
        builder.Append(",\"max_wait_time\":").Append(MaxWaitTime.ToString("0.000000", CultureInfo.InvariantCulture));
        builder.Append(",\"reason\":").Append(JsonSerializer.Serialize(Reason.Label()));
        builder.Append(",\"tie_break_reason\":").Append(TieBreakReason is { } reason ? JsonSerializer.Serialize(reason.Label()) : "null");
        builder.Append(",\"jobs\":[");
        for (var index = 0; index < Jobs.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            var job = Jobs[index];
            builder.Append("{\"job_id\":").Append(job.JobId.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"name\":").Append(job.Name is null ? "null" : JsonSerializer.Serialize(job.Name));
            builder.Append(",\"estimate_ms\":").Append(job.EstimateMilliseconds.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"weight\":").Append(job.Weight.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"ratio\":").Append(job.Ratio.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"aging_reward\":").Append(job.AgingReward.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"starvation_floor\":").Append(job.StarvationFloor.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"age_ms\":").Append(job.AgeMilliseconds.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"effective_priority\":").Append(job.EffectivePriority.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"objective_loss_proxy\":").Append(job.ObjectiveLossProxy.ToString("0.000000", CultureInfo.InvariantCulture));
            builder.Append(",\"estimate_source\":").Append(JsonSerializer.Serialize(job.EstimateSource.Label()));
            builder.Append(",\"weight_source\":").Append(JsonSerializer.Serialize(job.WeightSource.Label()));
            builder.Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }
}

public sealed record RuntimeQueueTaskSpec(
    double Weight = 1.0,
    double EstimatedMilliseconds = 1.0,
    RuntimeWeightSource WeightSource = RuntimeWeightSource.Default,
    RuntimeEstimateSource EstimateSource = RuntimeEstimateSource.Default,
    string? Name = null)
{
    public static RuntimeQueueTaskSpec Default { get; } = new();
}

public sealed record RuntimeQueueSchedulerStats(
    ulong TotalSubmitted = 0,
    ulong TotalCompleted = 0,
    ulong TotalRejected = 0,
    ulong TotalPreemptions = 0,
    double TotalProcessingTime = 0,
    double TotalResponseTime = 0,
    double MaxResponseTime = 0,
    int QueueLength = 0)
{
    public double MeanResponseTime => TotalCompleted > 0 ? TotalResponseTime / TotalCompleted : 0.0;

    public double Throughput => TotalProcessingTime > 0 ? TotalCompleted / TotalProcessingTime : 0.0;
}

public sealed class RuntimeQueueingScheduler
{
    private readonly RuntimeQueueSchedulerConfig _config;
    private readonly List<RuntimeQueueJob> _queue = [];
    private RuntimeQueueJob? _currentJob;
    private double _currentTime;
    private ulong _nextJobId = 1;
    private ulong _nextArrivalSeq = 1;
    private RuntimeQueueSchedulerStats _stats = new();

    public RuntimeQueueingScheduler(RuntimeQueueSchedulerConfig? config = null)
    {
        _config = (config ?? RuntimeQueueSchedulerConfig.Default).Normalized();
    }

    public RuntimeQueueSchedulerStats Stats => _stats with { QueueLength = QueueLength };

    public int MaxQueueSize => _config.MaxQueueSize;

    public int QueueLength => _queue.Count + (_currentJob is null ? 0 : 1);

    public RuntimeQueueJob? CurrentJob => _currentJob;

    public ulong? Submit(double weight, double estimatedTime) =>
        SubmitNamed(weight, estimatedTime, null);

    public ulong? SubmitNamed(double weight, double estimatedTime, string? name) =>
        SubmitWithSources(weight, estimatedTime, RuntimeWeightSource.Explicit, RuntimeEstimateSource.Explicit, name);

    public ulong? SubmitWithSources(
        double weight,
        double estimatedTime,
        RuntimeWeightSource weightSource,
        RuntimeEstimateSource estimateSource,
        string? name = null)
    {
        if (_queue.Count >= _config.MaxQueueSize)
        {
            _stats = _stats with { TotalRejected = _stats.TotalRejected + 1 };
            return null;
        }

        var normalizedWeight = NormalizeWeightWithSource(weight, weightSource);
        var normalizedTime = NormalizeTimeWithSource(estimatedTime, estimateSource);
        var job = new RuntimeQueueJob(
            _nextJobId++,
            normalizedWeight,
            normalizedTime,
            normalizedTime,
            _currentTime,
            _nextArrivalSeq++,
            estimateSource,
            weightSource,
            name);
        _queue.Add(job);
        _stats = _stats with { TotalSubmitted = _stats.TotalSubmitted + 1, QueueLength = QueueLength };
        if (_config.Preemptive)
        {
            MaybePreempt();
        }

        return job.Id;
    }

    public IReadOnlyList<ulong> Tick(double deltaTime)
    {
        var completed = new List<ulong>();
        if (!double.IsFinite(deltaTime) || deltaTime <= 0.0)
        {
            return completed;
        }

        var remaining = deltaTime;
        var now = _currentTime;
        var processed = 0.0;
        while (remaining > 0.0)
        {
            var job = _currentJob;
            if (job is null)
            {
                job = PopNextJob();
            }
            else
            {
                _currentJob = null;
            }

            if (job is null)
            {
                now += remaining;
                break;
            }

            var processTime = Math.Min(remaining, job.RemainingTime);
            var updated = job with { RemainingTime = job.RemainingTime - processTime };
            remaining -= processTime;
            now += processTime;
            processed += processTime;
            if (updated.IsComplete)
            {
                var responseTime = now - updated.ArrivalTime;
                _stats = _stats with
                {
                    TotalCompleted = _stats.TotalCompleted + 1,
                    TotalResponseTime = _stats.TotalResponseTime + responseTime,
                    MaxResponseTime = Math.Max(_stats.MaxResponseTime, responseTime)
                };
                completed.Add(updated.Id);
            }
            else
            {
                _currentJob = updated;
            }
        }

        _stats = _stats with
        {
            TotalProcessingTime = _stats.TotalProcessingTime + processed,
            QueueLength = QueueLength
        };
        _currentTime = now;
        return completed;
    }

    public RuntimeQueueJob? PeekNext() =>
        _currentJob ?? SortByPriority(_queue).FirstOrDefault();

    public RuntimeSchedulingEvidence Evidence()
    {
        var candidates = SortByPriority(_queue);
        if (_currentJob is not null)
        {
            candidates.Add(_currentJob);
            candidates = SortByPriority(candidates);
        }

        var selectedId = _currentJob?.Id ?? candidates.FirstOrDefault()?.Id;
        var tieBreak = _currentJob is not null
            ? RuntimeTieBreakReason.Continuation
            : candidates.Count > 1
                ? TieBreakReason(candidates[0], candidates[1])
                : (RuntimeTieBreakReason?)null;
        var reason = SelectionReason(candidates.FirstOrDefault(), currentSelected: _currentJob is not null);
        var waits = ComputeWaitStats();
        var jobs = candidates.Select(job =>
        {
            var ratio = ComputeBaseRatio(job);
            var terms = ComputePriorityTerms(job);
            var age = Math.Max(_currentTime - job.ArrivalTime, 0.0);
            return new RuntimeQueueJobEvidence(
                job.Id,
                job.Name,
                job.RemainingTime,
                job.Weight,
                ratio,
                terms.AgingReward,
                terms.StarvationFloor,
                age,
                terms.EffectivePriority,
                1.0 / Math.Max(terms.EffectivePriority, _config.MinWeight),
                job.EstimateSource,
                job.WeightSource);
        }).ToArray();

        return new RuntimeSchedulingEvidence(
            _currentTime,
            selectedId,
            QueueLength,
            waits.Mean,
            waits.Max,
            reason,
            tieBreak,
            jobs);
    }

    public bool Cancel(ulong jobId)
    {
        if (_currentJob?.Id == jobId)
        {
            _currentJob = null;
            _stats = _stats with { QueueLength = QueueLength };
            return true;
        }

        var removed = _queue.RemoveAll(job => job.Id == jobId) > 0;
        _stats = _stats with { QueueLength = QueueLength };
        return removed;
    }

    public void Clear()
    {
        _queue.Clear();
        _currentJob = null;
        _stats = _stats with { QueueLength = 0 };
    }

    public void Reset()
    {
        _queue.Clear();
        _currentJob = null;
        _currentTime = 0;
        _nextJobId = 1;
        _nextArrivalSeq = 1;
        _stats = new RuntimeQueueSchedulerStats();
    }

    private RuntimeQueueJob? PopNextJob()
    {
        if (_queue.Count == 0)
        {
            return null;
        }

        var sorted = SortByPriority(_queue);
        var next = sorted[0];
        _queue.Remove(next);
        return next;
    }

    private List<RuntimeQueueJob> SortByPriority(IEnumerable<RuntimeQueueJob> jobs)
    {
        if (_config.Mode == RuntimeSchedulingMode.Fifo)
        {
            return jobs
                .OrderBy(job => job.ArrivalSeq)
                .ThenBy(job => job.Id)
                .ToList();
        }

        return jobs
            .OrderByDescending(job => Priority(job))
            .ThenByDescending(ComputeBaseRatio)
            .ThenByDescending(job => job.Weight)
            .ThenBy(job => job.RemainingTime)
            .ThenBy(job => job.ArrivalSeq)
            .ThenBy(job => job.Id)
            .ToList();
    }

    private double NormalizeWeight(double weight)
    {
        if (double.IsNaN(weight)) return _config.MinWeight;
        if (double.IsPositiveInfinity(weight)) return _config.MaxWeight;
        if (double.IsNegativeInfinity(weight)) return _config.MinWeight;
        return Math.Clamp(weight, _config.MinWeight, _config.MaxWeight);
    }

    private double NormalizeTime(double estimate)
    {
        if (double.IsNaN(estimate)) return _config.MaxProcessingMilliseconds;
        if (double.IsPositiveInfinity(estimate)) return _config.MaxProcessingMilliseconds;
        if (double.IsNegativeInfinity(estimate)) return _config.MinProcessingMilliseconds;
        return Math.Clamp(estimate, _config.MinProcessingMilliseconds, _config.MaxProcessingMilliseconds);
    }

    private double NormalizeWeightWithSource(double weight, RuntimeWeightSource source) =>
        NormalizeWeight(source switch
        {
            RuntimeWeightSource.Default => _config.DefaultWeight,
            RuntimeWeightSource.Unknown => _config.UnknownWeight,
            _ => weight
        });

    private double NormalizeTimeWithSource(double estimate, RuntimeEstimateSource source) =>
        NormalizeTime(source switch
        {
            RuntimeEstimateSource.Default => _config.DefaultEstimateMilliseconds,
            RuntimeEstimateSource.Unknown => _config.UnknownEstimateMilliseconds,
            _ => estimate
        });

    private double ComputeBaseRatio(RuntimeQueueJob job)
    {
        if (_config.Mode == RuntimeSchedulingMode.Fifo) return 0.0;
        var weight = _config.Mode == RuntimeSchedulingMode.Smith ? job.Weight : 1.0;
        return weight / Math.Max(job.RemainingTime, _config.MinProcessingMilliseconds);
    }

    private (double AgingReward, double StarvationFloor, double EffectivePriority) ComputePriorityTerms(RuntimeQueueJob job)
    {
        if (_config.Mode == RuntimeSchedulingMode.Fifo) return (0, 0, 0);
        var baseRatio = ComputeBaseRatio(job);
        var wait = Math.Max(_currentTime - job.ArrivalTime, 0.0);
        var aging = _config.AgingFactor * wait;
        var floor = _config.StarvationGuardMilliseconds > 0.0 && wait >= _config.StarvationGuardMilliseconds
            ? baseRatio * _config.StarvationBoostRatio
            : 0.0;
        return (aging, floor, Math.Max(baseRatio + aging, floor));
    }

    private double Priority(RuntimeQueueJob job) => ComputePriorityTerms(job).EffectivePriority;

    private RuntimeTieBreakReason TieBreakReason(RuntimeQueueJob a, RuntimeQueueJob b)
    {
        if (_config.Mode == RuntimeSchedulingMode.Fifo)
        {
            return a.ArrivalSeq != b.ArrivalSeq ? RuntimeTieBreakReason.ArrivalSeq : RuntimeTieBreakReason.JobId;
        }

        if (Priority(a).CompareTo(Priority(b)) != 0) return RuntimeTieBreakReason.EffectivePriority;
        if (ComputeBaseRatio(a).CompareTo(ComputeBaseRatio(b)) != 0) return RuntimeTieBreakReason.BaseRatio;
        if (a.Weight.CompareTo(b.Weight) != 0) return RuntimeTieBreakReason.Weight;
        if (a.RemainingTime.CompareTo(b.RemainingTime) != 0) return RuntimeTieBreakReason.RemainingTime;
        return a.ArrivalSeq != b.ArrivalSeq ? RuntimeTieBreakReason.ArrivalSeq : RuntimeTieBreakReason.JobId;
    }

    private RuntimeSelectionReason SelectionReason(RuntimeQueueJob? selected, bool currentSelected)
    {
        if (QueueLength == 0) return RuntimeSelectionReason.QueueEmpty;
        if (currentSelected) return RuntimeSelectionReason.Continuation;
        if (_config.Mode == RuntimeSchedulingMode.Fifo) return RuntimeSelectionReason.Fifo;
        if (selected is null) return RuntimeSelectionReason.QueueEmpty;
        var wait = Math.Max(_currentTime - selected.ArrivalTime, 0.0);
        var ratio = ComputeBaseRatio(selected);
        var agingBoost = (_config.StarvationGuardMilliseconds > 0.0 && wait >= _config.StarvationGuardMilliseconds) ||
            _config.AgingFactor * wait > ratio * 0.5;
        if (agingBoost) return RuntimeSelectionReason.AgingBoost;
        return _config.SmithEnabled && selected.Weight > 1.0
            ? RuntimeSelectionReason.HighestWeightedPriority
            : RuntimeSelectionReason.ShortestRemaining;
    }

    private void MaybePreempt()
    {
        if (_config.Mode == RuntimeSchedulingMode.Fifo || _currentJob is null || _queue.Count == 0)
        {
            return;
        }

        var bestQueued = SortByPriority(_queue)[0];
        if (ComparePriority(bestQueued, _currentJob) < 0)
        {
            _queue.Add(_currentJob);
            _currentJob = null;
            _stats = _stats with { TotalPreemptions = _stats.TotalPreemptions + 1 };
        }
    }

    private int ComparePriority(RuntimeQueueJob left, RuntimeQueueJob right)
    {
        var sorted = SortByPriority([left, right]);
        return sorted[0].Id == left.Id ? -1 : 1;
    }

    private (double Mean, double Max) ComputeWaitStats()
    {
        var jobs = _queue.Concat(_currentJob is null ? [] : [_currentJob]).ToArray();
        if (jobs.Length == 0)
        {
            return (0, 0);
        }

        var waits = jobs.Select(job => Math.Max(_currentTime - job.ArrivalTime, 0.0)).ToArray();
        return (waits.Average(), waits.Max());
    }
}
