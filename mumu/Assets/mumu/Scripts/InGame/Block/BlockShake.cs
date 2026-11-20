using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ブロックの横揺れ（進めなかった時・橋が壊れた時）。重なっても元の位置からずれない
    /// </summary>
    public class BlockShake
    {
        private const float ShakeAmplitude = 4f;
        private const float ShakeDuration = 0.3f;

        private readonly CancellationToken destroyToken;
        private CancellationTokenSource cts = null;
        /// <summary>揺れていない時の横位置</summary>
        private float restX = 0f;
        /// <summary>揺れが終わる時刻（unscaled）</summary>
        private float endTime = 0f;

        /// <summary>
        /// 停止のためのトークンを受け取る
        /// </summary>
        /// <param name="_destroyToken"></param>
        public BlockShake(CancellationToken _destroyToken)
        {
            destroyToken = _destroyToken;
        }

        /// <summary>
        /// 揺らす（揺れ中なら前の揺れを止めて、元の位置から揺らし直す）
        /// </summary>
        /// <param name="_body"></param>
        public void Play(RectTransform _body)
        {
            if (_body == null)
            {
                return;
            }

            // 揺れ中は元の位置を覚えたままにして、重なっても少しずつずれないようにする
            if (Time.unscaledTime >= endTime)
            {
                restX = _body.anchoredPosition.x;
            }
            TokenUtility.Cancel(ref cts);
            _body.anchoredPosition = new Vector2(restX, _body.anchoredPosition.y);

            endTime = Time.unscaledTime + ShakeDuration;
            UiTween.ShakeX(_body, restX, ShakeAmplitude, ShakeDuration, TokenUtility.Restart(ref cts, destroyToken)).Forget();
        }

        /// <summary>
        /// 揺れを止める
        /// </summary>
        public void Cancel()
        {
            TokenUtility.Cancel(ref cts);
        }
    }
}
