using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FrankenTui.Testing.Harness;

var options = CaptureOptions.Parse(args);
var root = RepositoryPaths.FindRepositoryRoot();
var upstreamRoot = options.UpstreamRoot ?? UpstreamReferencePaths.FindUpstreamRoot();
var winTermDriverRoot = options.WinTermDriverRoot ?? Path.GetFullPath(Path.Combine(root, "..", "WinTermDriver"));
var outputRoot = options.OutputDirectory ?? CreateDefaultOutputDirectory(root);

Directory.CreateDirectory(outputRoot);

var layout = new ToolLayout(
    root,
    upstreamRoot,
    winTermDriverRoot,
    outputRoot);

Console.WriteLine($"Capture output: {layout.OutputRoot}");
Console.WriteLine($"Upstream root: {layout.UpstreamRoot}");
Console.WriteLine($"WinTermDriver root: {layout.WinTermDriverRoot}");

await EnsureToolchainAsync(layout, options).ConfigureAwait(false);

var screens = await ReadScreenRegistryAsync(layout.UpstreamDemoExe).ConfigureAwait(false);
var selectedScreens = SelectScreens(screens, options).ToArray();

if (selectedScreens.Length == 0)
{
    Console.Error.WriteLine("No screens selected.");
    return 1;
}

Console.WriteLine($"Capturing {selectedScreens.Length} upstream showcase screen(s).");

var (screenshotWidth, screenshotHeight) = ResolveScreenMetrics(options.Columns, options.Rows, options.Width, options.Height);

var manifest = new CaptureManifest
{
    GeneratedAtUtc = DateTimeOffset.UtcNow,
    RepositoryRoot = root,
    UpstreamRoot = layout.UpstreamRoot,
    UpstreamBasisCommit = UpstreamReferencePaths.BasisCommit,
    WinTermDriverRoot = layout.WinTermDriverRoot,
    OutputRoot = layout.OutputRoot,
    ImageWidth = screenshotWidth,
    ImageHeight = screenshotHeight,
    ScreenMode = options.ScreenMode,
    Columns = options.Columns,
    Rows = options.Rows,
    SettleMilliseconds = options.SettleMilliseconds
};

foreach (var screen in selectedScreens)
{
    var workspaceName = $"ftui-{screen.Index:00}-{screen.Slug}-{Environment.ProcessId}";
    var paneName = $"screen{screen.Index:00}";
    var workspaceFile = Path.Combine(layout.WorkspaceRoot, $"{screen.Index:00}-{screen.Slug}.yaml");
    var imagePath = Path.Combine(layout.ImagesRoot, $"{screen.Index:00}-{screen.Slug}.png");

    WriteWorkspaceDefinition(workspaceFile, workspaceName, paneName, layout.UpstreamDemoExe, layout.UpstreamRoot, screen, options);

    var startedAtUtc = DateTimeOffset.UtcNow;
    string? probeOutput = null;
    Exception? error = null;

    Console.WriteLine($"[{screen.Index:00}] {screen.Title}");

    try
    {
        await RunProcessCheckedAsync(
            layout.WtdExe,
            ["open", workspaceName, "--file", workspaceFile, "--recreate"],
            layout.WinTermDriverRoot,
            timeoutMs: 30_000,
            captureOutput: false).ConfigureAwait(false);

        probeOutput = await WaitForPaneReadyAsync(
            layout.WtdExe,
            layout.WinTermDriverRoot,
            paneName,
            screen.Title,
            options.ReadyTimeoutMilliseconds,
            options.PollMilliseconds,
            options.SettleMilliseconds).ConfigureAwait(false);

        await RunProcessCheckedAsync(
            layout.ScreenshotGenExe,
            [
                "--workspace", workspaceName,
                "--output", imagePath,
                "--width", screenshotWidth.ToString(),
                "--height", screenshotHeight.ToString()
            ],
            layout.WinTermDriverRoot,
            timeoutMs: 30_000).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        error = ex;
        Console.Error.WriteLine($"[{screen.Index:00}] capture failed: {ex.Message}");
    }
    finally
    {
        if (!options.HoldOpen)
        {
            try
            {
                await RunProcessAsync(
                    layout.WtdExe,
                    ["close", workspaceName, "--kill"],
                    layout.WinTermDriverRoot,
                    timeoutMs: 10_000,
                    captureOutput: false).ConfigureAwait(false);
            }
            catch
            {
            }
        }
        else
        {
            if (selectedScreens.Length == 1)
            {
                Console.WriteLine($"[hold-open] Workspace {workspaceName} left running. Press Enter to close it, or Ctrl+C to exit.");
                await Task.Run(Console.ReadLine);
            }
        }
    }

    manifest.Screens.Add(new ScreenCaptureManifest
    {
        Index = screen.Index,
        Title = screen.Title,
        Slug = screen.Slug,
        Description = screen.Description,
        WorkspaceFile = workspaceFile,
        ImagePath = imagePath,
        StartedAtUtc = startedAtUtc,
        CompletedAtUtc = DateTimeOffset.UtcNow,
        ProbeOutput = probeOutput,
        Success = error is null && File.Exists(imagePath),
        Error = error?.ToString()
    });
}

