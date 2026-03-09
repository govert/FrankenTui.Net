namespace FrankenTui.Core;

public static class TerminalCapabilityDetector
{
    public static TerminalCapabilities Detect(
        IReadOnlyDictionary<string, string?>? environment = null,
        CapabilityOverrideSet? overrides = null)
    {
        var detected = environment is null
            ? TerminalCapabilities.Detect()
            : TerminalCapabilities.Detect(environment);

        return overrides is null ? detected : overrides.Apply(detected);
    }
}
