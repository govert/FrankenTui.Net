using System.Runtime.InteropServices;
using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public enum MetricCategory
{
    Latency,
    Throughput,
    OutputCost,
    Memory
}

public enum FixtureFamily
{
    Render,
    Runtime,
    Doctor,
    Challenge
}

public enum StabilityClass
{
    Stable,
    Moderate,
    Unstable
}

public sealed record Sample(
    string Metric,
    MetricCategory Category,
    double Value,
    string Unit)
{
    public static Sample LatencyUs(string metric, long valueUs) => new(metric, MetricCategory.Latency, valueUs, "us");

    public static Sample ThroughputOps(string metric, double opsPerSecond) => new(metric, MetricCategory.Throughput, opsPerSecond, "ops/s");

    public static Sample OutputCost(string metric, long count) => new(metric, MetricCategory.OutputCost, count, "count");

    public static Sample MemoryBytes(string metric, long bytes) => new(metric, MetricCategory.Memory, bytes, "bytes");
}

public sealed record Percentiles(
    double P50,
    double P95,
    double P99,
    double P999,
    double Min,
    double Max);

public sealed record MetricBaseline(
    string Metric,
    MetricCategory Category,
    string Unit,
    int SampleCount,
    double Mean,
    double StandardDeviation,
    double CoefficientOfVariation,
    StabilityClass Stability,
    Percentiles Percentiles);

public sealed record EnvironmentFingerprint(
    string RuntimeVersion,
    string FrameworkDescription,
    string OperatingSystem,
    string ProcessArchitecture,
    int CpuCount,
    string Configuration)
{
    public static EnvironmentFingerprint Capture() =>
        new(
            Environment.Version.ToString(),
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.ProcessorCount,
#if DEBUG
            "debug"
#else
            "release"
#endif
        );
}

public sealed record FixtureBaseline(
    string FixtureId,
    FixtureFamily Family,
    EnvironmentFingerprint Environment,
    IReadOnlyList<MetricBaseline> Metrics)
{
    public bool IsStable => Metrics.All(static metric => metric.Stability is not StabilityClass.Unstable);

    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public sealed class BaselineCapture
{
    private readonly string _fixtureId;
    private readonly FixtureFamily _family;
    private readonly List<Sample> _samples = [];

    public BaselineCapture(string fixtureId, FixtureFamily family)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        _fixtureId = fixtureId;
        _family = family;
    }

    public void RecordSample(Sample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        _samples.Add(sample);
    }

    public void RecordSamples(IEnumerable<Sample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        foreach (var sample in samples)
        {
            RecordSample(sample);
        }
    }

    public FixtureBaseline Finalize(EnvironmentFingerprint? environment = null)
    {
        var metrics = _samples
            .GroupBy(static sample => (sample.Metric, sample.Category, sample.Unit))
            .OrderBy(static group => group.Key.Metric, StringComparer.Ordinal)
            .Select(BuildMetricBaseline)
            .ToArray();

        return new FixtureBaseline(_fixtureId, _family, environment ?? EnvironmentFingerprint.Capture(), metrics);
    }

    private static MetricBaseline BuildMetricBaseline(
        IGrouping<(string Metric, MetricCategory Category, string Unit), Sample> group)
    {
        var values = group.Select(static sample => sample.Value).OrderBy(static value => value).ToArray();
        if (values.Length == 0)
        {
            return new MetricBaseline(
                group.Key.Metric,
                group.Key.Category,
                group.Key.Unit,
                0,
                0,
                0,
                0,
                StabilityClass.Stable,
                new Percentiles(0, 0, 0, 0, 0, 0));
        }

        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Length;
        var standardDeviation = Math.Sqrt(variance);
        var coefficient = mean == 0 ? 0 : standardDeviation / mean;

        return new MetricBaseline(
            group.Key.Metric,
            group.Key.Category,
            group.Key.Unit,
            values.Length,
            mean,
            standardDeviation,
            coefficient,
            ClassifyStability(coefficient),
            new Percentiles(
                Percentile(values, 0.50),
                Percentile(values, 0.95),
                Percentile(values, 0.99),
                Percentile(values, 0.999),
                values[0],
                values[^1]));
    }

    private static StabilityClass ClassifyStability(double coefficientOfVariation) =>
        coefficientOfVariation switch
        {
            < 0.05 => StabilityClass.Stable,
            < 0.15 => StabilityClass.Moderate,
            _ => StabilityClass.Unstable
        };

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Clamp(Math.Ceiling(sortedValues.Count * percentile) - 1, 0, sortedValues.Count - 1);
        return sortedValues[index];
    }
}
