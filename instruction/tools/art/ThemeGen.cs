// Pixel-art isometric theme tile generator (C# 5 / .NET Framework, System.Drawing).
// Tiles are drawn on a 42x42 grid and scaled x4 into 169x169 so the cube matches theme1.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public delegate Color TopFn(int x, int y, double u, double v, Random r);
public delegate Color SideFn(int x, int y, bool left, double t, Random r);

public class Pal
{
    public Color L, M, D;
    public Pal(Color m) { M = m; L = G.Mix(m, Color.White, 0.35); D = G.Mul(m, 0.66); }
    public Pal(string hex) : this(G.H(hex)) { }
    public Color Pick(double light) { return light > 0.4 ? L : (light > -0.25 ? M : D); }
}

// 2D grid whose y index can start below 0 (decorations draw above the block)
public class Grid<T>
{
    readonly T[,] a; readonly int top;
    public Grid(int w, int top, int bottom) { this.top = top; a = new T[w, bottom - top]; }
    public T this[int x, int y] { get { return a[x, y - top]; } set { a[x, y - top] = value; } }
}

public class Px
{
    public const int N = 42;
    /// <summary>Top row of newly created canvases (negative = extra space above the block)</summary>
    public static int YMin = 0;
    public readonly int Top;
    public Grid<Color> c;
    public Grid<bool> m;
    public Random r;
    public Px(int seed) { r = new Random(seed); Top = YMin; c = new Grid<Color>(N, Top, N); m = new Grid<bool>(N, Top, N); }
    public bool In(int x, int y) { return x >= 0 && y >= Top && x < N && y < N; }
    public void Set(int x, int y, Color col) { if (In(x, y)) c[x, y] = col; }
    public void Obj(int x, int y, Color col) { if (In(x, y)) { c[x, y] = col; m[x, y] = true; } }
}

public static class G
{
    public const double CX = 21, HW = 20, TOPC = 11.5, HH = 10.5;

    public static int Cl(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
    public static Color C(int r, int g, int b) { return Color.FromArgb(255, Cl(r), Cl(g), Cl(b)); }
    public static Color H(string h) { return C(Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    public static Color Sh(Color c, int d) { return C(c.R + d, c.G + d, c.B + d); }
    public static Color Mul(Color c, double k) { return C((int)(c.R * k), (int)(c.G * k), (int)(c.B * k)); }
    public static Color Mix(Color a, Color b, double t) { return C((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
    public static double Frac(double v) { return v - Math.Floor(v); }

    public static bool InTop(double px, double py, double off) { return Math.Abs(px - CX) / HW + Math.Abs(py - (TOPC + off)) / HH <= 1.0; }
    public static bool InSil(double px, double py, double off)
    {
        double dx = Math.Abs(px - CX); if (dx > HW) return false;
        double top = (TOPC + off) - HH * (1 - dx / HW), bot = 31.5 + (1 - dx / HW) * 10;
        return py >= top && py <= bot;
    }

    // ---------- base cube ----------
    public static void Cube(Px p, TopFn top, SideFn side, double off, Color outline)
    {
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double px = x + 0.5, py = y + 0.5;
                if (!InSil(px, py, off)) continue;
                bool edge = !(InSil(px - 1, py, off) && InSil(px + 1, py, off) && InSil(px, py - 1, off) && InSil(px, py + 1, off));
                if (edge) { p.Set(x, y, outline); continue; }
                double dxn = (px - CX) / HW;
                if (InTop(px, py, off))
                {
                    double dyn = (py - (TOPC + off)) / HH;
                    p.Set(x, y, top(x, y, dxn + dyn, dyn - dxn, p.r));
                    continue;
                }
                double topLower = (TOPC + off) + HH * (1 - Math.Abs(dxn)), bot = 31.5 + 10 * (1 - Math.Abs(dxn));
                double t = (py - topLower) / Math.Max(1, bot - topLower);
                Color col = side(x, y, px < CX, t, p.r);
                if (Math.Abs(px - CX) < 0.6) col = Mul(col, 0.82);
                p.Set(x, y, col);
            }
    }

    public static TopFn Noise(string hex, int amp, params object[] spots)
    {
        Color b = H(hex);
        return delegate (int x, int y, double u, double v, Random r)
        {
            double roll = r.NextDouble(), acc = 0;
            for (int i = 0; i + 1 < spots.Length; i += 2)
            {
                acc += (double)spots[i + 1];
                if (roll < acc) return H((string)spots[i]);
            }
            return Sh(b, r.Next(-amp, amp + 1));
        };
    }

    public static SideFn Side(string leftHex, string rightHex, string rimHex, double rimT)
    {
        Color l = H(leftHex), rr = H(rightHex);
        Color rim = rimHex == null ? Color.Empty : H(rimHex);
        return delegate (int x, int y, bool left, double t, Random r)
        {
            if (rimHex != null && t < rimT) return Sh(left ? Mul(rim, 0.85) : rim, r.Next(-6, 7));
            return Sh(left ? l : rr, r.Next(-7, 8));
        };
    }

    public static SideFn Banded(string leftHex, string rightHex, string rimHex, double rimT, string bandHex, double b0, double b1)
    {
        SideFn baseFn = Side(leftHex, rightHex, rimHex, rimT);
        Color band = H(bandHex);
        return delegate (int x, int y, bool left, double t, Random r)
        {
            if (t >= b0 && t <= b1) return Sh(left ? Mul(band, 0.86) : band, r.Next(-4, 5));
            return baseFn(x, y, left, t, r);
        };
    }

    // ---------- top decorations ----------
    public static IEnumerable<int[]> TopSpots(Px p, double off, int count, int minGap, int seed)
    {
        Random r = new Random(seed);
        List<int[]> placed = new List<int[]>();
        for (int i = 0; i < 300 && placed.Count < count; i++)
        {
            int fx = r.Next(4, 38), fy = r.Next(2, 24);
            if (!(InTop(fx - 0.5, fy + 0.5, off) && InTop(fx + 1.5, fy + 0.5, off) && InTop(fx + 0.5, fy - 0.5, off) && InTop(fx + 0.5, fy + 1.5, off))) continue;
            bool near = false;
            foreach (int[] q in placed) if (Math.Abs(q[0] - fx) + Math.Abs(q[1] - fy) < minGap) near = true;
            if (near) continue;
            placed.Add(new int[] { fx, fy });
        }
        return placed;
    }

    public static void Flowers(Px p, double off, int count, Color center, params Color[] petals)
    {
        int i = 0;
        foreach (int[] s in TopSpots(p, off, count, 5, 5))
        {
            Color pc = petals[i++ % petals.Length];
            p.Set(s[0] + 1, s[1], pc); p.Set(s[0] - 1, s[1], pc); p.Set(s[0], s[1] + 1, pc); p.Set(s[0], s[1] - 1, pc);
            p.Set(s[0], s[1], center);
        }
    }

    public static void Dots(Px p, double off, int count, int seed, params Color[] cols)
    {
        int i = 0;
        foreach (int[] s in TopSpots(p, off, count, 3, seed)) p.Set(s[0], s[1], cols[i++ % cols.Length]);
    }

    public static void Dashes(Px p, double off, int count, int seed, Color col, int len)
    {
        foreach (int[] s in TopSpots(p, off, count, 6, seed))
            for (int k = 0; k < len; k++) if (InTop(s[0] + k + 0.5, s[1] + 0.5, off)) p.Set(s[0] + k, s[1], col);
    }

    // ---------- object primitives ----------
    public static void Shadow(Px p, double cx, double cy, double rx, double ry)
    {
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                if (dx * dx + dy * dy > 1 || !InTop(x + 0.5, y + 0.5, 0)) continue;
                // 透明なデコ画像では半透明の黒、ブロックの上では暗くする
                p.c[x, y] = p.c[x, y].A > 0 ? Mul(p.c[x, y], 0.74) : Color.FromArgb(70, 0, 0, 0);
            }
    }

    public static void Ellipse(Px p, double cx, double cy, double rx, double ry, Pal pal, double maxY)
    {
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                if (dx * dx + dy * dy > 1 || y + 0.5 > maxY) continue;
                p.Obj(x, y, Sh(pal.Pick(-dx * 0.6 - dy * 0.8), ((x * 7 + y * 13) % 5) - 2));
            }
    }
    public static void Ellipse(Px p, double cx, double cy, double rx, double ry, Pal pal) { Ellipse(p, cx, cy, rx, ry, pal, 99); }

    public static void Flat(Px p, double cx, double cy, double rx, double ry, Color col)
    {
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                if (dx * dx + dy * dy <= 1) p.Obj(x, y, col);
            }
    }

    // Small isometric box. by = y of the bottom diamond center, hw = half width, h = height.
    public static void Box(Px p, double cx, double by, double hw, double h, Color top, Color left, Color right)
    {
        double hh = hw / 2, ty = by - h;
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double px = x + 0.5, py = y + 0.5, dx = Math.Abs(px - cx);
                if (dx > hw) continue;
                double k = 1 - dx / hw;
                if (Math.Abs(px - cx) / hw + Math.Abs(py - ty) / hh <= 1) { p.Obj(x, y, Sh(top, ((x + y) % 3) - 1)); continue; }
                if (py >= ty - hh * k && py <= by + hh * k) p.Obj(x, y, Sh(px < cx ? left : right, ((x * 3 + y) % 3) - 1));
            }
    }
    public static void Box(Px p, double cx, double by, double hw, double h, Pal pal) { Box(p, cx, by, hw, h, pal.L, pal.M, pal.D); }

    // Vertical cylinder; topCol drawn as the top ellipse.
    public static void Cyl(Px p, double cx, double yTop, double yBot, double rx, double ry, Pal body, Color topCol)
    {
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double px = x + 0.5, py = y + 0.5, dx = (px - cx) / rx;
                if (Math.Abs(dx) > 1) continue;
                double e = ry * Math.Sqrt(Math.Max(0.0, 1.0 - dx * dx));
                double tdy = (py - yTop) / ry;
                if (dx * dx + tdy * tdy <= 1) { p.Obj(x, y, topCol); continue; }
                if (py >= yTop && py <= yBot + e) p.Obj(x, y, dx < -0.35 ? body.L : (dx < 0.35 ? body.M : body.D));
            }
    }

