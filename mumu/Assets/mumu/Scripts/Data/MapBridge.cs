using System.Collections.Generic;
using UnityEngine;
using static mumu.MapGrid;

namespace mumu
{
    /// <summary>
    /// 離れた陸どうしを橋でつなぐ
    /// </summary>
    internal static class MapBridge
    {
        /// <summary>
        /// 離れた陸を最短の橋でつなぐ（橋の数を返す）
        /// </summary>
        /// <param name="_cells"></param>
        public static int Connect(eCell[,] _cells)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            int bridges = 0;

            while (true)
            {
                int[,] component = labelComponents(_cells, out int count, out int largest);
                if (count <= 1)
                {
                    return bridges;
                }

                // 一番大きい陸から、陸以外を通る距離が最短の別の陸を探す（0-1 BFS）
                var cost = new int[width, height];
                var from = new Vector2Int[width, height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) cost[x, y] = int.MaxValue;

                var deque = new LinkedList<Vector2Int>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (component[x, y] == largest)
                        {
                            cost[x, y] = 0;
                            deque.AddLast(new Vector2Int(x, y));
                        }
                    }
                }

                Vector2Int target = new Vector2Int(-1, -1);
                while (deque.Count > 0)
                {
                    Vector2Int current = deque.First.Value;
                    deque.RemoveFirst();
                    if (component[current.x, current.y] >= 0 && component[current.x, current.y] != largest)
                    {
                        target = current;
                        break;
                    }

                    foreach (Vector2Int d in Directions)
                    {
                        Vector2Int next = current + d;
                        if (!IsInside(_cells, next.x, next.y))
                        {
                            continue;
                        }
                        int step = IsMoveable(_cells[next.x, next.y]) ? 0 : 1;
                        if (cost[current.x, current.y] + step >= cost[next.x, next.y])
                        {
                            continue;
                        }
                        cost[next.x, next.y] = cost[current.x, current.y] + step;
                        from[next.x, next.y] = current;
                        if (step == 0) deque.AddFirst(next); else deque.AddLast(next);
                    }
                }

                if (target.x < 0)
                {
                    return bridges;
                }

                // たどった経路の陸以外を橋にする
                var corridor = new List<Vector2Int>();
                for (Vector2Int p = target; component[p.x, p.y] != largest; p = from[p.x, p.y])
                {
                    if (!IsMoveable(_cells[p.x, p.y]))
                    {
                        corridor.Add(p);
                    }
                }
                foreach (Vector2Int p in corridor)
                {
                    _cells[p.x, p.y] = eCell.Bridge;
                }
                bridges += corridor.Count;

                // 渡って戻ってこられるよう、離れた場所にもう1本かける
                bridges += addSecondBridge(_cells, component, largest, component[target.x, target.y], corridor);
            }
        }

        /// <summary>
        /// 1本目の橋から離れた場所に、同じ陸どうしをつなぐ橋をもう1本かける（かけた数を返す）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_component"></param>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        /// <param name="_firstBridge"></param>
        private static int addSecondBridge(eCell[,] _cells, int[,] _component, int _from, int _to, List<Vector2Int> _firstBridge)
        {
            const int MaxExtraLength = 4;
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            if (_firstBridge.Count == 0)
            {
                return 0;
            }

            // 1本目とその周り1マスは通らない
            var blocked = new bool[width, height];
            foreach (Vector2Int p in _firstBridge)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = p.x + dx, y = p.y + dy;
                        if (IsInside(_cells, x, y)) blocked[x, y] = true;
                    }
                }
            }

            var cost = new int[width, height];
            var previous = new Vector2Int[width, height];
            var deque = new LinkedList<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cost[x, y] = int.MaxValue;
                    if (_component[x, y] == _from && !blocked[x, y])
                    {
                        cost[x, y] = 0;
                        deque.AddLast(new Vector2Int(x, y));
                    }
                }
            }

            while (deque.Count > 0)
            {
                Vector2Int current = deque.First.Value;
                deque.RemoveFirst();
                if (_component[current.x, current.y] == _to)
                {
                    if (cost[current.x, current.y] > _firstBridge.Count + MaxExtraLength)
                    {
                        return 0;
                    }

                    int added = 0;
                    for (Vector2Int p = current; _component[p.x, p.y] != _from; p = previous[p.x, p.y])
                    {
                        if (_cells[p.x, p.y] != eCell.Land && _cells[p.x, p.y] != eCell.Bridge)
                        {
                            _cells[p.x, p.y] = eCell.Bridge;
                            added++;
                        }
                    }
                    return added;
                }

                foreach (Vector2Int d in Directions)
                {
                    Vector2Int next = current + d;
                    if (!IsInside(_cells, next.x, next.y) || blocked[next.x, next.y])
                    {
                        continue;
                    }
                    // 他の陸は通らない（つなぐ2つの陸の間だけ）
                    int label = _component[next.x, next.y];
                    if (label >= 0 && label != _from && label != _to)
                    {
                        continue;
                    }
                    int step = label >= 0 ? 0 : 1;
                    if (cost[current.x, current.y] + step >= cost[next.x, next.y])
                    {
                        continue;
                    }
                    cost[next.x, next.y] = cost[current.x, current.y] + step;
                    previous[next.x, next.y] = current;
                    if (step == 0) deque.AddFirst(next); else deque.AddLast(next);
                }
            }
            return 0;
        }

        /// <summary>
        /// つながった陸ごとに番号を付ける（陸以外は -1）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_count"></param>
        /// <param name="_largest"></param>
        private static int[,] labelComponents(eCell[,] _cells, out int _count, out int _largest)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            var label = new int[width, height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) label[x, y] = -1;

            _count = 0;
            _largest = -1;
            int largestSize = 0;
            var stack = new Stack<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsMoveable(_cells[x, y]) || label[x, y] >= 0)
                    {
                        continue;
                    }

                    int size = 0;
                    label[x, y] = _count;
                    stack.Push(new Vector2Int(x, y));
                    while (stack.Count > 0)
                    {
                        Vector2Int p = stack.Pop();
                        size++;
                        foreach (Vector2Int d in Directions)
                        {
                            Vector2Int n = p + d;
                            if (IsInside(_cells, n.x, n.y) && IsMoveable(_cells[n.x, n.y]) && label[n.x, n.y] < 0)
                            {
                                label[n.x, n.y] = _count;
                                stack.Push(n);
                            }
                        }
                    }

                    if (size > largestSize)
                    {
                        largestSize = size;
                        _largest = _count;
                    }
                    _count++;
                }
            }
            return label;
        }
    }
}
