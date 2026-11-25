using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 文字をふわっと上へ動かしながら消す（Prefabs/Common/float_text を使う）
    /// </summary>
    public static class FloatText
    {
        private const string PrefabPath = "Prefabs/Common/float_text";
        /// <summary>上へ動く距離（ピクセル）</summary>
        private const float RiseDistance = 70f;
        /// <summary>消えるまでの時間（秒）</summary>
        private const float Duration = 0.9f;

        /// <summary>
        /// 文字を出して、少し上へ動かしてから消す
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_anchoredPosition"></param>
        /// <param name="_text"></param>
        /// <param name="_color"></param>
        /// <param name="_token"></param>
        public static void Spawn(Transform _parent, Vector2 _anchoredPosition, string _text, Color _color, CancellationToken _token)
        {
            TMP_Text text = create(_parent, _text, _color);
            if (text == null)
            {
                return;
            }
            ((RectTransform)text.transform).anchoredPosition = _anchoredPosition;
            animateAsync(text, _token).Forget();
        }

        /// <summary>
        /// 動かない文字を作る（ボタンの下の小さな表示など）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_text"></param>
        /// <param name="_color"></param>
        public static TMP_Text CreateLabel(Transform _parent, string _text, Color _color)
        {
            return create(_parent, _text, _color);
        }

        /// <summary>
        /// Prefab から文字を作る
        /// </summary>
        private static TMP_Text create(Transform _parent, string _text, Color _color)
        {
            GameObject prefab = ResourceManager.Instance.Load<GameObject>(PrefabPath);
            if (prefab == null || _parent == null)
            {
                return null;
            }
            GameObject go = UnityEngine.Object.Instantiate(prefab, _parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = _text;
                text.color = _color;
            }
            return text;
        }

        /// <summary>
        /// 上へ動かしながら消す
        /// </summary>
        private static async UniTaskVoid animateAsync(TMP_Text _text, CancellationToken _token)
        {
            var rect = (RectTransform)_text.transform;
            Vector2 start = rect.anchoredPosition;
            Color color = _text.color;
            for (float time = 0f; time < Duration; time += Time.unscaledDeltaTime)
            {
                if (_text == null)
                {
                    return;
                }
                float t = time / Duration;
                rect.anchoredPosition = start + new Vector2(0f, RiseDistance * t);
                color.a = 1f - t;
                _text.color = color;
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
            }
            if (_text != null)
            {
                UnityEngine.Object.Destroy(_text.gameObject);
            }
        }
    }
}
