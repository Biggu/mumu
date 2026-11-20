// テーマのアニメするデコ（スプライトシート・4コマ）を作る。C# 5 / .NET Framework（System.Drawing）
// 各テーマの _2 と _3 を、4コマ横に並べたシート（676x281）で作り直す。_4 と _5 は ThemeGen の静止画のまま。
// 42x70 のドット絵を 4倍して 169x281 のコマにする（下揃え、ThemeGen のデコと同じ大きさ）。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class DecoSheetGen
{
    const int GridW = 42, GridH = 70, Scale = 4;
    const int FrameW = 169, FrameH = 281, Frames = 4;

    /// テーマ ID → (_2 のデコ, _3 のデコ)
    static readonly string[][] Items =
    {
        new[] { "tree", "flower" },          // 1 そうげん
        new[] { "watermelon", "ball" },      // 2 なつ
        new[] { "snowman", "pine" },         // 3 ふゆ
        new[] { "maple", "mushroom" },       // 4 あき
        new[] { "tulip", "bird" },           // 5 はる
        new[] { "blossom", "sakura" },       // 6 さくら
        new[] { "cactus", "sun" },           // 7 さばく
        new[] { "strawberry", "cupcake" },   // 8 ケーキ
        new[] { "pudding", "spoon" },        // 9 プリン
        new[] { "lamp", "car" },             // 10 まち
        new[] { "windmill", "hay" },         // 11 むら
        new[] { "palm", "shell" },           // 12 うみ
        new[] { "snowflake", "penguin" },    // 13 こおり
        new[] { "coffee", "plant" },         // 14 オフィス
        new[] { "dish", "blinker" },         // 15 宇宙
        new[] { "jellyfish", "fish" },       // 16 海の底
        new[] { "lantern", "glowcap" },      // 17 夜の森
        new[] { "flag", "torch" },           // 18 お城
        new[] { "fir", "cloud" },            // 19 山
    };

    /// 作ったものを書き出す（themesFolder は Sprites/Themes。theme_XXXXXX の中に上書き）
    public static void Run(string _themesFolder)
    {
        for (int theme = 1; theme <= Items.Length; theme++)
        {
            string dir = Path.Combine(_themesFolder, "theme_" + theme.ToString("000000"));
            Directory.CreateDirectory(dir);
            for (int slot = 0; slot < 2; slot++)
            {
                string name = Items[theme - 1][slot];
                string path = Path.Combine(dir, theme.ToString("000000") + "_" + (2 + slot) + ".png");
                saveSheet(path, name);
            }
            Console.WriteLine("theme " + theme + ": " + Items[theme - 1][0] + ", " + Items[theme - 1][1]);
        }
    }

    /// 4コマを横に並べたシートを書き出す
    static void saveSheet(string _path, string _item)
    {
        using (var sheet = new Bitmap(FrameW * Frames, FrameH))
        using (Graphics g = Graphics.FromImage(sheet))
        {
            g.Clear(Color.Transparent);
            for (int f = 0; f < Frames; f++)
            {
                var canvas = new Canvas();
                drawItem(_item, canvas, f);
                using (Bitmap frame = render(canvas))
                {
                    g.DrawImage(frame, f * FrameW, 0);
                }
            }
            sheet.Save(_path, ImageFormat.Png);
        }
    }

    /// 42x70 の絵を 169x281 のコマにする（4倍。下揃え）
    static Bitmap render(Canvas _canvas)
    {
        var bmp = new Bitmap(FrameW, FrameH);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            for (int y = 0; y < GridH; y++)
            {
                for (int x = 0; x < GridW; x++)
                {
                    Color c = _canvas.Get(x, y);
                    if (c.A == 0)
                    {
                        continue;
                    }
                    using (var brush = new SolidBrush(c))
                    {
                        g.FillRectangle(brush, x * Scale, 1 + y * Scale, Scale, Scale);
                    }
                }
            }
        }
        return bmp;
    }

    /// 揺れ（左右に1マス）・跳ね（上に2マスまで）のずれ
    static int sway(int _f) { return new[] { 0, 1, 0, -1 }[_f]; }
    static int bob(int _f) { return new[] { 0, -1, -2, -1 }[_f]; }

    static Color H(string _hex)
    {
        return Color.FromArgb(Convert.ToInt32(_hex.Substring(0, 2), 16), Convert.ToInt32(_hex.Substring(2, 2), 16), Convert.ToInt32(_hex.Substring(4, 2), 16));
    }

    /// 1コマ描く（_f は 0〜3）
    static void drawItem(string _item, Canvas _c, int _f)
    {
        int s = sway(_f), b = bob(_f);
        switch (_item)
        {
            case "tree":
                _c.Rect(19, 44, 22, 68, H("8A5A34"));
                _c.CircleOutlined(20 + s, 30, 11, H("4E9A3A"), H("1E4A1E"));
                _c.CircleOutlined(14 + s, 38, 7, H("4E9A3A"), H("1E4A1E"));
                _c.CircleOutlined(27 + s, 38, 7, H("4E9A3A"), H("1E4A1E"));
                _c.Circle(17 + s, 25, 2, H("8ED05A"));
                break;
            case "flower":
                _c.Line(8, 68, 8, 50 + b, H("3E8A3E"));
                int hy = 46 + b;
                _c.Circle(8, hy - 4, 2, H("F07AA8"));
                _c.Circle(12, hy - 1, 2, H("F07AA8"));
                _c.Circle(10, hy + 3, 2, H("F07AA8"));
                _c.Circle(6, hy + 3, 2, H("F07AA8"));
                _c.Circle(4, hy - 1, 2, H("F07AA8"));
                _c.Circle(8, hy, 2, H("FFD84A"));
                break;
            case "watermelon":
                int wy = 50 + b;
                _c.CircleOutlined(21, wy, 14, H("2E7D32"), H("1B3A1F"));
                _c.Circle(21, wy, 11, H("E53935"));
                _c.Set(17, wy - 3, H("2A1A1A")); _c.Set(24, wy - 2, H("2A1A1A")); _c.Set(21, wy + 3, H("2A1A1A"));
                _c.Set(15, wy + 2, H("2A1A1A")); _c.Set(26, wy + 4, H("2A1A1A"));
                break;
            case "ball":
                DrawBall(_c, 21, 40 + b, 9);
                break;
            case "snowman":
                _c.CircleOutlined(21, 58 + b, 10, H("FFFFFF"), H("3A4A5A"));
                _c.CircleOutlined(21, 44 + b, 7, H("FFFFFF"), H("3A4A5A"));
                _c.CircleOutlined(21, 33 + b, 5, H("FFFFFF"), H("3A4A5A"));
                _c.Rect(17, 25 + b, 25, 27 + b, H("222222"));
                _c.Rect(19, 21 + b, 23, 26 + b, H("222222"));
                _c.Set(19, 32 + b, H("000000")); _c.Set(23, 32 + b, H("000000"));
                _c.Set(21, 35 + b, H("FF8C1A"));
                _c.Set(21, 40 + b, H("000000")); _c.Set(21, 46 + b, H("000000"));
                break;
            case "pine":
                for (int y = 12; y <= 60; y++)
                {
                    int half = Math.Min(10, (y - 12) / 6 + 1);
                    _c.Rect(21 + s - half, y, 21 + s + half, y, H("1F6B3A"));
                    if (y % 5 == 0)
                    {
                        _c.Set(18 + s, y, H("3E9A5A"));
                    }
                }
                _c.Rect(19, 60, 22, 68, H("6B4226"));
                break;
            case "maple":
                _c.Rect(19, 40, 22, 68, H("6B4226"));
                _c.CircleOutlined(21 + s, 28, 13, H("E8572A"), H("7A2A10"));
                _c.Circle(16 + s, 22, 3, H("FFB030"));
                _c.Set(26 + s, 32, H("FFB030"));
                break;
            case "mushroom":
                _c.Rect(17, 51 + b, 24, 69, H("3A2A1E"));
                _c.Rect(18, 52 + b, 23, 68, H("F5E6C8"));
                _c.CircleOutlined(21, 52 + b, 11, H("D62828"), H("6A1010"));
                _c.Set(15, 50 + b, H("FFFFFF")); _c.Set(20, 46 + b, H("FFFFFF")); _c.Set(26, 49 + b, H("FFFFFF")); _c.Set(24, 54 + b, H("FFFFFF"));
                break;
            case "tulip":
                _c.Line(21, 68, 21, 44 + b, H("3E8A3E"));
                _c.Line(21, 60, 13, 50, H("3E8A3E"));
                _c.Line(21, 58, 29, 48, H("3E8A3E"));
                _c.CircleOutlined(21, 38 + b, 6, H("FF6FA0"), H("7A1A3A"));
                _c.Rect(19, 42 + b, 23, 44 + b, H("FF6FA0"));
                break;
            case "bird":
                int wing = new[] { 0, -3, 0, 2 }[_f];
                _c.CircleOutlined(21, 40 + b, 7, H("3A7BD5"), H("1B3A66"));
                _c.CircleOutlined(28, 34 + b, 4, H("3A7BD5"), H("1B3A66"));
                _c.Rect(31, 33 + b, 33, 34 + b, H("FF9900"));
                _c.Set(29, 33 + b, H("000000"));
                _c.Line(19, 40 + b, 9, 38 + wing + b, H("1B3A66"));
                _c.Line(20, 41 + b, 12, 41 + wing + b, H("1B3A66"));
                break;
            case "blossom":
                _c.Rect(19, 44, 22, 68, H("8A5A34"));
                _c.CircleOutlined(21 + s, 30, 12, H("FFB7C5"), H("9C5A6E"));
                _c.CircleOutlined(13 + s, 38, 6, H("FFB7C5"), H("9C5A6E"));
                _c.CircleOutlined(29 + s, 38, 6, H("FFB7C5"), H("9C5A6E"));
                _c.Set(16 + s, 26, H("FFFFFF")); _c.Set(26 + s, 33, H("FFFFFF")); _c.Set(20 + s, 35, H("FFFFFF"));
                break;
            case "sakura":
                _c.Line(21, 68, 21, 47 + b, H("3E8A3E"));
                DrawPetals(_c, 21, 40 + b, 6, 4, H("FFC1D6"), H("9C5A6E"));
                _c.Circle(21, 40 + b, 2, H("FFD84A"));
                break;
            case "cactus":
                _c.RectOutlined(17, 28, 24, 68, H("3E9A4E"), H("1E4A24"));
                _c.RectOutlined(12, 44 + b, 16, 50 + b, H("3E9A4E"), H("1E4A24"));
                _c.RectOutlined(25, 38 + b, 29, 44 + b, H("3E9A4E"), H("1E4A24"));
                break;
            case "sun":
                for (int k = 0; k < 8; k++)
                {
                    double a = (k * 45 + _f * 11.25) * Math.PI / 180.0;
                    _c.Line(21, 20, 21 + (int)Math.Round(Math.Cos(a) * 13), 20 + (int)Math.Round(Math.Sin(a) * 13), H("FFD23F"));
                }
                _c.CircleOutlined(21, 20, 8, H("FFD23F"), H("C77700"));
                break;
            case "strawberry":
                _c.Rect(20, 35 + b, 22, 40 + b, H("3E8A3E"));
                _c.CircleOutlined(16, 37 + b, 3, H("3E8A3E"), H("1E4A1E"));
                _c.CircleOutlined(26, 37 + b, 3, H("3E8A3E"), H("1E4A1E"));
                _c.CircleOutlined(21, 47 + b, 9, H("E53935"), H("7A0A0A"));
                _c.Rect(19, 54 + b, 23, 56 + b, H("E53935"));
                _c.Set(21, 58 + b, H("E53935"));
                _c.Set(16, 44 + b, H("FFE066")); _c.Set(21, 42 + b, H("FFE066")); _c.Set(26, 44 + b, H("FFE066")); _c.Set(18, 50 + b, H("FFE066")); _c.Set(24, 50 + b, H("FFE066"));
                break;
            case "cupcake":
                _c.RectOutlined(12, 54 + b, 30, 68, H("E8B060"), H("6A4A1A"));
                _c.CircleOutlined(21, 48 + b, 10, H("FF9ECF"), H("8A3A6A"));
                _c.Circle(21, 38 + b, 2, H("E53935"));
                break;
            case "pudding":
                for (int y = 46; y <= 66; y++)
                {
                    int jig = y <= 52 ? s : 0;
                    _c.Rect(12 + jig, y, 30 + jig, y, y <= 48 ? H("FFE08A") : H("E8A33D"));
                    _c.Set(11 + jig, y, H("6A4A1A"));
                    _c.Set(31 + jig, y, H("6A4A1A"));
                }
                break;
            case "spoon":
                _c.Line(30, 62 + b, 38, 68, H("C0C8D0"));
                _c.CircleOutlined(29, 56 + b, 4, H("C0C8D0"), H("4A5A6A"));
                break;
            case "lamp":
                _c.Rect(20, 30, 22, 68, H("5A6070"));
                Color glow = _f % 2 == 0 ? H("FFE066") : H("C9A227");
                _c.Circle(21, 26, 9, Color.FromArgb(60, 255, 240, 150));
                _c.CircleOutlined(21, 26, 6, glow, H("5A4A10"));
                break;
            case "car":
                _c.RectOutlined(8, 50 + b, 34, 60 + b, H("D9342B"), H("6A1510"));
                _c.RectOutlined(14, 44 + b, 28, 50 + b, H("D9342B"), H("6A1510"));
                _c.Rect(16, 46 + b, 26, 49 + b, H("A8D8FF"));
                _c.Circle(14, 62 + b, 4, H("222222"));
                _c.Circle(28, 62 + b, 4, H("222222"));
                break;
            case "windmill":
                for (int y = 40; y <= 68; y++)
                {
                    int half = 3 + (68 - y) / 8;
                    _c.Rect(21 - half, y, 21 + half, y, H("F4F4F4"));
                }
                _c.Rect(20, 60, 22, 68, H("8A5A34"));
                for (int k = 0; k < 4; k++)
                {
                    double a = (k * 90 + _f * 22.5) * Math.PI / 180.0;
                    int ex = 21 + (int)Math.Round(Math.Cos(a) * 14), ey = 34 + (int)Math.Round(Math.Sin(a) * 14);
                    _c.Line(21, 34, ex, ey, H("E8D5A0"));
                    _c.Line(21, 35, ex, ey + 1, H("E8D5A0"));
                }
                _c.Circle(21, 34, 3, H("D62828"));
                break;
            case "hay":
                _c.RectOutlined(8, 54 + b, 34, 68, H("F2C94C"), H("8A6A10"));
                _c.Line(10, 58 + b, 32, 58 + b, H("B8862A"));
                _c.Line(10, 63 + b, 32, 63 + b, H("B8862A"));
                break;
            case "palm":
                _c.Line(20, 68, 24 + s, 30, H("8A5A34"));
                _c.Line(21, 68, 25 + s, 30, H("8A5A34"));
                _c.Line(19, 68, 23 + s, 30, H("8A5A34"));
                foreach (int deg in new[] { -160, -120, -60, -20 })
                {
                    double a = deg * Math.PI / 180.0;
                    int ex = 24 + s + (int)Math.Round(Math.Cos(a) * 14), ey = 30 + (int)Math.Round(Math.Sin(a) * 14);
                    _c.Line(24 + s, 30, ex, ey, H("2E8B57"));
                    _c.Line(24 + s, 31, ex, ey + 1, H("2E8B57"));
                }
                _c.Circle(23 + s, 33, 2, H("6B4226"));
                break;
            case "shell":
                _c.CircleOutlined(21, 50 + b, 11, H("FFA7B6"), H("8A3A4A"));
                for (int deg = -150; deg <= -30; deg += 20)
                {
                    double a = deg * Math.PI / 180.0;
                    _c.Line(21, 62 + b, 21 + (int)Math.Round(Math.Cos(a) * 10), 62 + b + (int)Math.Round(Math.Sin(a) * 10), H("E97A8F"));
                }
                break;
            case "snowflake":
                for (int k = 0; k < 6; k++)
                {
                    double a = (k * 60 + _f * 15) * Math.PI / 180.0;
                    int ex = 21 + (int)Math.Round(Math.Cos(a) * 14), ey = 34 + (int)Math.Round(Math.Sin(a) * 14);
                    _c.Line(21, 34, ex, ey, H("9BE7FF"));
                    int bx = 21 + (int)Math.Round(Math.Cos(a) * 8), by = 34 + (int)Math.Round(Math.Sin(a) * 8);
                    double a2 = a + Math.PI / 4;
                    _c.Line(bx, by, bx + (int)Math.Round(Math.Cos(a2) * 4), by + (int)Math.Round(Math.Sin(a2) * 4), H("9BE7FF"));
                }
                _c.Circle(21, 34, 2, H("FFFFFF"));
                break;
            case "penguin":
                _c.CircleOutlined(21, 48 + b, 10, H("1E1E28"), H("000000"));
                _c.Circle(21, 50 + b, 6, H("FFFFFF"));
                _c.CircleOutlined(21, 36 + b, 7, H("1E1E28"), H("000000"));
                _c.Circle(21, 38 + b, 4, H("FFFFFF"));
                _c.Set(19, 35 + b, H("000000")); _c.Set(23, 35 + b, H("000000"));
                _c.Rect(21, 39 + b, 25, 40 + b, H("FF9900"));
                _c.Rect(15, 58, 19, 60, H("FF9900"));
                _c.Rect(23, 58, 27, 60, H("FF9900"));
                break;
            case "coffee":
                _c.Rect(28, 50 + b, 31, 53 + b, H("F7F7F7"));
                _c.RectOutlined(14, 46 + b, 28, 64 + b, H("F7F7F7"), H("6B4A2A"));
                _c.Rect(15, 46 + b, 27, 47 + b, H("6B4226"));
                _c.Line(18, 40, 18 + s, 32, H("C8C8C8"));
                _c.Line(23, 40, 23 - s, 32, H("C8C8C8"));
                break;
            case "plant":
                _c.Line(21, 56, 21 + s, 46, H("3E8A3E"));
                _c.RectOutlined(15, 56, 27, 68, H("C0603A"), H("6A2A15"));
                _c.CircleOutlined(21 + s, 46, 7, H("3E9A4E"), H("1E4A1E"));
                _c.CircleOutlined(14 + s, 50, 5, H("3E9A4E"), H("1E4A1E"));
                _c.CircleOutlined(28 + s, 50, 5, H("3E9A4E"), H("1E4A1E"));
                break;
            case "dish":
                _c.Rect(20, 46, 22, 68, H("6A7488"));
                _c.CircleOutlined(21 + s, 40 + b, 8, H("C8D2E0"), H("1E2432"));
                _c.Circle(21 + s, 40 + b, 3, H("5A6478"));
                break;
            case "blinker":
                // 赤いランプが点滅する
                _c.Rect(20, 30, 22, 68, H("6A7488"));
                _c.Rect(16, 62, 26, 68, H("4A5366"));
                _c.Circle(21, 28 + b, 2, _f % 2 == 0 ? H("FF4A4A") : H("6A2A2A"));
                break;
            case "jellyfish":
                // 傘がふわっと上下し、触手が揺れる
                _c.CircleOutlined(21, 30 + b, 9, H("F09CD8"), H("6A2A5A"));
                for (int i = 0; i < 4; i++)
                {
                    int tx = 14 + i * 4;
                    int sw = (i % 2 == 0) ? s : -s;
                    _c.Line(tx, 36 + b, tx + sw, 52 + b, H("E878C8"));
                }
                break;
            case "fish":
                _c.Circle(21 + s, 50 + b, 7, H("FFA040"));
                _c.Line(12 + s, 50 + b, 7 + s, 45 + b, H("FF8020"));
                _c.Line(12 + s, 50 + b, 7 + s, 55 + b, H("FF8020"));
                _c.Circle(24 + s, 48 + b, 1, H("1E1E24"));
                break;
            case "lantern":
                // 灯りが強弱を繰り返す
                _c.Rect(20, 40, 22, 68, H("5A5E66"));
                _c.RectOutlined(17, 34, 25, 40, H("3A3E46"), H("1A1C22"));
                _c.Circle(21, 37, new[] { 3, 4, 3, 2 }[_f], H("FFE070"));
                break;
            case "glowcap":
                _c.Rect(19, 54, 23, 68, H("F0E4CC"));
                _c.CircleOutlined(21, 50 + b, 7, H("38D8C8"), H("1A4A46"));
                _c.Circle(18, 48 + b, 1, H("FFFFFF"));
                _c.Circle(24, 52 + b, 1, H("FFFFFF"));
                break;
            case "flag":
                // 旗がはためく
                _c.Rect(19, 22, 21, 68, H("6A6A6A"));
                _c.Rect(22 + s, 22, 30 + s, 30, H("D84040"));
                _c.Rect(25 + s, 25, 27 + s, 27, H("F8E070"));
                break;
            case "torch":
                _c.Rect(19, 46, 22, 68, H("6A4A2A"));
                _c.Circle(21, 42 + b, 3, H("FF8A20"));
                _c.Circle(21, 41 + b, 1, H("FFE070"));
                break;
            case "fir":
                // 木が左右に揺れる（上ほど大きく揺れる）
                _c.Rect(19, 54, 23, 68, H("6A4A2A"));
                for (int y = 26; y <= 56; y++)
                {
                    int w = (y - 26) / 4 + 1;
                    int cx = 21 + (s * (56 - y)) / 30;
                    _c.Line(cx - w, y, cx + w, y, H("2E7A4A"));
                }
                break;
            case "cloud":
                // 雲がふわふわ浮く
                _c.Rect(10 + s, 48 + b, 32 + s, 52 + b, H("F4F8FF"));
                _c.Circle(15 + s, 46 + b, 5, H("FFFFFF"));
                _c.Circle(23 + s, 44 + b, 7, H("FFFFFF"));
                _c.Circle(29 + s, 48 + b, 5, H("F4F8FF"));
                break;
            default:
                throw new ArgumentException("unknown item " + _item);
        }
    }

    /// 6色のうち3色（赤・白・青）の縞のボール（外側は暗い輪郭）
    static void DrawBall(Canvas _c, int _cx, int _cy, int _r)
    {
        _c.Circle(_cx, _cy, _r + 1, H("3A2A1E"));
        for (int y = _cy - _r; y <= _cy + _r; y++)
        {
            for (int x = _cx - _r; x <= _cx + _r; x++)
            {
                int dx = x - _cx, dy = y - _cy;
                if (dx * dx + dy * dy > _r * _r)
                {
                    continue;
                }
                double angle = Math.Atan2(dy, dx) + Math.PI;
                int sector = (int)(angle / (2 * Math.PI / 6)) % 3;
                Color c = sector == 0 ? H("E53935") : sector == 1 ? H("FFFFFF") : H("3A7BD5");
                _c.Set(x, y, c);
            }
        }
    }

    /// 花びら（円を放射状に並べる）
    static void DrawPetals(Canvas _c, int _cx, int _cy, int _dist, int _r, Color _fill, Color _outline)
    {
        for (int k = 0; k < 5; k++)
        {
            double a = (-90 + k * 72) * Math.PI / 180.0;
            int px = _cx + (int)Math.Round(Math.Cos(a) * _dist);
            int py = _cy + (int)Math.Round(Math.Sin(a) * _dist);
            _c.CircleOutlined(px, py, _r, _fill, _outline);
        }
    }

    /// 1コマ分の絵（42x70。透明で始まる）
    class Canvas
    {
        readonly Color[,] pixels = new Color[GridW, GridH];

        public Canvas()
        {
            for (int y = 0; y < GridH; y++)
                for (int x = 0; x < GridW; x++)
                    pixels[x, y] = Color.Transparent;
        }

        public Color Get(int _x, int _y)
        {
            return pixels[_x, _y];
        }

        public void Set(int _x, int _y, Color _c)
        {
            if (_x < 0 || _y < 0 || _x >= GridW || _y >= GridH)
            {
                return;
            }
            pixels[_x, _y] = _c;
        }

        public void Circle(int _cx, int _cy, int _r, Color _c)
        {
            for (int y = _cy - _r; y <= _cy + _r; y++)
                for (int x = _cx - _r; x <= _cx + _r; x++)
                {
                    int dx = x - _cx, dy = y - _cy;
                    if (dx * dx + dy * dy <= _r * _r)
                    {
                        Set(x, y, _c);
                    }
                }
        }

        public void CircleOutlined(int _cx, int _cy, int _r, Color _fill, Color _outline)
        {
            Circle(_cx, _cy, _r + 1, _outline);
            Circle(_cx, _cy, _r, _fill);
        }

        public void Rect(int _x0, int _y0, int _x1, int _y1, Color _c)
        {
            for (int y = _y0; y <= _y1; y++)
                for (int x = _x0; x <= _x1; x++)
                    Set(x, y, _c);
        }

        public void RectOutlined(int _x0, int _y0, int _x1, int _y1, Color _fill, Color _outline)
        {
            Rect(_x0 - 1, _y0 - 1, _x1 + 1, _y1 + 1, _outline);
            Rect(_x0, _y0, _x1, _y1, _fill);
        }

        public void Line(int _x0, int _y0, int _x1, int _y1, Color _c)
        {
            int n = Math.Max(Math.Abs(_x1 - _x0), Math.Abs(_y1 - _y0));
            if (n == 0)
            {
                Set(_x0, _y0, _c);
                return;
            }
            for (int i = 0; i <= n; i++)
            {
                int x = _x0 + (int)Math.Round((double)(_x1 - _x0) * i / n);
                int y = _y0 + (int)Math.Round((double)(_y1 - _y0) * i / n);
                Set(x, y, _c);
            }
        }
    }
}
