using FrankenTui.Core;

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
    public static string Sanitize(string input) =>
        new(input.Where(c => c is >= (char)32 and <= (char)126 or '\n' or '\r' or '\t').ToArray());
}

/// <summary>Spatial hit-testing index. Matches upstream spatial_hit_index.</summary>
public sealed class SpatialHitIndex
{
    private readonly Dictionary<(ushort, ushort), string> _hits = [];
    public void Register(ushort x, ushort y, string id) => _hits[(x, y)] = id;
    public string? Hit(ushort x, ushort y) => _hits.TryGetValue((x, y), out var id) ? id : null;
    public void Clear() => _hits.Clear();
}
