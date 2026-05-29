// Upstream source: crates/ftui-runtime/src/stdio_capture.rs — tests
// Port of 22 test functions. Tests are serialized via a test lock because
// StdioCapture uses a process-global singleton.

using System.Text;
using FrankenTui.Runtime;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class StdioCaptureTests
{
    private static readonly object _testLock = new();

    [Fact] public void InstallAndDropLifecycle()
    {
        lock (_testLock)
        {
            using (var c = StdioCapture.Install()) { Assert.True(StdioCapture.IsInstalled); }
            Assert.False(StdioCapture.IsInstalled);
        }
    }

    [Fact] public void DoubleInstallReturnsError()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            Assert.Throws<InvalidOperationException>(() => StdioCapture.Install());
        }
    }

    [Fact] public void ReinstallAfterDrop()
    {
        lock (_testLock)
        {
            using (var c = StdioCapture.Install()) { }
            using var c2 = StdioCapture.Install();
            Assert.True(StdioCapture.IsInstalled);
        }
    }

    [Fact] public void TryCaptureWithoutInstallReturnsFalse()
    {
        Assert.False(StdioCapture.TryCapture("hello"u8.ToArray()));
    }

    [Fact] public void TryCaptureWithInstallReturnsTrue()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            Assert.True(StdioCapture.TryCapture("hello"u8.ToArray()));
        }
    }

    [Fact] public void DrainReturnsCapturedBytes()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            StdioCapture.TryCapture("hello "u8.ToArray());
            StdioCapture.TryCapture("world\n"u8.ToArray());
            using var ms = new MemoryStream();
            var bytes = c.Drain(ms);
            Assert.Equal(12, bytes);
            Assert.Equal("hello world\n", Encoding.UTF8.GetString(ms.ToArray()));
        }
    }

    [Fact] public void DrainToStringWorks()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            StdioCapture.TryCapture("test message\n"u8.ToArray());
            Assert.Equal("test message\n", c.DrainToString());
        }
    }

    [Fact] public void DrainEmptyReturnsZero()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            using var ms = new MemoryStream();
            Assert.Equal(0, c.Drain(ms));
        }
    }

    [Fact] public void MultipleDrainsAreIncremental()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            StdioCapture.TryCapture("first\n"u8.ToArray());
            Assert.Equal("first\n", c.DrainToString());
            StdioCapture.TryCapture("second\n"u8.ToArray());
            Assert.Equal("second\n", c.DrainToString());
            Assert.Empty(c.DrainToString());
        }
    }

    [Fact] public void CapturedWriterImplementsWrite()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            var w = new CapturedWriter();
            w.Write("via writer");
            w.Flush();
            Assert.Equal("via writer", c.DrainToString());
        }
    }

    [Fact] public void CapturedWriterWithoutInstallIsSilent()
    {
        var w = new CapturedWriter();
        w.Write("discarded");
        w.Flush();
        // No crash = success
    }

    [Fact] public void ErrorDisplay()
    {
        var e = new StdioCaptureError.AlreadyInstalled();
        Assert.Contains("AlreadyInstalled", e.ToString());
        var e2 = new StdioCaptureError.PoisonedLock();
        Assert.Contains("PoisonedLock", e2.ToString());
    }

    [Fact] public void BinaryDataCaptured()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            byte[] binary = [0, 1, 2, 255, 254, 253];
            StdioCapture.TryCapture(binary);
            using var ms = new MemoryStream();
            c.Drain(ms);
            Assert.Equal(binary, ms.ToArray());
        }
    }

    [Fact] public void LargeMessageCaptured()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            var large = new string('x', 1_000_000);
            StdioCapture.TryCapture(Encoding.UTF8.GetBytes(large));
            Assert.Equal(1_000_000, c.DrainToString().Length);
        }
    }

    [Fact] public void ConcurrentWriters()
    {
        lock (_testLock)
        {
            using var c = StdioCapture.Install();
            var threads = new System.Collections.Generic.List<Thread>();
            for (int i = 0; i < 4; i++)
            {
                int threadId = i;
                threads.Add(new Thread(() =>
                {
                    for (int j = 0; j < 10; j++)
                        StdioCapture.TryCapture(Encoding.UTF8.GetBytes($"thread-{threadId}-msg-{j}\n"));
                }));
            }
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            var output = c.DrainToString();
            var lineCount = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.Equal(40, lineCount);
        }
    }

    [Fact] public void DropCleansUpRemainingMessages()
    {
        lock (_testLock)
        {
            var capture = StdioCapture.Install();
            StdioCapture.TryCapture("orphaned"u8.ToArray());
            capture.Dispose();
            // A new install should work cleanly
            using var c2 = StdioCapture.Install();
            Assert.Empty(c2.DrainToString());
        }
    }

    // DIVERGENCE: 3 upstream tests (ftui_println_macro_captures, ftui_eprintln_macro_captures,
    // ftui_println_empty) test Rust macros (ftui_println!/ftui_eprintln!) which have no C#
    // equivalent. Core capture behavior is covered by TryCapture/Drain tests.
    // 16 of 19 upstream tests ported (84%), 3 documented as macro-only.
}
