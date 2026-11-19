// Pixel-art UI sprite generator (C# 5 / System.Drawing). Drawn small and scaled x4.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class UiGen
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

    // inside a rounded rect (1px margin left for the outline)
    static bool InRound(int x, int y, int w, int h, int r)
    {
        int x0 = 1, y0 = 1, x1 = w - 2, y1 = h - 2;
        if (x < x0 || y < y0 || x > x1 || y > y1) return false;
        int cx = x < x0 + r ? x0 + r : (x > x1 - r ? x1 - r : x);
        int cy = y < y0 + r ? y0 + r : (y > y1 - r ? y1 - r : y);
        double dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r + 0.5;
    }

    static Canvas Button(string light, string mid, string dark)
    {
        int w = 24, h = 12; Canvas k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!InRound(x, y, w, h, 3)) continue;
                Color col = y <= 3 ? C(light) : (y >= h - 4 ? C(dark) : C(mid));
                k.P(x, y, col);
            }
        for (int x = 4; x < w - 4; x++) k.P(x, 2, Color.FromArgb(255, 255, 240, 200));
        k.Outline(Outline);
        return k;
    }

    static Canvas Panel()
    {
        int w = 24, h = 24; Canvas k = new Canvas(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!InRound(x, y, w, h, 3)) continue;
                bool inner = InRound(x - 2, y - 2, w - 4, h - 4, 2) && !InRound(x - 3, y - 3, w - 6, h - 6, 1);
                Color col = inner ? C("E8C890") : (y >= h - 3 ? C("F0DCB4") : C("FFF4DC"));
                k.P(x, y, col);
            }
        k.Outline(Outline);
        return k;
    }

    static Canvas Plate()
    {
        int w = 24, h = 16; Canvas k = new Canvas(w, h);
        Random r = new Random(3);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!InRound(x, y, w, h, 2)) continue;
                Color col = (y % 5 == 0) ? C("8A5A30") : C("C88A50");
                if (y % 5 != 0 && r.NextDouble() < 0.12) col = C("B07840");
                if (y <= 2) col = y % 5 == 0 ? col : C("D8A060");
                k.P(x, y, col);
            }
        foreach (int[] n in new[] { new[] { 3, 3 }, new[] { 20, 3 }, new[] { 3, 12 }, new[] { 20, 12 } }) { k.P(n[0], n[1], C("D8D8D8")); }
        k.Outline(Outline);
        return k;
    }

    static Canvas Round()
    {
        int n = 16; Canvas k = new Canvas(n, n);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                double dx = x + 0.5 - 8, dy = y + 0.5 - 8, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 6.9) continue;
                Color col = dy > 3.2 ? C("C8C8C8") : (dy < -3 && dx < 0 ? C("FFFFFF") : C("EEEEEE"));
                k.P(x, y, col);
            }
        k.Outline(Outline);
        return k;
    }

    static Canvas Toggle(bool on)
    {
        int w = 20, h = 10; Canvas k = new Canvas(w, h);
        string fill = on ? "5CC85C" : "A8A8A8", shade = on ? "3E9A3E" : "888888";
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (InRound(x, y, w, h, 4)) k.P(x, y, y >= h - 3 ? C(shade) : C(fill));
        double kx = on ? 14.5 : 5.5;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                double dx = x + 0.5 - kx, dy = y + 0.5 - 5;
                if (dx * dx + dy * dy <= 10.5) k.c[x, y] = dy > 1.2 ? C("D8D8D8") : C("FFFFFF");
            }
        k.Outline(Outline);
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
        Button("FFD080", "F8A040", "D07830").Save(Path.Combine(outDir, "ui_button.png"));
        Panel().Save(Path.Combine(outDir, "ui_panel.png"));
        Plate().Save(Path.Combine(outDir, "ui_plate.png"));
        Round().Save(Path.Combine(outDir, "ui_button_round.png"));
        Toggle(true).Save(Path.Combine(outDir, "ui_toggle_on.png"));
        Toggle(false).Save(Path.Combine(outDir, "ui_toggle_off.png"));

        Icon(delegate (double x, double y)
        {
            double dx = x - 6, dy = y - 6, d = Math.Sqrt(dx * dx + dy * dy), a = Math.Atan2(dy, dx);
            bool body = d <= 3.6 || (d <= 4.9 && Math.Cos(a * 8) > 0.2);
            return body && d > 1.5;
        }).Save(Path.Combine(outDir, "ui_icon_gear.png"));

        Icon(delegate (double x, double y)
        {
            if (x < 2 || x > 10 || y < 2 || y > 10) return false;
            return Math.Abs(x - y) <= 1.0 || Math.Abs(x + y - 12) <= 1.0;
        }).Save(Path.Combine(outDir, "ui_icon_close.png"));

        Icon(delegate (double x, double y)
        {
            bool box = x >= 1.5 && x <= 3.5 && y >= 4.5 && y <= 7.5;
            bool cone = x >= 3.5 && x <= 6.5 && Math.Abs(y - 6) <= (x - 3.5) * 0.9 + 1.5;
            double dx = x - 6, dy = y - 6, d = Math.Sqrt(dx * dx + dy * dy);
            bool wave = dx > 0 && Math.Abs(dy) < dx * 1.1 && ((d >= 2.6 && d <= 3.4) || (d >= 4.4 && d <= 5.2));
            return box || cone || wave;
        }).Save(Path.Combine(outDir, "ui_icon_se.png"));

        Icon(delegate (double x, double y)
        {
            double h1 = (x - 3.5) * (x - 3.5) + (y - 9) * (y - 9);
            double h2 = (x - 8.5) * (x - 8.5) + (y - 8) * (y - 8);
            bool heads = h1 <= 3.2 || h2 <= 3.2;
            bool stems = (x >= 4.5 && x <= 5.5 && y >= 2 && y <= 9) || (x >= 9.5 && x <= 10.5 && y >= 1 && y <= 8);
            bool beam = x >= 4.5 && x <= 10.5 && y >= 1.5 + (10.5 - x) * 0.15 && y <= 3.5 + (10.5 - x) * 0.15;
            return heads || stems || beam;
        }).Save(Path.Combine(outDir, "ui_icon_bgm.png"));

        // preview sheet
        string[] names = { "ui_button", "ui_panel", "ui_plate", "ui_button_round", "ui_toggle_on", "ui_toggle_off", "ui_icon_gear", "ui_icon_close", "ui_icon_se", "ui_icon_bgm" };
        using (Bitmap sheet = new Bitmap(10 * 110 + 10, 130))
        using (Graphics g = Graphics.FromImage(sheet))
        using (Font font = new Font("Segoe UI", 7))
        {
            g.Clear(Color.FromArgb(90, 140, 90));
            for (int i = 0; i < names.Length; i++)
                using (Image img = Image.FromFile(Path.Combine(outDir, names[i] + ".png")))
                {
                    g.DrawImage(img, 10 + i * 110, 10, img.Width, img.Height);
                    g.DrawString(names[i], font, Brushes.White, 10 + i * 110, 112);
                }
            sheet.Save(Path.Combine(outDir, "ui_preview.png"), ImageFormat.Png);
        }
        return "ok";
    }
}
