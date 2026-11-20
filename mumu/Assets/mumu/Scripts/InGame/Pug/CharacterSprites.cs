using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// キャラクターのパグのスプライト（Sprites/Pugs/{100 匹ずつのフォルダ}/{sprite}: _cry_N, _happy_N, _sit_N, _walk_N）
    /// 表示する子の分だけ読み込む（atlas にしない）。ずかんのアイコンは PugIcons
    /// </summary>
    public class CharacterSprites
    {
        public const string Folder = "Sprites/Pugs";
        /// <summary>1 フォルダに入れるキャラの数</summary>
        private const int PerFolder = 100;

        private static readonly Dictionary<string, CharacterSprites> cache = new Dictionary<string, CharacterSprites>();

        /// <summary>泣く</summary>
        public Sprite[] Cry { get; private set; }
        /// <summary>喜ぶ</summary>
        public Sprite[] Happy { get; private set; }
        /// <summary>座る</summary>
        public Sprite[] Sit { get; private set; }
        /// <summary>歩く（右向き）</summary>
        public Sprite[] Walk { get; private set; }

        /// <summary>アイコン（座りの1コマ目）</summary>
        public Sprite Icon => Sit[0];
        /// <summary>tier（5 以上はキラキラ）</summary>
        public int Tier { get; private set; }

        /// <summary>
        /// キャラクターの画像のパス（Resources から）
        /// </summary>
        /// <param name="_character"></param>
        public static string GetPath(CharacterData _character)
        {
            return $"{Folder}/{_character.Id / PerFolder * PerFolder:000}/{_character.Sprite}";
        }

        /// <summary>
        /// キャラクターの画像（初回のみ読み込み、足りなければ null）
        /// </summary>
        /// <param name="_character"></param>
        public static CharacterSprites Load(CharacterData _character)
        {
            if (_character == null || string.IsNullOrEmpty(_character.Sprite))
            {
                return null;
            }
            if (cache.TryGetValue(_character.Sprite, out CharacterSprites cached))
            {
                return cached;
            }

            Sprite[] all = ResourceManager.Instance.LoadAll<Sprite>(GetPath(_character));
            string prefix = _character.Sprite + "_";
            var sprites = new CharacterSprites
            {
                Cry = SpriteUtility.FilterByPrefix(all, prefix + "cry_"),
                Happy = SpriteUtility.FilterByPrefix(all, prefix + "happy_"),
                Sit = SpriteUtility.FilterByPrefix(all, prefix + "sit_"),
                Walk = SpriteUtility.FilterByPrefix(all, prefix + "walk_"),
                Tier = _character.Tier,
            };
            if (sprites.Cry.Length == 0 || sprites.Happy.Length == 0 || sprites.Sit.Length == 0 || sprites.Walk.Length == 0)
            {
                Debug.LogWarning($"[CharacterSprites] Sprites not found: {GetPath(_character)}");
                sprites = null;
            }
            cache[_character.Sprite] = sprites;
            return sprites;
        }
    }
}
