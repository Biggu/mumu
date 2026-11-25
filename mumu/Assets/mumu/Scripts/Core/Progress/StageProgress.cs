using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 冒険の進行（map_list.csv の並び順、SaveDataManager に保存）
    /// </summary>
    public static class StageProgress
    {
        /// <summary>現在のステージ番号（0 始まり）</summary>
        public static int CurrentIndex
        {
            get => Mathf.Max(0, SaveDataManager.Data.stageIndex);
            private set
            {
                SaveDataManager.Data.stageIndex = value;
                SaveDataManager.Save();
            }
        }

        /// <summary>クリアしたステージ数（先頭から）</summary>
        public static int ClearedCount
        {
            get => Mathf.Max(0, SaveDataManager.Data.clearedCount);
            private set
            {
                SaveDataManager.Data.clearedCount = value;
                SaveDataManager.Save();
            }
        }

        /// <summary>現在のステージ番号（表示用、1 始まり）</summary>
        public static int CurrentNumber => CurrentIndex + 1;

        /// <summary>
        /// 今のステージのテーマ番号
        /// </summary>
        public static int GetCurrentThemeId()
        {
            List<MapInfo> list = MapCsv.LoadList();
            if (list.Count == 0)
            {
                return ThemeDefine.DefaultThemeId;
            }
            normalize(list.Count);
            return list[CurrentIndex].ThemeId;
        }

        /// <summary>
        /// ステージ番号（1 始まり）のテーマ番号
        /// </summary>
        /// <param name="_stageNumber"></param>
        public static int GetThemeIdOfStage(int _stageNumber)
        {
            List<MapInfo> list = MapCsv.LoadList();
            int index = _stageNumber - 1;
            return index >= 0 && index < list.Count ? list[index].ThemeId : ThemeDefine.DefaultThemeId;
        }

        /// <summary>
        /// 今のステージが、そのテーマの最後のステージか
        /// </summary>
        public static bool IsLastOfCurrentTheme()
        {
            List<MapInfo> list = MapCsv.LoadList();
            if (list.Count == 0)
            {
                return false;
            }
            normalize(list.Count);
            int next = CurrentIndex + 1;
            return next >= list.Count || list[next].ThemeId != list[CurrentIndex].ThemeId;
        }

        /// <summary>
        /// 今あるステージをすべてクリアしたか
        /// </summary>
        public static bool IsAllCleared()
        {
            int count = MapCsv.LoadList().Count;
            return count > 0 && ClearedCount >= count;
        }

        /// <summary>
        /// 現在のステージのマップを読み込む
        /// </summary>
        public static MapData LoadCurrentMap()
        {
            List<MapInfo> list = MapCsv.LoadList();
            if (list.Count == 0)
            {
                return null;
            }

            normalize(list.Count);
            return loadMapAt(list, CurrentIndex);
        }

        /// <summary>
        /// 指定の番号（0 始まり）のマップを読み込む（クリア済みのステージをもう一度遊ぶ用）
        /// </summary>
        /// <param name="_index"></param>
        public static MapData LoadMapAt(int _index)
        {
            List<MapInfo> list = MapCsv.LoadList();
            if (list.Count == 0)
            {
                return null;
            }
            return loadMapAt(list, Mathf.Clamp(_index, 0, list.Count - 1));
        }

        /// <summary>
        /// マップを読み、クリア済みなら宝箱を取り除く
        /// </summary>
        /// <param name="_list"></param>
        /// <param name="_index"></param>
        private static MapData loadMapAt(List<MapInfo> _list, int _index)
        {
            MapData map = MapCsv.LoadMap(_list[_index]);
            // クリアしたステージは宝箱が無くなる（そこは普通の道になる。ほねはもう増えない）
            if (map != null && StageRecord.IsCleared(_index + 1))
            {
                removeTreasures(map);
            }
            return map;
        }

        /// <summary>
        /// 宝箱を全部取り除く（マスは通れる道のまま）
        /// </summary>
        /// <param name="_map"></param>
        private static void removeTreasures(MapData _map)
        {
            for (int y = 0; y < _map.height; y++)
            {
                for (int x = 0; x < _map.width; x++)
                {
                    foreach (eBlockSubType subType in new List<eBlockSubType>(_map.GetBlockSubTypes(x, y)))
                    {
                        if (subType == eBlockSubType.Treasure1 || subType == eBlockSubType.Treasure2 || subType == eBlockSubType.Treasure3)
                        {
                            _map.RemoveBlockSubType(x, y, subType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 現在のステージ番号（0 始まり、アップデートで増えたステージにも進める）
        /// </summary>
        public static int GetCurrentIndex()
        {
            int count = MapCsv.LoadList().Count;
            if (count > 0)
            {
                normalize(count);
            }
            return CurrentIndex;
        }
        /// <summary>
        /// 現在のステージの難易度
        /// </summary>
        public static int GetCurrentDifficulty()
        {
            List<MapInfo> list = MapCsv.LoadList();
            if (list.Count == 0)
            {
                return MapGenerator.MinDifficulty;
            }

            normalize(list.Count);
            return list[CurrentIndex].Difficulty;
        }

        /// <summary>
        /// 現在のステージをクリアして次へ（最後のステージはそのまま繰り返す）
        /// </summary>
        public static void Advance()
        {
            int count = MapCsv.LoadList().Count;
            ClearedCount = Mathf.Max(ClearedCount, CurrentIndex + 1);
            if (CurrentIndex + 1 < count)
            {
                CurrentIndex++;
            }
        }

        /// <summary>
        /// 範囲外を直し、アップデートでステージが増えていたら未クリアの先頭へ進める
        /// </summary>
        /// <param name="_count"></param>
        private static void normalize(int _count)
        {
            int index = Mathf.Min(CurrentIndex, _count - 1);
            if (index < ClearedCount && ClearedCount < _count)
            {
                index = ClearedCount;
            }
            if (index != CurrentIndex)
            {
                CurrentIndex = index;
            }
        }
    }
}
