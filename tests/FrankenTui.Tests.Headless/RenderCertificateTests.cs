// Upstream source: crates/ftui-render/src/render_certificate.rs (tests module)
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Intentional divergences: none.

using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class RenderCertificateTests
{
    [Fact]
    public void UnprovableConditionsForceFullWork()
    {
        var noPrevious = Inputs(dirty: 3, rows: 10) with { PrevAvailable = false };
        var certificate = RenderCertificate.EvaluateRenderCertificate(noPrevious, [1, 2, 3]);
        Assert.Equal(RenderCertificateLevel.FullRequired, certificate.Level);
        Assert.True(certificate.FellBack);
        Assert.Equal(["no-previous-frame"], certificate.Causes);

        var resized = Inputs(dirty: 3, rows: 10) with { DimsChanged = true };
        certificate = RenderCertificate.EvaluateRenderCertificate(resized, [1, 2, 3]);
        Assert.Equal(["viewport-changed"], certificate.Causes);

        var probe = Inputs(dirty: 3, rows: 10) with { FullRedrawDue = true };
        certificate = RenderCertificate.EvaluateRenderCertificate(probe, [1, 2, 3]);
        Assert.Equal(["full-redraw-probe-due"], certificate.Causes);
    }

    [Fact]
    public void ZeroDirtyRowsCertifyASkip()
    {
        var certificate = RenderCertificate.EvaluateRenderCertificate(Inputs(0, 10), []);

        Assert.Equal(RenderCertificateLevel.SkipAll, certificate.Level);
        Assert.False(certificate.FellBack);
        Assert.Equal(DiffSkipHintKind.SkipDiff, certificate.ToHint().Kind);
    }

    [Fact]
    public void DirtyRowsCertifyANarrowScan()
    {
        var certificate = RenderCertificate.EvaluateRenderCertificate(Inputs(2, 10), [3, 7]);

        Assert.Equal(RenderCertificateLevel.NarrowToDirty, certificate.Level);
        Assert.Equal(DiffSkipHintKind.NarrowToRows, certificate.ToHint().Kind);
        Assert.Equal([3, 7], certificate.ToHint().Rows);
    }

    [Fact]
    public void InconsistentWitnessFallsBackToFull()
    {
        var countMismatch = RenderCertificate.EvaluateRenderCertificate(Inputs(2, 10), [3]);
        Assert.Equal(RenderCertificateLevel.FullRequired, countMismatch.Level);
        Assert.True(countMismatch.FellBack);

        var outOfBounds = RenderCertificate.EvaluateRenderCertificate(Inputs(1, 10), [10]);
        Assert.Equal(RenderCertificateLevel.FullRequired, outOfBounds.Level);
        Assert.Equal(["dirty-row-witness-inconsistent"], outOfBounds.Causes);
    }

    [Fact]
    public void EvidenceJsonIsStableAndNamed()
    {
        var certificate = RenderCertificate.EvaluateRenderCertificate(Inputs(2, 10), [3, 7]);

        Assert.Equal(
            "{\"level\":\"narrow-to-dirty\",\"causes\":[\"dirty-rows-witnessed\"],\"narrowed_rows\":2,\"fell_back\":false}",
            certificate.ToEvidenceJson());
    }

    [Fact]
    public void CertifiedChangesEqualDirtyChangesAcrossGeneratedFrames()
    {
        const ushort width = 24;
        const ushort height = 8;
        ulong seed = 0x9E37_79B9_7F4A_7C15;

        ulong Next()
        {
            seed ^= seed << 13;
            seed ^= seed >> 7;
            seed ^= seed << 17;
            return seed;
        }

        for (var round = 0; round < 50; round++)
        {
            var oldBuffer = new RenderBuffer(width, height);
            var newBuffer = new RenderBuffer(width, height);
            newBuffer.ClearDirty();

            var mutations = (int)(Next() % 20);
            for (var mutation = 0; mutation < mutations; mutation++)
            {
                var x = (ushort)(Next() % width);
                var y = (ushort)(Next() % height);
                var character = (char)('A' + (Next() % 26));
                newBuffer.Set(x, y, Cell.FromChar(character));
            }

            var dirtyRows = Enumerable.Range(0, height)
                .Select(static row => (ushort)row)
                .Where(newBuffer.IsRowDirty)
                .ToArray();
            var certificate = RenderCertificate.EvaluateRenderCertificate(
                Inputs(newBuffer.DirtyRowCount, height),
                dirtyRows);
            Assert.False(certificate.FellBack, $"round {round}: unexpected fallback");

            var certified = BufferDiff.ComputeCertified(oldBuffer, newBuffer, certificate.ToHint());
            var truth = BufferDiff.ComputeDirty(oldBuffer, newBuffer);
            Assert.Equal(truth.Changes.ToArray(), certified.Changes.ToArray());
        }
    }

    [Fact]
    public void EqualityIsStructuralLikeRustDerivedEquality()
    {
        var left = RenderCertificate.EvaluateRenderCertificate(Inputs(2, 10), [3, 7]);
        var right = RenderCertificate.EvaluateRenderCertificate(Inputs(2, 10), [3, 7]);

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    private static RenderCertificateInputs Inputs(int dirty, ushort rows) => new(
        PrevAvailable: true,
        DimsChanged: false,
        FullRedrawDue: false,
        DirtyRowCount: dirty,
        TotalRows: rows);
}
