using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// お知らせ（Resources/Prefabs/Popup/notification：notification > plate > text）。
    /// 画面の真ん中に出して、しばらくしてフェードアウトして消す。順番は PopupManager が管理する
    /// </summary>
    public static class Notification
    {
        /// <summary>プレハブのパス（Resources/Prefabs/Popup/notification）</summary>
        public const string PrefabPath = "Prefabs/Popup/notification";
        /// <summary>見せている時間（秒）</summary>
        public const float ShowSeconds = 2f;
        /// <summary>消える時間（秒）</summary>
        public const float FadeSeconds = 0.4f;
        /// <summary>子の名前（plate と、その中の text）</summary>
        private const string PlateName = "plate";
        private const string TextName = "text";

        /// <summary>
        /// お知らせを1つ出して、消えるまで待つ
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

            RectTransform notification = UnityEngine.Object.Instantiate(prefab, _parent);
            notification.SetAsLastSibling();
            CommonUtility.PlaySe(eSe.NoticePing);
            setMessage(notification, _message);

            CanvasGroup group = notification.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = notification.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = 1f;

            if (await UniTask.Delay(TimeSpan.FromSeconds(ShowSeconds), cancellationToken: _token).SuppressCancellationThrow())
            {
                UnityEngine.Object.Destroy(notification.gameObject);
                return;
            }

            float time = 0f;
            while (time < FadeSeconds && notification != null)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(time / FadeSeconds);
            }

            if (notification != null)
            {
                UnityEngine.Object.Destroy(notification.gameObject);
            }
        }

        /// <summary>
        /// notification > plate > text の text に文字を入れる
        /// </summary>
        /// <param name="_notification"></param>
        /// <param name="_message"></param>
        private static void setMessage(RectTransform _notification, string _message)
        {
            Transform plate = _notification.Find(PlateName);
            Transform textTransform = plate != null ? plate.Find(TextName) : null;
            TextMeshProUGUI text = textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
            if (text != null)
            {
                text.text = _message;
            }
        }
    }
}
