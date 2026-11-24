using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ギミックの画像（Sprites/Common/gimmick_*）の読み込みと作成
    /// </summary>
    public static class GimmickSprites
    {
        private const string Folder = "Sprites/Common/";

        /// <summary>コマ送りの速さ（1秒あたりのコマ数）</summary>
        private const float FrameFps = 6f;

        private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

        /// <summary>
        /// ギミックの画像を中央に作る（コマが複数あれば コマ送りする）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        /// <param name="_isAnimated">コマ送りするか（false なら1コマ目のまま）</param>
        public static Image CreateImage(string _name, Transform _parent, string _sprite, float _size, bool _isAnimated = true)
        {
            Sprite[] frames = LoadFrames(_sprite);
            Image image = UiFactory.CreateImage(_name, _parent, frames.Length > 0 ? frames[0] : null, _size);
            if (_isAnimated && frames.Length > 1)
            {
                image.gameObject.AddComponent<SpriteFrameAnimation>().Play(frames, FrameFps);
            }
            return image;
        }

        /// <summary>
        /// 画像のコマを名前順に読む（シート は name_0, name_1 … のコマ。1枚絵なら1コマ）
        /// </summary>
        /// <param name="_sprite"></param>
        public static Sprite[] LoadFrames(string _sprite)
        {
            if (!cache.TryGetValue(_sprite, out Sprite[] frames))
            {
                Sprite[] all = ResourceManager.Instance.LoadAll<Sprite>(Folder + _sprite);
                frames = SpriteUtility.FilterByPrefix(all ?? new Sprite[0], _sprite + "_");
                if (frames.Length == 0)
                {
                    Sprite single = ResourceManager.Instance.Load<Sprite>(Folder + _sprite);
                    frames = single != null ? new[] { single } : new Sprite[0];
                }
                cache[_sprite] = frames;
            }
            return frames;
        }
    }
}
