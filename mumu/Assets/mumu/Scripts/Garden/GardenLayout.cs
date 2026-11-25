using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// おにわの配置（25×25）。日付から決まる seed で、その日は皆同じ配置になる（MapGenerator の地形）
    /// </summary>
    public class GardenLayout
    {
        /// <summary>おにわの一辺のマス数</summary>
        public const int Size = 25;

        private readonly MapData mapData;

        /// <summary>
        /// seed から地形を作る
        /// </summary>
        /// <param name="_seed"></param>
        public GardenLayout(int _seed)
        {
            mapData = ScriptableObject.CreateInstance<MapData>();
            MapGenerator.GenerateTerrain(mapData, Size, Size, _seed);
        }

        /// <summary>今日の seed（yyyyMMdd。日付が変わると配置も変わる）</summary>
        public static int TodaySeed()
        {
            return int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        }

        /// <summary>マスの横の数</summary>
        public int Width => mapData.width;
        /// <summary>マスの縦の数</summary>
        public int Height => mapData.height;

        /// <summary>
        /// マスの種類
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public eBlockType TypeAt(int _x, int _y)
        {
            return mapData.GetBlockType(_x, _y);
        }
    }
}
