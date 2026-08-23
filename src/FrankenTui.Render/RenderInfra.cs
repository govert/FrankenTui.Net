namespace FrankenTui.Render;

/// <summary>Headless renderer that outputs directly to a Buffer. Matches upstream headless.</summary>
public static class HeadlessRenderer
{
    public static string RenderToPlainText(Action<Buffer> renderFn, ushort width, ushort height)
    {
        var buffer = new Buffer(width, height);
        renderFn(buffer);
        return HeadlessBufferView.ScreenString(buffer);
    }
}

// LinkRegistry is the full 1-to-1 port in LinkRegistry.cs; the earlier stub here
// was a stash-restore artifact and has been removed to avoid a duplicate definition.

/// <summary>Output sanitization for terminal safety. Matches upstream sanitize.</summary>
public static class OutputSanitizer
{
    public static string Sanitize(string input) => global::FrankenTui.Render.Sanitize.SanitizeString(input);
}
