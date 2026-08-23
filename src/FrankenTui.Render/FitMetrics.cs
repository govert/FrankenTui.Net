// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/fit_metrics.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using System.Globalization;
using System.Numerics;

namespace FrankenTui.Render;

public record struct CellMetrics
{
    public const uint SubpixelScale = 256;

    public CellMetrics()
    {
        WidthSubpixels = 8 * SubpixelScale;
        HeightSubpixels = 16 * SubpixelScale;
    }

    public CellMetrics(uint widthSubpixels, uint heightSubpixels)
    {
        WidthSubpixels = widthSubpixels;
        HeightSubpixels = heightSubpixels;
    }

    public uint WidthSubpixels { get; set; }
    public uint HeightSubpixels { get; set; }

    public static CellMetrics Default => MonospaceDefault;
    public static readonly CellMetrics MonospaceDefault = new(8 * SubpixelScale, 16 * SubpixelScale);
    public static readonly CellMetrics Large = new(10 * SubpixelScale, 20 * SubpixelScale);

    public static CellMetrics? New(uint widthSubpixels, uint heightSubpixels) =>
        widthSubpixels == 0 || heightSubpixels == 0
            ? null
            : new CellMetrics(widthSubpixels, heightSubpixels);

    public static CellMetrics? FromPixels(double widthPixels, double heightPixels)
    {
        var width = FitMetrics.PixelsToSubpixels(widthPixels);
        var height = FitMetrics.PixelsToSubpixels(heightPixels);
        return width is { } validWidth && height is { } validHeight
            ? New(validWidth, validHeight)
            : null;
    }

    public readonly uint WidthPixels => WidthSubpixels / SubpixelScale;
    public readonly uint HeightPixels => HeightSubpixels / SubpixelScale;

    public override readonly string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{WidthPixels}x{HeightPixels}px ({WidthSubpixels / (double)SubpixelScale:F2}x{HeightSubpixels / (double)SubpixelScale:F2} sub-px)");
}

public record struct ContainerViewport
{
    public ContainerViewport(uint widthPixels, uint heightPixels, uint dprSubpixels, uint zoomSubpixels)
    {
        WidthPixels = widthPixels;
        HeightPixels = heightPixels;
        DprSubpixels = dprSubpixels;
        ZoomSubpixels = zoomSubpixels;
    }

    public uint WidthPixels { get; set; }
    public uint HeightPixels { get; set; }
    public uint DprSubpixels { get; set; }
    public uint ZoomSubpixels { get; set; }

    public static ContainerViewport? New(
        uint widthPixels,
        uint heightPixels,
        double devicePixelRatio,
        double zoom)
    {
        var dpr = FitMetrics.PixelsToSubpixels(devicePixelRatio);
        var zoomSubpixels = FitMetrics.PixelsToSubpixels(zoom);
        if (widthPixels == 0 || heightPixels == 0 || dpr is null or 0 || zoomSubpixels is null or 0)
        {
            return null;
        }

        return new ContainerViewport(widthPixels, heightPixels, dpr.Value, zoomSubpixels.Value);
    }

    public static ContainerViewport? Simple(uint widthPixels, uint heightPixels) =>
        New(widthPixels, heightPixels, 1.0, 1.0);

    public readonly uint EffectiveWidthSubpixels => EffectiveSubpixels(WidthPixels);
    public readonly uint EffectiveHeightSubpixels => EffectiveSubpixels(HeightPixels);

    public override readonly string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{WidthPixels}x{HeightPixels}px @{DprSubpixels / (double)CellMetrics.SubpixelScale:F2}x DPR, {ZoomSubpixels / (double)CellMetrics.SubpixelScale * 100:F0}% zoom");

    private readonly uint EffectiveSubpixels(uint physicalPixels)
    {
        const ulong scaleCubed =
            CellMetrics.SubpixelScale * CellMetrics.SubpixelScale * CellMetrics.SubpixelScale;
        var numerator = physicalPixels * scaleCubed;
        var denominator = (ulong)DprSubpixels * ZoomSubpixels;
        return denominator == 0 ? 0 : unchecked((uint)(numerator / denominator));
    }
}

