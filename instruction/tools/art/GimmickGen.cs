// Gimmick icons (C# 5 / System.Drawing), small pixel art scaled x4.
//   gimmick_arrow    : flat arrow pointing right (rotated on the block top face in game)
//   gimmick_turn     : "must turn" sign (corner arrow in a circle)
//   gimmick_straight : "go straight" sign (two bars + arrow heads in a circle)
//   gimmick_bone     : bone snack
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class GimmickGen
{
    const int S = 4;
    static Color C(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    static readonly Color Outline = C("16100A");
    const int OutlineWidth = 2;

    static void Save(string[] pat, Func<char, Color> color, string path)
    {
        int h = pat.Length, w = pat[0].Length;
        var c = new Color[w + 2, h + 2]; var m = new bool[w + 2, h + 2];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (pat[y][x] != '.') { c[x + 1, y + 1] = color(pat[y][x]); m[x + 1, y + 1] = true; }
        var add = new bool[w + 2, h + 2];
        for (int y = 0; y < h + 2; y++) for (int x = 0; x < w + 2; x++)
            {
                if (m[x, y]) continue;
                if ((x + 1 < w + 2 && m[x + 1, y]) || (x > 0 && m[x - 1, y]) || (y + 1 < h + 2 && m[x, y + 1]) || (y > 0 && m[x, y - 1])) add[x, y] = true;
            }
        for (int y = 0; y < h + 2; y++) for (int x = 0; x < w + 2; x++) if (add[x, y]) c[x, y] = Outline;
        using (var b = new Bitmap((w + 2) * S, (h + 2) * S, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < h + 2; y++) for (int x = 0; x < w + 2; x++)
                {
                    if (c[x, y].A == 0) continue;
                    for (int j = 0; j < S; j++) for (int i = 0; i < S; i++) b.SetPixel(x * S + i, y * S + j, c[x, y]);
                }
            b.Save(path, ImageFormat.Png);
        }
    }

    // circle sign 13x13 with a pattern on top ('X' = symbol)
    static string[] Sign(string[] symbol)
    {
        int n = 13; var rows = new string[n];
        for (int y = 0; y < n; y++)
        {
            var chars = new char[n];
            for (int x = 0; x < n; x++)
            {
                double dx = x - 6, dy = y - 6;
                double d = Math.Sqrt(dx * dx + dy * dy);
                char ch = d <= 6.2 ? (d > 5.0 ? 'R' : 'W') : '.';
                int sx = x - 2, sy = y - 2;
                if (sx >= 0 && sy >= 0 && sy < symbol.Length && sx < symbol[sy].Length && symbol[sy][sx] == 'X') ch = 'X';
                chars[x] = ch;
            }
            rows[y] = new string(chars);
        }
        return rows;
    }

    // Pattern -> colour grid with a thick outline (P px). Grid size = pattern + 2*P.
    static Color[,] Pad(string[] pat, Func<char, Color> color, int P)
    {
        int h = pat.Length, w = pat[0].Length, W = w + 2 * P, H = h + 2 * P;
        var c = new Color[W, H]; var m = new bool[W, H];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (pat[y][x] != '.') { c[x + P, y + P] = color(pat[y][x]); m[x + P, y + P] = true; }
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                if (m[x, y]) continue;
                bool near = false;
                for (int dy = -P; dy <= P && !near; dy++) for (int dx = -P; dx <= P && !near; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < W && ny < H && m[nx, ny]) near = true;
                    }
                if (near) c[x, y] = Outline;
            }
        return c;
    }

    // Sheet: frames side by side (pattern + 2px outline, drawn x4)
    static void SaveSheet(string[][] frames, Func<char, Color> color, string path)
    {
        const int Scale = 3;
        int Cell = (frames[0][0].Length + 2 * OutlineWidth) * Scale;
        using (var b = new Bitmap(frames.Length * Cell, Cell, PixelFormat.Format32bppArgb))
        {
            for (int f = 0; f < frames.Length; f++)
            {
                Color[,] c = Pad(frames[f], color, OutlineWidth);
                for (int y = 0; y < c.GetLength(1); y++) for (int x = 0; x < c.GetLength(0); x++)
                    {
                        if (c[x, y].A == 0) continue;
                        for (int j = 0; j < Scale; j++) for (int i = 0; i < Scale; i++) b.SetPixel(f * Cell + x * Scale + i, y * Scale + j, c[x, y]);
                    }
            }
            b.Save(path, ImageFormat.Png);
        }
    }
    // Arrow (one-way / やじるし): points right; the game turns it to the direction. A highlight flows along the shaft.
    // N = interior size in pixels (frame = N + 2 outline, x4). 30 gives a 128px frame.
    static string[] ArrowFrame(int step, int N)
    {
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        double cy = (N - 1) / 2.0;
        int x0 = (int)(N * 0.50);                                                          // where the head starts
        double shaft = N * 0.16;                                                           // half thickness of the shaft
        double headHalf = N * 0.62;                                                        // half height of the head base
        for (int x = 0; x <= x0; x++) for (int y = 0; y < N; y++) if (Math.Abs(y - cy) <= shaft) g[y, x] = 'B';
        for (int x = x0; x < N; x++)                                                       // head
        {
            double half = headHalf * (N - 1 - x) / (double)(N - 1 - x0);
            for (int y = 0; y < N; y++) if (Math.Abs(y - cy) <= half) g[y, x] = 'B';
        }
        // yellow body, darker lower half, and light stripes that run forward along the arrow.
        // Stripes repeat every 8px and move 1px per frame, so the 8-frame loop joins up.
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                if (g[y, x] != 'B') continue;
                g[y, x] = y > cy ? 'D' : 'Y';
                if ((((x - step) % 8) + 8) % 8 < 2) g[y, x] = 'S';
            }
