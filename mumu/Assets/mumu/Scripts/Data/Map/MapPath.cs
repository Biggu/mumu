using System;
using System.Collections.Generic;
using UnityEngine;
using static mumu.MapGrid;

namespace mumu
{
    /// <summary>
    /// 通る道（Start から Goal までの経路）を作る
    /// </summary>
    internal static class MapPath
    {
        /// <summary>経路を作る回数（目標の長さに一番近いものを使う）</summary>
        private const int PathAttempts = 12;

        /// <summary>
        /// 長い経路を作る（両端が Start・Goal。Start・Goal を除いて _waypointCount マスを目指す）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_moveable"></param>
        /// <param name="_waypointCount"></param>
        /// <param name="_random"></param>
        public static List<Vector2Int> CreatePath(eCell[,] _cells, List<Vector2Int> _moveable, int _waypointCount, System.Random _random)
        {
            int targetLength = _waypointCount + 2;

            // 行き止まりに近い陸（隣が少ない）から始めると長い経路になりやすい
            var starts = _moveable.FindAll(_p => _cells[_p.x, _p.y] == eCell.Land);
            starts.Sort((_a, _b) => degree(_cells, _a).CompareTo(degree(_cells, _b)));

            List<Vector2Int> best = new List<Vector2Int>();
            int bestScore = int.MinValue;
            for (int i = 0; i < PathAttempts && starts.Count > 0; i++)
            {
                Vector2Int start = i < PathAttempts / 2 && i < starts.Count ? starts[i] : starts[_random.Next(starts.Count)];
                List<Vector2Int> walk = walkWarnsdorff(_cells, start, _random);
                expand(_cells, walk, targetLength, _random);
                List<Vector2Int> path = trim(_cells, walk, targetLength);
                if (path.Count < 2)
                {
                    continue;
                }

                // 目標に届いたものは超過が少ないほど、届かないものは長いほど良い
                int score = path.Count >= targetLength ? int.MaxValue - (path.Count - targetLength) : path.Count;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = path;
                }
                if (path.Count == targetLength)
                {
                    break;
                }
            }
            return best;
        }

        /// <summary>
        /// 先が少ないマスを優先して進む（Warnsdorff 法）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_start"></param>
        /// <param name="_random"></param>
        private static List<Vector2Int> walkWarnsdorff(eCell[,] _cells, Vector2Int _start, System.Random _random)
        {
            var walk = new List<Vector2Int> { _start };
            var visited = new HashSet<Vector2Int> { _start };
            extendEnd(_cells, walk, visited, _random);
            return walk;
        }

        /// <summary>
        /// 末尾から、先が少ない空きマスを優先して伸ばす（伸ばせたら true）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_walk"></param>
        /// <param name="_visited"></param>
        /// <param name="_random"></param>
        private static bool extendEnd(eCell[,] _cells, List<Vector2Int> _walk, HashSet<Vector2Int> _visited, System.Random _random)
        {
            bool isExtended = false;
            while (true)
            {
                Vector2Int current = _walk[_walk.Count - 1];
                Vector2Int next = new Vector2Int(-1, -1);
                int bestOnward = int.MaxValue;
                int ties = 0;
                foreach (Vector2Int d in Directions)
                {
                    Vector2Int candidate = current + d;
                    if (!isFree(_cells, _visited, candidate))
                    {
                        continue;
                    }

                    int onward = 0;
                    foreach (Vector2Int d2 in Directions)
                    {
                        Vector2Int n = candidate + d2;
                        if (n != current && isFree(_cells, _visited, n)) onward++;
                    }

                    // 同じ数なら等確率で選ぶ
                    if (onward < bestOnward)
                    {
                        bestOnward = onward;
                        next = candidate;
                        ties = 1;
                    }
                    else if (onward == bestOnward && _random.Next(++ties) == 0)
                    {
                        next = candidate;
                    }
                }

                if (next.x < 0)
                {
                    return isExtended;
                }
                _walk.Add(next);
                _visited.Add(next);
                isExtended = true;
            }
        }

