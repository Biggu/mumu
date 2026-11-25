using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ギミックの説明（gimmick.csv の1行）
    /// </summary>
    public class GimmickData
    {
        /// <summary>ギミック ID</summary>
        public int Id;
        /// <summary>キー（arrow / turn / straight / bone）</summary>
        public string Key;
        public string NameJa;
        public string NameEn;
        public string DescriptionJa;
        public string DescriptionEn;
        /// <summary>アイコン（Resources/Sprites/Common/{Sprite}）</summary>
        public string Sprite;
        /// <summary>このステージをクリアするとおさんぽにも出る</summary>
        public int UnlockStage;

        /// <summary>表示名（今の言語）</summary>
        public string Name => TextLocalizer.IsJapanese() ? NameJa : NameEn;
        /// <summary>説明（今の言語）</summary>
        public string Description => TextLocalizer.IsJapanese() ? DescriptionJa : DescriptionEn;
    }

    /// <summary>
    /// ギミックのマスターデータ（Resources/MasterData/gimmick.csv）
    /// gimmick.csv: id,key,name_ja,name_en,desc_ja,desc_en,sprite,unlock_stage
    /// </summary>
    public static class GimmickMaster
    {
        public const string ArrowKey = "arrow";
        public const string TurnKey = "turn";
        public const string StraightKey = "straight";
        public const string TreasureKey = "treasure";
        public const string KeyKey = "key";
        public const string WarpKey = "warp";
        public const string CrumbleKey = "crumble";
        public const string FogKey = "fog";
        public const string OneWayKey = "oneway";
        public const string SwitchKey = "switch";
        private const string SpriteFolder = "Sprites/Common/";

        private static List<GimmickData> gimmicks = null;

        /// <summary>全ギミック（ID 順）</summary>
        public static IReadOnlyList<GimmickData> All
        {
            get
            {
                load();
                return gimmicks;
            }
        }

        /// <summary>
        /// キーで取得（なければ null）
        /// </summary>
        /// <param name="_key"></param>
        public static GimmickData Get(string _key)
        {
            load();
            return gimmicks.Find(_g => _g.Key == _key);
        }

        /// <summary>
        /// マスのギミック → キー
        /// </summary>
        /// <param name="_subType"></param>
        public static string GetKey(eBlockSubType _subType)
        {
            if (GimmickUtility.IsOneWay(_subType))
            {
                return OneWayKey;
            }
            if (GimmickUtility.IsArrow(_subType))
            {
                return ArrowKey;
            }
            if (GimmickUtility.IsSwitch(_subType))
            {
                return SwitchKey;
            }
            if (GimmickUtility.IsKey(_subType) || GimmickUtility.IsGate(_subType))
            {
                return KeyKey;
            }
            if (GimmickUtility.IsFog(_subType))
            {
                return FogKey;
            }
            if (GimmickUtility.IsWarpIn(_subType) || GimmickUtility.IsWarpOut(_subType))
            {
                return WarpKey;
            }
            switch (_subType)
            {
                case eBlockSubType.Turn: return TurnKey;
                case eBlockSubType.Straight: return StraightKey;
                case eBlockSubType.Treasure1:
                case eBlockSubType.Treasure2:
                case eBlockSubType.Treasure3:
                    return TreasureKey;
                case eBlockSubType.Crumble: return CrumbleKey;
                default: return null;
            }
        }

        /// <summary>
        /// アイコン
        /// </summary>
        /// <param name="_gimmick"></param>
        public static Sprite LoadIcon(GimmickData _gimmick)
        {
            if (_gimmick == null)
            {
                return null;
            }
            Sprite[] frames = GimmickSprites.LoadFrames(_gimmick.Sprite);
            return frames.Length > 0 ? frames[0] : null;
        }

        /// <summary>
        /// マップにあるギミックのうち、まだ説明を見ていないもの（ID 順）
        /// </summary>
        /// <param name="_mapData"></param>
        public static List<GimmickData> GetUnseen(MapData _mapData)
        {
            var keys = new HashSet<string>();
            for (int y = 0; y < _mapData.height; y++)
            {
                for (int x = 0; x < _mapData.width; x++)
                {
                    foreach (eBlockSubType subType in _mapData.GetBlockSubTypes(x, y))
                    {
                        string key = GetKey(subType);
                        if (key != null)
                        {
                            keys.Add(key);
                        }
                    }
                }
            }

            var result = new List<GimmickData>();
            foreach (GimmickData gimmick in All)
            {
                if (keys.Contains(gimmick.Key) && !SaveDataManager.Data.seenGimmicks.Contains(gimmick.Key))
                {
                    result.Add(gimmick);
                }
            }
            return result;
        }

        /// <summary>
        /// 説明を見たことにする
        /// </summary>
        /// <param name="_key"></param>
        public static void MarkSeen(string _key)
        {
            if (!SaveDataManager.Data.seenGimmicks.Contains(_key))
            {
                SaveDataManager.Data.seenGimmicks.Add(_key);
                SaveDataManager.Save();
            }
        }

        /// <summary>
        /// おさんぽで使えるギミック（冒険で unlock_stage をクリアしたもの）
        /// </summary>
        public static GimmickSettings GetStrollSettings()
        {
            int cleared = StageProgress.ClearedCount;
            return new GimmickSettings
            {
                Arrow = isUnlocked(ArrowKey, cleared),
                Turn = isUnlocked(TurnKey, cleared),
                Straight = isUnlocked(StraightKey, cleared),

                Key = isUnlocked(KeyKey, cleared),
                Warp = isUnlocked(WarpKey, cleared),
                Crumble = isUnlocked(CrumbleKey, cleared),
                Switch = isUnlocked(SwitchKey, cleared),
                Fog = isUnlocked(FogKey, cleared),
                OneWay = isUnlocked(OneWayKey, cleared),
            };
        }

        /// <summary>
        /// 冒険で解放済みか
        /// </summary>
        /// <param name="_key"></param>
        /// <param name="_cleared"></param>
        private static bool isUnlocked(string _key, int _cleared)
        {
            GimmickData gimmick = Get(_key);
            return gimmick != null && _cleared >= gimmick.UnlockStage;
        }

        /// <summary>
        /// 初回のみ CSV を読み込む
        /// </summary>
        private static void load()
        {
            if (gimmicks != null)
            {
                return;
            }

            gimmicks = new List<GimmickData>();
            var asset = ResourceManager.LoadResource<TextAsset>($"{CharacterMaster.Folder}/gimmick");
            if (asset == null)
            {
                Debug.LogError($"[GimmickMaster] CSV not found: {CharacterMaster.Folder}/gimmick");
                return;
            }

            List<string[]> rows = CsvUtility.Parse(asset.text);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 8 || !int.TryParse(row[0], out int id))
                {
                    continue;
                }
                gimmicks.Add(new GimmickData
                {
                    Id = id,
                    Key = row[1].Trim(),
                    NameJa = row[2],
                    NameEn = row[3],
                    DescriptionJa = row[4],
                    DescriptionEn = row[5],
                    Sprite = row[6].Trim(),
                    UnlockStage = int.TryParse(row[7], out int stage) ? stage : 0,
                });
            }
            gimmicks.Sort((_a, _b) => _a.Id.CompareTo(_b.Id));
        }
    }
}