public abstract record FitPolicy
{
    private sealed record Automatic : FitPolicy;

    public static FitPolicy FitToContainer { get; } = new Automatic();
    public static FitPolicy Default => FitToContainer;

    public sealed record Fixed(ushort Columns, ushort Rows) : FitPolicy;
    public sealed record FitWithMinimum(ushort MinimumColumns, ushort MinimumRows) : FitPolicy;

    internal bool IsAutomatic => this is Automatic;
}

public record struct FitResult(
    ushort Columns,
    ushort Rows,
    uint PaddingRightSubpixels,
    uint PaddingBottomSubpixels)
{
    public readonly bool IsValid => Columns > 0 && Rows > 0;

    public override readonly string ToString() => $"{Columns}x{Rows} cells";
}

public enum FitError
{
    ContainerTooSmall,
    DimensionOverflow,
}

public static class FitErrorExtensions
{
    public static string ToDisplayString(this FitError error) => error switch
    {
        FitError.ContainerTooSmall => "container too small to fit any cells",
        FitError.DimensionOverflow => "computed grid dimensions overflow u16",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
    };
}

/// <summary>Managed value representation of Rust Result&lt;FitResult, FitError&gt;.</summary>
public readonly record struct FitOutcome
{
    private FitOutcome(FitResult? result, FitError? error)
    {
        Result = result;
        Error = error;
    }

    public FitResult? Result { get; }
    public FitError? Error { get; }
    public bool IsSuccess => Result.HasValue;

    public static FitOutcome Success(FitResult result) => new(result, null);
    public static FitOutcome Failure(FitError error) => new(null, error);
}

public static class FitMetrics
{
    public static FitOutcome FitToContainer(
        ContainerViewport viewport,
        CellMetrics cell,
        FitPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy switch
        {
            FitPolicy.Fixed fixedSize => FitOutcome.Success(new FitResult(
                fixedSize.Columns, fixedSize.Rows, 0, 0)),
            FitPolicy.FitWithMinimum minimum => FitInternal(
                viewport,
                cell,
                Math.Max(minimum.MinimumColumns, (ushort)1),
                Math.Max(minimum.MinimumRows, (ushort)1)),
            _ when policy.IsAutomatic => FitInternal(viewport, cell, 1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null),
        };
    }

    internal static uint? PixelsToSubpixels(double pixels)
    {
        if (!double.IsFinite(pixels) || pixels < 0)
        {
            return null;
        }

        var value = Math.Round(
            pixels * CellMetrics.SubpixelScale,
            MidpointRounding.AwayFromZero);
        return value > uint.MaxValue ? null : (uint)value;
    }

    private static FitOutcome FitInternal(
        ContainerViewport viewport,
        CellMetrics cell,
        ushort minimumColumns,
        ushort minimumRows)
    {
        var effectiveWidth = viewport.EffectiveWidthSubpixels;
        var effectiveHeight = viewport.EffectiveHeightSubpixels;
        var rawColumns = effectiveWidth / cell.WidthSubpixels;
        var rawRows = effectiveHeight / cell.HeightSubpixels;
        var columns = Math.Max(rawColumns, minimumColumns);
        var rows = Math.Max(rawRows, minimumRows);

        if (columns == 0 || rows == 0)
        {
            return FitOutcome.Failure(FitError.ContainerTooSmall);
        }

        if (columns > ushort.MaxValue || rows > ushort.MaxValue)
        {
            return FitOutcome.Failure(FitError.DimensionOverflow);
        }

        var finalColumns = (ushort)columns;
        var finalRows = (ushort)rows;
        var usedWidth = unchecked((uint)finalColumns * cell.WidthSubpixels);
        var usedHeight = unchecked((uint)finalRows * cell.HeightSubpixels);
        return FitOutcome.Success(new FitResult(
            finalColumns,
            finalRows,
            SaturatingSubtract(effectiveWidth, usedWidth),
            SaturatingSubtract(effectiveHeight, usedHeight)));
    }

