using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ヒント（設計した経路の両端）。N 回目は Start 側・Goal 側それぞれ 5% × N の割合（途中は出さない）
    /// 使える回数は経路の長さで決まる（30 マス未満は 1 回、長いほど増えて最大 9 回）
    /// </summary>
    public class HintPlan
    {
        /// <summary>1 回使うごとに、Start 側と Goal 側それぞれ増える割合</summary>
        public const float RatePerUse = 0.05f;
        /// <summary>使える回数の上限</summary>
        public const int MaxUses = 9;
        /// <summary>2 回目以降を使うのに必要な経路の長さ（マス数）</summary>
        public const int MinRouteForSecondUse = 30;
        /// <summary>経路の長さ何マスごとに 1 回増えるか</summary>
        public const int CellsPerUse = 15;

        private readonly Vector2Int[] designed;
        private readonly Dictionary<Vector2Int, int> indexByCell = new Dictionary<Vector2Int, int>();
        private readonly HashSet<int> hinted = new HashSet<int>();

        /// <summary>
        /// 設計した経路（順番どおり）と、今まで使った回数からヒントを作る
        /// </summary>
        /// <param name="_designed"></param>
        /// <param name="_useCount">使った回数（0 なら何も出さない）</param>
        public HintPlan(Vector2Int[] _designed, int _useCount)
        {
            designed = _designed;
            for (int i = 0; i < designed.Length; i++)
            {
                indexByCell[designed[i]] = i;
            }
            choose(_useCount);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 設計した経路を全部ヒントにする（デバッグ用）
        /// </summary>
        public void SelectAll()
        {
            for (int i = 0; i < designed.Length; i++)
            {
                hinted.Add(i);
            }
        }
#endif

        /// <summary>
        /// 経路の長さから、使える回数の上限を求める
        /// </summary>
        /// <param name="_routeLength"></param>
        public static int GetMaxUses(int _routeLength)
        {
            if (_routeLength < MinRouteForSecondUse)
            {
                return 1;
            }
            return Mathf.Min(MaxUses, _routeLength / CellsPerUse);
        }

        /// <summary>
        /// 設計した経路の位置（順番どおり。見つからない番号は除く）
        /// </summary>
        /// <param name="_mapData"></param>
        public static Vector2Int[] GetDesignedPath(MapData _mapData)
        {
            Vector2Int[] positions = _mapData.GetWaypointPositions(out bool _);
            var result = new List<Vector2Int>(positions.Length);
            foreach (Vector2Int position in positions)
            {
                if (position != MapData.NoPosition)
                {
                    result.Add(position);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// このマスがヒントか
        /// </summary>
        /// <param name="_cell"></param>
        public bool Contains(Vector2Int _cell)
        {
            return indexByCell.TryGetValue(_cell, out int index) && hinted.Contains(index);
        }

        /// <summary>
        /// 経路の前のマスがヒントなら、そちらへの向き（なければ zero）
        /// </summary>
        /// <param name="_cell"></param>
        public Vector2Int PreviousDirection(Vector2Int _cell)
        {
            return directionTo(_cell, -1);
        }

        /// <summary>
        /// 経路の次のマスがヒントなら、そちらへの向き（なければ zero）
        /// </summary>
        /// <param name="_cell"></param>
        public Vector2Int NextDirection(Vector2Int _cell)
        {
            return directionTo(_cell, 1);
        }

        /// <summary>
        /// 経路上の前後へ、ヒントのマスなら向きを返す
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_offset"></param>
        private Vector2Int directionTo(Vector2Int _cell, int _offset)
        {
            if (!indexByCell.TryGetValue(_cell, out int index))
            {
                return Vector2Int.zero;
            }
            int target = index + _offset;
            if (target < 0 || target >= designed.Length || !hinted.Contains(target))
            {
                return Vector2Int.zero;
            }
            return designed[target] - _cell;
        }

        /// <summary>
        /// Start 側・Goal 側それぞれ 5% × 使った回数（最低 1 マス）を出す
        /// </summary>
        /// <param name="_useCount"></param>
        private void choose(int _useCount)
        {
            int total = designed.Length;
            if (_useCount <= 0 || total == 0)
            {
                return;
            }

            int edge = Mathf.Clamp(Mathf.RoundToInt(total * RatePerUse * _useCount), 1, total);
            for (int i = 0; i < edge; i++)
            {
                hinted.Add(i);
                hinted.Add(total - 1 - i);
            }
        }
    }
}
