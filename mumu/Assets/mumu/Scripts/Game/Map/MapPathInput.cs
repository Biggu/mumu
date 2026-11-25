using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 押す・ドラッグで経路を進める操作（押した場所の判定、ドラッグの向き、1マス進める）
    /// </summary>
    public class MapPathInput
    {
        /// <summary>経路の先端からこのマス数以内の経路外ブロックは、押すと経路を伸ばすドラッグを始める（2 = 5x5）</summary>
        private static int NoDragRange => MapInputSettings.Current.NoDragRange;

        /// <summary>ドラッグで1マス進むのに必要な移動量（ローカル座標。1マスの半分）</summary>
        private static float DragStepDistance => MapLayout.CellDistance * MapInputSettings.Current.DragStepRate;

        /// <summary>ドラッグの4方向（マップ座標と、ローカル座標上の1マス分のベクトル）</summary>
        private static readonly (Vector2Int Direction, Vector2 Local)[] Directions =
        {
            (new Vector2Int(1, 0), new Vector2(MapLayout.HalfTileWidth, MapLayout.HalfTileHeight)),
            (new Vector2Int(-1, 0), new Vector2(-MapLayout.HalfTileWidth, -MapLayout.HalfTileHeight)),
            (new Vector2Int(0, -1), new Vector2(-MapLayout.HalfTileWidth, MapLayout.HalfTileHeight)),
            (new Vector2Int(0, 1), new Vector2(MapLayout.HalfTileWidth, -MapLayout.HalfTileHeight)),
        };

        private readonly TilePicker picker;
        private readonly RefusalFeedback refusal;
        private readonly MapData mapData;
        private readonly PuzzlePath path;
        /// <summary>1手進める処理（描画・効果音・クリアを含む）</summary>
        private readonly Func<Vector2Int, eMoveResult> move;

        /// <summary>
        /// 操作に使う判定・演出・経路を受け取る
        /// </summary>
        /// <param name="_picker"></param>
        /// <param name="_refusal"></param>
        /// <param name="_mapData"></param>
        /// <param name="_path"></param>
        /// <param name="_move"></param>
        public MapPathInput(TilePicker _picker, RefusalFeedback _refusal, MapData _mapData, PuzzlePath _path, Func<Vector2Int, eMoveResult> _move)
        {
            picker = _picker;
            refusal = _refusal;
            mapData = _mapData;
            path = _path;
            move = _move;
        }

        /// <summary>
        /// 押した場所の種類（経路上なら巻き戻し、次に進めるマスなら進めてドラッグ開始）
        /// 押したマスがブロックでなくても、先端の5×5の中ならドラッグを始められる
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        public ePressTarget GetPressTarget(Vector2 _screenPosition, Camera _camera)
        {
            if (!picker.ToLocal(_screenPosition, _camera, out Vector2 local))
            {
                return ePressTarget.Background;
            }

            // 水・空きマス・盤面の外でも、先端の周辺ならドラッグを始められる（移動はしない）
            if (!picker.TryGetCell(local, out Vector2Int cell))
            {
                return !path.IsCleared && isNearHead(picker.EstimateCell(local))
                    ? ePressTarget.NearHead
                    : ePressTarget.Background;
            }

            refusal.Reset();
            if (path.CanAdvanceTo(cell) || (path.CanBeginAt(cell) && cell != path.Head))
            {
                move(cell);
                return ePressTarget.PathHead;
            }
            if (path.CanBeginAt(cell))
            {
                return ePressTarget.PathHead;
            }

            refusal.Check(cell, path, mapData);
            return isNearHead(cell) ? ePressTarget.NearHead : ePressTarget.Block;
        }

        /// <summary>
        /// 起点から半マス以上動いたか。動いていれば一番近い斜めの向き（マップの向き）を返す（ローカル座標なので拡大縮小に影響されない）
        /// </summary>
        /// <param name="_fromScreen"></param>
        /// <param name="_toScreen"></param>
        /// <param name="_camera"></param>
        /// <param name="_direction"></param>
        public bool TryGetDragDirection(Vector2 _fromScreen, Vector2 _toScreen, Camera _camera, out Vector2Int _direction)
        {
            _direction = Vector2Int.zero;
            if (!picker.ToLocal(_fromScreen, _camera, out Vector2 from) || !picker.ToLocal(_toScreen, _camera, out Vector2 to))
            {
                return false;
            }

            Vector2 delta = to - from;
            if (delta.magnitude < DragStepDistance)
            {
                return false;
            }

            float best = float.MinValue;
            foreach ((Vector2Int direction, Vector2 local) in Directions)
            {
                float dot = Vector2.Dot(delta, local);
                if (dot > best)
                {
                    best = dot;
                    _direction = direction;
                }
            }
            return true;
        }

        /// <summary>
        /// ドラッグで先端から向きへ1マス進める（経路上なら1つ前のマスにだけ戻れる）
        /// </summary>
        /// <param name="_direction"></param>
        public void DragStep(Vector2Int _direction)
        {
            Vector2Int target = path.Head + _direction;
            if (!mapData.IsInside(target.x, target.y))
            {
                return;
            }

            int index = path.IndexOf(target);
            if (index >= 0 && !isOneStepBack(index))
            {
                return;
            }

            if (move(target) == eMoveResult.None)
            {
                refusal.Check(target, path, mapData);
            }
        }

        /// <summary>
        /// 経路上の順番が1つ前のマス（先端の一つ後ろ）か
        /// </summary>
        /// <param name="_index"></param>
        private bool isOneStepBack(int _index)
        {
            return _index == path.Cells.Count - 2;
        }

        /// <summary>
        /// 経路の先端付近か（クリア後は対象外）。盤面の外のマスも判定できる
        /// </summary>
        /// <param name="_cell"></param>
        private bool isNearHead(Vector2Int _cell)
        {
            Vector2Int head = path.Head;
            return !path.IsCleared
                && Mathf.Abs(_cell.x - head.x) <= NoDragRange
                && Mathf.Abs(_cell.y - head.y) <= NoDragRange;
        }
    }
}