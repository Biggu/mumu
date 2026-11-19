using System;
using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// パグのスプライト（Sprites/Common/pug、Goal の女の子はキャラクターに差し替えられる）
    /// </summary>
    public class PugSprites
    {
        public const string SheetPath = "Sprites/Common/pug";
        public const string GirlCryPrefix = "pug_girl_cry_";
        private const string GirlHappyPrefix = "pug_girl_happy_";
        private const string GirlSitPrefix = "pug_girl_sit_";
        private const string BoySitPrefix = "pug_boy_sit_";
        private const string BoyRunPrefix = "pug_boy_run_";
        private const string BoyHappyPrefix = "pug_boy_happy_";

        /// <summary>Goal で泣いている女の子</summary>
        public Sprite[] GirlCry { get; private set; }
        /// <summary>クリア時に喜ぶ女の子</summary>
        public Sprite[] GirlHappy { get; private set; }
        /// <summary>男の子が近づいた時の女の子（泣きやんでこっちを見る）</summary>
        public Sprite[] GirlSit { get; private set; }
        /// <summary>経路の先端で座っている男の子</summary>
        public Sprite[] BoySit { get; private set; }
        /// <summary>走る男の子（右向き）</summary>
        public Sprite[] BoyRun { get; private set; }
        /// <summary>クリア時に喜ぶ男の子</summary>
        public Sprite[] BoyHappy { get; private set; }
        /// <summary>Goal の子の tier（5 以上はキラキラ、いつもの女の子は 0）</summary>
        public int GirlTier { get; private set; }
        /// <summary>連れていくパグの tier（5 以上はキラキラ、男の子は 0）</summary>
        public int BoyTier { get; private set; }

        /// <summary>
        /// 読み込み（コマが足りなければ null）
        /// </summary>
        public static PugSprites Load()
        {
            Sprite[] all = ResourceManager.Instance.LoadAll<Sprite>(SheetPath);
            var sprites = new PugSprites
            {
                GirlCry = Filter(all, GirlCryPrefix),
                GirlHappy = Filter(all, GirlHappyPrefix),
                GirlSit = Filter(all, GirlSitPrefix),
                BoySit = Filter(all, BoySitPrefix),
                BoyRun = Filter(all, BoyRunPrefix),
                BoyHappy = Filter(all, BoyHappyPrefix),
            };

            if (sprites.GirlCry.Length == 0 || sprites.GirlHappy.Length == 0 || sprites.BoySit.Length == 0 || sprites.BoyRun.Length == 0 || sprites.BoyHappy.Length == 0)
            {
                Debug.LogWarning($"[PugSprites] Sprites not found: {SheetPath}");
                return null;
            }
            if (sprites.GirlSit.Length == 0)
            {
                sprites.GirlSit = sprites.GirlCry;
            }
            return sprites;
        }

        /// <summary>
        /// Goal の女の子をキャラクターに差し替えた複製
        /// </summary>
        /// <param name="_character"></param>
        public PugSprites WithGirl(CharacterSprites _character)
        {
            var sprites = (PugSprites)MemberwiseClone();
            if (_character != null)
            {
                sprites.GirlCry = _character.Cry;
                sprites.GirlHappy = _character.Happy;
                sprites.GirlSit = _character.Sit;
                sprites.GirlTier = _character.Tier;
            }
            return sprites;
        }

        /// <summary>
        /// 主人公（経路を走る子）を連れていくパグに差し替えた複製（座る・歩く＝走る・喜ぶ）
        /// </summary>
        /// <param name="_character"></param>
        public PugSprites WithBoy(CharacterSprites _character)
        {
            var sprites = (PugSprites)MemberwiseClone();
            if (_character != null)
            {
                sprites.BoySit = _character.Sit;
                sprites.BoyRun = _character.Walk;
                sprites.BoyHappy = _character.Happy;
                sprites.BoyTier = _character.Tier;
            }
            return sprites;
        }

        /// <summary>
        /// 名前の接頭辞で抽出して番号順に並べる
        /// </summary>
        /// <param name="_sprites"></param>
        /// <param name="_prefix"></param>
        public static Sprite[] Filter(Sprite[] _sprites, string _prefix)
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

    /// <summary>
    /// キャラクターのパグのスプライト（Sprites/Pugs/{100 匹ずつのフォルダ}/{sprite}: _cry_N, _happy_N, _sit_N, _walk_N）
    /// 表示する子の分だけ読み込む（atlas にしない）。ずかんのアイコンは PugIcons
    /// </summary>
    public class CharacterSprites
    {
        public const string Folder = "Sprites/Pugs";
        /// <summary>1 フォルダに入れるキャラの数</summary>
        private const int PerFolder = 100;

        /// <summary>
        /// キャラの画像のパス（Resources から）
        /// </summary>
        /// <param name="_character"></param>
        public static string GetPath(CharacterData _character)
        {
            return $"{Folder}/{_character.Id / PerFolder * PerFolder:000}/{_character.Sprite}";
        }

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
                Cry = PugSprites.Filter(all, prefix + "cry_"),
                Happy = PugSprites.Filter(all, prefix + "happy_"),
                Sit = PugSprites.Filter(all, prefix + "sit_"),
                Walk = PugSprites.Filter(all, prefix + "walk_"),
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