if (options.StopHostOnExit)
{
    try
    {
        await RunProcessAsync(layout.WtdExe, ["host", "stop"], layout.WinTermDriverRoot, 10_000).ConfigureAwait(false);
        
    }
    catch
    {
    }
}

var manifestPath = Path.Combine(layout.OutputRoot, "manifest.json");
var manifestJson = JsonSerializer.Serialize(manifest, CreateJsonOptions());
await File.WriteAllTextAsync(manifestPath, manifestJson).ConfigureAwait(false);

        Console.WriteLine($"Manifest: {manifestPath}");
        Console.WriteLine($"Images:   {layout.ImagesRoot}");

return manifest.Screens.All(static item => item.Success) ? 0 : 1;

static string CreateDefaultOutputDirectory(string root)
{
    var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
    return Path.Combine(root, "artifacts", "pty", "upstream-showcase-capture", stamp);
}

static async Task EnsureToolchainAsync(ToolLayout layout, CaptureOptions options)
{
    if (options.RebuildTools || !File.Exists(layout.UpstreamDemoExe))
    {
        Console.WriteLine("Building upstream ftui-demo-showcase...");
        await RunProcessCheckedAsync(
            "cargo",
            ["build", "--manifest-path", layout.UpstreamManifest, "-p", "ftui-demo-showcase"],
            layout.UpstreamRoot,
            timeoutMs: 300_000).ConfigureAwait(false);
    }

    if (options.RebuildTools || !File.Exists(layout.WtdExe) || !File.Exists(layout.ScreenshotGenExe))
    {
        Console.WriteLine("Building WinTermDriver controller/capture tools...");
        await RunProcessCheckedAsync(
            "cargo",
            ["build", "-p", "wtd-cli", "-p", "screenshot-gen"],
            layout.WinTermDriverRoot,
            timeoutMs: 300_000).ConfigureAwait(false);
    }
}

static async Task<IReadOnlyList<ScreenSpec>> ReadScreenRegistryAsync(string upstreamDemoExe)
{
    var result = await RunProcessCheckedAsync(
        upstreamDemoExe,
        ["--help"],
        Path.GetDirectoryName(upstreamDemoExe)!,
        timeoutMs: 30_000).ConfigureAwait(false);

    var lines = result.Stdout.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    var inScreens = false;
    var regex = new Regex(@"^\s*(\d+)\s+(.+?)\s{2,}(.+?)\s*$", RegexOptions.Compiled);
    var screens = new List<ScreenSpec>();

    foreach (var raw in lines)
    {
        var line = raw.TrimEnd();
        if (line == "SCREENS:")
        {
            inScreens = true;
            continue;
        }

        if (!inScreens)
        {
            continue;
        }

        if (line == "KEYBINDINGS:")
        {
            break;
        }

        var match = regex.Match(line);
        if (!match.Success)
        {
            continue;
        }

        var index = int.Parse(match.Groups[1].Value);
        var title = match.Groups[2].Value.Trim();
        var description = match.Groups[3].Value.Trim();
        screens.Add(new ScreenSpec(index, title, Slugify(index, title), description));
    }

    if (screens.Count == 0)
    {
        throw new InvalidOperationException("Could not parse upstream screen registry from ftui-demo-showcase --help.");
    }

    return screens;
}

static IEnumerable<ScreenSpec> SelectScreens(IReadOnlyList<ScreenSpec> screens, CaptureOptions options)
{
    if (options.ScreenIndex is int single)
    {
        return screens.Where(screen => screen.Index == single);
    }

    return screens;
}