    public static void Tri(Px p, double cx, double yTop, double yBase, double hw, Pal pal)
    {
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double py = y + 0.5, px = x + 0.5;
                if (py < yTop || py > yBase) continue;
                double half = hw * (py - yTop) / Math.Max(0.5, yBase - yTop);
                double dx = px - cx;
                if (Math.Abs(dx) > half + 0.3) continue;
                p.Obj(x, y, dx < -half * 0.3 ? pal.L : (dx < half * 0.35 ? pal.M : pal.D));
            }
    }

    public static void Capsule(Px p, double ax, double ay, double bx, double by, double rad, Pal pal)
    {
        double vx = bx - ax, vy = by - ay, len2 = vx * vx + vy * vy, len = Math.Sqrt(len2);
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                double px = x + 0.5, py = y + 0.5;
                double t = Math.Max(0.0, Math.Min(1.0, ((px - ax) * vx + (py - ay) * vy) / len2));
                double qx = ax + t * vx, qy = ay + t * vy;
                double ddx = px - qx, ddy = py - qy;
                if (ddx * ddx + ddy * ddy > rad * rad) continue;
                double side = (ddx * (-vy) + ddy * vx) / len;
                p.Obj(x, y, side < -rad * 0.35 ? pal.L : (side < rad * 0.35 ? pal.M : pal.D));
            }
    }

    public static void Rect(Px p, int x0, int y0, int x1, int y1, Color col)
    {
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) p.Obj(x, y, col);
    }

    public static void Outline(Px p, Color col)
    {
        var add = new Grid<bool>(Px.N, p.Top, Px.N);
        for (int y = p.Top; y < Px.N; y++)
            for (int x = 0; x < Px.N; x++)
            {
                if (p.m[x, y]) continue;
                if ((p.In(x + 1, y) && p.m[x + 1, y]) || (p.In(x - 1, y) && p.m[x - 1, y]) || (p.In(x, y + 1) && p.m[x, y + 1]) || (p.In(x, y - 1) && p.m[x, y - 1])) add[x, y] = true;
            }
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++) if (add[x, y]) p.c[x, y] = col;
    }

    public static void Save(Px p, string path)
    {
        using (Bitmap big = new Bitmap(169, 169 - p.Top * 4, PixelFormat.Format32bppArgb))
        {
            for (int y = p.Top; y < Px.N; y++)
                for (int x = 0; x < Px.N; x++)
                {
                    Color col = p.c[x, y];
                    if (col.A == 0) continue;
                    for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) big.SetPixel(x * 4 + i, (y - p.Top) * 4 + j, col);
                }
            big.Save(path, ImageFormat.Png);
        }
    }
}

// ---------- objects (drawn on the top face, center ~(21, 11.5)) ----------
public static class Obj
{
    static Color O(Pal pal) { return G.Mul(pal.D, 0.5); }

    public static void Rock(Px p, Pal pal)
    {
        G.Shadow(p, 23, 13, 10, 4.5);
        G.Ellipse(p, 21, 8.5, 9.5, 7, pal, 12.5);
        int[][] crack = { new[] { 19, 6 }, new[] { 20, 7 }, new[] { 20, 8 }, new[] { 21, 9 } };
        foreach (int[] c in crack) p.Set(c[0], c[1], pal.D);
        p.Set(16, 5, G.Mix(pal.L, Color.White, 0.5)); p.Set(17, 4, G.Mix(pal.L, Color.White, 0.5));
        G.Outline(p, O(pal));
    }

    public static void CapRock(Px p, Pal pal, Pal cap)
    {
        G.Shadow(p, 23, 13, 10, 4.5);
        G.Ellipse(p, 21, 8.5, 9.5, 7, pal, 12.5);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
                if (p.m[x, y] && y <= 5 + ((x * 3) % 2)) p.c[x, y] = cap.Pick(-(x - 21) * 0.1 - (y - 3) * 0.3);
        G.Outline(p, O(pal));
    }

