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
    var resolved = screen with { Width = (ushort)options.Width, Height = (ushort)options.Height };
    var result = Compare(resolved, upstreamDirectory, outputDirectory);
    results.Add(result);
}

WriteIndex(results, outputDirectory, upstreamDirectory);

var diffCount = results.Count(result => !result.ExactMatch);
var missingCount = results.Count(result => result.UpstreamMissing);
Console.WriteLine(
    FormattableString.Invariant(
        $"Wrote {results.Count} showcase comparisons to {outputDirectory}. Diffing screens: {diffCount}; missing upstream snapshots: {missingCount}."));

if (options.Baseline)
{
    foreach (var result in results)
    {
        if (result.LocalSnapshotPath is { } localRel)
        {
            var localPath = Path.Combine(outputDirectory, localRel);
            var localText = File.ReadAllText(localPath, Encoding.UTF8);
            var normalized = Normalize(localText);
            var upstreamSnapshotName = $"app_{Slug(result.Screen)}_{result.Screen.Width}x{result.Screen.Height}";
            var upstreamPath = Path.Combine(options.UpstreamDirectory, $"{upstreamSnapshotName}.snap");
            Directory.CreateDirectory(Path.GetDirectoryName(upstreamPath)!);
            File.WriteAllText(upstreamPath, normalized, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
    Console.WriteLine($"Baseline updated: {results.Count} local snapshots written to {options.UpstreamDirectory}.");
    return 0;
}

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
    var baseName = $"app_{Slug(screen)}_{screen.Width}x{screen.Height}";
    var localText = RenderLocal(screen);
    var upstreamPath = Path.Combine(upstreamDirectory, $"{baseName}.snap");
    var upstreamMissing = !File.Exists(upstreamPath);
    var upstreamText = upstreamMissing ? string.Empty : File.ReadAllText(upstreamPath, Encoding.UTF8);

    localText = Normalize(localText);
    upstreamText = Normalize(upstreamText);

    var metrics = CompareText(upstreamText, localText);
    var exactMatch = !upstreamMissing && string.Equals(upstreamText, localText, StringComparison.Ordinal);
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
    builder.AppendLine(FormattableString.Invariant($"Upstream snapshot: app_{Slug(screen)}_{screen.Width}x{screen.Height}.snap"));
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
                $"| {result.Screen.Number} {EscapeMarkdown(result.Screen.Title)} | `app_{Slug(result.Screen)}_{result.Screen.Width}x{result.Screen.Height}.snap` | {exact} | {result.Metrics.EqualRows} | {result.Metrics.DifferingRows} | {result.Metrics.UpstreamNonBlankChars} | {result.Metrics.LocalNonBlankChars} | {result.Metrics.LocalToUpstreamCharRatio:0.000} | [{Path.GetFileName(result.DiffPath)}]({ToMarkdownPath(result.DiffPath)}) |"));
    }

    File.WriteAllText(Path.Combine(outputDirectory, "index.md"), builder.ToString(), Encoding.UTF8);
}