static void WriteWorkspaceDefinition(
    string workspaceFile,
    string workspaceName,
    string paneName,
    string upstreamDemoExe,
    string upstreamRoot,
    ScreenSpec screen,
    CaptureOptions options)
{
    Directory.CreateDirectory(Path.GetDirectoryName(workspaceFile)!);

    var yaml = $$"""
version: 1
name: {{workspaceName}}
defaults:
  terminalSize:
    cols: {{options.Columns}}
    rows: {{options.Rows}}
profiles:
  ftui:
    type: custom
    executable: '{{YamlSingleQuote(upstreamDemoExe)}}'
    cwd: '{{YamlSingleQuote(upstreamRoot)}}'
    env:
      FTUI_DEMO_DETERMINISTIC: '1'
      FTUI_DEMO_MOUSE: 'off'
    args:
      - --screen={{screen.Index}}
      - --screen-mode={{options.ScreenMode}}
      - --exit-after-ms={{options.ExitAfterMilliseconds}}
tabs:
  - name: main
    layout:
      type: pane
      name: {{paneName}}
      session:
        profile: ftui
""";

    File.WriteAllText(workspaceFile, yaml);
}

static string YamlSingleQuote(string value) =>
    value.Replace("'", "''", StringComparison.Ordinal);

static async Task<string?> WaitForPaneReadyAsync(
    string wtdExe,
    string workingDirectory,
    string paneName,
    string expectedTitle,
    int timeoutMs,
    int pollMs,
    int settleMs)
{
    _ = expectedTitle;
    _ = pollMs;

    await Task.Delay(settleMs, CancellationToken.None).ConfigureAwait(false);

    var capture = await RunProcessAsync(
        wtdExe,
        ["capture", paneName],
        workingDirectory,
        timeoutMs: Math.Min(10_000, timeoutMs)).ConfigureAwait(false);

    return capture.ExitCode == 0 ? capture.Stdout : null;
}

static string Slugify(int index, string title)
{
    var slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-")
        .Trim('-');
    return string.IsNullOrWhiteSpace(slug) ? $"screen-{index:00}" : slug;
}

static (int width, int height) ResolveScreenMetrics(int columns, int rows, int width, int height)
{
    const int fallbackCellWidth = 10;
    const int fallbackCellHeight = 20;
    var safeColumns = Math.Max(columns, 1);
    var safeRows = Math.Max(rows, 1);
    var resolvedWidth = width > 0 ? width : safeColumns * fallbackCellWidth;
    var resolvedHeight = height > 0 ? height : safeRows * fallbackCellHeight;
    return (resolvedWidth, resolvedHeight);
}

static async Task<ProcessResult> RunProcessCheckedAsync(
    string fileName,
    IReadOnlyList<string> arguments,
    string workingDirectory,
    int timeoutMs,
    bool captureOutput = true)
{
    var result = await RunProcessAsync(fileName, arguments, workingDirectory, timeoutMs, captureOutput).ConfigureAwait(false);
    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"Command failed ({result.ExitCode}): {fileName} {string.Join(' ', arguments)}{Environment.NewLine}{result.Stderr}{result.Stdout}");
    }

    return result;
}

static async Task<ProcessResult> RunProcessAsync(
    string fileName,
    IReadOnlyList<string> arguments,
    string workingDirectory,
    int timeoutMs,
    bool captureOutput = true)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    foreach (var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    process.Start();

    Task<string>? stdoutTask = null;
    Task<string>? stderrTask = null;
    if (captureOutput)
    {
        stdoutTask = process.StandardOutput.ReadToEndAsync();
        stderrTask = process.StandardError.ReadToEndAsync();
    }

    using var cts = new CancellationTokenSource(timeoutMs);
    await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

    return new ProcessResult(
        process.ExitCode,
        stdoutTask is null ? string.Empty : await stdoutTask.ConfigureAwait(false),
        stderrTask is null ? string.Empty : await stderrTask.ConfigureAwait(false));
}

static JsonSerializerOptions CreateJsonOptions() =>
    new()
    {
        WriteIndented = true
    };

