// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/schema_compat.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Suite-wide trace and evidence schema compatibility.
// Centralizes schema version constants and provides a compatibility checker.

namespace FrankenTui.Runtime;

// ── SchemaKind ────────────────────────────────────────────────────────────

public enum SchemaKind
{
    Evidence, RenderTrace, EventTrace, GoldenTrace, Telemetry, MigrationIr,
}

internal static class SchemaVersions
{
    public static string CurrentVersion(SchemaKind kind) => kind switch
    {
        SchemaKind.Evidence => "ftui-evidence-v2",
        SchemaKind.RenderTrace => "render-trace-v1",
        SchemaKind.EventTrace => "event-trace-v1",
        SchemaKind.GoldenTrace => "golden-trace-v1",
        SchemaKind.Telemetry => "1.0.0",
        SchemaKind.MigrationIr => "migration-ir-v1",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string VersionPrefix(SchemaKind kind) => kind switch
    {
        SchemaKind.Evidence => "ftui-evidence-v",
        SchemaKind.RenderTrace => "render-trace-v",
        SchemaKind.EventTrace => "event-trace-v",
        SchemaKind.GoldenTrace => "golden-trace-v",
        SchemaKind.Telemetry => "",
        SchemaKind.MigrationIr => "migration-ir-v",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string AsString(SchemaKind kind) => kind switch
    {
        SchemaKind.Evidence => "evidence",
        SchemaKind.RenderTrace => "render_trace",
        SchemaKind.EventTrace => "event_trace",
        SchemaKind.GoldenTrace => "golden_trace",
        SchemaKind.Telemetry => "telemetry",
        SchemaKind.MigrationIr => "migration_ir",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static readonly SchemaKind[] All = Enum.GetValues<SchemaKind>();
}

// ── Compatibility ─────────────────────────────────────────────────────────

public abstract record Compatibility
{
    public sealed record Exact : Compatibility;
    public sealed record Forward(uint ReaderVersion, uint WriterVersion) : Compatibility;
    public sealed record Backward(uint ReaderVersion, uint WriterVersion) : Compatibility;
    public sealed record Unknown(string WriterVersion) : Compatibility;

    public bool IsCompatible => this is Exact or Forward;

    public override string ToString() => this switch
    {
        Exact => "exact match",
        Forward f => $"forward compatible (reader=v{f.ReaderVersion}, writer=v{f.WriterVersion})",
        Backward b => $"incompatible: writer newer (reader=v{b.ReaderVersion}, writer=v{b.WriterVersion})",
        Unknown u => $"unknown version format: {u.WriterVersion}",
        _ => "",
    };
}

// ── CompatCheckResult ─────────────────────────────────────────────────────

public sealed record CompatCheckResult(SchemaKind Kind, string ReaderVersion, string WriterVersion, Compatibility Comp)
{
    public bool IsCompatible => Comp.IsCompatible;

    public override string ToString() =>
        $"{SchemaVersions.AsString(Kind)}: {Comp} (reader={ReaderVersion}, writer={WriterVersion})";
}

// ── Version Parsing Helpers ────────────────────────────────────────────────

internal static class SchemaVersionParser
{
    public static uint? ParsePrefixedVersion(string version, string prefix)
    {
        if (!version.StartsWith(prefix)) return null;
        var numStr = version[prefix.Length..];
        return uint.TryParse(numStr, out var n) ? n : null;
    }

    public static uint? ParseSemverMajor(string version)
    {
        var dot = version.IndexOf('.');
        var major = dot >= 0 ? version[..dot] : version;
        return uint.TryParse(major, out var n) ? n : null;
    }

    public static uint? ParseVersionNumber(SchemaKind kind, string version)
    {
        if (kind == SchemaKind.Telemetry)
            return ParseSemverMajor(version);
        return ParsePrefixedVersion(version, SchemaVersions.VersionPrefix(kind));
    }
}

// ── Core API ──────────────────────────────────────────────────────────────

/// <summary>
/// Schema compatibility checker for trace and evidence formats.
/// </summary>
public static class SchemaCompat
{
    /// <summary>Classify compatibility without side effects.</summary>
    public static CompatCheckResult ClassifySchemaCompat(SchemaKind kind, string writerVersion)
    {
        var readerVersion = SchemaVersions.CurrentVersion(kind);
        Compatibility comp;

        if (writerVersion == readerVersion)
        {
            comp = new Compatibility.Exact();
        }
        else
        {
            var rv = SchemaVersionParser.ParseVersionNumber(kind, readerVersion);
            var wv = SchemaVersionParser.ParseVersionNumber(kind, writerVersion);
            comp = (rv, wv) switch
            {
                (not null, not null) when rv > wv => new Compatibility.Forward(rv.Value, wv.Value),
                (not null, not null) when rv == wv => new Compatibility.Exact(),
                (not null, not null) => new Compatibility.Backward(rv.Value, wv.Value),
                _ => new Compatibility.Unknown(writerVersion),
            };
        }

        return new CompatCheckResult(kind, readerVersion, writerVersion, comp);
    }

    /// <summary>
    /// Check compatibility between reader and writer version.
    /// Increments metrics on incompatibility.
    /// </summary>
    public static CompatCheckResult CheckSchemaCompat(SchemaKind kind, string writerVersion)
    {
        var result = ClassifySchemaCompat(kind, writerVersion);
        if (!result.IsCompatible)
        {
            Metrics.Registry.Counter(BuiltinCounter.TraceCompatFailuresTotal).Inc();
        }
        return result;
    }

    public static CompatCheckResult CheckEvidenceCompat(string writerVersion) =>
        CheckSchemaCompat(SchemaKind.Evidence, writerVersion);
    public static CompatCheckResult CheckRenderTraceCompat(string writerVersion) =>
        CheckSchemaCompat(SchemaKind.RenderTrace, writerVersion);
    public static CompatCheckResult CheckEventTraceCompat(string writerVersion) =>
        CheckSchemaCompat(SchemaKind.EventTrace, writerVersion);
    public static CompatCheckResult CheckGoldenTraceCompat(string writerVersion) =>
        CheckSchemaCompat(SchemaKind.GoldenTrace, writerVersion);

    /// <summary>Run the full compatibility matrix and return all results.</summary>
    public static List<(MatrixEntry Entry, CompatCheckResult Result)> RunCompatibilityMatrix(MatrixEntry[] entries)
    {
        return entries.Select(e => (e, ClassifySchemaCompat(e.Kind, e.WriterVersion))).ToList();
    }

    /// <summary>Build the default compatibility matrix covering all schema kinds.</summary>
    public static List<MatrixEntry> DefaultCompatibilityMatrix()
    {
        var entries = new List<MatrixEntry>();
        foreach (var kind in SchemaVersions.All)
        {
            var current = SchemaVersions.CurrentVersion(kind);
            entries.Add(new(kind, current, true));
            entries.Add(new(kind, "not-a-version", false));

            if (kind == SchemaKind.Telemetry)
            {
                entries.Add(new(kind, "0.9.0", true));
                entries.Add(new(kind, "2.0.0", false));
            }
            else
            {
                var prefix = SchemaVersions.VersionPrefix(kind);
                var currentNum = SchemaVersionParser.ParseVersionNumber(kind, current);
                if (currentNum is > 0)
                {
                    entries.Add(new(kind, $"{prefix}{currentNum - 1}", true));
                }
                entries.Add(new(kind, $"{prefix}{currentNum + 1}", false));
            }
        }
        return entries;
    }
}

/// <summary>Entry in the compatibility matrix.</summary>
public sealed record MatrixEntry(SchemaKind Kind, string WriterVersion, bool ExpectedCompatible);
