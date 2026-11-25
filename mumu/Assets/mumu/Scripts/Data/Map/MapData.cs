using System;
using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップデータ（CSV から実行時に作る。MapCsv 参照）
    /// </summary>
    public class MapData : ScriptableObject
    {
        /// <summary>
        /// マスごとの追加情報
        /// </summary>
        [Serializable]
        public class BlockSubTypeList
        {
            /// <summary>追加属性</summary>
            public List<eBlockSubType> subTypes = new List<eBlockSubType>();
            /// <summary>経路順（-1 = 経路外）</summary>
            public int waypointIndex = -1;
        }

        /// <summary>未設定の位置</summary>
        public static readonly Vector2Int NoPosition = new Vector2Int(-1, -1);
        /// <summary>色つきギミックの色の名前（エラー表示用）</summary>
        private static readonly string[] ColorNames = { "Red", "Blue", "Yellow" };

        /// <summary>チェックポイントを置く waypoint の割合</summary>
        public static readonly float[] CheckpointRates = { 0.25f, 0.5f, 0.75f };

        /// <summary>マップ ID</summary>
        public int id;
        /// <summary>表示名</summary>
        public string mapName;
        /// <summary>テーマ ID（Sprites/Themes/theme_{id:000000}）</summary>
        public int themeId = ThemeDefine.DefaultThemeId;
        /// <summary>横のマス数</summary>
        public int width;
        /// <summary>縦のマス数</summary>
        public int height;
        /// <summary>難易度（Goal に入れる通過数の割合に使う）</summary>
        public int difficulty = 1;
        /// <summary>ボーナスステージか（Goal までの割合が少なく、宝箱のほねが倍になる）</summary>
        public bool isBonus = false;
        /// <summary>テーマの中の順番（0 = 1ステージ目）。Goal 解放の割合に使う</summary>
        public int goalStep = 0;
        /// <summary>ブロック種別（y * width + x）</summary>
        public eBlockType[] blockTypes;
        /// <summary>マスごとの追加情報（y * width + x）</summary>
        public BlockSubTypeList[] blockSubTypes;

        /// <summary>マス数</summary>
        public int Count => width * height;

        /// <summary>
        /// 指定サイズで初期化
        /// </summary>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        public void Initialize(int _width, int _height)
        {
            width = _width;
            height = _height;
            blockTypes = new eBlockType[Count];
            blockSubTypes = new BlockSubTypeList[Count];

            for (int i = 0; i < blockSubTypes.Length; i++)
            {
                blockSubTypes[i] = new BlockSubTypeList();
            }
        }

        /// <summary>
        /// サイズと配列が一致しているか
        /// </summary>
        public bool IsValidSize()
        {
            return width > 0 && height > 0
                && blockTypes != null && blockTypes.Length == Count
                && blockSubTypes != null && blockSubTypes.Length == Count;
        }

        /// <summary>
        /// マップ内か
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public bool IsInside(int _x, int _y)
        {
            return _x >= 0 && _x < width && _y >= 0 && _y < height;
        }

        /// <summary>
        /// 座標 → 配列の添字
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public int ToIndex(int _x, int _y)
        {
            return _y * width + _x;
        }

        /// <summary>
        /// ブロック種別を取得
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public eBlockType GetBlockType(int _x, int _y)
        {
            return blockTypes[ToIndex(_x, _y)];
        }

        /// <summary>
        /// ブロック種別を設定
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_type"></param>
        public void SetBlockType(int _x, int _y, eBlockType _type)
        {
            blockTypes[ToIndex(_x, _y)] = _type;
        }

        /// <summary>
        /// 追加属性を取得
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public IReadOnlyList<eBlockSubType> GetBlockSubTypes(int _x, int _y)
        {
            return blockSubTypes[ToIndex(_x, _y)].subTypes;
        }

        /// <summary>
        /// 追加属性を追加
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_subType"></param>
        public void AddBlockSubType(int _x, int _y, eBlockSubType _subType)
        {
            var subTypeList = blockSubTypes[ToIndex(_x, _y)].subTypes;
            if (!subTypeList.Contains(_subType))
            {
                subTypeList.Add(_subType);
            }
        }

        /// <summary>
        /// 追加属性を削除
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_subType"></param>
        public void RemoveBlockSubType(int _x, int _y, eBlockSubType _subType)
        {
            blockSubTypes[ToIndex(_x, _y)].subTypes.Remove(_subType);
        }

        /// <summary>
        /// 経路順を取得
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public int GetWaypointIndex(int _x, int _y)
        {
            return blockSubTypes[ToIndex(_x, _y)].waypointIndex;
        }

        /// <summary>
        /// 経路順を設定
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_index"></param>
        public void SetWaypointIndex(int _x, int _y, int _index)
        {
            blockSubTypes[ToIndex(_x, _y)].waypointIndex = _index;
        }

        /// <summary>
        /// 最大の経路順（経路なしは -1）
        /// </summary>
        public int GetMaxWaypointIndex()
        {
            int max = -1;
            foreach (var cell in blockSubTypes)
            {
                if (cell.waypointIndex > max)
                {
                    max = cell.waypointIndex;
                }
            }
            return max;
        }

        /// <summary>
        /// 経路順ごとの位置（見つからない番号は NoPosition）
        /// </summary>
        /// <param name="_duplicated"></param>
        public Vector2Int[] GetWaypointPositions(out bool _duplicated)
        {
            var positions = new Vector2Int[GetMaxWaypointIndex() + 1];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = NoPosition;
            }

            _duplicated = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = GetWaypointIndex(x, y);
                    if (index < 0)
                    {
                        continue;
                    }
                    if (positions[index] != NoPosition)
                    {
                        _duplicated = true;
                    }
                    positions[index] = new Vector2Int(x, y);
                }
            }
            return positions;
        }

        /// <summary>
        /// 指定番号以降の経路を削除
        /// </summary>
        /// <param name="_index"></param>
        public void RemoveWaypointsFrom(int _index)
        {
            foreach (var cell in blockSubTypes)
            {
                if (cell.waypointIndex >= _index)
                {
                    cell.waypointIndex = -1;
                }
            }
        }

        /// <summary>
        /// 経路をすべて削除
        /// </summary>
        public void ClearWaypoints()
        {
            RemoveWaypointsFrom(0);
        }

        /// <summary>
        /// 途切れた番号以降の経路を削除
        /// </summary>
        public void TrimBrokenWaypoints()
        {
            Vector2Int[] positions = GetWaypointPositions(out _);
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i] == NoPosition)
                {
                    RemoveWaypointsFrom(i);
                    return;
                }
            }
        }

        /// <summary>
        /// チェックポイントの経路順（重複なし）
        /// </summary>
        public List<int> GetCheckpointIndices()
        {
            int count = GetMaxWaypointIndex() + 1;
            var indices = new List<int>();
            if (count <= 0)
            {
                return indices;
            }

            foreach (float rate in CheckpointRates)
            {
                int index = Mathf.Clamp(Mathf.CeilToInt(count * rate) - 1, 0, count - 1);
                if (!indices.Contains(index))
                {
                    indices.Add(index);
                }
            }
            return indices;
        }

        /// <summary>
        /// チェックポイントの位置
        /// </summary>
        public List<Vector2Int> GetCheckpoints()
        {
            Vector2Int[] positions = GetWaypointPositions(out _);
            var checkpoints = new List<Vector2Int>();
            foreach (int index in GetCheckpointIndices())
            {
                if (positions[index] != NoPosition)
                {
                    checkpoints.Add(positions[index]);
                }
            }
            return checkpoints;
        }

        /// <summary>
        /// 指定番号の経路の位置
        /// </summary>
        /// <param name="_index"></param>
        /// <param name="_pos"></param>
        public bool TryFindWaypoint(int _index, out Vector2Int _pos)
        {
            for (int i = 0; i < blockSubTypes.Length; i++)
            {
                if (blockSubTypes[i].waypointIndex == _index)
                {
                    _pos = new Vector2Int(i % width, i / width);
                    return true;
                }
            }
            _pos = NoPosition;
            return false;
        }

        /// <summary>
        /// 指定種別の最初のブロックの位置
        /// </summary>
        /// <param name="_type"></param>
        /// <param name="_pos"></param>
        public bool TryFindBlock(eBlockType _type, out Vector2Int _pos)
        {
            for (int i = 0; i < blockTypes.Length; i++)
            {
                if (blockTypes[i] == _type)
                {
                    _pos = new Vector2Int(i % width, i / width);
                    return true;
                }
            }
            _pos = NoPosition;
            return false;
        }

        /// <summary>
        /// 指定種別のブロック数
        /// </summary>
        /// <param name="_type"></param>
        public int CountBlocks(eBlockType _type)
        {
            int count = 0;
            foreach (var type in blockTypes)
            {
                if (type == _type)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Start・Goal の周囲 3x3 か（デコで隠れないように使う）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public bool IsNearStartOrGoal(int _x, int _y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!IsInside(_x + dx, _y + dy))
                    {
                        continue;
                    }
                    eBlockType type = GetBlockType(_x + dx, _y + dy);
                    if (type == eBlockType.Start || type == eBlockType.Goal)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 上下左右に隣接しているか
        /// </summary>
        /// <param name="_a"></param>
        /// <param name="_b"></param>
        public static bool IsAdjacent(Vector2Int _a, Vector2Int _b)
        {
            return Mathf.Abs(_a.x - _b.x) + Mathf.Abs(_a.y - _b.y) == 1;
        }

        /// <summary>
        /// Start → 経路 → Goal が繋がっているか検証
        /// </summary>
        /// <param name="_error"></param>
        public bool ValidatePath(out string _error)
        {
            return ValidatePath(out _error, out _);
        }

        /// <summary>
        /// Start → 経路 → Goal が繋がっていて、ギミックの決まりを守っているか検証（_errorCell は問題のマス、なければ NoPosition）
        /// </summary>
        /// <param name="_error"></param>
        /// <param name="_errorCell"></param>
        public bool ValidatePath(out string _error, out Vector2Int _errorCell)
        {
            _errorCell = NoPosition;
            if (!IsValidSize())
            {
                _error = "Invalid map size";
                return false;
            }
            if (CountBlocks(eBlockType.Start) != 1)
            {
                _error = "Start must be exactly one";
                return false;
            }
            if (CountBlocks(eBlockType.Goal) != 1)
            {
                _error = "Goal must be exactly one";
                return false;
            }

            Vector2Int[] positions = GetWaypointPositions(out bool duplicated);
            if (positions.Length == 0)
            {
                _error = "No waypoint";
                return false;
            }
            if (duplicated)
            {
                _error = "Duplicated waypoint index";
                return false;
            }

            TryFindBlock(eBlockType.Start, out Vector2Int prev);
            var visited = new HashSet<Vector2Int> { prev };
            for (int i = 0; i < positions.Length; i++)
            {
                Vector2Int pos = positions[i];
                if (pos == NoPosition)
                {
                    _error = $"Waypoint {i + 1} not found";
                    return false;
                }
                if (!BlockTypeUtility.IsWalkable(GetBlockType(pos.x, pos.y)))
                {
                    _errorCell = pos;
                    _error = $"Waypoint {i + 1} ({pos.x},{pos.y}) is not walkable";
                    return false;
                }
                if (!IsAdjacent(prev, pos) && !IsWarpJump(prev, pos, visited))
                {
                    _errorCell = pos;
                    bool isWarpEntrance = GimmickUtility.IsWarpIn(GimmickUtility.GetExclusive(GetBlockSubTypes(prev.x, prev.y)));
                    _error = i == 0 ? $"Start is not next to waypoint 1 ({pos.x},{pos.y})"
                        : isWarpEntrance ? $"Waypoint {i + 1} ({pos.x},{pos.y}) is not the exit of the warp at waypoint {i} (missing or wrong color exit?)"
                        : $"Waypoint {i + 1} ({pos.x},{pos.y}) is not next to waypoint {i}";
                    return false;
                }
                visited.Add(pos);
                prev = pos;
            }

            TryFindBlock(eBlockType.Goal, out Vector2Int goal);
            if (!IsAdjacent(prev, goal))
            {
                _errorCell = prev;
                bool isWarpEntrance = GimmickUtility.IsWarpIn(GimmickUtility.GetExclusive(GetBlockSubTypes(prev.x, prev.y)));
                _error = isWarpEntrance ? $"The last waypoint ({prev.x},{prev.y}) is a warp entrance: continue the path from its exit"
                    : $"The last waypoint ({prev.x},{prev.y}) is not next to the Goal";
                return false;
            }

            return validateGimmicks(positions, out _error, out _errorCell);
        }

        /// <summary>
        /// 設計した経路がギミックの決まりを守っているか（ギミックは通れるマスだけ、色の組がそろっているか、カギは扉より先か、ワープは使うか）
        /// </summary>
        /// <param name="_waypoints"></param>
        /// <param name="_error"></param>
        private bool validateGimmicks(Vector2Int[] _waypoints, out string _error, out Vector2Int _errorCell)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 霧は水や通れないブロックの上にも置ける（見た目だけで、通り方には関係しない）
                    bool hasRouteGimmick = false;
                    foreach (eBlockSubType subType in GetBlockSubTypes(x, y))
                    {
                        if (subType != eBlockSubType.Fog)
                        {
                            hasRouteGimmick = true;
                        }
                    }
                    if (hasRouteGimmick && GetBlockType(x, y) != eBlockType.Walkable)
                    {
                        _errorCell = new Vector2Int(x, y);
                        _error = $"Gimmick on a non-walkable block ({x},{y})";
                        return false;
                    }
                }
            }

            for (int color = 0; color < GimmickUtility.ColorCount; color++)
            {
                int ins = CountSubType(GimmickUtility.WarpIns[color]);
                int outs = CountSubType(GimmickUtility.WarpOuts[color]);
                string colorName = ColorNames[color];
                if (ins > 1 || outs > 1 || ins != outs)
                {
                    TryFindSubType(ins > 0 ? GimmickUtility.WarpIns[color] : GimmickUtility.WarpOuts[color], out _errorCell);
                    _error = $"{colorName} warp needs exactly one entrance and one exit (entrance {ins}, exit {outs})";
                    return false;
                }
                if (CountSubType(GimmickUtility.Gates[color]) > 0 && CountSubType(GimmickUtility.Keys[color]) + CountSubType(GimmickUtility.Switches[color]) == 0)
                {
                    TryFindSubType(GimmickUtility.Gates[color], out _errorCell);
                    _error = $"{colorName} gate has no {colorName.ToLowerInvariant()} key ({_errorCell.x},{_errorCell.y})";
                    return false;
                }
            }

            // Start → 経路 → Goal の順に、入る向き・出る向き・カギ・ワープを調べる
            var cells = new List<Vector2Int>(_waypoints.Length + 2);
            TryFindBlock(eBlockType.Start, out Vector2Int start);
            TryFindBlock(eBlockType.Goal, out Vector2Int goal);
            cells.Add(start);
            cells.AddRange(_waypoints);
            cells.Add(goal);

            var visited = new HashSet<Vector2Int> { start };
            for (int i = 1; i < cells.Count - 1; i++)
            {
                Vector2Int cell = cells[i];
                IReadOnlyList<eBlockSubType> subTypes = GetBlockSubTypes(cell.x, cell.y);
                eBlockSubType exclusive = GimmickUtility.GetExclusive(subTypes);

                if (GimmickUtility.IsGate(exclusive) && !hasKeyIn(visited, GimmickUtility.GetColorIndex(exclusive)))
                {
                    _errorCell = cell;
                    _error = $"Waypoint {i} ({cell.x},{cell.y}) reaches a {ColorNames[GimmickUtility.GetColorIndex(exclusive)].ToLowerInvariant()} gate before its key";
                    return false;
                }

                // ワープの入口は、出口がまだ経路になければ必ず出口へ飛ぶ
                if (GimmickUtility.IsWarpIn(exclusive) && TryFindSubType(GimmickUtility.WarpOuts[GimmickUtility.GetColorIndex(exclusive)], out Vector2Int exit)
                    && !visited.Contains(exit) && cells[i + 1] != exit)
                {
                    _errorCell = cell;
                    _error = $"Waypoint {i} ({cell.x},{cell.y}) is a warp entrance: the next waypoint must be its exit ({exit.x},{exit.y})";
                    return false;
                }

                // ワープで飛んだ側は向きを見ない
                Vector2Int stepIn = IsAdjacent(cells[i - 1], cell) ? cell - cells[i - 1] : Vector2Int.zero;
                Vector2Int stepOut = IsAdjacent(cell, cells[i + 1]) ? cells[i + 1] - cell : Vector2Int.zero;
                eBlockSubType route = GimmickUtility.GetRoute(subTypes);
                if (route != eBlockSubType.None && !GimmickUtility.IsAllowed(route, stepIn, stepOut))
                {
                    _errorCell = cell;
                    _error = $"Waypoint {i} ({cell.x},{cell.y}) breaks {route}";
                    return false;
                }
                visited.Add(cell);
            }

            _error = null;
            _errorCell = NoPosition;
            return true;
        }

        /// <summary>
        /// _from（ワープの入口）から _to（同じ色の出口、まだ通っていない）へ飛べるか
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        /// <param name="_visited"></param>
        public bool IsWarpJump(Vector2Int _from, Vector2Int _to, ICollection<Vector2Int> _visited)
        {
            eBlockSubType entrance = GimmickUtility.GetExclusive(GetBlockSubTypes(_from.x, _from.y));
            if (!GimmickUtility.IsWarpIn(entrance) || _visited.Contains(_to))
            {
                return false;
            }
            return HasSubType(_to.x, _to.y, GimmickUtility.WarpOuts[GimmickUtility.GetColorIndex(entrance)]);
        }

        /// <summary>
        /// 指定のギミックがあるマスを探す（最初の1つ）
        /// </summary>
        /// <param name="_subType"></param>
        /// <param name="_pos"></param>
        public bool TryFindSubType(eBlockSubType _subType, out Vector2Int _pos)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (HasSubType(x, y, _subType))
                    {
                        _pos = new Vector2Int(x, y);
                        return true;
                    }
                }
            }
            _pos = NoPosition;
            return false;
        }

        /// <summary>
        /// 通ったマスに指定の色のカギがあるか
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_color"></param>
        private bool hasKeyIn(IEnumerable<Vector2Int> _cells, int _color)
        {
            foreach (Vector2Int cell in _cells)
            {
                if (HasSubType(cell.x, cell.y, GimmickUtility.Keys[_color]) || HasSubType(cell.x, cell.y, GimmickUtility.Switches[_color]))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// マスにギミックがあるか
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_subType"></param>
        public bool HasSubType(int _x, int _y, eBlockSubType _subType)
        {
            return blockSubTypes[ToIndex(_x, _y)].subTypes.Contains(_subType);
        }

        /// <summary>
        /// ギミックを全部消す
        /// </summary>
        public void ClearSubTypes()
        {
            foreach (var cell in blockSubTypes)
            {
                cell.subTypes.Clear();
            }
        }

        /// <summary>
        /// 置かれているギミックの数
        /// </summary>
        /// <param name="_subType"></param>
        public int CountSubType(eBlockSubType _subType)
        {
            int count = 0;
            foreach (var cell in blockSubTypes)
            {
                if (cell.subTypes.Contains(_subType))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
