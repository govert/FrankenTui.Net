// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/focus/spatial.rs (494L)
// Spatial navigation: arrow-key focus movement based on widget geometry.

using FrankenTui.Core;

namespace FrankenTui.Widgets.Focus;

public static class SpatialNav
{
    public static ulong? SpatialNavigate(FocusGraph graph, ulong origin, NavDirection dir)
    {
        // 1. Check explicit edge first.
        if (graph.Navigate(origin, dir) is { } target && graph.Get(target)?.IsFocusable == true)
            return target;

        // Only spatial directions
        if (dir is not (NavDirection.Up or NavDirection.Down or NavDirection.Left or NavDirection.Right))
            return null;

        var originNode = graph.Get(origin);
        if (originNode == null) return null;
        var oc = CenterI32(originNode.Bounds);

        (ulong Id, long Score)? best = null;

        foreach (var candidateId in graph.NodeIds)
        {
            if (candidateId == origin) continue;
            var candidate = graph.Get(candidateId);
            if (candidate == null || !candidate.IsFocusable) continue;
            var cc = CenterI32(candidate.Bounds);
            if (!InQuadrant(oc, cc, dir)) continue;
            long score = DistanceScore(oc, cc, dir);
            if (best == null || score < best.Value.Score || (score == best.Value.Score && candidateId < best.Value.Id))
                best = (candidateId, score);
        }
        return best?.Id;
    }

    public static void BuildSpatialEdges(FocusGraph graph)
    {
        var ids = graph.NodeIds.ToList();
        var dirs = new[] { NavDirection.Up, NavDirection.Down, NavDirection.Left, NavDirection.Right };
        var toAdd = new List<(ulong, NavDirection, ulong)>();
        foreach (var id in ids)
            foreach (var dir in dirs)
                if (graph.Navigate(id, dir) == null && SpatialNavigate(graph, id, dir) is { } target)
                    toAdd.Add((id, dir, target));
        foreach (var (from, dir, to) in toAdd) graph.Connect(from, dir, to);
    }

    static (int x, int y) CenterI32(Rect r) => (2 * r.X + r.Width, 2 * r.Y + r.Height);
    static bool InQuadrant((int x, int y) origin, (int x, int y) candidate, NavDirection dir) => dir switch
    {
        NavDirection.Up => candidate.y < origin.y,
        NavDirection.Down => candidate.y > origin.y,
        NavDirection.Left => candidate.x < origin.x,
        NavDirection.Right => candidate.x > origin.x,
        _ => false,
    };
    static long DistanceScore((int x, int y) o, (int x, int y) c, NavDirection dir)
    {
        long primary = dir switch
        {
            NavDirection.Up => o.y - c.y, NavDirection.Down => c.y - o.y,
            NavDirection.Left => o.x - c.x, NavDirection.Right => c.x - o.x,
            _ => long.MaxValue / 2,
        };
        long ortho = dir switch
        {
            NavDirection.Up or NavDirection.Down => Math.Abs(o.x - c.x),
            NavDirection.Left or NavDirection.Right => Math.Abs(o.y - c.y),
            _ => 0,
        };
        return 10 * primary + 3 * ortho;
    }
}