    public static void Log(Px p, Pal bark, Color cut)
    {
        G.Shadow(p, 22, 13, 11, 4);
        G.Capsule(p, 12, 14, 29.5, 5.5, 3.6, bark);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - 12) / 2.6, dy = (y + 0.5 - 14) / 3.6, d2 = dx * dx + dy * dy;
                if (d2 <= 1) p.Obj(x, y, d2 < 0.12 ? bark.M : (d2 > 0.45 && d2 < 0.62 ? G.Mul(cut, 0.8) : cut));
            }
        G.Outline(p, O(bark));
    }

    public static void Bush(Px p, Pal leaf, params Color[] dots)
    {
        G.Shadow(p, 22, 13.5, 11, 4);
        G.Ellipse(p, 15, 11, 5.2, 5.2, leaf); G.Ellipse(p, 27, 11, 5.2, 5.2, leaf);
        G.Ellipse(p, 21, 12, 5.2, 5.2, leaf); G.Ellipse(p, 21, 6.5, 6.2, 6.2, leaf);
        int[][] spots = { new[] { 17, 9 }, new[] { 25, 13 }, new[] { 22, 4 }, new[] { 19, 13 }, new[] { 27, 9 }, new[] { 14, 12 } };
        for (int i = 0; i < spots.Length && dots.Length > 0; i++) { p.Set(spots[i][0], spots[i][1], dots[i % dots.Length]); p.Set(spots[i][0] - 1, spots[i][1] - 1, G.Mix(dots[i % dots.Length], Color.White, 0.5)); }
        G.Outline(p, O(leaf));
    }

    public static void Stump(Px p, Pal bark, Color wood)
    {
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Cyl(p, 21, 6, 12, 6.5, 3.2, bark, wood);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - 21) / 6.5, dy = (y + 0.5 - 6) / 3.2, d2 = dx * dx + dy * dy;
                if (d2 <= 1 && ((d2 > 0.35 && d2 < 0.55) || d2 < 0.08)) p.c[x, y] = G.Mul(wood, 0.8);
            }
        int[][] roots = { new[] { 13, 14 }, new[] { 14, 14 }, new[] { 28, 14 }, new[] { 29, 15 }, new[] { 21, 16 } };
        foreach (int[] rt in roots) p.Obj(rt[0], rt[1], bark.D);
        G.Outline(p, O(bark));
    }

    public static void Palm(Px p)
    {
        Pal trunk = new Pal("A0703C"), leaf = new Pal("3CA83C");
        G.Shadow(p, 22, 13.5, 9, 3.5);
        for (int y = -12; y <= 13; y++) { int x = 21 + (int)Math.Round(Math.Sin((13 - y) / 25.0 * 1.6) * 3); p.Obj(x, y, y % 2 == 0 ? trunk.M : trunk.L); p.Obj(x + 1, y, trunk.D); }
        double[][] fronds = { new[] { 11.0, -6.0 }, new[] { 36.0, -6.0 }, new[] { 13.0, -19.0 }, new[] { 34.0, -19.0 }, new[] { 25.0, -25.0 } };
        foreach (double[] f in fronds) G.Capsule(p, 24, -13, f[0], f[1], 1.7, leaf);
        p.Obj(23, -11, G.H("6A4020")); p.Obj(25, -11, G.H("6A4020")); p.Obj(24, -10, G.H("6A4020"));
        G.Outline(p, G.H("1E3A1E"));
    }

    public static void BeachBall(Px p)
    {
        Color[] cols = { G.H("E84040"), G.H("F8F8F0"), G.H("3C78E0"), G.H("F8D040") };
        G.Shadow(p, 22, 13.5, 8, 3.5);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - 21) / 7, dy = (y + 0.5 - 7.5) / 7;
                if (dx * dx + dy * dy > 1) continue;
                int seg = (int)((Math.Atan2(dy, dx) + Math.PI) / (Math.PI / 3)) % 4;
                Color c = cols[seg];
                double l = -dx * 0.6 - dy * 0.8;
                p.Obj(x, y, l > 0.5 ? G.Mix(c, Color.White, 0.35) : (l < -0.4 ? G.Mul(c, 0.72) : c));
            }
        p.Set(18, 3, Color.White); p.Set(19, 3, Color.White);
        G.Outline(p, G.H("3A2A2A"));
    }

    public static void Parasol(Px p)
    {
        G.Shadow(p, 21, 13.5, 9, 3.5);
        for (int y = -6; y <= 14; y++) p.Obj(21, y, G.H("C8C8C0"));
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - 21) / 12, dy = (y + 0.5 + 4) / 6;
                if (dx * dx + dy * dy > 1 || y > -5) continue;
                bool red = ((x + 30) / 3) % 2 == 0;
                Color c = red ? G.H("E04848") : G.H("F8F4EC");
                p.Obj(x, y, dx > 0.3 ? G.Mul(c, 0.8) : c);
            }
        G.Outline(p, G.H("4A2A2A"));
    }

    public static void Sandcastle(Px p)
    {
        Pal sand = new Pal("E0B870");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Box(p, 21, 14, 8, 5, sand);
        G.Box(p, 21, 9.5, 3.5, 5, sand);
        for (int x = 15; x <= 27; x += 3) p.Obj(x, 5 + Math.Abs(x - 21) / 3 + (x < 21 ? 0 : 0), sand.L);
        p.Obj(21, 1, G.H("8A6A40")); p.Obj(21, 0, G.H("8A6A40")); p.Obj(22, 0, G.H("E84040")); p.Obj(23, 0, G.H("E84040"));
        G.Outline(p, G.Mul(sand.D, 0.55));
    }

    public static void Snowman(Px p)
    {
        Pal snow = new Pal("F0F4FC");
        G.Shadow(p, 22, 13.5, 8, 3.5);
        G.Ellipse(p, 21, 8.5, 7, 6.2, snow);
        G.Ellipse(p, 21, -1.5, 4.8, 4.5, snow);
        G.Rect(p, 19, -11, 23, -6, G.H("2A2A34")); G.Rect(p, 17, -6, 25, -5, G.H("2A2A34")); G.Rect(p, 19, -7, 23, -7, G.H("C83838"));
        p.Obj(19, -2, G.H("1E1E24")); p.Obj(23, -2, G.H("1E1E24")); p.Obj(24, 0, G.H("F08030")); p.Obj(25, 0, G.H("F08030")); p.Obj(26, 0, G.H("F08030"));
        p.Obj(21, 6, G.H("1E1E24")); p.Obj(21, 9, G.H("1E1E24")); p.Obj(21, 12, G.H("1E1E24"));
        foreach (int i in new[] { 0, 1, 2, 3 }) { p.Obj(13 - i, 5 - i, G.H("7A5030")); p.Obj(29 + i, 5 - i, G.H("7A5030")); }
        G.Outline(p, G.H("6A7A90"));
    }

    public static void SnowPine(Px p)
    {
        Pal leaf = new Pal("2E7A4A"); Pal trunk = new Pal("7A5030");
        G.Shadow(p, 22, 13.5, 8, 3.2);
        G.Rect(p, 20, 9, 22, 14, trunk.M);
        G.Tri(p, 21, -2, 11, 9, leaf); G.Tri(p, 21, -11, 3, 7, leaf); G.Tri(p, 21, -20, -5, 5, leaf);
        Color snow = G.H("F4F8FF");
        for (int x = 11; x <= 31; x++) { if (p.m[x, 10]) p.c[x, 10] = (x % 2 == 0) ? snow : p.c[x, 10]; }
        for (int x = 13; x <= 29; x++) { if (p.m[x, 2]) p.c[x, 2] = (x % 2 == 1) ? snow : p.c[x, 2]; }
        for (int x = 15; x <= 27; x++) { if (p.m[x, -6]) p.c[x, -6] = (x % 2 == 0) ? snow : p.c[x, -6]; }
        p.c[21, -20] = snow; p.c[21, -19] = snow; p.c[20, -18] = snow; p.c[22, -18] = snow;
        G.Outline(p, G.H("163A26"));
    }

    public static void Gift(Px p, Pal box, Color ribbon)
    {
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Box(p, 21, 13.5, 7, 7, box);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                if (!p.m[x, y]) continue;
                double dx = x + 0.5 - 21, dy = y + 0.5 - 6.5;
                bool onTop = Math.Abs(dx) / 7 + Math.Abs(dy) / 3.5 <= 1;
                if (onTop ? (Math.Abs(dx - 2 * dy) < 1.2 || Math.Abs(dx + 2 * dy) < 1.2) : Math.Abs(dx) < 1) p.c[x, y] = ribbon;
            }
        G.Flat(p, 19, 3.5, 2, 1.5, ribbon); G.Flat(p, 23, 3.5, 2, 1.5, ribbon);
        G.Outline(p, G.Mul(box.D, 0.5));
    }

    public static void Pumpkin(Px p)
    {
        Pal pal = new Pal("F08A28");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Ellipse(p, 21, 8.5, 9, 6, pal);
        foreach (int x in new[] { 16, 21, 26 }) for (int y = 4; y <= 13; y++) if (p.m[x, y]) p.c[x, y] = G.Mul(p.c[x, y], 0.8);
        G.Rect(p, 21, 1, 22, 2, G.H("5A7A2A")); p.Obj(23, 2, G.H("6AA83A")); p.Obj(24, 2, G.H("6AA83A"));
        G.Outline(p, G.H("5A2A0A"));
    }

    public static void Mushroom(Px p, string capHex)
    {
        Pal stem = new Pal("F0E4CC"), cap = new Pal(capHex);
        G.Shadow(p, 22, 13.5, 8, 3.5);
        G.Cyl(p, 21, 7, 13, 2.6, 1.2, stem, stem.L);
        G.Ellipse(p, 21, 6.5, 8.5, 5.5, cap, 7.5);
        int[][] dots = { new[] { 17, 4 }, new[] { 21, 2 }, new[] { 25, 4 }, new[] { 19, 6 }, new[] { 24, 6 } };
        foreach (int[] d in dots) { p.Set(d[0], d[1], Color.White); p.Set(d[0] + 1, d[1], Color.White); }
        G.Outline(p, G.Mul(cap.D, 0.5));
    }

    public static void Tulips(Px p)
    {
        Color stem = G.H("3E9A3A");
        Color[] heads = { G.H("E83C48"), G.H("F8D040"), G.H("F07AB4") };
        int[] xs = { 15, 21, 27 }; int[] tops = { 3, -2, 4 };
        G.Shadow(p, 22, 13.5, 10, 3.8);
        for (int i = 0; i < 3; i++)
        {
            for (int y = tops[i]; y <= 13; y++) p.Obj(xs[i], y, stem);
            p.Obj(xs[i] - 1, 11, stem); p.Obj(xs[i] - 2, 10, stem); p.Obj(xs[i] + 1, 10, stem);
            G.Ellipse(p, xs[i] + 0.5, tops[i] - 0.5, 2.2, 2.4, new Pal(heads[i]));
            p.Set(xs[i], tops[i] - 3, p.c[xs[i] - 1, tops[i] - 1]);
        }
        G.Outline(p, G.H("1E3A1E"));
    }

    public static void Tree(Px p, Pal trunk, Pal canopy, params Color[] dots)
    {
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Rect(p, 20, -2, 22, 14, trunk.M);
        for (int y = -2; y <= 14; y++) p.Obj(22, y, trunk.D);
        p.Obj(23, 1, trunk.D); p.Obj(24, 0, trunk.D); p.Obj(19, 3, trunk.D); p.Obj(18, 2, trunk.D);
        G.Ellipse(p, 14, -5, 7, 5.8, canopy); G.Ellipse(p, 28, -5, 7, 5.8, canopy);
        G.Ellipse(p, 21, -13, 8.5, 7, canopy); G.Ellipse(p, 21, -3, 7.5, 5, canopy);
        int i = 0;
        foreach (int[] d in new[] { new[] { 14, -6 }, new[] { 25, -14 }, new[] { 18, -16 }, new[] { 28, -4 }, new[] { 19, -3 }, new[] { 23, -8 } })
            if (dots.Length > 0) p.Set(d[0], d[1], dots[i++ % dots.Length]);
        G.Outline(p, G.Mul(canopy.D, 0.5));
    }

    public static void StoneLantern(Px p)
    {
        Pal stone = new Pal("A8A8A0");
        G.Shadow(p, 22, 13.5, 7, 3);
        G.Box(p, 21, 14.5, 5, 2, stone);
        G.Box(p, 21, 12.5, 2.2, 8, stone);
        G.Box(p, 21, 4.5, 4.5, 4, stone);
        p.Obj(19, 2, G.H("F8D870")); p.Obj(23, 2, G.H("F8D870")); p.Obj(19, 3, G.H("F0C050")); p.Obj(23, 3, G.H("E0B040"));
        G.Box(p, 21, 0.5, 7, 2, new Pal("7A7A74"));
        p.Obj(21, -5, stone.M); p.Obj(21, -4, stone.D); p.Obj(20, -4, stone.L); p.Obj(22, -4, stone.D);
        G.Outline(p, G.H("3A3A38"));
    }

    public static void Bamboo(Px p)
    {
        Pal g = new Pal("6AB84A");
        G.Shadow(p, 22, 13.5, 9, 3.2);
        int[] xs = { 15, 20, 25 }; int[] tops = { -14, -22, -11 };
        for (int i = 0; i < 3; i++)
            for (int y = tops[i]; y <= 13; y++)
            {
                bool node = (y - tops[i]) % 5 == 4;
                p.Obj(xs[i], y, node ? g.D : g.L); p.Obj(xs[i] + 1, y, node ? g.D : g.M);
            }
        int[][] leaves = { new[] { 17, -10 }, new[] { 18, -11 }, new[] { 19, -12 }, new[] { 22, -18 }, new[] { 23, -19 }, new[] { 13, -6 }, new[] { 12, -7 }, new[] { 27, -6 }, new[] { 28, -7 }, new[] { 29, -8 } };
        foreach (int[] l in leaves) p.Obj(l[0], l[1], g.M);
        G.Outline(p, G.H("1E4A1E"));
    }

    public static void Cactus(Px p)
    {
        Pal g = new Pal("4AA050");
        G.Shadow(p, 22, 13.5, 8, 3.2);
        G.Ellipse(p, 21, -7, 3.2, 3.2, g);
        G.Cyl(p, 21, -7, 13, 3.2, 0.8, g, g.M);
        G.Capsule(p, 14.5, 5, 18, 5, 1.6, g); G.Capsule(p, 14.5, -3, 14.5, 5, 1.6, g);
        G.Capsule(p, 24, 1, 27.5, 1, 1.6, g); G.Capsule(p, 27.5, -6, 27.5, 1, 1.6, g);
        foreach (int[] s in new[] { new[] { 20, -3 }, new[] { 22, 2 }, new[] { 20, 7 }, new[] { 22, 10 }, new[] { 14, -1 }, new[] { 28, -3 } }) p.Set(s[0], s[1], G.H("E8F0C8"));
        p.Obj(21, -11, G.H("F070A0")); p.Obj(20, -10, G.H("F070A0")); p.Obj(22, -10, G.H("F070A0")); p.Obj(21, -10, G.H("F8E060"));
        G.Outline(p, G.H("1E4A24"));
    }

    public static void ClayPot(Px p)
    {
        Pal clay = new Pal("C8683C");
        G.Shadow(p, 22, 13.5, 8, 3.5);
        G.Ellipse(p, 21, 9, 6.8, 5.2, clay);
        G.Cyl(p, 21, 2.5, 4.5, 3, 1.2, clay, G.H("6A2A14"));
        for (int x = 14; x <= 28; x++) if (p.m[x, 8]) p.c[x, 8] = G.H("F0D8A0");
        G.Outline(p, G.H("4A1E10"));
    }

    public static void Tumbleweed(Px p)
    {
        Pal w = new Pal("B08850");
        G.Shadow(p, 22, 13.5, 9, 3.5);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                double dx = (x + 0.5 - 21) / 7.5, dy = (y + 0.5 - 7.5) / 6.5, d2 = dx * dx + dy * dy;
                if (d2 > 1) continue;
                if (d2 > 0.75 || (x * 7 + y * 5) % 4 == 0 || (x + 2 * y) % 5 == 0) p.Obj(x, y, w.Pick(-dx * 0.6 - dy * 0.8));
            }
        G.Outline(p, G.H("4A3018"));
    }

    public static void Strawberry(Px p)
    {
        Pal red = new Pal("E83848");
        G.Shadow(p, 22, 13.5, 8, 3.5);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                double py = y + 0.5, dx = x + 0.5 - 21;
                if (py < 3 || py > 14) continue;
                double half = py < 7 ? 6.5 * Math.Sqrt(Math.Max(0.0, 1 - Math.Pow((7 - py) / 4.5, 2))) : 6.5 * (14 - py) / 7.0 + 0.5;
                if (Math.Abs(dx) <= half) p.Obj(x, y, red.Pick(-dx / 6.5 * 0.6 - (py - 7) / 7 * 0.8));
            }
        for (int y = 5; y <= 12; y += 2) for (int x = 16 + (y % 4 == 1 ? 1 : 0); x <= 26; x += 3) if (p.m[x, y]) p.c[x, y] = G.H("F8E070");
        Color lf = G.H("3EA03A");
        G.Rect(p, 18, 2, 24, 2, lf); p.Obj(17, 3, lf); p.Obj(25, 3, lf); p.Obj(21, 1, lf); p.Obj(21, 0, G.H("2A7A2A"));
        G.Outline(p, G.H("5A1018"));
    }

    public static void Candle(Px p)
    {
        Pal wax = new Pal("F8F0F4");
        G.Shadow(p, 22, 13.5, 6, 2.8);
        G.Cyl(p, 21, -5, 13, 2.8, 1.1, wax, wax.L);
        for (int y = -4; y <= 13; y++) for (int x = 18; x <= 24; x++) if (p.m[x, y] && (x + y) % 4 == 0) p.c[x, y] = G.H("F07AA0");
        p.Obj(21, -7, G.H("3A2A2A"));
        p.Obj(21, -8, G.H("F8A030")); p.Obj(21, -9, G.H("F8D850")); p.Obj(20, -8, G.H("F8D850")); p.Obj(22, -8, G.H("F8C040")); p.Obj(21, -10, G.H("FFF4C0")); p.Obj(21, -11, G.H("FFF4C0"));
        G.Outline(p, G.H("7A4A5A"));
    }

    public static void Cherry(Px p)
    {
        Pal red = new Pal("D82838");
        G.Shadow(p, 22, 13.5, 9, 3.5);
        for (int y = 2; y <= 9; y++) { p.Obj(17 + (9 - y) / 2, y, G.H("4A6A2A")); p.Obj(25 - (9 - y) / 3, y, G.H("4A6A2A")); }
        G.Ellipse(p, 16.5, 11, 3.8, 3.6, red); G.Ellipse(p, 25.5, 10, 3.8, 3.6, red);
        G.Flat(p, 23.5, 1.5, 2.5, 1.2, G.H("4AA83A"));
        p.Set(15, 9, Color.White); p.Set(24, 8, Color.White);
        G.Outline(p, G.H("4A0A10"));
    }

    public static void Macaron(Px p)
    {
        Pal shell = new Pal("F0A0C0");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Ellipse(p, 21, 11, 8.5, 3.8, new Pal(shell.D));
        G.Ellipse(p, 21, 8.5, 8.3, 3.2, new Pal("FFF4E0"));
        G.Ellipse(p, 21, 6, 8.5, 4.5, shell);
        for (int x = 13; x <= 29; x += 2) { if (p.m[x, 9]) p.c[x, 9] = shell.M; }
        G.Outline(p, G.H("7A3A50"));
    }

    public static void CreamSwirl(Px p)
    {
        Pal cream = new Pal("FFF8EC");
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Ellipse(p, 21, 11, 8, 3.6, cream); G.Ellipse(p, 21, 8, 6.2, 3.2, cream);
        G.Ellipse(p, 21, 5, 4.4, 2.8, cream); G.Ellipse(p, 21.5, 2.5, 2.4, 1.8, cream);
        for (int x = 13; x <= 29; x++) { if (p.m[x, 10] && x % 3 == 0) p.c[x, 10] = cream.D; if (p.m[x, 7] && x % 3 == 1) p.c[x, 7] = cream.D; }
        G.Outline(p, G.H("B09A80"));
    }

    public static void Biscuit(Px p)
    {
        Pal b = new Pal("D8A060");
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Box(p, 21, 13, 7.5, 3, b);
        G.Box(p, 21, 9.5, 7.5, 2.5, new Pal("6A3A20"));
        G.Box(p, 21, 7, 7.5, 3, b);
        foreach (int[] d in new[] { new[] { 18, 3 }, new[] { 21, 2 }, new[] { 24, 3 }, new[] { 21, 5 } }) p.Set(d[0], d[1], b.D);
        G.Outline(p, G.H("5A3418"));
    }

    public static void MintLeaf(Px p)
    {
        Pal g = new Pal("5AC870");
        G.Shadow(p, 22, 13.5, 8, 3.4);
        G.Capsule(p, 15, 11, 21, 7, 3, g); G.Capsule(p, 27, 10, 21, 7, 3, g); G.Capsule(p, 21, 2, 21, 7, 3, g);
        for (int i = 0; i < 5; i++) { p.Set(16 + i, 10 - i * 3 / 4, g.D); p.Set(26 - i, 9 - i * 2 / 4, g.D); p.Set(21, 3 + i, g.D); }
        G.Outline(p, G.H("1E5A2A"));
    }

    public static void TrafficCone(Px p)
    {
        Pal o = new Pal("F07828");
        G.Shadow(p, 22, 13.5, 8, 3.4);
        G.Box(p, 21, 14.5, 6.5, 1.5, o);
        G.Tri(p, 21, 0.5, 12.5, 4.8, o);
        for (int y = 5; y <= 7; y++) for (int x = 15; x <= 27; x++) if (p.m[x, y]) p.c[x, y] = x > 22 ? G.H("D8D8D8") : G.H("F8F8F8");
        G.Outline(p, G.H("5A2A0A"));
    }

    public static void TrashCan(Px p)
    {
        Pal g = new Pal("8A9098");
        G.Shadow(p, 22, 13.5, 8, 3.4);
        G.Cyl(p, 21, 4, 13, 5.5, 2.2, g, g.D);
        for (int y = 5; y <= 14; y++) foreach (int x in new[] { 18, 21, 24 }) if (p.m[x, y]) p.c[x, y] = G.Mul(p.c[x, y], 0.82);
        G.Flat(p, 21, 3.5, 6, 2.2, g.L); G.Rect(p, 20, 1, 22, 1, g.D);
        G.Outline(p, G.H("2A2E34"));
    }

    public static void Building(Px p)
    {
        Pal wall = new Pal("9098A8");
        G.Shadow(p, 22, 14, 9, 3.8);
        const double height = 26;
        G.Box(p, 21, 14.5, 7, height, wall.L, wall.M, wall.D);
        for (int y = -9; y <= 15; y += 3)
            for (int x = 15; x <= 27; x += 2)
                if (x != 21 && p.m[x, y] && p.m[x, y + 1])
                {
                    double dx = Math.Abs(x + 0.5 - 21);
                    double faceTop = 14.5 - height + 3.5 * (1 - dx / 7);
                    double faceBottom = 14.5 + 3.5 * (1 - dx / 7);
                    if (y + 0.5 > faceTop + 1 && y + 1.5 < faceBottom) p.c[x, y] = ((x * 7 + y * 3) % 5 == 0) ? G.H("F8E080") : G.H("5A7AB0");
                }
        G.Outline(p, G.H("2A2E3A"));
    }

    public static void Hydrant(Px p)
    {
        Pal r = new Pal("D83838");
        G.Shadow(p, 22, 13.5, 7, 3);
        G.Cyl(p, 21, 5, 13, 3.2, 1.4, r, r.L);
        G.Ellipse(p, 21, 4.5, 3.4, 2.4, r, 5);
        G.Rect(p, 16, 8, 17, 9, r.D); G.Rect(p, 25, 8, 26, 9, r.D); G.Rect(p, 20, 2, 22, 2, r.M);
        G.Rect(p, 18, 13, 24, 13, r.D);
        G.Outline(p, G.H("4A0A0A"));
    }

    public static void House(Px p)
    {
        Pal wall = new Pal("F0E0C0"), roof = new Pal("C84838");
        G.Shadow(p, 22, 14, 10, 4);
        G.Box(p, 21, 15, 8, 9, wall);
        G.Rect(p, 26, -9, 28, -2, G.H("7A5A4A"));
        G.Tri(p, 21, -10, 7.5, 11, roof);
        G.Rect(p, 15, 9, 17, 14, G.H("7A4A2A"));
        p.Obj(24, 9, G.H("6AA0D8")); p.Obj(25, 9, G.H("6AA0D8")); p.Obj(24, 10, G.H("5A88C0")); p.Obj(25, 10, G.H("5A88C0"));
        G.Outline(p, G.H("4A2018"));
    }

    public static void HayBale(Px p)
    {
        Pal hay = new Pal("E8C058");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Box(p, 21, 13.5, 8, 6, hay);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++) if (p.m[x, y] && (x * 5 + y * 3) % 7 == 0) p.c[x, y] = hay.D;
        for (int y = 3; y <= 18; y++) { if (p.m[17, y]) p.c[17, y] = G.H("8A5A2A"); if (p.m[25, y]) p.c[25, y] = G.H("8A5A2A"); }
        G.Outline(p, G.H("6A4A18"));
    }

    public static void Barrel(Px p)
    {
        Pal wood = new Pal("A8703C");
        G.Shadow(p, 22, 13.5, 8, 3.4);
        G.Cyl(p, 21, 4, 13, 5.5, 2.2, wood, G.H("C89A60"));
        for (int x = 14; x <= 28; x++) { if (p.m[x, 7]) p.c[x, 7] = G.H("4A4A50"); if (p.m[x, 12]) p.c[x, 12] = G.H("4A4A50"); }
        foreach (int x in new[] { 18, 21, 24 }) for (int y = 6; y <= 14; y++) if (p.m[x, y] && p.c[x, y] != G.H("4A4A50")) p.c[x, y] = G.Mul(p.c[x, y], 0.85);
        G.Outline(p, G.H("3A2010"));
    }

    public static void Crate(Px p)
    {
        Pal wood = new Pal("C08A50");
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Box(p, 21, 13.5, 7, 7, wood);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                if (!p.m[x, y]) continue;
                double dx = x + 0.5 - 21, dy = y + 0.5 - 6.5;
                bool top = Math.Abs(dx) / 7 + Math.Abs(dy) / 3.5 <= 1;
                if (top ? ((int)(dx - 2 * dy + 40) % 4 == 0) : (Math.Abs(Math.Abs(dx) - 6.5) < 0.6 || Math.Abs(dx) < 0.6 || (y % 4 == 0))) p.c[x, y] = wood.D;
            }
        G.Outline(p, G.H("4A2A10"));
    }

    public static void Coral(Px p)
    {
        Pal c = new Pal("F06A80");
        G.Shadow(p, 22, 13.5, 9, 3.5);
        G.Capsule(p, 21, 13, 21, -2, 1.8, c); G.Capsule(p, 21, 8, 14, -3, 1.6, c); G.Capsule(p, 21, 6, 28, -5, 1.6, c);
        G.Capsule(p, 15, -2, 12, -9, 1.3, c); G.Capsule(p, 27, -4, 30, -11, 1.3, c); G.Capsule(p, 21, -1, 22, -13, 1.3, c);
        foreach (int[] t in new[] { new[] { 12, -9 }, new[] { 30, -11 }, new[] { 22, -13 } }) p.Set(t[0], t[1], c.L);
        G.Outline(p, G.H("6A1A2A"));
    }

    public static void Seaweed(Px p)
    {
        Pal g = new Pal("3AA060");
        G.Shadow(p, 22, 13.5, 9, 3.2);
        int[] xs = { 15, 21, 27 }; int[] tops = { -10, -18, -7 };
        for (int i = 0; i < 3; i++)
            for (int y = tops[i]; y <= 13; y++)
            {
                int x = xs[i] + (int)Math.Round(Math.Sin(y * 0.5 + i) * 1.6);
                p.Obj(x, y, g.L); p.Obj(x + 1, y, g.D);
            }
        G.Outline(p, G.H("164A2A"));
    }

    public static void Chest(Px p)
    {
        Pal wood = new Pal("9A5A2A");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Box(p, 21, 14, 7.5, 5, wood);
        G.Box(p, 21, 9, 7.5, 3, new Pal("7A4420"));
        for (int y = 2; y <= 18; y++) for (int x = 20; x <= 22; x++) if (p.m[x, y] && x == 21) p.c[x, y] = G.H("F0C040");
        p.Obj(21, 10, G.H("F8E070")); p.Obj(21, 11, G.H("C89820"));
        p.Set(18, 4, G.H("F8D850")); p.Set(24, 5, G.H("F8D850"));
        G.Outline(p, G.H("3A1A08"));
    }

    public static void Clam(Px p)
    {
        Pal shell = new Pal("F4C8D0");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Ellipse(p, 21, 11, 8.5, 3.6, new Pal(shell.D));
        G.Ellipse(p, 21, 7.5, 8.5, 5.5, shell, 9);
        for (int y = 2; y <= 9; y++) foreach (int x in new[] { 15, 18, 21, 24, 27 }) if (p.m[x, y]) p.c[x, y] = G.Mul(p.c[x, y], 0.85);
        G.Ellipse(p, 21, 10, 2, 1.8, new Pal("FFFFFF"));
        G.Outline(p, G.H("7A4A58"));
    }

    public static void IceCrystal(Px p)
    {
        Pal ice = new Pal("88D8F8");
        G.Shadow(p, 22, 13.5, 8, 3.2);
        Action<double, double, double, double> shard = delegate (double cx, double cy, double rx, double ry)
        {
            for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
                {
                    double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                    if (Math.Abs(dx) + Math.Abs(dy) <= 1) p.Obj(x, y, dx < -0.15 ? ice.L : (dx < 0.25 ? ice.M : ice.D));
                }
        };
        shard(21, 1, 4.4, 12.5); shard(15, 8, 2.8, 6); shard(27, 7, 2.8, 7);
        p.Set(20, -7, Color.White); p.Set(20, -6, Color.White); p.Set(20, -5, Color.White); p.Set(15, 5, Color.White);
        G.Outline(p, G.H("2A5A7A"));
    }

    public static void Igloo(Px p)
    {
        Pal snow = new Pal("E8F2FC");
        G.Shadow(p, 22, 13.5, 10, 4);
        G.Ellipse(p, 21, 12, 9.5, 9, snow, 13);
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                if (!p.m[x, y]) continue;
                if (y % 3 == 0 || ((x + (y / 3) * 2) % 5 == 0)) p.c[x, y] = G.Mul(p.c[x, y], 0.86);
            }
        G.Ellipse(p, 16.5, 12, 2.5, 3.2, new Pal("4A5A78"), 13);
        G.Outline(p, G.H("5A7090"));
    }

    public static void Penguin(Px p)
    {
        G.Shadow(p, 22, 13.5, 7, 3);
        G.Ellipse(p, 21, 7.5, 5.2, 6.5, new Pal("2A3448"));
        G.Ellipse(p, 21, 9, 3.2, 4.8, new Pal("F8F8F8"));
        p.Obj(19, 4, Color.White); p.Obj(23, 4, Color.White); p.Obj(19, 5, G.H("1E1E24")); p.Obj(23, 5, G.H("1E1E24"));
        p.Obj(21, 6, G.H("F8A030")); p.Obj(20, 6, G.H("F8A030")); p.Obj(22, 6, G.H("F8A030"));
        G.Rect(p, 18, 14, 19, 14, G.H("F8A030")); G.Rect(p, 23, 14, 24, 14, G.H("F8A030"));
        G.Outline(p, G.H("12161E"));
    }

    public static void IceBlock(Px p)
    {
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Box(p, 21, 13.5, 7, 7, G.H("D0F0FC"), G.H("8CCCEC"), G.H("6AAAD8"));
        for (int i = 0; i < 5; i++) { p.Set(16 + i, 9 + i / 2, Color.White); p.Set(23, 10 + i, G.H("B8E4F8")); }
        G.Outline(p, G.H("2A5A80"));
    }

    public static void Desk(Px p)
    {
        Pal wood = new Pal("B07A48");
        G.Shadow(p, 22, 14, 10, 4);
        G.Box(p, 21, 15, 8.5, 4, wood);
        G.Rect(p, 15, 1, 27, 8, G.H("2A2E38"));
        for (int y = 2; y <= 7; y++) for (int x = 16; x <= 26; x++) p.c[x, y] = G.Mix(G.H("5AA8E8"), G.H("3A70C0"), (y - 2) / 5.0);
        p.c[17, 3] = G.H("C8E8FF"); p.c[18, 3] = G.H("C8E8FF");
        G.Rect(p, 20, 9, 22, 10, G.H("3A3E48"));
        G.Outline(p, G.H("1E1E24"));
    }

    public static void PottedPlant(Px p)
    {
        Pal pot = new Pal("D07848"), leaf = new Pal("4AA84A");
        G.Shadow(p, 22, 13.5, 8, 3.4);
        G.Cyl(p, 21, 9, 13, 4.2, 1.6, pot, G.H("5A3A20"));
        G.Capsule(p, 21, 8, 15, 3, 1.8, leaf); G.Capsule(p, 21, 8, 27, 3, 1.8, leaf); G.Capsule(p, 21, 8, 21, 0.5, 1.8, leaf);
        G.Capsule(p, 21, 8, 17, 6, 1.6, leaf); G.Capsule(p, 21, 8, 25.5, 6.5, 1.6, leaf);
        G.Outline(p, G.H("1E3A1E"));
    }

    public static void Cabinet(Px p)
    {
        G.Shadow(p, 22, 14, 8, 3.4);
        G.Box(p, 21, 14.5, 6, 11, G.H("C8CCD4"), G.H("9AA0AA"), G.H("7A808A"));
        for (int y = p.Top; y < Px.N; y++) for (int x = 0; x < Px.N; x++)
            {
                if (!p.m[x, y]) continue;
                double dx = Math.Abs(x + 0.5 - 21);
                double faceTop = 14.5 - 11 + 3 * (1 - dx / 6);
                if (y + 0.5 > faceTop + 0.5 && ((int)(y + 0.5 - faceTop) % 4 == 0)) p.c[x, y] = G.H("5A606A");
            }
        foreach (int[] h in new[] { new[] { 18, 8 }, new[] { 18, 12 }, new[] { 24, 8 }, new[] { 24, 12 } }) p.Set(h[0], h[1], G.H("E8E8F0"));
        G.Outline(p, G.H("2A2E36"));
    }

    public static void Books(Px p)
    {
        G.Shadow(p, 22, 13.5, 9, 3.8);
        G.Box(p, 21, 14, 7.5, 2.2, new Pal("4A70C8"));
        G.Box(p, 20.5, 11.8, 7, 2.2, new Pal("D84848"));
        G.Box(p, 21.5, 9.6, 7.2, 2.2, new Pal("4AA860"));
        G.Box(p, 21, 7.4, 6.5, 2.2, new Pal("F0C048"));
        G.Outline(p, G.H("1E1E28"));
    }
}

