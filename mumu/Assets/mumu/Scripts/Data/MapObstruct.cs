using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 経路の外の陸に、通れないブロックを置く
    /// </summary>
    internal static class MapObstruct
    {
        /// <summary>
        /// 経路外の陸に通れないブロックを置く（置いた数を返す）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_path"></param>
        /// <param name="_count"></param>
        /// <param name="_random"></param>
        public static int Place(MapData _mapData, eCell[,] _cells, List<Vector2Int> _path, int _count, System.Random _random)
        {
            var onPath = new HashSet<Vector2Int>(_path);
            var candidates = new List<Vector2Int>();
            for (int y = 0; y < _mapData.height; y++)
            {
                for (int x = 0; x < _mapData.width; x++)
                {
                    var p = new Vector2Int(x, y);
                    if (_cells[x, y] == eCell.Land && !onPath.Contains(p))
                    {
                        candidates.Add(p);
                    }
                }
            }

            // シードで並べ替えて先頭から置く
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Vector2Int tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            int placed = Mathf.Min(_count, candidates.Count);
            for (int i = 0; i < placed; i++)
            {
                eBlockType obstruct = BlockTypeUtility.Obstructs[_random.Next(BlockTypeUtility.Obstructs.Length)];
                _mapData.SetBlockType(candidates[i].x, candidates[i].y, obstruct);
            }
            return placed;
        }
    }
}
