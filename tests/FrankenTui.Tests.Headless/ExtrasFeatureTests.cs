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
        Assert.Contains("Markdown", screen);
        Assert.Contains("Validation", screen);
    }
}
