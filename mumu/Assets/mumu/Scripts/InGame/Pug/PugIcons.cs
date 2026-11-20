using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ずかん用のパグのアイコン（Sprites/PugIcons/pug_icons_N の icon_XXX。全キャラ分が2枚ほどにまとまっている）
    /// </summary>
    public static class PugIcons
    {
        private const string Folder = "Sprites/PugIcons";
        private const string Prefix = "icon_";

        private static Dictionary<int, Sprite> icons = null;

        /// <summary>
        /// アイコン（なければ null）
        /// </summary>
        /// <param name="_characterId"></param>
        public static Sprite Get(int _characterId)
        {
            if (icons == null)
            {
                icons = new Dictionary<int, Sprite>();
                foreach (Sprite sprite in Resources.LoadAll<Sprite>(Folder))
                {
                    if (sprite.name.StartsWith(Prefix) && int.TryParse(sprite.name.Substring(Prefix.Length), out int id))
                    {
                        icons[id] = sprite;
                    }
                }
            }
            return icons.TryGetValue(_characterId, out Sprite icon) ? icon : null;
        }
    }
}