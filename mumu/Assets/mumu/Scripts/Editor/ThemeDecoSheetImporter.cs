using System.IO;
using UnityEditor;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// テーマのコマ送り画像（_2・_3 のデコと _bg の背景、4コマのシート）を、スプライトシートとして取り込む設定にする
    /// </summary>
    [InitializeOnLoad]
    public static class ThemeDecoSheetImporter
    {
        private const string ThemesFolder = "Assets/mumu/Resources/Sprites/Themes";
        private const int ThemeCount = 33;
        private const int FrameCount = 4;
        private const int DecoFrameWidth = 169;
        private const int DecoFrameHeight = 281;
        private const int BackgroundFrameSize = 256;
        /// <summary>デコの番号（2〜9）。2〜5 は高いデコのコマ送り、6〜9 は普通・小さいデコの静止画</summary>
        private const int DecoFirstSlot = 2;
        private const int DecoLastSlot = 9;
        private const int DecoFirstStaticSlot = 6;
        private const string BackgroundSuffix = "_bg";

        /// <summary>
        /// コンパイル後に、取り込み設定がまだなら設定する
        /// </summary>
        static ThemeDecoSheetImporter()
        {
            EditorApplication.delayCall += apply;
        }

        /// <summary>
        /// 全テーマのコマ送り画像を設定する
        /// </summary>
        private static void apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            for (int theme = 1; theme <= ThemeCount; theme++)
            {
                string id = theme.ToString("000000");
                string folder = ThemesFolder + "/theme_" + id + "/";
                for (int slot = DecoFirstSlot; slot <= DecoLastSlot; slot++)
                {
                    string name = id + "_" + slot;
                    string path = folder + name + ".png";
                    if (!File.Exists(path))
                    {
                        continue;
                    }
                    if (slot < DecoFirstStaticSlot)
                    {
                        setup(path, name, DecoFrameWidth, DecoFrameHeight, SpriteAlignment.BottomCenter, new Vector2(0.5f, 0f));
                    }
                    else
                    {
                        setupSingle(path, name);
                    }
                }
                string bgName = id + BackgroundSuffix;
                if (File.Exists(folder + bgName + ".png"))
                {
                    setup(folder + bgName + ".png", bgName, BackgroundFrameSize, BackgroundFrameSize, SpriteAlignment.Center, new Vector2(0.5f, 0.5f));
                }
            }
        }

        /// <summary>
        /// 静止画のデコの設定（ドット絵、底辺をそろえる）。もう設定済みなら何もしない
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_name"></param>
        private static void setupSingle(string _path, string _name)
        {
            var importer = AssetImporter.GetAtPath(_path) as TextureImporter;
            if (importer == null)
            {
                return;
            }
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0f);
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 1枚のシートを、横に並んだ4コマに切る設定にする（もう設定済みなら何もしない）
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_name"></param>
        /// <param name="_frameWidth"></param>
        /// <param name="_frameHeight"></param>
        /// <param name="_alignment"></param>
        /// <param name="_pivot"></param>
        private static void setup(string _path, string _name, int _frameWidth, int _frameHeight, SpriteAlignment _alignment, Vector2 _pivot)
        {
            var importer = AssetImporter.GetAtPath(_path) as TextureImporter;
            if (importer == null)
            {
                return;
            }
            if (importer.spriteImportMode == SpriteImportMode.Multiple && importer.spritesheet.Length == FrameCount)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var sprites = new SpriteMetaData[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                sprites[i] = new SpriteMetaData
                {
                    name = _name + "_" + i,
                    rect = new Rect(i * _frameWidth, 0f, _frameWidth, _frameHeight),
                    pivot = _pivot,
                    alignment = (int)_alignment,
                };
            }
            importer.spritesheet = sprites;
            importer.SaveAndReimport();
        }
    }
}
