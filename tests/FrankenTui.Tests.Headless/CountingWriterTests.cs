// Differential contract for crates/ftui-render/src/counting_writer.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class CountingWriterTests
{
    [Fact]
    public void WriteAllAccumulatesBytesAndPreservesContent()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);

        writer.WriteAll("Hello"u8);
        writer.WriteAll(", world!"u8.ToArray());

        Assert.Equal(13UL, writer.BytesWritten);
        Assert.Equal("Hello, world!", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void WriteReturnsTheCountWritten()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);

        Assert.Equal(5, writer.Write("Hello"u8));
        Assert.Equal(5, writer.Write("-Hello-"u8.ToArray(), 1, 5));
        Assert.Equal(10UL, writer.BytesWritten);
    }

    [Fact]
    public void EmptyWritesDoNotChangeCounter()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);

        Assert.Equal(0, writer.Write(ReadOnlySpan<byte>.Empty));
        writer.WriteAll(Array.Empty<byte>());

        Assert.Equal(0UL, writer.BytesWritten);
    }

    [Fact]
    public void ResetIsIdempotentAndDoesNotClearInnerStream()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);
        writer.WriteAll("abc"u8);

        writer.ResetCounter();
        writer.ResetCounter();
        writer.WriteAll("de"u8);

        Assert.Equal(2UL, writer.BytesWritten);
        Assert.Equal("abcde", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void FlushDoesNotChangeCounter()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);
        writer.WriteAll("test"u8);

        writer.Flush();
        writer.Flush();

        Assert.Equal(4UL, writer.BytesWritten);
    }

    [Fact]
    public void InnerAndInnerMutExposeSameStreamAndBypassCounter()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);
        writer.WriteAll("test"u8);

        writer.InnerMut.WriteByte((byte)'!');

        Assert.Same(stream, writer.Inner);
        Assert.Equal("test!", Encoding.UTF8.GetString(stream.ToArray()));
        Assert.Equal(4UL, writer.BytesWritten);
    }

    [Fact]
    public void IntoInnerTransfersOwnershipAndPreservesData()
    {
        var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);
        writer.WriteAll("hello world"u8);

        var transferred = writer.IntoInner();

        Assert.Same(stream, transferred);
        Assert.Equal("hello world", Encoding.UTF8.GetString(transferred.ToArray()));
        Assert.Throws<InvalidOperationException>(() => writer.Flush());
        transferred.Dispose();
    }

    [Fact]
    public void FailedWriteAndFlushDoNotChangeCounter()
    {
        using var stream = new ThrowingStream();
        var writer = new CountingWriter<ThrowingStream>(stream);

        Assert.Throws<IOException>(() => writer.WriteAll("failure"u8));
        Assert.Throws<IOException>(() => writer.Flush());
        Assert.Equal(0UL, writer.BytesWritten);
    }

    [Fact]
    public void NullInnerAndBufferAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CountingWriter<MemoryStream>(null!));
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);
        Assert.Throws<ArgumentNullException>(() => writer.WriteAll((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, 0, 0));
    }

    [Theory]
    [InlineData(100UL, 10UL, 2UL, 10.0, 50.0)]
    [InlineData(10UL, 3UL, 3UL, 3.3333333333333335, 3.3333333333333335)]
    [InlineData(0UL, 0UL, 0UL, 0.0, 0.0)]
    public void PresentStatsCalculatesRatios(
        ulong bytes,
        ulong cells,
        ulong runs,
        double expectedPerCell,
        double expectedPerRun)
    {
        var stats = new PresentStats(bytes, cells, runs, TimeSpan.FromMicroseconds(50));

        Assert.Equal(expectedPerCell, stats.BytesPerCell, 12);
        Assert.Equal(expectedPerRun, stats.BytesPerRun, 12);
    }

    [Fact]
    public void PresentStatsDefaultCloneEqualityAndLogMatchSourceContract()
    {
        var stats = PresentStats.Default;

        Assert.Equal(new PresentStats(0, 0UL, 0UL, TimeSpan.Zero), stats);
        Assert.Equal(stats, stats with { });
        stats.Log();
    }

    [Fact]
    public void WithinBudgetIncludesExactBoundary()
    {
        Assert.True(new PresentStats(440, 10UL, 2UL, TimeSpan.Zero).WithinBudget);
        Assert.False(new PresentStats(441, 10UL, 2UL, TimeSpan.Zero).WithinBudget);
    }

    [Theory]
    [InlineData(0UL, 0UL, 20UL)]
    [InlineData(1UL, 1UL, 70UL)]
    [InlineData(10UL, 2UL, 440UL)]
    [InlineData(1UL, 100UL, 1_060UL)]
    [InlineData(1_000UL, 1UL, 40_030UL)]
    public void ExpectedMaxBytesMatchesRustFormula(ulong cells, ulong runs, ulong expected)
    {
        Assert.Equal(expected, PresentBudget.ExpectedMaxBytes(cells, runs));
    }

    [Fact]
    public void BudgetConstantsMatchSource()
    {
        Assert.Equal(40UL, PresentBudget.BytesPerCellMax);
        Assert.Equal(20UL, PresentBudget.SyncOverhead);
        Assert.Equal(10UL, PresentBudget.BytesPerCursorMove);
    }

    [Fact]
    public void ManagedSignedAdaptersRejectValuesRustUsizeCannotRepresent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PresentBudget.ExpectedMaxBytes(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PresentStats(0, -1, 0, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => StatsCollector.Start(0, -1));
    }

    [Fact]
    public void BudgetOverflowIsExplicit()
    {
        Assert.Throws<OverflowException>(() => PresentBudget.ExpectedMaxBytes(ulong.MaxValue, 0));
        Assert.Throws<OverflowException>(() => PresentBudget.ExpectedMaxBytes(0, ulong.MaxValue));
    }

    [Fact]
    public void StatsCollectorCapturesDimensionsBytesAndMonotonicDuration()
    {
        var collector = StatsCollector.Start(10, 2);
        Thread.SpinWait(10_000);

        var stats = collector.Finish(150);

        Assert.Equal(10UL, stats.CellsChanged);
        Assert.Equal(2UL, stats.RunCount);
        Assert.Equal(150UL, stats.BytesEmitted);
        Assert.True(stats.Duration >= TimeSpan.Zero);
        Assert.Throws<InvalidOperationException>(() => collector.Finish(150));
    }

    [Fact]
    public void FullPresentWorkflowStaysWithinBudget()
    {
        using var stream = new MemoryStream();
        var writer = new CountingWriter<MemoryStream>(stream);
        var collector = StatsCollector.Start(5, 1);

        writer.WriteAll("\u001b[1;1H"u8);
        writer.WriteAll("\u001b[0m"u8);
        writer.WriteAll("Hello"u8);
        writer.Flush();
        var stats = collector.Finish(writer.BytesWritten);

        Assert.Equal(15UL, stats.BytesEmitted);
        Assert.True(stats.WithinBudget);
    }

    [Theory]
    [InlineData(35UL, 1UL, 1UL, true)]
    [InlineData(2_500UL, 80UL, 1UL, true)]
    [InlineData(50_000UL, 1_920UL, 24UL, true)]
    [InlineData(500UL, 10UL, 2UL, false)]
    public void RepresentativeFrameBudgetsMatchSource(
        ulong bytes,
        ulong cells,
        ulong runs,
        bool expected)
    {
        Assert.Equal(expected, new PresentStats(bytes, cells, runs, TimeSpan.Zero).WithinBudget);
    }

    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new IOException("flush failed");

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("write failed");

        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("write failed");
    }
}
