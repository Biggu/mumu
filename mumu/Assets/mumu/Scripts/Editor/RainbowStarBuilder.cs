using System.IO;
using UnityEditor;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 虹色の星のスプライトシート（Resources/Sprites/Common/rainbow_star.png）を作る。ui_star_on と同じ大きさ・ドット絵風。未作成ならコンパイル後に自動作成。作り直す時は mumu > Build Rainbow Star
    /// </summary>
    [InitializeOnLoad]
    public static class RainbowStarBuilder
    {
        /// <summary>スプライトシートのパス（Assets から）</summary>
        public const string TexturePath = "Assets/mumu/Resources/Sprites/Common/rainbow_star.png";
        /// <summary>1コマの大きさ（ui_star_on と同じ 64px）</summary>
        private const int CellSize = 64;
        /// <summary>コマ数（1周期でキラッと光る）</summary>
        private const int FrameCount = 8;
        /// <summary>星の外側の半径（ピクセル）</summary>
        private const float BaseRadius = 26f;
        /// <summary>輪郭の太さ（ピクセル）</summary>
        private const float OutlineWidth = 1.2f;
        private const string StampKey = "RainbowStarBuilder";

        /// <summary>輪郭の色（ui_star_on と同じ）</summary>
        private static readonly Color32 OutlineColor = new Color32(0x3A, 0x2A, 0x1E, 0xFF);
        /// <summary>キラッの色（ui_star_on と同じ白）</summary>
        private static readonly Color32 SparkleColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        /// <summary>虹の帯の色（ドット絵らしく、なめらかにせず色を分ける）</summary>
        private static readonly Color32[] RainbowColors =
        {
            new Color32(0xFF, 0x5A, 0x5A, 0xFF),
            new Color32(0xFF, 0xA0, 0x40, 0xFF),
            new Color32(0xFF, 0xD0, 0x30, 0xFF),
            new Color32(0x6B, 0xD6, 0x6B, 0xFF),
            new Color32(0x5A, 0xAF, 0xFF, 0xFF),
            new Color32(0xB0, 0x70, 0xFF, 0xFF),
        };

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static RainbowStarBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (!EditorBuildStamp.NeedsBuild(StampKey, true, new[] { TexturePath }))
            {
                return;
            }
            Build();
        }

        /// <summary>
        /// スプライトシートを作り直す
        /// </summary>
        [MenuItem("mumu/Build Rainbow Star")]
        public static void Build()
        {
            int width = CellSize * FrameCount;
            var texture = new Texture2D(width, CellSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (int i = 0; i < FrameCount; i++)
            {
                drawFrame(texture, i);
            }
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(Path.Combine(Application.dataPath, TexturePath.Substring("Assets/".Length)), png);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            setupImporter();

            EditorBuildStamp.Save(StampKey);
            Debug.Log("[RainbowStarBuilder] Rainbow star sprite sheet built");
        }

        /// <summary>
        /// 1コマ描く（虹の帯が流れる星。輪郭は暗い色、先の白いキラッは点滅で出たり消えたり）
        /// </summary>
        /// <param name="_texture"></param>
        /// <param name="_index"></param>
        private static void drawFrame(Texture2D _texture, int _index)
        {
            float t = (float)_index / FrameCount;
            float twinkle = 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI);
            // 大きさは1ピクセル単位で変える（ドット絵のまま）
            float outer = BaseRadius + Mathf.Round(twinkle);
            Vector2[] star = starPoints(outer, outer * 0.5f);
            float center = CellSize * 0.5f;

            for (int y = 0; y < CellSize; y++)
            {
                for (int x = 0; x < CellSize; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - center, y + 0.5f - center);
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (insidePolygon(p, star))
                    {
                        color = distanceToBoundary(p, star) <= OutlineWidth ? OutlineColor : rainbowAt(p, outer, t);
                    }
                    _texture.SetPixel(_index * CellSize + x, y, color);
                }
            }

            // 先のキラッ（点滅が強い時だけ、ピクセルの十字）
            if (twinkle > 0.4f)
            {
                int arm = twinkle > 0.7f ? 2 : 1;
                Vector2 tip = new Vector2(Mathf.Cos(-54f * Mathf.Deg2Rad), Mathf.Sin(-54f * Mathf.Deg2Rad)) * (outer + 5f);
                drawSparkle(_texture, _index, center + Mathf.Round(tip.x), center + Mathf.Round(tip.y), arm);
            }
        }

        /// <summary>
        /// 斜めに流れる虹の帯の色（色の境目はくっきり）
        /// </summary>
        /// <param name="_p"></param>
        /// <param name="_outer"></param>
        /// <param name="_t"></param>
        private static Color32 rainbowAt(Vector2 _p, float _outer, float _t)
        {
            float flow = Mathf.Repeat((_p.x - _p.y) / (_outer * 2f) + _t, 1f);
            return RainbowColors[Mathf.FloorToInt(flow * RainbowColors.Length) % RainbowColors.Length];
        }

        /// <summary>
        /// 白い十字のキラッ（中心から腕の長さ分）
        /// </summary>
        /// <param name="_texture"></param>
        /// <param name="_index"></param>
        /// <param name="_cx"></param>
        /// <param name="_cy"></param>
        /// <param name="_arm"></param>
        private static void drawSparkle(Texture2D _texture, int _index, float _cx, float _cy, int _arm)
        {
            for (int d = -_arm; d <= _arm; d++)
            {
                setSparkle(_texture, _index, Mathf.RoundToInt(_cx) + d, Mathf.RoundToInt(_cy));
                setSparkle(_texture, _index, Mathf.RoundToInt(_cx), Mathf.RoundToInt(_cy) + d);
            }
        }

        /// <summary>
        /// キラッの1点（コマの中だけ）
        /// </summary>
        /// <param name="_texture"></param>
        /// <param name="_index"></param>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private static void setSparkle(Texture2D _texture, int _index, int _x, int _y)
        {
            if (_x < 0 || _x >= CellSize || _y < 0 || _y >= CellSize)
            {
                return;
            }
            _texture.SetPixel(_index * CellSize + _x, _y, SparkleColor);
        }

        /// <summary>
        /// 星の頂点（上から、外と内を交互に10個）
        /// </summary>
        /// <param name="_outer"></param>
        /// <param name="_inner"></param>
        private static Vector2[] starPoints(float _outer, float _inner)
        {
            var points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                float radius = i % 2 == 0 ? _outer : _inner;
                float angle = (-90f + i * 36f) * Mathf.Deg2Rad;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        /// <summary>
        /// 点が多角形の中か（交差の数で判定）
        /// </summary>
        /// <param name="_p"></param>
        /// <param name="_polygon"></param>
        private static bool insidePolygon(Vector2 _p, Vector2[] _polygon)
        {
            bool inside = false;
            for (int i = 0, j = _polygon.Length - 1; i < _polygon.Length; j = i++)
            {
                bool crosses = (_polygon[i].y > _p.y) != (_polygon[j].y > _p.y);
                if (crosses && _p.x < (_polygon[j].x - _polygon[i].x) * (_p.y - _polygon[i].y) / (_polygon[j].y - _polygon[i].y) + _polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// 点から多角形の辺までの一番近い距離
        /// </summary>
        /// <param name="_p"></param>
        /// <param name="_polygon"></param>
        private static float distanceToBoundary(Vector2 _p, Vector2[] _polygon)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _polygon.Length; i++)
            {
                Vector2 a = _polygon[i];
                Vector2 b = _polygon[(i + 1) % _polygon.Length];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(_p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(_p, a + ab * t));
            }
            return best;
        }

        /// <summary>
        /// スプライトシートとして切る（コマごとに名前 rainbow_star_0 … を付ける）。ドット絵なので拡大縮小しても滲まない
        /// </summary>
        private static void setupImporter()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
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
                    name = "rainbow_star_" + i,
                    rect = new Rect(i * CellSize, 0f, CellSize, CellSize),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center,
                };
            }
            importer.spritesheet = sprites;
            importer.SaveAndReimport();
        }
    }
}
