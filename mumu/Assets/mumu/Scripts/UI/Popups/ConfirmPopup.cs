using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 2つのボタンの確認（おにわ・ずかんで共通。題名と左右のボタン）
    /// </summary>
    public class ConfirmPopup : PopupBase
    {
        public const string PrefabName = "confirm_dialog";

        private const float OpenPunchAmount = 0.08f;
        private const float OpenPunchDuration = 0.2f;

        /// <summary>中身のパネル（開く時に跳ねる）</summary>
        [SerializeField]
        private RectTransform panel = null;
        /// <summary>見出し</summary>
        [SerializeField]
        private TMP_Text titleText = null;
        /// <summary>左のボタン</summary>
        [SerializeField]
        private MumuButton firstButton = null;
        /// <summary>右のボタン</summary>
        [SerializeField]
        private MumuButton secondButton = null;
        /// <summary>背景の暗幕（押すと閉じる）</summary>
        [SerializeField]
        private MumuButton dimButton = null;
        /// <summary>閉じるボタン（右上。何もせず閉じる）</summary>
        [SerializeField]
        private MumuButton closeButton = null;

        /// <summary>左のボタンを押した後の処理</summary>
        private Action onFirstAction = null;
        /// <summary>右のボタンを押した後の処理</summary>
        private Action onSecondAction = null;

        /// <summary>
        /// 初期化（ConfirmParam を受け取り、表示する）
        /// </summary>
        /// <param name="_param"></param>
        public override void Setup(BaseParam _param)
        {
            var param = (ConfirmParam)_param;
            onFirstAction = param.OnFirst;
            onSecondAction = param.OnSecond;

            titleText.text = param.Title;
            firstButton.SetText(param.FirstLabel);
            firstButton.interactable = param.FirstEnabled;
            firstButton.SetOnClick(onClickFirstButton);
            secondButton.SetText(param.SecondLabel);
            secondButton.interactable = param.SecondEnabled;
            secondButton.SetOnClick(onClickSecondButton);
            if (closeButton != null)
            {
                closeButton.SetOnClick(onClickCloseButton);
            }
            if (dimButton != null)
            {
                dimButton.SetClickSeEnabled(false);
                dimButton.SetOnClick(onClickDimButton);
            }

            UiTween.PunchScale(panel, Vector3.one, OpenPunchAmount, OpenPunchDuration, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// 左のボタン押下時の処理
        /// </summary>
        private void onClickFirstButton()
        {
            close(true, onFirstAction);
        }

        /// <summary>
        /// 右のボタン押下時の処理
        /// </summary>
        private void onClickSecondButton()
        {
            close(false, onSecondAction);
        }

        /// <summary>
        /// 閉じるボタン押下時の処理（何もせず閉じる）
        /// </summary>
        private void onClickCloseButton()
        {
            close(false);
        }

        /// <summary>
        /// 暗幕押下時の処理（何もせず閉じる）
        /// </summary>
        private void onClickDimButton()
        {
            close(false);
        }
    }
}
