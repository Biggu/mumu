using System.Collections.Generic;

namespace mumu
{
    /// <summary>
    /// 仲間になったパグ（1匹ずつ、同じ子をまたもらったらほねになる。SaveDataManager に保存）
    /// </summary>
    public static class PugCollection
    {
        /// <summary>男の子（いつもの主人公）を連れていく時の ID</summary>
        public const int DefaultPlayerId = 0;

        /// <summary>仲間のキャラクター ID（仲間になった順）</summary>
        public static IReadOnlyList<int> Owned => SaveDataManager.Data.pugs;

        /// <summary>冒険・おさんぽで連れていくパグ（0 = 男の子）</summary>
        public static int EquippedId => IsOwned(SaveDataManager.Data.equippedPugId) ? SaveDataManager.Data.equippedPugId : DefaultPlayerId;

        /// <summary>
        /// 持っているか
        /// </summary>
        /// <param name="_characterId"></param>
        public static bool IsOwned(int _characterId)
        {
            return SaveDataManager.Data.pugs.Contains(_characterId);
        }

        /// <summary>
        /// 仲間に加える（もう持っていたら代わりにほねをもらう。もらったほねを返す、新しい子なら 0）
        /// </summary>
        /// <param name="_characterId"></param>
        public static int Add(int _characterId)
        {
            UserSaveData data = SaveDataManager.Data;
            int bones = 0;
            if (data.pugs.Contains(_characterId))
            {
                CharacterData character = CharacterMaster.Get(_characterId);
                bones = character != null ? character.DuplicateBones : 0;
                data.bones += bones;
            }
            else
            {
                data.pugs.Add(_characterId);
                if (data.gardenPugs.Count < UserSaveData.MaxGarden)
                {
                    data.gardenPugs.Add(_characterId);
                }
            }
            SaveDataManager.Save();
            return bones;
        }

        /// <summary>おにわにいる子（持っている子だけ）</summary>
        public static IReadOnlyList<int> GardenIds => SaveDataManager.Data.gardenPugs;

        /// <summary>おにわにいる子か</summary>
        /// <param name="_characterId"></param>
        public static bool IsInGarden(int _characterId)
        {
            return SaveDataManager.Data.gardenPugs.Contains(_characterId);
        }

        /// <summary>
        /// おにわに入れる（持っていて、上限に空きがある時だけ）。入れたら true
        /// </summary>
        /// <param name="_characterId"></param>
        public static bool AddToGarden(int _characterId)
        {
            UserSaveData data = SaveDataManager.Data;
            if (!IsOwned(_characterId) || data.gardenPugs.Contains(_characterId) || data.gardenPugs.Count >= UserSaveData.MaxGarden)
            {
                return false;
            }
            data.gardenPugs.Add(_characterId);
            SaveDataManager.Save();
            return true;
        }

        /// <summary>
        /// おにわから出す（1匹は残すので、最後の1匹は出せない）。出せたら true
        /// </summary>
        /// <param name="_characterId"></param>
        public static bool RemoveFromGarden(int _characterId)
        {
            UserSaveData data = SaveDataManager.Data;
            if (!data.gardenPugs.Contains(_characterId) || data.gardenPugs.Count <= 1)
            {
                return false;
            }
            data.gardenPugs.Remove(_characterId);
            if (data.equippedPugId == _characterId)
            {
                data.equippedPugId = DefaultPlayerId;
            }
            SaveDataManager.Save();
            return true;
        }

        /// <summary>
        /// 冒険・おさんぽで連れていくパグを決める（0 で男の子に戻す）
        /// </summary>
        /// <param name="_characterId"></param>
        public static void Equip(int _characterId)
        {
            SaveDataManager.Data.equippedPugId = _characterId == DefaultPlayerId || IsOwned(_characterId) ? _characterId : DefaultPlayerId;
            SaveDataManager.Save();
        }

        /// <summary>
        /// 持っている数（1匹ずつなので 0 か 1）
        /// </summary>
        /// <param name="_characterId"></param>
        public static int Count(int _characterId)
        {
            return IsOwned(_characterId) ? 1 : 0;
        }
    }
}