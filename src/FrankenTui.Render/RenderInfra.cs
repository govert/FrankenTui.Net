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

/// <summary>OSC-8 hyperlink registry. Matches upstream link_registry.</summary>
public sealed class LinkRegistry
{
    private readonly Dictionary<string, string> _links = [];
    private int _nextId = 1;

    public int Register(string url, string? label = null) { var id = _nextId++; _links[$"link_{id}"] = url; return id; }
    public string? GetUrl(int id) => _links.TryGetValue($"link_{id}", out var url) ? url : null;
    public void Clear() { _links.Clear(); _nextId = 1; }
}

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
