using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public sealed class FormWidget : IWidget
{
    public IReadOnlyList<FormTextField> Fields { get; init; } = [];

    public FormValidationResult Validation { get; init; } = FormValidationResult.Empty;

    public int SelectedFieldIndex { get; init; } = -1;

    public void Render(RuntimeRenderContext context)
    {
        var row = 0;
        for (var index = 0; index < Fields.Count && row < context.Bounds.Height; index++)
        {
            var field = Fields[index];
            var messages = Validation.ForField(field.Id);
            var style = index == SelectedFieldIndex
                ? context.Theme.Selection
                : messages.Count > 0
                    ? FormValidator.ToStyle(messages[0].Severity)
                    : context.Theme.Default;

            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + row),
                $"{field.Label}: {field.Value}",
                style.ToCell());
            row++;

            if (!string.IsNullOrWhiteSpace(field.HelpText) && row < context.Bounds.Height)
            {
                BufferPainter.WriteText(
                    context.Buffer,
                    (ushort)(context.Bounds.X + 2),
                    (ushort)(context.Bounds.Y + row),
                    field.HelpText,
                    context.Theme.Muted.ToCell());
                row++;
            }
        }

        if (context.Bounds.Height > 0)
        {
            var summary = Validation.Messages.Count == 0
                ? "Validation: clean"
                : $"Validation: {Validation.ErrorCount} errors, {Validation.WarningCount} warnings";
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Bottom - 1),
                summary,
                (Validation.HasErrors ? context.Theme.Danger : context.Theme.Success).ToCell());
        }
    }
}

public sealed class ValidationSummaryWidget : IWidget
{
    public FormValidationResult Validation { get; init; } = FormValidationResult.Empty;

    public void Render(RuntimeRenderContext context)
    {
        if (Validation.Messages.Count == 0)
        {
            new ParagraphWidget("No validation issues.")
            {
                Style = context.Theme.Success
            }.Render(context);
            return;
        }

        var items = Validation.Messages
            .Take(context.Bounds.Height)
            .Select(message => $"{message.FieldId}: {message.Message}")
            .ToArray();
        new ListWidget
        {
            Items = items
        }.Render(context);
    }
}
