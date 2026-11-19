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
    static readonly Color Outline = C("3A2A1E");

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
