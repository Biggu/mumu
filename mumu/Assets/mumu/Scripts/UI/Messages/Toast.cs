using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 画面下のお知らせ1つ分（Resources/Prefabs/Popup/toast のルートに付ける）。ふわっと出して、少しして消す。順番は PopupManager が管理する
    /// </summary>
    public class Toast : MonoBehaviour
    {
        /// <summary>プレハブの名前（Resources/Prefabs/Popup）</summary>
        public const string PrefabName = "toast";
        /// <summary>プレハブのパス</summary>
        private const string PrefabPath = "Prefabs/Popup/" + PrefabName;
        /// <summary>出てくる・消える時間（秒）</summary>
        private const float FadeSeconds = 0.2f;
        /// <summary>見せている時間（秒）</summary>
        private const float ShowSeconds = 2.4f;

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

            RectTransform toast = UnityEngine.Object.Instantiate(prefab, _parent);
            toast.SetAsLastSibling();
            CommonUtility.PlaySe(eSe.ToastPop);
            await toast.GetComponent<Toast>().playAsync(_message, _token);
        }

        /// <summary>
        /// 文字を出して、フェードして消えるまで待つ（最後に自分を破棄する）
        /// </summary>
        /// <param name="_message"></param>
        /// <param name="_token"></param>
        private async UniTask playAsync(string _message, CancellationToken _token)
        {
            messageText.text = _message;
            float total = FadeSeconds * 2f + ShowSeconds;
            for (float time = 0f; time < total; time += Time.unscaledDeltaTime)
            {
                if (this == null)
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

            if (this != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
