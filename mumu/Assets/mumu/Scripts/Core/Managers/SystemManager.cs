using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 起動処理
    /// </summary>
    public class SystemManager : MonoBehaviour
    {
        /// <summary>
        /// 起動
        /// </summary>
        private void Awake()
        {
            init().Forget();
        }

        /// <summary>
        /// タイトル画面へ
        /// </summary>
        private async UniTaskVoid init()
        {
            await TextLocalizer.PreloadAsync();
            await ViewManager.Instance.ToTitle();
        }
    }
}
