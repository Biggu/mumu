using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ヒント（設計した経路の一部）。Start 側 5%・Goal 側 5%・残りは seed でランダム（合計 40%）
    /// </summary>
    public class HintPlan
    {
        /// <summary>ヒントに出す割合（経路全体）</summary>
        public const float HintRate = 0.4f;
        /// <summary>Start 側と Goal 側それぞれの割合</summary>
        public const float EdgeRate = 0.05f;
        /// <summary>途中に選ぶ区間の長さ（マス数）</summary>
        private const int MinSegmentLength = 3;
        private const int MaxSegmentLength = 6;
        /// <summary>区間を選び直す回数の上限</summary>
        private const int MaxSegmentAttempts = 200;

        private readonly Vector2Int[] designed;
        private readonly Dictionary<Vector2Int, int> indexByCell = new Dictionary<Vector2Int, int>();
        private readonly HashSet<int> hinted = new HashSet<int>();

        /// <summary>
        /// 設計した経路（順番どおり）から、seed で決まるヒントを作る
        /// </summary>
        /// <param name="_designed"></param>
        /// <param name="_seed"></param>
        public HintPlan(Vector2Int[] _designed, int _seed)
        {
            designed = _designed;
            for (int i = 0; i < designed.Length; i++)
            {
                indexByCell[designed[i]] = i;
            }
            choose(_seed);
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
        /// Start 側・Goal 側の5%と、残りを seed のランダムで選ぶ
        /// </summary>
        /// <param name="_seed"></param>
        private void choose(int _seed)
        {
            int total = designed.Length;
            int edge = Mathf.RoundToInt(total * EdgeRate);
            int count = Mathf.RoundToInt(total * HintRate);

            for (int i = 0; i < edge && i < total; i++)
            {
                hinted.Add(i);
                hinted.Add(total - 1 - i);
            }

            // 残りは途中の「つながった区間」をランダムに選ぶ（線で向きが分かるように）
            var random = new System.Random(_seed);
            int rest = count - hinted.Count;
            int middleStart = edge;
            int middleEnd = total - edge;
            for (int attempt = 0; attempt < MaxSegmentAttempts && rest > 0 && middleEnd - middleStart > 0; attempt++)
            {
                int length = Mathf.Min(random.Next(MinSegmentLength, MaxSegmentLength + 1), rest);
                int last = middleEnd - length;
                if (last < middleStart)
                {
                    break;
                }

                int start = random.Next(middleStart, last + 1);
                if (!isFree(start, length))
                {
                    continue;
                }
                for (int i = start; i < start + length; i++)
                {
                    hinted.Add(i);
                }
                rest -= length;
            }
        }

        /// <summary>
        /// 区間の全マスがまだヒントでないか
        /// </summary>
        /// <param name="_start"></param>
        /// <param name="_length"></param>
        private bool isFree(int _start, int _length)
        {
            for (int i = _start; i < _start + _length; i++)
            {
                if (hinted.Contains(i))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
