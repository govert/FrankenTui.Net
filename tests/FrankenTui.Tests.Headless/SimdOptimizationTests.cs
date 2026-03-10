using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Simd;
using FrankenTui.Testing.Harness;
using FrankenTui.Text;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class SimdOptimizationTests
{
    [Fact]
    public void SimdAcceleratorsPreserveDiffAndWrapResults()
    {
        var previousEnabled = SimdAccelerators.IsEnabled;
        SimdAccelerators.Disable();

        try
        {
            var oldBuffer = new RenderBuffer(80, 24);
            var newBuffer = new RenderBuffer(80, 24);
            var accent = FrankenTui.Style.UiStyle.Accent.ToCell('x');
            for (ushort row = 0; row < 24; row += 3)
            {
                BufferPainter.WriteText(newBuffer, 2, row, "frankentui", accent);
            }

            var baselineDiff = BufferDiff.Compute(oldBuffer, newBuffer).Changes.ToArray();
            var baselineWrap = TextWrapper.WrapLine(
                "alpha beta gamma delta epsilon zeta eta theta iota kappa",
                18,
                TextWrapMode.Word).ToArray();

            SimdAccelerators.EnableIfSupported();

            var simdDiff = BufferDiff.Compute(oldBuffer, newBuffer).Changes.ToArray();
            var simdWrap = TextWrapper.WrapLine(
                "alpha beta gamma delta epsilon zeta eta theta iota kappa",
                18,
                TextWrapMode.Word).ToArray();

            Assert.Equal(baselineDiff, simdDiff);
            Assert.Equal(baselineWrap, simdWrap);
        }
        finally
        {
            if (!previousEnabled)
            {
                SimdAccelerators.Disable();
            }
            else
            {
                SimdAccelerators.EnableIfSupported();
            }
        }
    }

    [Fact]
    public void BenchmarkRunnerLeavesSimdStateStable()
    {
        var previousEnabled = SimdAccelerators.IsEnabled;
        SimdAccelerators.Disable();

        try
        {
            var budgets = PerformanceBenchmarkRunner.LoadBudgets(PerformanceBenchmarkRunner.DefaultBudgetPath);
            var suite = PerformanceBenchmarkRunner.RunDefault(budgets);

            Assert.Equal(budgets.Count, suite.Measurements.Count);
            Assert.False(SimdAccelerators.IsEnabled);
        }
        finally
        {
            if (previousEnabled)
            {
                SimdAccelerators.EnableIfSupported();
            }
        }
    }

    [Fact]
    public void SimdCapabilitiesAreReported()
    {
        var capabilities = SimdAccelerators.Capabilities;

        Assert.True(capabilities.ByteLaneCount >= 1);
        Assert.Contains("vector=", capabilities.Summary);
    }
}
