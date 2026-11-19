using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 画面の基底
    /// </summary>
    public class ViewBase : MonoBehaviour
    {
        /// <summary>
        /// 初期化
        /// </summary>
        public async UniTask OnInitView()
        {
            await onInitView();
        }

        /// <summary>
        /// 派生クラスの初期化
        /// </summary>
        protected virtual UniTask onInitView()
        {
            return UniTask.CompletedTask;
        }
    }
}
