using System.Globalization;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

var options = CompareOptions.Parse(args);
var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var upstreamDirectory = ResolvePath(options.UpstreamDirectory, repoRoot);
var outputDirectory = ResolvePath(options.OutputDirectory, repoRoot);
var selectedScreens = ScreenSelection.Parse(options.Screens);

Directory.CreateDirectory(outputDirectory);
Directory.CreateDirectory(Path.Combine(outputDirectory, "local"));
Directory.CreateDirectory(Path.Combine(outputDirectory, "upstream"));
Directory.CreateDirectory(Path.Combine(outputDirectory, "diff"));

var cases = ScreenCases.All
    .Where(screen => selectedScreens.Contains(screen.Number))
    .ToArray();
var results = new List<ComparisonResult>(cases.Length);

foreach (var screen in cases)
{
    var result = Compare(screen, upstreamDirectory, outputDirectory);
    results.Add(result);
}

WriteIndex(results, outputDirectory, upstreamDirectory);

var diffCount = results.Count(result => !result.ExactMatch);
var missingCount = results.Count(result => result.UpstreamMissing);
Console.WriteLine(
    FormattableString.Invariant(
        $"Wrote {results.Count} showcase comparisons to {outputDirectory}. Diffing screens: {diffCount}; missing upstream snapshots: {missingCount}."));

if (options.FailOnDiff && diffCount > 0)
{
    return 2;
}

if (missingCount > 0)
{
    return 1;
}

return 0;

static ComparisonResult Compare(ScreenCase screen, string upstreamDirectory, string outputDirectory)
{
    var localText = RenderLocal(screen);
    var upstreamPath = Path.Combine(upstreamDirectory, screen.UpstreamSnapshot);
    var upstreamMissing = !File.Exists(upstreamPath);
    var upstreamText = upstreamMissing ? string.Empty : File.ReadAllText(upstreamPath, Encoding.UTF8);

    localText = Normalize(localText);
    upstreamText = Normalize(upstreamText);

    var metrics = CompareText(upstreamText, localText);
    var exactMatch = !upstreamMissing && string.Equals(upstreamText, localText, StringComparison.Ordinal);
    var baseName = Path.GetFileNameWithoutExtension(screen.UpstreamSnapshot);
    var localSnapshotPath = Path.Combine(outputDirectory, "local", $"{baseName}.local.snap");
    var upstreamSnapshotPath = Path.Combine(outputDirectory, "upstream", $"{baseName}.upstream.snap");
    var diffPath = Path.Combine(outputDirectory, "diff", $"{baseName}.diff.txt");

    File.WriteAllText(localSnapshotPath, localText, Encoding.UTF8);
    File.WriteAllText(upstreamSnapshotPath, upstreamText, Encoding.UTF8);
    File.WriteAllText(diffPath, BuildDiff(screen, upstreamMissing, upstreamText, localText, metrics), Encoding.UTF8);

    return new ComparisonResult(
        screen,
        upstreamMissing,
        exactMatch,
        metrics,
        RelativePath(outputDirectory, localSnapshotPath),
        RelativePath(outputDirectory, upstreamSnapshotPath),
        RelativePath(outputDirectory, diffPath));
}

static string RenderLocal(ScreenCase screen)
{
    var buffer = new RenderBuffer((ushort)screen.Width, (ushort)screen.Height);
    var view = ShowcaseViewFactory.Build(
        inlineMode: false,
        screenNumber: screen.Number,
        frame: 0,
        width: (ushort)screen.Width,
        height: (ushort)screen.Height);

    view.Render(new RuntimeRenderContext(buffer, Rect.FromSize((ushort)screen.Width, (ushort)screen.Height), Theme.DefaultTheme));
    return HeadlessBufferView.ScreenString(buffer);
}

