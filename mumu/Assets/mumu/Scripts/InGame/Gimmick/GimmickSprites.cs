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

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// ギミックの画像を中央に作る
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        public static Image CreateImage(string _name, Transform _parent, string _sprite, float _size)
        {
            return UiFactory.CreateImage(_name, _parent, SpriteUtility.LoadCached(cache, Folder + _sprite), _size);
        }
    }
}
