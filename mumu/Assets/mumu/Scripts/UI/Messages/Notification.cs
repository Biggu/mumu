using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// お知らせ1つ分（Resources/Prefabs/Popup/notification のルートに付ける）。文字を出して、しばらくしてフェードアウトして消す。順番は PopupManager が管理する
    /// </summary>
    public class Notification : MonoBehaviour
    {
        /// <summary>プレハブのパス（Resources/Prefabs/Popup/notification）</summary>
        public const string PrefabPath = "Prefabs/Popup/notification";
        /// <summary>見せている時間（秒）</summary>
        private const float ShowSeconds = 2f;
        /// <summary>消える時間（秒）</summary>
        private const float FadeSeconds = 0.4f;

        /// <summary>お知らせの文字</summary>
        [SerializeField]
        private TMP_Text messageText = null;
        /// <summary>フェードに使うグループ</summary>
        [SerializeField]
        private CanvasGroup group = null;

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
            await notification.GetComponent<Notification>().playAsync(_message, _token);
        }

        /// <summary>
        /// 文字を出して、消えるまで待つ（最後に自分を破棄する）
        /// </summary>
        /// <param name="_message"></param>
        /// <param name="_token"></param>
        private async UniTask playAsync(string _message, CancellationToken _token)
        {
            messageText.text = _message;
            group.alpha = 1f;

            if (await UniTask.Delay(TimeSpan.FromSeconds(ShowSeconds), cancellationToken: _token).SuppressCancellationThrow())
            {
                Destroy(gameObject);
                return;
            }

            float time = 0f;
            while (time < FadeSeconds && this != null)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(time / FadeSeconds);
            }

            if (this != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
