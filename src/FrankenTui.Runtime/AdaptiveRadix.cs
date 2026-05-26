namespace FrankenTui.Runtime;

/// <summary>Adaptive radix tree for fast string-keyed lookups. Matches upstream adaptive_radix.</summary>
public sealed class AdaptiveRadixTree<T> where T : class
{
    private sealed class Node
    {
        public byte[] Key = [];
        public T? Value;
        public Node?[] Children = [];
    }

    private readonly Node _root = new();
    public int Count { get; private set; }

    public T? Get(ReadOnlySpan<byte> key)
    {
        var node = _root;
        var offset = 0;
        while (offset < key.Length)
        {
            if (node.Children.Length == 0) return null;
            var idx = key[offset] % node.Children.Length;
            var child = node.Children[idx];
            if (child is null) return null;
            node = child;
            offset++;
        }
        return node.Value;
    }

    public void Insert(ReadOnlySpan<byte> key, T value)
    {
        var node = _root;
        foreach (var b in key)
        {
            if (node.Children.Length == 0)
                node.Children = new Node[4];
            var idx = b % node.Children.Length;
            node.Children[idx] ??= new Node();
            node = node.Children[idx]!;
        }
        if (node.Value is null) Count++;
        node.Value = value;
    }
}