    private static uint SaturatingSubtract(uint left, uint right) => left >= right ? left - right : 0;
}

public readonly record struct MetricGeneration(ulong Value) : IComparable<MetricGeneration>
{
    public static MetricGeneration Zero => default;

    public MetricGeneration Next => Value == ulong.MaxValue ? this : new MetricGeneration(Value + 1);

    public int CompareTo(MetricGeneration other) => Value.CompareTo(other.Value);

    public override string ToString() => $"gen:{Value}";

    public static bool operator <(MetricGeneration left, MetricGeneration right) => left.Value < right.Value;
    public static bool operator >(MetricGeneration left, MetricGeneration right) => left.Value > right.Value;
    public static bool operator <=(MetricGeneration left, MetricGeneration right) => left.Value <= right.Value;
    public static bool operator >=(MetricGeneration left, MetricGeneration right) => left.Value >= right.Value;
}

public enum MetricInvalidation
{
    FontLoaded,
    DprChanged,
    ZoomChanged,
    ContainerResized,
    FontSizeChanged,
    FullReset,
}

public static class MetricInvalidationExtensions
{
    private static readonly MetricInvalidation[] CanonicalOrder =
    [
        MetricInvalidation.FullReset,
        MetricInvalidation.FontSizeChanged,
        MetricInvalidation.DprChanged,
        MetricInvalidation.ZoomChanged,
        MetricInvalidation.FontLoaded,
        MetricInvalidation.ContainerResized,
    ];

    internal static byte Bit(this MetricInvalidation invalidation) => invalidation switch
    {
        MetricInvalidation.FontLoaded => 1 << 0,
        MetricInvalidation.DprChanged => 1 << 1,
        MetricInvalidation.ZoomChanged => 1 << 2,
        MetricInvalidation.ContainerResized => 1 << 3,
        MetricInvalidation.FontSizeChanged => 1 << 4,
        MetricInvalidation.FullReset => 1 << 5,
        _ => throw new ArgumentOutOfRangeException(nameof(invalidation), invalidation, null),
    };

    public static IReadOnlyList<MetricInvalidation> OrderedPendingFromMask(byte mask)
    {
        var pending = new List<MetricInvalidation>(CanonicalOrder.Length);
        foreach (var reason in CanonicalOrder)
        {
            if ((mask & reason.Bit()) != 0)
            {
                pending.Add(reason);
            }
        }

        return pending;
    }

    public static bool RequiresRasterization(this MetricInvalidation invalidation) => invalidation is
        MetricInvalidation.FontLoaded or
        MetricInvalidation.DprChanged or
        MetricInvalidation.FontSizeChanged or
        MetricInvalidation.FullReset;

    public static bool RequiresRefit(this MetricInvalidation invalidation) => true;

    public static string ToDisplayString(this MetricInvalidation invalidation) => invalidation switch
    {
        MetricInvalidation.FontLoaded => "font_loaded",
        MetricInvalidation.DprChanged => "dpr_changed",
        MetricInvalidation.ZoomChanged => "zoom_changed",
        MetricInvalidation.ContainerResized => "container_resized",
        MetricInvalidation.FontSizeChanged => "font_size_changed",
        MetricInvalidation.FullReset => "full_reset",
        _ => throw new ArgumentOutOfRangeException(nameof(invalidation), invalidation, null),
    };
}

public sealed class MetricLifecycle
{
    private CellMetrics _cellMetrics;
    private ContainerViewport? _viewport;
    private FitPolicy _policy;
    private MetricGeneration _generation;
    private bool _pendingRefit;
    private MetricInvalidation? _lastInvalidation;
    private byte _pendingInvalidationMask;
    private FitResult? _lastFit;
    private ulong _totalInvalidations;
    private ulong _totalRefits;

    public MetricLifecycle(CellMetrics cellMetrics, FitPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _cellMetrics = cellMetrics;
        _policy = policy;
    }

