using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 経路移動の結果
    /// </summary>
    public enum eMoveResult
    {
        None,
        Added,
        Undone,
        Cleared,
    }

    /// <summary>
    /// 一筆書きパズルの経路（Start から先端までの並びと、進む・戻る）。ギミックの決まりは PathRules
    /// </summary>
    public class PuzzlePath
    {
        private readonly MapData mapData;
        private readonly Vector2Int start;
        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        /// <summary>マス → 経路上の順番</summary>
        private readonly Dictionary<Vector2Int, int> indices = new Dictionary<Vector2Int, int>();
        private readonly List<Vector2Int> checkpoints;
        /// <summary>ワープで飛んだ入口の順番（cells[i] → cells[i + 1] が飛んだ所）</summary>
        private readonly HashSet<int> warpJumps = new HashSet<int>();
        /// <summary>一度でも通ったスイッチの色（巻き戻しても開いたまま）</summary>
        private readonly HashSet<int> openedSwitches = new HashSet<int>();
        private readonly PathRules rules;

        /// <summary>Start からの経路（クリア時は Goal を含む）</summary>
        public IReadOnlyList<Vector2Int> Cells => cells;
        /// <summary>経路の先端</summary>
        public Vector2Int Head => cells[cells.Count - 1];
        /// <summary>クリア済みか</summary>
        public bool IsCleared { get; private set; }
        /// <summary>MapData の経路数</summary>
        /// <summary>ボーナスステージの Goal 解放の割合（経路数に対して）</summary>
        public const float BonusGoalUnlockRate = 0.01f;

        public int WaypointCount { get; }
        /// <summary>Goal に入るのに必要な最低手数</summary>
        public int RequiredMoveCount { get; }
        /// <summary>Start・Goal を除いた通過ブロック数</summary>
        public int MoveCount => cells.Count - 1 - (IsCleared ? 1 : 0);
        /// <summary>Goal に入れるか</summary>
        public bool CanEnterGoal => MoveCount >= RequiredMoveCount;
        /// <summary>チェックポイント</summary>
        public IReadOnlyList<Vector2Int> Checkpoints => checkpoints;

        /// <summary>通過済みチェックポイント数</summary>
        public int PassedCheckpointCount
        {
            get
            {
                int count = 0;
                foreach (Vector2Int checkpoint in checkpoints)
                {
                    if (indices.ContainsKey(checkpoint))
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>獲得★数（Goal 解放前は 0）</summary>
        public int StarCount
        {
            get
            {
                if (!CanEnterGoal && !IsCleared)
                {
                    return 0;
                }

                int passed = PassedCheckpointCount;
                if (passed >= checkpoints.Count) return 3;
                if (passed >= checkpoints.Count - 1) return 2;
                return 1;
            }
        }

        /// <summary>マップの宝箱でもらえるほねの合計</summary>
        public int BoneCount { get; }

        /// <summary>
        /// マスの宝箱のほね（通常のステージはレベルで増える。ボーナスステージはレベルの倍率）
        /// </summary>
        /// <param name="_subTypes"></param>
        private int treasureBones(IReadOnlyList<eBlockSubType> _subTypes)
        {
            if (mapData.isBonus)
            {
                return GimmickUtility.GetTreasureBones(_subTypes) * GimmickUtility.GetBoneMultiplier(mapData.difficulty);
            }
            return GimmickUtility.GetNormalTreasureBones(_subTypes, mapData.difficulty);
        }

        /// <summary>
        /// そのマスの宝箱のほね（表示用）
        /// </summary>
        /// <param name="_cell"></param>
        public int BonesAt(Vector2Int _cell)
        {
            return treasureBones(mapData.GetBlockSubTypes(_cell.x, _cell.y));
        }

        /// <summary>経路上で開けている宝箱のほねの合計</summary>
        public int CollectedBoneCount
        {
            get
            {
                int count = 0;
                foreach (Vector2Int cell in cells)
                {
                    count += treasureBones(mapData.GetBlockSubTypes(cell.x, cell.y));
                }
                return count;
            }
        }

        /// <summary>
        /// こわれた橋の一番後ろの順番（そこまでは戻れない。なければ -1）
        /// 橋は降りた時（その先に進んだ時）に壊れる
        /// </summary>
        public int LockedIndex
        {
            get
            {
                for (int i = cells.Count - 2; i >= 1; i--)
                {
                    if (mapData.HasSubType(cells[i].x, cells[i].y, eBlockSubType.Crumble))
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>PERFECT（★3 で、設計した経路の長さ以上を通ってクリア）</summary>
        public bool IsPerfect => IsCleared && StarCount >= 3 && MoveCount >= WaypointCount;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_mapData"></param>
        public PuzzlePath(MapData _mapData)
        {
            mapData = _mapData;
            rules = new PathRules(this, _mapData);
            mapData.TryFindBlock(eBlockType.Start, out start);
            WaypointCount = mapData.GetMaxWaypointIndex() + 1;
            // ボーナスステージは少し進めばゴールできる（宝箱は通った分だけ取れる）
            float goalRate = _mapData.isBonus ? BonusGoalUnlockRate : MapGenerator.GetGoalUnlockRate(_mapData.goalStep);
            RequiredMoveCount = Mathf.CeilToInt(WaypointCount * goalRate);
            checkpoints = mapData.GetCheckpoints();
            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    BoneCount += treasureBones(mapData.GetBlockSubTypes(x, y));
                }
            }

            Reset();
        }

        /// <summary>
        /// Start だけに戻す
        /// </summary>
        public void Reset()
        {
            cells.Clear();
            indices.Clear();
            warpJumps.Clear();
            openedSwitches.Clear();
            add(start);
            IsCleared = false;
        }

        /// <summary>
        /// cells[_index - 1] から cells[_index] へワープで飛んだか（線を引かない・パグが跳ぶ）
        /// </summary>
        /// <param name="_index"></param>
        public bool IsWarpArrival(int _index)
        {
            return warpJumps.Contains(_index - 1);
        }

        /// <summary>
        /// 経路にその色のカギがあるか（同じ色の扉が開く）
        /// </summary>
        /// <param name="_color"></param>
        public bool HasKey(int _color)
        {
            if (openedSwitches.Contains(_color))
            {
                return true;
            }
            foreach (Vector2Int cell in cells)
            {
                if (mapData.HasSubType(cell.x, cell.y, GimmickUtility.Keys[_color]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// こわれた橋のせいで、そのマスへ巻き戻せないか（経路上のマスのみ）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsRewindLocked(Vector2Int _cell)
        {
            int index = IndexOf(_cell);
            return index >= 0 && getRewindIndex(index) <= LockedIndex;
        }

        /// <summary>
        /// そのマスまで戻すと、先端から消える道のマス数（戻せない・戻らない時は 0）
        /// </summary>
        /// <param name="_cell"></param>
        public int GetRewindCount(Vector2Int _cell)
        {
            int index = IndexOf(_cell);
            if (IsCleared || index < 0)
            {
                return 0;
            }
            int target = getRewindIndex(index);
            if (target <= LockedIndex || target == cells.Count - 1)
            {
                return 0;
            }
            return cells.Count - 1 - target;
        }

        /// <summary>
        /// そのマスまで戻すと消える道のマス（先端から順に並ぶ。戻らない時は空）
        /// </summary>
        /// <param name="_cell"></param>
        public List<Vector2Int> GetRewindCells(Vector2Int _cell)
        {
            int count = GetRewindCount(_cell);
            return count > 0 ? cells.GetRange(cells.Count - count, count) : new List<Vector2Int>();
        }

        /// <summary>
        /// ドラッグを開始できるマスか
        /// </summary>
        /// <param name="_cell"></param>
        public bool CanBeginAt(Vector2Int _cell)
        {
            return !IsCleared && indices.ContainsKey(_cell);
        }

        /// <summary>
        /// 経路上の順番（経路外は -1）
        /// </summary>
        /// <param name="_cell"></param>
        public int IndexOf(Vector2Int _cell)
        {
            return indices.TryGetValue(_cell, out int index) ? index : -1;
        }

        /// <summary>
        /// 指定マスへ進む・巻き戻す
        /// </summary>
        /// <param name="_cell"></param>
        public eMoveResult MoveTo(Vector2Int _cell)
        {
            if (IsCleared || _cell == Head || !mapData.IsInside(_cell.x, _cell.y))
            {
                return eMoveResult.None;
            }

            int index = IndexOf(_cell);
            if (index >= 0)
            {
                // こわれた橋より前には戻れない
                int target = getRewindIndex(index);
                if (target <= LockedIndex || target == cells.Count - 1)
                {
                    return eMoveResult.None;
                }
                truncate(target);
                return eMoveResult.Undone;
            }

            if (!CanAdvanceTo(_cell))
            {
                return eMoveResult.None;
            }

            add(_cell);

            // ワープの入口は、出口がまだ経路になければ出口へ飛ぶ（出口がもう経路上なら普通のマス）
            eBlockSubType exclusive = GimmickUtility.GetExclusive(mapData.GetBlockSubTypes(_cell.x, _cell.y));
            if (GimmickUtility.IsWarpIn(exclusive)
                && mapData.TryFindSubType(GimmickUtility.WarpOuts[GimmickUtility.GetColorIndex(exclusive)], out Vector2Int exit)
                && !indices.ContainsKey(exit))
            {
                warpJumps.Add(cells.Count - 1);
                add(exit);
            }
            if (mapData.GetBlockType(_cell.x, _cell.y) == eBlockType.Goal)
            {
                IsCleared = true;
                return eMoveResult.Cleared;
            }
            return eMoveResult.Added;
        }

        /// <summary>
        /// 先端に隣接する未通過のマスか（クリア後は false）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsNextTo(Vector2Int _cell)
        {
            return !IsCleared && mapData.IsInside(_cell.x, _cell.y) && MapData.IsAdjacent(Head, _cell) && !indices.ContainsKey(_cell);
        }

        /// <summary>
        /// 先端から次に進めるマスか（隣で、通れて、ギミックの決まりに合う）
        /// </summary>
        /// <param name="_cell"></param>
        public bool CanAdvanceTo(Vector2Int _cell)
        {
            if (!IsNextTo(_cell))
            {
                return false;
            }

            switch (mapData.GetBlockType(_cell.x, _cell.y))
            {
                case eBlockType.Walkable:
                case eBlockType.Bridge:
                    return rules.IsGimmickAllowed(_cell) && rules.IsGateOpen(_cell);
                case eBlockType.Goal: return CanEnterGoal && rules.IsGimmickAllowed(_cell);
                default: return false;
            }
        }

        /// <summary>
        /// 隣の扉がカギを持っていないせいで閉じているか（拒否の演出用）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsBlockedByGate(Vector2Int _cell)
        {
            return rules.IsBlockedByGate(_cell);
        }

        /// <summary>
        /// 隣の通れるマスなのに、ギミック（やじるし・まがる・まっすぐ）のせいで進めないか（拒否の演出用）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsBlockedByGimmick(Vector2Int _cell)
        {
            return rules.IsBlockedByGimmick(_cell);
        }

        /// <summary>
        /// 隣の一方通行が向きの決まりで閉じているか（拒否の演出用）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsBlockedByOneWay(Vector2Int _cell)
        {
            return rules.IsBlockedByOneWay(_cell);
        }

        /// <summary>
        /// 経路上のマスを押した時に戻る順番（ワープの入口を押したら入る前まで戻す）
        /// </summary>
        /// <param name="_index"></param>
        private int getRewindIndex(int _index)
        {
            return warpJumps.Contains(_index) ? _index - 1 : _index;
        }

        /// <summary>
        /// 経路の末尾に追加
        /// </summary>
        /// <param name="_cell"></param>
        private void add(Vector2Int _cell)
        {
            indices[_cell] = cells.Count;
            cells.Add(_cell);
            // スイッチは通ったら開いたまま（巻き戻しても閉じない）
            foreach (eBlockSubType subType in mapData.GetBlockSubTypes(_cell.x, _cell.y))
            {
                if (GimmickUtility.IsSwitch(subType))
                {
                    openedSwitches.Add(GimmickUtility.GetColorIndex(subType));
                }
            }
        }

        /// <summary>
        /// 指定順番より後ろを削除
        /// </summary>
        /// <param name="_index"></param>
        private void truncate(int _index)
        {
            for (int i = cells.Count - 1; i > _index; i--)
            {
                indices.Remove(cells[i]);
            }
            cells.RemoveRange(_index + 1, cells.Count - _index - 1);
            warpJumps.RemoveWhere(_i => _i >= _index);
        }
    }
}
