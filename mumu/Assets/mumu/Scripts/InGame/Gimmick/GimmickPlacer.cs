using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 設計した経路に合うマスへギミックを置く（生成時と、エディタの Auto Place で使う）
    /// </summary>
    public class GimmickPlacer
    {
        /// <summary>ギミックの乱数を経路の乱数と分けるための値</summary>
        public const int SeedSalt = 0x5bd1e995;
        /// <summary>宝箱の種類の出やすさ（木・銀・金、合計 100）</summary>
        private static readonly int[] TreasureWeights = { 60, 30, 10 };
        /// <summary>ワープで飛ばす経路の長さ（マス数）</summary>
        private const int MinSkip = 3;
        private const int MaxSkip = 6;

        /// <summary>
        /// 設計した経路に合うマスへギミックを置き直す（前のギミックは消す。置けた数を返す）
        /// ワープは経路の途中を飛ばして入口と出口をつなぎ、飛ばしたマスは通れないブロックにする。
        /// やじるし・まっすぐは経路がまっすぐなマス、まがるは曲がるマス、扉はカギより後、宝箱・カギは経路上。
        /// ほかに進める道が多いマスほど選ばれやすい（迷う所に置くと効く）。
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_counts"></param>
        /// <param name="_random"></param>
        public GimmickCounts Place(MapData _mapData, GimmickCounts _counts, System.Random _random)
        {
            _mapData.ClearSubTypes();
            var placed = new GimmickCounts();
            if (!_mapData.TryFindBlock(eBlockType.Start, out Vector2Int start) || !_mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal))
            {
                return placed;
            }

            var cells = new List<Vector2Int> { start };
            cells.AddRange(_mapData.GetWaypointPositions(out _));
            cells.Add(goal);

            for (int color = 0; color < Mathf.Min(_counts.Warps, GimmickUtility.ColorCount); color++)
            {
                if (insertWarp(_mapData, cells, color, _random))
                {
                    placed.Warps++;
                }
            }

            // 1マスに1つだけのギミックが置けるマス（扉・こわれる橋・やじるし・まがる・まっすぐ）
            var straights = new List<Vector2Int>();
            var turns = new List<Vector2Int>();
            var free = new List<Vector2Int>();
            var weights = new Dictionary<Vector2Int, int>();
            for (int i = 1; i < cells.Count - 1; i++)
            {
                Vector2Int cell = cells[i];
                if (_mapData.GetBlockType(cell.x, cell.y) != eBlockType.Walkable || GimmickUtility.GetExclusive(_mapData.GetBlockSubTypes(cell.x, cell.y)) != eBlockSubType.None)
                {
                    continue;
                }
                bool isJumpIn = !MapData.IsAdjacent(cells[i - 1], cell);
                bool isJumpOut = !MapData.IsAdjacent(cell, cells[i + 1]);
                if (isJumpIn || isJumpOut)
                {
                    continue;
                }
                Vector2Int stepIn = cell - cells[i - 1];
                Vector2Int stepOut = cells[i + 1] - cell;
                (stepIn == stepOut ? straights : turns).Add(cell);
                free.Add(cell);
                weights[cell] = 1 + countOpen(_mapData, cell, cells[i - 1], cells[i + 1]) * 2;
            }

            // 扉（後ろの方）とカギ（扉より前の経路上）
            for (int color = 0; color < Mathf.Min(_counts.Keys, GimmickUtility.ColorCount); color++)
            {
                var gateCandidates = free.FindAll(_c => cells.IndexOf(_c) >= 4);
                List<Vector2Int> gatePick = pick(gateCandidates, weights, 1, _random);
                if (gatePick.Count == 0)
                {
                    break;
                }
                Vector2Int gate = gatePick[0];
                int gateIndex = cells.IndexOf(gate);
                var keyCandidates = new List<Vector2Int>();
                for (int i = 1; i < gateIndex - 1; i++)
                {
                    Vector2Int cell = cells[i];
                    if (_mapData.GetBlockType(cell.x, cell.y) == eBlockType.Walkable && GimmickUtility.GetKey(_mapData.GetBlockSubTypes(cell.x, cell.y)) == eBlockSubType.None
                        && !GimmickUtility.IsGate(GimmickUtility.GetExclusive(_mapData.GetBlockSubTypes(cell.x, cell.y))))
                    {
                        keyCandidates.Add(cell);
                    }
                }
                if (keyCandidates.Count == 0)
                {
                    continue;
                }
                Vector2Int key = keyCandidates[_random.Next(keyCandidates.Count)];
                // スイッチの数まではカギの代わりにスイッチを置く（開け方が違うだけで、経路で通ると開く）
                bool useSwitch = placed.Switches < _counts.Switches;
                _mapData.AddBlockSubType(gate.x, gate.y, GimmickUtility.Gates[color]);
                _mapData.AddBlockSubType(key.x, key.y, useSwitch ? GimmickUtility.Switches[color] : GimmickUtility.Keys[color]);
                free.Remove(gate);
                straights.Remove(gate);
                turns.Remove(gate);
                if (useSwitch)
                {
                    placed.Switches++;
                }
                else
                {
                    placed.Keys++;
                }
            }

            // こわれる橋（最初の数マスは避ける）
            var crumbleCandidates = free.FindAll(_c => cells.IndexOf(_c) >= 3);
            foreach (Vector2Int cell in pick(crumbleCandidates, weights, _counts.Crumbles, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Crumble);
                straights.Remove(cell);
                turns.Remove(cell);
                placed.Crumbles++;
            }

            foreach (Vector2Int cell in pick(straights, weights, _counts.Arrows, _random))
            {
                int index = cells.IndexOf(cell);
                _mapData.AddBlockSubType(cell.x, cell.y, GimmickUtility.ToArrow(cells[index + 1] - cell));
                placed.Arrows++;
            }
            foreach (Vector2Int cell in pick(straights, weights, _counts.OneWays, _random))
            {
                int index = cells.IndexOf(cell);
                _mapData.AddBlockSubType(cell.x, cell.y, GimmickUtility.ToOneWay(cells[index + 1] - cell));
                placed.OneWays++;
            }
            foreach (Vector2Int cell in pick(straights, weights, _counts.Straights, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Straight);
                placed.Straights++;
            }
            foreach (Vector2Int cell in pick(turns, weights, _counts.Turns, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Turn);
                placed.Turns++;
            }

            // 霧は経路の上のギミックのないマス（最初の数マスは避ける）
            var fogCandidates = new List<Vector2Int>();
            var fogWeights = new Dictionary<Vector2Int, int>();
            for (int i = 2; i < cells.Count - 2; i++)
            {
                Vector2Int cell = cells[i];
                if (_mapData.GetBlockType(cell.x, cell.y) == eBlockType.Walkable && _mapData.GetBlockSubTypes(cell.x, cell.y).Count == 0)
                {
                    fogCandidates.Add(cell);
                    fogWeights[cell] = weights.TryGetValue(cell, out int fogWeight) ? fogWeight : 1;
                }
            }
            foreach (Vector2Int cell in pick(fogCandidates, fogWeights, _counts.Fogs, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Fog);
                placed.Fogs++;
            }

            // 宝箱は設計した経路の上（必ず取れる）。カギと同じマスは避け、ほかに道がある所ほど選ばれやすい
            var treasureCells = new List<Vector2Int>();
            var treasureWeights = new Dictionary<Vector2Int, int>();
            for (int i = 1; i < cells.Count - 1; i++)
            {
                Vector2Int cell = cells[i];
                IReadOnlyList<eBlockSubType> subTypes = _mapData.GetBlockSubTypes(cell.x, cell.y);
                if (_mapData.GetBlockType(cell.x, cell.y) != eBlockType.Walkable || GimmickUtility.GetKey(subTypes) != eBlockSubType.None || GimmickUtility.GetTreasure(subTypes) != eBlockSubType.None)
                {
                    continue;
                }
                treasureCells.Add(cell);
                treasureWeights[cell] = weights.TryGetValue(cell, out int weight) ? weight : 1;
            }
            foreach (Vector2Int cell in pick(treasureCells, treasureWeights, _counts.Treasures, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, pickTreasure(_random));
                placed.Treasures++;
            }
            return placed;
        }

        /// <summary>
        /// 宝箱の種類を選ぶ（木 60% / 銀 30% / 金 10%）
        /// </summary>
        /// <param name="_random"></param>
        private eBlockSubType pickTreasure(System.Random _random)
        {
            int roll = _random.Next(100);
            int total = 0;
            for (int i = 0; i < TreasureWeights.Length; i++)
            {
                total += TreasureWeights[i];
                if (roll < total)
                {
                    return GimmickUtility.Treasures[i];
                }
            }
            return GimmickUtility.Treasures[0];
        }

        /// <summary>
        /// 経路の途中（3〜6マス）を飛ばしてワープでつなぐ（飛ばしたマスは通れないブロックにして、経路の番号を振り直す）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_color"></param>
        /// <param name="_random"></param>
        private bool insertWarp(MapData _mapData, List<Vector2Int> _cells, int _color, System.Random _random)
        {
            var candidates = new List<Vector2Int>();
            for (int i = 2; i < _cells.Count - 3; i++)
            {
                for (int skip = MinSkip; skip <= MaxSkip; skip++)
                {
                    int j = i + skip + 1;
                    if (j >= _cells.Count - 1)
                    {
                        break;
                    }
                    if (isWarpable(_mapData, _cells, i) && isWarpable(_mapData, _cells, j) && !MapData.IsAdjacent(_cells[i], _cells[j]) && isSkippable(_mapData, _cells, i + 1, j - 1))
                    {
                        candidates.Add(new Vector2Int(i, j));
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return false;
            }

            Vector2Int chosen = candidates[_random.Next(candidates.Count)];
            Vector2Int entrance = _cells[chosen.x];
            Vector2Int exit = _cells[chosen.y];
            for (int i = chosen.x + 1; i < chosen.y; i++)
            {
                Vector2Int skipped = _cells[i];
                _mapData.SetWaypointIndex(skipped.x, skipped.y, -1);
                _mapData.SetBlockType(skipped.x, skipped.y, BlockTypeUtility.Obstructs[_random.Next(BlockTypeUtility.Obstructs.Length)]);
            }
            _cells.RemoveRange(chosen.x + 1, chosen.y - chosen.x - 1);
            _mapData.AddBlockSubType(entrance.x, entrance.y, GimmickUtility.WarpIns[_color]);
            _mapData.AddBlockSubType(exit.x, exit.y, GimmickUtility.WarpOuts[_color]);

            for (int i = 1; i < _cells.Count - 1; i++)
            {
                _mapData.SetWaypointIndex(_cells[i].x, _cells[i].y, i - 1);
            }
            return true;
        }

        /// <summary>
        /// ワープの入口・出口にできるマスか（通れるマスで、前後がつながっていて、ほかのギミックがない）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_index"></param>
        private bool isWarpable(MapData _mapData, List<Vector2Int> _cells, int _index)
        {
            Vector2Int cell = _cells[_index];
            return _mapData.GetBlockType(cell.x, cell.y) == eBlockType.Walkable
                && _mapData.GetBlockSubTypes(cell.x, cell.y).Count == 0
                && MapData.IsAdjacent(_cells[_index - 1], cell)
                && MapData.IsAdjacent(cell, _cells[_index + 1]);
        }

        /// <summary>
        /// 飛ばしてよいマスか（Start・Goal の近くやギミックのあるマスは飛ばさない）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        private bool isSkippable(MapData _mapData, List<Vector2Int> _cells, int _from, int _to)
        {
            for (int i = _from; i <= _to; i++)
            {
                Vector2Int cell = _cells[i];
                if (_mapData.GetBlockType(cell.x, cell.y) != eBlockType.Walkable || _mapData.GetBlockSubTypes(cell.x, cell.y).Count > 0 || _mapData.IsNearStartOrGoal(cell.x, cell.y))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 経路の前後以外に進める隣のマス数
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cell"></param>
        /// <param name="_previous"></param>
        /// <param name="_next"></param>
        private int countOpen(MapData _mapData, Vector2Int _cell, Vector2Int _previous, Vector2Int _next)
        {
            int count = 0;
            foreach (Vector2Int d in GridUtility.Directions4)
            {
                Vector2Int n = _cell + d;
                if (n != _previous && n != _next && _mapData.IsInside(n.x, n.y) && BlockTypeUtility.IsWalkable(_mapData.GetBlockType(n.x, n.y)))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 重みつきで重ならないように選び、選んだマスは候補から外す
        /// </summary>
        /// <param name="_candidates"></param>
        /// <param name="_weights"></param>
        /// <param name="_count"></param>
        /// <param name="_random"></param>
        private List<Vector2Int> pick(List<Vector2Int> _candidates, Dictionary<Vector2Int, int> _weights, int _count, System.Random _random)
        {
            var result = new List<Vector2Int>();
            while (result.Count < _count && _candidates.Count > 0)
            {
                int total = 0;
                foreach (Vector2Int cell in _candidates)
                {
                    total += _weights[cell];
                }
                int roll = _random.Next(total);
                for (int i = 0; i < _candidates.Count; i++)
                {
                    roll -= _weights[_candidates[i]];
                    if (roll < 0)
                    {
                        result.Add(_candidates[i]);
                        _candidates.RemoveAt(i);
                        break;
                    }
                }
            }
            return result;
        }
    }
}
