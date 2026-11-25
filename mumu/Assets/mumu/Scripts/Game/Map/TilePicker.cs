using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 画面の位置から、タッチしたマスを求める
    /// </summary>
    public class TilePicker
    {
        /// <summary>タッチ判定で調べるブロックの高さ</summary>
        private static readonly float[] CandidateHeights = { 0f, MapLayout.RaiseHeight };

        private readonly MapLayout layout;
        private readonly MapData mapData;
        private readonly RectTransform root;
        private readonly Func<Vector2Int, Block> getBlock;

        /// <summary>
        /// 判定に使うマップの情報
        /// </summary>
        /// <param name="_layout"></param>
        /// <param name="_mapData"></param>
        /// <param name="_root"></param>
        /// <param name="_getBlock"></param>
        public TilePicker(MapLayout _layout, MapData _mapData, RectTransform _root, Func<Vector2Int, Block> _getBlock)
        {
            layout = _layout;
            mapData = _mapData;
            root = _root;
            getBlock = _getBlock;
        }

        /// <summary>
        /// スクリーン座標 → マップのローカル座標
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        /// <param name="_local"></param>
        public bool ToLocal(Vector2 _screenPosition, Camera _camera, out Vector2 _local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, _screenPosition, _camera, out _local);
        }

        /// <summary>
        /// ローカル座標 → 見えているキューブ上面のマス
        /// </summary>
        /// <param name="_local"></param>
        /// <param name="_cell"></param>
        public bool TryGetCell(Vector2 _local, out Vector2Int _cell)
        {
            _cell = GridUtility.NoCell;
            int bestSortIndex = int.MinValue;

            // 低い位置・持ち上げ位置それぞれで推定したマスの周辺から、手前のブロックを優先
            foreach (float height in CandidateHeights)
            {
                Vector2Int estimate = estimateCell(_local, height);
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int x = estimate.x + dx;
                        int y = estimate.y + dy;
                        if (!mapData.IsInside(x, y))
                        {
                            continue;
                        }

                        Block block = getBlock(new Vector2Int(x, y));
                        if (block == null || !isPickable(block.BlockType) || block.SortIndex <= bestSortIndex || !layout.IsInTopFace(_local, new Vector2Int(x, y), block.IsRaised))
                        {
                            continue;
                        }

                        bestSortIndex = block.SortIndex;
                        _cell = new Vector2Int(x, y);
                    }
                }
            }

            return bestSortIndex != int.MinValue;
        }

        /// <summary>
        /// ローカル座標からマスを推定（BlockPosition の逆変換）
        /// </summary>
        /// <param name="_local"></param>
        /// <param name="_height"></param>
        private Vector2Int estimateCell(Vector2 _local, float _height)
        {
            Vector2 offset = layout.CenterOffset;
            float u = (_local.x + offset.x) / MapLayout.HalfTileWidth;
            float v = (_local.y - MapLayout.TopFaceOffsetY - _height + offset.y) / MapLayout.HalfTileHeight;
            int x = Mathf.RoundToInt((u + v) * 0.5f);
            int flippedY = Mathf.RoundToInt((v - u) * 0.5f);
            return new Vector2Int(x, (mapData.height - 1) - flippedY);
        }

        /// <summary>
/// ローカル座標のマス（盤面の外・水・空きマスでも返す。ブロックの判定はしない）
/// </summary>
/// <param name="_local"></param>
public Vector2Int EstimateCell(Vector2 _local)
{
    return estimateCell(_local, 0f);
}

        /// <summary>
        /// タッチ対象になるブロックか
        /// </summary>
        /// <param name="_type"></param>
        private static bool isPickable(eBlockType _type)
        {
            return _type != eBlockType.Empty && _type != eBlockType.Water;
        }
    }
}
