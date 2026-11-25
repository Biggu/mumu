using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ボーナスのお知らせ（Resources/Prefabs/Popup/bone_bonus：ほねの絵と文字を上に出す）。
    /// 少し見せてからフェードして消す
    /// </summary>
    public static class BoneBonusOverlay
    {
        /// <summary>プレハブの名前（Resources/Prefabs/Popup）</summary>
        public const string PrefabName = "bone_bonus";
        private const string PrefabPath = "Prefabs/Popup/" + PrefabName;
        /// <summary>見せている時間（秒）</summary>
        private const float ShowSeconds = 1.6f;
        /// <summary>消える時間（秒）</summary>
        private const float FadeSeconds = 0.4f;
        /// <summary>出てくる時に跳ねる量と時間</summary>
        private const float PunchAmount = 0.15f;
        private const float PunchDuration = 0.25f;
        /// <summary>文字の子の名前</summary>
        private const string TextName = "text";
        /// <summary>詳しい説明の子の名前（ボーナスの下に出す）</summary>
        private const string DetailName = "detail";

        /// <summary>
        /// 文字を上に出して、消えるまで待つ
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_message"></param>
        /// <param name="_token"></param>
        public static async UniTask ShowAsync(Transform _parent, string _message, string _detail, CancellationToken _token)
        {
            RectTransform prefab = await ResourceManager.Instance.LoadAsync<RectTransform>(PrefabPath, _token);
            if (prefab == null || _parent == null || _token.IsCancellationRequested)
            {
                return;
            }

            RectTransform overlay = UnityEngine.Object.Instantiate(prefab, _parent);
            overlay.SetAsLastSibling();
            CommonUtility.PlaySe(eSe.BonusCoin);
            setMessage(overlay, _message, _detail);

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = overlay.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = 1f;
            UiTween.PunchScale(overlay, Vector3.one, PunchAmount, PunchDuration, _token).Forget();

            if (await UniTask.Delay(TimeSpan.FromSeconds(ShowSeconds), cancellationToken: _token).SuppressCancellationThrow())
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                return;
            }

            float time = 0f;
            while (time < FadeSeconds && overlay != null)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    break;
                }
                time += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(time / FadeSeconds);
            }

            if (overlay != null)
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
            }
        }

        /// <summary>
        /// text と detail の子に文字を入れる
        /// </summary>
        /// <param name="_overlay"></param>
        /// <param name="_message"></param>
        /// <param name="_detail"></param>
        private static void setMessage(RectTransform _overlay, string _message, string _detail)
        {
            setText(_overlay, TextName, _message);
            setText(_overlay, DetailName, _detail);
        }

        /// <summary>
        /// 名前の子の文字を入れる
        /// </summary>
        /// <param name="_overlay"></param>
        /// <param name="_name"></param>
        /// <param name="_message"></param>
        private static void setText(RectTransform _overlay, string _name, string _message)
        {
            Transform textTransform = _overlay.Find(_name);
            TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            if (text != null)
            {
                text.text = _message;
            }
        }
    }
}
