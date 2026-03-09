using System.Text;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Tty;

namespace FrankenTui.Tests.Headless;

public sealed class TerminalKernelTests
{
    [Fact]
    public void InputParserHandlesKeysPasteAndMouse()
    {
        var parser = new TerminalInputParser();
        var events = parser.Parse("A\u001b[A\u001b[200~paste\u001b[201~\u001b[<0;3;4M");

        Assert.Collection(
            events,
            item => Assert.Equal(TerminalKey.Character, Assert.IsType<KeyTerminalEvent>(item).Gesture.Key),
            item => Assert.Equal(TerminalKey.Up, Assert.IsType<KeyTerminalEvent>(item).Gesture.Key),
            item => Assert.Equal("paste", Assert.IsType<PasteTerminalEvent>(item).Text),
            item =>
            {
                var mouse = Assert.IsType<MouseTerminalEvent>(item);
                Assert.Equal((ushort)2, mouse.Gesture.Column);
                Assert.Equal((ushort)3, mouse.Gesture.Row);
                Assert.Equal(TerminalMouseButton.Left, mouse.Gesture.Button);
            });
    }

    [Fact]
    public void EventCoalescerKeepsLastResizeAndMouseMove()
    {
        var coalesced = TerminalEventCoalescer.Coalesce(
        [
            TerminalEvent.Resize(new Size(10, 10)),
            TerminalEvent.Resize(new Size(20, 5)),
            TerminalEvent.Mouse(new MouseGesture(1, 1, TerminalMouseButton.None, TerminalMouseKind.Move)),
            TerminalEvent.Mouse(new MouseGesture(4, 2, TerminalMouseButton.None, TerminalMouseKind.Move))
        ]);

        Assert.Equal(2, coalesced.Count);
        Assert.Equal(new Size(20, 5), Assert.IsType<ResizeTerminalEvent>(coalesced[0]).Size);
        var mouse = Assert.IsType<MouseTerminalEvent>(coalesced[1]);
        Assert.Equal((ushort)4, mouse.Gesture.Column);
    }

    [Fact]
    public async Task TerminalSessionEmitsBalancedLifecycleSequences()
    {
        var backend = new MemoryTerminalBackend(new Size(30, 8));
        await using var session = new TerminalSession(backend, new TerminalSessionOptions { UseMouseTracking = true });

        await session.EnterAsync();
        var transcript = backend.DrainOutput();
        Assert.Contains("\u001b[?1049h", transcript);
        Assert.Contains("\u001b[?25l", transcript);
        Assert.Contains("\u001b[?1004h", transcript);
        Assert.Contains("\u001b[?2004h", transcript);
        Assert.Contains("\u001b[?1003h", transcript);

        await session.DisposeAsync();
        var cleanup = backend.DrainOutput();
        Assert.Contains("\u001b[?1049l", cleanup);
        Assert.Contains("\u001b[?25h", cleanup);
        Assert.Contains("\u001b[?1004l", cleanup);
        Assert.Contains("\u001b[?2004l", cleanup);
        Assert.Contains("\u001b[?1006l", cleanup);
    }

    [Fact]
    public void SingleWriterGateRejectsSecondLease()
    {
        var gate = new SingleWriterGate();
        using var lease = gate.Acquire();

        Assert.Throws<InvalidOperationException>(() => gate.Acquire());
    }

    [Fact]
    public void TerminalHostMatrixCoversPrimaryPlatforms()
    {
        Assert.Contains(TerminalHostMatrix.Profiles, static profile => profile.Platform == "linux");
        Assert.Contains(TerminalHostMatrix.Profiles, static profile => profile.Platform == "macos");
        Assert.Contains(TerminalHostMatrix.Profiles, static profile => profile.Platform == "windows");
    }
}
