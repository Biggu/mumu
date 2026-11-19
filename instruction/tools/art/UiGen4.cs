// Crown (on/off), heart and paw print sprites (C# 5 / System.Drawing), scaled x4.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class UiGen4
{
    const int S = 4;
    static Color C(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    static readonly Color Outline = C("3A2A1E");

    static void Save(string[] pat, Func<char, Color> color, string path, bool outline)
    {
        int h = pat.Length, w = pat[0].Length;
        var c = new Color[w + 2, h + 2]; var m = new bool[w + 2, h + 2];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (pat[y][x] != '.') { c[x + 1, y + 1] = color(pat[y][x]); m[x + 1, y + 1] = true; }
        if (outline)
        {
            var add = new bool[w + 2, h + 2];
            for (int y = 0; y < h + 2; y++) for (int x = 0; x < w + 2; x++)
                {
                    if (m[x, y]) continue;
                    if ((x + 1 < w + 2 && m[x + 1, y]) || (x > 0 && m[x - 1, y]) || (y + 1 < h + 2 && m[x, y + 1]) || (y > 0 && m[x, y - 1])) add[x, y] = true;
                }
            for (int y = 0; y < h + 2; y++) for (int x = 0; x < w + 2; x++) if (add[x, y]) c[x, y] = Outline;
        }
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

    public static string Run(string dir)
    {
        string[] crown = {
            "..L....L....L..",
            ".LYL..LYL..LYL.",
            "..Y....Y....Y..",
            "..YY..YYY..YY..",
            "..YYYYYYYYYYY..",
            "..YYRYYBYYRYY..",
            "..YYYYYYYYYYY..",
            "..DDDDDDDDDDD.." };
        Save(crown, ch => ch == 'L' ? C("FFF4A0") : ch == 'D' ? C("E89A10") : ch == 'R' ? C("F04860") : ch == 'B' ? C("5AC8F0") : C("FFD030"), Path.Combine(dir, "ui_crown_on.png"), true);
        Save(crown, ch => ch == 'L' ? C("E0E0E0") : ch == 'D' ? C("909090") : ch == 'R' || ch == 'B' ? C("A0A0A0") : C("B8B8B8"), Path.Combine(dir, "ui_crown_off.png"), true);

        string[] heart = { ".XX.XX.", "XWXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
        Save(heart, ch => ch == 'W' ? C("FFC0CC") : C("F04860"), Path.Combine(dir, "ui_heart.png"), true);

        // paw print (pointing up = forward), no outline, soft brown
        string[] paw = {
            ".X.X.",
            "X...X",
            ".....",
            ".XXX.",
            "XXXXX",
            ".XXX." };
        Save(paw, ch => C("7A5A3A"), Path.Combine(dir, "paw.png"), false);
        return "ok";
    }
}
