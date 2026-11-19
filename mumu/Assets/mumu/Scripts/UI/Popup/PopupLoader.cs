using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ポップアップのプレハブを読み込んで画面の一番手前に出す（Resources/Prefabs/Popup）
    /// </summary>
    public static class PopupLoader
    {
        public const string Folder = "Prefabs/Popup";

        /// <summary>
        /// 読み込んで親いっぱいに生成（失敗時は null）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_token"></param>
        public static async UniTask<T> InstantiateAsync<T>(string _name, Transform _parent, CancellationToken _token) where T : Component
        {
            T prefab = await ResourceManager.Instance.LoadAsync<T>($"{Folder}/{_name}", _token);
            if (prefab == null || _parent == null || _token.IsCancellationRequested)
            {
                return null;
            }

            T popup = Object.Instantiate(prefab, _parent);
            var rect = (RectTransform)popup.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            return popup;
        }
    }
}
