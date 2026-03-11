using FrankenTui.Extras;
using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class HostedParityRuntimeCaptureTests
{
    [Fact]
    public async Task HostedParityRuntimeCaptureProducesReplayTraceAndDiffArtifacts()
    {
        var capture = await HostedParityRuntimeHarness.CaptureAsync(
            "vrf357-runtime-capture",
            HostedParityScenarioId.Tooling,
            width: 64,
            height: 18,
            events: []);
        var artifacts = capture.WriteArtifacts("vrf357-runtime-capture");

        Assert.Equal(capture.Events.Count + 1, capture.ReplayTape.Entries.Count);
        Assert.Equal(capture.ReplayTape.Entries.Count, capture.Trace.Entries.Count);
        Assert.Equal(capture.ReplayTape.Entries.Count, capture.DiffEvidence.Decisions.Count);
        Assert.Contains("FrankenTui.Net", capture.Evidence.Web.Title);
        Assert.Contains("tooling", capture.Evidence.Json, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(capture.TerminalTranscript));

        Assert.True(File.Exists(artifacts["replay_tape"]));
        Assert.True(File.Exists(artifacts["runtime_trace"]));
        Assert.True(File.Exists(artifacts["diff_evidence"]));
        Assert.True(File.Exists(artifacts["event_script"]));
        Assert.True(File.Exists(artifacts["terminal_transcript"]));
    }
}
