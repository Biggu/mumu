using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 画面下のお知らせ（Resources/Prefabs/Popup/toast：ふわっと出して、少しして消す）。順番は PopupManager が管理する
    /// </summary>
    public static class Toast
    {
        /// <summary>プレハブの名前（Resources/Prefabs/Popup）</summary>
        public const string PrefabName = "toast";
        private const string PrefabPath = "Prefabs/Popup/" + PrefabName;
        /// <summary>出てくる・消える時間（秒）</summary>
        private const float FadeSeconds = 0.2f;
        /// <summary>見せている時間（秒）</summary>
        private const float ShowSeconds = 2.4f;
        /// <summary>文字の子の名前</summary>
        private const string TextName = "text";

        /// <summary>
        /// 文字を出して、消えるまで待つ
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

            RectTransform toast = UnityEngine.Object.Instantiate(prefab, _parent);
            toast.SetAsLastSibling();
            CommonUtility.PlaySe(eSe.ToastPop);
            setMessage(toast, _message);

            CanvasGroup group = toast.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = toast.gameObject.AddComponent<CanvasGroup>();
            }

            float total = FadeSeconds * 2f + ShowSeconds;
            for (float time = 0f; time < total; time += Time.unscaledDeltaTime)
            {
                if (toast == null)
                {
                    return;
                }
                group.alpha = time < FadeSeconds ? time / FadeSeconds
                    : (time > total - FadeSeconds ? (total - time) / FadeSeconds : 1f);
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
            }

            if (toast != null)
            {
                UnityEngine.Object.Destroy(toast.gameObject);
            }
        }

        /// <summary>
        /// text の子に文字を入れる
        /// </summary>
        /// <param name="_toast"></param>
        /// <param name="_message"></param>
        private static void setMessage(RectTransform _toast, string _message)
        {
            Transform textTransform = _toast.Find(TextName);
            TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            if (text != null)
            {
                text.text = _message;
            }
        }
    }
}
