using UnityEngine;

namespace mumu
{
    /// <summary>
    /// パグのスプライト（Sprites/Common/pug。Goal の女の子と連れていく男の子はキャラクターに差し替えられる）
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
                GirlCry = SpriteUtility.FilterByPrefix(all, GirlCryPrefix),
                GirlHappy = SpriteUtility.FilterByPrefix(all, GirlHappyPrefix),
                GirlSit = SpriteUtility.FilterByPrefix(all, GirlSitPrefix),
                BoySit = SpriteUtility.FilterByPrefix(all, BoySitPrefix),
                BoyRun = SpriteUtility.FilterByPrefix(all, BoyRunPrefix),
                BoyHappy = SpriteUtility.FilterByPrefix(all, BoyHappyPrefix),
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
    }
}
