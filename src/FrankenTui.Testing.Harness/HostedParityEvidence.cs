using System.Text.Json;
using FrankenTui.Web;

namespace FrankenTui.Testing.Harness;

public sealed record HostedParityEvidence(
    string Name,
    RenderSnapshot Terminal,
    WebFrame Web,
    string Json)
{
    public static HostedParityEvidence Create(string name, RenderSnapshot terminal, WebFrame web)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(web);

        var json = JsonSerializer.Serialize(
            new
            {
                name,
                terminal = new
                {
                    terminal.Rows,
                    terminal.Text
                },
                web = new
                {
                    web.Title,
                    web.Language,
                    web.Direction,
                    web.Metadata,
                    web.Accessibility.Nodes,
                    web.Rows,
                    web.Text
                }
            },
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        return new HostedParityEvidence(name, terminal, web, json);
    }

    public IReadOnlyDictionary<string, string> WriteArtifacts(string category = "replay")
    {
        var jsonPath = ArtifactPathBuilder.For(category, $"{Name}.json");
        var textPath = ArtifactPathBuilder.For(category, $"{Name}.txt");
        var htmlPath = ArtifactPathBuilder.For("web", $"{Name}.html");

        File.WriteAllText(jsonPath, Json);
        File.WriteAllText(textPath, Terminal.Text);
        File.WriteAllText(htmlPath, Web.DocumentHtml);

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["json"] = jsonPath,
            ["text"] = textPath,
            ["html"] = htmlPath
        };
    }
}
