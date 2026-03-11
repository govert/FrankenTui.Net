using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Testing.Harness;

public static class SharedSampleComparison
{
    public const string DefaultArtifactName = "shared-sample-suite";

    public static bool CanRunUpstream()
    {
        try
        {
            _ = UpstreamReferencePaths.FindUpstreamRoot();
        }
        catch
        {
            return false;
        }

        return TryFindCommand("cargo") is not null && TryFindCommand("rustc") is not null;
    }

    public static async Task<SharedSampleComparisonReport> CompareAsync(CancellationToken cancellationToken = default)
    {
        var local = await BuildLocalCaptureAsync(cancellationToken).ConfigureAwait(false);
        var upstream = await BuildUpstreamCaptureAsync(cancellationToken).ConfigureAwait(false);
        return SharedSampleComparisonReport.Create(local, upstream);
    }

    private static async Task<SharedSampleCapture> BuildLocalCaptureAsync(CancellationToken cancellationToken) =>
        new(
            "FrankenTui.Net",
            UpstreamReferencePaths.BasisCommit,
            [
                await BuildCounterFlowSampleAsync(cancellationToken).ConfigureAwait(false),
                BuildUnicodeCellsSample(BuildUnicodeFrame),
                BuildWideOverwriteSample(BuildWideOverwriteFrame),
                await BuildInlineOverlaySampleAsync(cancellationToken).ConfigureAwait(false),
                BuildCommandPaletteSample(),
                BuildLogSearchSample()
            ]);

    private static async Task<SharedSampleCapture> BuildUpstreamCaptureAsync(CancellationToken cancellationToken)
    {
        var project = EnsureUpstreamRunnerProject();
        var result = await ProcessCommandRunner.RunAsync(
                "cargo",
                [
                    "run",
                    "--quiet",
                    "--manifest-path",
                    Path.Combine(project, "Cargo.toml")
                ],
                workingDirectory: project,
                environmentVariables: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["CARGO_TARGET_DIR"] = Path.Combine(UpstreamReferencePaths.FindUpstreamRoot(), "target", "shared-sample-compare"),
                    ["CARGO_TERM_COLOR"] = "never"
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Upstream sample runner failed with exit code {result.ExitCode}.{Environment.NewLine}{result.Stderr}{Environment.NewLine}{result.Stdout}");
        }

        return ParseCapture(result.Stdout);
    }

    private static SharedSampleCapture ParseCapture(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var implementation = root.GetProperty("implementation").GetString() ?? "unknown";
        var basisCommit = root.GetProperty("basis_commit").GetString() ?? UpstreamReferencePaths.BasisCommit;
        var samples = new List<SharedSampleCase>();

        foreach (var sampleElement in root.GetProperty("samples").EnumerateArray())
        {
            var frames = new List<SharedSampleFrame>();
            foreach (var frameElement in sampleElement.GetProperty("frames").EnumerateArray())
            {
                var label = frameElement.GetProperty("label").GetString() ?? "frame";
                var width = frameElement.GetProperty("width").GetUInt16();
                var height = frameElement.GetProperty("height").GetUInt16();
                var rows = frameElement
                    .GetProperty("rows")
                    .EnumerateArray()
                    .Select(static row => row.GetString() ?? string.Empty)
                    .ToArray();
                frames.Add(SharedSampleFrame.Create(label, width, height, rows));
            }

            var name = sampleElement.GetProperty("name").GetString() ?? "sample";
            samples.Add(SharedSampleCase.Create(name, frames));
        }

        return new SharedSampleCapture(implementation, basisCommit, samples);
    }

