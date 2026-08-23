// Upstream source: crates/ftui-extras/src/pty_capture.rs
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// 13 config tests + 14 integration tests (cross-platform: Windows ConPTY + Unix PTY).

using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class PtyCaptureTests
{
    private static string Shell => OperatingSystem.IsWindows() ? "cmd.exe" : "sh";
    private static string Echo(string text) => OperatingSystem.IsWindows() ? $"/c echo {text}" : $"-c \"printf '{text}'\"";
    private static string Cmd(string cmd) => OperatingSystem.IsWindows() ? $"/c {cmd}" : $"-c \"{cmd}\"";

    // ===== Config tests (13) =====
    [Fact] public void DefaultConfigValues() { var c = new PtyCaptureConfig(); Assert.Equal(80, c.Cols); Assert.Equal(24, c.Rows); Assert.Equal("xterm-256color", c.Term); Assert.Empty(c.Env); }
    [Fact] public void WithSizeOverridesDimensions() { var c = new PtyCaptureConfig().WithSize(120, 50); Assert.Equal(120, c.Cols); Assert.Equal(50, c.Rows); }
    [Fact] public void WithTermOverridesTerminal() { Assert.Equal("dumb", new PtyCaptureConfig().WithTerm("dumb").Term); }
    [Fact] public void WithEnvAppends() { var c = new PtyCaptureConfig().WithEnv("K", "V"); Assert.Single(c.Env); Assert.Equal("K", c.Env[0].Key); }
    [Fact] public void WithSizeZero() { var c = new PtyCaptureConfig().WithSize(0, 0); Assert.Equal(0, c.Cols); }
    [Fact] public void WithSizeLarge() { var c = new PtyCaptureConfig().WithSize(500, 200); Assert.Equal(500, c.Cols); }
    [Fact] public void WithTermEmptyString() { Assert.Equal("", new PtyCaptureConfig().WithTerm("").Term); }
    [Fact] public void WithEnvPreservesOrder() { var c = new PtyCaptureConfig().WithEnv("B","1").WithEnv("A","2"); Assert.Equal("B", c.Env[0].Key); Assert.Equal("A", c.Env[1].Key); }
    [Fact] public void WithEnvAllowsDuplicateKeys() { var c = new PtyCaptureConfig().WithEnv("K","a").WithEnv("K","b"); Assert.Equal(2, c.Env.Count); }
    [Fact] public void BuilderChainingAllMethods() { var c = new PtyCaptureConfig().WithSize(132,43).WithTerm("screen").WithEnv("A","1").WithEnv("B","2"); Assert.Equal(132, c.Cols); Assert.Equal(2, c.Env.Count); }
    [Fact] public void DefaultDoesNotSetEnv() { Assert.Empty(new PtyCaptureConfig().Env); }
    [Fact] public void DefaultTermIsXterm256Color() { Assert.Equal("xterm-256color", new PtyCaptureConfig().Term); }
    [Fact] public void ConfigDebugImpl() { Assert.NotNull(new PtyCaptureConfig().ToString()); }

    // ===== Integration tests (14) =====

    // Exercises the native backend DIRECTLY (no Process fallback) so the ConPTY
    // path itself is validated. On Windows this hits WindowsPtySession; on Unix,
    // UnixPtySession. Skips on platforms without native PTY support.
    [Fact] public void NativePtyReadsOutput()
    {
        if (!PtyNative.IsAvailable) return;
        var args = OperatingSystem.IsWindows()
            ? "/c echo hello-conpty & ping -n 2 127.0.0.1 > nul & echo second-line"
            : Echo("hello-conpty");
        using var s = PtyNative.Spawn(new PtyCaptureConfig(), Shell, args);
        var start = DateTime.UtcNow;
        var all = new System.Collections.Generic.List<byte>();
        while (DateTime.UtcNow - start < TimeSpan.FromSeconds(6))
        {
            var chunk = s.ReadAvailableWithTimeout(TimeSpan.FromMilliseconds(50));
            if (chunk.Length > 0) all.AddRange(chunk);
            else if (s.IsEof) break;
        }
        var text = System.Text.Encoding.UTF8.GetString(all.ToArray());
        Assert.Contains("hello-conpty", text);
        Assert.Contains("second-line", text);
        Assert.True(s.IsEof, "expected EOF after child exit");
    }

    [Fact] public void PtyReadsOutput()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, Echo("hello-pty"));
        var out_ = DrainUntilEof(cap, TimeSpan.FromSeconds(3));
        Assert.Contains("hello-pty", System.Text.Encoding.UTF8.GetString(out_));
    }

    [Fact] public void PtyTimeoutBoundaryAllowsSubsequentDelayedOutput()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, OperatingSystem.IsWindows() ? "/c ping -n 2 127.0.0.1 >nul & echo late-output" : "-c \"sleep 0.15; printf late-output\"");
        var early = cap.ReadAvailableWithTimeout(TimeSpan.FromMilliseconds(20));
        var output = new System.Collections.Generic.List<byte>(early);
        var timeout = TimeSpan.FromSeconds(3);
        var wait = System.Diagnostics.Stopwatch.StartNew();
        while (wait.Elapsed < timeout)
        {
            var text = System.Text.Encoding.UTF8.GetString([.. output]);
            if (text.Contains("late-output", StringComparison.Ordinal)) break;

            var remaining = timeout - wait.Elapsed;
            var next = cap.ReadAvailableWithTimeout(remaining);
            if (next.Length > 0) output.AddRange(next);
            else if (cap.IsEof) break;
        }

        Assert.Contains("late-output", System.Text.Encoding.UTF8.GetString([.. output]));
    }

    [Fact] public void PtyPartialReadsAcrossCalls()
    {
        var command = OperatingSystem.IsWindows()
            ? "/d /q /c \"echo part-1 & set /p value= & echo part-2\""
            : "-c \"printf part-1; read value; printf part-2\"";
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, command);

        var timeout = TimeSpan.FromSeconds(5);
        var firstPhase = new System.Collections.Generic.List<byte>();
        var firstWait = System.Diagnostics.Stopwatch.StartNew();
        while (firstWait.Elapsed < timeout)
        {
            var text = System.Text.Encoding.UTF8.GetString([.. firstPhase]);
            if (text.Contains("part-1", StringComparison.Ordinal)) break;

            var remaining = timeout - firstWait.Elapsed;
            var next = cap.ReadAvailableWithTimeout(remaining);
            if (next.Length > 0) firstPhase.AddRange(next);
            else if (cap.IsEof) break;
        }
        firstWait.Stop();

        Assert.Contains("part-1", System.Text.Encoding.UTF8.GetString([.. firstPhase]));
        Assert.True(
            firstWait.Elapsed < TimeSpan.FromSeconds(4),
            $"Expected the first-data signal before the {timeout} timeout; elapsed {firstWait.Elapsed}.");

        cap.SendInput(System.Text.Encoding.UTF8.GetBytes(OperatingSystem.IsWindows() ? "go\r\n" : "go\n"));
        var combined = new System.Collections.Generic.List<byte>(firstPhase);
        var secondWait = System.Diagnostics.Stopwatch.StartNew();
        while (secondWait.Elapsed < timeout)
        {
            var text = System.Text.Encoding.UTF8.GetString([.. combined]);
            if (text.Contains("part-2", StringComparison.Ordinal)) break;

            var remaining = timeout - secondWait.Elapsed;
            var next = cap.ReadAvailableWithTimeout(remaining);
            if (next.Length > 0) combined.AddRange(next);
            else if (cap.IsEof) break;
        }

        var output = System.Text.Encoding.UTF8.GetString([.. combined]);
        Assert.Contains("part-1", output);
        Assert.Contains("part-2", output);
    }

    [Fact] public void PtySendInput()
    {
        if (OperatingSystem.IsWindows()) return; // cat has different behavior on Windows
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), "cat", "");
        Thread.Sleep(200);
        cap.SendInput(System.Text.Encoding.UTF8.GetBytes("hello\n"));
        var out_ = DrainUntilEof(cap, TimeSpan.FromSeconds(2));
        Assert.Contains("hello", System.Text.Encoding.UTF8.GetString(out_));
    }

    [Fact] public void PtySendEmptyInputIsNoop()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, Cmd("echo done"));
        cap.SendInput([]);
        var out_ = DrainUntilEof(cap, TimeSpan.FromSeconds(2));
        Assert.NotEmpty(out_);
    }

    [Fact] public void PtyWaitReturnsExitStatus()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, Cmd("exit 0"));
        Assert.Equal(0, cap.Wait());
    }

    [Fact] public void PtyWaitNonzeroExit()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, OperatingSystem.IsWindows() ? "/c exit /b 42" : "-c \"exit 42\"");
        Assert.Equal(42, cap.Wait());
    }

    [Fact] public void PtyChildPidIsSome()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, Cmd("exit 0"));
        Assert.NotNull(cap.ChildPid); Assert.True(cap.ChildPid > 0);
    }

    [Fact] public void PtyIsEofInitiallyFalse()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, OperatingSystem.IsWindows() ? "/c ping -n 3 127.0.0.1" : "-c \"sleep 0.5\"");
        Assert.False(cap.IsEof);
    }

    [Fact] public void PtyEofAfterChildExits()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, Echo("eof-test"));
        DrainUntilEof(cap, TimeSpan.FromSeconds(2));
        Assert.True(cap.IsEof);
    }

    [Fact] public void PtyReadAvailableNonblocking()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, OperatingSystem.IsWindows() ? "/c ping -n 2 127.0.0.1 >nul" : "-c \"sleep 1; printf never\"");
        var out_ = cap.ReadAvailable();
    }

    [Fact] public void PtyReadAfterEofReturnsEmpty()
    {
        using var cap = PtyCapture.Spawn(new PtyCaptureConfig(), Shell, Cmd("echo done"));
        DrainUntilEof(cap, TimeSpan.FromSeconds(2));
        Assert.True(cap.IsEof); Assert.Empty(cap.ReadAvailable());
    }

    [Fact] public void PtyCustomSize()
    {
        var cfg = new PtyCaptureConfig().WithSize(132, 50);
        using var cap = PtyCapture.Spawn(cfg, Shell, Cmd("echo ok"));
        var out_ = DrainUntilEof(cap, TimeSpan.FromSeconds(2));
        Assert.NotEmpty(out_);
    }

    [Fact] public void PtyCustomEnv()
    {
        var cfg = new PtyCaptureConfig().WithEnv("MY_VAR", "custom_value");
        using var cap = PtyCapture.Spawn(cfg, Shell, OperatingSystem.IsWindows() ? "/c echo %MY_VAR%" : "-c \"printf $MY_VAR\"");
        var out_ = DrainUntilEof(cap, TimeSpan.FromSeconds(2));
        Assert.Contains("custom_value", System.Text.Encoding.UTF8.GetString(out_));
    }

    // DIVERGENCE: pty_capture_routes_through_log_sink (needs LogSink integration),
    // pty_capture_deterministic_checksum (needs JSONL env harness),
    // pty_capture_drop_does_not_block_when_background_process_keeps_pty_open (complex threading harness)
    // pty_capture_debug_format (ToString not Debug impl)
    // are documented as platform/Rust-specific.

    private static byte[] DrainUntilEof(PtyCapture cap, TimeSpan deadline)
    {
        var start = DateTime.UtcNow;
        var all = new System.Collections.Generic.List<byte>();
        while (DateTime.UtcNow - start < deadline)
        {
            var chunk = cap.ReadAvailableWithTimeout(TimeSpan.FromMilliseconds(50));
            if (chunk.Length > 0) all.AddRange(chunk);
            else if (cap.IsEof) break;
            else Thread.Sleep(10);
        }
        return [.. all];
    }
}
