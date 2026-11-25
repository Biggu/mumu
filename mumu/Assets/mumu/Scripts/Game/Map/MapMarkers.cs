using System;
using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップの目印（走るパグと、次に進む方向を示す矢印）
    /// </summary>
    public class MapMarkers
    {
        /// <summary>走るパグの大きさ（Goal のパグと同じ）</summary>
        private const float PlayerPugSize = 44f;
        /// <summary>矢印の大きさ</summary>
        private const float ArrowSize = 26f;
        /// <summary>上面の中心から矢印の先端までの高さ（座っているパグの頭の上）</summary>
        private const float ArrowOffsetY = 42f;

        private PlayerPug playerPug = null;
        private PointArrow pointArrow = null;
        private readonly List<Vector2> pathPoints = new List<Vector2>();
        /// <summary>矢印を表示するか（タイトルの見本では出さない）</summary>
        private bool isArrowEnabled = true;

        /// <summary>走るパグ（パグの画像がなければ null）</summary>
        public PlayerPug PlayerPug => playerPug;
        /// <summary>矢印を表示する設定か</summary>
        public bool IsArrowEnabled => isArrowEnabled;

        /// <summary>
        /// 走るパグと矢印を作る（ブロックより手前に表示）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_pugSprites"></param>
        /// <param name="_path"></param>
        /// <param name="_getBlock"></param>
        public void Create(Transform _parent, PugSprites _pugSprites, PuzzlePath _path, Func<Vector2Int, Block> _getBlock)
        {
            if (_pugSprites != null)
            {
                playerPug = PlayerPug.Create(_parent, _pugSprites, PlayerPugSize, MapLayout.CellDistance);
                if (playerPug != null)
                {
                    playerPug.SetHopCells(_path.Checkpoints);
                    playerPug.SetBlockProvider(_getBlock);
                    playerPug.SetWarpProvider(_path.IsWarpArrival);
                }
            }
            pointArrow = PointArrow.Create(_parent, ArrowSize);
        }

        /// <summary>
        /// 矢印を表示するか（デモの見本では出さない）
        /// </summary>
        /// <param name="_isEnabled"></param>
        public void SetArrowEnabled(bool _isEnabled)
        {
            isArrowEnabled = _isEnabled;
        }

        /// <summary>
        /// 走るパグと矢印を経路に合わせる
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_topFacePosition">マスの上面の中心を返す処理</param>
        public void Refresh(PuzzlePath _path, Func<Vector2Int, Vector2> _topFacePosition)
        {
            IReadOnlyList<Vector2Int> cells = _path.Cells;
            pathPoints.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                pathPoints.Add(_topFacePosition(cells[i]));
            }

            // クリア時は Goal のパグの隣に並ぶ
            if (_path.IsCleared)
            {
                pathPoints[pathPoints.Count - 1] -= new Vector2(Block.GoalPairOffset, 0f);
            }

            if (playerPug != null)
            {
                playerPug.SetPath(cells, pathPoints);
            }

            if (pointArrow != null)
            {
                pointArrow.SetVisible(isArrowEnabled && !_path.IsCleared);
                pointArrow.SetTarget(_topFacePosition(_path.Head) + new Vector2(0f, ArrowOffsetY));
            }
        }
    }
}
