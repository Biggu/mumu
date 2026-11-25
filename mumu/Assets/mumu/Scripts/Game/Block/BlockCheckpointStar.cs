using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// チェックポイントの★（ブロックの上でふわふわ動き、通過時は回りながら跳ねる）
    /// </summary>
    public class BlockCheckpointStar
    {
        private const string CheckpointStarPath = "Sprites/UI/base_ui/ui_star_on";
        private const float CheckpointStarSize = 28f;
        /// <summary>ブロックの中心から★の中心までの高さ（持ち上げは body の移動に含まれる）</summary>
        private const float CheckpointStarY = 44f;
        private const float CheckpointStarBob = 3f;
        private const float CheckpointStarBobSpeed = 3f;
        /// <summary>未通過の★の濃さと大きさ</summary>
        private const float CheckpointStarIdleAlpha = 0.6f;
        private const float CheckpointStarIdleScale = 0.8f;
        private const float CheckpointJumpHeight = 36f;
        private const float CheckpointJumpScale = 0.8f;
        private const float CheckpointJumpDuration = 0.6f;

        private static Sprite starSprite = null;

        private readonly CancellationToken destroyToken;
        private Image image = null;
        /// <summary>ふわふわの位相（ブロックの順番でずらす）</summary>
        private float phase = 0f;
        private bool isPassed = false;
        private float passedTime = float.NegativeInfinity;
        private CancellationTokenSource cts = null;

        /// <summary>
        /// 停止のためのトークンを受け取る
        /// </summary>
        /// <param name="_destroyToken"></param>
        public BlockCheckpointStar(CancellationToken _destroyToken)
        {
            destroyToken = _destroyToken;
        }

        /// <summary>★を作ったか</summary>
        public bool IsCreated => image != null;

        /// <summary>
        /// ★を作ってふわふわさせる
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_sortIndex"></param>
        public void Create(Transform _parent, int _sortIndex)
        {
            phase = _sortIndex;
            image = UiFactory.CreateImage("checkpointStar", _parent, SpriteUtility.LoadCached(ref starSprite, CheckpointStarPath), CheckpointStarSize);
            starAsync(TokenUtility.Restart(ref cts, destroyToken)).Forget();
        }

        /// <summary>
        /// 表示・非表示
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetVisible(bool _isVisible)
        {
            if (image != null)
            {
                image.gameObject.SetActive(_isVisible);
            }
        }

        /// <summary>
        /// 経路に乗ったか（跳ねる演出に使う）
        /// </summary>
        /// <param name="_isPassed"></param>
        /// <param name="_passedTime"></param>
        public void SetPassed(bool _isPassed, float _passedTime)
        {
            isPassed = _isPassed;
            passedTime = _passedTime;
        }

        /// <summary>
        /// ふわふわを止める
        /// </summary>
        public void Cancel()
        {
            TokenUtility.Cancel(ref cts);
        }

        /// <summary>
        /// ★をふわふわ動かし、通過時は回りながら跳ねる
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid starAsync(CancellationToken _token)
        {
            RectTransform rect = image.rectTransform;
            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                bool passed = isPassed;
                float y = CheckpointStarY + Mathf.Sin(Time.unscaledTime * CheckpointStarBobSpeed + phase) * CheckpointStarBob;
                float scale = passed ? 1f : CheckpointStarIdleScale;
                float angle = 0f;

                float t = (Time.unscaledTime - passedTime) / CheckpointJumpDuration;
                if (t >= 0f && t < 1f)
                {
                    float arc = Mathf.Sin(t * Mathf.PI);
                    y += arc * CheckpointJumpHeight;
                    scale += arc * CheckpointJumpScale;
                    angle = -t * 360f;
                }

                rect.anchoredPosition = new Vector2(0f, y);
                rect.localScale = new Vector3(scale, scale, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                Color color = image.color;
                color.a = passed ? 1f : CheckpointStarIdleAlpha;
                image.color = color;
            }
        }
    }
}
