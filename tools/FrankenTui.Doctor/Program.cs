using System.Text.Json;
using FrankenTui.Doctor;

var report = EnvironmentDoctor.CreateReport();
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true
}));
