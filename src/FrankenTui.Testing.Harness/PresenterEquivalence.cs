using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FrankenTui.Render;

namespace FrankenTui.Testing.Harness;

public enum EquivalenceClass
{
    CursorPath,
    ResetVariant,
    StyleOrder,
    RedundantState,
    BatchedWrites,
    TrailingWhitespace
}

public enum TypicalSavings
{
    Negligible,
    Small,
    Medium,
    Large
}

public enum NonEquivalentVariation
{
    MissingRowReset,
    WrongCursorPosition,
    DroppedAttribute,
    ColorDepthMismatch,
    ContentMismatch,
    WideCharMisalignment,
    HyperlinkStateLeak
}

public enum ViolationSeverity
{
    Minor,
    Major,
    Critical
}

public sealed record PresenterEquivalenceViolation(
    NonEquivalentVariation Variation,
    ViolationSeverity Severity,
    string Detail);

public sealed record PresenterEquivalenceReport(
    bool Equivalent,
    string ExpectedChecksum,
    string ActualChecksum,
    IReadOnlyList<EquivalenceClass> SafeClasses,
    IReadOnlyList<PresenterEquivalenceViolation> Violations)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class PresenterEquivalence
{
    public static PresenterEquivalenceReport Compare(
        string expectedAnsi,
        string actualAnsi,
        ushort width,
        ushort height)
    {
        ArgumentNullException.ThrowIfNull(expectedAnsi);
        ArgumentNullException.ThrowIfNull(actualAnsi);

        var expectedModel = new TerminalModel(width, height);
        var actualModel = new TerminalModel(width, height);
        expectedModel.Process(expectedAnsi);
        actualModel.Process(actualAnsi);

        var violations = new List<PresenterEquivalenceViolation>();

        if (!string.Equals(expectedModel.ScreenString(), actualModel.ScreenString(), StringComparison.Ordinal))
        {
            violations.Add(new PresenterEquivalenceViolation(
                NonEquivalentVariation.ContentMismatch,
                ViolationSeverity.Critical,
                "Terminal-visible cell content diverged."));
        }

        if (expectedModel.CursorColumn != actualModel.CursorColumn || expectedModel.CursorRow != actualModel.CursorRow)
        {
            violations.Add(new PresenterEquivalenceViolation(
                NonEquivalentVariation.WrongCursorPosition,
                ViolationSeverity.Critical,
                $"Expected cursor {expectedModel.CursorColumn},{expectedModel.CursorRow} but saw {actualModel.CursorColumn},{actualModel.CursorRow}."));
        }

        if (expectedModel.ActiveLinkId != actualModel.ActiveLinkId)
        {
            violations.Add(new PresenterEquivalenceViolation(
                NonEquivalentVariation.HyperlinkStateLeak,
                ViolationSeverity.Minor,
                "Active hyperlink state diverged after presentation."));
        }

        for (ushort row = 0; row < height; row++)
        {
            for (ushort column = 0; column < width; column++)
            {
                var expectedCell = expectedModel.Cell(column, row);
                var actualCell = actualModel.Cell(column, row);
                if (expectedCell != actualCell)
                {
                    if (expectedCell?.Text != actualCell?.Text)
                    {
                        violations.Add(new PresenterEquivalenceViolation(
                            NonEquivalentVariation.ContentMismatch,
                            ViolationSeverity.Critical,
                            $"Cell {column},{row} text diverged."));
                    }
                    else if (expectedCell?.Attributes != actualCell?.Attributes)
                    {
                        violations.Add(new PresenterEquivalenceViolation(
                            NonEquivalentVariation.DroppedAttribute,
                            ViolationSeverity.Major,
                            $"Cell {column},{row} attributes diverged."));
                    }
                    else if (expectedCell?.Foreground != actualCell?.Foreground ||
                             expectedCell?.Background != actualCell?.Background)
                    {
                        violations.Add(new PresenterEquivalenceViolation(
                            NonEquivalentVariation.ColorDepthMismatch,
                            ViolationSeverity.Minor,
                            $"Cell {column},{row} colors diverged."));
                    }
                }
            }
        }

        return new PresenterEquivalenceReport(
            violations.Count == 0,
            ComputeChecksum(expectedAnsi),
            ComputeChecksum(actualAnsi),
            [
                EquivalenceClass.CursorPath,
                EquivalenceClass.ResetVariant,
                EquivalenceClass.StyleOrder,
                EquivalenceClass.RedundantState,
                EquivalenceClass.BatchedWrites,
                EquivalenceClass.TrailingWhitespace
            ],
            violations);
    }

    public static TypicalSavings SavingsFor(EquivalenceClass equivalenceClass) =>
        equivalenceClass switch
        {
            EquivalenceClass.CursorPath => TypicalSavings.Medium,
            EquivalenceClass.ResetVariant => TypicalSavings.Small,
            EquivalenceClass.StyleOrder => TypicalSavings.Negligible,
            EquivalenceClass.RedundantState => TypicalSavings.Large,
            EquivalenceClass.BatchedWrites => TypicalSavings.Medium,
            _ => TypicalSavings.Small
        };

    public static ViolationSeverity SeverityFor(NonEquivalentVariation variation) =>
        variation switch
        {
            NonEquivalentVariation.WrongCursorPosition => ViolationSeverity.Critical,
            NonEquivalentVariation.ContentMismatch => ViolationSeverity.Critical,
            NonEquivalentVariation.DroppedAttribute => ViolationSeverity.Major,
            NonEquivalentVariation.WideCharMisalignment => ViolationSeverity.Major,
            _ => ViolationSeverity.Minor
        };

    private static string ComputeChecksum(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
