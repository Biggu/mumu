using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// エディタでの確認用の操作（ゲームの動作には使わない）
    /// </summary>
    public class MapDebugTools
    {
        /// <summary>
        /// 設計した経路どおりに Goal まで進める（クリア演出も通常どおり）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_path"></param>
        /// <param name="_move"></param>
        /// <param name="_refresh"></param>
        public void Solve(MapData _mapData, PuzzlePath _path, Func<Vector2Int, eMoveResult> _move, Action _refresh)
        {
            if (_path.IsCleared || !_mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal))
            {
                return;
            }

            _path.Reset();
            foreach (Vector2Int waypoint in _mapData.GetWaypointPositions(out _))
            {
                _path.MoveTo(waypoint);
            }
            _refresh();
            _move(goal);
        }

        /// <summary>
        /// 各マスに経路の番号を表示する
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_blocks"></param>
        /// <param name="_isVisible"></param>
        public void SetWaypointLabels(MapData _mapData, MapBlockView _blocks, bool _isVisible)
        {
            for (int x = 0; x < _mapData.width; x++)
            {
                for (int y = 0; y < _mapData.height; y++)
                {
                    int waypoint = _mapData.GetWaypointIndex(x, y);
                    _blocks.Get(new Vector2Int(x, y)).SetDebugLabel(_isVisible && waypoint >= 0 ? (waypoint + 1).ToString() : null);
                }
            }
        }
    }
}
