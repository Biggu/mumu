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
        /// <summary>座っている時のコマ送りの速さ</summary>
        private const float SitFps = 3f;
        /// <summary>走っている時のコマ送りの速さ</summary>
        private const float RunFps = 12f;
        /// <summary>喜んでいる時のコマ送りの速さ</summary>
        private const float HappyFps = 8f;
        /// <summary>巻き戻しでこのマス数より離れたら走らずに移動</summary>
        private const int JumpThreshold = 1;
        /// <summary>跳ねの高さ</summary>
        private const float HopHeight = 14f;
        /// <summary>跳ねる時間（秒）</summary>
        private const float HopDuration = 0.35f;

        /// <summary>パグの画像</summary>
        [SerializeField]
        private Image image = null;
        /// <summary>コマ送り</summary>
        [SerializeField]
        private SpriteFrameAnimation frameAnimation = null;

        /// <summary>パグのスプライト</summary>
        private PugSprites sprites = null;
        /// <summary>自分の RectTransform</summary>
        private RectTransform rect = null;

        /// <summary>経路のマス</summary>
        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        /// <summary>経路の画面上の点（cells と同じ順）</summary>
        private readonly List<Vector2> points = new List<Vector2>();
        /// <summary>向かっている（または立っている）点の番号</summary>
        private int targetIndex = 0;
        /// <summary>今いる位置</summary>
        private Vector2 position = Vector2.zero;
        /// <summary>1マスの距離</summary>
        private float cellDistance = 1f;
        /// <summary>基本の速さ</summary>
        private float baseSpeed = 0f;
        /// <summary>遅れた分の速さ</summary>
        private float catchUpSpeed = 0f;
        /// <summary>最高速度</summary>
        private float maxSpeed = 0f;

        /// <summary>跳ねるマス（チェックポイント）</summary>
        private readonly HashSet<Vector2Int> hopCells = new HashSet<Vector2Int>();
        /// <summary>跳ね済みのマス（経路から外れたら戻す）</summary>
        private readonly HashSet<Vector2Int> hoppedCells = new HashSet<Vector2Int>();
        /// <summary>跳ね始めた時刻</summary>
        private float hopStartTime = float.NegativeInfinity;

        /// <summary>マスのブロック（描画順をブロックの間に合わせるため）</summary>
        private Func<Vector2Int, Block> getBlockFunc = null;
        /// <summary>経路のその順番へワープで着いたか（走らずに跳んで移る）</summary>
        private Func<int, bool> isWarpArrivalFunc = null;

        /// <summary>喜んでいるか</summary>
        private bool isHappy = false;
        /// <summary>左を向いているか</summary>
        private bool isFacingLeft = false;

        /// <summary>先端に着いているか</summary>
        public bool IsArrived => points.Count > 0 && targetIndex == points.Count - 1 && position == points[targetIndex];

        /// <summary>
        /// 生成（PlayerPug プレハブを読み込んで付ける）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_sprites"></param>
        /// <param name="_size"></param>
        /// <param name="_cellDistance"></param>
        public static PlayerPug Create(Transform _parent, PugSprites _sprites, float _size, float _cellDistance)
        {
            var pug = PartsLoaderManager.Instance.Create(ePart.PlayerPug, _parent).GetComponent<PlayerPug>();
            pug.setup(_sprites, _size, _cellDistance);
            return pug;
        }

        /// <summary>
        /// 経路を設定する
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_points"></param>
        public void SetPath(IReadOnlyList<Vector2Int> _cells, IReadOnlyList<Vector2> _points)
        {
            bool isFirst = points.Count == 0;
            int common = commonPrefix(_cells);

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
                // 立っていたマスが経路から外れた：近ければ走って戻り、遠ければその場へ移動
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
        /// マスのブロックを返す処理を設定する
        /// </summary>
        /// <param name="_getBlock"></param>
        public void SetBlockProvider(Func<Vector2Int, Block> _getBlock)
        {
            getBlockFunc = _getBlock;
        }

        /// <summary>
        /// ワープで着いたかを返す処理を設定する
        /// </summary>
        /// <param name="_isWarpArrival"></param>
        public void SetWarpProvider(Func<int, bool> _isWarpArrival)
        {
            isWarpArrivalFunc = _isWarpArrival;
        }

        /// <summary>
        /// 着いた時に跳ねるマスを設定する
        /// </summary>
        /// <param name="_cells"></param>
        public void SetHopCells(IEnumerable<Vector2Int> _cells)
        {
            hopCells.Clear();
            hopCells.UnionWith(_cells);
        }

        /// <summary>
        /// 喜ぶかどうかを設定する
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
            UiFactory.SetPugImage(image, false);

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
        /// 経路に沿って移動する（遅れているほど速く走る）
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
                if (isWarpExit())
                {
                    position = points[targetIndex];
                    startHop();
                    if (targetIndex >= last)
                    {
                        break;
                    }
                    targetIndex++;
                    continue;
                }

                Vector2 toTarget = points[targetIndex] - position;
                float distance = toTarget.magnitude;
                updateFacing(toTarget, distance);

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
        /// 経路の先頭から共通している数を数える
        /// </summary>
        /// <param name="_newCells"></param>
        private int commonPrefix(IReadOnlyList<Vector2Int> _newCells)
        {
            int common = 0;
            while (common < cells.Count && common < _newCells.Count && cells[common] == _newCells[common])
            {
                common++;
            }
            return common;
        }

        /// <summary>
        /// 今の目標がワープの出口で、入口に立っているか
        /// </summary>
        private bool isWarpExit()
        {
            return targetIndex > 0 && isWarpArrivalFunc != null && isWarpArrivalFunc(targetIndex) && position == points[targetIndex - 1];
        }

        /// <summary>
        /// 進む向きに合わせて左右を向く
        /// </summary>
        /// <param name="_toTarget"></param>
        /// <param name="_distance"></param>
        private void updateFacing(Vector2 _toTarget, float _distance)
        {
            if (_distance > 0.01f && Mathf.Abs(_toTarget.x) > 0.01f)
            {
                isFacingLeft = _toTarget.x < 0f;
            }
        }

        /// <summary>
        /// 跳ね始める
        /// </summary>
        private void startHop()
        {
            hopStartTime = Time.unscaledTime;
        }

        /// <summary>
        /// 立っている（移動中は手前の）ブロックのすぐ後ろに描く
        /// </summary>
        /// <param name="_arrived"></param>
        private void applyDepth(bool _arrived)
        {
            if (getBlockFunc == null || cells.Count == 0)
            {
                return;
            }

            Block block = getBlockFunc(cells[targetIndex]);
            if (!_arrived && targetIndex > 0)
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
                startHop();
            }
        }

        /// <summary>
        /// 跳ねの高さ（跳ねていない時は 0）
        /// </summary>
        private float hopOffset()
        {
            float hop = (Time.unscaledTime - hopStartTime) / HopDuration;
            return hop >= 0f && hop < 1f ? Mathf.Sin(hop * Mathf.PI) * HopHeight : 0f;
        }

        /// <summary>
        /// 位置・向き・描画順・アニメーションを反映する
        /// </summary>
        private void apply()
        {
            if (rect == null)
            {
                return;
            }

            bool arrived = IsArrived;
            rect.anchoredPosition = position + new Vector2(0f, hopOffset());
            rect.localScale = new Vector3(isFacingLeft ? -1f : 1f, 1f, 1f);
            applyDepth(arrived);
            playAnimation(arrived);
        }

        /// <summary>
        /// 走る・座る・喜ぶのどれかを再生する
        /// </summary>
        /// <param name="_arrived"></param>
        private void playAnimation(bool _arrived)
        {
            Sprite[] frames;
            float fps;
            if (!_arrived)
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
