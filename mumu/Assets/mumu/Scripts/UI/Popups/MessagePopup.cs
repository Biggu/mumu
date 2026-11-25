using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// メッセージダイアログ（OK のみ、または はい／いいえ）
    /// </summary>
    public class MessagePopup : PopupBase
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
        /// <summary>閉じるボタン（右上。いいえ扱いで閉じる）</summary>
        [SerializeField]
        private MumuButton closeButton = null;
        /// <summary>アイコン（パグなど、なければ非表示）</summary>
        [SerializeField]
        private Image iconImage = null;

        private bool hasNo = false;

        /// <summary>
        /// 初期化（AlertParam を受け取り、表示する）
        /// </summary>
        /// <param name="_param"></param>
        public override void Setup(BaseParam _param)
        {
            var param = (AlertParam)_param;
            hasNo = param.HasNo;

            titleText.text = param.Title;
            bodyText.text = param.Body;
            yesButton.SetText((hasNo ? MumuText.YES() : MumuText.OK()));
            yesButton.SetOnClick(onClickYesButton);
            noButton.gameObject.SetActive(hasNo);
            if (hasNo)
            {
                noButton.SetText(MumuText.NO());
                noButton.SetOnClick(onClickNoButton);
            }
            else
            {
                // OK だけの時は中央に寄せる
                var rect = (RectTransform)yesButton.transform;
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            }
            if (closeButton != null)
            {
                closeButton.SetOnClick(onClickCloseButton);
            }
            if (dimButton != null)
            {
                dimButton.SetClickSeEnabled(false);
                dimButton.SetOnClick(onClickDimButton);
            }

            setIcon(param.IconFrames);
            if (iconImage != null && iconImage.gameObject.activeSelf)
            {
                PugAura.Attach(iconImage.rectTransform, param.AuraTier, iconImage.rectTransform.sizeDelta.x);
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
                    CommonUtility.GetOrAddComponent<SpriteFrameAnimation>(iconImage.gameObject).Play(_frames, IconFps);
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
            close(true);
        }

        /// <summary>
        /// いいえボタン押下時の処理
        /// </summary>
        private void onClickNoButton()
        {
            close(false);
        }

        /// <summary>
        /// 閉じるボタン押下時の処理（いいえ扱い）
        /// </summary>
        private void onClickCloseButton()
        {
            close(false);
        }

        /// <summary>
        /// 暗幕押下時の処理（はい／いいえの時だけ いいえ 扱い）
        /// </summary>
        private void onClickDimButton()
        {
            if (hasNo)
            {
                close(false);
            }
        }
    }
}