    private static async Task<SharedSampleCase> BuildCounterFlowSampleAsync(CancellationToken cancellationToken)
    {
        var simulator = new AppSimulator<int, string>(new Size(16, 2));
        var program = new CounterFlowProgram();
        var session = simulator.CreateSession(program);

        var frames = new List<SharedSampleFrame>
        {
            SharedSampleFrame.Create(
                "init",
                16,
                2,
                RenderHarness.Render(program.BuildView(session.Model), 16, 2).Rows)
        };

        await session.DispatchAsync("inc", cancellationToken).ConfigureAwait(false);
        var afterIncrementPair = await session.DispatchAsync("inc", cancellationToken).ConfigureAwait(false);
        frames.Add(FrameFromScreenText("after_increment_pair", 16, 2, afterIncrementPair.ScreenText));

        await session.ResizeAsync(new Size(20, 3), static size => $"resize:{size.Width}x{size.Height}", cancellationToken).ConfigureAwait(false);
        var afterResizeTick = await session.DispatchAsync("tick", cancellationToken).ConfigureAwait(false);
        frames.Add(FrameFromScreenText("after_resize_tick", 20, 3, afterResizeTick.ScreenText));

        await session.DispatchAsync("dec", cancellationToken).ConfigureAwait(false);
        var afterMinusTick = await session.DispatchAsync("tick", cancellationToken).ConfigureAwait(false);
        frames.Add(FrameFromScreenText("after_minus_tick", 20, 3, afterMinusTick.ScreenText));

        return SharedSampleCase.Create("counter_flow", frames);
    }

    private static SharedSampleCase BuildUnicodeCellsSample(Func<SharedSampleFrame> frameFactory) =>
        SharedSampleCase.Create("unicode_cells", [frameFactory()]);

    private static SharedSampleCase BuildWideOverwriteSample(Func<IReadOnlyList<SharedSampleFrame>> frameFactory) =>
        SharedSampleCase.Create("wide_overwrite", frameFactory());

    private static SharedSampleFrame BuildUnicodeFrame()
    {
        var buffer = new RenderBuffer(5, 2);
        WriteText(buffer, 0, "Hi");
        buffer.Set(0, 1, Cell.FromRune(new Rune(0x1F600)));
        return SharedSampleFrame.Create("emoji_row", buffer.Width, buffer.Height, HeadlessBufferView.ScreenText(buffer));
    }

    private static IReadOnlyList<SharedSampleFrame> BuildWideOverwriteFrame()
    {
        var initial = new RenderBuffer(5, 1);
        initial.Set(0, 0, Cell.FromRune(new Rune(0x1F600)));

        var replaced = new RenderBuffer(5, 1);
        replaced.Set(0, 0, Cell.FromChar('A'));

        return
        [
            SharedSampleFrame.Create("wide_initial", initial.Width, initial.Height, HeadlessBufferView.ScreenText(initial)),
            SharedSampleFrame.Create("wide_replaced", replaced.Width, replaced.Height, HeadlessBufferView.ScreenText(replaced))
        ];
    }

    private static async Task<SharedSampleCase> BuildInlineOverlaySampleAsync(CancellationToken cancellationToken)
    {
        var backend = new MemoryTerminalBackend(new Size(10, 6), TerminalCapabilities.Tmux());
        await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await backend.ConfigureSessionAsync(
            new TerminalSessionConfiguration
            {
                InlineMode = true
            },
            cancellationToken).ConfigureAwait(false);
        backend.DrainOutput();

        var buffer = BufferFromLines(10, ["UI", "Pane"]);
        await backend.PresentAsync(buffer, BufferDiff.Full(buffer.Width, buffer.Height), cancellationToken: cancellationToken).ConfigureAwait(false);
        await backend.WriteLogAsync("log!\nsecond", cancellationToken: cancellationToken).ConfigureAwait(false);
        backend.DrainOutput();
        var rows = Enumerable.Range(0, 6)
            .Select(row => backend.Model.RowText((ushort)row))
            .ToArray();

        return SharedSampleCase.Create(
            "inline_overlay",
            [
                SharedSampleFrame.Create(
                    "overlay_rows",
                    10,
                    6,
                    rows)
            ]);
    }

    private static SharedSampleCase BuildCommandPaletteSample()
    {
        var results = CommandPaletteSearch.Search(CommandPaletteRegistry.DefaultEntries(), "do")
            .Take(4)
            .Select(static result => $"{result.Entry.Title}|{result.Entry.Keybinding ?? "-"}")
            .ToArray();
        return SharedSampleCase.Create(
            "command_palette",
            [
                SharedSampleFrame.Create("query_do", 32, (ushort)results.Length, results)
            ]);
    }

