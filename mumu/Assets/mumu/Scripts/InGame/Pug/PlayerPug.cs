using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 経路に沿って走るパグ（先端に着いたら座る）
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PlayerPug : MonoBehaviour
    {
        private const float SitFps = 3f;
        private const float RunFps = 12f;
        private const float HappyFps = 8f;
        /// <summary>巻き戻しでこのマス数より離れたら走らずに移動</summary>
        private const int JumpThreshold = 1;
        private const float HopHeight = 14f;
        private const float HopDuration = 0.35f;

        private Image image = null;
        private SpriteFrameAnimation frameAnimation = null;
        private PugSprites sprites = null;
        private RectTransform rect = null;

        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private readonly List<Vector2> points = new List<Vector2>();
        /// <summary>向かっている（または立っている）点の番号</summary>
        private int targetIndex = 0;
        private Vector2 position = Vector2.zero;
        private float cellDistance = 1f;
        private float baseSpeed = 0f;
        private float catchUpSpeed = 0f;
        private float maxSpeed = 0f;

        /// <summary>跳ねるマス（チェックポイント）</summary>
        private readonly HashSet<Vector2Int> hopCells = new HashSet<Vector2Int>();
        /// <summary>跳ね済みのマス（経路から外れたら戻す）</summary>
        private readonly HashSet<Vector2Int> hoppedCells = new HashSet<Vector2Int>();
        private float hopStartTime = float.NegativeInfinity;

        /// <summary>マスのブロック（描画順をブロックの間に合わせるため）</summary>
        private Func<Vector2Int, Block> getBlockFunc = null;
        /// <summary>経路のその順番へワープで着いたか（走らずに跳んで移る）</summary>
        private Func<int, bool> isWarpArrivalFunc = null;

        private bool isHappy = false;
        private bool isFacingLeft = false;

        /// <summary>先端に着いているか</summary>
        public bool IsArrived => points.Count > 0 && targetIndex == points.Count - 1 && position == points[targetIndex];

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_sprites"></param>
        /// <param name="_size"></param>
        /// <param name="_cellDistance"></param>
        public static PlayerPug Create(Transform _parent, PugSprites _sprites, float _size, float _cellDistance)
        {
            GameObject go = PartsLoaderManager.Instance.Create(ePart.PlayerPug, _parent);
            if (go == null)
            {
                return null;
            }
            var pug = go.AddComponent<PlayerPug>();
            pug.setup(_sprites, _size, _cellDistance);
            return pug;
        }

        /// <summary>
        /// 経路を設定（短くなった時は近ければ走って戻り、遠ければその場へ移動）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_points"></param>
        public void SetPath(IReadOnlyList<Vector2Int> _cells, IReadOnlyList<Vector2> _points)
        {
            bool isFirst = points.Count == 0;
            int common = 0;
            while (common < cells.Count && common < _cells.Count && cells[common] == _cells[common])
            {
                common++;
            }

            cells.Clear();
            cells.AddRange(_cells);
            hoppedCells.IntersectWith(cells);
            points.Clear();
            points.AddRange(_points);

            if (isFirst)
            {
                targetIndex = 0;
                position = points[0];
            }
            else if (targetIndex >= common)
            {
                // 立っていたマスが経路から外れた
                int newIndex = Mathf.Max(0, common - 1);
                if (targetIndex - newIndex > JumpThreshold)
                {
                    position = points[newIndex];
                }
                targetIndex = newIndex;
            }
            targetIndex = Mathf.Min(targetIndex, points.Count - 1);
            apply();
        }

        /// <summary>
        /// マスのブロックを返す処理（立っているブロックのすぐ後ろに描いて、手前の木などに隠れるようにする）
        /// </summary>
        /// <param name="_getBlock"></param>
        public void SetBlockProvider(Func<Vector2Int, Block> _getBlock)
        {
            getBlockFunc = _getBlock;
        }

        /// <summary>
        /// ワープで着いたかを返す処理
        /// </summary>
        /// <param name="_isWarpArrival"></param>
        public void SetWarpProvider(Func<int, bool> _isWarpArrival)
        {
            isWarpArrivalFunc = _isWarpArrival;
        }

        /// <summary>
        /// 着いた時に跳ねるマス（チェックポイント）
        /// </summary>
        /// <param name="_cells"></param>
        public void SetHopCells(IEnumerable<Vector2Int> _cells)
        {
            hopCells.Clear();
            hopCells.UnionWith(_cells);
        }

        /// <summary>
        /// 喜ぶ（先端に着いている時だけ反映）
        /// </summary>
        /// <param name="_isHappy"></param>
        public void SetHappy(bool _isHappy)
        {
            isHappy = _isHappy;
            if (_isHappy)
            {
                isFacingLeft = false;
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_sprites"></param>
        /// <param name="_size"></param>
        /// <param name="_cellDistance"></param>
        private void setup(PugSprites _sprites, float _size, float _cellDistance)
        {
            sprites = _sprites;
            image = GetComponent<Image>();
            UiFactory.SetPugImage(image, false);
            frameAnimation = gameObject.AddComponent<SpriteFrameAnimation>();

            // Goal のパグと同じく足元を上面の中心に合わせる
            rect = (RectTransform)transform;
            UiFactory.SetFootRect(rect, _size);

            cellDistance = Mathf.Max(1f, _cellDistance);
            PugAura.Attach(rect, sprites.BoyTier, _size);
            baseSpeed = cellDistance * 8f;
            catchUpSpeed = cellDistance * 3f;
            maxSpeed = cellDistance * 30f;
        }

        /// <summary>
        /// 経路に沿って移動（遅れているほど速く走る）
        /// </summary>
        private void Update()
        {
            if (points.Count == 0)
            {
                return;
            }

            int last = points.Count - 1;
            float remaining = (last - targetIndex) + Vector2.Distance(position, points[targetIndex]) / cellDistance;
            float step = Mathf.Min(maxSpeed, baseSpeed + catchUpSpeed * remaining) * Time.unscaledDeltaTime;

            while (step > 0f)
            {
                // ワープの出口へは入口から一瞬で移る（ぴょんと跳ねる）
                if (targetIndex > 0 && isWarpArrivalFunc != null && isWarpArrivalFunc(targetIndex) && position == points[targetIndex - 1])
                {
                    position = points[targetIndex];
                    hopStartTime = Time.unscaledTime;
                    if (targetIndex >= last)
                    {
                        break;
                    }
                    targetIndex++;
                    continue;
                }

                Vector2 toTarget = points[targetIndex] - position;
                float distance = toTarget.magnitude;
                if (distance > 0.01f && Mathf.Abs(toTarget.x) > 0.01f)
                {
                    isFacingLeft = toTarget.x < 0f;
                }

                if (distance <= step)
                {
                    position = points[targetIndex];
                    step -= distance;
                    checkHop(cells[targetIndex]);
                    if (targetIndex >= last)
                    {
                        break;
                    }
                    targetIndex++;
                }
                else
                {
                    position += toTarget / distance * step;
                    step = 0f;
                }
            }
            apply();
        }

        /// <summary>
        /// 立っている（移動中は手前の）ブロックのすぐ後ろに描く
        /// </summary>
        private void applyDepth()
        {
            if (getBlockFunc == null || cells.Count == 0)
            {
                return;
            }

            Block block = getBlockFunc(cells[targetIndex]);
            if (!IsArrived && targetIndex > 0)
            {
                block = IsoDepth.Front(block, getBlockFunc(cells[targetIndex - 1]));
            }
            if (block != null)
            {
                IsoDepth.PlaceAfter(transform, block.transform);
            }
        }

        /// <summary>
        /// チェックポイントに初めて着いたら跳ねる
        /// </summary>
        /// <param name="_cell"></param>
        private void checkHop(Vector2Int _cell)
        {
            if (hopCells.Contains(_cell) && hoppedCells.Add(_cell))
            {
                hopStartTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// 位置・向き・アニメーションを反映
        /// </summary>
        private void apply()
        {
            if (rect == null)
            {
                return;
            }

            float hop = (Time.unscaledTime - hopStartTime) / HopDuration;
            float hopOffset = hop >= 0f && hop < 1f ? Mathf.Sin(hop * Mathf.PI) * HopHeight : 0f;
            rect.anchoredPosition = position + new Vector2(0f, hopOffset);
            rect.localScale = new Vector3(isFacingLeft ? -1f : 1f, 1f, 1f);
            applyDepth();

            Sprite[] frames;
            float fps;
            if (!IsArrived)
            {
                frames = sprites.BoyRun;
                fps = RunFps;
            }
            else if (isHappy)
            {
                frames = sprites.BoyHappy;
                fps = HappyFps;
            }
            else
            {
                frames = sprites.BoySit;
                fps = SitFps;
            }

            frameAnimation.PlayIfChanged(frames, fps);
        }
    }
}