    public CellMetrics CellMetrics => _cellMetrics;
    public MetricGeneration Generation => _generation;
    public bool IsPending => _pendingRefit;
    public MetricInvalidation? LastInvalidation => _lastInvalidation;
    public IReadOnlyList<MetricInvalidation> PendingInvalidations =>
        MetricInvalidationExtensions.OrderedPendingFromMask(_pendingInvalidationMask);
    public FitResult? LastFit => _lastFit;
    public ulong TotalInvalidations => _totalInvalidations;
    public ulong TotalRefits => _totalRefits;

    public void Invalidate(MetricInvalidation reason, CellMetrics? newMetrics = null)
    {
        _generation = _generation.Next;
        _pendingRefit = true;
        _lastInvalidation = reason;
        _pendingInvalidationMask |= reason.Bit();
        _totalInvalidations = unchecked(_totalInvalidations + 1);
        if (newMetrics is { } metrics)
        {
            _cellMetrics = metrics;
        }
    }

    public void SetViewport(ContainerViewport viewport)
    {
        var changed = _viewport is null || _viewport.Value != viewport;
        if (changed)
        {
            var primaryReason = MetricInvalidation.ContainerResized;
            if (_viewport is { } previous)
            {
                if (previous.DprSubpixels != viewport.DprSubpixels)
                {
                    _pendingInvalidationMask |= MetricInvalidation.DprChanged.Bit();
                    primaryReason = MetricInvalidation.DprChanged;
                }

                if (previous.ZoomSubpixels != viewport.ZoomSubpixels)
                {
                    _pendingInvalidationMask |= MetricInvalidation.ZoomChanged.Bit();
                    if (primaryReason == MetricInvalidation.ContainerResized)
                    {
                        primaryReason = MetricInvalidation.ZoomChanged;
                    }
                }

                if (previous.WidthPixels != viewport.WidthPixels ||
                    previous.HeightPixels != viewport.HeightPixels)
                {
                    _pendingInvalidationMask |= MetricInvalidation.ContainerResized.Bit();
                }
            }
            else
            {
                _pendingInvalidationMask |= MetricInvalidation.ContainerResized.Bit();
            }

            _generation = _generation.Next;
            _pendingRefit = true;
            _lastInvalidation = primaryReason;
            _totalInvalidations = unchecked(_totalInvalidations + 1);
        }

        _viewport = viewport;
    }

    public void SetPolicy(FitPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (_policy != policy)
        {
            _policy = policy;
            _pendingRefit = true;
        }
    }

    public FitResult? Refit()
    {
        if (!_pendingRefit)
        {
            return null;
        }

        _pendingRefit = false;
        _pendingInvalidationMask = 0;
        _totalRefits = unchecked(_totalRefits + 1);
        if (_viewport is not { } viewport)
        {
            return null;
        }

        var outcome = FitMetrics.FitToContainer(viewport, _cellMetrics, _policy);
        if (outcome.Result is not { } result)
        {
            return null;
        }

        var changed = _lastFit is not { } previous ||
                      previous.Columns != result.Columns ||
                      previous.Rows != result.Rows;
        _lastFit = result;
        return changed ? result : null;
    }

    public MetricSnapshot Snapshot => new(
        _generation.Value,
        _pendingRefit,
        _cellMetrics.WidthSubpixels,
        _cellMetrics.HeightSubpixels,
        _viewport?.WidthPixels ?? 0,
        _viewport?.HeightPixels ?? 0,
        _viewport?.DprSubpixels ?? 0,
        _viewport?.ZoomSubpixels ?? 0,
        _lastFit?.Columns ?? 0,
        _lastFit?.Rows ?? 0,
        _pendingInvalidationMask,
        (byte)BitOperations.PopCount(_pendingInvalidationMask),
        _totalInvalidations,
        _totalRefits);
}

public record struct MetricSnapshot(
    ulong Generation,
    bool PendingRefit,
    uint CellWidthSubpixels,
    uint CellHeightSubpixels,
    uint ViewportWidthPixels,
    uint ViewportHeightPixels,
    uint DprSubpixels,
    uint ZoomSubpixels,
    ushort FitColumns,
    ushort FitRows,
    byte PendingInvalidationMask,
    byte PendingInvalidationCount,
    ulong TotalInvalidations,
    ulong TotalRefits);
