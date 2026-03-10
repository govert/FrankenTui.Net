using FrankenTui.Render;
using FrankenTui.Text;

namespace FrankenTui.Simd;

public static class SimdAccelerators
{
    private static readonly SimdBufferDiffAccelerator BufferDiffAccelerator = new();
    private static readonly SimdTextWrapAccelerator TextWrapAccelerator = new();

    public static SimdCapabilities Capabilities { get; } = SimdCapabilities.Detect();

    public static bool IsEnabled =>
        ReferenceEquals(BufferDiff.Accelerator, BufferDiffAccelerator) &&
        ReferenceEquals(TextWrapper.Accelerator, TextWrapAccelerator);

    public static bool Enable()
    {
        if (!Capabilities.IsSupported)
        {
            return false;
        }

        BufferDiff.Accelerator = BufferDiffAccelerator;
        TextWrapper.Accelerator = TextWrapAccelerator;
        return true;
    }

    public static bool EnableIfSupported() => Enable();

    public static void Disable()
    {
        if (ReferenceEquals(BufferDiff.Accelerator, BufferDiffAccelerator))
        {
            BufferDiff.Accelerator = null;
        }

        if (ReferenceEquals(TextWrapper.Accelerator, TextWrapAccelerator))
        {
            TextWrapper.Accelerator = null;
        }
    }
}
