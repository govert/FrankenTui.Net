// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/cached.rs (610L)
// Cached widget wrapper with manual/cache-key invalidation.

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

public sealed class CachedWidgetState
{
    public Buffer? Cache { get; set; }
    public Rect? LastArea { get; set; }
    public bool Dirty { get; set; } = true;
    public ulong? LastKey { get; set; }
}

public interface ICacheKey<in W>
{
    ulong? Key(W widget);
}

public sealed class NoCacheKey<W> : ICacheKey<W>
{
    public ulong? Key(W _) => null;
}

public sealed class CachedWidget<W> : IStatefulWidget<CachedWidgetState> where W : IWidget
{
    W _inner; ICacheKey<W>? _key;

    public CachedWidget(W inner, ICacheKey<W>? key = null) { _inner = inner; _key = key; }

    public void Invalidate(CachedWidgetState state) => state.Dirty = true;

    public void Render(Rect area, Frame frame, CachedWidgetState state)
    {
        if (area.Width == 0 || area.Height == 0) return;

        ulong? currentKey = _key?.Key(_inner);
        bool hit = state.Cache != null && !state.Dirty
            && state.LastArea == area
            && state.LastKey == currentKey;

        if (!hit)
        {
            // Cache miss: re-render into a scratch buffer
            var scratch = new Buffer(area.Width, area.Height);
            state.Cache = scratch;
            state.LastArea = area;
            state.Dirty = false;
            state.LastKey = currentKey;
  
            // DIVERGENCE: Render into scratch buffer via temporary Frame
            var tmpPool = new GraphemePool();
            var tmpFrame = new Frame(area.Width, area.Height, tmpPool) { BufferOverride = scratch };
            _inner.Render(new Rect(0, 0, area.Width, area.Height), tmpFrame);
        }

        // Replay cached cells into actual frame
        if (state.Cache != null)
        {
            for (ushort y = 0; y < state.Cache.Height && (area.Y + y) < frame.Buffer.Height; y++)
                for (ushort x = 0; x < state.Cache.Width && (area.X + x) < frame.Buffer.Width; x++)
                {
                    var cell = state.Cache.Get(x, y);
                    if (cell.HasValue)
                        frame.Buffer.SetFast((ushort)(area.X + x), (ushort)(area.Y + y), cell.Value);
                }
        }
    }
}
