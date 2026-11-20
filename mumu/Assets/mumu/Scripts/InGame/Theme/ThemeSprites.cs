using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップのテーマ画像
    /// Sprites/Themes/theme_{id:000000}/{id:000000}_{番号}: 0 = 未通過, 1 = 通過済み, 2〜5 = デコ1〜4（透明背景）, 6 = 水, 7 = 通れないブロック, 8 = 橋, bg = 背景
    /// デコ1・2（_2・_3）はコマ送りのスプライトシート（4コマ）
    /// </summary>
    public class ThemeSprites
    {
        public const int UntouchedIndex = 0;
        public const int TouchedIndex = 1;
        public const int DecoStartIndex = 2;
        public const int DecoCount = 4;
        /// <summary>デコのうち、最初の何個をコマ送りにするか（_2・_3）</summary>
        public const int AnimatedDecoCount = 2;
        /// <summary>コマ送りの速さ（秒あたりのコマ数）</summary>
        public const float DecoFps = 6f;
        public const int WaterIndex = 6;
        public const int ObstructIndex = 7;
        public const int BridgeIndex = 8;
        public const string BackgroundSuffix = "bg";

        /// <summary>未通過</summary>
        public Sprite Untouched { get; private set; }
        /// <summary>通過済み</summary>
        public Sprite Touched { get; private set; }
        /// <summary>通れないブロック（デコの土台）</summary>
        public Sprite Obstruct { get; private set; }
        /// <summary>通れないブロックの上のデコ（コマ送りのデコは1コマ目）</summary>
        public Sprite[] Decos { get; private set; }
        /// <summary>デコのコマ送りの絵（静止画のデコは null）</summary>
        public Sprite[][] DecoFrames { get; private set; }
        /// <summary>水</summary>
        public Sprite Water { get; private set; }
        /// <summary>橋</summary>
        public Sprite Bridge { get; private set; }
        /// <summary>背景タイル（1コマ目）</summary>
        public Sprite Background { get; private set; }
        /// <summary>背景のコマ送りの絵（きらめき・4コマ）</summary>
        public Sprite[] BackgroundFrames { get; private set; }

        /// <summary>
        /// デコ画像（コマ送りのデコは1コマ目）
        /// </summary>
        /// <param name="_index"></param>
        public Sprite GetDeco(int _index)
        {
            return Decos[Mathf.Clamp(_index, 0, DecoCount - 1)];
        }

        /// <summary>
        /// デコのコマ送りの絵（静止画なら null）
        /// </summary>
        /// <param name="_index"></param>
        public Sprite[] GetDecoFrames(int _index)
        {
            return DecoFrames[Mathf.Clamp(_index, 0, DecoCount - 1)];
        }

        /// <summary>
        /// テーマ画像を読み込む（不足があれば null）
        /// </summary>
        /// <param name="_themeId"></param>
        /// <param name="_token"></param>
        public static async UniTask<ThemeSprites> LoadAsync(int _themeId, CancellationToken _token = default)
        {
            const int fixedCount = 5;
            int staticCount = DecoCount - AnimatedDecoCount;
            var loads = new UniTask<Sprite>[fixedCount + staticCount];
            loads[0] = load(_themeId, UntouchedIndex.ToString(), _token);
            loads[1] = load(_themeId, TouchedIndex.ToString(), _token);
            loads[2] = load(_themeId, ObstructIndex.ToString(), _token);
            loads[3] = load(_themeId, WaterIndex.ToString(), _token);
            loads[4] = load(_themeId, BridgeIndex.ToString(), _token);
            for (int i = 0; i < staticCount; i++)
            {
                loads[fixedCount + i] = load(_themeId, (DecoStartIndex + AnimatedDecoCount + i).ToString(), _token);
            }

            Sprite[] results = await UniTask.WhenAll(loads);
            foreach (Sprite sprite in results)
            {
                if (sprite == null)
                {
                    return null;
                }
            }

            Sprite[] backgroundFrames = loadFrames(_themeId, BackgroundSuffix);
            if (backgroundFrames == null)
            {
                return null;
            }

            var sprites = new ThemeSprites
            {
                Untouched = results[0],
                Touched = results[1],
                Obstruct = results[2],
                Water = results[3],
                Bridge = results[4],
                Background = backgroundFrames[0],
                BackgroundFrames = backgroundFrames,
                Decos = new Sprite[DecoCount],
                DecoFrames = new Sprite[DecoCount][],
            };
            for (int i = 0; i < AnimatedDecoCount; i++)
            {
                Sprite[] frames = loadFrames(_themeId, (DecoStartIndex + i).ToString());
                if (frames == null)
                {
                    return null;
                }
                sprites.DecoFrames[i] = frames;
                sprites.Decos[i] = frames[0];
            }
            for (int i = 0; i < staticCount; i++)
            {
                sprites.Decos[AnimatedDecoCount + i] = results[fixedCount + i];
            }
            return sprites;
        }

        /// <summary>
        /// スプライトシートのコマを名前の順に読み込む（無ければ null）
        /// </summary>
        /// <param name="_themeId"></param>
        /// <param name="_suffix"></param>
        private static Sprite[] loadFrames(int _themeId, string _suffix)
        {
            Sprite[] frames = ResourceManager.Instance.LoadAll<Sprite>(ResourceManager.GetThemeSpritePath(_themeId, _suffix));
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[ThemeSprites] コマが見つかりません: theme {_themeId} / {_suffix}");
                return null;
            }
            System.Array.Sort(frames, (_a, _b) => string.CompareOrdinal(_a.name, _b.name));
            return frames;
        }

        /// <summary>
        /// テーマ画像を1枚読み込む
        /// </summary>
        /// <param name="_themeId"></param>
        /// <param name="_suffix"></param>
        /// <param name="_token"></param>
        private static UniTask<Sprite> load(int _themeId, string _suffix, CancellationToken _token)
        {
            return ResourceManager.Instance.LoadAsync<Sprite>(ResourceManager.GetThemeSpritePath(_themeId, _suffix), _token);
        }
    }
}