public class ObstructSet
{
    public Action<Px> Base;
    public Action<Px>[] Decos;
}

public class Theme
{
    public string Id, Name;
    public Action<Px> Untouched, Touched, Water, Bridge;
    public ObstructSet Obstructs;
    public Func<int, int, Random, Color> BgBase;
    public Action<Color[,], Random> BgDecor;
}

public static class ThemeGen
{
    const int B = 64;
    /// <summary>Extra rows above the block for tall decorations</summary>
    public const int DecoExtraRows = 28;

    static Action<Px> Ground(TopFn top, SideFn side, Action<Px> deco)
    {
        return delegate (Px p) { G.Cube(p, top, side, 0, G.H("2A2018")); if (deco != null) deco(p); };
    }

    static Action<Px> Liquid(string topHex, string leftHex, string rightHex, string waveHex)
    {
        return delegate (Px p)
        {
            G.Cube(p, G.Noise(topHex, 7), G.Side(leftHex, rightHex, null, 0), 7, G.Mul(G.H(leftHex), 0.5));
            G.Dashes(p, 7, 5, 9, G.H(waveHex), 4);
        };
    }

    static string Hex(Color c) { return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"); }

    // 水の上の板の足場（板の厚みの下に水が見える）
    static Action<Px> BridgeTile(string plankHex, string leftHex, string rightHex)
    {
        return delegate (Px p)
        {
            Color plank = G.H(plankHex);
            Color gap = G.Mul(G.H(leftHex), 0.7);
            Color beam = G.Mul(plank, 0.6);
            TopFn top = delegate (int x, int y, double u, double v, Random r)
            {
                // 板の端の下を通る横木
                if (Math.Abs(u) > 0.8) return G.Sh(beam, r.Next(-4, 5));

                // 板（4枚）と板の隙間
                double plankPos = (v + 1) * 2;
                if (G.Frac(plankPos) < 0.2) return gap;
                int index = (int)Math.Floor(plankPos);
                Color baseColor = index % 2 == 0 ? plank : G.Mul(plank, 0.9);

                // 釘
                double center = Math.Abs(G.Frac(plankPos) - 0.6);
                if (center < 0.12 && Math.Abs(Math.Abs(u) - 0.62) < 0.06) return G.Mul(plank, 0.45);

                // 木目
                if (Math.Abs(Math.Sin(u * 9 + index * 1.7)) < 0.12 && center > 0.2) return G.Mul(baseColor, 0.82);
                return G.Sh(baseColor, r.Next(-5, 6));
            };
            G.Cube(p, top, G.Side(leftHex, rightHex, Hex(G.Mul(plank, 0.7)), 0.2), 4, G.Mul(G.H(leftHex), 0.5));

            // 左右の角の杭
            Color post = G.Mul(plank, 0.55), cap = G.Mul(plank, 0.85);
            foreach (int px in new[] { 2, 38 })
            {
                for (int py = 10; py <= 16; py++)
                {
                    p.Set(px, py, post);
                    p.Set(px + 1, py, G.Mul(post, 0.8));
                }
                p.Set(px, 9, cap);
                p.Set(px + 1, 9, cap);
                p.Set(px - 1, 9, G.Mul(G.H(leftHex), 0.5));
                p.Set(px + 2, 9, G.Mul(G.H(leftHex), 0.5));
                p.Set(px, 8, G.Mul(G.H(leftHex), 0.5));
                p.Set(px + 1, 8, G.Mul(G.H(leftHex), 0.5));
            }
        };
    }
    static Action<Px> On(Action<Px> baseFn, Action<Px> obj) { return delegate (Px p) { baseFn(p); obj(p); }; }

    // ---------- seamless background helpers ----------
    static void Stamp(Color[,] img, int x, int y, Color c) { img[((x % B) + B) % B, ((y % B) + B) % B] = c; }
    static void Patches(Color[,] img, Random r, int count, double k)
    {
        for (int i = 0; i < count; i++)
        {
            int cx = r.Next(B), cy = r.Next(B), rad = r.Next(3, 8);
            for (int dy = -rad; dy <= rad; dy++) for (int dx = -rad; dx <= rad; dx++)
                    if (dx * dx + dy * dy <= rad * rad) { int x = ((cx + dx) % B + B) % B, y = ((cy + dy) % B + B) % B; img[x, y] = G.Mul(img[x, y], k); }
        }
    }
    static void Sprinkle(Color[,] img, Random r, int count, params Color[] cols) { for (int i = 0; i < count; i++) Stamp(img, r.Next(B), r.Next(B), cols[i % cols.Length]); }
    static void Tufts(Color[,] img, Random r, int count, Color c)
    {
        for (int i = 0; i < count; i++) { int x = r.Next(B), y = r.Next(B); Stamp(img, x, y, c); Stamp(img, x - 1, y - 1, c); Stamp(img, x + 1, y - 1, c); Stamp(img, x, y - 1, c); }
    }
    static void Petals(Color[,] img, Random r, int count, params Color[] cols)
    {
        for (int i = 0; i < count; i++) { int x = r.Next(B), y = r.Next(B); Color c = cols[i % cols.Length]; Stamp(img, x, y, c); Stamp(img, x + 1, y, c); Stamp(img, x, y + 1, G.Mul(c, 0.9)); }
    }
    static Func<int, int, Random, Color> Plain(string hex, int amp) { Color b = G.H(hex); return delegate (int x, int y, Random r) { return G.Sh(b, r.Next(-amp, amp + 1)); }; }

    public static List<Theme> All()
    {
        List<Theme> list = new List<Theme>();
        Color outline = G.H("2A2018");

        // shared sides
        SideFn dirtSide = G.Side("60422C", "805A3C", null, 0);
        SideFn grassSide = G.Side("60422C", "805A3C", "4E9238", 0.14);

        // 000001 meadow
        list.Add(new Theme
        {
            Id = "000001", Name = "meadow",
            Untouched = Ground(G.Noise("986C48", 10, "BE9C74", 0.04, "704E34", 0.04), dirtSide, null),
            Touched = Ground(G.Noise("70BE48", 8, "96D660", 0.10, "54983A", 0.08), grassSide, delegate (Px p) { G.Flowers(p, 0, 9, G.H("FACD3C"), G.H("FFFFFF"), G.H("FFAAC8"), G.H("BE96FF"), G.H("FFE65A")); }),
            Obstructs = MakeObstructs(G.Noise("56923E", 8, "6EA84E", 0.1, "407630", 0.08), grassSide,
                delegate (Px p) { Obj.Rock(p, new Pal("989AA4")); },
                delegate (Px p) { Obj.Log(p, new Pal("845632"), G.H("DEBA7C")); },
                delegate (Px p) { Obj.Tree(p, new Pal("7C502E"), new Pal("4AA040"), G.H("E2383C"), G.H("F06048")); },
                delegate (Px p) { Obj.Stump(p, new Pal("7C502E"), G.H("E2BE82")); }),
            Water = Liquid("58AAE6", "224A82", "326CAC", "C8ECFF"),
            Bridge = BridgeTile("9A6A40", "224A82", "326CAC"),
            BgBase = Plain("4A7A42", 5),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 9, 0.94); Tufts(img, r, 40, G.H("689C54")); Sprinkle(img, r, 7, G.H("ECECDC"), G.H("ECB0C4")); }
        });

