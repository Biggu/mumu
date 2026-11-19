using System;
using System.Collections.Generic;

namespace mumu
{
    /// <summary>
    /// ユーザーの保存データ（save.json の中身）
    /// </summary>
    [Serializable]
    public class UserSaveData
    {
        /// <summary>今の形式のバージョン（変えた時は SaveDataManager に変換を追加）</summary>
        public const int CurrentVersion = 2;

        /// <summary>形式のバージョン</summary>
        public int version = CurrentVersion;

        /// <summary>効果音を鳴らすか</summary>
        public bool isSeEnabled = true;
        /// <summary>BGM を鳴らすか</summary>
        public bool isBgmEnabled = true;

        /// <summary>冒険の今のステージ（0 始まり）</summary>
        public int stageIndex = 0;
        /// <summary>冒険のクリアしたステージ数（先頭から）</summary>
        public int clearedCount = 0;
        /// <summary>冒険のステージごとの最高記録</summary>
        public List<StageRecordData> stageRecords = new List<StageRecordData>();

        /// <summary>仲間のパグのキャラクター ID（仲間になった順、1匹ずつ）</summary>
        public List<int> pugs = new List<int>();
        /// <summary>冒険・おさんぽで連れていくパグ（0 = 男の子）</summary>
        public int equippedPugId = 0;

        /// <summary>今日のおさんぽの日付（yyyyMMdd、日が変わったら回数を戻す）</summary>
        public string dailyStrollDate = "";
        /// <summary>今日クリアしたおさんぽの回数</summary>
        public int dailyStrollCount = 0;

        /// <summary>貯めたほね</summary>
        public int bones = 0;
        /// <summary>説明を見たギミック（gimmick.csv の key）</summary>
        public List<string> seenGimmicks = new List<string>();
    }

    /// <summary>
    /// ステージの最高記録
    /// </summary>
    [Serializable]
    public class StageRecordData
    {
        /// <summary>ステージ番号（1 始まり）</summary>
        public int stage;
        /// <summary>最高の★数</summary>
        public int stars;
        /// <summary>PERFECT を取ったか</summary>
        public bool isPerfect;
        /// <summary>一番多く拾ったほねの数（くり返し遊んでも同じほねは1回だけ貯まる）</summary>
        public int bones;
    }
}
