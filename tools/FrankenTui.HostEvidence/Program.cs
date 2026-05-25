using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Widgets;

const string evidenceDir = "artifacts/host-evidence";
Directory.CreateDirectory(evidenceDir);

var host = new
{
    os = $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}",
    framework = RuntimeInformation.FrameworkDescription,
    processor_count = Environment.ProcessorCount,
    machine_name_hash = ComputeSha256(Environment.MachineName)[..16],
    timestamp_utc = DateTimeOffset.UtcNow.ToString("O"),
    local_timestamp = DateTimeOffset.Now.ToString("O"),
    culture = CultureInfo.CurrentCulture.Name
};

Console.WriteLine(JsonSerializer.Serialize(host, new JsonSerializerOptions { WriteIndented = true }));

var screens = new List<object>();
for (var screenNumber = 1; screenNumber <= 45; screenNumber++)
{
    var state = ShowcaseDemoState.Create(
        inlineMode: false,
        viewport: new Size(80, 24),
        screenNumber,
        language: "en-US",
        flowDirection: WidgetFlowDirection.LeftToRight);

    var buffer = new FrankenTui.Render.Buffer(80, 24);
    var view = ShowcaseSurface.Create(state);
    view.Render(new RuntimeRenderContext(buffer, Rect.FromSize(80, 24), Theme.DefaultTheme));

    var text = string.Join('\n', HeadlessBufferView.ScreenText(buffer));
    var hash = ComputeFnv1a64(text);

    screens.Add(new
    {
        screen_number = screenNumber,
        slug = state.CurrentScreen.Slug,
        title = state.CurrentScreen.Title,
        category = state.CurrentScreen.Category.ToString(),
        fnv1a64 = hash,
        text_length = text.Length,
        line_count = buffer.Height
    });

    Console.Write($"\rRendered screen {screenNumber}/45  hash={hash}");
}

Console.WriteLine();

var evidence = new { host, rendered_at_utc = DateTimeOffset.UtcNow.ToString("O"), screens };
var path = Path.Combine(evidenceDir, $"host-evidence-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json");
var json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(path, json, Encoding.UTF8);

Console.WriteLine($"\nEvidence written to {Path.GetFullPath(path)}");
Console.WriteLine($"Screens rendered: {screens.Count}");

static string ComputeSha256(string input) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();

static string ComputeFnv1a64(string text)
{
    const ulong offsetBasis = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offsetBasis;
    foreach (var b in Encoding.UTF8.GetBytes(text))
    {
        hash ^= b;
        hash *= prime;
    }

    return $"fnv1a64:{hash:x16}";
}