var rows = new string[N];
        for (int y = 0; y < N; y++) { var s = new char[N]; for (int x = 0; x < N; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }

    // Flat arrow (points right) with a shadow circle under it. The game rotates it to the direction, and the block's
    // plane squashes it to the iso top face, so the art stays flat. It floats up and down over 8 frames.
    public static string RunArrowSheet(string outDir)
    {
        Directory.CreateDirectory(outDir);
        const int Scale = 3, Cell = 96;
        int[] lifts = { 0, 1, 2, 3, 4, 3, 2, 1 };
        using (var b = new Bitmap(8 * Cell, Cell, PixelFormat.Format32bppArgb))
        {
            for (int f = 0; f < 8; f++)
            {
                int ox = f * Cell, lift = lifts[f];
                double r = 22 - lift * 0.5;
                for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
                    {
                        double dx = x - 48, dy = y - 52;
                        if (dx * dx + dy * dy <= r * r) b.SetPixel(ox + x, y, Color.FromArgb(150, 10, 6, 4));
                    }
                Color[,] c = Pad(ArrowFrame(f, 28), ch => ch == 'S' ? C("FFF8B8") : ch == 'D' ? C("E0930C") : C("FFD21F"), OutlineWidth);
                for (int y = 0; y < c.GetLength(1); y++) for (int x = 0; x < c.GetLength(0); x++)
                    {
                        if (c[x, y].A == 0) continue;
                        for (int j = 0; j < Scale; j++) for (int i = 0; i < Scale; i++)
                            {
                                int py = y * Scale + j - lift;
                                if (py >= 0) b.SetPixel(ox + x * Scale + i, py, c[x, y]);
                            }
                    }
            }
            b.Save(Path.Combine(outDir, "gimmick_arrow.png"), ImageFormat.Png);
        }
        return "ok";
    }
    // One frame pattern -> bitmap (same look as the sheet frames)
    static Bitmap FrameBitmap(string[] pat, Func<char, Color> color)
    {
        const int Scale = 3;
        Color[,] c = Pad(pat, color, OutlineWidth);
        var b = new Bitmap(c.GetLength(0) * Scale, c.GetLength(1) * Scale, PixelFormat.Format32bppArgb);
        for (int y = 0; y < c.GetLength(1); y++) for (int x = 0; x < c.GetLength(0); x++)
            {
                if (c[x, y].A == 0) continue;
                for (int j = 0; j < Scale; j++) for (int i = 0; i < Scale; i++) b.SetPixel(x * Scale + i, y * Scale + j, c[x, y]);
            }
        return b;
    }
    // Four direction arrows on the iso top face. +x = up-right, +y = down-right, -x = down-left, -y (back) = up-left.
    // Each is an 8-frame sheet; the flat arrow is rotated to its direction and squashed to the 2:1 top face.
    public static string RunArrowDirections(string outDir)
    {
        Directory.CreateDirectory(outDir);
        string[] names = { "right", "down", "left", "up" };
        // rotation R (2x2, row-major) that turns the local right-pointing arrow to each grid direction
        float[][] rotations = {
            new float[] { 1, 0, 0, 1 },     // right (+x)
            new float[] { 0, -1, 1, 0 },    // down (+y)
            new float[] { -1, 0, 0, -1 },   // left (-x)
            new float[] { 0, 1, -1, 0 } };  // up (-y, back)
        const int Cell = 96;
        // float height per frame: up 0 -> 12 px and back down, so the 8-frame loop joins smoothly (ping-pong)
        int[] Lift = { 0, 2, 5, 7, 9, 7, 5, 2 };
        for (int d = 0; d < 4; d++)
        {
            using (var sheet = new Bitmap(8 * Cell, Cell, PixelFormat.Format32bppArgb))
            {
                for (int f = 0; f < 8; f++)
                {
                    using (var fb = FrameBitmap(ArrowFrame(f, 28), ch => ch == 'S' ? C("FFF8B8") : ch == 'D' ? C("E0930C") : C("FFD21F")))
                    using (var g = Graphics.FromImage(sheet))
                    {
                        // exact iso map of the flat arrow: grid +x = (k, -k/2), grid +y = (k, k/2) on screen.
                        // The local arrow (pointing right) is turned to the direction first, then mapped.
                        float k = 30f * 2.3f / Cell;
                        float[] rot = rotations[d];                                   // A = M * R, see below
                        float a11 = k * rot[0] + k * rot[2];
                        float a12 = k * rot[1] + k * rot[3];
                        float a21 = -k / 2 * rot[0] + k / 2 * rot[2];
                        float a22 = -k / 2 * rot[1] + k / 2 * rot[3];
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                        // Matrix(m11, m12, m21, m22, dx, dy): x' = m11*x + m21*y + dx, y' = m12*x + m22*y + dy
                        // shadow: a flat circle on the top face (same iso map), shrinks while the arrow is up
                        float cx = f * Cell + Cell / 2f, cy = Cell / 2f;
                        float r = 23f - Lift[f] * 0.4f;
                        using (var shadow = new SolidBrush(Color.FromArgb(190, 16, 10, 6)))
                        {
                            g.Transform = new System.Drawing.Drawing2D.Matrix(a11, a21, a12, a22, cx, cy);
                            g.FillEllipse(shadow, -r, -r, r * 2, r * 2);
                        }
                        // arrow: floats up by Lift[f] (screen up), so the loop goes up and back down
                        g.Transform = new System.Drawing.Drawing2D.Matrix(a11, a21, a12, a22, cx, cy - Lift[f]);
                        g.DrawImage(fb, new RectangleF(-Cell / 2f, -Cell / 2f, Cell, Cell), new RectangleF(0, 0, fb.Width, fb.Height), GraphicsUnit.Pixel);
                        g.ResetTransform();
                    }
                }
                sheet.Save(Path.Combine(outDir, "gimmick_dir_" + names[d] + ".png"), ImageFormat.Png);
            }
        }
        return "ok";
    }

    // Treasure chest (2D, 64px frames, 8 frames). The chest floats up and down (ping-pong) over a flat shadow.
    // Chest pattern is 28x28 + 2px outline = 32 grid cells, drawn x2 = 64px.
    static string[] ChestFrame(int step, int lift)
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        int top = 4 - lift;                                                                 // chest is 30% bigger: 26 wide, 21 tall
        for (int x = 1; x <= 26; x++) for (int y = top; y <= top + 6; y++)                  // lid
        {
            if (y == top && (x < 4 || x > 23)) continue;
            g[y, x] = 'L';
        }
        for (int x = 1; x <= 26; x++) for (int y = top + 7; y <= top + 19; y++) g[y, x] = 'B';  // body
        for (int x = 0; x <= 27; x++) g[top + 20, x] = 'R';                                  // rim
        for (int x = 1; x <= 26; x++) g[top + 7, x] = 'M';                                   // seam band
        for (int y = top; y <= top + 20; y++) for (int x = 12; x <= 14; x++) g[y, x] = 'M';  // strap
        for (int y = top + 7; y <= top + 11; y++) for (int x = 11; x <= 15; x++) g[y, x] = 'G';  // lock
        for (int x = 1; x <= 26; x++) for (int y = top + 1; y <= top + 2; y++)               // shine moving forward
            if (g[y, x] == 'L' && (((x - step) % 8) + 8) % 8 < 1) g[y, x] = 'S';
        var rows = new string[N];
        for (int y = 0; y < N; y++) { var s = new char[N]; for (int x = 0; x < N; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }
    static void SaveChestSheet(Func<char, Color> color, string path)
    {
        const int Scale = 3, Cell = 96;                                                       // 96px frames
        int[] lifts = { 0, 1, 2, 3, 4, 3, 2, 1 };
        using (var b = new Bitmap(8 * Cell, Cell, PixelFormat.Format32bppArgb))
        {
            for (int f = 0; f < 8; f++)
            {
                int ox = f * Cell, lift = lifts[f];
                // shadow on the ground: flat ellipse, smaller while the chest is up
                double rx = 20 - lift * 0.9, ry = 6 - lift * 0.4;
                for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
                    {
                        double dx = (x - 48) / rx, dy = (y - 86) / ry;
                        if (dx * dx + dy * dy <= 1.0) b.SetPixel(ox + x, y, Color.FromArgb(150, 10, 6, 4));
                    }
                Color[,] c = Pad(ChestFrame(f, lift), color, OutlineWidth);
                for (int y = 0; y < c.GetLength(1); y++) for (int x = 0; x < c.GetLength(0); x++)
                    {
                        if (c[x, y].A == 0) continue;
                        for (int j = 0; j < Scale; j++) for (int i = 0; i < Scale; i++) b.SetPixel(ox + x * Scale + i, y * Scale + j, c[x, y]);
                    }
            }
            b.Save(path, ImageFormat.Png);
        }
    }
    public static string RunTreasureSheets(string outDir)
    {
        Directory.CreateDirectory(outDir);
        // 1 wood, 2 silver, 3 gold
        SaveChestSheet(ch => ch == 'L' ? C("E8A957") : ch == 'B' ? C("B8702E") : ch == 'R' ? C("6A4018") : ch == 'M' ? C("9AA3B5") : ch == 'G' ? C("FFD84A") : ch == 'S' ? C("FFF0C0") : C("E8A957"), Path.Combine(outDir, "gimmick_treasure1.png"));
        SaveChestSheet(ch => ch == 'L' ? C("E6EBF5") : ch == 'B' ? C("B6C0D2") : ch == 'R' ? C("6B7689") : ch == 'M' ? C("4A5568") : ch == 'G' ? C("FFD84A") : ch == 'S' ? C("FFFFFF") : C("E6EBF5"), Path.Combine(outDir, "gimmick_treasure2.png"));
        SaveChestSheet(ch => ch == 'L' ? C("FFE15A") : ch == 'B' ? C("F5B325") : ch == 'R' ? C("A86A10") : ch == 'M' ? C("7A4A10") : ch == 'G' ? C("FFF6B0") : ch == 'S' ? C("FFFFFF") : C("FFE15A"), Path.Combine(outDir, "gimmick_treasure3.png"));
        return "ok";
    }

    // Turn sign: an L-shaped arrow (up, then turns right). Stripes run forward along the sign.
    static string[] TurnFrame(int step)
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        for (int y = 8; y <= 24; y++) for (int x = 8; x <= 12; x++) g[y, x] = 'Y';           // vertical part
        for (int y = 8; y <= 12; y++) for (int x = 8; x <= 19; x++) g[y, x] = 'Y';           // horizontal part
        for (int x = 18; x <= 26; x++)                                                        // head pointing right
        {
            double half = (26 - x) * 0.9;
            for (int y = 0; y < N; y++) if (Math.Abs(y - 10) <= half) g[y, x] = 'Y';
        }
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                if (g[y, x] != 'Y') continue;
                bool vertical = x <= 12 && y >= 8 && y <= 24;
                int p = vertical ? 24 - y : 16 + (x - 8);
                if (((((p - step) % 8) + 8) % 8) < 2) g[y, x] = 'S';
                else if ((vertical && x >= 11) || (!vertical && y >= 11)) g[y, x] = 'D';
            }
        var rows = new string[N];
        for (int y = 0; y < N; y++) { var s = new char[N]; for (int x = 0; x < N; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }

    // Two-way straight sign: a bar with arrowheads at both ends (no fixed direction). Stripes run along the bar.
    static string[] StraightFrame(int step)
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        for (int y = 11; y <= 15; y++) for (int x = 8; x <= 19; x++) g[y, x] = 'Y';          // bar
        for (int x = 1; x <= 9; x++)                                                          // left head (tip at x 1)
        {
            double half = (x - 1) * 0.9;
            for (int y = 0; y < N; y++) if (Math.Abs(y - 13) <= half) g[y, x] = 'Y';
        }
        for (int x = 18; x <= 26; x++)                                                        // right head (tip at x 26)
        {
            double half = (26 - x) * 0.9;
            for (int y = 0; y < N; y++) if (Math.Abs(y - 13) <= half) g[y, x] = 'Y';
        }
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                if (g[y, x] != 'Y') continue;
                if (((((x - step) % 8) + 8) % 8) < 2) g[y, x] = 'S';
                else if (y >= 14) g[y, x] = 'D';
            }
        var rows = new string[N];
        for (int y = 0; y < N; y++) { var s = new char[N]; for (int x = 0; x < N; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }

    // Upright 2D sign with a flat elliptical shadow under it. The sign floats up and down (ping-pong). 96px frames.
    static void SaveUprightSheet(Func<int, string[]> frameOf, Func<char, Color> colour, string path)
    {
        const int Scale = 3, Cell = 96;
        int[] lifts = { 0, 1, 2, 3, 4, 3, 2, 1 };
        using (var b = new Bitmap(8 * Cell, Cell, PixelFormat.Format32bppArgb))
        {
            for (int f = 0; f < 8; f++)
            {
                int ox = f * Cell, lift = lifts[f];
                double rx = 20 - lift * 0.9, ry = 6 - lift * 0.4;
                for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
                    {
                        double dx = (x - 48) / rx, dy = (y - 86) / ry;
                        if (dx * dx + dy * dy <= 1.0) b.SetPixel(ox + x, y, Color.FromArgb(150, 10, 6, 4));
                    }
                Color[,] c = Pad(frameOf(f), colour, OutlineWidth);
                for (int y = 0; y < c.GetLength(1); y++) for (int x = 0; x < c.GetLength(0); x++)
                    {
                        if (c[x, y].A == 0) continue;
                        for (int j = 0; j < Scale; j++) for (int i = 0; i < Scale; i++)
                            {
                                int py = y * Scale + j - lift;
                                if (py >= 0) b.SetPixel(ox + x * Scale + i, py, c[x, y]);
                            }
                    }
            }
            b.Save(path, ImageFormat.Png);
        }
    }
    // Flat sign with a shadow circle, floating up and down (same as the arrow sheet). 96px frames.
    static void SaveFloatSheet(Func<int, string[]> frameOf, Func<char, Color> colour, string path)
    {
        const int Scale = 3, Cell = 96;
        int[] lifts = { 0, 1, 2, 3, 4, 3, 2, 1 };
        using (var b = new Bitmap(8 * Cell, Cell, PixelFormat.Format32bppArgb))
        {
            for (int f = 0; f < 8; f++)
            {
                int ox = f * Cell, lift = lifts[f];
                double r = 22 - lift * 0.5;
                for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
                    {
                        double dx = x - 48, dy = y - 52;
                        if (dx * dx + dy * dy <= r * r) b.SetPixel(ox + x, y, Color.FromArgb(150, 10, 6, 4));
                    }
                Color[,] c = Pad(frameOf(f), colour, OutlineWidth);
                for (int y = 0; y < c.GetLength(1); y++) for (int x = 0; x < c.GetLength(0); x++)
                    {
                        if (c[x, y].A == 0) continue;
                        for (int j = 0; j < Scale; j++) for (int i = 0; i < Scale; i++)
                            {
                                int py = y * Scale + j - lift;
                                if (py >= 0) b.SetPixel(ox + x * Scale + i, py, c[x, y]);
                            }
                    }
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    public static string RunSignSheets(string outDir)
    {
        Directory.CreateDirectory(outDir);
        // 2D (upright) signs: turn = pink L-arrow, straight = cyan two-way arrow
        SaveUprightSheet(f => TurnFrame(f), ch => ch == 'S' ? C("FFE0F0") : ch == 'D' ? C("C8337F") : C("FF6FB0"), Path.Combine(outDir, "gimmick_turn.png"));
        SaveUprightSheet(f => StraightFrame(f), ch => ch == 'S' ? C("E6FBFF") : ch == 'D' ? C("1A8FC0") : C("3CD2FF"), Path.Combine(outDir, "gimmick_straight.png"));
        return "ok";
    }
    // One-way ladder, flat on the top face (pointing right; the game rotates it). Two rails along the allowed direction
    // and rungs across them. A light runs along the rungs in the allowed direction (8 frames, loops).
    // One-way ladder, flat on the top face (pointing right; the game rotates it). Smaller than the block:
    // two rails along the allowed direction and three rungs across them. The rungs light up in turn, moving forward.
    // One-way gate, flat on the top face (pointing right; the game rotates it). A bar across the path with a post at
    // each end, and white chevrons that move through the gate in the allowed direction (8 frames, loops).
    static string[] OneWayFrame(int step)
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        for (int y = 4; y <= 23; y++) for (int x = 13; x <= 14; x++) g[y, x] = 'B';          // gate bar
        for (int y = 2; y <= 5; y++) for (int x = 11; x <= 16; x++) g[y, x] = 'P';             // posts
        for (int y = 22; y <= 25; y++) for (int x = 11; x <= 16; x++) g[y, x] = 'P';
        int cx = (step * 3) % 24 + 2;                                                          // chevron tip moves right
        for (int k = 0; k <= 5; k++)
        {
            int x = cx - k;
            if (x < 0 || x >= N) continue;
            foreach (int y in new[] { 13 - k, 14 + k })
            {
                if (y < 0 || y >= N) continue;
                g[y, x] = 'W';
            }
        }
        var rows = new string[N];
        for (int y = 0; y < N; y++) { var s = new char[N]; for (int x = 0; x < N; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }
    public static string RunOneWaySheet(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var frames = new string[8][];
        for (int f = 0; f < 8; f++) frames[f] = OneWayFrame(f);
        SaveSheet(frames, ch => ch == 'W' ? C("FFFFFF") : ch == 'P' ? C("1F6FC0") : C("2EA8FF"), Path.Combine(outDir, "gimmick_oneway.png"));
        return "ok";
    }
    // Warp portal, upright 2D: a swirl that turns over 8 frames (loops). Warp out has a dark X on it.
    // Drawn white; the game tints it with the warp colour.
    static string[] WarpFrame(int step, bool withX)
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                g[y, x] = '.';
                double dx = x - 13.5, dy = y - 13.5;
                double r = Math.Sqrt(dx * dx + dy * dy);
                if (r > 12.2) continue;
                if (r < 3.5) { g[y, x] = 'H'; continue; }
                double th = Math.Atan2(dy, dx);
                double tt = th / (2 * Math.PI) * 2 + r / 9.0 - step / 4.0;                   // two arms, turning
                double f = tt - Math.Floor(tt);
                g[y, x] = f < 0.35 ? 'W' : 'L';
            }
        if (withX)
        {
            for (int i = 8; i <= 19; i++)
                for (int d = 0; d <= 1; d++)
                {
                    g[i, i + d] = 'X';
                    g[i, 27 - i + d] = 'X';
                }
        }
        var rows = new string[N];
        for (int y = 0; y < N; y++) { var s = new char[N]; for (int x = 0; x < N; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }

    public static string RunWarpSheets(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var inFrames = new string[8][];
        var outFrames = new string[8][];
        for (int f = 0; f < 8; f++) { inFrames[f] = WarpFrame(f, false); outFrames[f] = WarpFrame(f, true); }
        Func<char, Color> colour = ch => ch == 'W' ? C("FFFFFF") : ch == 'L' ? C("C9D1DC") : ch == 'H' ? C("14141C") : C("2A1E14");
        SaveSheet(inFrames, colour, Path.Combine(outDir, "gimmick_warp_in.png"));
        SaveSheet(outFrames, colour, Path.Combine(outDir, "gimmick_warp_out.png"));
        return "ok";
    }
    static string[] toRows(char[,] g, int n)
    {
        var rows = new string[n];
        for (int y = 0; y < n; y++) { var s = new char[n]; for (int x = 0; x < n; x++) s[x] = g[y, x]; rows[y] = new string(s); }
        return rows;
    }

    // 2D カギ（立った絵）: 輪・軸・歯。白で描いて、ゲームでカギの色を付ける
    static string[] KeyFrame()
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                g[y, x] = '.';
                double dx = x + 0.5 - 8.5, dy = y + 0.5 - 9.5;
                double r = Math.Sqrt(dx * dx + dy * dy);
                if (r <= 3.2) g[y, x] = 'H';
                else if (r <= 7.0) g[y, x] = dx + dy > 0 ? 'L' : 'W';
            }
        for (int y = 8; y <= 10; y++) for (int x = 14; x <= 25; x++) g[y, x] = 'W';
        for (int y = 11; y <= 13; y++) { g[y, 21] = 'W'; g[y, 22] = 'W'; }
        for (int y = 11; y <= 12; y++) { g[y, 24] = 'L'; g[y, 25] = 'L'; }
        return toRows(g, N);
    }

    // 2D の扉（ブロックと同じ大きさの立方体。横に柵の線）。白で描いて、ゲームで扉の色を付ける
    static string[] GateFrame()
    {
        const int N = 28, H = 10;
        const double cx = 13.5, half = 14.0, halfY = 7.0, baseY = 20.0;
        double topY = baseY - H;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                g[y, x] = '.';
                double px = x + 0.5, py = y + 0.5;
                if (Math.Abs(px - cx) / half + Math.Abs(py - topY) / halfY <= 1) { g[y, x] = 'W'; continue; }
                bool inSide = false;
                for (int t = 0; t <= H && !inSide; t++)
                {
                    if (Math.Abs(px - cx) / half + Math.Abs(py - (baseY - t)) / halfY <= 1) inSide = true;
                }
                if (!inSide) continue;
                if (px < cx) g[y, x] = (x == 4 || x == 9) ? 'B' : 'L';
                else g[y, x] = (x == 18 || x == 23) ? 'B' : 'D';
            }
        return toRows(g, N);
    }

    public static string RunKeyGateSheets(string outDir)
    {
        Directory.CreateDirectory(outDir);
        Func<char, Color> keyColour = ch => ch == 'W' ? C("FFFFFF") : ch == 'L' ? C("B8C0CC") : C("1A1A22");
        Func<char, Color> gateColour = ch => ch == 'W' ? C("FFFFFF") : ch == 'L' ? C("C4C4C4") : ch == 'D' ? C("8E8E8E") : C("5A5A5A");
        SaveFloatSheet(_ => KeyFrame(), keyColour, Path.Combine(outDir, "gimmick_key.png"));
        SaveSheet(new[] { GateFrame() }, gateColour, Path.Combine(outDir, "gimmick_gate.png"));
        return "ok";
    }
    // 2D のハンマー（壊れる橋）: 頭と柄。白で描いて、ゲームで色は付けない（浮かぶ・影はアロー同じ）
    static string[] HammerFrame()
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        for (int y = 3; y <= 10; y++) for (int x = 4; x <= 22; x++) g[y, x] = y <= 6 ? 'W' : 'L';
        for (int y = 11; y <= 26; y++) for (int x = 12; x <= 15; x++) g[y, x] = 'D';
        return toRows(g, N);
    }

    // 雲の霧（上面を覆う）: 白い雲の塊。浮かぶ・影はアロー同じ
    static string[] FogFrame()
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                g[y, x] = '.';
                double px = x + 0.5, py = y + 0.5;
                bool inside = (px - 9.5) * (px - 9.5) + (py - 13.5) * (py - 13.5) <= 4.5 * 4.5
                    || (px - 15.5) * (px - 15.5) + (py - 11.5) * (py - 11.5) <= 6.0 * 6.0
                    || (px - 20.5) * (px - 20.5) + (py - 14.5) * (py - 14.5) <= 4.5 * 4.5
                    || (px >= 4 && px <= 24 && py >= 13 && py <= 18);
                if (inside) g[y, x] = py <= 12 ? 'W' : 'L';
            }
        return toRows(g, N);
    }

    public static string RunHammerFogSheets(string outDir)
    {
        Directory.CreateDirectory(outDir);
        Func<char, Color> hammerColour = ch => ch == 'W' ? C("FFFFFF") : ch == 'L' ? C("B8C0CC") : C("7A8494");
        Func<char, Color> fogColour = ch => ch == 'W' ? C("FFFFFF") : C("D0D6DE");
        SaveFloatSheet(_ => HammerFrame(), hammerColour, Path.Combine(outDir, "gimmick_crumble.png"));
        SaveFloatSheet(_ => FogFrame(), fogColour, Path.Combine(outDir, "gimmick_fog.png"));
        return "ok";
    }
    // スイッチ（2D・浮かぶ）: 台とレバー。白で描いて、ゲームで色を付ける
    static string[] SwitchFrame()
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        for (int y = 21; y <= 24; y++) for (int x = 7; x <= 20; x++) g[y, x] = y <= 21 ? 'L' : 'D';
        for (double s = 0; s <= 1.0; s += 0.02)
        {
            double px = 12.5 + 8.0 * s, py = 21.5 - 16.0 * s;
            int ix = (int)px, iy = (int)py;
            g[iy, ix] = 'W'; g[iy, ix + 1] = 'W';
        }
        for (int y = 2; y <= 8; y++) for (int x = 17; x <= 24; x++)
            {
                double dx = x + 0.5 - 20.5, dy = y + 0.5 - 5.5;
                if (dx * dx + dy * dy <= 3.8 * 3.8) g[y, x] = (dx + dy > 0) ? 'L' : 'W';
            }
        return toRows(g, N);
    }

    // 扉（2D・浮かぶ）: 格子の門（縁と縦横の桟）。白で描いて、ゲームで扉の色を付ける
    static string[] GateGrilleFrame()
    {
        const int N = 28;
        var g = new char[N, N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = '.';
        for (int y = 3; y <= 24; y++) for (int x = 2; x <= 25; x++)
            {
                bool border = x <= 3 || x >= 24 || y <= 4 || y >= 23;
                bool vbar = x == 7 || x == 12 || x == 16 || x == 20;
                bool hbar = y == 10 || y == 16;
                if (border) g[y, x] = 'W';
                else if (vbar || hbar) g[y, x] = 'L';
            }
        return toRows(g, N);
    }

    public static string RunSwitchGateSheets(string outDir)
    {
        Directory.CreateDirectory(outDir);
        Func<char, Color> switchColour = ch => ch == 'W' ? C("FFFFFF") : ch == 'L' ? C("C9D1DC") : C("8A94A6");
        Func<char, Color> gateColour = ch => ch == 'W' ? C("FFFFFF") : C("C4C4C4");
        SaveFloatSheet(_ => SwitchFrame(), switchColour, Path.Combine(outDir, "gimmick_switch.png"));
        SaveFloatSheet(_ => GateGrilleFrame(), gateColour, Path.Combine(outDir, "gimmick_gate.png"));
        return "ok";
    }
    public static string Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        string[] arrow = {
            "......YY....",
            "......YYY...",
            "YYYYYYYYYY..",
            "YLLLLLLLYYY.",
            "YYYYYYYYYY..",
            "......YYY...",
            "......YY...." };
        Save(arrow, ch => ch == 'L' ? C("FFFFFF") : C("FFE8A0"), Path.Combine(outDir, "gimmick_arrow.png"));

        // corner arrow: go up then turn right
        string[] turn = Sign(new[] {
            ".....X...",
            ".....XX..",
            "..XXXXXX.",
            "..X..XX..",
            "..X..X...",
            "..X......",
            "..X......",
            "..X......",
            "........." });
        Save(turn, ch => ch == 'R' ? C("F08C3A") : ch == 'X' ? C("4A3A34") : C("FFF4E0"), Path.Combine(outDir, "gimmick_turn.png"));

        // straight: arrows on both ends of a straight bar
        string[] straight = Sign(new[] {
            "....X....",
            "...XXX...",
            "..XXXXX..",
            "....X....",
            "....X....",
            "....X....",
            "..XXXXX..",
            "...XXX...",
            "....X...." });
        Save(straight, ch => ch == 'R' ? C("4AA8E0") : ch == 'X' ? C("4A3A34") : C("FFF4E0"), Path.Combine(outDir, "gimmick_straight.png"));

        string[] bone = {
            ".WW.......WW.",
            "WWWW.....WWWW",
            "WLWWWWWWWWWWW",
            ".WWLLLLLLLWW.",
            "WWWWWWWWWWWWW",
            "WWWW.....WWWW",
            ".WW.......WW." };
        Save(bone, ch => ch == 'L' ? C("FFFFFF") : C("F4E8D0"), Path.Combine(outDir, "gimmick_bone.png"));
        // tinted in game (white body + dark outline): key, gate fence, warp holes
        string[] key = {
            ".WWW.......",
            "WW.WW......",
            "W...WWWWWWW",
            "WW.WW..W.W.",
            ".WWW...W.W." };
        Save(key, ch => C("FFFFFF"), Path.Combine(outDir, "gimmick_key.png"));

        string[] gate = {
            ".W..W..W..W.",
            "WWWWWWWWWWWW",
            ".W..W..W..W.",
            ".W..W..W..W.",
            "WWWWWWWWWWWW",
            ".W..W..W..W.",
            ".W..W..W..W." };
        Save(gate, ch => C("FFFFFF"), Path.Combine(outDir, "gimmick_gate.png"));

        // entrance: dark hole with a white ring (tinted ring color)
        string[] warpIn = {
            "....WWWWW....",
            "..WWDDDDDWW..",
            ".WDDDSDDDDDW.",
            "WDDDSDDDSDDDW",
            "WDDSDDDDDSDDW",
            "WDDDSDDDSDDDW",
            ".WDDDDDSDDDW.",
            "..WWDDDDDWW..",
            "....WWWWW...." };
        Save(warpIn, ch => ch == 'D' ? C("2A1E3A") : ch == 'S' ? C("8A7AB0") : C("FFFFFF"), Path.Combine(outDir, "gimmick_warp_in.png"));

        // exit: light hole with up arrows
        string[] warpOut = {
            "....WWWWW....",
            "..WWLLLLLWW..",
            ".WLLLLALLLLW.",
            "WLLLLAAALLLLW",
            "WLLLAAAAALLLW",
            "WLLLLLALLLLLW",
            ".WLLLLALLLLW.",
            "..WWLLLLLWW..",
            "....WWWWW...." };
        Save(warpOut, ch => ch == 'L' ? C("F4F4FF") : ch == 'A' ? C("A0A0C0") : C("FFFFFF"), Path.Combine(outDir, "gimmick_warp_out.png"));

        // treasure chests: 1 wood, 2 silver, 3 gold
        string[] chest = {
            "..BBBBBBBBBB..",
            ".BLLLLLLLLLLB.",
            "BLMMMMMMMMMMLB",
            "BMMMMMMMMMMMMB",
            "BKKKKKGGKKKKKB",
            "BMMMMMGGMMMMMB",
            "BMMMMMMMMMMMMB",
            "BDDDDDDDDDDDDB",
            ".BBBBBBBBBBBB." };
        string[][] chestColors = {
            new[] { "C88A50", "E0B080", "8A5A30", "6A4A3A", "FFD84A" },
            new[] { "B8C0D0", "E8ECF4", "8890A8", "5A6278", "FFFFFF" },
            new[] { "FFC830", "FFF0A0", "D89A10", "B87800", "FF5A8C" } };
        for (int i = 0; i < 3; i++)
        {
            string[] col = chestColors[i];
            Save(chest, ch => ch == 'L' ? C(col[1]) : ch == 'D' ? C(col[2]) : ch == 'B' ? C(col[2]) : ch == 'K' ? C(col[3]) : ch == 'G' ? C(col[4]) : C(col[0]), Path.Combine(outDir, "gimmick_treasure" + (i + 1) + ".png"));
        }

        // crumbling plank (cracks)
        string[] crumble = {
            "PPPPPPPPPPPP",
            "PLLLLKLLLLLP",
            "PPPPPKPPPPPP",
            "PPPPKPPPPPPP",
            "PDDDKDDDKDDP",
            "PPPPPPPPKPPP",
            "PLLLLLLLKLLP",
            "PPPPPPPPPPPP" };
        Save(crumble, ch => ch == 'L' ? C("D8A868") : ch == 'D' ? C("8A5A30") : ch == 'K' ? C("3A2A1E") : C("B88048"), Path.Combine(outDir, "gimmick_crumble.png"));
        return "ok";
    }
}
