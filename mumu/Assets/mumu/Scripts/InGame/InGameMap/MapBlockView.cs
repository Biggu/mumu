using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マスのブロックの生成と、マップ・経路の見た目の反映
    /// </summary>
    public class MapBlockView
    {
        /// <summary>経路の先端からこのマス数以内に来たら Goal の子が泣きやむ</summary>
        private const int GoalNoticeDistance = 3;
        /// <summary>マップの外の枠のプレハブ（Resources から）</summary>
        private const string RingPath = "Prefabs/InGame/ring_block";

        private readonly MapLayout layout;
        private readonly GimmickSync gimmickSync = new GimmickSync();
        private Block[,] blocks = null;
        /// <summary>マップの外の枠（軽いブロック。作り直さず、足りない分だけ増やして使い回す）</summary>
        private readonly List<RingBlock> ring = new List<RingBlock>();
        private Transform ringParent = null;

        /// <summary>枠の1マス（配置と並び順）</summary>
        private struct RingCell
        {
            public int X;
            public int Y;
            public int Pick;
            public int Key;
        }

        /// <summary>
        /// 表示位置の計算を受け取る
        /// </summary>
        /// <param name="_layout"></param>
        public MapBlockView(MapLayout _layout)
        {
            layout = _layout;
        }

        /// <summary>
        /// マスのブロック
        /// </summary>
        /// <param name="_cell"></param>
        public Block Get(Vector2Int _cell)
        {
            return blocks[_cell.x, _cell.y];
        }

        /// <summary>
        /// ブロックを生成して並べる（奥から手前の順）
        /// </summary>
        /// <param name="_prefab"></param>
        /// <param name="_parent"></param>
        /// <param name="_mapData"></param>
        public void Create(Block _prefab, Transform _parent, MapData _mapData)
        {
            int xCount = _mapData.width;
            int yCount = _mapData.height;

            blocks = new Block[xCount, yCount];
            ringParent = _parent;
            var created = new List<Block>(xCount * yCount);
            var size = new Vector2(MapLayout.BlockSize, MapLayout.BlockSize);
            var center = new Vector2(0.5f, 0.5f);
            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    Block newBlock = Object.Instantiate(_prefab, _parent);
                    RectTransform rect = (RectTransform)newBlock.transform;

                    rect.anchorMin = center;
                    rect.anchorMax = center;
                    rect.pivot = center;
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                    rect.sizeDelta = size;
                    rect.anchoredPosition = layout.BlockPosition(x, y);

                    newBlock.SetOrder(x, (yCount - 1) - y, xCount, yCount);
                    blocks[x, y] = newBlock;
                    created.Add(newBlock);
                }
            }

            created.Sort((_a, _b) => _a.SortIndex.CompareTo(_b.SortIndex));
            for (int i = 0; i < created.Count; i++)
            {
                created[i].transform.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// マップデータ（地形・ギミック・テーマ・チェックポイント）をブロックに反映
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_sprites"></param>
        /// <param name="_pugSprites"></param>
        /// <param name="_checkpoints"></param>
        public void ApplyMap(MapData _mapData, ThemeSprites _sprites, PugSprites _pugSprites, IReadOnlyList<Vector2Int> _checkpoints)
        {
            buildRing(_mapData, _sprites);

            for (int x = 0; x < _mapData.width; x++)
            {
                for (int y = 0; y < _mapData.height; y++)
                {
                    Block block = blocks[x, y];
                    block.Setup(_mapData.GetBlockType(x, y), _mapData.GetBlockSubTypes(x, y), _sprites);
                    block.SetupPug(_pugSprites);
                    block.SetDecoVisible(!_mapData.IsNearStartOrGoal(x, y));
                }
            }

            for (int i = 0; i < _checkpoints.Count; i++)
            {
                Get(_checkpoints[i]).SetCheckpoint(i + 1);
            }
        }

        /// <summary>
        /// マップの後ろ2マス分の枠を並べる（外側は高いデコ、内側は 60% 小さい・普通、20% なし、20% 高い）
        /// 枠は軽いブロック（土台とデコだけ）で、作り直さず使い回す。デコの選び方は位置とテーマで決まる（毎回同じ）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_sprites"></param>
        private void buildRing(MapData _mapData, ThemeSprites _sprites)
        {
            if (_sprites == null || ringParent == null)
            {
                return;
            }

            int w = _mapData.width;
            int h = _mapData.height;
            var cells = new List<RingCell>();
            for (int x = 0; x <= w + 1; x++)
            {
                for (int y = -2; y <= h - 1; y++)
                {
                    bool backBand = (y == -1 || y == -2);
                    bool rightBand = (x == w || x == w + 1);
                    if (!backBand && !rightBand)
                    {
                        continue;
                    }

                    bool outer = (y == -2) || (x == w + 1);
                    int hash = hashCell(x, y, _mapData.themeId);
                    // 後ろの枠（y が負）はマップより奥に描く（並び順の鍵を小さくする）
                    int key = w * h - ((backBand ? h : y) * w + x);
                    cells.Add(new RingCell { X = x, Y = y, Pick = outer ? (hash % 4) : pickInner(hash), Key = key });
                }
            }
            cells.Sort((_a, _b) => _a.Key.CompareTo(_b.Key));

            var size = new Vector2(MapLayout.BlockSize, MapLayout.BlockSize);
            var center = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < cells.Count; i++)
            {
                if (i >= ring.Count)
                {
                    if (!addRingBlock())
                    {
                        break;
                    }
                }

                RingCell cell = cells[i];
                RingBlock b = ring[i];
                RectTransform rect = (RectTransform)b.transform;
                rect.anchorMin = center;
                rect.anchorMax = center;
                rect.pivot = center;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.sizeDelta = size;
                rect.anchoredPosition = layout.BlockPosition(cell.X, cell.Y);
                rect.SetSiblingIndex(i);

                Sprite deco = cell.Pick >= 0 ? _sprites.GetDeco(cell.Pick) : null;
                Sprite[] frames = cell.Pick >= 0 ? _sprites.GetDecoFrames(cell.Pick) : null;
                b.Show(_sprites.Obstruct, deco, frames, MapLayout.BlockSize);
            }

            for (int i = cells.Count; i < ring.Count; i++)
            {
                ring[i].Hide();
            }
        }

        /// <summary>
        /// 枠のブロックを1つ増やす（プレハブがなければ false）
        /// </summary>
        private bool addRingBlock()
        {
            GameObject prefab = ResourceManager.Instance.Load<GameObject>(RingPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[MapBlockView] 枠のプレハブがありません: {RingPath}");
                return false;
            }
            GameObject go = Object.Instantiate(prefab, ringParent, false);
            RingBlock b = go.GetComponent<RingBlock>();
            if (b == null)
            {
                return false;
            }
            ring.Add(b);
            return true;
        }

        /// <summary>
        /// 内側の枠のデコ（60%: 小さい・普通 4〜7、20%: なし -1、20%: 高い 0〜3）
        /// </summary>
        /// <param name="_hash"></param>
        private static int pickInner(int _hash)
        {
            int r = _hash % 100;
            if (r < 60)
            {
                return 4 + (_hash / 100) % 4;
            }
            if (r < 80)
            {
                return -1;
            }
            return (_hash / 100) % 4;
        }

        /// <summary>
        /// マスの位置とテーマから決まる、毎回同じ値（0 以上）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_themeId"></param>
        private static int hashCell(int _x, int _y, int _themeId)
        {
            unchecked
            {
                uint v = (uint)(_x * 73856093) ^ (uint)(_y * 19349663) ^ (uint)(_themeId * 83492791);
                v ^= v >> 13;
                v *= 0x5bd1e995;
                v ^= v >> 15;
                return (int)(v & 0x7fffffff);
            }
        }

        /// <summary>
        /// 経路を全ブロックに反映（1ブロック1回ずつ。経路に残るブロックのアニメーションを再生し直さない）
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_mapData"></param>
        public void ApplyPath(PuzzlePath _path, MapData _mapData, HintPlan _hint)
        {
            for (int x = 0; x < _mapData.width; x++)
            {
                for (int y = 0; y < _mapData.height; y++)
                {
                    applyCell(new Vector2Int(x, y), _path, _mapData, _hint);
                }
            }
        }

        /// <summary>
        /// 1マス分の経路の見た目（順番・次に進めるか・Goal・線・ギミック）
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_path"></param>
        /// <param name="_mapData"></param>
        private void applyCell(Vector2Int _cell, PuzzlePath _path, MapData _mapData, HintPlan _hint)
        {
            IReadOnlyList<Vector2Int> cells = _path.Cells;
            int last = cells.Count - 1;
            int step = _path.IndexOf(_cell);
            int lockedIndex = _path.LockedIndex;
            bool isCleared = _path.IsCleared;
            Block block = Get(_cell);

            block.SetPathStep(step);
            block.SetHighlight(_path.CanAdvanceTo(_cell));
            block.SetGoalUnlocked(_path.CanEnterGoal);
            block.SetGoalNoticed(!isCleared && Mathf.Abs(_cell.x - _path.Head.x) + Mathf.Abs(_cell.y - _path.Head.y) <= GoalNoticeDistance);

            // クリア時の喜びは PlayClearAsync で走るパグの到着後に反映
            if (!isCleared)
            {
                block.SetCleared(false);
            }

            // ワープで飛んだ所には線を引かない
            Vector2Int toPrevious = step > 0 && !_path.IsWarpArrival(step) ? cells[step - 1] - _cell : Vector2Int.zero;
            Vector2Int toNext = step >= 0 && step < last && !_path.IsWarpArrival(step + 1) ? cells[step + 1] - _cell : Vector2Int.zero;
            block.SetLine(step >= 0, toPrevious, toNext);

            // 経路の外にあるヒントのマスだけ、ヒントの線を出す（経路に乗ったら普通の線になる）
            bool isHint = _hint != null && step < 0 && _hint.Contains(_cell);
            block.SetHint(isHint, isHint ? _hint.PreviousDirection(_cell) : Vector2Int.zero, isHint ? _hint.NextDirection(_cell) : Vector2Int.zero);

            gimmickSync.Apply(block, _cell, step, lockedIndex, _path, _mapData);
        }

    }
}