        // 000002 summer (beach)
        SideFn sandSide = G.Side("B08850", "C8A064", null, 0);
        list.Add(new Theme
        {
            Id = "000002", Name = "summer",
            Untouched = Ground(G.Noise("E6C88C", 8, "F4DCA8", 0.06, "C8A870", 0.05), sandSide, null),
            Touched = Ground(G.Noise("5CC850", 8, "84E06A", 0.10, "40A03C", 0.08), G.Side("B08850", "C8A064", "48B040", 0.14), delegate (Px p) { G.Flowers(p, 0, 7, G.H("F8E050"), G.H("F04848"), G.H("F89030"), G.H("F06AB0")); }),
            Obstructs = MakeObstructs(G.Noise("EED6A0", 6, "F8E8C0", 0.08), sandSide,
                Obj.Palm, Obj.BeachBall, Obj.Parasol, Obj.Sandcastle),
            Water = Liquid("40D0D0", "1E7A8A", "2AA0B0", "E0FFFF"),
            Bridge = BridgeTile("C8A878", "1E7A8A", "2AA0B0"),
            BgBase = Plain("F0DCA0", 5),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 8, 0.96); Sprinkle(img, r, 10, G.H("F8F0E0"), G.H("F0B8A0"), G.H("D8B880")); }
        });

        // 000003 winter
        SideFn frostSide = G.Side("5A5048", "7A6A5E", null, 0);
        SideFn snowSide = G.Side("5A5048", "7A6A5E", "E8F0FA", 0.16);
        list.Add(new Theme
        {
            Id = "000003", Name = "winter",
            Untouched = Ground(G.Noise("8A8C98", 8, "A8AAB8", 0.06, "6E7080", 0.05), frostSide, null),
            Touched = Ground(G.Noise("F2F6FE", 4, "FFFFFF", 0.08, "D8E4F4", 0.08), snowSide, delegate (Px p) { G.Dots(p, 0, 8, 3, G.H("B8D8F8"), G.H("FFFFFF")); }),
            Obstructs = MakeObstructs(G.Noise("E4ECF8", 4, "FFFFFF", 0.08), snowSide,
                Obj.Snowman, Obj.SnowPine,
                delegate (Px p) { Obj.Gift(p, new Pal("D83C48"), G.H("F8D850")); },
                delegate (Px p) { Obj.CapRock(p, new Pal("8A8C98"), new Pal("F4F8FF")); }),
            Water = Liquid("3A6A9A", "1A3456", "26486E", "A8C8E8"),
            Bridge = BridgeTile("8A6A50", "1A3456", "26486E"),
            BgBase = Plain("E8F0F8", 3),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 8, 0.97); Sprinkle(img, r, 16, G.H("FFFFFF"), G.H("C8DCF0")); }
        });

        // 000004 autumn
        SideFn autumnSide = G.Side("60422C", "805A3C", "B08030", 0.14);
        list.Add(new Theme
        {
            Id = "000004", Name = "autumn",
            Untouched = Ground(G.Noise("9A6E46", 10, "BA9068", 0.04, "6E4C30", 0.05), dirtSide, null),
            Touched = Ground(G.Noise("C89838", 8, "E0B450", 0.10, "A87A28", 0.08), autumnSide, delegate (Px p) { G.Flowers(p, 0, 8, G.H("C84020"), G.H("E86A28"), G.H("D83A28"), G.H("F0A030")); }),
            Obstructs = MakeObstructs(G.Noise("A07A3A", 8, "B88E48", 0.1, "805E2A", 0.08), autumnSide,
                Obj.Pumpkin,
                delegate (Px p) { Obj.Mushroom(p, "D84030"); },
                delegate (Px p) { Obj.Bush(p, new Pal("D86A28"), G.H("F0C040"), G.H("B83020")); },
                delegate (Px p) { Obj.Stump(p, new Pal("7C502E"), G.H("E2BE82")); }),
            Water = Liquid("4E90C8", "1E4070", "2C5A90", "C8E4FF"),
            Bridge = BridgeTile("8A5A30", "1E4070", "2C5A90"),
            BgBase = Plain("A0602C", 6),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 10, 0.92); Petals(img, r, 40, G.H("D84828"), G.H("F0A030"), G.H("C87028"), G.H("8A4A20")); }
        });

        // 000005 spring
        SideFn springSide = G.Side("6E4C32", "8E6644", "72C04A", 0.14);
        list.Add(new Theme
        {
            Id = "000005", Name = "spring",
            Untouched = Ground(G.Noise("A88058", 9, "C8A27C", 0.05, "86603E", 0.04), G.Side("6E4C32", "8E6644", null, 0), null),
            Touched = Ground(G.Noise("8CDC5C", 7, "AEEA7C", 0.10, "6CBE48", 0.08), springSide, delegate (Px p) { G.Flowers(p, 0, 9, G.H("F8D040"), G.H("FFFFFF"), G.H("F890B8"), G.H("FFFFFF"), G.H("A8C8FF")); }),
            Obstructs = MakeObstructs(G.Noise("6CB048", 7, "84C45C", 0.1), springSide,
                Obj.Tulips,
                delegate (Px p) { Obj.Bush(p, new Pal("4EA848"), G.H("FFFFFF"), G.H("F8B0D0")); },
                delegate (Px p) { Obj.CapRock(p, new Pal("9A9CA4"), new Pal("6AB04A")); },
                delegate (Px p) { Obj.Log(p, new Pal("8A5C36"), G.H("E4C488")); }),
            Water = Liquid("64B8EC", "26568E", "3474B4", "E0F4FF"),
            Bridge = BridgeTile("A87A48", "26568E", "3474B4"),
            BgBase = Plain("6CAE50", 5),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 9, 0.95); Tufts(img, r, 30, G.H("8CC86A")); Petals(img, r, 10, G.H("FFFFFF"), G.H("F8E070"), G.H("F8A8C8")); }
        });

        // 000006 sakura
        SideFn sakuraSide = G.Side("5E4434", "7E5E48", "6AAA50", 0.14);
        list.Add(new Theme
        {
            Id = "000006", Name = "sakura",
            Untouched = Ground(G.Noise("8E6A50", 9, "AA8668", 0.05, "6E5040", 0.04), G.Side("5E4434", "7E5E48", null, 0), null),
            Touched = Ground(G.Noise("7CC060", 7, "98D47A", 0.1, "60A048", 0.08), sakuraSide, delegate (Px p) { G.Flowers(p, 0, 9, G.H("F8E8F0"), G.H("F8B8D0"), G.H("F4A0C0"), G.H("FFD8E8")); }),
            Obstructs = MakeObstructs(G.Noise("609A48", 7, "78B05A", 0.1), sakuraSide,
                delegate (Px p) { Obj.Tree(p, new Pal("6A4434"), new Pal("F4A8C8"), G.H("FFFFFF"), G.H("FFD0E4")); },
                Obj.StoneLantern, Obj.Bamboo,
                delegate (Px p) { Obj.Rock(p, new Pal("8A8C94")); }),
            Water = Liquid("4CA0C8", "1E4A6A", "2C6A8E", "D0F0FF"),
            Bridge = BridgeTile("B04A3A", "1E4A6A", "2C6A8E"),
            BgBase = Plain("F4D4DC", 3),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 8, 0.97); Petals(img, r, 24, G.H("F4A0BC"), G.H("FFFFFF"), G.H("F8C0D4")); }
        });

        // 000007 desert
        SideFn desertSide = G.Side("9A5430", "B8683C", null, 0);
        list.Add(new Theme
        {
            Id = "000007", Name = "desert",
            Untouched = Ground(G.Noise("C8784A", 8, "DA8E5E", 0.06, "A8603A", 0.05), desertSide, null),
            Touched = Ground(delegate (int x, int y, double u, double v, Random r) { return G.Frac(u * 3.2 + v * 0.6) < 0.16 ? G.H("D4A458") : G.Sh(G.H("E8C070"), r.Next(-6, 7)); },
                G.Side("9A5430", "B8683C", "D8A858", 0.12), delegate (Px p) { G.Dots(p, 0, 5, 7, G.H("A89040"), G.H("F8E8B8")); }),
            Obstructs = MakeObstructs(G.Noise("D49A5A", 7, "E0AE70", 0.08), desertSide,
                Obj.Cactus,
                delegate (Px p) { Obj.Rock(p, new Pal("B8603C")); },
                Obj.ClayPot, Obj.Tumbleweed),
            Water = Liquid("3CB4C8", "1A5A6A", "26808E", "D8FCFF"),
            Bridge = BridgeTile("B08850", "1A5A6A", "26808E"),
            BgBase = delegate (int x, int y, Random r) { return G.Frac((x + y * 2) / 16.0 + Math.Sin(x / 10.0) * 0.1) < 0.12 ? G.H("D0A060") : G.Sh(G.H("E0B070"), r.Next(-4, 5)); },
            BgDecor = delegate (Color[,] img, Random r) { Sprinkle(img, r, 8, G.H("C89050"), G.H("F0D090")); }
        });

        // 000008 cake
        SideFn spongeSide = G.Banded("D8B060", "E6C478", "F8F0E8", 0.12, "F8A0C0", 0.45, 0.6);
        list.Add(new Theme
        {
            Id = "000008", Name = "cake",
            Untouched = Ground(G.Noise("F0D890", 6, "D8B868", 0.10, "FAE8B0", 0.05), G.Banded("D8B060", "E6C478", null, 0, "F8F0E8", 0.45, 0.6), null),
            Touched = Ground(G.Noise("F8A0C0", 5, "FAB8D0", 0.08), G.Banded("D8B060", "E6C478", "F890B4", 0.16, "F8F0E8", 0.45, 0.6), delegate (Px p) { G.Dots(p, 0, 14, 4, G.H("F8E050"), G.H("60C8F0"), G.H("FFFFFF"), G.H("80E080"), G.H("B070E0")); }),
            Obstructs = MakeObstructs(G.Noise("F8F0E8", 3, "FFFFFF", 0.08), spongeSide,
                Obj.Strawberry, Obj.Candle, Obj.Cherry, Obj.Macaron),
            Water = Liquid("6A3A24", "3A2014", "50301C", "A87050"),
            Bridge = BridgeTile("D8A060", "3A2014", "50301C"),
            BgBase = Plain("FAD0DC", 3),
            BgDecor = delegate (Color[,] img, Random r) { Sprinkle(img, r, 30, G.H("F8E050"), G.H("60C8F0"), G.H("FFFFFF"), G.H("80E080"), G.H("B070E0")); }
        });

        // 000009 pudding
        SideFn custardSide = G.Side("D8A840", "E8C050", null, 0);
        list.Add(new Theme
        {
            Id = "000009", Name = "pudding",
            Untouched = Ground(G.Noise("F6D86C", 5, "FAE494", 0.08), custardSide, null),
            Touched = Ground(delegate (int x, int y, double u, double v, Random r) { return (u < -0.55 && v < -0.2) ? G.H("E8A860") : G.Sh(G.H("C8783C"), r.Next(-5, 6)); },
                G.Side("D8A840", "E8C050", "A85A20", 0.2), delegate (Px p) { G.Dots(p, 0, 4, 11, G.H("FFF8EC")); }),
            Obstructs = MakeObstructs(G.Noise("FAE494", 4), custardSide,
                Obj.CreamSwirl, Obj.Cherry, Obj.Biscuit, Obj.MintLeaf),
            Water = Liquid("A85A20", "5A2A0A", "7A3C12", "E0A060"),
            Bridge = BridgeTile("E8C878", "5A2A0A", "7A3C12"),
            BgBase = delegate (int x, int y, Random r) { int cx = x % 16, cy = (y + (x / 16 % 2) * 8) % 16; return ((cx - 8) * (cx - 8) + (cy - 8) * (cy - 8) < 9) ? G.H("FCE8A8") : G.Sh(G.H("FFF4D8"), r.Next(-2, 3)); },
            BgDecor = null
        });

        // 000010 city
        SideFn concreteSide = G.Side("5A5C64", "72747C", null, 0);
        list.Add(new Theme
        {
            Id = "000010", Name = "city",
            Untouched = Ground(G.Noise("5A5C64", 6, "6E7078", 0.06, "4A4C54", 0.05), G.Side("3E4046", "4C4E56", null, 0), null),
            Touched = Ground(delegate (int x, int y, double u, double v, Random r) { return (G.Frac(u * 2.5) < 0.14 || G.Frac(v * 2.5) < 0.14) ? G.H("8E9098") : G.Sh(G.H("C8C8CC"), r.Next(-4, 5)); },
                concreteSide, null),
            Obstructs = MakeObstructs(G.Noise("A0A0A8", 5), concreteSide,
                Obj.TrafficCone, Obj.TrashCan, Obj.Building, Obj.Hydrant),
            Water = Liquid("3A5A78", "182A3A", "22384E", "8AA8C4"),
            Bridge = BridgeTile("8A8E96", "182A3A", "22384E"),
            BgBase = delegate (int x, int y, Random r) { return (x % 16 == 0 || y % 16 == 0) ? G.H("70747C") : G.Sh(G.H("8A8E96"), r.Next(-3, 4)); },
            BgDecor = delegate (Color[,] img, Random r) { Sprinkle(img, r, 10, G.H("7A7E86"), G.H("9A9EA6")); }
        });

        // 000011 village
        SideFn villageSide = G.Side("6E4C32", "8E6644", "5E9A42", 0.14);
        list.Add(new Theme
        {
            Id = "000011", Name = "village",
            Untouched = Ground(G.Noise("B08A5C", 8, "C8A478", 0.05, "8E6A44", 0.05), G.Side("6E4C32", "8E6644", null, 0), null),
            Touched = Ground(G.Noise("78B850", 7, "94CC68", 0.1, "5E9A40", 0.08), villageSide, delegate (Px p) { G.Dots(p, 0, 9, 13, G.H("4E8A34"), G.H("A8E080"), G.H("FFFFFF")); }),
            Obstructs = MakeObstructs(G.Noise("6AA048", 7, "80B45A", 0.1), villageSide,
                Obj.House, Obj.HayBale, Obj.Barrel, Obj.Crate),
            Water = Liquid("58A0D8", "22487A", "30649E", "D0ECFF"),
            Bridge = BridgeTile("9A6A40", "22487A", "30649E"),
            BgBase = Plain("5E9448", 5),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 8, 0.94); Tufts(img, r, 26, G.H("78AE5E")); Sprinkle(img, r, 12, G.H("A89A88"), G.H("8A7E70")); }
        });

        // 000012 sea (underwater)
        SideFn seabedSide = G.Side("7A6E54", "948668", null, 0);
        list.Add(new Theme
        {
            Id = "000012", Name = "sea",
            Untouched = Ground(G.Noise("A89A7A", 7, "BEB090", 0.05, "8E8264", 0.05), seabedSide, null),
            Touched = Ground(G.Noise("F0DCA8", 6, "F8EAC4", 0.08), G.Side("7A6E54", "948668", "E0C890", 0.12), delegate (Px p) { G.Flowers(p, 0, 4, G.H("F07848"), G.H("F8A070"), G.H("F8C8B0")); G.Dots(p, 0, 5, 17, G.H("FFFFFF"), G.H("F8C0D0")); }),
            Obstructs = MakeObstructs(G.Noise("C8B888", 6), seabedSide,
                Obj.Coral, Obj.Seaweed, Obj.Chest, Obj.Clam),
            Water = Liquid("205A9A", "0E2A4E", "163E6C", "70A8E0"),
            Bridge = BridgeTile("A08A68", "0E2A4E", "163E6C"),
            BgBase = Plain("1E5A8C", 4),
            BgDecor = delegate (Color[,] img, Random r) { Patches(img, r, 10, 1.12); Sprinkle(img, r, 12, G.H("6AA0D0"), G.H("A8D0F0")); }
        });

        // 000013 ice
        SideFn iceSide = G.Side("3A6A98", "5080B0", null, 0);
        list.Add(new Theme
        {
            Id = "000013", Name = "ice",
            Untouched = Ground(delegate (int x, int y, double u, double v, Random r) { return (Math.Abs(u - v * 0.5) < 0.06 || Math.Abs(v + 0.4 - u * 0.3) < 0.05) ? G.H("A8D0F0") : G.Sh(G.H("6A9AC8"), r.Next(-6, 7)); }, iceSide, null),
            Touched = Ground(G.Noise("E0F4FF", 4, "FFFFFF", 0.1, "C0E4F8", 0.08), G.Side("3A6A98", "5080B0", "E0F4FF", 0.16), delegate (Px p) { G.Dots(p, 0, 7, 19, G.H("FFFFFF"), G.H("A8E0FF")); }),
            Obstructs = MakeObstructs(G.Noise("C8E4F4", 4, "E8F6FF", 0.1), iceSide,
                Obj.IceCrystal, Obj.Igloo, Obj.Penguin, Obj.IceBlock),
            Water = Liquid("88C8E8", "3A7AA8", "5096C4", "F0FCFF"),
            Bridge = BridgeTile("C8E8F8", "3A7AA8", "5096C4"),
            BgBase = Plain("C8E0F0", 3),
            BgDecor = delegate (Color[,] img, Random r)
            {
                for (int i = 0; i < 6; i++) { int x = r.Next(B), y = r.Next(B), dx = r.Next(2) == 0 ? 1 : -1; for (int k = 0; k < 10; k++) { Stamp(img, x + k * dx, y + k / 2, G.H("F4FAFF")); } }
                Sprinkle(img, r, 10, G.H("FFFFFF"), G.H("A8C8E0"));
            }
        });

        // 000014 office
        SideFn officeSide = G.Side("4A4E56", "5E626A", null, 0);
        list.Add(new Theme
        {
            Id = "000014", Name = "office",
            Untouched = Ground(delegate (int x, int y, double u, double v, Random r) { return ((x + y) % 4 == 0) ? G.H("7A7E86") : G.Sh(G.H("8A8E96"), r.Next(-4, 5)); }, officeSide, null),
            Touched = Ground(delegate (int x, int y, double u, double v, Random r)
            {
                int plank = (int)Math.Floor(v * 4 + 4);
                if (G.Frac(v * 4) < 0.12) return G.H("8A6038");
                if (G.Frac(u * 2 + plank * 0.37) < 0.06) return G.H("9A6E42");
                return G.Sh(plank % 2 == 0 ? G.H("C89A64") : G.H("BC8E58"), r.Next(-4, 5));
            }, G.Side("4A4E56", "5E626A", "A87A48", 0.1), null),
            Obstructs = MakeObstructs(G.Noise("7A88A0", 5), officeSide,
                Obj.Desk, Obj.PottedPlant, Obj.Cabinet, Obj.Books),
            Water = Liquid("5A3820", "2A180C", "3E2614", "A88060"),
            Bridge = BridgeTile("B08860", "2A180C", "3E2614"),
            BgBase = delegate (int x, int y, Random r) { return ((x / 16 + y / 16) % 2 == 0) ? G.Sh(G.H("707A8A"), r.Next(-3, 4)) : G.Sh(G.H("66707E"), r.Next(-3, 4)); },
            BgDecor = null
        });

        return list;
    }

    static ObstructSet MakeObstructs(TopFn baseTop, SideFn baseSide, params Action<Px>[] objs)
    {
        return new ObstructSet
        {
            Base = delegate (Px p) { G.Cube(p, baseTop, baseSide, 0, G.H("2A2018")); },
            Decos = objs,
        };
    }

    static void SaveBg(Theme t, string path)
    {
        Random r = new Random(t.Id.GetHashCode());
        Color[,] img = new Color[B, B];
        for (int y = 0; y < B; y++) for (int x = 0; x < B; x++) img[x, y] = t.BgBase(x, y, r);
        if (t.BgDecor != null) t.BgDecor(img, r);
        using (Bitmap big = new Bitmap(B * 4, B * 4, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < B; y++) for (int x = 0; x < B; x++) for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) big.SetPixel(x * 4 + i, y * 4 + j, img[x, y]);
            big.Save(path, ImageFormat.Png);
        }
    }

    public static string Run(string outRoot)
    {
        List<Theme> themes = All();
        int seed = 1;
        foreach (Theme t in themes)
        {
            string dir = Path.Combine(outRoot, "theme_" + t.Id);
            Directory.CreateDirectory(dir);
            Px p;
            p = new Px(seed++); t.Untouched(p); G.Save(p, Path.Combine(dir, t.Id + "_0.png"));
            p = new Px(seed++); t.Touched(p); G.Save(p, Path.Combine(dir, t.Id + "_1.png"));
            // 2〜5 = デコ（透明背景）、7 = 通れないブロックの土台
            Px.YMin = -DecoExtraRows;
            for (int i = 0; i < t.Obstructs.Decos.Length; i++) { p = new Px(seed++); t.Obstructs.Decos[i](p); G.Save(p, Path.Combine(dir, t.Id + "_" + (2 + i) + ".png")); }
            Px.YMin = 0;
            p = new Px(seed++); t.Water(p); G.Save(p, Path.Combine(dir, t.Id + "_6.png"));
            p = new Px(seed++); t.Obstructs.Base(p); G.Save(p, Path.Combine(dir, t.Id + "_7.png"));
            p = new Px(seed++); t.Bridge(p); G.Save(p, Path.Combine(dir, t.Id + "_8.png"));
            SaveBg(t, Path.Combine(dir, t.Id + "_bg.png"));
        }
        SaveSheets(outRoot, themes);
        return themes.Count + " themes";
    }

    static void SaveSheets(string outRoot, List<Theme> themes)
    {
        const int cell = 120, pad = 8, label = 18, perSheet = 7;
        for (int s = 0; s * perSheet < themes.Count; s++)
        {
            int rows = Math.Min(perSheet, themes.Count - s * perSheet);
            int rowHeight = (cell * 281 / 169) + label + pad;
            using (Bitmap sheet = new Bitmap(pad + 10 * (cell + pad) + 90, pad + rows * rowHeight))
            using (Graphics g = Graphics.FromImage(sheet))
            using (Font font = new Font("Segoe UI", 9))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.Clear(Color.FromArgb(236, 240, 232));
                for (int row = 0; row < rows; row++)
                {
                    Theme t = themes[s * perSheet + row];
                    string dir = Path.Combine(outRoot, "theme_" + t.Id);
                    int y = pad + row * rowHeight;
                    int baseY = y + rowHeight - pad - cell;
                    g.DrawString("theme_" + t.Id + "  " + t.Name, font, Brushes.Black, pad, y);
                    string[] names = { "_bg", "_0", "_1", "_7", "_2", "_3", "_4", "_5", "_6", "_8" };
                    for (int i = 0; i < names.Length; i++)
                        using (Image img = Image.FromFile(Path.Combine(dir, t.Id + names[i] + ".png")))
                        {
                            int x = pad + i * (cell + pad);
                            if (i == 0) { g.DrawImage(img, new Rectangle(x, baseY, cell, cell), new Rectangle(0, 0, 120, 120), GraphicsUnit.Pixel); continue; }
                            // デコは土台の上に重ねて見せる（下端をそろえる）
                            int h = cell * img.Height / img.Width;
                            if (i >= 4 && i <= 7)
                                using (Image block = Image.FromFile(Path.Combine(dir, t.Id + "_7.png"))) g.DrawImage(block, x, baseY, cell, cell);
                            g.DrawImage(img, x, baseY + cell - h, cell, h);
                        }
                }
                sheet.Save(Path.Combine(outRoot, "sheet_" + (s + 1) + ".png"), ImageFormat.Png);
            }
        }
    }
}
