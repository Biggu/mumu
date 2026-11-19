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
            return MapCsv.LoadMap(list[CurrentIndex]);
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
