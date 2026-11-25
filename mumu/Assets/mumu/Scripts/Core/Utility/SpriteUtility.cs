using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 共通処理（SpriteUtility）
    /// </summary>
    public static class SpriteUtility
    {
        /// <summary>
        /// 1回だけ読み込んで保存する（保存済みなら読み直さない）
        /// </summary>
        /// <param name="_cache"></param>
        /// <param name="_path"></param>
        public static Sprite LoadCached(ref Sprite _cache, string _path)
        {
            if (_cache == null)
            {
                _cache = ResourceManager.Instance.Load<Sprite>(_path);
            }
            return _cache;
        }


        /// <summary>
        /// パスごとに読み込んで保存する
        /// </summary>
        /// <param name="_cache"></param>
        /// <param name="_path"></param>
        public static Sprite LoadCached(Dictionary<string, Sprite> _cache, string _path)
        {
            if (!_cache.TryGetValue(_path, out Sprite sprite))
            {
                sprite = ResourceManager.Instance.Load<Sprite>(_path);
                _cache[_path] = sprite;
            }
            return sprite;
        }


        /// <summary>
        /// 名前の接頭辞で抽出して、名前順に並べる
        /// </summary>
        /// <param name="_sprites"></param>
        /// <param name="_prefix"></param>
        public static Sprite[] FilterByPrefix(Sprite[] _sprites, string _prefix)
        {
            var result = new List<Sprite>();
            foreach (Sprite sprite in _sprites)
            {
                if (sprite.name.StartsWith(_prefix, StringComparison.Ordinal))
                {
                    result.Add(sprite);
                }
            }
            result.Sort((_a, _b) => string.CompareOrdinal(_a.name, _b.name));
            return result.ToArray();
        }

    }
}
