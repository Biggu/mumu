// 夜のテーマ（ID 20〜33 = 昼のテーマ 1〜14）を、昼の画像を暗く青寄りにして作る。C# 5 / .NET Framework（System.Drawing）
// 先に ThemeGen.Run と DecoSheetGen.Run で昼の画像（_2・_3 のシート含む）を作ってから呼ぶ。
// 画像のファイル名の テーマID 部分だけ付け替える（例: 000001_bg.png -> 000020_bg.png）。
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class NightGen
{
    const int DayCount = 14;
    const int NightOffset = 19;
    const int IdLength = 6;

    /// <summary>
    /// 昼の 1〜14 から夜の 20〜33 を作る（テーマ ID の付いたフォルダの中に書き出す）
    /// </summary>
    /// <param name="_themesFolder">Sprites/Themes（theme_XXXXXX の親）</param>
    public static void Run(string _themesFolder)
    {
        for (int day = 1; day <= DayCount; day++)
        {
            int night = day + NightOffset;
            string src = Path.Combine(_themesFolder, "theme_" + day.ToString("000000"));
            string dst = Path.Combine(_themesFolder, "theme_" + night.ToString("000000"));
            Directory.CreateDirectory(dst);
            foreach (string path in Directory.GetFiles(src, "*.png"))
            {
                // ファイル名の先頭（昼の ID）だけ夜の ID にする
                string name = Path.GetFileName(path);
                string newName = night.ToString("000000") + name.Substring(IdLength);
                darken(path, Path.Combine(dst, newName));
            }
            Console.WriteLine("night theme " + night + " <- day " + day);
        }
    }

    /// <summary>
    /// 1枚の画像を夜の色にする（赤と緑を下げ、青は少し上げる）。透明な部分はそのまま
    /// </summary>
    /// <param name="_src"></param>
    /// <param name="_dst"></param>
    static void darken(string _src, string _dst)
    {
        using (Bitmap src = new Bitmap(_src))
        using (Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    if (c.A == 0)
                    {
                        continue;
                    }
                    int r = (int)(c.R * 0.5);
                    int g = (int)(c.G * 0.55);
                    int b = (int)(c.B * 0.8) + 24;
                    dst.SetPixel(x, y, Color.FromArgb(c.A, clamp(r), clamp(g), clamp(b)));
                }
            }
            dst.Save(_dst, ImageFormat.Png);
        }
    }

    static int clamp(int _v)
    {
        return _v < 0 ? 0 : (_v > 255 ? 255 : _v);
    }
}
