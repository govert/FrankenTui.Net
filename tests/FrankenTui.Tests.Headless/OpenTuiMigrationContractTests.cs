using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class OpenTuiMigrationContractTests
{
    [Fact]
    public void OpenTuiContractBundleLoadsAndValidatesUpstreamReference()
    {
        var bundle = OpenTuiMigrationContractBundle.LoadUpstreamReference();
        var summary = bundle.ToSummary();

        Assert.Empty(bundle.Validate());
        Assert.Equal("ready", summary.Status);
        Assert.True(summary.CoverageComplete);
        Assert.True(summary.ClauseTraceability);
        Assert.True(summary.ClauseCount > 0);
        Assert.True(summary.PolicyCellCount > 0);
    }
}
