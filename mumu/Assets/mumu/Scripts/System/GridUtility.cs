using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 共通処理（GridUtility）
    /// </summary>
    public static class GridUtility
    {
        /// <summary>
        /// ブロック1つ分（持ち上げ分を含む）を範囲に広げる
        /// </summary>
        /// <param name="_position"></param>
        /// <param name="_blockSize"></param>
        /// <param name="_raise"></param>
        /// <param name="_min"></param>
        /// <param name="_max"></param>
        public static void ExpandBounds(Vector2 _position, float _blockSize, float _raise, ref Vector2 _min, ref Vector2 _max)
        {
            Vector2 half = new Vector2(_blockSize, _blockSize) * 0.5f;
            _min = Vector2.Min(_min, _position - half);
            _max = Vector2.Max(_max, _position + half + new Vector2(0f, _raise));
        }

        /// <summary>
        /// 「マスなし」を表す値
        /// </summary>
        public static readonly Vector2Int NoCell = new Vector2Int(int.MinValue, int.MinValue);


        /// <summary>
        /// 上・下・左・右の隣のマス
        /// </summary>
        public static readonly Vector2Int[] Directions4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };


        /// <summary>
        /// マスの位置（アイソメ表示。y=0 を画面の奥に見せるため上下を反転）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_height"></param>
        /// <param name="_halfWidth"></param>
        /// <param name="_halfHeight"></param>
        public static Vector2 IsoPosition(int _x, int _y, int _height, float _halfWidth, float _halfHeight)
        {
            int flippedY = (_height - 1) - _y;
            return new Vector2((_x - flippedY) * _halfWidth, (_x + flippedY) * _halfHeight);
        }


        /// <summary>
        /// グリッド方向 → 上面での角度（画像は右向き）
        /// </summary>
        /// <param name="_direction"></param>
        public static float GetPlaneAngle(Vector2Int _direction)
        {
            if (_direction.x > 0) return 45f;    // 右上
            if (_direction.x < 0) return 225f;   // 左下
            if (_direction.y > 0) return -45f;   // 右下（エディタの下方向）
            return 135f;                         // 左上（エディタの上方向）
        }
    }
}
