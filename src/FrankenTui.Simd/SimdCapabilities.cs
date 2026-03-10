using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace FrankenTui.Simd;

public sealed record SimdCapabilities(
    bool VectorAcceleration,
    int ByteLaneCount,
    bool Sse2,
    bool Avx2,
    bool AdvSimd)
{
    public bool IsSupported => VectorAcceleration;

    public string Summary =>
        $"vector={(VectorAcceleration ? "on" : "off")} lanes={ByteLaneCount} sse2={(Sse2 ? "on" : "off")} avx2={(Avx2 ? "on" : "off")} advsimd={(AdvSimd ? "on" : "off")}";

    public static SimdCapabilities Detect() =>
        new(
            Vector.IsHardwareAccelerated,
            Vector<byte>.Count,
            System.Runtime.Intrinsics.X86.Sse2.IsSupported,
            System.Runtime.Intrinsics.X86.Avx2.IsSupported,
            System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported);
}
