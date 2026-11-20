using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// UI 用の簡易トゥイーン
    /// </summary>
    public static class UiTween
    {
        /// <summary>
        /// 一瞬拡大して戻す
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_baseScale"></param>
        /// <param name="_amount"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static async UniTask PunchScale(Transform _target, Vector3 _baseScale, float _amount, float _duration, CancellationToken _token)
        {
            float time = 0f;
            while (time < _duration && _target != null)
            {
                _target.localScale = _baseScale * (1f + _amount * Mathf.Sin(time / _duration * Mathf.PI));
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
            }

            if (_target != null)
            {
                _target.localScale = _baseScale;
            }
        }

        /// <summary>
        /// その場で跳ねる
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_baseY"></param>
        /// <param name="_height"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static async UniTask HopY(RectTransform _target, float _baseY, float _height, float _duration, CancellationToken _token)
        {
            float time = 0f;
            while (time < _duration && _target != null)
            {
                setY(_target, _baseY + Mathf.Sin(time / _duration * Mathf.PI) * _height);
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
            }

            if (_target != null)
            {
                setY(_target, _baseY);
            }
        }

        /// <summary>
        /// 横に揺らす（Y は Animator が書き込むため触らない）
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_baseX"></param>
        /// <param name="_amplitude"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static async UniTask ShakeX(RectTransform _target, float _baseX, float _amplitude, float _duration, CancellationToken _token)
        {
            const float Cycles = 3f;
            float baseX = _baseX;
            float time = 0f;

            while (time < _duration && _target != null)
            {
                float progress = time / _duration;
                setX(_target, baseX + Mathf.Sin(progress * Mathf.PI * 2f * Cycles) * _amplitude * (1f - progress));
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
            }

            if (_target != null)
            {
                setX(_target, baseX);
            }
        }

        /// <summary>
        /// _move だけ動きながら消えて、最後に GameObject ごと破棄する
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_graphic"></param>
        /// <param name="_move"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static async UniTask MoveFade(RectTransform _target, UnityEngine.UI.Graphic _graphic, Vector2 _move, float _duration, CancellationToken _token)
        {
            Vector2 start = _target.anchoredPosition;
            Color color = _graphic.color;
            float time = 0f;

            while (time < _duration && _target != null)
            {
                float progress = time / _duration;
                _target.anchoredPosition = start + _move * progress;
                _graphic.color = new Color(color.r, color.g, color.b, color.a * (1f - progress * progress));
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
            }

            if (_target != null)
            {
                Object.Destroy(_target.gameObject);
            }
        }

        /// <summary>
        /// anchoredPosition.x を設定
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_x"></param>
        private static void setX(RectTransform _target, float _x)
        {
            Vector2 position = _target.anchoredPosition;
            position.x = _x;
            _target.anchoredPosition = position;
        }

        /// <summary>
        /// anchoredPosition.y を設定
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_y"></param>
        private static void setY(RectTransform _target, float _y)
        {
            Vector2 position = _target.anchoredPosition;
            position.y = _y;
            _target.anchoredPosition = position;
        }
    }
}
