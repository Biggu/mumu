using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ハートがふわっと飛んで消える演出
    /// </summary>
    public static class HeartEffect
    {
        private const string SpritePath = "Sprites/UI/ui_heart";

        private static Sprite sprite = null;

        /// <summary>
        /// 1つ飛ばす（_from は _parent の pivot 基準の座標、_move だけ動いて消える）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_from"></param>
        /// <param name="_move"></param>
        /// <param name="_size"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public static async UniTaskVoid Play(RectTransform _parent, Vector2 _from, Vector2 _move, float _size, float _duration, CancellationToken _token)
        {
            if (sprite == null)
            {
                sprite = ResourceManager.Instance.Load<Sprite>(SpritePath);
            }

            var go = new GameObject("heart", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            Image heart = go.GetComponent<Image>();
            heart.sprite = sprite;
            heart.raycastTarget = false;
            heart.preserveAspect = true;

            RectTransform rect = heart.rectTransform;
            rect.anchorMin = _parent.pivot;
            rect.anchorMax = _parent.pivot;
            rect.sizeDelta = new Vector2(_size, _size);

            float time = 0f;
            while (time < _duration)
            {
                float t = time / _duration;
                float ease = 1f - (1f - t) * (1f - t);
                rect.anchoredPosition = _from + _move * ease;
                rect.localScale = Vector3.one * (0.6f + 0.6f * Mathf.Sin(Mathf.Min(1f, t * 2f) * Mathf.PI * 0.5f));
                heart.color = new Color(1f, 1f, 1f, 1f - t * t);
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
            }

            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        /// <summary>
        /// 中心から周りへたくさん飛ばす
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_center"></param>
        /// <param name="_count"></param>
        /// <param name="_radius"></param>
        /// <param name="_token"></param>
        public static void Burst(RectTransform _parent, Vector2 _center, int _count, float _radius, CancellationToken _token)
        {
            for (int i = 0; i < _count; i++)
            {
                float angle = (i + Random.value * 0.5f) / _count * Mathf.PI * 2f;
                Vector2 move = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius * Random.Range(0.7f, 1.1f);
                Play(_parent, _center, move, Random.Range(36f, 56f), Random.Range(0.8f, 1.2f), _token).Forget();
            }
        }
    }
}
