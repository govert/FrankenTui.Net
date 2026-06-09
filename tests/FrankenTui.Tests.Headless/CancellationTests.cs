// Tests for .external/frankentui/crates/ftui-runtime/src/cancellation.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// DIVERGENCE: Uses FtuiCancellationToken/FtuiCancellationSource (see Cancellation.cs header).

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class CancellationTests
{
    [Fact]
    public void TokenStartsUncancelled()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        Assert.False(token.IsCancelled);
        Assert.False(source.IsCancelled);
    }

    [Fact]
    public void CancelPropagatesToToken()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        source.Cancel();
        Assert.True(token.IsCancelled);
    }

    [Fact]
    public void CancelPropagatesToAllClones()
    {
        var source = new FtuiCancellationSource();
        var t1 = source.Token;
        var t2 = source.Token;
        var t3 = source.Token;
        source.Cancel();
        Assert.True(t1.IsCancelled);
        Assert.True(t2.IsCancelled);
        Assert.True(t3.IsCancelled);
    }

    [Fact]
    public void DropSourceDoesNotCancel()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        // Simulate "dropping" the source — let source become unreachable,
        // token holds inner alive via shared reference.
#pragma warning disable xUnit1031
        Task.Run(() => { GC.KeepAlive(token); }).Wait();
#pragma warning restore xUnit1031
        Assert.False(token.IsCancelled);
    }

    [Fact]
    public void WaitTimeoutReturnsTrueWhenAlreadyCancelled()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        source.Cancel();
        Assert.True(token.WaitTimeout(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void WaitTimeoutReturnsFalseOnTimeout()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        Assert.False(token.WaitTimeout(TimeSpan.FromMilliseconds(10)));
    }

    [Fact]
    public void WaitTimeoutWakesOnCancel()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        var result = false;

        var thread = new Thread(() =>
        {
            result = token.WaitTimeout(TimeSpan.FromSeconds(10));
        });
        thread.Start();

        Thread.Sleep(20);
        source.Cancel();
        thread.Join();
        Assert.True(result);
    }

    [Fact]
    public void CancelIsIdempotent()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        source.Cancel();
        source.Cancel();
        source.Cancel();
        Assert.True(token.IsCancelled);
    }

    [Fact]
    public void TokenWorksAcrossThreads()
    {
        var source = new FtuiCancellationSource();
        var token = source.Token;
        var flag = 0;

        var thread = new Thread(() =>
        {
            while (!token.IsCancelled)
            {
                Thread.Sleep(5);
            }
            Interlocked.Exchange(ref flag, 1);
        });
        thread.Start();

        Thread.Sleep(20);
        source.Cancel();
        thread.Join();
        Assert.Equal(1, flag);
    }
}
