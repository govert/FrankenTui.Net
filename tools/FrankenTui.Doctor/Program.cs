using System.Text.Json;
using FrankenTui.Doctor;
using FrankenTui.Testing.Harness;

var format = Parse(args, "--format") ?? "json";
var width = ParseUShort(args, "--width", 72);
var height = ParseUShort(args, "--height", 16);
var writeArtifacts = args.Contains("--write-artifacts", StringComparer.OrdinalIgnoreCase);

var report = EnvironmentDoctor.CreateReport();
if (writeArtifacts)
{
    var evidence = RenderHarness.CaptureHostedParity(
        "doctor-dashboard",
        DoctorDashboardViewFactory.Build(report),
        width,
        height,
        options: DoctorDashboardViewFactory.CreateWebOptions(report));
    var artifactPaths = evidence.WriteArtifacts("doctor");
    report = report with { ArtifactPaths = artifactPaths };
}

if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(DoctorDashboardViewFactory.RenderText(report));
}
else
{
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
}

static ushort ParseUShort(string[] arguments, string name, ushort fallback) =>
    ushort.TryParse(Parse(arguments, name), out var value) ? value : fallback;

static string? Parse(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length - 1; index++)
    {
        if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[index + 1];
        }
    }

    return null;
}
