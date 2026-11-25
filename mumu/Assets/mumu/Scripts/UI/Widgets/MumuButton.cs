using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// クリック・押下・長押し対応ボタン
    /// </summary>
    public class MumuButton : Button
    {
        private const float MinLongPressTime = 0.1f;
        private const float MinRepeatInterval = 0.01f;

        /// <summary>ラベル</summary>
        [Header("Text Component")]
        [SerializeField] private TextMeshProUGUI textComponent = null;

        /// <summary>長押しと判定するまでの時間</summary>
        [Header("Long Press Settings")]
        [SerializeField] private float longPressTime = 1.0f;
        /// <summary>長押し中のリピート間隔</summary>
        [SerializeField] private float repeatInterval = 0.1f;

        /// <summary>クリック時に鳴らす効果音</summary>
        [Header("Sound")]
        [SerializeField] private eSe clickSe = eSe.ButtonClick;
        /// <summary>クリック時に効果音を鳴らすか</summary>
        [SerializeField] private bool isClickSeEnabled = true;

        /// <summary>クリック時の処理</summary>
        private Action onClickAction = null;
        /// <summary>押下時の処理</summary>
        private Action onPressAction = null;
        /// <summary>長押し時の処理</summary>
        private Action onLongPressAction = null;

        private bool longPressTriggered = false;
        private CancellationTokenSource longPressCts = null;

        /// <summary>操作できるか</summary>
        private bool canInteract => IsActive() && IsInteractable();

        /// <summary>使えないボタンの色（明るいグレー）</summary>
        private static readonly Color DisabledGray = new Color(0.78f, 0.78f, 0.78f, 1f);

        /// <summary>
        /// 状態に合わせて見た目を変える（使えない時は明るいグレー）
        /// </summary>
        /// <param name="_state"></param>
        /// <param name="_instant"></param>
        protected override void DoStateTransition(SelectionState _state, bool _instant)
        {
            base.DoStateTransition(_state, _instant);
            if (!IsInteractable() && targetGraphic != null)
            {
                targetGraphic.color = DisabledGray;
            }
        }

        /// <summary>
        /// 押下
        /// </summary>
        /// <param name="_eventData"></param>
        public override void OnPointerDown(PointerEventData _eventData)
        {
            // 使えないボタンは押した時に押す演出なしでお知らせだけ出す
            if (_eventData.button == PointerEventData.InputButton.Left && IsActive() && !IsInteractable())
            {
                PopupManager.Instance.Notify(MumuText.NOTICE_UNAVAILABLE());
                return;
            }

            base.OnPointerDown(_eventData);

            if (_eventData.button != PointerEventData.InputButton.Left || !canInteract)
            {
                return;
            }

            longPressTriggered = false;
            onPressAction?.Invoke();
            startLongPress();
        }

        /// <summary>
        /// 離した
        /// </summary>
        /// <param name="_eventData"></param>
        public override void OnPointerUp(PointerEventData _eventData)
        {
            base.OnPointerUp(_eventData);
            cancelLongPress();
        }

        /// <summary>
        /// ボタン外に出た
        /// </summary>
        /// <param name="_eventData"></param>
        public override void OnPointerExit(PointerEventData _eventData)
        {
            base.OnPointerExit(_eventData);
            cancelLongPress();
        }

        /// <summary>
        /// クリック（ボタン上で離した時のみ呼ばれる）
        /// </summary>
        /// <param name="_eventData"></param>
        public override void OnPointerClick(PointerEventData _eventData)
        {
            if (longPressTriggered)
            {
                return;
            }

            bool canClick = _eventData.button == PointerEventData.InputButton.Left && canInteract;
            base.OnPointerClick(_eventData);
            if (canClick)
            {
                playClickSe();
                onClickAction?.Invoke();
            }
        }

        /// <summary>
        /// 決定キー
        /// </summary>
        /// <param name="_eventData"></param>
        public override void OnSubmit(BaseEventData _eventData)
        {
            bool canClick = canInteract;
            base.OnSubmit(_eventData);
            if (canClick)
            {
                playClickSe();
                onClickAction?.Invoke();
            }
        }

        /// <summary>
        /// クリック時の効果音を鳴らすか（暗幕など音を出したくないボタン用）
        /// </summary>
        /// <param name="_isEnabled"></param>
        public void SetClickSeEnabled(bool _isEnabled)
        {
            isClickSeEnabled = _isEnabled;
        }

        /// <summary>
        /// クリック時の効果音（効果音 OFF なら SoundManager 側で鳴らない）
        /// </summary>
        private void playClickSe()
        {
            if (!isClickSeEnabled)
            {
                return;
            }

            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(clickSe);
            }
        }

        /// <summary>
        /// 無効化時に長押しを止める
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            cancelLongPress();
        }

        /// <summary>
        /// 破棄時に長押しを止める
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            cancelLongPress();
        }

#if UNITY_EDITOR
        /// <summary>
        /// インスペクタの値を制限
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            longPressTime = Mathf.Max(MinLongPressTime, longPressTime);
            repeatInterval = Mathf.Max(MinRepeatInterval, repeatInterval);
        }
#endif

        /// <summary>
        /// クリック時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnClick(Action _action)
        {
            onClickAction = _action;
        }

        /// <summary>
        /// 押下時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnPress(Action _action)
        {
            onPressAction = _action;
        }

        /// <summary>
        /// 長押し時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnLongPress(Action _action)
        {
            onLongPressAction = _action;
        }

        /// <summary>
        /// 処理をすべて解除
        /// </summary>
        public void ClearAllActions()
        {
            onClickAction = null;
            onPressAction = null;
            onLongPressAction = null;
        }

        /// <summary>
        /// ラベルを設定
        /// </summary>
        /// <param name="_text"></param>
        public void SetText(string _text)
        {
            if (textComponent != null)
            {
                textComponent.text = _text;
            }
        }

        /// <summary>
        /// 長押し判定時間を設定
        /// </summary>
        /// <param name="_time"></param>
        public void SetLongPressTime(float _time)
        {
            longPressTime = Mathf.Max(MinLongPressTime, _time);
        }

        /// <summary>
        /// リピート間隔を設定
        /// </summary>
        /// <param name="_interval"></param>
        public void SetRepeatInterval(float _interval)
        {
            repeatInterval = Mathf.Max(MinRepeatInterval, _interval);
        }

        /// <summary>
        /// 長押し判定を開始
        /// </summary>
        private void startLongPress()
        {
            cancelLongPress();

            // 長押し未登録時は通常クリックとして扱う
            if (onLongPressAction == null)
            {
                return;
            }

            longPressCts = new CancellationTokenSource();
            longPressAsync(longPressCts.Token).Forget();
        }

        /// <summary>
        /// 長押し判定を止める
        /// </summary>
        private void cancelLongPress()
        {
            TokenUtility.Cancel(ref longPressCts);
        }

        /// <summary>
        /// 長押しとリピート
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid longPressAsync(CancellationToken _token)
        {
            float elapsed = 0f;
            float repeatElapsed = 0f;

            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                float deltaTime = Time.unscaledDeltaTime;

                if (!longPressTriggered)
                {
                    elapsed += deltaTime;
                    if (elapsed >= longPressTime)
                    {
                        longPressTriggered = true;
                        repeatElapsed = 0f;
                        onLongPressAction?.Invoke();
                    }
                    continue;
                }

                repeatElapsed += deltaTime;
                if (repeatElapsed >= repeatInterval)
                {
                    repeatElapsed = 0f;
                    onLongPressAction?.Invoke();
                }
            }
        }
    }
}
