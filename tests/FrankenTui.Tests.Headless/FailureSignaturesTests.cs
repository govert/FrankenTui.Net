using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class FailureSignaturesTests
{
    [Fact]
    public void FailureSignatureValidationRequiresExpectedFields()
    {
        var validation = FailureSignatures.Validate(
            new FailureLogEntry(
                FailureClass.Timeout,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "reason",
                    "timeout_ms"
                }));

        Assert.False(validation.Passes);
        Assert.Contains("elapsed_ms", validation.MissingFields);
        Assert.Contains("operation", validation.MissingFields);
    }

    [Fact]
    public void FailureSignatureParserRecognizesStableReasonCodes()
    {
        Assert.Equal(FailureClass.QueueOverload, FailureSignatures.ParseReasonCode("QUEUE_OVERLOAD"));
        Assert.Null(FailureSignatures.ParseReasonCode("UNKNOWN"));
    }

    [Fact]
    public void FailureSignatureBatchSummaryCountsFailures()
    {
        var summary = FailureSignatures.ValidateBatch(
        [
            new FailureLogEntry(
                FailureClass.ProcessFailure,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "reason",
                    "program",
                    "exit_code",
                    "sub_id"
                }),
            new FailureLogEntry(
                FailureClass.Rollback,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "reason",
                    "previous_lane"
                })
        ]);

        Assert.Equal(2, summary.EntryCount);
        Assert.Equal(1, summary.FailureCount);
    }
}
