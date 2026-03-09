namespace FrankenTui.Layout;

public static class ResponsiveLayout
{
    public static ResponsiveBreakpoint Select(ushort width, IReadOnlyList<ResponsiveBreakpoint> breakpoints)
    {
        ArgumentNullException.ThrowIfNull(breakpoints);

        if (breakpoints.Count == 0)
        {
            return new ResponsiveBreakpoint("default", 0);
        }

        return breakpoints
            .Where(point => width >= point.MinimumWidth)
            .DefaultIfEmpty(breakpoints[0])
            .MaxBy(static point => point.MinimumWidth);
    }
}
