using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// パグのキャラクター（character.csv の1行）
    /// </summary>
    public class CharacterData
    {
        /// <summary>キャラクター ID</summary>
        public int Id;
        /// <summary>名前（日本語）</summary>
        public string NameJa;
        /// <summary>名前（英語）</summary>
        public string NameEn;
        /// <summary>画像（Resources/Sprites/Pugs/{Sprite}）</summary>
        public string Sprite;
        /// <summary>ランク（1〜6、上ほどおさんぽで出にくく、ほねの交換が高い。6 はとくべつで交換できない）</summary>
        public int Tier;

        /// <summary>表示名（今の言語）</summary>
        public string Name => TextLocalizer.IsJapanese() ? NameJa : NameEn;
        /// <summary>ほねで交換する時の値段（交換できなければ 0）</summary>
        public int Price => CharacterMaster.GetPrice(Tier);
        /// <summary>ほねで交換できるか（tier が交換できるもので、まだ持っていない）</summary>
        public bool IsForSale => Price > 0;
        /// <summary>もう持っている子をもらった時に代わりにもらえるほね</summary>
        public int DuplicateBones => CharacterMaster.GetDuplicateBones(Tier);
        /// <summary>とくべつな子か（持つまでずかんでもシルエット）</summary>
        public bool IsSecret => Tier >= CharacterMaster.SecretTier;
    }

    /// <summary>
    /// キャラクターのマスターデータ（Resources/MasterData/character.csv）
    /// character.csv: id,name_ja,name_en,sprite,tier,fur,outfit（fur と outfit は画像作成用のメモ）
    /// 手に入れ方: おさんぽのクリア時（StrollRewardRate で当たり、tier の重みで選ぶ）と、ずかんでほねと交換
    /// </summary>
    public static class CharacterMaster
    {
        public const string Folder = "MasterData";
        public const int MinTier = 1;
        public const int MaxTier = 6;
        /// <summary>この tier 以上はとくべつ（交換できず、持つまでずかんでシルエット）</summary>
        public const int SecretTier = 6;
        /// <summary>おさんぽでクリア時にパグがもらえる確率</summary>
        public const float StrollRewardRate = 0.2f;
        /// <summary>tier ごとのほねの値段（tier1〜6、0 = 交換できない）</summary>
        private static readonly int[] TierPrices = { 200, 500, 1000, 5000, 20000, 0 };
        /// <summary>
        /// tier ごとのおさんぽで出る重み（tier1〜6、合計 100000 = 0.001% 単位）。
        /// tier5 = 0.5%、tier6 = 0.1%（2匹なので1匹 0.05%）
        /// </summary>
        private static readonly int[] TierWeights = { 59400, 28000, 10000, 2000, 500, 100 };
        /// <summary>もう持っている子をもらった時のほね（tier1〜6）</summary>
        private static readonly int[] DuplicateBonesByTier = { 5, 10, 20, 50, 500, 10000 };

        private static List<CharacterData> characters = null;

        /// <summary>全キャラクター（ID 順）</summary>
        public static IReadOnlyList<CharacterData> All
        {
            get
            {
                load();
                return characters;
            }
        }

        /// <summary>
        /// キャラクターを取得（なければ null）
        /// </summary>
        /// <param name="_id"></param>
        public static CharacterData Get(int _id)
        {
            load();
            return characters.Find(_c => _c.Id == _id);
        }

        /// <summary>
        /// tier の値段
        /// </summary>
        /// <param name="_tier"></param>
        public static int GetPrice(int _tier)
        {
            return TierPrices[Mathf.Clamp(_tier, MinTier, MaxTier) - 1];
        }

        /// <summary>
        /// もう持っている子をもらった時のほね
        /// </summary>
        /// <param name="_tier"></param>
        public static int GetDuplicateBones(int _tier)
        {
            return DuplicateBonesByTier[Mathf.Clamp(_tier, MinTier, MaxTier) - 1];
        }

        /// <summary>
        /// おさんぽの報酬を抽選（StrollRewardRate で当たり、tier の重みで選んで、その tier から均等に。外れは 0。
        /// _random は日替わりのシードで同じ結果にするため）
        /// </summary>
        /// <param name="_random"></param>
        public static int RollStrollReward(System.Random _random)
        {
            load();
            if (_random.NextDouble() >= StrollRewardRate)
            {
                return 0;
            }

            // キャラがいない tier は抽選から外す
            int total = 0;
            for (int tier = MinTier; tier <= MaxTier; tier++)
            {
                total += hasTier(tier) ? TierWeights[tier - 1] : 0;
            }
            if (total <= 0)
            {
                return 0;
            }

            int roll = _random.Next(total);
            for (int tier = MinTier; tier <= MaxTier; tier++)
            {
                if (!hasTier(tier))
                {
                    continue;
                }
                roll -= TierWeights[tier - 1];
                if (roll < 0)
                {
                    List<CharacterData> candidates = characters.FindAll(_c => _c.Tier == tier);
                    return candidates[_random.Next(candidates.Count)].Id;
                }
            }
            return 0;
        }

        /// <summary>
        /// ほねと交換する（交換できない・持っている・足りなければ false）
        /// </summary>
        /// <param name="_characterId"></param>
        public static bool Exchange(int _characterId)
        {
            CharacterData character = Get(_characterId);
            if (character == null || !character.IsForSale || PugCollection.IsOwned(_characterId) || SaveDataManager.Data.bones < character.Price)
            {
                return false;
            }

            SaveDataManager.Data.bones -= character.Price;
            PugCollection.Add(_characterId);
            return true;
        }

        /// <summary>
        /// その tier のキャラがいるか
        /// </summary>
        /// <param name="_tier"></param>
        private static bool hasTier(int _tier)
        {
            return characters.Exists(_c => _c.Tier == _tier);
        }

        /// <summary>
        /// 初回のみ CSV を読み込む
        /// </summary>
        private static void load()
        {
            if (characters != null)
            {
                return;
            }

            characters = new List<CharacterData>();
            var asset = Resources.Load<TextAsset>($"{Folder}/character");
            if (asset == null)
            {
                Debug.LogError($"[CharacterMaster] CSV not found: {Folder}/character");
                return;
            }

            List<string[]> rows = CsvUtility.Parse(asset.text);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 5 || !int.TryParse(row[0], out int id))
                {
                    continue;
                }
                characters.Add(new CharacterData
                {
                    Id = id,
                    NameJa = row[1],
                    NameEn = row[2],
                    Sprite = row[3],
                    Tier = int.TryParse(row[4], out int tier) ? Mathf.Clamp(tier, MinTier, MaxTier) : MinTier,
                });
            }
            characters.Sort((_a, _b) => _a.Id.CompareTo(_b.Id));
        }
    }
}
