using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// テーマのおはなし（Resources/Prefabs/Popup/theme_story：真ん中に一行出して、しばらくしてフェードして消す）
    /// </summary>
    public static class ThemeStory
    {
        /// <summary>プレハブの名前（Resources/Prefabs/Popup）</summary>
        public const string PrefabName = "theme_story";
        private const string PrefabPath = "Prefabs/Popup/" + PrefabName;
        /// <summary>見せている時間（秒）</summary>
        private const float ShowSeconds = 3.5f;
        /// <summary>消える時間（秒）</summary>
        private const float FadeSeconds = 0.4f;
        /// <summary>文字の子の名前</summary>
        private const string TextName = "text";

        /// <summary>
        /// 一行出して、消えるまで待つ
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_message"></param>
        /// <param name="_token"></param>
        public static async UniTask ShowAsync(Transform _parent, string _message, CancellationToken _token)
        {
            RectTransform prefab = await ResourceManager.Instance.LoadAsync<RectTransform>(PrefabPath, _token);
            if (prefab == null || _parent == null || _token.IsCancellationRequested)
            {
                return;
            }

            RectTransform story = UnityEngine.Object.Instantiate(prefab, _parent);
            story.SetAsLastSibling();
            CommonUtility.PlaySe(eSe.Chime);
            setMessage(story, _message);

            CanvasGroup group = story.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = story.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = 1f;

            if (await UniTask.Delay(TimeSpan.FromSeconds(ShowSeconds), cancellationToken: _token).SuppressCancellationThrow())
            {
                UnityEngine.Object.Destroy(story.gameObject);
                return;
            }

            float time = 0f;
            while (time < FadeSeconds && story != null)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(time / FadeSeconds);
            }

            if (story != null)
            {
                UnityEngine.Object.Destroy(story.gameObject);
            }
        }

        /// <summary>
        /// text の子に文字を入れる
        /// </summary>
        /// <param name="_story"></param>
        /// <param name="_message"></param>
        private static void setMessage(RectTransform _story, string _message)
        {
            Transform textTransform = _story.Find(TextName);
            TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            if (text != null)
            {
                text.text = _message;
            }
        }
    }
}
