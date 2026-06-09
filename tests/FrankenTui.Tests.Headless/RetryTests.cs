// Tests for .external/frankentui/crates/ftui-runtime/src/retry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class RetryTests
{
    [Fact]
    public void FixedBackoffConstantDelay()
    {
        var policy = new RetryPolicy(3, new BackoffStrategy.Fixed(100));
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.Delay(0));
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.Delay(1));
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.Delay(2));
    }

    [Fact]
    public void ExponentialBackoffDoubles()
    {
        var policy = new RetryPolicy(5, new BackoffStrategy.Exponential(100, 5000));
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.Delay(0));
        Assert.Equal(TimeSpan.FromMilliseconds(200), policy.Delay(1));
        Assert.Equal(TimeSpan.FromMilliseconds(400), policy.Delay(2));
        Assert.Equal(TimeSpan.FromMilliseconds(800), policy.Delay(3));
    }

    [Fact]
    public void ExponentialBackoffCapsAtMax()
    {
        var policy = new RetryPolicy(5, new BackoffStrategy.Exponential(1000, 3000));
        Assert.Equal(TimeSpan.FromMilliseconds(1000), policy.Delay(0));
        Assert.Equal(TimeSpan.FromMilliseconds(2000), policy.Delay(1));
        Assert.Equal(TimeSpan.FromMilliseconds(3000), policy.Delay(2)); // capped
        Assert.Equal(TimeSpan.FromMilliseconds(3000), policy.Delay(3)); // capped
    }

    [Fact]
    public void LinearBackoffIncrements()
    {
        var policy = new RetryPolicy(4, new BackoffStrategy.Linear(100, 500));
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.Delay(0));
        Assert.Equal(TimeSpan.FromMilliseconds(200), policy.Delay(1));
        Assert.Equal(TimeSpan.FromMilliseconds(300), policy.Delay(2));
        Assert.Equal(TimeSpan.FromMilliseconds(400), policy.Delay(3));
        Assert.Equal(TimeSpan.FromMilliseconds(500), policy.Delay(4)); // capped
    }

    [Fact]
    public void LinearBackoffCapsAtMax()
    {
        var policy = new RetryPolicy(4, new BackoffStrategy.Linear(200, 500));
        // 200 * 3 = 600, capped at 500
        Assert.Equal(TimeSpan.FromMilliseconds(500), policy.Delay(2));
    }

    [Fact]
    public void NoRetryPolicy()
    {
        var policy = RetryPolicy.NoRetry();
        Assert.Equal(0u, policy.MaxRetries);
    }

    [Fact]
    public void TotalMaxDelayFixed()
    {
        var policy = new RetryPolicy(3, new BackoffStrategy.Fixed(100));
        Assert.Equal(TimeSpan.FromMilliseconds(300), policy.TotalMaxDelay());
    }

    [Fact]
    public void TotalMaxDelayExponential()
    {
        var policy = new RetryPolicy(3, new BackoffStrategy.Exponential(100, 10000));
        // Delays: 100 + 200 + 400 = 700
        Assert.Equal(TimeSpan.FromMilliseconds(700), policy.TotalMaxDelay());
    }

    [Fact]
    public void TotalMaxDelayZeroRetries()
    {
        var policy = RetryPolicy.NoRetry();
        Assert.Equal(TimeSpan.Zero, policy.TotalMaxDelay());
    }

    [Fact]
    public void TotalMaxDelayFixedSaturatesWithoutIterating()
    {
        var policy = new RetryPolicy(uint.MaxValue, new BackoffStrategy.Fixed(ulong.MaxValue));
        Assert.Equal(TimeSpan.MaxValue, policy.TotalMaxDelay());
    }

    [Fact]
    public void TotalMaxDelayLinearHandlesLargeRetryCounts()
    {
        var policy = new RetryPolicy(uint.MaxValue, new BackoffStrategy.Linear(1, 10));
        var expected = TimeSpan.FromMilliseconds(
            (ulong)10 * uint.MaxValue - 45);
        Assert.Equal(expected, policy.TotalMaxDelay());
    }

    [Fact]
    public void TotalMaxDelayExponentialSaturatesAfterCap()
    {
        var policy = new RetryPolicy(6, new BackoffStrategy.Exponential(10, 35));
        Assert.Equal(
            TimeSpan.FromMilliseconds(10 + 20 + 35 * 4),
            policy.TotalMaxDelay());
    }

    [Fact]
    public void TotalMaxDelayMatchesDelaySequenceForRepresentativePolicies()
    {
        var policies = new[]
        {
            new RetryPolicy(5, new BackoffStrategy.Fixed(7)),
            new RetryPolicy(6, new BackoffStrategy.Linear(3, 10)),
            new RetryPolicy(4, new BackoffStrategy.Linear(10, 3)),
            new RetryPolicy(6, new BackoffStrategy.Exponential(2, 9)),
        };

        foreach (var policy in policies)
        {
            double expectedMs = 0;
            for (uint attempt = 0; attempt < policy.MaxRetries; attempt++)
                expectedMs += policy.Delay(attempt).TotalMilliseconds;
            Assert.Equal(expectedMs, policy.TotalMaxDelay().TotalMilliseconds);
        }
    }

    [Fact]
    public void ExponentialBackoffOverflowSaturates()
    {
        var policy = new RetryPolicy(1, new BackoffStrategy.Exponential(ulong.MaxValue / 2, ulong.MaxValue));
        // Should not throw on overflow
        var _ = policy.Delay(30);
    }

    [Fact]
    public void LinearBackoffOverflowSaturates()
    {
        var policy = new RetryPolicy(1, new BackoffStrategy.Linear(ulong.MaxValue / 2, ulong.MaxValue));
        var _ = policy.Delay(30);
    }

    [Fact]
    public void TaskConstructorProducesBackgroundTaskTypeName()
    {
        var cmd = Cmd<string>.Task(() => "hello");
        Assert.Equal("Task", cmd.TypeName);
    }

    [Fact]
    public void TaskWithTimeoutProducesBackgroundTask()
    {
        var cmd = RetryTasks.TaskWithTimeout(
            TimeSpan.FromSeconds(1),
            _ => "result",
            "timeout");
        Assert.Equal("Task", cmd.TypeName);
    }

    [Fact]
    public void TaskWithTimeoutRequestsCancellationOnTimeout()
    {
        var cancelled = 0;
        var workerExited = 0;

        var cmd = RetryTasks.TaskWithTimeout(
            TimeSpan.FromMilliseconds(10),
            token =>
            {
                Interlocked.Exchange(ref cancelled, token.WaitTimeout(TimeSpan.FromSeconds(1)) ? 1 : 0);
                Interlocked.Exchange(ref workerExited, 1);
                return 0;
            },
            999);

        var result = cmd switch
        {
            Cmd<int>.BackgroundTask(_, var work) => work(),
            _ => throw new Exception("Expected BackgroundTask"),
        };

        Assert.Equal(999, result);
        Thread.Sleep(50);
        Assert.Equal(1, Volatile.Read(ref cancelled));
        Assert.Equal(1, Volatile.Read(ref workerExited));
    }

    [Fact]
    public void TaskWithRetryAndTimeoutCancelsEachTimedOutAttempt()
    {
        var attempts = 0;
        var cancelled = 0;

        var policy = new RetryPolicy(1, new BackoffStrategy.Fixed(0));

        var cmd = RetryTasks.TaskWithRetryAndTimeout(
            policy,
            TimeSpan.FromMilliseconds(10),
            token =>
            {
                Interlocked.Increment(ref attempts);
                if (token.WaitTimeout(TimeSpan.FromSeconds(1)))
                    Interlocked.Increment(ref cancelled);
                return (false, "cancelled");
            },
            err => err);

        var result = cmd switch
        {
            Cmd<string>.BackgroundTask(_, var work) => work(),
            _ => throw new Exception("Expected BackgroundTask"),
        };

        Assert.Equal("timeout", result);
        Thread.Sleep(50);
        Assert.Equal(2, Volatile.Read(ref attempts));
        Assert.Equal(2, Volatile.Read(ref cancelled));
    }
}