internal sealed record CaptureOptions(
    int? ScreenIndex,
    int Columns,
    int Rows,
    int Width,
    int Height,
    int SettleMilliseconds,
    int PollMilliseconds,
    int ReadyTimeoutMilliseconds,
    int ExitAfterMilliseconds,
    string ScreenMode,
    bool RebuildTools,
    bool HoldOpen,
    bool StopHostOnExit,
    string? OutputDirectory,
    string? UpstreamRoot,
    string? WinTermDriverRoot)
{
    public static CaptureOptions Parse(string[] args)
    {
        int? screenIndex = null;
        var columns = 196;
        var rows = 48;
        var width = 0;
        var height = 0;
        var settleMs = 3000;
        var pollMs = 500;
        var readyTimeoutMs = 12000;
        var exitAfterMs = 15000;
        var screenMode = "alt";
        var rebuildTools = false;
        var holdOpen = false;
        var stopHostOnExit = false;
        string? outputDirectory = null;
        string? upstreamRoot = null;
        string? winTermDriverRoot = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--screen":
                    screenIndex = int.Parse(args[++i]);
                    break;
                case "--columns":
                case "--cols":
                    columns = int.Parse(args[++i]);
                    break;
                case "--rows":
                    rows = int.Parse(args[++i]);
                    break;
                case "--width":
                    width = int.Parse(args[++i]);
                    break;
                case "--height":
                    height = int.Parse(args[++i]);
                    break;
                case "--settle-ms":
                    settleMs = int.Parse(args[++i]);
                    break;
                case "--poll-ms":
                    pollMs = int.Parse(args[++i]);
                    break;
                case "--ready-timeout-ms":
                    readyTimeoutMs = int.Parse(args[++i]);
                    break;
                case "--exit-after-ms":
                    exitAfterMs = int.Parse(args[++i]);
                    break;
                case "--screen-mode":
                    screenMode = args[++i];
                    break;
                case "--output-dir":
                    outputDirectory = Path.GetFullPath(args[++i]);
                    break;
                case "--upstream-root":
                    upstreamRoot = Path.GetFullPath(args[++i]);
                    break;
                case "--wtd-root":
                    winTermDriverRoot = Path.GetFullPath(args[++i]);
                    break;
                case "--rebuild":
                    rebuildTools = true;
                    break;
                case "--hold":
                    holdOpen = true;
                    break;
                case "--stop-host":
                    stopHostOnExit = true;
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        return new CaptureOptions(
            screenIndex,
            columns,
            rows,
            width,
            height,
            settleMs,
            pollMs,
            readyTimeoutMs,
            exitAfterMs,
            screenMode,
            rebuildTools,
            holdOpen,
            stopHostOnExit,
            outputDirectory,
            upstreamRoot,
            winTermDriverRoot);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
FrankenTui upstream showcase capture driver

Usage:
  dotnet run --project tests/FrankenTui.Tests.CaptureDriver -- [options]

Options:
  --screen N             Capture only upstream screen N (default: all)
  --cols N               Terminal columns in workspace (default: 196)
  --rows N               Terminal rows in workspace (default: 48)
  --width N              PNG width in pixels (default: columns * 10)
  --height N             PNG height in pixels (default: rows * 20)
  --settle-ms N          Extra wait before screenshot once ready (default: 3000)
  --poll-ms N            Poll interval for pane readiness (default: 500)
  --ready-timeout-ms N   Pane readiness timeout (default: 12000)
  --exit-after-ms N      Upstream demo auto-exit timeout (default: 15000)
  --screen-mode MODE     Upstream screen mode (default: alt)
  --output-dir PATH      Output directory (default: artifacts/pty/upstream-showcase-capture/<utc-stamp>)
  --upstream-root PATH   Override upstream frankentui root
  --wtd-root PATH        Override WinTermDriver root
  --rebuild              Rebuild upstream demo and WinTermDriver tools first
  --hold                 Keep opened workspace after capture for inspection (single screen only)
  --stop-host            Stop wtd-host when finished
""");
    }
}

internal sealed record ToolLayout(
    string RepositoryRoot,
    string UpstreamRoot,
    string WinTermDriverRoot,
    string OutputRoot)
{
    public string UpstreamManifest => Path.Combine(UpstreamRoot, "Cargo.toml");
    public string UpstreamDemoExe => Path.Combine(UpstreamRoot, "target", "debug", "ftui-demo-showcase.exe");
    public string WtdExe => Path.Combine(WinTermDriverRoot, "target", "debug", "wtd.exe");
    public string ScreenshotGenExe => Path.Combine(WinTermDriverRoot, "target", "debug", "screenshot-gen.exe");
    public string WorkspaceRoot => Path.Combine(OutputRoot, "workspaces");
    public string ImagesRoot => Path.Combine(OutputRoot, "images");
}

internal sealed record ScreenSpec(int Index, string Title, string Slug, string Description);

internal sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);

internal sealed class CaptureManifest
{
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string RepositoryRoot { get; set; } = string.Empty;
    public string UpstreamRoot { get; set; } = string.Empty;
    public string UpstreamBasisCommit { get; set; } = string.Empty;
    public string WinTermDriverRoot { get; set; } = string.Empty;
    public string OutputRoot { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string ScreenMode { get; set; } = string.Empty;
    public int Columns { get; set; }
    public int Rows { get; set; }
    public int SettleMilliseconds { get; set; }
    public List<ScreenCaptureManifest> Screens { get; } = [];
}

internal sealed class ScreenCaptureManifest
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WorkspaceFile { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public bool Success { get; set; }
    public string? ProbeOutput { get; set; }
    public string? Error { get; set; }
}
