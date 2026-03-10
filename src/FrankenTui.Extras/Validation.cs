using FrankenTui.Style;

namespace FrankenTui.Extras;

public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record ValidationMessage(
    string Code,
    string FieldId,
    string Message,
    ValidationSeverity Severity = ValidationSeverity.Error);

public delegate IEnumerable<ValidationMessage> TextValidator(FormTextField field);

public sealed record FormTextField(
    string Id,
    string Label,
    string Value,
    string? HelpText = null,
    bool HasFocus = false);

public sealed record FormValidationResult(IReadOnlyList<ValidationMessage> Messages)
{
    public static FormValidationResult Empty { get; } = new([]);

    public int ErrorCount => Messages.Count(static message => message.Severity == ValidationSeverity.Error);

    public int WarningCount => Messages.Count(static message => message.Severity == ValidationSeverity.Warning);

    public bool HasErrors => ErrorCount > 0;

    public IReadOnlyList<ValidationMessage> ForField(string fieldId) =>
        Messages.Where(message => string.Equals(message.FieldId, fieldId, StringComparison.Ordinal)).ToArray();
}

public static class ValidationRules
{
    public static TextValidator Required(string? message = null) =>
        field => string.IsNullOrWhiteSpace(field.Value)
            ? [new ValidationMessage("required", field.Id, message ?? $"{field.Label} is required.")]
            : [];

    public static TextValidator MinLength(int length, string? message = null) =>
        field => field.Value.Trim().Length < length
            ? [new ValidationMessage("min-length", field.Id, message ?? $"{field.Label} must be at least {length} characters.")]
            : [];

    public static TextValidator ContainsDigit(string? message = null) =>
        field => field.Value.Any(char.IsDigit)
            ? []
            : [new ValidationMessage("digit", field.Id, message ?? $"{field.Label} must include a digit.", ValidationSeverity.Warning)];
}

public static class FormValidator
{
    public static FormValidationResult Validate(
        IReadOnlyList<FormTextField> fields,
        IReadOnlyDictionary<string, IReadOnlyList<TextValidator>> validators)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(validators);

        var messages = new List<ValidationMessage>();
        foreach (var field in fields)
        {
            if (!validators.TryGetValue(field.Id, out var fieldValidators))
            {
                continue;
            }

            foreach (var validator in fieldValidators)
            {
                messages.AddRange(validator(field));
            }
        }

        return new FormValidationResult(messages);
    }

    public static UiStyle ToStyle(ValidationSeverity severity) => severity switch
    {
        ValidationSeverity.Info => UiStyle.Muted,
        ValidationSeverity.Warning => UiStyle.Warning,
        _ => UiStyle.Danger
    };
}
