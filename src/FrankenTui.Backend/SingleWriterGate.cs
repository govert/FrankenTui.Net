namespace FrankenTui.Backend;

public sealed class SingleWriterGate
{
    private int _held;

    public Lease Acquire()
    {
        if (Interlocked.CompareExchange(ref _held, 1, 0) != 0)
        {
            throw new InvalidOperationException("A terminal writer lease is already active.");
        }

        return new Lease(this);
    }

    private void Release() => Interlocked.Exchange(ref _held, 0);

    public readonly struct Lease : IDisposable
    {
        private readonly SingleWriterGate? _owner;

        internal Lease(SingleWriterGate owner)
        {
            _owner = owner;
        }

        public void Dispose() => _owner?.Release();
    }
}