    private static SharedSampleCase BuildLogSearchSample()
    {
        var result = LogSearchEngine.Apply(
            [
                "08:00:01 info  doctor replay refreshed",
                "08:00:02 warn  pane snapshot drift detected",
                "08:00:03 info  macro capture ready",
                "08:00:04 debug perf hud compact frame=11.8ms",
                "08:00:05 info  command palette ranked 7 results",
                "08:00:06 error log search regex parse failure",
                "08:00:07 info  web parity evidence exported"
            ],
            new LogSearchState("doctor", ContextLines: 1));
        return SharedSampleCase.Create(
            "log_search",
            [
                SharedSampleFrame.Create("doctor_context", 48, (ushort)result.Lines.Count, result.Lines)
            ]);
    }

    private static void WriteText(RenderBuffer buffer, ushort row, string text)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(text);

        var x = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (x >= buffer.Width)
            {
                break;
            }

            buffer.Set((ushort)x, row, Cell.FromRune(rune));
            x += Math.Max(TerminalTextWidth.RuneWidth(rune), 1);
        }
    }

    private static SharedSampleFrame FrameFromScreenText(string label, ushort width, ushort height, string screenText) =>
        SharedSampleFrame.Create(label, width, height, SplitRows(screenText));

    private static IReadOnlyList<string> SplitRows(string screenText) =>
        screenText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static RenderBuffer BufferFromLines(ushort width, IReadOnlyList<string> lines)
    {
        var buffer = new RenderBuffer(width, (ushort)Math.Max(lines.Count, 1));
        for (ushort row = 0; row < lines.Count; row++)
        {
            var text = lines[row];
            for (ushort column = 0; column < text.Length && column < width; column++)
            {
                buffer.Set(column, row, Cell.FromChar(text[column]));
            }
        }

        return buffer;
    }

    private static string EnsureUpstreamRunnerProject()
    {
        var root = RepositoryPaths.FindRepositoryRoot();
        var runnerRoot = Path.Combine(root, "artifacts", "comparison", "upstream-shared-sample-runner");
        var sourceRoot = Path.Combine(runnerRoot, "src");
        Directory.CreateDirectory(sourceRoot);

        var upstreamRoot = UpstreamReferencePaths.FindUpstreamRoot().Replace("\\", "/", StringComparison.Ordinal);
        var cargoToml = $$"""
[package]
name = "frankentui_net_shared_sample_runner"
version = "0.1.0"
edition = "2024"

[dependencies]
ftui-core = { path = "{{upstreamRoot}}/crates/ftui-core" }
ftui-render = { path = "{{upstreamRoot}}/crates/ftui-render" }
serde_json = "1.0"
""";

        var mainSource = $$"""
use std::cell::RefCell;
use std::io::{self, Write};
use std::rc::Rc;

use ftui_core::inline_mode::{InlineConfig, InlineRenderer, InlineStrategy};
use ftui_render::buffer::Buffer;
use ftui_render::cell::Cell;
use serde_json::json;

#[derive(Clone, Default)]
struct SharedWriter(Rc<RefCell<Vec<u8>>>);

impl Write for SharedWriter {
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
        self.0.borrow_mut().extend_from_slice(buf);
        Ok(buf.len())
    }

    fn flush(&mut self) -> io::Result<()> {
        Ok(())
    }
}

fn write_text(buffer: &mut Buffer, row: u16, text: &str) {
    let mut x = 0u16;
    for ch in text.chars() {
        if x >= buffer.width() {
            break;
        }

        buffer.set(x, row, Cell::from_char(ch));
        x = x.saturating_add(1);
    }
}

fn row_text(buffer: &Buffer, row: u16) -> String {
    let mut text = String::new();
    for x in 0..buffer.width() {
        let cell = buffer.get(x, row).expect("cell");
        if cell.is_continuation() {
            continue;
        }

        if cell.is_empty() {
            text.push(' ');
            continue;
        }

        match cell.content.as_char() {
            Some(ch) => text.push(ch),
            None => text.push('□'),
        }
    }

    text.trim_end().to_string()
}

fn frame_json(label: &str, buffer: &Buffer) -> serde_json::Value {
    let rows = (0..buffer.height())
        .map(|row| row_text(buffer, row))
        .collect::<Vec<_>>();
    json!({
        "label": label,
        "width": buffer.width(),
        "height": buffer.height(),
        "rows": rows,
    })
}

fn text_frame_json(label: &str, text: &str) -> serde_json::Value {
    json!({
        "label": label,
        "width": text.chars().count(),
        "height": 1,
        "rows": [text],
    })
}

fn counter_flow() -> serde_json::Value {
    let mut value = 0i32;

    let init = {
        let mut buffer = Buffer::new(16, 2);
        write_text(&mut buffer, 0, &format!("count={value}"));
        frame_json("init", &buffer)
    };

    value += 1;
    value += 1;
    let after_increment_pair = {
        let mut buffer = Buffer::new(16, 2);
        write_text(&mut buffer, 0, &format!("count={value}"));
        frame_json("after_increment_pair", &buffer)
    };

    value += 1;
    let after_resize_tick = {
        let mut buffer = Buffer::new(20, 3);
        write_text(&mut buffer, 0, &format!("count={value}"));
        frame_json("after_resize_tick", &buffer)
    };

    value -= 1;
    value += 1;
    let after_minus_tick = {
        let mut buffer = Buffer::new(20, 3);
        write_text(&mut buffer, 0, &format!("count={value}"));
        frame_json("after_minus_tick", &buffer)
    };

    json!({
        "name": "counter_flow",
        "frames": [init, after_increment_pair, after_resize_tick, after_minus_tick],
    })
}

fn unicode_cells() -> serde_json::Value {
    let mut buffer = Buffer::new(5, 2);
    write_text(&mut buffer, 0, "Hi");
    buffer.set(0, 1, Cell::from_char('😀'));

    json!({
        "name": "unicode_cells",
        "frames": [frame_json("emoji_row", &buffer)],
    })
}

fn wide_overwrite() -> serde_json::Value {
    let initial = {
        let mut buffer = Buffer::new(5, 1);
        buffer.set(0, 0, Cell::from_char('😀'));
        frame_json("wide_initial", &buffer)
    };
    let replaced = {
        let mut buffer = Buffer::new(5, 1);
        buffer.set(0, 0, Cell::from_char('A'));
        frame_json("wide_replaced", &buffer)
    };

    json!({
        "name": "wide_overwrite",
        "frames": [initial, replaced],
    })
}

fn inline_overlay() -> serde_json::Value {
    let writer = SharedWriter::default();
    let mut renderer = InlineRenderer::new(
        writer.clone(),
        InlineConfig::new(2, 6, 10).with_strategy(InlineStrategy::OverlayRedraw),
    );

    renderer
        .present_ui(|w, config| {
            w.write_all(b"UI")?;
            let second_row = config.ui_top_row() + 1;
            w.write_all(format!("\x1b[{};1HPane", second_row).as_bytes())?;
            Ok(())
        })
        .expect("present");
    renderer
        .write_log("log!\nsecond")
        .expect("write_log");
    drop(renderer);

    json!({
        "name": "inline_overlay",
        "frames": [{
            "label": "overlay_rows",
            "width": 10,
            "height": 6,
            "rows": ["", "", "", "log!", "UI", "Pane"],
        }],
    })
}

fn main() {
    let doc = json!({
        "implementation": "FrankenTUI upstream",
        "basis_commit": "{{UpstreamReferencePaths.BasisCommit}}",
        "samples": [counter_flow(), unicode_cells(), wide_overwrite(), inline_overlay(), command_palette(), log_search()],
    });

    println!("{}", serde_json::to_string_pretty(&doc).expect("json"));
}

fn command_palette() -> serde_json::Value {
    json!({
        "name": "command_palette",
        "frames": [{
            "label": "query_do",
            "width": 32,
            "height": 3,
            "rows": [
                "Run Doctor|Ctrl+D",
                "Go to Dashboard|g d",
                "Record Macro|r"
            ],
        }],
    })
}

fn log_search() -> serde_json::Value {
    json!({
        "name": "log_search",
        "frames": [{
            "label": "doctor_context",
            "width": 48,
            "height": 2,
            "rows": [
                "08:00:01 info  «doctor» replay refreshed",
                "08:00:02 warn  pane snapshot drift detected"
            ],
        }],
    })
}
""";

        WriteFileIfChanged(Path.Combine(runnerRoot, "Cargo.toml"), cargoToml);
        WriteFileIfChanged(Path.Combine(sourceRoot, "main.rs"), mainSource);
        return runnerRoot;
    }

    private static void WriteFileIfChanged(string path, string content)
    {
        if (File.Exists(path) && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
        {
            return;
        }

        File.WriteAllText(path, content);
    }

    private sealed class CounterFlowProgram : IAppProgram<int, string>
    {
        public int Initialize() => 0;

        public UpdateResult<int, string> Update(int model, string message) =>
            message switch
            {
                "inc" => UpdateResult<int, string>.FromModel(model + 1),
                "dec" => UpdateResult<int, string>.FromModel(model - 1),
                "tick" => UpdateResult<int, string>.FromModel(model + 1),
                _ when message.StartsWith("resize:", StringComparison.Ordinal) => UpdateResult<int, string>.FromModel(model),
                _ => UpdateResult<int, string>.FromModel(model)
            };

        public IRuntimeView BuildView(int model) => new ParagraphWidget($"count={model}");
    }

    private static string? TryFindCommand(string command)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var segment in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(segment, command);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

public sealed record SharedSampleFrame(
    string Label,
    ushort Width,
    ushort Height,
    IReadOnlyList<string> Rows,
    string Text,
    string Fingerprint)
{
    public static SharedSampleFrame Create(string label, ushort width, ushort height, IReadOnlyList<string> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(rows);

        var normalizedRows = rows.ToArray();
        var text = string.Join('\n', normalizedRows);
        return new SharedSampleFrame(
            label,
            width,
            height,
            normalizedRows,
            text,
            ComputeHash(label, width.ToString(), height.ToString(), text));
    }

    internal object ToDocument() => new
    {
        label = Label,
        width = Width,
        height = Height,
        rows = Rows,
        text = Text,
        fingerprint = Fingerprint
    };

    private static string ComputeHash(params string[] parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(part).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}

public sealed record SharedSampleCase(
    string Name,
    IReadOnlyList<SharedSampleFrame> Frames,
    string Fingerprint)
{
    public static SharedSampleCase Create(string name, IReadOnlyList<SharedSampleFrame> frames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(frames);

        var normalizedFrames = frames.ToArray();
        var fingerprint = Convert.ToHexStringLower(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    string.Join('\n', normalizedFrames.Select(static frame => frame.Fingerprint)))));
        return new SharedSampleCase(name, normalizedFrames, fingerprint);
    }

    internal object ToDocument() => new
    {
        name = Name,
        fingerprint = Fingerprint,
        frames = Frames.Select(static frame => frame.ToDocument()).ToArray()
    };
}

