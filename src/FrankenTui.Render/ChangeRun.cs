namespace FrankenTui.Render;

public readonly record struct ChangeRun(ushort Y, ushort X0, ushort X1)
{
    public int Length => X1 >= X0 ? X1 - X0 + 1 : 0;

    public bool IsEmpty => X1 < X0;
}
