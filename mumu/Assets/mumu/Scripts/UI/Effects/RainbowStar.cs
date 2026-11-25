using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 虹色の星のスプライトシート（Resources/Sprites/Common/rainbow_star）。コマは名前の順に並ぶ
    /// </summary>
    public static class RainbowStar
    {
        /// <summary>スプライトシートのパス（Resources から）</summary>
        public const string SheetPath = "Sprites/Common/rainbow_star";
        /// <summary>キラッとさせるコマ送りの速さ（秒あたりのコマ数）</summary>
        public const float TwinkleFps = 10f;

        private static Sprite[] frames = null;

        /// <summary>
        /// 全コマ（キラッと動かす時に使う）
        /// </summary>
        public static Sprite[] Frames
        {
            get
            {
                if (frames == null)
                {
                    frames = ResourceManager.Instance.LoadAll<Sprite>(SheetPath);
                    Array.Sort(frames, (_a, _b) => string.CompareOrdinal(_a.name, _b.name));
                }
                return frames;
            }
        }

        /// <summary>
        /// 1コマだけの絵（動かさない時に使う）
        /// </summary>
        public static Sprite Still => Frames.Length > 0 ? Frames[0] : null;
    }
}
