// Upstream source: crates/ftui-extras/src/filesize.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of SizeUnit, UnitStyle, SizeFormat, format_size, and convenience functions.

namespace FrankenTui.Extras;

/// <summary>Short unit labels for binary (1024-based) formatting.</summary>
internal static class BinaryUnitLabels
{
    public static readonly string[] Short = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB"];
    public static readonly string[] Long = ["bytes", "kibibytes", "mebibytes", "gibibytes", "tebibytes", "pebibytes", "exbibytes", "zebibytes", "yobibytes"];
}

/// <summary>Short unit labels for decimal (1000-based) formatting.</summary>
internal static class DecimalUnitLabels
{
    public static readonly string[] Short = ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
    public static readonly string[] Long = ["bytes", "kilobytes", "megabytes", "gigabytes", "terabytes", "petabytes", "exabytes", "zettabytes", "yottabytes"];
}

/// <summary>Unit system to use when formatting sizes.</summary>
public enum SizeUnit
{
    /// <summary>Binary units (1024-based): KiB, MiB, GiB, etc.</summary>
    Binary,
    /// <summary>Decimal units (1000-based): KB, MB, GB, etc.</summary>
    Decimal,
}

/// <summary>Unit label style.</summary>
public enum UnitStyle
{
    /// <summary>Short labels, e.g. "KB" / "KiB".</summary>
    Short,
    /// <summary>Long labels, e.g. "kilobytes" / "kibibytes".</summary>
    Long,
}

/// <summary>Formatting configuration for file sizes.</summary>
public readonly record struct SizeFormat
{
    /// <summary>Unit system (binary or decimal).</summary>
    public SizeUnit Unit { get; init; }
    /// <summary>Unit label style (short or long).</summary>
    public UnitStyle Style { get; init; }
    /// <summary>Number of decimal places for non-byte units.</summary>
    public int Precision { get; init; }

    /// <summary>Binary units with short labels and 1 decimal place.</summary>
    public static SizeFormat Binary() => new() { Unit = SizeUnit.Binary, Style = UnitStyle.Short, Precision = 1 };

    /// <summary>Decimal units with short labels and 1 decimal place.</summary>
    public static SizeFormat Decimal() => new() { Unit = SizeUnit.Decimal, Style = UnitStyle.Short, Precision = 1 };

    /// <summary>Use short unit labels (KB, KiB).</summary>
    public SizeFormat Short() => this with { Style = UnitStyle.Short };

    /// <summary>Use long unit labels (kilobytes, kibibytes).</summary>
    public SizeFormat Long() => this with { Style = UnitStyle.Long };

    /// <summary>Set precision (decimal places) for non-byte units.</summary>
    public SizeFormat WithPrecision(int precision) => this with { Precision = precision };
}

/// <summary>Human-friendly file size formatting utilities.</summary>
public static class FileSize
{
    private static (double Base, string[] Units) GetUnits(SizeUnit unit, UnitStyle style) => (unit, style) switch
    {
        (SizeUnit.Binary, UnitStyle.Short) => (1024.0, BinaryUnitLabels.Short),
        (SizeUnit.Binary, UnitStyle.Long) => (1024.0, BinaryUnitLabels.Long),
        (SizeUnit.Decimal, UnitStyle.Short) => (1000.0, DecimalUnitLabels.Short),
        (SizeUnit.Decimal, UnitStyle.Long) => (1000.0, DecimalUnitLabels.Long),
        _ => (1024.0, BinaryUnitLabels.Short),
    };

    /// <summary>
    /// Format a size (bytes) into a human-readable string using the given format config.
    /// Bytes are always rendered without decimals (e.g., "999 B" or "999 bytes").
    /// </summary>
    public static string FormatSize(long size, SizeFormat format)
    {
        var (base_, units) = GetUnits(format.Unit, format.Style);
        var negative = size < 0;
        var absSize = negative ? (ulong)(-(long)(ulong)size) : (ulong)size; // handle i64::MIN safely

        if (absSize < (ulong)base_)
        {
            var prefix = negative ? "-" : "";
            return $"{prefix}{absSize} {units[0]}";
        }

        var value = (double)absSize;
        var unitIdx = 0;
        while (value >= base_ && unitIdx < units.Length - 1)
        {
            value /= base_;
            unitIdx++;
        }

        var prefix2 = negative ? "-" : "";
        return $"{prefix2}{value.ToString($"F{format.Precision}")} {units[unitIdx]}";
    }

    /// <summary>Format bytes using decimal (1000-based) units with 1 decimal place.</summary>
    public static string Decimal(long size)
    {
        var clamped = (ulong)size <= long.MaxValue ? size : long.MaxValue;
        if (size < 0) clamped = size; // preserve negative
        return FormatSize(clamped, SizeFormat.Decimal());
    }

    // DIVERGENCE: Upstream takes u64, .NET uses long for CLS compliance.
    // u64 overload provided for convenience.
    /// <inheritdoc cref="Decimal(long)"/>
    public static string Decimal(ulong size)
    {
        var clamped = size > (ulong)long.MaxValue ? long.MaxValue : (long)size;
        return FormatSize(clamped, SizeFormat.Decimal());
    }

    /// <summary>Format bytes using decimal (1000-based) units with custom precision.</summary>
    public static string DecimalWithPrecision(long size, int precision)
    {
        var clamped = (ulong)size <= long.MaxValue ? size : long.MaxValue;
        if (size < 0) clamped = size;
        return FormatSize(clamped, SizeFormat.Decimal() with { Precision = precision });
    }

    /// <inheritdoc cref="DecimalWithPrecision(long, int)"/>
    public static string DecimalWithPrecision(ulong size, int precision)
    {
        var clamped = size > (ulong)long.MaxValue ? long.MaxValue : (long)size;
        return FormatSize(clamped, SizeFormat.Decimal() with { Precision = precision });
    }

    /// <summary>Format bytes using binary (1024-based) units with 1 decimal place.</summary>
    public static string Binary(long size)
    {
        var clamped = (ulong)size <= long.MaxValue ? size : long.MaxValue;
        if (size < 0) clamped = size;
        return FormatSize(clamped, SizeFormat.Binary());
    }

    /// <inheritdoc cref="Binary(long)"/>
    public static string Binary(ulong size)
    {
        var clamped = size > (ulong)long.MaxValue ? long.MaxValue : (long)size;
        return FormatSize(clamped, SizeFormat.Binary());
    }

    /// <summary>Format bytes using binary (1024-based) units with custom precision.</summary>
    public static string BinaryWithPrecision(long size, int precision)
    {
        var clamped = (ulong)size <= long.MaxValue ? size : long.MaxValue;
        if (size < 0) clamped = size;
        return FormatSize(clamped, SizeFormat.Binary() with { Precision = precision });
    }

    /// <inheritdoc cref="BinaryWithPrecision(long, int)"/>
    public static string BinaryWithPrecision(ulong size, int precision)
    {
        var clamped = size > (ulong)long.MaxValue ? long.MaxValue : (long)size;
        return FormatSize(clamped, SizeFormat.Binary() with { Precision = precision });
    }
}
