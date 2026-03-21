using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public enum FailureClass
{
    Mismatch,
    Timeout,
    Cancellation,
    QueueOverload,
    ProcessFailure,
    Rollback,
    ShadowDivergence,
    PanicCaught,
    NetworkFailure
}

public sealed record FailureLogEntry(
    FailureClass Class,
    IReadOnlySet<string> Fields);

public sealed record FailureLogValidation(
    FailureClass Class,
    IReadOnlyList<string> MissingFields,
    bool Passes);

public sealed record FailureLogSummary(
    int EntryCount,
    int FailureCount,
    IReadOnlyList<FailureLogValidation> Failures)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class FailureSignatures
{
    public static string ReasonCode(FailureClass failureClass) => failureClass switch
    {
        FailureClass.Mismatch => "MISMATCH",
        FailureClass.Timeout => "TIMEOUT",
        FailureClass.Cancellation => "CANCELLATION",
        FailureClass.QueueOverload => "QUEUE_OVERLOAD",
        FailureClass.ProcessFailure => "PROCESS_FAILURE",
        FailureClass.Rollback => "ROLLBACK",
        FailureClass.ShadowDivergence => "SHADOW_DIVERGENCE",
        FailureClass.PanicCaught => "PANIC_CAUGHT",
        _ => "NETWORK_FAILURE"
    };

    public static IReadOnlyList<string> RequiredFields(FailureClass failureClass) => failureClass switch
    {
        FailureClass.Mismatch => ["reason", "frame_idx", "expected_hash", "actual_hash", "scenario", "seed"],
        FailureClass.Timeout => ["reason", "timeout_ms", "elapsed_ms", "operation"],
        FailureClass.Cancellation => ["reason", "trigger", "elapsed_ms", "pending"],
        FailureClass.QueueOverload => ["reason", "queue_depth", "high_water", "dropped"],
        FailureClass.ProcessFailure => ["reason", "program", "exit_code", "sub_id"],
        FailureClass.Rollback => ["reason", "previous_lane", "rollback_lane", "rollback_reason"],
        FailureClass.ShadowDivergence => ["reason", "diverged_count", "total_frames", "baseline_label", "candidate_label"],
        FailureClass.PanicCaught => ["reason", "sub_id", "panic_msg", "effect_type"],
        _ => ["reason", "url", "stage", "attempts", "last_error"]
    };

    public static string SummaryTemplate(FailureClass failureClass) => failureClass switch
    {
        FailureClass.Mismatch => "Frame {frame_idx} diverged: expected {expected_hash}, got {actual_hash} (scenario={scenario}, seed={seed})",
        FailureClass.Timeout => "{operation} timed out after {elapsed_ms}ms (limit: {timeout_ms}ms)",
        FailureClass.Cancellation => "Cancelled by {trigger} after {elapsed_ms}ms ({pending} operations pending)",
        FailureClass.QueueOverload => "Queue overloaded: depth={queue_depth}, high_water={high_water}, dropped={dropped}",
        FailureClass.ProcessFailure => "Process '{program}' exited with code {exit_code} (sub_id={sub_id})",
        FailureClass.Rollback => "Rolled back from {previous_lane} to {rollback_lane}: {rollback_reason}",
        FailureClass.ShadowDivergence => "Shadow diverged: {diverged_count}/{total_frames} frames differ ({baseline_label} vs {candidate_label})",
        FailureClass.PanicCaught => "Panic caught in {effect_type} (id={sub_id}): {panic_msg}",
        _ => "Network failure at stage '{stage}' ({url}): {last_error} after {attempts} attempts"
    };

    public static FailureLogValidation Validate(FailureLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var missing = RequiredFields(entry.Class)
            .Where(field => !entry.Fields.Contains(field))
            .ToArray();
        return new FailureLogValidation(entry.Class, missing, missing.Length == 0);
    }

    public static FailureLogSummary ValidateBatch(IEnumerable<FailureLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var validations = entries.Select(Validate).ToArray();
        var failures = validations.Where(validation => !validation.Passes).ToArray();
        return new FailureLogSummary(validations.Length, failures.Length, failures);
    }

    public static FailureClass? ParseReasonCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        foreach (FailureClass failureClass in Enum.GetValues<FailureClass>())
        {
            if (string.Equals(ReasonCode(failureClass), code, StringComparison.Ordinal))
            {
                return failureClass;
            }
        }

        return null;
    }

    public static string WriteSummary(string runId, FailureLogSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(summary);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-failure-signatures.json");
        File.WriteAllText(path, summary.ToJson());
        return path;
    }
}
