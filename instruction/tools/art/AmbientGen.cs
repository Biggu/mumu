// Ambient particle textures (C# 5 / System.Drawing). Pixel art at 1 px per texel; the game point-filters them.
//   ambient_square : 4x4 solid square (leaves, petals, sparkle, embers)
//   ambient_round  : 6x6 round dot (snow, bubbles, fireflies)
// The particle colour comes from the ParticleSystem, so these are white.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class AmbientGen
{
    const int PreviewScale = 8;

    static void Save(string[] pat, string path, string previewPath)
    {
        int h = pat.Length, w = pat[0].Length;
        using (var b = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        using (var p = new Bitmap(w * PreviewScale, h * PreviewScale, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool on = pat[y][x] == 'X';
                    Color c = on ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(0, 0, 0, 0);
                    b.SetPixel(x, y, c);
                    for (int j = 0; j < PreviewScale; j++)
                    {
                        for (int i = 0; i < PreviewScale; i++)
                        {
                            p.SetPixel(x * PreviewScale + i, y * PreviewScale + j, on ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 60, 60, 60));
                        }
                    }
                }
            }
            b.Save(path, ImageFormat.Png);
            p.Save(previewPath, ImageFormat.Png);
        }
    }

    /// <summary>
    /// 粒の画像を out フォルダに作る（プレビューは *_preview.png）
    /// </summary>
    public static void Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        Save(new[] { "XXXX", "XXXX", "XXXX", "XXXX" },
            Path.Combine(outDir, "ambient_square.png"), Path.Combine(outDir, "ambient_square_preview.png"));
        Save(new[] { ".XXXX.", "XXXXXX", "XXXXXX", "XXXXXX", "XXXXXX", ".XXXX." },
            Path.Combine(outDir, "ambient_round.png"), Path.Combine(outDir, "ambient_round_preview.png"));
    }
}
