using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class ClipboardTests
{
    [Fact] public void WriteAndReadRoundTrip()
    {
        ClipboardSystem.Clear();
        ClipboardSystem.Write("hello clipboard");
        Assert.Equal("hello clipboard", ClipboardSystem.Read());
    }

    [Fact] public void ReadEmptyReturnsNull()
    {
        ClipboardSystem.Clear();
        Assert.Null(ClipboardSystem.Read());
    }

    [Fact] public void Clear()
    {
        ClipboardSystem.Write("data");
        ClipboardSystem.Clear();
        Assert.Null(ClipboardSystem.Read());
    }

    [Fact] public void Overwrite()
    {
        ClipboardSystem.Write("first");
        ClipboardSystem.Write("second");
        Assert.Equal("second", ClipboardSystem.Read());
    }
}
