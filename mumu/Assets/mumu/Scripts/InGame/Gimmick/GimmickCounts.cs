using System;
using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 自動生成で使うギミック（数はマップの経路の長さから決める）
    /// </summary>
    public struct GimmickSettings
    {
        public bool Arrow;
        public bool Turn;
        public bool Straight;
        public bool Key;
        public bool Warp;
        public bool Crumble;

        /// <summary>どれか使うか（宝箱は指定がなくても必ず1つ置く）</summary>
        public bool IsAny => Arrow || Turn || Straight || Key || Warp || Crumble;
    }

    /// <summary>
    /// ギミックの数
    /// </summary>
    public struct GimmickCounts
    {
        /// <summary>経路の長さに対するやじるしの割合</summary>
        private const float ArrowRate = 0.06f;
        private const float TurnRate = 0.05f;
        private const float StraightRate = 0.04f;
        /// <summary>自動生成で置く宝箱の数（必ず1つ）</summary>
        private const int TreasuresPerMap = 1;

        public int Arrows;
        public int Turns;
        public int Straights;
        /// <summary>宝箱の数（種類は置く時に選ぶ）</summary>
        public int Treasures;
        /// <summary>カギと扉の組の数（最大3色）</summary>
        public int Keys;
        /// <summary>ワープの組の数（最大3色）</summary>
        public int Warps;
        public int Crumbles;

        /// <summary>合計</summary>
        public int Total => Arrows + Turns + Straights + Treasures + Keys + Warps + Crumbles;

        /// <summary>
        /// 経路の長さに合わせた数
        /// </summary>
        /// <param name="_waypoints"></param>
        /// <param name="_settings"></param>
        public static GimmickCounts FromPath(int _waypoints, GimmickSettings _settings)
        {
            return new GimmickCounts
            {
                Arrows = _settings.Arrow ? Mathf.Max(1, Mathf.RoundToInt(_waypoints * ArrowRate)) : 0,
                Turns = _settings.Turn ? Mathf.Max(1, Mathf.RoundToInt(_waypoints * TurnRate)) : 0,
                Straights = _settings.Straight ? Mathf.Max(1, Mathf.RoundToInt(_waypoints * StraightRate)) : 0,
                Treasures = TreasuresPerMap,
                Keys = _settings.Key ? (_waypoints >= 30 ? 2 : 1) : 0,
                Warps = _settings.Warp ? (_waypoints >= 40 ? 2 : 1) : 0,
                Crumbles = _settings.Crumble ? Mathf.Clamp(_waypoints / 25, 1, 2) : 0,
            };
        }

        /// <summary>
        /// 表示用
        /// </summary>
        public override string ToString()
        {
            return $"arrow {Arrows}, turn {Turns}, straight {Straights}, treasure {Treasures}, key {Keys}, warp {Warps}, crumble {Crumbles}";
        }
    }
}
