using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ポップアップの基底（PopupManager が開いて、閉じるまで待つ）
    /// </summary>
    public abstract class PopupBase : MonoBehaviour
    {
        private readonly UniTaskCompletionSource<bool> resultSource = new UniTaskCompletionSource<bool>();

        /// <summary>閉じた後に実行する処理（PopupManager が破棄してから呼ぶ）</summary>
        public Action AfterClose { get; private set; } = null;

        /// <summary>
        /// 内容を渡して初期化する
        /// </summary>
        /// <param name="_param"></param>
        public abstract void Setup(BaseParam _param);

        /// <summary>
        /// 閉じるまで待つ（閉じた時の結果。途中で止めたら false）
        /// </summary>
        /// <param name="_token"></param>
        public async UniTask<bool> WaitClosedAsync(CancellationToken _token)
        {
            (bool isCanceled, bool result) = await resultSource.Task.AttachExternalCancellation(_token).SuppressCancellationThrow();
            return !isCanceled && result;
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        /// <param name="_result"></param>
        /// <param name="_afterClose"></param>
        protected void close(bool _result, Action _afterClose = null)
        {
            AfterClose = _afterClose;
            resultSource.TrySetResult(_result);
        }
    }
}
