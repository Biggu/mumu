using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 冒険のステージごとの最高記録（★数と PERFECT、SaveDataManager に保存）
    /// </summary>
    public static class StageRecord
    {
        private const int MaxStars = 3;

        /// <summary>
        /// クリアしたことがあるか（記録があれば true）
        /// </summary>
        /// <param name="_stageNumber"></param>
        public static bool IsCleared(int _stageNumber)
        {
            return find(_stageNumber) != null;
        }

        /// <summary>
        /// 最高の★数
        /// </summary>
        /// <param name="_stageNumber"></param>
        public static int GetStars(int _stageNumber)
        {
            StageRecordData record = find(_stageNumber);
            return record != null ? record.stars : 0;
        }

        /// <summary>
        /// PERFECT を取ったか
        /// </summary>
        /// <param name="_stageNumber"></param>
        public static bool IsPerfect(int _stageNumber)
        {
            StageRecordData record = find(_stageNumber);
            return record != null && record.isPerfect;
        }

        /// <summary>
        /// もどらずにクリアしたか
        /// </summary>
        /// <param name="_stageNumber"></param>
        public static bool IsNoRewind(int _stageNumber)
        {
            StageRecordData record = find(_stageNumber);
            return record != null && record.isNoRewind;
        }

        /// <summary>
        /// 記録を更新（良い方を残す）。もどらずクリアを初めて達成した時は true を返す
        /// </summary>
        /// <param name="_stageNumber"></param>
        /// <param name="_stars"></param>
        /// <param name="_isPerfect"></param>
        /// <param name="_isNoRewind"></param>
        public static bool Save(int _stageNumber, int _stars, bool _isPerfect, bool _isNoRewind)
        {
            StageRecordData record = find(_stageNumber);
            if (record == null)
            {
                record = new StageRecordData { stage = _stageNumber };
                SaveDataManager.Data.stageRecords.Add(record);
            }
            record.stars = Mathf.Max(record.stars, Mathf.Clamp(_stars, 0, MaxStars));
            record.isPerfect |= _isPerfect;
            bool isFirstNoRewind = _isNoRewind && !record.isNoRewind;
            record.isNoRewind |= _isNoRewind;
            SaveDataManager.Save();
            return isFirstNoRewind;
        }

        /// <summary>
        /// 拾ったほねを記録して、新しく貯まる数を返す（これまでの最高より多い分だけ）
        /// </summary>
        /// <param name="_stageNumber"></param>
        /// <param name="_collected"></param>
        public static int SaveBones(int _stageNumber, int _collected)
        {
            StageRecordData record = find(_stageNumber);
            if (record == null)
            {
                record = new StageRecordData { stage = _stageNumber };
                SaveDataManager.Data.stageRecords.Add(record);
            }
            int gained = Mathf.Max(0, _collected - record.bones);
            record.bones = Mathf.Max(record.bones, _collected);
            SaveDataManager.Save();
            return gained;
        }

        /// <summary>
        /// ステージの記録（なければ null）
        /// </summary>
        /// <param name="_stageNumber"></param>
        private static StageRecordData find(int _stageNumber)
        {
            return SaveDataManager.Data.stageRecords.Find(_r => _r.stage == _stageNumber);
        }
    }
}