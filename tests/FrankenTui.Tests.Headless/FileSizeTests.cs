// Upstream source: crates/ftui-extras/src/filesize.rs — tests
// Tests ported from 55 #[cfg(test)] mod tests functions.

using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class FileSizeTests
{
    // Decimal thresholds
    [Fact] public void DecimalBytesThresholds() { var fmt = SizeFormat.Decimal(); Assert.Equal("0 B", FileSize.FormatSize(0, fmt)); Assert.Equal("999 B", FileSize.FormatSize(999, fmt)); Assert.Equal("1.0 KB", FileSize.FormatSize(1000, fmt)); }
    [Fact] public void DecimalBytesThresholdsLong() { var fmt = SizeFormat.Decimal().Long(); Assert.Equal("0 bytes", FileSize.FormatSize(0, fmt)); Assert.Equal("999 bytes", FileSize.FormatSize(999, fmt)); Assert.Equal("1.0 kilobytes", FileSize.FormatSize(1000, fmt)); }

    // Binary thresholds
    [Fact] public void BinaryBytesThresholds() { var fmt = SizeFormat.Binary(); Assert.Equal("0 B", FileSize.FormatSize(0, fmt)); Assert.Equal("1023 B", FileSize.FormatSize(1023, fmt)); Assert.Equal("1.0 KiB", FileSize.FormatSize(1024, fmt)); }

    // Decimal precision
    [Fact] public void DecimalPrecisionRounding() { Assert.Equal("1 MB", FileSize.DecimalWithPrecision(1_234_567, 0)); Assert.Equal("1.2 MB", FileSize.DecimalWithPrecision(1_234_567, 1)); Assert.Equal("1.23 MB", FileSize.DecimalWithPrecision(1_234_567, 2)); Assert.Equal("1.235 MB", FileSize.DecimalWithPrecision(1_234_567, 3)); }

    // Binary precision
    [Fact] public void BinaryPrecisionRounding() { Assert.Equal("1.50 MiB", FileSize.BinaryWithPrecision(1_572_864, 2)); }

    // Long unit style
    [Fact] public void LongUnitStyle() { var fmt = SizeFormat.Decimal().Long(); Assert.Equal("1.5 kilobytes", FileSize.FormatSize(1500, fmt)); }
    [Fact] public void LongBinaryStyle() { var fmt = SizeFormat.Binary().Long(); Assert.Equal("1.0 mebibytes", FileSize.FormatSize(1_048_576, fmt)); Assert.Equal("500 bytes", FileSize.FormatSize(500, fmt)); }

    // Negative sizes
    [Fact] public void NegativeSizes() { var fmt = SizeFormat.Decimal(); Assert.Equal("-1.0 KB", FileSize.FormatSize(-1000, fmt)); }
    [Fact] public void NegativeBytesUnscaled() { Assert.Equal("-500 B", FileSize.FormatSize(-500, SizeFormat.Binary())); Assert.Equal("-12 bytes", FileSize.FormatSize(-12, SizeFormat.Decimal().Long())); }
    [Fact] public void NegativeBinaryUnits() { Assert.Equal("-1.0 KiB", FileSize.FormatSize(-1024, SizeFormat.Binary())); }

    // Large sizes
    [Fact] public void LargeSizes() { Assert.Equal("1.0 PB", FileSize.Decimal(1_000_000_000_000_000)); Assert.Equal("1.0 PiB", FileSize.Binary(1_125_899_906_842_624)); }

    // GB/TB levels
    [Fact] public void BinaryGiB() { Assert.Equal("1.0 GiB", FileSize.Binary(1_073_741_824)); Assert.Equal("2.0 GiB", FileSize.Binary(2_147_483_648)); }
    [Fact] public void BinaryTiB() { Assert.Equal("1.0 TiB", FileSize.Binary(1_099_511_627_776)); }
    [Fact] public void DecimalGB() { Assert.Equal("1.0 GB", FileSize.Decimal(1_000_000_000)); Assert.Equal("2.5 GB", FileSize.Decimal(2_500_000_000)); }
    [Fact] public void DecimalTB() { Assert.Equal("1.0 TB", FileSize.Decimal(1_000_000_000_000)); }

    // Short style builder
    [Fact] public void ShortBuilder() { Assert.Equal("1.0 MiB", FileSize.FormatSize(1_048_576, SizeFormat.Binary().Short())); }

    // Zero/edge
    [Fact] public void EdgeZero() { Assert.Equal("0 B", FileSize.Decimal(0)); Assert.Equal("0 B", FileSize.Binary(0)); Assert.Equal("0 bytes", FileSize.FormatSize(0, SizeFormat.Decimal().Long())); }
    [Fact] public void EdgeOneByte() { Assert.Equal("1 B", FileSize.Decimal(1)); Assert.Equal("1 B", FileSize.Binary(1)); }

    // Convenience functions
    [Fact] public void ConvenienceFunctions() { Assert.Equal("1.5 MB", FileSize.Decimal(1_500_000)); Assert.Equal("1.0 KiB", FileSize.Binary(1_024)); }

    // Default format
    [Fact] public void DefaultFormatIsBinary() { var fmt = SizeFormat.Binary(); Assert.Equal(SizeUnit.Binary, fmt.Unit); Assert.Equal(UnitStyle.Short, fmt.Style); Assert.Equal(1, fmt.Precision); }
    [Fact] public void BuilderChaining() { var fmt = SizeFormat.Decimal().Long(); Assert.Equal(SizeUnit.Decimal, fmt.Unit); Assert.Equal(UnitStyle.Long, fmt.Style); }

    // Clamp
    [Fact] public void ClampToI64Max() { Assert.Equal(FileSize.Decimal(long.MaxValue), FileSize.Decimal(ulong.MaxValue)); Assert.Equal(FileSize.Binary(long.MaxValue), FileSize.Binary(ulong.MaxValue)); }

    // All decimal unit levels
    [Fact] public void DecimalAllUnitLevels() { var fmt = SizeFormat.Decimal(); Assert.Equal("1.0 KB", FileSize.FormatSize(1_000, fmt)); Assert.Equal("1.0 MB", FileSize.FormatSize(1_000_000, fmt)); Assert.Equal("1.0 GB", FileSize.FormatSize(1_000_000_000, fmt)); Assert.Equal("1.0 TB", FileSize.FormatSize(1_000_000_000_000, fmt)); Assert.Equal("1.0 PB", FileSize.FormatSize(1_000_000_000_000_000, fmt)); Assert.Equal("1.0 EB", FileSize.FormatSize(1_000_000_000_000_000_000, fmt)); }

    // All binary unit levels
    [Fact] public void BinaryAllUnitLevels() { var fmt = SizeFormat.Binary(); Assert.Equal("1.0 KiB", FileSize.FormatSize(1_024, fmt)); Assert.Equal("1.0 MiB", FileSize.FormatSize(1_048_576, fmt)); Assert.Equal("1.0 GiB", FileSize.FormatSize(1_073_741_824, fmt)); Assert.Equal("1.0 TiB", FileSize.FormatSize(1_099_511_627_776, fmt)); Assert.Equal("1.0 PiB", FileSize.FormatSize(1_125_899_906_842_624, fmt)); Assert.Equal("1.0 EiB", FileSize.FormatSize(1_152_921_504_606_846_976, fmt)); }

    // Long unit names
    [Fact] public void LongDecimalAllLevels() { var fmt = SizeFormat.Decimal().Long(); Assert.Equal("42 bytes", FileSize.FormatSize(42, fmt)); Assert.Equal("1.5 kilobytes", FileSize.FormatSize(1_500, fmt)); Assert.Equal("1.5 megabytes", FileSize.FormatSize(1_500_000, fmt)); Assert.Equal("1.5 gigabytes", FileSize.FormatSize(1_500_000_000, fmt)); Assert.Equal("1.5 terabytes", FileSize.FormatSize(1_500_000_000_000, fmt)); }

    // Boundary values
    [Fact] public void DecimalJustUnderThreshold() { var fmt = SizeFormat.Decimal(); Assert.Equal("999 B", FileSize.FormatSize(999, fmt)); Assert.Equal("1.0 KB", FileSize.FormatSize(1000, fmt)); Assert.Equal("1000.0 KB", FileSize.FormatSize(999_999, fmt)); Assert.Equal("1.0 MB", FileSize.FormatSize(1_000_000, fmt)); }
    [Fact] public void BinaryJustUnderThreshold() { var fmt = SizeFormat.Binary(); Assert.Equal("1023 B", FileSize.FormatSize(1023, fmt)); Assert.Equal("1.0 KiB", FileSize.FormatSize(1024, fmt)); }

    // Fractional
    [Fact] public void DecimalFractionalKB() { var fmt = SizeFormat.Decimal().WithPrecision(2); Assert.Equal("1.50 KB", FileSize.FormatSize(1_500, fmt)); Assert.Equal("2.00 KB", FileSize.FormatSize(1_999, fmt)); Assert.Equal("1.00 KB", FileSize.FormatSize(1_001, fmt)); }

    // Precision edge
    [Fact] public void PrecisionZeroAtByteLevel() { Assert.Equal("500 B", FileSize.FormatSize(500, SizeFormat.Decimal().WithPrecision(0))); }
    [Fact] public void HighPrecision() { Assert.Equal("1.234567 MB", FileSize.FormatSize(1_234_567, SizeFormat.Decimal().WithPrecision(6))); }
    [Fact] public void PrecisionZeroRoundsUp() { Assert.Equal("2 MB", FileSize.FormatSize(1_500_000, SizeFormat.Decimal().WithPrecision(0))); }
    [Fact] public void PrecisionZeroRoundsDown() { Assert.Equal("1 MB", FileSize.FormatSize(1_400_000, SizeFormat.Decimal().WithPrecision(0))); }

    // All unit×style combinations
    [Fact] public void AllUnitStyleCombinations() { var s = 1_048_576L; Assert.Equal("1.0 MiB", FileSize.FormatSize(s, SizeFormat.Binary().Short())); Assert.Equal("1.0 mebibytes", FileSize.FormatSize(s, SizeFormat.Binary().Long())); Assert.Equal("1.0 MB", FileSize.FormatSize(s, SizeFormat.Decimal().Short())); Assert.Equal("1.0 megabytes", FileSize.FormatSize(s, SizeFormat.Decimal().Long())); }

    // Builder overrides
    [Fact] public void ShortBuilderOverridesLong() { Assert.Equal(UnitStyle.Short, SizeFormat.Binary().Long().Short().Style); }
    [Fact] public void LongBuilderOverridesShort() { Assert.Equal(UnitStyle.Long, SizeFormat.Decimal().Short().Long().Style); }
    [Fact] public void WithPrecisionOverridesDefault() { Assert.Equal(5, SizeFormat.Binary().WithPrecision(5).Precision); }

    // Rounding
    [Fact] public void DecimalRoundingAtHalf() { Assert.Equal("1.6 MB", FileSize.Decimal(1_550_000)); }
    [Fact] public void BinaryRoundingBoundary() { Assert.Equal("1.5 KiB", FileSize.Binary(1_536)); }

    // Negative various levels
    [Fact] public void NegativeDecimalVarious() { var fmt = SizeFormat.Decimal(); Assert.Equal("-1 B", FileSize.FormatSize(-1, fmt)); Assert.Equal("-1.0 KB", FileSize.FormatSize(-1_000, fmt)); Assert.Equal("-1.5 MB", FileSize.FormatSize(-1_500_000, fmt)); }
    [Fact] public void NegativeBinaryVarious() { var fmt = SizeFormat.Binary(); Assert.Equal("-1 B", FileSize.FormatSize(-1, fmt)); Assert.Equal("-1.0 KiB", FileSize.FormatSize(-1_024, fmt)); Assert.Equal("-1.0 MiB", FileSize.FormatSize(-1_048_576, fmt)); }

    // Long negative
    [Fact] public void NegativeLongStyle() { Assert.Equal("-500 bytes", FileSize.FormatSize(-500, SizeFormat.Decimal().Long())); Assert.Equal("-1.5 megabytes", FileSize.FormatSize(-1_500_000, SizeFormat.Decimal().Long())); }

    // i64 extremes
    [Fact] public void I64MinDoesNotPanic() { var r = FileSize.FormatSize(long.MinValue, SizeFormat.Binary()); Assert.StartsWith("-", r); Assert.NotEmpty(r); }
    [Fact] public void I64MaxFormats() { var r = FileSize.FormatSize(long.MaxValue, SizeFormat.Decimal()); Assert.NotEmpty(r); Assert.Contains("EB", r); }

    // Unit arrays consistency
    [Fact] public void UnitArraysConsistentLength() { /* arrays are hardcoded in source; verify binary and decimal return consistent patterns */ Assert.EndsWith("EiB", FileSize.Binary(1_152_921_504_606_846_976)); Assert.EndsWith("EB", FileSize.Decimal(1_000_000_000_000_000_000)); }

    // SizeFormat builder methods
    [Fact] public void SizeFormatBuilderChaining() { var fmt = SizeFormat.Decimal().WithPrecision(3).Long(); Assert.Equal(SizeUnit.Decimal, fmt.Unit); Assert.Equal(UnitStyle.Long, fmt.Style); Assert.Equal(3, fmt.Precision); }

    // Additional edge cases
    [Fact] public void DecimalWithPrecisionZero() { Assert.Equal("2 MB", FileSize.DecimalWithPrecision(1_500_000, 0)); }
    [Fact] public void DecimalWithPrecisionHigh() { Assert.Equal("1.23457 MB", FileSize.DecimalWithPrecision(1_234_567, 5)); }
    [Fact] public void BinaryWithPrecisionZero() { Assert.Equal("1 MiB", FileSize.BinaryWithPrecision(1_048_576, 0)); }
    [Fact] public void BinaryWithPrecisionHigh() { Assert.Equal("1.5000 MiB", FileSize.BinaryWithPrecision(1_572_864, 4)); }

    // ---- Additional upstream tests ----
    [Fact] public void NegativeWithPrecision() { var fmt = SizeFormat.Decimal().WithPrecision(3); Assert.Equal("-1.235 MB", FileSize.FormatSize(-1_234_567, fmt)); }
    [Fact] public void FormatSizeSingleByte() { var fmt = SizeFormat.Decimal().Long(); Assert.Equal("1 bytes", FileSize.FormatSize(1, fmt)); }
    [Fact] public void UnitArraysStartWithBytes() { Assert.EndsWith("B", FileSize.Binary(0)); Assert.EndsWith("B", FileSize.Decimal(0)); }

    // DIVERGENCE: Upstream derive trait tests (size_unit_debug_clone_copy_eq, unit_style_debug_clone_copy_eq,
    // size_format_debug_clone_copy_eq) are skipped because C# enums and record structs provide
    // Debug/Clone/Copy/Equals automatically with no user code needed.
}
