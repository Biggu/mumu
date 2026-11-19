using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// メッセージダイアログ（OK のみ、または はい／いいえ）
    /// </summary>
    public class MessageDialog : MonoBehaviour
    {
        public const string PrefabName = "message_dialog";

        private const float OpenPunchAmount = 0.08f;
        private const float OpenPunchDuration = 0.2f;
        private const float IconFps = 8f;
        /// <summary>アイコンがない時に本文を上げる量</summary>
        private const float BodyRaiseWithoutIcon = 70f;

        /// <summary>中身のパネル（開く時に跳ねる）</summary>
        [SerializeField]
        private RectTransform panel = null;
        /// <summary>見出し</summary>
        [SerializeField]
        private TMP_Text titleText = null;
        /// <summary>本文</summary>
        [SerializeField]
        private TMP_Text bodyText = null;
        /// <summary>はい（OK）ボタン</summary>
        [SerializeField]
        private MumuButton yesButton = null;
        /// <summary>いいえボタン</summary>
        [SerializeField]
        private MumuButton noButton = null;
        /// <summary>背景の暗幕（はい／いいえの時は押すと いいえ）</summary>
        [SerializeField]
        private MumuButton dimButton = null;
        /// <summary>アイコン（パグなど、なければ非表示）</summary>
        [SerializeField]
        private Image iconImage = null;

        private UniTaskCompletionSource<bool> resultSource = null;
        private bool hasNo = false;

        /// <summary>
        /// 表示して押されたボタンを待つ（はい・OK = true）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_titleKey"></param>
        /// <param name="_bodyKey"></param>
        /// <param name="_hasNo"></param>
        /// <param name="_token"></param>
        public static UniTask<bool> ShowAsync(Transform _parent, string _titleKey, string _bodyKey, bool _hasNo, CancellationToken _token)
        {
            return ShowTextAsync(_parent, TextLocalizer.Get(_titleKey), TextLocalizer.Get(_bodyKey), _hasNo, null, _token);
        }

        /// <summary>
        /// 文字をそのまま表示して押されたボタンを待つ（_iconFrames はアイコンのコマ送り、null で非表示）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_title"></param>
        /// <param name="_body"></param>
        /// <param name="_hasNo"></param>
        /// <param name="_iconFrames"></param>
        /// <param name="_token"></param>
        /// <param name="_auraTier"></param>
        public static async UniTask<bool> ShowTextAsync(Transform _parent, string _title, string _body, bool _hasNo, Sprite[] _iconFrames, CancellationToken _token, int _auraTier = 0)
        {
            MessageDialog dialog = await PopupLoader.InstantiateAsync<MessageDialog>(PrefabName, _parent, _token);
            if (dialog == null)
            {
                return false;
            }

            dialog.setup(_title, _body, _hasNo);
            dialog.setIcon(_iconFrames);
            if (dialog.iconImage != null && dialog.iconImage.gameObject.activeSelf)
            {
                PugAura.Attach(dialog.iconImage.rectTransform, _auraTier, dialog.iconImage.rectTransform.sizeDelta.x);
            }
            (bool isCanceled, bool result) = await dialog.resultSource.Task.AttachExternalCancellation(_token).SuppressCancellationThrow();
            if (dialog != null)
            {
                Destroy(dialog.gameObject);
            }
            return !isCanceled && result;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_title"></param>
        /// <param name="_body"></param>
        /// <param name="_hasNo"></param>
        private void setup(string _title, string _body, bool _hasNo)
        {
            hasNo = _hasNo;
            resultSource = new UniTaskCompletionSource<bool>();

            titleText.text = _title;
            bodyText.text = _body;
            yesButton.SetText(TextLocalizer.Get(_hasNo ? "YES" : "OK"));
            yesButton.SetOnClick(onClickYesButton);
            noButton.gameObject.SetActive(_hasNo);
            if (_hasNo)
            {
                noButton.SetText(TextLocalizer.Get("NO"));
                noButton.SetOnClick(onClickNoButton);
            }
            else
            {
                // OK だけの時は中央に寄せる
                var rect = (RectTransform)yesButton.transform;
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            }
            if (dimButton != null)
            {
                dimButton.SetClickSeEnabled(false);
                dimButton.SetOnClick(onClickDimButton);
            }

            UiTween.PunchScale(panel, Vector3.one, OpenPunchAmount, OpenPunchDuration, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// アイコンを設定（なければ隠して本文を上げる）
        /// </summary>
        /// <param name="_frames"></param>
        private void setIcon(Sprite[] _frames)
        {
            bool hasIcon = _frames != null && _frames.Length > 0;
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    iconImage.sprite = _frames[0];
                    var frameAnimation = iconImage.GetComponent<SpriteFrameAnimation>();
                    if (frameAnimation == null)
                    {
                        frameAnimation = iconImage.gameObject.AddComponent<SpriteFrameAnimation>();
                    }
                    frameAnimation.Play(_frames, IconFps);
                }
            }

            if (!hasIcon && iconImage != null)
            {
                var body = (RectTransform)bodyText.transform;
                body.anchoredPosition += new Vector2(0f, BodyRaiseWithoutIcon);
            }
        }

        /// <summary>
        /// はいボタン押下時の処理
        /// </summary>
        private void onClickYesButton()
        {
            resultSource.TrySetResult(true);
        }

        /// <summary>
        /// いいえボタン押下時の処理
        /// </summary>
        private void onClickNoButton()
        {
            resultSource.TrySetResult(false);
        }

        /// <summary>
        /// 暗幕押下時の処理（はい／いいえの時だけ いいえ 扱い）
        /// </summary>
        private void onClickDimButton()
        {
            if (hasNo)
            {
                resultSource.TrySetResult(false);
            }
        }
    }
}
