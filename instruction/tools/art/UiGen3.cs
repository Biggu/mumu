// Extra pixel-art UI sprites: stars, gauge, menu/retry/home icons (C# 5 / System.Drawing), scaled x4.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class UiGen3
{
    const int S = 4;

    static Color C(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    static readonly Color Outline = C("3A2A1E");

    class Canvas
    {
        public int W, H;
        public Color[,] c;
        public bool[,] m;
        public Canvas(int w, int h) { W = w; H = h; c = new Color[w, h]; m = new bool[w, h]; }
        public void P(int x, int y, Color col) { if (x >= 0 && y >= 0 && x < W && y < H) { c[x, y] = col; m[x, y] = true; } }
        public void Outline(Color col)
        {
            bool[,] add = new bool[W, H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    if (m[x, y]) continue;
                    if ((x + 1 < W && m[x + 1, y]) || (x > 0 && m[x - 1, y]) || (y + 1 < H && m[x, y + 1]) || (y > 0 && m[x, y - 1])) add[x, y] = true;
                }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (add[x, y]) { c[x, y] = col; m[x, y] = true; }
        }
        public void Save(string path)
        {
            using (Bitmap b = new Bitmap(W * S, H * S, PixelFormat.Format32bppArgb))
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

    static bool InRound(int x, int y, int w, int h, int r)
    {
        int x0 = 1, y0 = 1, x1 = w - 2, y1 = h - 2;
        if (x < x0 || y < y0 || x > x1 || y > y1) return false;
        int cx = x < x0 + r ? x0 + r : (x > x1 - r ? x1 - r : x);
        int cy = y < y0 + r ? y0 + r : (y > y1 - r ? y1 - r : y);
        double dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r + 0.5;
    }

    // 5-point star test (center 8,8.4)
    static bool InStar(double x, double y, double r)
    {
        double cx = 8, cy = 8.6;
        double a = Math.Atan2(x - cx, -(y - cy));
        double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        double seg = Math.PI * 2 / 5;
        double t = ((a % seg) + seg) % seg / seg;
        double k = Math.Abs(t - 0.5) * 2; // 1 at tip, 0 between tips
        double lim = r * (0.45 + 0.55 * Math.Pow(k, 1.6));
        return d <= lim;
    }

    static Canvas Star(bool on)
    {
        int n = 16; Canvas k = new Canvas(n, n);
        Color light = on ? C("FFF4A0") : C("E0E0E0"), mid = on ? C("FFD030") : C("B8B8B8"), dark = on ? C("E89A10") : C("909090");
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                if (!InStar(x + 0.5, y + 0.5, 7.2)) continue;
                double lt = -(x - 8) * 0.5 - (y - 8) * 0.8;
                k.P(x, y, lt > 2.5 ? light : (lt > -2.5 ? mid : dark));
            }
        k.Outline(Outline);
        if (on) { k.c[6, 5] = Color.White; k.c[6, 6] = Color.White; }
        return k;
    }

    static Canvas GaugeBack()
    {
        int w = 24, h = 10; Canvas k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!InRound(x, y, w, h, 3)) continue;
                k.P(x, y, y <= 2 ? C("4A3226") : C("6A4A36"));
            }
        k.Outline(Outline);
        return k;
    }

    static Canvas GaugeFill()
    {
        int w = 16, h = 8; Canvas k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!InRound(x, y, w, h, 2)) continue;
                k.P(x, y, y <= 2 ? C("FFB0D0") : (y >= h - 3 ? C("E0508C") : C("FF78AC")));
            }
        for (int x = 3; x < w - 3; x++) k.P(x, 2, C("FFE0EE"));
        // no outline: sits inside the back
        return k;
    }

    static Canvas Icon(Func<double, double, bool> shape)
    {
        int n = 12; Canvas k = new Canvas(n, n);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) if (shape(x + 0.5, y + 0.5)) k.P(x, y, Color.White);
        k.Outline(Outline);
        return k;
    }

    public static string Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        Star(true).Save(Path.Combine(outDir, "ui_star_on.png"));
        Star(false).Save(Path.Combine(outDir, "ui_star_off.png"));
        GaugeBack().Save(Path.Combine(outDir, "ui_gauge_back.png"));
        GaugeFill().Save(Path.Combine(outDir, "ui_gauge_fill.png"));

        Icon(delegate (double x, double y)
        {
            bool bar = x >= 2 && x <= 10;
            return bar && ((y >= 2.5 && y <= 4) || (y >= 5.25 && y <= 6.75) || (y >= 8 && y <= 9.5));
        }).Save(Path.Combine(outDir, "ui_icon_menu.png"));

        string[] retry = {
            "............",
            "....XXXX.X..",
            "..XXXXXXXX..",
            ".XX....XXX..",
            ".XX...XXXX..",
            "XX..........",
            "XX......XX..",
            "XX......XX..",
            ".XX....XX...",
            "..XXXXXXX...",
            "....XXXX....",
            "............" };
        Icon(delegate (double x, double y) { return retry[(int)y][(int)x] == 'X'; }).Save(Path.Combine(outDir, "ui_icon_retry.png"));

        Icon(delegate (double x, double y)
        {
            bool roof = y >= 1 && y <= 6 && Math.Abs(x - 6) <= (y - 1) + 0.6;
            bool body = x >= 2.5 && x <= 9.5 && y >= 5.5 && y <= 10.5;
            bool door = x >= 5 && x <= 7 && y >= 7.5 && y <= 10.5;
            return (roof || body) && !door;
        }).Save(Path.Combine(outDir, "ui_icon_home.png"));

        string[] names = { "ui_star_on", "ui_star_off", "ui_gauge_back", "ui_gauge_fill", "ui_icon_menu", "ui_icon_retry", "ui_icon_home" };
        using (Bitmap sheet = new Bitmap(names.Length * 110 + 10, 120))
        using (Graphics g = Graphics.FromImage(sheet))
        {
            g.Clear(Color.FromArgb(90, 140, 90));
            for (int i = 0; i < names.Length; i++)
                using (Image img = Image.FromFile(Path.Combine(outDir, names[i] + ".png")))
                    g.DrawImage(img, 10 + i * 110, 10, img.Width, img.Height);
            sheet.Save(Path.Combine(outDir, "ui2_preview.png"), ImageFormat.Png);
        }
        return "ok";
    }
}
