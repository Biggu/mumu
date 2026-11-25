using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ギミックの通り方の決まり（扉・やじるし・まがる・まっすぐ）を、経路の今の状態に当てはめて判定する
    /// 新しいギミックの決まりはこのクラスに足す
    /// </summary>
    public class PathRules
    {
        private readonly PuzzlePath path;
        private readonly MapData mapData;

        /// <summary>
        /// 判定する経路とマップを受け取る
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_mapData"></param>
        public PathRules(PuzzlePath _path, MapData _mapData)
        {
            path = _path;
            mapData = _mapData;
        }

        /// <summary>
        /// 扉なら同じ色のカギを経路で通っているか（扉でなければ true）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsGateOpen(Vector2Int _cell)
        {
            eBlockSubType exclusive = GimmickUtility.GetExclusive(mapData.GetBlockSubTypes(_cell.x, _cell.y));
            return !GimmickUtility.IsGate(exclusive) || path.HasKey(GimmickUtility.GetColorIndex(exclusive));
        }

        /// <summary>
        /// 隣の扉がカギを持っていないせいで閉じているか（拒否の演出用）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsBlockedByGate(Vector2Int _cell)
        {
            if (!path.IsNextTo(_cell))
            {
                return false;
            }
            return !IsGateOpen(_cell);
        }

        /// <summary>
        /// 隣の通れるマスなのに、ギミック（やじるし・まがる・まっすぐ）のせいで進めないか（拒否の演出用）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsBlockedByGimmick(Vector2Int _cell)
        {
            if (!path.IsNextTo(_cell))
            {
                return false;
            }
            eBlockType type = mapData.GetBlockType(_cell.x, _cell.y);
            bool isPassable = type == eBlockType.Walkable || type == eBlockType.Bridge || (type == eBlockType.Goal && path.CanEnterGoal);
            return isPassable && !IsGimmickAllowed(_cell);
        }

        /// <summary>
        /// 隣の一方通行が向きの決まりで閉じているか（拒否の演出用、お知らせを出す）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsBlockedByOneWay(Vector2Int _cell)
        {
            eBlockSubType route = GimmickUtility.GetRoute(mapData.GetBlockSubTypes(_cell.x, _cell.y));
            return GimmickUtility.IsOneWay(route) && IsBlockedByGimmick(_cell);
        }

        /// <summary>
        /// 先端から _cell へ進むのがギミックの決まりに合うか（先端を出る向きと、_cell に入る向き）
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsGimmickAllowed(Vector2Int _cell)
        {
            Vector2Int head = path.Head;
            Vector2Int step = _cell - head;
            // ワープで着いた先端は入ってきた向きがない
            bool hasIncoming = path.Cells.Count >= 2 && !path.IsWarpArrival(path.Cells.Count - 1);
            Vector2Int headIn = hasIncoming ? head - path.Cells[path.Cells.Count - 2] : Vector2Int.zero;

            eBlockSubType headRoute = GimmickUtility.GetRoute(mapData.GetBlockSubTypes(head.x, head.y));
            if (headRoute != eBlockSubType.None && !GimmickUtility.IsAllowed(headRoute, headIn, step))
            {
                return false;
            }

            // 入る時は向きだけ（出る向きはまだ決まっていない）
            eBlockSubType cellRoute = GimmickUtility.GetRoute(mapData.GetBlockSubTypes(_cell.x, _cell.y));
            return !GimmickUtility.IsArrowLike(cellRoute) || GimmickUtility.IsAllowed(cellRoute, step, Vector2Int.zero);
        }
    }
}
