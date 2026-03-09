namespace FrankenTui.Core;

public static class TerminalEventCoalescer
{
    public static IReadOnlyList<TerminalEvent> Coalesce(IEnumerable<TerminalEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var result = new List<TerminalEvent>();
        foreach (var terminalEvent in events)
        {
            switch (terminalEvent)
            {
                case ResizeTerminalEvent resize:
                    var resizeIndex = result.FindLastIndex(static item => item is ResizeTerminalEvent);
                    if (resizeIndex >= 0)
                    {
                        result[resizeIndex] = resize;
                    }
                    else
                    {
                        result.Add(resize);
                    }

                    break;
                case MouseTerminalEvent mouse when mouse.Gesture.Kind == TerminalMouseKind.Move:
                    if (result.Count > 0 &&
                        result[^1] is MouseTerminalEvent previousMouse &&
                        previousMouse.Gesture.Kind == TerminalMouseKind.Move)
                    {
                        result[^1] = mouse;
                    }
                    else
                    {
                        result.Add(mouse);
                    }

                    break;
                default:
                    result.Add(terminalEvent);
                    break;
            }
        }

        return result;
    }
}
