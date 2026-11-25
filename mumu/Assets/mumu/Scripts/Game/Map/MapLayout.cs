using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マスの表示位置（アイソメ表示）。InGameMap と GardenMap で使う並び
    /// </summary>
    public class MapLayout
    {
        /// <summary>ブロックの幅（px）</summary>
        public const int BlockSize = 60;
        public const int BlockSpacing = 0;
        /// <summary>ブロック中心からキューブ上面の中心までの高さ</summary>
        public const float TopFaceOffsetY = BlockSize * 0.25f;
        public const float TileWidth = BlockSize + BlockSpacing;
        public const float HalfTileWidth = TileWidth * 0.5f;
        public const float HalfTileHeight = TileWidth * 0.25f;
        /// <summary>通れない・通過済みブロックの持ち上げ量</summary>
        public const float RaiseHeight = 6f;

        /// <summary>隣のマスまでの距離（ローカル座標）</summary>
        public static float CellDistance => new Vector2(HalfTileWidth, HalfTileHeight).magnitude;

        private readonly MapData mapData;

        /// <summary>表示の中心を合わせるためのずれ（マップ生成時に決める）</summary>
        public Vector2 CenterOffset { get; set; } = Vector2.zero;

        /// <summary>
        /// マップのサイズに合わせて作る
        /// </summary>
        /// <param name="_mapData"></param>
        public MapLayout(MapData _mapData)
        {
            mapData = _mapData;
        }

        /// <summary>
        /// マスの表示位置（mapRoot の座標系）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public Vector2 BlockPosition(int _x, int _y)
        {
            return GridUtility.IsoPosition(_x, _y, mapData.height, HalfTileWidth, HalfTileHeight) - CenterOffset;
        }

        /// <summary>
        /// マスの上面の中心（mapRoot の座標系）
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_isRaised"></param>
        public Vector2 TopFacePosition(Vector2Int _cell, bool _isRaised)
        {
            Vector2 position = BlockPosition(_cell.x, _cell.y);
            position.y += TopFaceOffsetY + (_isRaised ? RaiseHeight : 0f);
            return position;
        }

        /// <summary>
        /// 空きマス以外の範囲（mapRoot の座標系）
        /// </summary>
        public Rect ContentBounds()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    if (mapData.GetBlockType(x, y) == eBlockType.Empty)
                    {
                        continue;
                    }

                    GridUtility.ExpandBounds(BlockPosition(x, y), BlockSize, RaiseHeight, ref min, ref max);
                }
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// ローカル座標がキューブ上面のひし形の中か
        /// </summary>
        /// <param name="_local"></param>
        /// <param name="_cell"></param>
        /// <param name="_isRaised"></param>
        public bool IsInTopFace(Vector2 _local, Vector2Int _cell, bool _isRaised)
        {
            Vector2 diff = _local - TopFacePosition(_cell, _isRaised);
            return Mathf.Abs(diff.x) / HalfTileWidth + Mathf.Abs(diff.y) / HalfTileHeight <= 1f;
        }
    }
}
