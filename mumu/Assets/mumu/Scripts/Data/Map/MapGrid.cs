using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップ生成で共通の方向・範囲・通れる判定
    /// </summary>
    internal static class MapGrid
    {
        /// <summary>上下左右（順番を変えると同じ seed の結果が変わるので注意）</summary>
        public static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        /// <summary>
        /// マスが範囲内か
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public static bool IsInside(eCell[,] _cells, int _x, int _y)
        {
            return _x >= 0 && _y >= 0 && _x < _cells.GetLength(0) && _y < _cells.GetLength(1);
        }

        /// <summary>
        /// 経路にできるマスか（陸・橋）
        /// </summary>
        /// <param name="_cell"></param>
        public static bool IsMoveable(eCell _cell)
        {
            return _cell == eCell.Land || _cell == eCell.Bridge;
        }
    }
}
