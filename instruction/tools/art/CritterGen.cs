// Small ambient critters (2 frames each), 12x12 px scaled x4 -> 48x48 cells. Sheet: 2 cols x 5 rows.
// Rows: butterfly, gull, bunny, candy, plane. Sprite names: critter_{name}_{0|1}.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public static class CritterGen
{
    const int N = 12, S = 4;
    static Color C(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    static readonly Color Outline = C("3A2A1E");

    public static readonly string[] Names = { "butterfly", "gull", "bunny", "candy", "plane" };

    static readonly Dictionary<string, string[][]> Frames = new Dictionary<string, string[][]>
    {
        { "butterfly", new[] {
            new[] { "............", "..B......B..", "...B....B...", ".WWW.BB.WWW.", "WWLWWBBWWLWW", "WWWWWBBWWWWW", ".WWWWBBWWWW.", "..WWWBBWWW..", ".YYY.BB.YYY.", ".YY..BB..YY.", "............", "............" },
            new[] { "............", "..B......B..", "...B....B...", "....WBBW....", "...WWBBWW...", "...WLBBLW...", "...WWBBWW...", "....YBBY....", "....YBBY....", ".....BB.....", "............", "............" } } },
        { "gull", new[] {
            new[] { "............", "............", ".WW......WW.", "WWWW....WWWW", "...WW..WW...", "....WWWW....", ".....GG.....", "............", "............", "............", "............", "............" },
            new[] { "............", "............", "............", "............", "....WWWW....", "..WWWGGWWW..", ".WW......WW.", "WW........WW", "............", "............", "............", "............" } } },
        { "bunny", new[] {
            new[] { "............", ".....WW.....", ".....WP.....", ".....WW.....", "....WWWW....", "...WWWWWW...", "..WWWWWWEW..", "..WWWWWWWW..", "..WWWWWWWN..", "...WWWWWW...", "..WW....WW..", "............" },
            new[] { ".....WW.....", ".....WP.....", "....WW......", "...WWWW.....", "..WWWWWWW...", ".WWWWWWWEW..", ".WWWWWWWWW..", "..WWWWWWWN..", "...WWWWW....", ".WW......WW.", "W..........W", "............" } } },
        { "candy", new[] {
            new[] { "............", "............", "............", "YY..CCCC..YY", "YYYCCSCCCYYY", "YYYCSCCCCYYY", "YY..CCCC..YY", "............", "............", "............", "............", "............" },
            new[] { "..........*.", ".........*.*", "..........*.", "YY..CCCC..YY", "YYYCCCSCCYYY", "YYYCCSCCCYYY", "YY..CCCC..YY", "............", "............", "............", "............", "............" } } },
        { "plane", new[] {
            new[] { "............", "............", ".........WW.", ".......WWWW.", ".....WWWWG..", "...WWWWWG...", ".WWWWWGG....", "....WGG.....", ".....G......", "............", "............", "............" },
            new[] { "............", ".........WW.", ".......WWWW.", ".....WWWWG..", "...WWWWWG...", ".WWWWWGG....", "....WGG.....", ".....G......", "............", "............", "............", "............" } } },
    };

    static Color ColorOf(string name, char ch)
    {
        switch (ch)
        {
            case 'B': return C("4A3A34");
            case 'E': return C("1E1614");
            case 'N': case 'P': return C("FF9CB4");
            case 'L': return C("FFF4FA");
            case 'S': return C("FFFFFF");
            case 'C': return C("FF7AB4");
            case '*': return C("FFF4A0");
            case 'G': return name == "plane" ? C("C8D0E0") : C("B8B8C0");
            case 'Y': return name == "candy" ? C("FFD84A") : C("FFD84A");
            default: return name == "butterfly" ? C("FF9CC8") : C("FFFFFF");
        }
    }

    public static string Run(string sheetPath)
    {
        using (Bitmap sheet = new Bitmap(2 * N * S, Names.Length * N * S, PixelFormat.Format32bppArgb))
        {
            for (int row = 0; row < Names.Length; row++)
            {
                string name = Names[row];
                for (int f = 0; f < 2; f++)
                {
                    string[] pat = Frames[name][f];
                    var c = new Color[N, N]; var m = new bool[N, N];
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                        {
                            char ch = pat[y][x];
                            if (ch == '.' || ch == '*') continue;
                            c[x, y] = ColorOf(name, ch); m[x, y] = true;
                        }
                    // outline around the body (sparkles stay without outline)
                    var add = new bool[N, N];
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                        {
                            if (m[x, y]) continue;
                            if ((x + 1 < N && m[x + 1, y]) || (x > 0 && m[x - 1, y]) || (y + 1 < N && m[x, y + 1]) || (y > 0 && m[x, y - 1])) add[x, y] = true;
                        }
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (add[x, y]) c[x, y] = Outline;
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (pat[y][x] == '*') c[x, y] = ColorOf(name, '*');

                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                        {
                            if (c[x, y].A == 0) continue;
                            for (int j = 0; j < S; j++) for (int i = 0; i < S; i++) sheet.SetPixel(f * N * S + x * S + i, row * N * S + y * S + j, c[x, y]);
                        }
                }
            }
            sheet.Save(sheetPath, ImageFormat.Png);
        }
        return "ok";
    }
}