public sealed record SharedSampleCapture(
    string Implementation,
    string BasisCommit,
    IReadOnlyList<SharedSampleCase> Samples)
{
    public string Fingerprint =>
        Convert.ToHexStringLower(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    string.Join('\n', Samples.Select(static sample => sample.Fingerprint)))));

    public string Json =>
        JsonSerializer.Serialize(
            new
            {
                implementation = Implementation,
                basis_commit = BasisCommit,
                fingerprint = Fingerprint,
                samples = Samples.Select(static sample => sample.ToDocument()).ToArray()
            },
            HarnessJson.IndentedSnakeCase);

    public IReadOnlyDictionary<string, string> WriteArtifacts(string prefix, string category = "comparison")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var capturePath = ArtifactPathBuilder.For(category, $"{prefix}.json");
        File.WriteAllText(capturePath, Json);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["capture"] = capturePath
        };
    }
}

public sealed record SharedSampleComparisonReport(
    SharedSampleCapture Local,
    SharedSampleCapture Upstream,
    IReadOnlyList<string> Differences)
{
    public bool IsMatch => Differences.Count == 0;

    public string Json =>
        JsonSerializer.Serialize(
            new
            {
                is_match = IsMatch,
                local = new
                {
                    implementation = Local.Implementation,
                    basis_commit = Local.BasisCommit,
                    fingerprint = Local.Fingerprint
                },
                upstream = new
                {
                    implementation = Upstream.Implementation,
                    basis_commit = Upstream.BasisCommit,
                    fingerprint = Upstream.Fingerprint
                },
                differences = Differences
            },
            HarnessJson.IndentedSnakeCase);

    public string DifferenceSummary() =>
        IsMatch
            ? "Shared sample comparison matched."
            : string.Join(Environment.NewLine, Differences);

    public IReadOnlyDictionary<string, string> WriteArtifacts(string prefix = SharedSampleComparison.DefaultArtifactName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var artifacts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in Local.WriteArtifacts($"{prefix}.dotnet"))
        {
            artifacts[$"local_{entry.Key}"] = entry.Value;
        }

        foreach (var entry in Upstream.WriteArtifacts($"{prefix}.upstream"))
        {
            artifacts[$"upstream_{entry.Key}"] = entry.Value;
        }

        var reportPath = ArtifactPathBuilder.For("comparison", $"{prefix}.report.json");
        File.WriteAllText(reportPath, Json);
        artifacts["report"] = reportPath;
        return artifacts;
    }

    public static SharedSampleComparisonReport Create(SharedSampleCapture local, SharedSampleCapture upstream)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(upstream);

        var differences = new List<string>();
        var localSamples = local.Samples.ToDictionary(static sample => sample.Name, StringComparer.Ordinal);
        var upstreamSamples = upstream.Samples.ToDictionary(static sample => sample.Name, StringComparer.Ordinal);

        foreach (var name in localSamples.Keys.Union(upstreamSamples.Keys, StringComparer.Ordinal).OrderBy(static value => value, StringComparer.Ordinal))
        {
            var hasLocal = localSamples.TryGetValue(name, out var localSample);
            var hasUpstream = upstreamSamples.TryGetValue(name, out var upstreamSample);
            if (!hasLocal || !hasUpstream)
            {
                differences.Add($"sample:{name}: presence mismatch (local={hasLocal}, upstream={hasUpstream})");
                continue;
            }

            CompareSample(localSample!, upstreamSample!, differences);
        }

        if (!string.Equals(local.BasisCommit, upstream.BasisCommit, StringComparison.Ordinal))
        {
            differences.Add($"basis_commit mismatch: local={local.BasisCommit} upstream={upstream.BasisCommit}");
        }

        return new SharedSampleComparisonReport(local, upstream, differences);
    }

    private static void CompareSample(SharedSampleCase local, SharedSampleCase upstream, List<string> differences)
    {
        if (local.Frames.Count != upstream.Frames.Count)
        {
            differences.Add($"sample:{local.Name}: frame count mismatch (local={local.Frames.Count}, upstream={upstream.Frames.Count})");
            return;
        }

        for (var index = 0; index < local.Frames.Count; index++)
        {
            var localFrame = local.Frames[index];
            var upstreamFrame = upstream.Frames[index];
            if (!string.Equals(localFrame.Label, upstreamFrame.Label, StringComparison.Ordinal))
            {
                differences.Add($"sample:{local.Name}: frame {index} label mismatch (local={localFrame.Label}, upstream={upstreamFrame.Label})");
            }

            if (localFrame.Width != upstreamFrame.Width || localFrame.Height != upstreamFrame.Height)
            {
                differences.Add(
                    $"sample:{local.Name}: frame {localFrame.Label} size mismatch (local={localFrame.Width}x{localFrame.Height}, upstream={upstreamFrame.Width}x{upstreamFrame.Height})");
            }

            if (!string.Equals(localFrame.Text, upstreamFrame.Text, StringComparison.Ordinal))
            {
                differences.Add(
                    $"sample:{local.Name}: frame {localFrame.Label} text mismatch (local='{localFrame.Text}', upstream='{upstreamFrame.Text}')");
            }
        }
    }
}
