// Tests for .external/frankentui/crates/ftui-runtime/src/telemetry_schema.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class TelemetrySchemaTests
{
    [Fact] public void SchemaVersionIsSemver()
    {
        var parts = TelemetrySchemaMeta.SchemaVersion.Split('.');
        Assert.Equal(3, parts.Length);
        foreach (var p in parts)
            Assert.True(uint.TryParse(p, out _), $"each semver component must be a number: {p}");
    }

    [Fact] public void AllTargetsAreDotted()
    {
        foreach (var t in TelemetryTarget.All)
        {
            Assert.Contains('.', t);
            Assert.StartsWith("ftui.", t);
        }
    }

    [Fact] public void AllEventsHaveDottedNames()
    {
        foreach (var e in TelemetryEventNames.All)
            Assert.Contains('.', e);
    }

    [Fact] public void AllMetricsAreSnakeCase()
    {
        foreach (var m in TelemetryMetric.All)
            Assert.True(m.All(c => char.IsAsciiLetterLower(c) || c == '_'), $"metric should be snake_case: {m}");
    }

    [Fact] public void NoDuplicateTargets()
    {
        var seen = new HashSet<string>();
        foreach (var t in TelemetryTarget.All)
            Assert.True(seen.Add(t), $"duplicate target: {t}");
    }

    [Fact] public void NoDuplicateEvents()
    {
        var seen = new HashSet<string>();
        foreach (var e in TelemetryEventNames.All)
            Assert.True(seen.Add(e), $"duplicate event: {e}");
    }

    [Fact] public void NoDuplicateMetrics()
    {
        var seen = new HashSet<string>();
        foreach (var m in TelemetryMetric.All)
            Assert.True(seen.Add(m), $"duplicate metric: {m}");
    }
}
