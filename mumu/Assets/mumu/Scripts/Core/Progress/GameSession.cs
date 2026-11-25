using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace mumu
{
    /// <summary>
    /// 遊び方
    /// </summary>
    public enum eGameMode
    {
        /// <summary>冒険（map_list.csv のステージを順番に）</summary>
        Adventure,
        /// <summary>おさんぽ（1日5回は日替わりのマップでパグに会えるチャンス、その後はランダム）</summary>
        Stroll,
    }

    /// <summary>
    /// 遊んでいるモードとおさんぽのマップ
    /// </summary>
    public static class GameSession
    {
        /// <summary>1日にパグに会えるチャンスがあるおさんぽの回数</summary>
        public const int DailyStrollCount = 5;
        /// <summary>おさんぽの難易度の幅（冒険の現在の難易度 ± この値）</summary>
        private const int StrollDifficultyRange = 10;
        private const string DateFormat = "yyyyMMdd";

        private static int strollDifficulty = MapGenerator.MinDifficulty;
        private static eMapPattern strollPattern = eMapPattern.Square;
        private static int strollSeed = 0;
        /// <summary>おさんぽのマップの、テーマの中の順番（Goal 解放の割合に使う）</summary>
        private static int strollStep = 0;
        private static int strollThemeId = ThemeDefine.DefaultThemeId;
        /// <summary>おさんぽのマップで待っているパグ（0 = いつもの女の子で報酬なし）</summary>
        private static int strollRewardId = 0;

        /// <summary>遊んでいるモード</summary>
        public static eGameMode Mode { get; private set; } = eGameMode.Adventure;
        /// <summary>今のおさんぽが日替わり（パグに会えるチャンスあり）か</summary>
        public static bool IsDailyStroll { get; private set; }
        /// <summary>今のおさんぽが今日の何回目か（1 始まり、日替わりの時のみ）</summary>
        public static int DailyStrollNumber { get; private set; }
        /// <summary>冒険でクリアしたステージ（ステージマップで次のステージまで歩く、-1 = なし）</summary>
        public static int WalkFromIndex { get; set; } = -1;
        /// <summary>冒険で、クリア済みのステージをもう一度遊んでいるか（進みは変わらない）</summary>
        public static bool IsReplay { get; private set; } = false;
        /// <summary>もう一度遊ぶステージ（0 始まり）</summary>
        public static int ReplayIndex { get; private set; } = -1;
        /// <summary>今遊んでいる冒険のステージ番号（1 始まり。もう一度遊ぶ時はそのステージ）</summary>
        public static int PlayedStageNumber => Mode == eGameMode.Adventure && IsReplay ? ReplayIndex + 1 : StageProgress.CurrentNumber;

        /// <summary>今日の残りのチャンス</summary>
        public static int DailyStrollRemaining
        {
            get
            {
                refreshDailyDate();
                return Mathf.Max(0, DailyStrollCount - SaveDataManager.Data.dailyStrollCount);
            }
        }

        /// <summary>
        /// 冒険を始める
        /// </summary>
        public static void StartAdventure()
        {
            Mode = eGameMode.Adventure;
            IsReplay = false;
        }

        /// <summary>
        /// クリア済みの冒険のステージをもう一度遊ぶ（クリアしても進みは変わらない）
        /// </summary>
        /// <param name="_index"></param>
        public static void StartReplay(int _index)
        {
            Mode = eGameMode.Adventure;
            IsReplay = true;
            ReplayIndex = _index;
        }

        /// <summary>
        /// おさんぽを始める（新しいマップを選ぶ）
        /// </summary>
        public static void StartStroll()
        {
            Mode = eGameMode.Stroll;
            IsReplay = false;
            NextStroll();
        }

        /// <summary>
        /// おさんぽの次のマップを選ぶ（チャンスが残っていれば日替わりのシード、なければランダムで報酬なし）
        /// </summary>
        public static void NextStroll()
        {
            int remaining = DailyStrollRemaining;
            IsDailyStroll = remaining > 0;
            DailyStrollNumber = IsDailyStroll ? DailyStrollCount - remaining + 1 : 0;

            // 日替わりは日付と回数から決めた乱数で、マップも報酬も毎回同じになる
            var random = IsDailyStroll ? new System.Random(getDailySeed(DateTime.Now, DailyStrollNumber)) : new System.Random(Random.Range(int.MinValue, int.MaxValue));

            int center = StageProgress.GetCurrentDifficulty();
            int min = Mathf.Max(MapGenerator.MinDifficulty, center - StrollDifficultyRange);
            int max = Mathf.Min(MapGenerator.MaxDifficulty, center + StrollDifficultyRange);
            strollDifficulty = random.Next(min, max + 1);

            var patterns = (eMapPattern[])Enum.GetValues(typeof(eMapPattern));
            strollPattern = patterns[random.Next(patterns.Length)];
            strollSeed = random.Next(int.MinValue, int.MaxValue);
            strollStep = random.Next(MapGenerator.StepsPerBlock - 1);

            var themeIds = unlockedThemeIds();
            strollThemeId = themeIds[random.Next(themeIds.Count)];

            // 当たりなら Goal でその子が待つ（やりなおしても同じ）
            strollRewardId = IsDailyStroll ? CharacterMaster.RollStrollReward(random) : 0;
        }

        /// <summary>
        /// おさんぽのテーマの候補（これまでに遊んだ冒険のステージのテーマ。ボーナスは入れない）
        /// </summary>
        private static List<int> unlockedThemeIds()
        {
            var result = new List<int>();
            List<MapInfo> list = MapCsv.LoadList();
            int reached = Mathf.Min(list.Count, StageProgress.ClearedCount + 1);
            for (int i = 0; i < reached; i++)
            {
                MapInfo info = list[i];
                if (!info.IsBonus && !result.Contains(info.ThemeId))
                {
                    result.Add(info.ThemeId);
                }
            }
            if (result.Count == 0)
            {
                result.Add(ThemeDefine.DefaultThemeId);
            }
            return result;
        }

        /// <summary>
        /// 今のマップの Goal で待っているキャラクター（いつもの女の子なら 0。パグがもらえるのはおさんぽだけ）
        /// </summary>
        public static int GetGoalCharacterId()
        {
            return Mode == eGameMode.Stroll ? strollRewardId : 0;
        }

        /// <summary>
        /// クリア時の報酬を受け取る（Advance より前に呼ぶ。なければ 0。もう持っている子なら _duplicateBones にほね）
        /// </summary>
        /// <param name="_duplicateBones"></param>
        public static int ClaimReward(out int _duplicateBones)
        {
            _duplicateBones = 0;
            int id = GetGoalCharacterId();
            if (id <= 0 || CharacterMaster.Get(id) == null)
            {
                return 0;
            }

            _duplicateBones = PugCollection.Add(id);
            return id;
        }

        /// <summary>
        /// 現在のモードのマップを作る（おさんぽは選んだマップを同じ seed で作り直す）
        /// </summary>
        public static MapData CreateMap()
        {
#if UNITY_EDITOR
            // デバッグメニューの「デバッグマップ」を遊ぶ時だけ（エディタ専用）
            if (DebugTools.HasPendingMap)
            {
                return DebugTools.TakePendingMap();
            }
#endif
            if (Mode == eGameMode.Adventure)
            {
                return IsReplay ? StageProgress.LoadMapAt(ReplayIndex) : StageProgress.LoadCurrentMap();
            }

            var mapData = ScriptableObject.CreateInstance<MapData>();
            MapGenerator.Generate(mapData, strollDifficulty, strollPattern, strollSeed, false, GimmickMaster.GetStrollSettings());
            mapData.themeId = strollThemeId;
            mapData.goalStep = strollStep;
            return mapData;
        }

        /// <summary>
        /// クリア後に次のマップへ（日替わりのおさんぽはチャンスを1回使う）
        /// </summary>
        public static void Advance()
        {
            if (Mode == eGameMode.Adventure)
            {
                // もう一度遊んだステージは、進みを変えない（歩く演出もなし）
                if (IsReplay)
                {
                    WalkFromIndex = -1;
                    return;
                }
                WalkFromIndex = StageProgress.CurrentIndex;
                StageProgress.Advance();
                return;
            }

            if (IsDailyStroll)
            {
                refreshDailyDate();
                SaveDataManager.Data.dailyStrollCount++;
                SaveDataManager.Save();
            }
            NextStroll();
        }

        /// <summary>
        /// 日が変わっていたら今日の回数を戻す
        /// </summary>
        private static void refreshDailyDate()
        {
            string today = DateTime.Now.ToString(DateFormat);
            UserSaveData data = SaveDataManager.Data;
            if (data.dailyStrollDate != today)
            {
                data.dailyStrollDate = today;
                data.dailyStrollCount = 0;
                SaveDataManager.Save();
            }
        }

        /// <summary>
        /// 日付と回数から決まるシード（同じ日の同じ回は誰でも同じ）
        /// </summary>
        /// <param name="_date"></param>
        /// <param name="_number"></param>
        private static int getDailySeed(DateTime _date, int _number)
        {
            int day = _date.Year * 10000 + _date.Month * 100 + _date.Day;
            unchecked
            {
                uint hash = (uint)day * 2654435761u;
                hash ^= (uint)_number * 40503u;
                hash ^= hash >> 15;
                hash *= 2246822519u;
                return (int)(hash ^ (hash >> 13));
            }
        }
    }
}