        /// <summary>
        /// 目標の長さまで、寄り道を挟む・両端を伸ばすを繰り返す
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_walk"></param>
        /// <param name="_targetLength"></param>
        /// <param name="_random"></param>
        private static void expand(eCell[,] _cells, List<Vector2Int> _walk, int _targetLength, System.Random _random)
        {
            var visited = new HashSet<Vector2Int>(_walk);
            bool isChanged = true;
            while (isChanged && _walk.Count < _targetLength)
            {
                isChanged = extendEnd(_cells, _walk, visited, _random);
                _walk.Reverse();
                isChanged |= extendEnd(_cells, _walk, visited, _random);
                _walk.Reverse();

                // 毎回違う位置から寄り道を探して偏りを減らす
                int offset = _random.Next(Mathf.Max(1, _walk.Count - 1));
                for (int k = 0; k < _walk.Count - 1 && _walk.Count < _targetLength; k++)
                {
                    int i = (offset + k) % (_walk.Count - 1);
                    if (tryDetour(_cells, _walk, visited, i, _random))
                    {
                        isChanged = true;
                    }
                }
            }
        }

        /// <summary>
        /// 目標の長さに切り詰める（両端は陸、Start・Goal は MinStartGoalDistance 以上離す）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_walk"></param>
        /// <param name="_targetLength"></param>
        private static List<Vector2Int> trim(eCell[,] _cells, List<Vector2Int> _walk, int _targetLength)
        {
            // 先頭が橋なら陸まで捨てる
            int first = 0;
            while (first < _walk.Count && _cells[_walk[first].x, _walk[first].y] != eCell.Land)
            {
                first++;
            }

            // 目標の長さに近い順（長い方を優先）に、条件を満たす終端を探す
            int ideal = Mathf.Min(_walk.Count - 1, first + _targetLength - 1);
            for (int delta = 0; delta < _walk.Count; delta++)
            {
                foreach (int last in new[] { ideal + delta, ideal - delta })
                {
                    if (last <= first || last >= _walk.Count)
                    {
                        continue;
                    }
                    Vector2Int start = _walk[first], goal = _walk[last];
                    int distance = Mathf.Abs(start.x - goal.x) + Mathf.Abs(start.y - goal.y);
                    if (_cells[goal.x, goal.y] == eCell.Land && distance >= MapGenerator.MinStartGoalDistance)
                    {
                        return _walk.GetRange(first, last - first + 1);
                    }
                }
            }
            return new List<Vector2Int>();
        }

        /// <summary>
        /// path[i] → path[i + 1] の間に空きマスだけを通る寄り道を入れる（入れられたら true）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_path"></param>
        /// <param name="_visited"></param>
        /// <param name="_index"></param>
        /// <param name="_random"></param>
        private static bool tryDetour(eCell[,] _cells, List<Vector2Int> _path, HashSet<Vector2Int> _visited, int _index, System.Random _random)
        {
            Vector2Int a = _path[_index], b = _path[_index + 1];
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (Vector2Int d in shuffledDirections(_random))
            {
                Vector2Int n = a + d;
                if (isFree(_cells, _visited, n))
                {
                    previous[n] = a;
                    queue.Enqueue(n);
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (MapData.IsAdjacent(current, b))
                {
                    var detour = new List<Vector2Int>();
                    for (Vector2Int p = current; p != a; p = previous[p])
                    {
                        detour.Add(p);
                    }
                    detour.Reverse();
                    _path.InsertRange(_index + 1, detour);
                    _visited.UnionWith(detour);
                    return true;
                }

                foreach (Vector2Int d in Directions)
                {
                    Vector2Int next = current + d;
                    if (isFree(_cells, _visited, next) && !previous.ContainsKey(next))
                    {
                        previous[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 上下左右をシードで並べ替える
        /// </summary>
        /// <param name="_random"></param>
        private static Vector2Int[] shuffledDirections(System.Random _random)
        {
            var directions = (Vector2Int[])Directions.Clone();
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Vector2Int tmp = directions[i];
                directions[i] = directions[j];
                directions[j] = tmp;
            }
            return directions;
        }

        /// <summary>
        /// 経路に使える空きマスか
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_visited"></param>
        /// <param name="_p"></param>
        private static bool isFree(eCell[,] _cells, HashSet<Vector2Int> _visited, Vector2Int _p)
        {
            return IsInside(_cells, _p.x, _p.y) && IsMoveable(_cells[_p.x, _p.y]) && !_visited.Contains(_p);
        }

        /// <summary>
        /// 通れる隣のマス数
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_p"></param>
        private static int degree(eCell[,] _cells, Vector2Int _p)
        {
            int count = 0;
            foreach (Vector2Int d in Directions)
            {
                Vector2Int n = _p + d;
                if (IsInside(_cells, n.x, n.y) && IsMoveable(_cells[n.x, n.y])) count++;
            }
            return count;
        }
    }
}
