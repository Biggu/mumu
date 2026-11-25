using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace mumu
{
    /// <summary>
    /// おにわのパグの行き先（となりの草へ数マス歩く道、ほかの子のところまでついて歩く道）
    /// </summary>
    public class GardenPugRoute
    {
        /// <summary>となりの草へ歩く最大マス数</summary>
        private const int MaxWalkSteps = 4;

        private readonly Queue<Vector2Int> cells = new Queue<Vector2Int>();

        /// <summary>これから歩くマスがあるか</summary>
        public bool HasNext => cells.Count > 0;

        /// <summary>次に歩くマス</summary>
        public Vector2Int Next => cells.Peek();

        /// <summary>
        /// 次のマスに着いたので、道から外す
        /// </summary>
        public Vector2Int Dequeue()
        {
            return cells.Dequeue();
        }

        /// <summary>
        /// 道を空にする
        /// </summary>
        public void Clear()
        {
            cells.Clear();
        }

        /// <summary>
        /// となりの草へ数マス歩く道を決める（行けなければ false）
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_map"></param>
        public bool PlanWalk(Vector2Int _from, GardenMap _map)
        {
            cells.Clear();
            Vector2Int current = _from;
            Vector2Int previous = _from;
            int steps = Random.Range(1, MaxWalkSteps + 1);
            var candidates = new List<Vector2Int>(4);
            for (int i = 0; i < steps; i++)
            {
                candidates.Clear();
                foreach (Vector2Int d in GridUtility.Directions4)
                {
                    Vector2Int next = current + d;
                    if (_map.IsWalkable(next) && next != previous)
                    {
                        candidates.Add(next);
                    }
                }
                if (candidates.Count == 0)
                {
                    break;
                }
                previous = current;
                current = candidates[Random.Range(0, candidates.Count)];
                cells.Enqueue(current);
            }
            return cells.Count > 0;
        }

        /// <summary>
        /// 連れていく子の場所まで道を決める（相手と同じマスには入らず、となりで止まる。行けなければ false）
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_leaderCell"></param>
        /// <param name="_map"></param>
        /// <param name="_maxSteps"></param>
        public bool PlanFollow(Vector2Int _from, Vector2Int _leaderCell, GardenMap _map, int _maxSteps)
        {
            List<Vector2Int> path = _map.FindPath(_from, _leaderCell, _maxSteps);
            if (path.Count > 0 && path[path.Count - 1] == _leaderCell)
            {
                path.RemoveAt(path.Count - 1);
            }
            if (path.Count == 0)
            {
                return false;
            }

            cells.Clear();
            foreach (Vector2Int step in path)
            {
                cells.Enqueue(step);
            }
            return true;
        }
    }
}