static ComparisonMetrics CompareText(string upstreamText, string localText)
{
    var upstreamLines = SplitLines(upstreamText);
    var localLines = SplitLines(localText);
    var maxRows = Math.Max(upstreamLines.Length, localLines.Length);
    var equalRows = 0;
    var differingRows = 0;

    for (var row = 0; row < maxRows; row++)
    {
        var upstream = row < upstreamLines.Length ? upstreamLines[row] : string.Empty;
        var local = row < localLines.Length ? localLines[row] : string.Empty;
        if (string.Equals(upstream, local, StringComparison.Ordinal))
        {
            equalRows++;
        }
        else
        {
            differingRows++;
        }
    }

    var upstreamNonBlankChars = CountNonBlankChars(upstreamText);
    var localNonBlankChars = CountNonBlankChars(localText);
    return new ComparisonMetrics(
        upstreamLines.Length,
        localLines.Length,
        equalRows,
        differingRows,
        upstreamNonBlankChars,
        localNonBlankChars,
        CountNonBlankLines(upstreamLines),
        CountNonBlankLines(localLines),
        upstreamNonBlankChars == 0 ? 0 : (double)localNonBlankChars / upstreamNonBlankChars);
}

static string BuildDiff(ScreenCase screen, bool upstreamMissing, string upstreamText, string localText, ComparisonMetrics metrics)
{
    var builder = new StringBuilder();
    builder.AppendLine(FormattableString.Invariant($"Screen {screen.Number}: {screen.Title}"));
    builder.AppendLine(FormattableString.Invariant($"Upstream snapshot: {screen.UpstreamSnapshot}"));
    builder.AppendLine(FormattableString.Invariant($"Viewport: {screen.Width}x{screen.Height}"));
    builder.AppendLine(FormattableString.Invariant($"Upstream nonblank chars: {metrics.UpstreamNonBlankChars}"));
    builder.AppendLine(FormattableString.Invariant($"Local nonblank chars: {metrics.LocalNonBlankChars}"));
    builder.AppendLine(FormattableString.Invariant($"Local/upstream char ratio: {metrics.LocalToUpstreamCharRatio:0.000}"));
    builder.AppendLine(FormattableString.Invariant($"Equal rows: {metrics.EqualRows}; differing rows: {metrics.DifferingRows}"));
    builder.AppendLine();

    if (upstreamMissing)
    {
        builder.AppendLine("Missing upstream snapshot.");
        return builder.ToString();
    }

    var upstreamLines = SplitLines(upstreamText);
    var localLines = SplitLines(localText);
    var maxRows = Math.Max(upstreamLines.Length, localLines.Length);
    for (var row = 0; row < maxRows; row++)
    {
        var upstream = row < upstreamLines.Length ? upstreamLines[row] : string.Empty;
        var local = row < localLines.Length ? localLines[row] : string.Empty;
        if (string.Equals(upstream, local, StringComparison.Ordinal))
        {
            continue;
        }

        builder.AppendLine(FormattableString.Invariant($"@@ row {row + 1:00}"));
        builder.AppendLine($"upstream: {upstream}");
        builder.AppendLine($"local   : {local}");
    }

    return builder.ToString();
}

