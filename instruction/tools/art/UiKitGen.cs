// UI kit (C# 5 / System.Drawing): frames, buttons, tabs, banners, bubbles, lines, bars in 8 colors,
// plus white icons, badges and toggles. Drawn small and scaled x4. Writes kit_border.txt (name -> 9-slice border l,b,r,t in px).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class UiKitGen
{
    const int S = 4;
    static Color C(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    static readonly Color Outline = C("3A2A1E");
    static readonly Color White = C("FFFFFF");
    static readonly Color Red = C("F04848"), RedDark = C("B82828"), Green = C("4CC860");

    // name, mid, light, dark
    static readonly string[][] Palette =
    {
        new[] { "cream", "FFF4DC", "FFFFFF", "E8C890" },
        new[] { "pink", "FF9CC0", "FFD0E0", "E06890" },
        new[] { "mint", "8EDCB8", "C8F4DC", "4EAA84" },
        new[] { "sky", "8CC8F0", "C8E8FF", "4A90C8" },
        new[] { "lemon", "FFE070", "FFF4B0", "E0B030" },
        new[] { "lavender", "C0A8F0", "E4D8FF", "8A70C8" },
        new[] { "brown", "C88A50", "E0B080", "8A5A30" },
        new[] { "gray", "C8C8D0", "F0F0F4", "8A8A96" },
    };

    class Canvas
    {
        public int W, H; public Color[,] c; public bool[,] m;
        public Canvas(int w, int h) { W = w; H = h; c = new Color[w, h]; m = new bool[w, h]; }
        public void P(int x, int y, Color col) { if (x >= 0 && y >= 0 && x < W && y < H) { c[x, y] = col; m[x, y] = true; } }
        public void Clear(int x, int y) { if (x >= 0 && y >= 0 && x < W && y < H) { c[x, y] = Color.Empty; m[x, y] = false; } }
        public void Outline(Color col)
        {
            var add = new bool[W, H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    if (m[x, y]) continue;
                    if ((x + 1 < W && m[x + 1, y]) || (x > 0 && m[x - 1, y]) || (y + 1 < H && m[x, y + 1]) || (y > 0 && m[x, y - 1])) add[x, y] = true;
                }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (add[x, y]) { c[x, y] = col; m[x, y] = true; }
        }
        public void Save(string path)
        {
            using (var b = new Bitmap(W * S, H * S, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                    {
                        if (c[x, y].A == 0) continue;
                        for (int j = 0; j < S; j++) for (int i = 0; i < S; i++) b.SetPixel(x * S + i, y * S + j, c[x, y]);
                    }
                b.Save(path, ImageFormat.Png);
            }
        }
    }

    // inside a rounded rect with 1px margin for the outline
    static bool InRound(int x, int y, int w, int h, int r)
    {
        int x0 = 1, y0 = 1, x1 = w - 2, y1 = h - 2;
        if (x < x0 || y < y0 || x > x1 || y > y1) return false;
        int cx = x < x0 + r ? x0 + r : (x > x1 - r ? x1 - r : x);
        int cy = y < y0 + r ? y0 + r : (y > y1 - r ? y1 - r : y);
        double dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r + 0.5;
    }

    static readonly Dictionary<string, int[]> borders = new Dictionary<string, int[]>();
    static string outDir;

    static void Save(Canvas k, string name, int l, int b, int r, int t)
    {
        k.Save(Path.Combine(outDir, name + ".png"));
        if (l + b + r + t > 0) borders[name] = new[] { l * S, b * S, r * S, t * S };
    }

    static Canvas Frame(Color mid, Color light, Color dark)
    {
        int n = 24; var k = new Canvas(n, n);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                if (!InRound(x, y, n, n, 3)) continue;
                bool inner = InRound(x - 2, y - 2, n - 4, n - 4, 2) && !InRound(x - 3, y - 3, n - 6, n - 6, 1);
                k.P(x, y, inner ? dark : (y <= 2 ? light : mid));
            }
        k.Outline(Outline);
        return k;
    }

    static Canvas ThinFrame(Color mid, Color dark)
    {
        int n = 16; var k = new Canvas(n, n);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                if (InRound(x, y, n, n, 3) && !InRound(x - 2, y - 2, n - 4, n - 4, 2)) k.P(x, y, y >= n - 4 ? dark : mid);
        k.Outline(Outline);
        // keep the hole transparent (outline also draws inside)
        return k;
    }

    static Canvas Button(Color mid, Color light, Color dark)
    {
        int w = 24, h = 12; var k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!InRound(x, y, w, h, 3)) continue;
                k.P(x, y, y <= 3 ? light : (y >= h - 4 ? dark : mid));
            }
        for (int x = 4; x < w - 4; x++) k.P(x, 2, White);
        k.Outline(Outline);
        return k;
    }

    static Canvas Tab(Color mid, Color light)
    {
        int w = 20, h = 12; var k = new Canvas(w, h);
        for (int y = 1; y < h; y++) for (int x = 1; x < w - 1; x++)
            {
                // rounded top corners only
                if (y < 4 && ((x < 4 && (x - 4) * (x - 4) + (y - 4) * (y - 4) > 9) || (x > w - 5 && (x - (w - 5)) * (x - (w - 5)) + (y - 4) * (y - 4) > 9))) continue;
                k.P(x, y, y <= 3 ? light : mid);
            }
        k.Outline(Outline);
        return k;
    }

    static Canvas Banner(Color mid, Color light, Color dark)
    {
        int w = 40, h = 13; var k = new Canvas(w, h);
        // tails behind
        for (int y = 4; y <= 11; y++)
        {
            for (int x = 1; x <= 6; x++) k.P(x, y, dark);
            for (int x = w - 7; x <= w - 2; x++) k.P(x, y, dark);
        }
        // V notches
        int[][] notch = { new[] { 6, 1 }, new[] { 7, 2 }, new[] { 8, 2 }, new[] { 9, 1 } };
        foreach (int[] n in notch) for (int i = 0; i < n[1]; i++) { k.Clear(1 + i, n[0]); k.Clear(w - 2 - i, n[0]); }
        // main ribbon
        for (int y = 1; y <= 9; y++) for (int x = 5; x <= w - 6; x++) k.P(x, y, y <= 2 ? light : (y >= 8 ? dark : mid));
        k.Outline(Outline);
        return k;
    }

    static Canvas Bubble(Color mid, Color light)
    {
        int w = 24, h = 24; var k = new Canvas(w, h);
        for (int y = 0; y < 19; y++) for (int x = 0; x < w; x++) if (InRound(x, y, w, 19, 3)) k.P(x, y, y <= 2 ? light : mid);
        for (int y = 18; y <= 21; y++) for (int x = 5; x <= 9 - (y - 18); x++) k.P(x, y, mid);
        k.Outline(Outline);
        return k;
    }

    static Canvas Line(Color mid, Color light, Color dark)
    {
        var k = new Canvas(16, 3);
        for (int x = 0; x < 16; x++) { k.P(x, 0, light); k.P(x, 1, mid); k.P(x, 2, dark); }
        return k;
    }

    static Canvas Dotted(Color dark)
    {
        var k = new Canvas(16, 2);
        for (int x = 0; x < 16; x++) if ((x / 2) % 2 == 0) { k.P(x, 0, dark); k.P(x, 1, dark); }
        return k;
    }

    static Canvas BarFill(Color mid, Color light, Color dark)
    {
        int w = 16, h = 8; var k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (InRound(x, y, w, h, 2)) k.P(x, y, y <= 2 ? light : (y >= h - 3 ? dark : mid));
        for (int x = 3; x < w - 3; x++) k.P(x, 2, White);
        return k;
    }

    static Canvas BarBack()
    {
        int w = 24, h = 10; var k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (InRound(x, y, w, h, 3)) k.P(x, y, y <= 2 ? C("4A3226") : C("6A4A36"));
        k.Outline(Outline);
        return k;
    }

    static Canvas FromPattern(string[] pat, Func<char, Color> color, bool outline)
    {
        int h = pat.Length, w = pat[0].Length;
        var k = new Canvas(w + 2, h + 2);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (pat[y][x] != '.') k.P(x + 1, y + 1, color(pat[y][x]));
        if (outline) k.Outline(Outline);
        return k;
    }

    static readonly Dictionary<string, string[]> Icons = new Dictionary<string, string[]>
    {
        { "check", new[] { "..........", ".........X", "........XX", ".......XX.", "X.....XX..", "XX...XX...", ".XX.XX....", "..XXX.....", "...X......", ".........." } },
        { "cross", new[] { "XX......XX", "XXX....XXX", ".XXX..XXX.", "..XXXXXX..", "...XXXX...", "...XXXX...", "..XXXXXX..", ".XXX..XXX.", "XXX....XXX", "XX......XX" } },
        { "lock", new[] { "...XXXX...", "..X....X..", "..X....X..", "..X....X..", ".XXXXXXXX.", ".XXXXXXXX.", ".XXX..XXX.", ".XXX..XXX.", ".XXXXXXXX.", ".XXXXXXXX." } },
        { "question", new[] { "..XXXXX...", ".XX...XX..", ".XX...XX..", "......XX..", ".....XX...", "....XX....", "....XX....", "..........", "....XX....", "....XX...." } },
        { "info", new[] { "....XX....", "....XX....", "..........", "...XXX....", "....XX....", "....XX....", "....XX....", "....XX....", "...XXXX...", "...XXXX..." } },
        { "plus", new[] { "..........", "....XX....", "....XX....", "....XX....", ".XXXXXXXX.", ".XXXXXXXX.", "....XX....", "....XX....", "....XX....", ".........." } },
        { "minus", new[] { "..........", "..........", "..........", "..........", ".XXXXXXXX.", ".XXXXXXXX.", "..........", "..........", "..........", ".........." } },
        { "arrow_right", new[] { "....X.....", "....XX....", "....XXX...", "XXXXXXXX..", "XXXXXXXXX.", "XXXXXXXX..", "....XXX...", "....XX....", "....X.....", ".........." } },
        { "heart", new[] { ".XX....XX.", "XXXX..XXXX", "XXXXXXXXXX", "XXXXXXXXXX", "XXXXXXXXXX", ".XXXXXXXX.", "..XXXXXX..", "...XXXX...", "....XX....", ".........." } },
        { "star", new[] { "....XX....", "....XX....", "...XXXX...", "XXXXXXXXXX", ".XXXXXXXX.", "..XXXXXX..", "..XXXXXX..", ".XXX..XXX.", ".XX....XX.", ".........." } },
        { "exclamation", new[] { "....XX....", "...XXXX...", "...XXXX...", "...XXXX...", "....XX....", "....XX....", "..........", "....XX....", "...XXXX...", "....XX...." } },
        { "gift", new[] { "..XX..XX..", "...XXXX...", "XXXXXXXXXX", "XXXXXXXXXX", "....XX....", ".XXXXXXXX.", ".XXXXXXXX.", ".XXXXXXXX.", ".XXXXXXXX.", ".XXXXXXXX." } },
        { "bell", new[] { "....XX....", "...XXXX...", "..XXXXXX..", "..XXXXXX..", "..XXXXXX..", ".XXXXXXXX.", "XXXXXXXXXX", "..........", "....XX....", ".........." } },
        { "pause", new[] { "..........", ".XXX..XXX.", ".XXX..XXX.", ".XXX..XXX.", ".XXX..XXX.", ".XXX..XXX.", ".XXX..XXX.", ".XXX..XXX.", ".XXX..XXX.", ".........." } },
        { "play", new[] { ".XX.......", ".XXXX.....", ".XXXXXX...", ".XXXXXXXX.", ".XXXXXXXXX", ".XXXXXXXX.", ".XXXXXX...", ".XXXX.....", ".XX.......", ".........." } },
        { "trash", new[] { "...XXXX...", "XXXXXXXXXX", "..........", ".XXXXXXXX.", ".X.X..X.X.", ".X.X..X.X.", ".X.X..X.X.", ".X.X..X.X.", ".XXXXXXXX.", ".........." } },
    };

    static string[] Rotate(string[] pat)
    {
        int n = pat.Length; var rows = new string[n];
        for (int y = 0; y < n; y++) { var ch = new char[n]; for (int x = 0; x < n; x++) ch[x] = pat[n - 1 - x][y]; rows[y] = new string(ch); }
        return rows;
    }

    // 5-row pixel letters for the NEW badge (width varies)
    static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
    {
        { 'N', new[] { "X..X", "XX.X", "X.XX", "X..X", "X..X" } },
        { 'E', new[] { "XXX", "X..", "XX.", "X..", "XXX" } },
        { 'W', new[] { "X...X", "X...X", "X.X.X", "X.X.X", ".X.X." } },
    };

    public static string Run(string dir)
    {
        outDir = dir; borders.Clear();
        Directory.CreateDirectory(dir);
        foreach (string[] p in Palette)
        {
            Color mid = C(p[1]), light = C(p[2]), dark = C(p[3]);
            string n = p[0];
            Save(Frame(mid, light, dark), "kit_frame_" + n, 6, 6, 6, 6);
            Save(ThinFrame(mid, dark), "kit_frame_thin_" + n, 5, 5, 5, 5);
            Save(Button(mid, light, dark), "kit_button_" + n, 5, 5, 5, 5);
            Save(Tab(mid, light), "kit_tab_" + n, 5, 3, 5, 5);
            Save(Banner(mid, light, dark), "kit_banner_" + n, 10, 0, 10, 0);
            Save(Bubble(mid, light), "kit_bubble_" + n, 6, 9, 6, 5);
            Save(Line(mid, light, dark), "kit_line_" + n, 2, 0, 2, 0);
            Save(Dotted(dark), "kit_line_dotted_" + n, 0, 0, 0, 0);
            Save(BarFill(mid, light, dark), "kit_bar_fill_" + n, 3, 3, 3, 3);
        }
        Save(BarBack(), "kit_bar_back", 4, 4, 4, 4);

        foreach (var icon in Icons)
        {
            FromPattern(icon.Value, ch => White, true).Save(Path.Combine(dir, "kit_icon_" + icon.Key + ".png"));
        }
        string[] arrow = Icons["arrow_right"];
        string[] down = Rotate(arrow), left = Rotate(down), up = Rotate(left);
        FromPattern(down, ch => White, true).Save(Path.Combine(dir, "kit_icon_arrow_down.png"));
        FromPattern(left, ch => White, true).Save(Path.Combine(dir, "kit_icon_arrow_left.png"));
        FromPattern(up, ch => White, true).Save(Path.Combine(dir, "kit_icon_arrow_up.png"));

        // badges
        var alert = new Canvas(14, 14);
        for (int y = 0; y < 14; y++) for (int x = 0; x < 14; x++) { double dx = x - 6.5, dy = y - 6.5; if (dx * dx + dy * dy <= 36) alert.P(x, y, dy < -2 && dx < 0 ? C("FF7878") : Red); }
        for (int y = 3; y <= 7; y++) { alert.P(6, y, White); alert.P(7, y, White); }
        alert.P(6, 9, White); alert.P(7, 9, White); alert.P(6, 10, White); alert.P(7, 10, White);
        alert.Outline(Outline); alert.Save(Path.Combine(dir, "kit_badge_alert.png"));

        var dot = new Canvas(8, 8);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) { double dx = x - 3.5, dy = y - 3.5; if (dx * dx + dy * dy <= 7) dot.P(x, y, Red); }
        dot.Outline(Outline); dot.Save(Path.Combine(dir, "kit_badge_dot.png"));

        var badgeNew = new Canvas(20, 9);
        for (int y = 0; y < 9; y++) for (int x = 0; x < 20; x++) if (InRound(x, y, 20, 9, 3)) badgeNew.P(x, y, C("FF5A8C"));
        badgeNew.Outline(Outline);
        int penX = 3;
        foreach (char letter in "NEW")
        {
            string[] g = Font[letter];
            for (int y = 0; y < 5; y++) for (int x = 0; x < g[y].Length; x++) if (g[y][x] == 'X') badgeNew.P(penX + x, 2 + y, White);
            penX += g[0].Length + 1;
        }
        badgeNew.Save(Path.Combine(dir, "kit_badge_new.png"));

        var pill = new Canvas(12, 9);
        for (int y = 0; y < 9; y++) for (int x = 0; x < 12; x++) if (InRound(x, y, 12, 9, 3)) pill.P(x, y, y >= 6 ? RedDark : Red);
        pill.Outline(Outline);
        Save(pill, "kit_badge_count", 4, 4, 4, 4);

        // toggles
        foreach (bool on in new[] { false, true })
        {
            var box = new Canvas(12, 12);
            for (int y = 1; y < 11; y++) for (int x = 1; x < 11; x++) box.P(x, y, y >= 9 ? C("D8D8D8") : White);
            box.Outline(Outline);
            if (on) { string[] ck = Icons["check"]; for (int y = 0; y < 10; y++) for (int x = 0; x < 10; x++) if (ck[y][x] == 'X' && x >= 0) box.P(x + 1, y + 1, Green); }
            box.Save(Path.Combine(dir, on ? "kit_checkbox_on.png" : "kit_checkbox_off.png"));

            var radio = new Canvas(12, 12);
            for (int y = 0; y < 12; y++) for (int x = 0; x < 12; x++) { double dx = x - 5.5, dy = y - 5.5; if (dx * dx + dy * dy <= 22) radio.P(x, y, White); }
            radio.Outline(Outline);
            if (on) for (int y = 0; y < 12; y++) for (int x = 0; x < 12; x++) { double dx = x - 5.5, dy = y - 5.5; if (dx * dx + dy * dy <= 6) radio.P(x, y, Green); }
            radio.Save(Path.Combine(dir, on ? "kit_radio_on.png" : "kit_radio_off.png"));
        }

        using (var w = new StreamWriter(Path.Combine(dir, "kit_border.txt")))
            foreach (var b in borders) w.WriteLine(b.Key + "," + string.Join(",", b.Value));
        return "ok";
    }
}
