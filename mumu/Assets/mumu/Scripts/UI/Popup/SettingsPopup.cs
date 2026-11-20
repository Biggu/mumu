using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 設定ポップアップ（効果音・BGM の ON/OFF、ゲーム中はやりなおし・タイトルへ）
    /// </summary>
    public class SettingsPopup : PopupBase
    {
        public const string PrefabName = "settings_popup";

        private const float OpenPunchAmount = 0.08f;
        private const float OpenPunchDuration = 0.2f;

        /// <summary>中身のパネル（開く時に跳ねる）</summary>
        [SerializeField]
        private RectTransform panel = null;
        /// <summary>効果音の切り替えボタン</summary>
        [SerializeField]
        private MumuButton seToggle = null;
        /// <summary>BGM の切り替えボタン</summary>
        [SerializeField]
        private MumuButton bgmToggle = null;
        /// <summary>閉じるボタン</summary>
        [SerializeField]
        private MumuButton closeButton = null;
        /// <summary>背景の暗幕（押すと閉じる）</summary>
        [SerializeField]
        private MumuButton dimButton = null;
        /// <summary>ON の画像</summary>
        [SerializeField]
        private Sprite toggleOn = null;
        /// <summary>OFF の画像</summary>
        [SerializeField]
        private Sprite toggleOff = null;
        /// <summary>やりなおしボタン（ゲーム中のみ表示）</summary>
        [SerializeField]
        private MumuButton retryButton = null;
        /// <summary>タイトルへボタン（ゲーム中のみ表示）</summary>
        [SerializeField]
        private MumuButton titleButton = null;

        /// <summary>やりなおし押下時の処理</summary>
        private Action onRetryAction = null;
        /// <summary>タイトルへ押下時の処理</summary>
        private Action onTitleAction = null;

        /// <summary>
        /// 初期化（SettingsParam を受け取り、表示する）
        /// </summary>
        /// <param name="_param"></param>
        public override void Setup(BaseParam _param)
        {
            var param = (SettingsParam)_param;
            onRetryAction = param.OnRetry;
            onTitleAction = param.OnTitle;

            seToggle.SetOnClick(onClickSeToggle);
            bgmToggle.SetOnClick(onClickBgmToggle);
            closeButton.SetOnClick(onClickCloseButton);
            if (dimButton != null)
            {
                dimButton.SetClickSeEnabled(false);
                dimButton.SetOnClick(onClickCloseButton);
            }
            setupMenuButton(retryButton, param.OnRetry != null, onClickRetryButton);
            setupMenuButton(titleButton, param.OnTitle != null, onClickTitleButton);

            refresh();
            if (panel != null)
            {
                UiTween.PunchScale(panel, Vector3.one, OpenPunchAmount, OpenPunchDuration, destroyCancellationToken).Forget();
            }
        }

        /// <summary>
        /// ゲーム中だけのボタンを設定
        /// </summary>
        /// <param name="_button"></param>
        /// <param name="_isVisible"></param>
        /// <param name="_onClick"></param>
        private static void setupMenuButton(MumuButton _button, bool _isVisible, Action _onClick)
        {
            if (_button == null)
            {
                return;
            }
            _button.gameObject.SetActive(_isVisible);
            if (_isVisible)
            {
                _button.SetOnClick(_onClick);
            }
        }

        /// <summary>
        /// 効果音ボタン押下時の処理
        /// </summary>
        private void onClickSeToggle()
        {
            GameSettings.IsSeEnabled = !GameSettings.IsSeEnabled;
            refresh();

            // ON にした時は押した音が鳴らなかったので、ここで鳴らして確認できるようにする
            SoundManager sound = SoundManager.Instance;
            if (GameSettings.IsSeEnabled && sound != null)
            {
                sound.PlaySe(eSe.ButtonClick);
            }
        }

        /// <summary>
        /// BGM ボタン押下時の処理
        /// </summary>
        private void onClickBgmToggle()
        {
            GameSettings.IsBgmEnabled = !GameSettings.IsBgmEnabled;
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.ApplySettings();
            }
            refresh();
        }

        /// <summary>
        /// やりなおしボタン押下時の処理
        /// </summary>
        private void onClickRetryButton()
        {
            close(false, onRetryAction);
        }

        /// <summary>
        /// タイトルへボタン押下時の処理
        /// </summary>
        private void onClickTitleButton()
        {
            close(false, onTitleAction);
        }

        /// <summary>
        /// 閉じるボタン押下時の処理
        /// </summary>
        private void onClickCloseButton()
        {
            close(false);
        }

        /// <summary>
        /// ボタンの表示を反映
        /// </summary>
        private void refresh()
        {
            setToggle(seToggle, GameSettings.IsSeEnabled);
            setToggle(bgmToggle, GameSettings.IsBgmEnabled);
        }

        /// <summary>
        /// 切り替えボタンの画像
        /// </summary>
        /// <param name="_button"></param>
        /// <param name="_isOn"></param>
        private void setToggle(MumuButton _button, bool _isOn)
        {
            if (_button != null && _button.image != null)
            {
                _button.image.sprite = _isOn ? toggleOn : toggleOff;
            }
        }
    }
}