static void WriteIndex(IReadOnlyList<ComparisonResult> results, string outputDirectory, string upstreamDirectory)
{
    var builder = new StringBuilder();
    builder.AppendLine("# FrankenTui Showcase Comparison");
    builder.AppendLine();
    builder.AppendLine(FormattableString.Invariant($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"));
    builder.AppendLine(FormattableString.Invariant($"Upstream snapshots: `{upstreamDirectory}`"));
    builder.AppendLine();
    builder.AppendLine("| Screen | Upstream snapshot | Exact | Equal rows | Differing rows | Upstream chars | Local chars | Local/upstream | Diff |");
    builder.AppendLine("| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |");

    foreach (var result in results)
    {
        var exact = result.ExactMatch ? "yes" : "no";
        if (result.UpstreamMissing)
        {
            exact = "missing";
        }

        builder.AppendLine(
            FormattableString.Invariant(
                $"| {result.Screen.Number} {EscapeMarkdown(result.Screen.Title)} | `{result.Screen.UpstreamSnapshot}` | {exact} | {result.Metrics.EqualRows} | {result.Metrics.DifferingRows} | {result.Metrics.UpstreamNonBlankChars} | {result.Metrics.LocalNonBlankChars} | {result.Metrics.LocalToUpstreamCharRatio:0.000} | [{Path.GetFileName(result.DiffPath)}]({ToMarkdownPath(result.DiffPath)}) |"));
    }

    File.WriteAllText(Path.Combine(outputDirectory, "index.md"), builder.ToString(), Encoding.UTF8);
}

static string Normalize(string text)
{
    var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n', '\r');
    var lines = normalized
        .Split('\n', StringSplitOptions.None)
        .Select(static line => line.TrimEnd());
    return string.Join('\n', lines) + "\n";
}

static string[] SplitLines(string text) =>
    Normalize(text).Split('\n', StringSplitOptions.None).SkipLast(1).ToArray();

static int CountNonBlankChars(string text) => text.Count(character => !char.IsWhiteSpace(character));

static int CountNonBlankLines(IEnumerable<string> lines) => lines.Count(line => line.Any(character => !char.IsWhiteSpace(character)));

static string ResolvePath(string path, string repoRoot) =>
    Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(repoRoot, path));

static string FindRepoRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "FrankenTui.Net.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static string RelativePath(string basePath, string path) =>
    Path.GetRelativePath(basePath, path).Replace('\\', '/');

static string ToMarkdownPath(string path) => Uri.EscapeDataString(path).Replace("%2F", "/", StringComparison.Ordinal);

static string EscapeMarkdown(string text) => text.Replace("|", "\\|", StringComparison.Ordinal);

internal sealed record CompareOptions(
    string OutputDirectory,
    string UpstreamDirectory,
    string Screens,
    bool FailOnDiff)
{
    public static CompareOptions Parse(string[] args)
    {
        var outputDirectory = Path.Combine("artifacts", "showcase-compare");
        var upstreamDirectory = Path.Combine(".external", "frankentui", "crates", "ftui-demo-showcase", "tests", "snapshots");
        var screens = "1-45";
        var failOnDiff = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--out":
                    outputDirectory = RequireValue(args, ref index, "--out");
                    break;
                case "--upstream":
                    upstreamDirectory = RequireValue(args, ref index, "--upstream");
                    break;
                case "--screens":
                    screens = RequireValue(args, ref index, "--screens");
                    break;
                case "--fail-on-diff":
                    failOnDiff = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[index]}");
            }
        }

        return new CompareOptions(outputDirectory, upstreamDirectory, screens, failOnDiff);
    }

    private static string RequireValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{optionName} requires a value.");
        }

        index++;
        return args[index];
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run --project tools/FrankenTui.ShowcaseCompare -- [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --out <path>       Output directory. Default: artifacts/showcase-compare");
        Console.WriteLine("  --upstream <path>  Upstream snapshot directory.");
        Console.WriteLine("  --screens <list>   Screen list/ranges, for example 1-45 or 42,43,44.");
        Console.WriteLine("  --fail-on-diff     Return exit code 2 when any compared screen differs.");
    }
}

internal static class ScreenSelection
{
    public static ISet<int> Parse(string value)
    {
        var selected = new SortedSet<int>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var range = part.Split('-', StringSplitOptions.TrimEntries);
            if (range.Length == 1)
            {
                selected.Add(ParseScreenNumber(range[0]));
                continue;
            }

            if (range.Length != 2)
            {
                throw new ArgumentException($"Invalid screen selection: {part}");
            }

            var start = ParseScreenNumber(range[0]);
            var end = ParseScreenNumber(range[1]);
            if (end < start)
            {
                throw new ArgumentException($"Invalid descending screen range: {part}");
            }

            for (var screen = start; screen <= end; screen++)
            {
                selected.Add(screen);
            }
        }

        return selected.Count == 0 ? throw new ArgumentException("At least one screen must be selected.") : selected;
    }

    private static int ParseScreenNumber(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var screenNumber) && screenNumber is >= 1 and <= 45
            ? screenNumber
            : throw new ArgumentException($"Invalid screen number: {value}");
}

