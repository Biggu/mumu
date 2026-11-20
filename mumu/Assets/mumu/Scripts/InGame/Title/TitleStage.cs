using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace mumu
{
    /// <summary>
    /// タイトル画面の背景に表示するランダムなステージ
    /// </summary>
    public static class TitleStage
    {
        /// <summary>背景に使う難易度の範囲（小さめのマップ）</summary>
        private const int MinDifficulty = 4;
        private const int MaxDifficulty = 9;

        /// <summary>
        /// ランダムなテーマ・形のマップを生成
        /// </summary>
        public static MapData CreateRandom()
        {
            var mapData = ScriptableObject.CreateInstance<MapData>();

            var patterns = (eMapPattern[])Enum.GetValues(typeof(eMapPattern));
            MapGenerator.Generate(mapData,
                Random.Range(MinDifficulty, MaxDifficulty + 1),
                patterns[Random.Range(0, patterns.Length)],
                Random.Range(int.MinValue, int.MaxValue),
                false);

            var themeIds = new List<int>(ThemeDefine.ThemeIds);
            mapData.themeId = themeIds[Random.Range(0, themeIds.Count)];
            return mapData;
        }
    }
}
