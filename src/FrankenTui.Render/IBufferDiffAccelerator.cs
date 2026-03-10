namespace FrankenTui.Render;

public interface IBufferDiffAccelerator
{
    bool TryAppendRowChanges(
        ReadOnlySpan<Cell> oldRow,
        ReadOnlySpan<Cell> newRow,
        ushort row,
        List<CellPosition> changes);
}
