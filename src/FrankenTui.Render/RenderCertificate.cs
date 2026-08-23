// Upstream source: crates/ftui-render/src/render_certificate.rs
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Intentional divergences: none. Rust's owned Vec values are exposed through
// read-only, defensively copied .NET collections, and usize maps to Int32.

namespace FrankenTui.Render;

/// <summary>Facts the writer can prove about a frame before diffing.</summary>
public readonly record struct RenderCertificateInputs(
    bool PrevAvailable,
    bool DimsChanged,
    bool FullRedrawDue,
    int DirtyRowCount,
    ushort TotalRows);

/// <summary>The certified level of work elimination.</summary>
public enum RenderCertificateLevel
{
    FullRequired,
    SkipAll,
    NarrowToDirty
}

public static class RenderCertificateLevelExtensions
{
    /// <summary>Returns the stable lowercase tag used in evidence logs.</summary>
    public static string Label(this RenderCertificateLevel level) => level switch
    {
        RenderCertificateLevel.FullRequired => "full-required",
        RenderCertificateLevel.SkipAll => "skip-all",
        RenderCertificateLevel.NarrowToDirty => "narrow-to-dirty",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}

/// <summary>An explicit, explainable skip decision for one frame.</summary>
public sealed class RenderCertificate : IEquatable<RenderCertificate>
{
    private readonly IReadOnlyList<string> _causes;
    private readonly IReadOnlyList<ushort> _dirtyRows;

    public RenderCertificate(
        RenderCertificateLevel level,
        IEnumerable<string> causes,
        IEnumerable<ushort> dirtyRows,
        bool fellBack)
    {
        ArgumentNullException.ThrowIfNull(causes);
        ArgumentNullException.ThrowIfNull(dirtyRows);

        Level = level;
        _causes = Array.AsReadOnly(causes.ToArray());
        _dirtyRows = Array.AsReadOnly(dirtyRows.ToArray());
        FellBack = fellBack;
    }

    public RenderCertificateLevel Level { get; }

    public IReadOnlyList<string> Causes => _causes;

    public IReadOnlyList<ushort> DirtyRows => _dirtyRows;

    public bool FellBack { get; }

    /// <summary>Translates this certificate into the diff-stage hint.</summary>
    public DiffSkipHint ToHint() => Level switch
    {
        RenderCertificateLevel.FullRequired => DiffSkipHint.FullDiff,
        RenderCertificateLevel.SkipAll => DiffSkipHint.SkipDiff,
        RenderCertificateLevel.NarrowToDirty => DiffSkipHint.NarrowToRows(_dirtyRows),
        _ => throw new ArgumentOutOfRangeException(nameof(Level), Level, null)
    };

    /// <summary>Returns a compact JSON fragment with stable field order.</summary>
    public string ToEvidenceJson()
    {
        var causes = string.Join(",", _causes.Select(static cause => $"\"{cause}\""));
        var fellBack = FellBack ? "true" : "false";
        return $"{{\"level\":\"{Level.Label()}\",\"causes\":[{causes}],\"narrowed_rows\":{_dirtyRows.Count},\"fell_back\":{fellBack}}}";
    }

    /// <summary>Evaluates the conservative production decision tree.</summary>
    /// <remarks>
    /// <paramref name="dirtyRows"/> must contain the buffer's dirty-row indices
    /// in ascending order. It is consulted only when the decision narrows.
    /// </remarks>
    public static RenderCertificate EvaluateRenderCertificate(
        RenderCertificateInputs inputs,
        IEnumerable<ushort> dirtyRows)
    {
        ArgumentNullException.ThrowIfNull(dirtyRows);
        var witnessedRows = dirtyRows.ToArray();

        if (!inputs.PrevAvailable)
        {
            return FullRequired("no-previous-frame");
        }

        if (inputs.DimsChanged)
        {
            return FullRequired("viewport-changed");
        }

        if (inputs.FullRedrawDue)
        {
            return FullRequired("full-redraw-probe-due");
        }

        if (inputs.DirtyRowCount == 0)
        {
            return new RenderCertificate(
                RenderCertificateLevel.SkipAll,
                ["zero-dirty-rows"],
                [],
                fellBack: false);
        }

        if (witnessedRows.Length != inputs.DirtyRowCount
            || witnessedRows.Any(row => row >= inputs.TotalRows))
        {
            return FullRequired("dirty-row-witness-inconsistent");
        }

        return new RenderCertificate(
            RenderCertificateLevel.NarrowToDirty,
            ["dirty-rows-witnessed"],
            witnessedRows,
            fellBack: false);
    }

    public bool Equals(RenderCertificate? other) =>
        other is not null
        && Level == other.Level
        && FellBack == other.FellBack
        && _causes.SequenceEqual(other._causes)
        && _dirtyRows.SequenceEqual(other._dirtyRows);

    public override bool Equals(object? obj) => Equals(obj as RenderCertificate);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Level);
        hash.Add(FellBack);
        foreach (var cause in _causes)
        {
            hash.Add(cause, StringComparer.Ordinal);
        }

        foreach (var row in _dirtyRows)
        {
            hash.Add(row);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(RenderCertificate? left, RenderCertificate? right) =>
        Equals(left, right);

    public static bool operator !=(RenderCertificate? left, RenderCertificate? right) =>
        !Equals(left, right);

    private static RenderCertificate FullRequired(string cause) => new(
        RenderCertificateLevel.FullRequired,
        [cause],
        [],
        fellBack: true);
}
