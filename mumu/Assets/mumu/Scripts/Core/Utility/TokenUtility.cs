using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 共通処理（TokenUtility）
    /// </summary>
    public static class TokenUtility
    {
        /// <summary>
        /// CancellationTokenSource を止めて破棄（null なら何もしない）
        /// </summary>
        /// <param name="_cts"></param>
        public static void Cancel(ref CancellationTokenSource _cts)
        {
            if (_cts == null)
            {
                return;
            }
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }


        /// <summary>
        /// 前の演出を止めて、新しい演出のトークンを返す
        /// </summary>
        /// <param name="_cts"></param>
        /// <param name="_destroyToken"></param>
        public static CancellationToken Restart(ref CancellationTokenSource _cts, CancellationToken _destroyToken)
        {
            Cancel(ref _cts);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_destroyToken);
            return _cts.Token;
        }


        /// <summary>
        /// まだ無ければ作って、そのトークンを返す（動いている演出は止めない）
        /// </summary>
        /// <param name="_cts"></param>
        /// <param name="_destroyToken"></param>
        public static CancellationToken Ensure(ref CancellationTokenSource _cts, CancellationToken _destroyToken)
        {
            if (_cts == null)
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(_destroyToken);
            }
            return _cts.Token;
        }

    }
}
