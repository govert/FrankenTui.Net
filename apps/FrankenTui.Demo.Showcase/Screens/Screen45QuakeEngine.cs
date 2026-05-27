using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using System.Numerics;

namespace FrankenTui.Demo.Showcase;

// Vec3 math — port of quake.rs Vec3
internal readonly record struct Vec3F(float X, float Y, float Z)
{
    public static Vec3F operator +(Vec3F a, Vec3F b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3F operator -(Vec3F a, Vec3F b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3F operator *(Vec3F a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public float Dot(Vec3F other) => X * other.X + Y * other.Y + Z * other.Z;
    public Vec3F Cross(Vec3F other) => new(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
    public float Len() => MathF.Sqrt(X * X + Y * Y + Z * Z);
    public Vec3F Normalized() { var l = Len(); return l > 0 ? new(X / l, Y / l, Z / l) : this; }
    public static readonly Vec3F Zero = new(0, 0, 0);
}

internal static class QuakeMath
{
    public static float Cross2(float ax, float ay, float bx, float by) => ax * by - ay * bx;
    public static float PointSegmentDistSq(float px, float py, float x1, float y1, float x2, float y2)
    {
        float vx = x2 - x1, vy = y2 - y1;
        float lenSq = vx * vx + vy * vy;
        if (lenSq <= 1e-6f) { float dx = px - x1, dy = py - y1; return dx * dx + dy * dy; }
        float t = Math.Clamp(((px - x1) * vx + (py - y1) * vy) / lenSq, 0f, 1f);
        float projX = x1 + t * vx, projY = y1 + t * vy;
        float dx2 = px - projX, dy2 = py - projY;
        return dx2 * dx2 + dy2 * dy2;
    }
}

internal struct ClippedTriangle { public Vec3F V0, V1, V2, V3; public int Len; }

internal static class QuakeClipper
{
    public static ClippedTriangle ClipNear(Vec3F v0, Vec3F v1, Vec3F v2, float near)
    {
        Span<Vec3F> poly = [v0, v1, v2];
        var result = new ClippedTriangle();
        var prev = poly[2]; var prevInside = prev.Z >= near;
        for (int i = 0; i < 3; i++)
        {
            var curr = poly[i]; var currInside = curr.Z >= near;
            if (prevInside && currInside) { Push(ref result, curr); }
            else if (prevInside && !currInside)
            {
                float denom = curr.Z - prev.Z;
                if (Math.Abs(denom) > 1e-6f)
                {
                    float t = (near - prev.Z) / denom;
                    Push(ref result, new Vec3F(prev.X + (curr.X - prev.X) * t, prev.Y + (curr.Y - prev.Y) * t, near));
                }
            }
            else if (!prevInside && currInside)
            {
                float denom = curr.Z - prev.Z;
                if (Math.Abs(denom) > 1e-6f)
                {
                    float t = (near - prev.Z) / denom;
                    Push(ref result, new Vec3F(prev.X + (curr.X - prev.X) * t, prev.Y + (curr.Y - prev.Y) * t, near));
                }
                Push(ref result, curr);
            }
            prev = curr; prevInside = currInside;
        }
        return result;
    }
    static void Push(ref ClippedTriangle ct, Vec3F v) { if (ct.Len < 4) { ct.Len++; if (ct.Len == 1) ct.V0 = v; else if (ct.Len == 2) ct.V1 = v; else if (ct.Len == 3) ct.V2 = v; else ct.V3 = v; } }
}

internal struct WallSeg { public float X1, Y1, X2, Y2; }
internal struct FloorTri { public Vec3F V0, V1, V2; public float MinX, MaxX, MinY, MaxY, Area; }
internal struct RenderTri { public int I0, I1, I2; public Vec3F Normal; public float Diffuse; public PackedRgba Base; }

internal class QuakePlayer
{
    public Vec3F Pos, Vel;
    public float Yaw, Pitch;
    public bool Grounded;
    public QuakePlayer(Vec3F pos) { Pos = pos; Vel = Vec3F.Zero; Grounded = true; }
}

// Quake palette — browns, tans, greys
internal static class QuakePalette
{
    public static PackedRgba Stone(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var c1 = (47, 43, 35); var c2 = (83, 75, 60); var c3 = (131, 120, 95); var c4 = (110, 100, 90);
        (float r, float g, float b) =
            t < 0.33f ? (Lerp(c1, c2, t / 0.33f)) :
            t < 0.66f ? (Lerp(c2, c3, (t - 0.33f) / 0.33f)) :
                         (Lerp(c3, c4, (t - 0.66f) / 0.34f));
        return PackedRgba.Rgb((byte)r, (byte)g, (byte)b);
    }
    static (float, float, float) Lerp((int, int, int) a, (int, int, int) b, float t) =>
        (a.Item1 * (1 - t) + b.Item1 * t, a.Item2 * (1 - t) + b.Item2 * t, a.Item3 * (1 - t) + b.Item3 * t);
}

// Main Quake E1M1 state and renderer
internal class QuakeE1M1State
{
    const float EyeHeight = 0.18f, Gravity = -0.28f, JumpVel = 0.22f, CollisionRadius = 0.06f;
    const float MoveSpeed = 0.08f, StrafeSpeed = 0.07f, Friction = 0.85f, Accel = 0.02f;
    const float InvScale = 1f / 1024f;

    public QuakePlayer Player;
    public float FireFlash;
    Vec3F _boundsMin, _boundsMax;
    WallSeg[] _walls;
    FloorTri[] _floors;
    Vec3F[] _worldVerts, _camVerts;
    RenderTri[] _renderTris;
    float[] _depth;
    int _depthW, _depthH;
    float _moveFwd, _moveSide;

    public QuakeE1M1State()
    {
        (_boundsMin, _boundsMax) = ComputeBounds();
        (_walls, _floors) = BuildCollision();
        (_worldVerts, _renderTris) = BuildRenderMesh();
        _camVerts = new Vec3F[_worldVerts.Length];
        float cx = (_boundsMin.X + _boundsMax.X) * 0.5f, cy = (_boundsMin.Y + _boundsMax.Y) * 0.5f;
        Player = new QuakePlayer(new Vec3F(cx, cy, _boundsMin.Z + EyeHeight));
        _depth = [];
    }

    static (Vec3F, Vec3F) ComputeBounds()
    {
        var verts = QuakeMapData.Vertices.Span;
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        foreach (var (x, y, z) in verts)
        {
            float wx = x * InvScale, wy = y * InvScale, wz = z * InvScale;
            if (wx < minX) minX = wx; if (wy < minY) minY = wy; if (wz < minZ) minZ = wz;
            if (wx > maxX) maxX = wx; if (wy > maxY) maxY = wy; if (wz > maxZ) maxZ = wz;
        }
        return (new Vec3F(minX, minY, minZ), new Vec3F(maxX, maxY, maxZ));
    }

    (WallSeg[], FloorTri[]) BuildCollision()
    {
        var verts = QuakeMapData.Vertices.Span;
        var tris = QuakeMapData.Triangles.Span;
        var walls = new List<WallSeg>();
        var floors = new List<FloorTri>();

        foreach (var (i0, i1, i2) in tris)
        {
            var (v0x, v0y, v0z) = verts[i0]; var w0 = new Vec3F(v0x * InvScale, v0y * InvScale, v0z * InvScale);
            var (v1x, v1y, v1z) = verts[i1]; var w1 = new Vec3F(v1x * InvScale, v1y * InvScale, v1z * InvScale);
            var (v2x, v2y, v2z) = verts[i2]; var w2 = new Vec3F(v2x * InvScale, v2y * InvScale, v2z * InvScale);

            var n = (w1 - w0).Cross(w2 - w0);
            if (n.Len() <= 1e-6f) continue;

            if (Math.Abs(n.Z) < 0.35f) { PushEdge(w0, w1, walls); PushEdge(w1, w2, walls); PushEdge(w2, w0, walls); }
            if (n.Z > 0.35f) { var ft = MakeFloorTri(w0, w1, w2); if (ft.HasValue) floors.Add(ft.Value); }
        }
        return (walls.ToArray(), floors.ToArray());
    }

    static void PushEdge(Vec3F a, Vec3F b, List<WallSeg> w)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        if (dx * dx + dy * dy <= 1e-6f) return;
        w.Add(new WallSeg { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y });
    }

    static FloorTri? MakeFloorTri(Vec3F a, Vec3F b, Vec3F c)
    {
        float area = QuakeMath.Cross2(b.X - a.X, b.Y - a.Y, c.X - a.X, c.Y - a.Y);
        if (Math.Abs(area) <= 1e-6f) return null;
        return new FloorTri { V0 = a, V1 = b, V2 = c, MinX = Min3(a.X, b.X, c.X), MaxX = Max3(a.X, b.X, c.X), MinY = Min3(a.Y, b.Y, c.Y), MaxY = Max3(a.Y, b.Y, c.Y), Area = area };
    }

    (Vec3F[], RenderTri[]) BuildRenderMesh()
    {
        var verts = QuakeMapData.Vertices.Span;
        var tris = QuakeMapData.Triangles.Span;
        var wv = new Vec3F[verts.Length];
        for (int i = 0; i < verts.Length; i++) { var (x, y, z) = verts[i]; wv[i] = new Vec3F(x * InvScale, y * InvScale, z * InvScale); }
        float hSpan = Math.Max(_boundsMax.Z - _boundsMin.Z, 0.001f);
        var lightDir = new Vec3F(0.4f, -0.6f, 0.5f).Normalized();
        var rt = new RenderTri[tris.Length];
        for (int i = 0; i < tris.Length; i++)
        {
            var (i0, i1, i2) = tris[i];
            var n = (wv[i1] - wv[i0]).Cross(wv[i2] - wv[i0]).Normalized();
            float ht = Math.Clamp((wv[i0].Z - _boundsMin.Z) / hSpan, 0f, 1f);
            rt[i] = new RenderTri { I0 = i0, I1 = i1, I2 = i2, Normal = n, Diffuse = Math.Max(n.Dot(lightDir), 0f), Base = QuakePalette.Stone(ht) };
        }
        return (wv, rt);
    }

    static float Min3(float a, float b, float c) => Math.Min(a, Math.Min(b, c));
    static float Max3(float a, float b, float c) => Math.Max(a, Math.Max(b, c));

    float? GroundHeightAt(float x, float y)
    {
        float? best = null;
        foreach (ref var tri in _floors.AsSpan())
        {
            if (x < tri.MinX || x > tri.MaxX || y < tri.MinY || y > tri.MaxY) continue;
            float w0 = QuakeMath.Cross2(tri.V1.X - x, tri.V1.Y - y, tri.V2.X - x, tri.V2.Y - y) / tri.Area;
            float w1 = QuakeMath.Cross2(tri.V2.X - x, tri.V2.Y - y, tri.V0.X - x, tri.V0.Y - y) / tri.Area;
            float w2 = 1f - w0 - w1;
            if (w0 >= -1e-3f && w1 >= -1e-3f && w2 >= -1e-3f)
            {
                float z = w0 * tri.V0.Z + w1 * tri.V1.Z + w2 * tri.V2.Z;
                if (!best.HasValue || z > best.Value) best = z;
            }
        }
        return best;
    }

    float GroundEyeHeight(float x, float y) => (GroundHeightAt(x, y) ?? _boundsMin.Z) + EyeHeight;

    bool Collides(float x, float y)
    {
        float rSq = CollisionRadius * CollisionRadius;
        foreach (ref var s in _walls.AsSpan())
            if (QuakeMath.PointSegmentDistSq(x, y, s.X1, s.Y1, s.X2, s.Y2) < rSq) return true;
        return false;
    }

    public void Look(float yawD, float pitchD) { Player.Yaw = (Player.Yaw + yawD) % (2 * MathF.PI); Player.Pitch = Math.Clamp(Player.Pitch + pitchD, -1.2f, 1.2f); }
    public void SetMoveFwd(float v) => _moveFwd = Math.Clamp(v, -1f, 1f);
    public void SetMoveSide(float v) => _moveSide = Math.Clamp(v, -1f, 1f);
    public void Jump() { if (Player.Grounded) { Player.Vel = new Vec3F(Player.Vel.X, Player.Vel.Y, JumpVel); Player.Grounded = false; } }
    public void Fire() => FireFlash = 1f;

    void ApplyPhysics()
    {
        var (sy, cy) = MathF.SinCos(Player.Yaw);
        float fwdX = cy, fwdY = sy, sideX = -sy, sideY = cy;
        float tvx = fwdX * _moveFwd * MoveSpeed + sideX * _moveSide * StrafeSpeed;
        float tvy = fwdY * _moveFwd * MoveSpeed + sideY * _moveSide * StrafeSpeed;
        Player.Vel = new Vec3F(Player.Vel.X + (tvx - Player.Vel.X) * 0.2f, Player.Vel.Y + (tvy - Player.Vel.Y) * 0.2f, Player.Vel.Z);
        Player.Vel = new Vec3F(Player.Vel.X * Friction, Player.Vel.Y * Friction, Player.Vel.Z);

        float nx = Player.Pos.X + Player.Vel.X, ny = Player.Pos.Y;
        if (Collides(nx, Player.Pos.Y)) Player.Vel = new Vec3F(0, Player.Vel.Y, Player.Vel.Z);
        else Player.Pos = new Vec3F(nx, Player.Pos.Y, Player.Pos.Z);
        nx = Player.Pos.X; ny = Player.Pos.Y + Player.Vel.Y;
        if (Collides(Player.Pos.X, ny)) Player.Vel = new Vec3F(Player.Vel.X, 0, Player.Vel.Z);
        else Player.Pos = new Vec3F(Player.Pos.X, ny, Player.Pos.Z);

        float margin = Math.Max(CollisionRadius + 0.02f, 0.04f);
        Player.Pos = new Vec3F(Math.Clamp(Player.Pos.X, _boundsMin.X + margin, _boundsMax.X - margin), Math.Clamp(Player.Pos.Y, _boundsMin.Y + margin, _boundsMax.Y - margin), Player.Pos.Z);

        if (!Player.Grounded) { Player.Vel = new Vec3F(Player.Vel.X, Player.Vel.Y, Player.Vel.Z + Gravity * 0.05f); Player.Pos = new Vec3F(Player.Pos.X, Player.Pos.Y, Player.Pos.Z + Player.Vel.Z); }
        float ground = GroundEyeHeight(Player.Pos.X, Player.Pos.Y);
        if (Player.Pos.Z <= ground) { Player.Pos = new Vec3F(Player.Pos.X, Player.Pos.Y, ground); Player.Vel = new Vec3F(Player.Vel.X, Player.Vel.Y, 0); Player.Grounded = true; }
        else if (Player.Pos.Z > ground + 0.05f) Player.Grounded = false;
    }

    public void Update()
    {
        if (FireFlash > 0f) FireFlash = Math.Max(FireFlash - 0.1f, 0f);
        ApplyPhysics();
    }

    void EnsureDepth(int w, int h)
    {
        int len = w * h;
        if (_depth.Length < len) _depth = new float[len];
        _depthW = w; _depthH = h;
    }

    void ClearDepth() { Array.Fill(_depth, float.PositiveInfinity); }

    public void Render(CanvasPainter painter, int width, int height, FxQuality quality, long frame)
    {
        if (width == 0 || height == 0) return;
        int stride = quality == FxQuality.Off ? 0 : 1;
        if (stride == 0) return;

        EnsureDepth(width, height);
        ClearDepth();

        float w = width, h = height;
        var center = new Vec3F(w * 0.5f, h * 0.5f, 0);
        var eye = Player.Pos;
        var (sy, cy) = MathF.SinCos(Player.Yaw);
        var (sp, cp) = MathF.SinCos(Player.Pitch);
        var forward = new Vec3F(cy * cp, sy * cp, sp).Normalized();
        var right = new Vec3F(-sy, cy, 0).Normalized();
        var up = right.Cross(forward).Normalized();
        float projScale = Math.Min(w, h) * 0.9f, near = 0.04f, far = 8f;

        int triStep = quality switch { FxQuality.Full => 1, FxQuality.Reduced => 2, FxQuality.Minimal => 4, _ => 0 };
        int edgeStride = triStep == 1 ? 1 : triStep * 2;

        // Transform vertices to camera space
        for (int i = 0; i < _worldVerts.Length; i++)
        {
            var rel = _worldVerts[i] - eye;
            _camVerts[i] = new Vec3F(rel.Dot(right), rel.Dot(up), rel.Dot(forward));
        }

        float Edge(float ax, float ay, float bx, float by, float cx, float cy) => (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);

        for (int ti = 0; ti < _renderTris.Length; ti += triStep)
        {
            var tri = _renderTris[ti];
            var cam0 = _camVerts[tri.I0]; var cam1 = _camVerts[tri.I1]; var cam2 = _camVerts[tri.I2];
            if (cam0.Z < near && cam1.Z < near && cam2.Z < near) continue;

            var w0 = _worldVerts[tri.I0];
            float facing = tri.Normal.Dot((eye - w0).Normalized());
            if (facing <= 0.02f) continue;

            var clipped = QuakeClipper.ClipNear(cam0, cam1, cam2, near);
            if (clipped.Len < 3) continue;

            float rim = MathF.Pow(1f - Math.Clamp(facing, 0f, 1f), 3f) * 0.5f;
            float light = Math.Clamp(0.15f + tri.Diffuse * 0.8f + rim, 0f, 1.5f);

            void DrawTri(Vec3F va, Vec3F vb, Vec3F vc)
            {
                float sx0 = center.X + va.X / va.Z * projScale, sy0 = center.Y - va.Y / va.Z * projScale;
                float sx1 = center.X + vb.X / vb.Z * projScale, sy1 = center.Y - vb.Y / vb.Z * projScale;
                float sx2 = center.X + vc.X / vc.Z * projScale, sy2 = center.Y - vc.Y / vc.Z * projScale;

                int minX = Math.Max((int)MathF.Floor(Min3(sx0, sx1, sx2)), 0);
                int maxX = Math.Min((int)MathF.Ceiling(Max3(sx0, sx1, sx2)), width - 1);
                int minY = Math.Max((int)MathF.Floor(Min3(sy0, sy1, sy2)), 0);
                int maxY = Math.Min((int)MathF.Ceiling(Max3(sy0, sy1, sy2)), height - 1);
                if (minX > maxX || minY > maxY) return;

                float area = Edge(sx0, sy0, sx1, sy1, sx2, sy2);
                if (Math.Abs(area) < 1e-5f) return;
                float invArea = 1f / area;
                float e12dy = sy2 - sy1, e12dx = sx2 - sx1;
                float e20dy = sy0 - sy2, e20dx = sx0 - sx2;
                float e01dy = sy1 - sy0, e01dx = sx1 - sx0;

                for (int py = minY; py <= maxY; py += stride)
                {
                    float fy = py;
                    float rw0fy = (fy - sy1) * e12dx, rw1fy = (fy - sy2) * e20dx, rw2fy = (fy - sy0) * e01dx;
                    bool entered = false;
                    for (int px = minX; px <= maxX; px += stride)
                    {
                        float fx = px;
                        float w0e = (fx - sx1) * e12dy - rw0fy;
                        float w1e = (fx - sx2) * e20dy - rw1fy;
                        float w2e = (fx - sx0) * e01dy - rw2fy;
                        if (w0e * area < 0 || w1e * area < 0 || w2e * area < 0) { if (entered) break; continue; }
                        entered = true;

                        float b0 = w0e * invArea, b1 = w1e * invArea, b2 = w2e * invArea;
                        float z = b0 * va.Z + b1 * vb.Z + b2 * vc.Z;
                        int idx = py * width + px;
                        if (z >= _depth[idx]) continue;
                        _depth[idx] = z;

                        float fog = Math.Clamp((z - near) / (far - near), 0f, 1f);
                        float fade = MathF.Pow(1f - fog, 1.8f);
                        float grain = (((uint)px * 73856093u ^ (uint)py * 19349663u ^ (uint)frame) & 3) / 12f;
                        float brightness = Math.Clamp(light * fade + grain + (FireFlash > 0 ? FireFlash * 0.4f : 0), 0f, 1f);
                        var c = tri.Base;
                        painter.PointColored(px, py, PackedRgba.Rgb((byte)(c.R * brightness), (byte)(c.G * brightness), (byte)(c.B * brightness)));
                    }
                }
            }

            var clipArr = clipped.Len switch { 3 => new[] { clipped.V0, clipped.V1, clipped.V2 }, _ => new[] { clipped.V0, clipped.V1, clipped.V2, clipped.V3 } };
            if (clipped.Len == 3) DrawTri(clipArr[0], clipArr[1], clipArr[2]);
            else for (int j = 1; j < clipped.Len - 1; j++) DrawTri(clipArr[0], clipArr[j], clipArr[j + 1]);
        }

        // Crosshair
        int chx = width / 2, chy = height / 2;
        float flash = FireFlash;
        var cr = PackedRgba.Rgb((byte)Math.Min(200 + flash * 55, 255), (byte)Math.Max(240 - flash * 100, 0), (byte)Math.Max(240 - flash * 100, 0));
        painter.LineColored(chx - 4, chy, chx - 2, chy, cr);
        painter.LineColored(chx + 2, chy, chx + 4, chy, cr);
        painter.LineColored(chx, chy - 4, chx, chy - 2, cr);
        painter.LineColored(chx, chy + 2, chx, chy + 4, cr);
        painter.PointColored(chx, chy, PackedRgba.Rgb(255, 50, 50));
    }
}

/// <summary>Screen 45: Quake E1M1 — full 3D raycaster. Ported from quake.rs.</summary>
internal sealed class QuakeEasterEggScreen : IWidget
{
    readonly QuakeE1M1State _quake = new();
    readonly CanvasPainter _painter = new(0, 0, CanvasMode.Braille, colored: true);
    FxQuality _quality = FxQuality.Reduced;
    long _frame;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || area.Width < 20 || area.Height < 6) return;

        // Layout: header (1) + canvas (fill) + status (1)
        int canvasH = area.Height - 2;
        if (canvasH <= 0) return;

        var headerY = area.Y;
        var canvasY = (ushort)(area.Y + 1);
        var statusY = (ushort)(area.Y + 1 + canvasH);

        string header = $"WASD move · Arrows look · Space jump · F fire · V quality [{QualityLabel()}] · R reset";
        BufferPainter.WriteText(context.Buffer, area.X, headerY, header, Cell.FromChar(' '));

        var canvasArea = new Rect(area.X, canvasY, area.Width, (ushort)canvasH);
        _painter.EnsureForArea(canvasArea, CanvasMode.Braille);
        _painter.Clear();

        _quake.Update();
        _quake.Render(_painter, _painter.Width, _painter.Height, _quality, _frame);

        // Render braille to buffer
        _painter.Render(canvasArea, context.Buffer, Cell.FromChar(' '));

        var p = _quake.Player;
        string status = $"pos=({p.Pos.X:F2},{p.Pos.Y:F2},{p.Pos.Z:F2}) yaw={p.Yaw:F2} pitch={p.Pitch:F2} quality={QualityLabel()}";
        BufferPainter.WriteText(context.Buffer, area.X, statusY, status, Cell.FromChar(' '));

        _frame++;
    }

    string QualityLabel() => _quality switch { FxQuality.Full => "Full", FxQuality.Reduced => "Reduced", FxQuality.Minimal => "Minimal", _ => "Off" };
}

internal static class Screen45QuakePort
{
    public static IWidget Build(ShowcaseDemoState state) => new QuakeEasterEggScreen();
}
