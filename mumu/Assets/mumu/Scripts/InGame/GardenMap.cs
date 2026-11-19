using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace mumu
{
    /// <summary>
    /// おにわ（パグが遊ぶ箱庭）のブロック表示
    /// </summary>
    public class GardenMap
    {
        /// <summary>おにわのテーマ</summary>
        public const int ThemeId = 1;

        private const int BlockSize = 60;
        private const float TopFaceOffsetY = BlockSize * 0.25f;
        private const float HalfTileWidth = BlockSize * 0.5f;
        private const float HalfTileHeight = BlockSize * 0.25f;

        /// <summary>レイアウト（. = 草、# = 木や岩、~ = 水。上の行が奥）</summary>
        private static readonly string[] Layout =
        {
            "#..#....#.",
            "..........",
            "#..~~.....",
            "...~~..#..",
            "..........",
            ".#........",
            "......~~..",
            "..#...~~.#",
            "..........",
            "#....#....",
        };

        private readonly List<Vector2Int> walkableCells = new List<Vector2Int>();
        private GameObject mapRoot = null;
        private int width = 0;
        private int height = 0;
        private float centerOffsetX = 0f;
        private float centerOffsetY = 0f;
        private bool[,] isWalkable = null;
        private Block[,] blockArray = null;

        /// <summary>テーマ画像</summary>
        public ThemeSprites Sprites { get; private set; }
        /// <summary>草のマス</summary>
        public IReadOnlyList<Vector2Int> WalkableCells => walkableCells;

        /// <summary>
        /// 読み込みとブロック生成
        /// </summary>
        /// <param name="_mapRoot"></param>
        /// <param name="_token"></param>
        public async UniTask<bool> InitAsync(GameObject _mapRoot, CancellationToken _token)
        {
            mapRoot = _mapRoot;
            Block prefab = await ResourceManager.Instance.LoadAsync<Block>(ResourceManager.GetInGamePrefabPath("block"), _token);
            Sprites = await ThemeSprites.LoadAsync(ThemeId, _token);
            if (prefab == null || Sprites == null)
            {
                return false;
            }

            height = Layout.Length;
            width = Layout[0].Length;
            isWalkable = new bool[width, height];
            blockArray = new Block[width, height];
            centerOffsetX = (width - height) * HalfTileWidth * 0.5f;
            centerOffsetY = (width + height - 2) * HalfTileHeight * 0.5f;

            var blocks = new List<Block>(width * height);
            var noSubTypes = new List<eBlockSubType>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    eBlockType type = getType(x, y);
                    Block block = Object.Instantiate(prefab, mapRoot.transform);
                    var rect = (RectTransform)block.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(BlockSize, BlockSize);
                    rect.anchoredPosition = getBlockPosition(x, y);
                    block.SetOrder(x, (height - 1) - y, width, height);
                    block.Setup(type, noSubTypes, Sprites);
                    block.SetGardenGround();
                    blocks.Add(block);
                    blockArray[x, y] = block;

                    if (type == eBlockType.Walkable)
                    {
                        isWalkable[x, y] = true;
                        walkableCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 奥から手前の順に並べる
            blocks.Sort((_a, _b) => _a.SortIndex.CompareTo(_b.SortIndex));
            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].transform.SetSiblingIndex(i);
            }
            return true;
        }

        /// <summary>
        /// 草のマスか
        /// </summary>
        /// <param name="_cell"></param>
        public bool IsWalkable(Vector2Int _cell)
        {
            return _cell.x >= 0 && _cell.y >= 0 && _cell.x < width && _cell.y < height && isWalkable[_cell.x, _cell.y];
        }

        /// <summary>
        /// マスのブロック（範囲外は null）
        /// </summary>
        /// <param name="_cell"></param>
        public Block GetBlock(Vector2Int _cell)
        {
            bool isInside = _cell.x >= 0 && _cell.y >= 0 && _cell.x < width && _cell.y < height;
            return isInside ? blockArray[_cell.x, _cell.y] : null;
        }

        /// <summary>
        /// 草のマスだけを通る最短の道（_from を除き _to まで、行けなければ空）
        /// </summary>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        /// <param name="_maxSteps"></param>
        public List<Vector2Int> FindPath(Vector2Int _from, Vector2Int _to, int _maxSteps)
        {
            var previous = new Dictionary<Vector2Int, Vector2Int> { { _from, _from } };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(_from);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == _to)
                {
                    var path = new List<Vector2Int>();
                    for (Vector2Int p = current; p != _from; p = previous[p])
                    {
                        path.Add(p);
                    }
                    path.Reverse();
                    return path.Count <= _maxSteps ? path : path.GetRange(0, _maxSteps);
                }
                foreach (Vector2Int d in directions)
                {
                    Vector2Int next = current + d;
                    if (IsWalkable(next) && !previous.ContainsKey(next))
                    {
                        previous[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return new List<Vector2Int>();
        }

        /// <summary>
        /// マスの上面の中心（mapRoot の座標系）
        /// </summary>
        /// <param name="_cell"></param>
        public Vector2 GetTopFacePosition(Vector2Int _cell)
        {
            return getBlockPosition(_cell.x, _cell.y) + new Vector2(0f, TopFaceOffsetY);
        }

        /// <summary>
        /// 全体の範囲（mapRoot の座標系）
        /// </summary>
        public Rect GetContentBounds()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            Vector2 half = new Vector2(BlockSize, BlockSize) * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 position = getBlockPosition(x, y);
                    min = Vector2.Min(min, position - half);
                    max = Vector2.Max(max, position + half + new Vector2(0f, Block.RaiseHeight));
                }
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// レイアウトの文字 → ブロック種別（木や岩の種類は位置で決める）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private static eBlockType getType(int _x, int _y)
        {
            switch (Layout[_y][_x])
            {
                case '#':
                    return BlockTypeUtility.Obstructs[(_x * 7 + _y * 3) % BlockTypeUtility.Obstructs.Length];
                case '~':
                    return eBlockType.Water;
                default:
                    return eBlockType.Walkable;
            }
        }

        /// <summary>
        /// マスの表示位置（InGameMap と同じ並び）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private Vector2 getBlockPosition(int _x, int _y)
        {
            int flippedY = (height - 1) - _y;
            return new Vector2(
                (_x - flippedY) * HalfTileWidth - centerOffsetX,
                (_x + flippedY) * HalfTileHeight - centerOffsetY);
        }
    }
}