static string Normalize(string text)
{
    var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n', '\r');
    if (normalized.Length > 0 && normalized[0] == '\uFEFF')
        normalized = normalized[1..];
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

static string Slug(ScreenCase screen) => screen.Number switch
{
    1=>"guidedtour",2=>"dashboard",3=>"shakespeare",4=>"codeexplorer",
    5=>"widgetgallery",6=>"layoutlab",7=>"formsinput",8=>"dataviz",
    9=>"filebrowser",10=>"advancedfeatures",11=>"tablethemegallery",
    12=>"terminalcapabilities",13=>"macrorecorder",14=>"performance",
    15=>"markdownrichtext",16=>"mermaidshowcase",17=>"mermaidmegashowcase",
    18=>"visualeffects",19=>"responsivedemo",20=>"logsearch",
    21=>"notifications",22=>"actiontimeline",23=>"intrinsicsizing",
    24=>"layoutinspector",25=>"advancedtexteditor",26=>"mouseplayground",
    27=>"formvalidation",28=>"virtualizedsearch",29=>"asynctasks",
    30=>"themestudio",31=>"snapshotplayer",32=>"performancehud",
    33=>"explainabilitycockpit",34=>"i18ndemo",35=>"voioverlay",
    36=>"inlinemodestory",37=>"accessibilitypanel",38=>"widgetbuilder",
    39=>"commandpalettelab",40=>"determinismlab",41=>"hyperlinkplayground",
    42=>"kanbanboard",43=>"markdownliveeditor",44=>"dragdrop",
    45=>"quakeeasteregg",
    _ => screen.Title.ToLowerInvariant().Replace(" ", "")
};

internal sealed record CompareOptions(
    string OutputDirectory,
    string UpstreamDirectory,
    string Screens,
    bool FailOnDiff,
    bool Baseline,
    ushort Width,
    ushort Height)
{
    public static CompareOptions Parse(string[] args)
    {
        var outputDirectory = Path.Combine("artifacts", "showcase-compare");
        var upstreamDirectory = Path.Combine("artifacts", "showcase-compare", "upstream");
        var screens = "1-45";
        var failOnDiff = false;
        var baseline = false;
        ushort width = 80;
        ushort height = 24;

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
                case "--baseline":
                    baseline = true;
                    break;
                case "--width":
                    width = ushort.Parse(RequireValue(args, ref index, "--width"), CultureInfo.InvariantCulture);
                    break;
                case "--height":
                    height = ushort.Parse(RequireValue(args, ref index, "--height"), CultureInfo.InvariantCulture);
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

        return new CompareOptions(outputDirectory, upstreamDirectory, screens, failOnDiff, baseline, width, height);
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
        Console.WriteLine("  --width <cols>     Viewport width (default: 80)");
        Console.WriteLine("  --height <rows>    Viewport height (default: 24)");
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
        new(1, "Guided Tour"),
        new(2, "Dashboard"),
        new(3, "Shakespeare"),
        new(4, "Code Explorer"),
        new(5, "Widget Gallery"),
        new(6, "Layout Lab"),
        new(7, "Forms & Input"),
        new(8, "Data Viz"),
        new(9, "File Browser"),
        new(10, "Advanced Features"),
        new(11, "Table Theme Gallery"),
        new(12, "Terminal Capabilities"),
        new(13, "Macro Recorder"),
        new(14, "Performance"),
        new(15, "Markdown Rich Text"),
        new(16, "Mermaid Showcase"),
        new(17, "Mermaid Mega Showcase"),
        new(18, "Visual Effects"),
        new(19, "Responsive Layout"),
        new(20, "Log Search"),
        new(21, "Notifications"),
        new(22, "Action Timeline"),
        new(23, "Intrinsic Sizing"),
        new(24, "Layout Inspector"),
        new(25, "Advanced Text Editor"),
        new(26, "Mouse Playground"),
        new(27, "Form Validation"),
        new(28, "Virtualized Search"),
        new(29, "Async Tasks"),
        new(30, "Theme Studio"),
        new(31, "Time-Travel Studio"),
        new(32, "Performance Challenge"),
        new(33, "Explainability Cockpit"),
        new(34, "i18n Stress Lab"),
        new(35, "VOI Overlay"),
        new(36, "Inline Mode Story"),
        new(37, "Accessibility Panel"),
        new(38, "Widget Builder"),
        new(39, "Command Palette Lab"),
        new(40, "Determinism Lab"),
        new(41, "Hyperlink Playground"),
        new(42, "Kanban Board"),
        new(43, "Live Markdown Editor"),
        new(44, "Drag & Drop Lab"),
        new(45, "Quake E1M1")
    ];
}

internal sealed record ScreenCase(int Number, string Title, ushort Width = 80, ushort Height = 24);

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