internal static class ScreenCases
{
    public static readonly IReadOnlyList<ScreenCase> All =
    [
        new(1, "Guided Tour", "app_guidedtour_80x24.snap"),
        new(2, "Dashboard", "app_dashboard_80x24.snap"),
        new(3, "Shakespeare", "app_shakespeare_80x24.snap"),
        new(4, "Code Explorer", "app_codeexplorer_80x24.snap"),
        new(5, "Widget Gallery", "app_widgetgallery_80x24.snap"),
        new(6, "Layout Lab", "app_layoutlab_80x24.snap"),
        new(7, "Forms & Input", "app_formsinput_80x24.snap"),
        new(8, "Data Viz", "app_dataviz_80x24.snap"),
        new(9, "File Browser", "app_filebrowser_80x24.snap"),
        new(10, "Advanced Features", "app_advancedfeatures_80x24.snap"),
        new(11, "Table Theme Gallery", "app_tablethemegallery_80x24.snap"),
        new(12, "Terminal Capabilities", "app_terminalcapabilities_80x24.snap"),
        new(13, "Macro Recorder", "app_macrorecorder_80x24.snap"),
        new(14, "Performance", "app_performance_80x24.snap"),
        new(15, "Markdown Rich Text", "app_markdownrichtext_80x24.snap"),
        new(16, "Mermaid Showcase", "app_mermaidshowcase_80x24.snap"),
        new(17, "Mermaid Mega Showcase", "app_mermaidmegashowcase_80x24.snap"),
        new(18, "Visual Effects", "app_visualeffects_80x24.snap"),
        new(19, "Responsive Layout", "app_responsivedemo_80x24.snap"),
        new(20, "Log Search", "app_logsearch_80x24.snap"),
        new(21, "Notifications", "app_notifications_80x24.snap"),
        new(22, "Action Timeline", "app_actiontimeline_80x24.snap"),
        new(23, "Intrinsic Sizing", "app_intrinsicsizing_80x24.snap"),
        new(24, "Layout Inspector", "app_layoutinspector_80x24.snap"),
        new(25, "Advanced Text Editor", "app_advancedtexteditor_80x24.snap"),
        new(26, "Mouse Playground", "app_mouseplayground_80x24.snap"),
        new(27, "Form Validation", "app_formvalidation_80x24.snap"),
        new(28, "Virtualized Search", "app_virtualizedsearch_80x24.snap"),
        new(29, "Async Tasks", "app_asynctasks_80x24.snap"),
        new(30, "Theme Studio", "app_themestudio_80x24.snap"),
        new(31, "Time-Travel Studio", "app_snapshotplayer_80x24.snap"),
        new(32, "Performance Challenge", "app_performancehud_80x24.snap"),
        new(33, "Explainability Cockpit", "app_explainabilitycockpit_80x24.snap"),
        new(34, "i18n Stress Lab", "app_i18ndemo_80x24.snap"),
        new(35, "VOI Overlay", "app_voioverlay_80x24.snap"),
        new(36, "Inline Mode Story", "app_inlinemodestory_80x24.snap"),
        new(37, "Accessibility Panel", "app_accessibilitypanel_80x24.snap"),
        new(38, "Widget Builder", "app_widgetbuilder_80x24.snap"),
        new(39, "Command Palette Lab", "app_commandpalettelab_80x24.snap"),
        new(40, "Determinism Lab", "app_determinismlab_80x24.snap"),
        new(41, "Hyperlink Playground", "app_hyperlinkplayground_80x24.snap"),
        new(42, "Kanban Board", "app_kanbanboard_80x24.snap"),
        new(43, "Live Markdown Editor", "app_markdownliveeditor_80x24.snap"),
        new(44, "Drag & Drop Lab", "app_dragdrop_80x24.snap"),
        new(45, "Quake E1M1", "app_quakeeasteregg_80x24.snap")
    ];
}

internal sealed record ScreenCase(int Number, string Title, string UpstreamSnapshot, int Width = 80, int Height = 24);

internal sealed record ComparisonResult(
    ScreenCase Screen,
    bool UpstreamMissing,
    bool ExactMatch,
    ComparisonMetrics Metrics,
    string LocalSnapshotPath,
    string UpstreamSnapshotPath,
    string DiffPath);

internal sealed record ComparisonMetrics(
    int UpstreamRows,
    int LocalRows,
    int EqualRows,
    int DifferingRows,
    int UpstreamNonBlankChars,
    int LocalNonBlankChars,
    int UpstreamNonBlankLines,
    int LocalNonBlankLines,
    double LocalToUpstreamCharRatio);
