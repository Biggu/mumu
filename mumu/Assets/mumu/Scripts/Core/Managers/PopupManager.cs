using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ポップアップと通知・トーストの表示管理（system シーンに置く）
    /// </summary>
    public class PopupManager : MonoSingleton<PopupManager>
    {
        /// <summary>ポップアップの親（system>main>canvas>base>uiRoot）</summary>
        [SerializeField]
        private RectTransform uiRoot = null;
        /// <summary>お知らせ・トーストの親（system>main>canvas>base>notificationRoot。uiRoot より手前に出す）</summary>
        [SerializeField]
        private RectTransform notificationRoot = null;

        /// <summary>真ん中に出すお知らせ（1つずつ順番に）</summary>
        private readonly NoticeQueue notifications = new NoticeQueue(Notification.ShowAsync);
        /// <summary>画面下に出すトースト（1つずつ順番に）</summary>
        private readonly NoticeQueue toasts = new NoticeQueue(Toast.ShowAsync);

        /// <summary>
        /// ポップアップを開いて閉じるまで待つ（肯定の結果なら true。はい・OK・ずかんで交換した時など）
        /// </summary>
        /// <param name="_popup"></param>
        /// <param name="_param"></param>
        /// <param name="_token"></param>
        public async UniTask<bool> Open(ePopup _popup, BaseParam _param = null, CancellationToken _token = default)
        {
            if (uiRoot != null)
            {
                return await openAsync(_popup, _param, _token);
            }
            Debug.LogError("[PopupManager] uiRoot が設定されていません");
            return false;
        }

        /// <summary>
        /// お知らせを予約する（出しているか、待っている文字と同じなら無視。1つずつ順番に出す）
        /// </summary>
        /// <param name="_message"></param>
        public void Notify(string _message)
        {
            if (notificationRoot != null)
            {
                notifications.Enqueue(notificationRoot, _message, destroyCancellationToken);
                return;
            }
            Debug.LogError("[PopupManager] notificationRoot が設定されていません");
        }

        /// <summary>
        /// テーマのおはなしを真ん中に出して、消えるまで待つ（順番待ちはしない）
        /// </summary>
        /// <param name="_message"></param>
        public UniTask ShowStoryAsync(string _message)
        {
            if (notificationRoot != null)
            {
                return ThemeStory.ShowAsync(notificationRoot, _message, destroyCancellationToken);
            }
            Debug.LogError("[PopupManager] notificationRoot が設定されていません");
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 画面下のトーストを予約する（ゲームのしかけの知らせなど。1つずつ順番に出す）
        /// </summary>
        /// <param name="_message"></param>
        public void ShowToast(string _message)
        {
            if (notificationRoot != null)
            {
                toasts.Enqueue(notificationRoot, _message, destroyCancellationToken);
                return;
            }
            Debug.LogError("[PopupManager] notificationRoot が設定されていません");
        }

        /// <summary>
        /// ポップアップを出して閉じるまで待つ（呼び出し元が止めた時、またはポップアップが壊された時も待つのをやめる）
        /// </summary>
        /// <param name="_popup"></param>
        /// <param name="_param"></param>
        /// <param name="_token"></param>
        private async UniTask<bool> openAsync(ePopup _popup, BaseParam _param, CancellationToken _token)
        {
            PopupBase popup = await PopupLoader.InstantiateAsync<PopupBase>(getPrefabName(_popup), uiRoot, _token);
            if (popup == null)
            {
                return false;
            }

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_token, popup.destroyCancellationToken))
            {
                CommonUtility.PlaySe(eSe.PopupOpen);
                popup.Setup(_param);
                bool result = await popup.WaitClosedAsync(linked.Token);
                CommonUtility.PlaySe(eSe.PopupClose);

                // 途中で止まった時は、閉じた後の処理を動かさない
                Action afterClose = linked.IsCancellationRequested ? null : popup.AfterClose;
                if (popup != null)
                {
                    Destroy(popup.gameObject);
                }
                afterClose?.Invoke();
                return result;
            }
        }

        /// <summary>
        /// ポップアップの種類からプレハブ名を返す
        /// </summary>
        /// <param name="_popup"></param>
        private static string getPrefabName(ePopup _popup)
        {
            switch (_popup)
            {
                case ePopup.Alert:
                    return MessagePopup.PrefabName;
                case ePopup.Settings:
                    return SettingsPopup.PrefabName;
                case ePopup.Confirm:
                    return ConfirmPopup.PrefabName;
                case ePopup.PugBook:
                    return PugBookPopup.PrefabName;
            }
            Debug.LogError($"[PopupManager] 未定義のポップアップ: {_popup}");
            return null;
        }

        /// <summary>
        /// お知らせ・トーストの待ち行列（1つずつ出す。出しているか待っている文字と同じなら無視）
        /// </summary>
        private sealed class NoticeQueue
        {
            private readonly Queue<string> items = new Queue<string>();
            private readonly Func<Transform, string, CancellationToken, UniTask> showAsync;
            /// <summary>いま出しているお知らせ</summary>
            private string showing = null;
            private bool isRunning = false;

            /// <summary>
            /// 出す処理を受け取る
            /// </summary>
            /// <param name="_showAsync"></param>
            public NoticeQueue(Func<Transform, string, CancellationToken, UniTask> _showAsync)
            {
                showAsync = _showAsync;
            }

            /// <summary>
            /// 予約する（出している・待っている文字と同じなら無視）
            /// </summary>
            /// <param name="_parent"></param>
            /// <param name="_message"></param>
            /// <param name="_token"></param>
            public void Enqueue(Transform _parent, string _message, CancellationToken _token)
            {
                if (_message == showing || items.Contains(_message))
                {
                    return;
                }

                items.Enqueue(_message);
                if (!isRunning)
                {
                    runAsync(_parent, _token).Forget();
                }
            }

            /// <summary>
            /// 待っているものを、消えてから次を出す（なくなるまで）
            /// </summary>
            /// <param name="_parent"></param>
            /// <param name="_token"></param>
            private async UniTaskVoid runAsync(Transform _parent, CancellationToken _token)
            {
                isRunning = true;
                while (items.Count > 0 && _parent != null)
                {
                    // 出す前に一番手前へ（ポップアップより上に出す）
                    _parent.SetAsLastSibling();
                    showing = items.Dequeue();
                    await showAsync(_parent, showing, _token);
                }
                items.Clear();
                showing = null;
                isRunning = false;
            }
        }
    }
}
