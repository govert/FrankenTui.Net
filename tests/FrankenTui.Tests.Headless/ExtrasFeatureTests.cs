using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class ExtrasFeatureTests
{
    [Fact]
    public void MarkdownBuilderParsesHeadingsLinksAndCode()
    {
        var document = MarkdownDocumentBuilder.Parse(
            """
            # Extras
            - **Markdown** [docs](https://example.invalid/docs)

            ```csharp
            var value = 42;
            ```
            """);

        Assert.Contains("Extras", document.PlainText);
        Assert.Contains("<https://example.invalid/docs>", document.PlainText);
        Assert.Contains(
            document.Lines.SelectMany(static line => line.Spans),
            static span => span.Text == "var" && span.Style == UiStyle.Accent);
    }

    [Fact]
    public void MarkdownBuilderCachesDocumentsAndMathConversions()
    {
        MarkdownDocumentBuilder.ClearCaches();
        const string markdown = "Inline $\\alpha + \\beta$ and $\\alpha + \\beta$.\n\n$$\\sqrt{x}$$";

        var first = MarkdownDocumentBuilder.ParseCached(markdown);
        var second = MarkdownDocumentBuilder.ParseCached(markdown);

        Assert.Same(first, second);
        Assert.True(MarkdownDocumentBuilder.CachedDocumentCount >= 1);
        Assert.True(MarkdownDocumentBuilder.CachedMathCount >= 2);
        Assert.Contains("α + β", first.PlainText);
        Assert.Contains("√x", first.PlainText);
    }

    [Fact]
    public void PublicMarkdownHelperUsesCachedParsePath()
    {
        MarkdownDocumentBuilder.ClearCaches();
        const string markdown = "# Public\n- cached";

        var first = FrankenTui.Ui.Markdown(markdown);
        var second = FrankenTui.Ui.Markdown(markdown);

        Assert.Same(first.Document, second.Document);
        Assert.Contains("Public", first.Document.PlainText);
        Assert.True(MarkdownDocumentBuilder.CachedDocumentCount >= 1);
    }

    [Fact]
    public void MarkdownBuilderPreservesInlineStylingInsideTableCells()
    {
        var document = MarkdownDocumentBuilder.Parse(
            """
            | Feature | Link |
            | --- | --- |
            | **Docs** | [guide](https://example.invalid/guide) |
            """);

        var row = Assert.Single(document.Lines, static line => line.PlainText.Contains("guide", StringComparison.Ordinal));
        Assert.Contains(row.Spans, static span => span.Text == "Docs" && span.Style == UiStyle.Accent);
        Assert.Contains(row.Spans, static span => span.Text == "guide" && span.Style == UiStyle.Accent.WithFlags(CellStyleFlags.Underline));
        Assert.Contains(row.Spans, static span => span.Text == " <https://example.invalid/guide>" && span.Style == UiStyle.Muted);
    }

    [Fact]
    public void BufferExportAndConsoleTextExposeVisibleContent()
    {
        var buffer = new RenderBuffer(24, 4);
        new ParagraphWidget("Export ready").Render(new RuntimeRenderContext(buffer, Rect.FromSize(24, 4), Theme.DefaultTheme));

        var export = BufferExport.Capture(buffer);
        var stripped = ConsoleText.StripAnsi("\u001b[32mExport ready\u001b[0m");

        Assert.Contains("Export ready", export.PlainText);
        Assert.Contains("<!doctype html>", export.Html);
        Assert.Equal("Export ready", stripped.Trim());
    }

    [Fact]
    public void FormValidationAndTracebackHelpersRender()
    {
        var fields = new[]
        {
            new FormTextField("repo", "Repository", "ftui"),
            new FormTextField("seed", "Seed", "abc")
        };
        var validation = FormValidator.Validate(
            fields,
            new Dictionary<string, IReadOnlyList<TextValidator>>(StringComparer.Ordinal)
            {
                ["repo"] = [ValidationRules.MinLength(6)],
                ["seed"] = [ValidationRules.ContainsDigit()]
            });

        var buffer = new RenderBuffer(48, 8);
        new FormWidget
        {
            Fields = fields,
            Validation = validation,
            SelectedFieldIndex = 0
        }.Render(new RuntimeRenderContext(buffer, Rect.FromSize(48, 8), Theme.DefaultTheme));

        var trace = TracebackView.FromException(new InvalidOperationException("Validation failed."));
        var screen = HeadlessBufferView.ScreenString(buffer);

        Assert.Contains("Repository", screen);
        Assert.Equal(2, validation.Messages.Count);
        Assert.Contains("InvalidOperationException", trace.PlainText);
    }

    [Fact]
    public void ExtrasScenarioRendersMaterialExtrasSurface()
    {
        var session = HostedParitySession.ForFrame(false, 2, HostedParityScenarioId.Extras);
        var buffer = new RenderBuffer(72, 18);

        HostedParitySurface.Create(session)
            .Render(new RuntimeRenderContext(buffer, Rect.FromSize(72, 18), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Extras", screen);
        Assert.Contains("Pane Workspace", screen);
        Assert.Contains("Command Palette", screen);
        Assert.Contains("Mermaid Showcase", screen);
    }
}
