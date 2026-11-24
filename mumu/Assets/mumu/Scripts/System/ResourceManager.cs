using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// Resources フォルダからの読み込み
    /// </summary>
    public class ResourceManager : MonoSingleton<ResourceManager>
    {
        /// <summary>
        /// 画面プレハブのパス
        /// </summary>
        /// <param name="_viewType"></param>
        public static string GetViewPath(eViewType _viewType) => $"Prefabs/View/{_viewType}";

        /// <summary>
        /// インゲームプレハブのパス
        /// </summary>
        /// <param name="_prefabName"></param>
        public static string GetInGamePrefabPath(string _prefabName) => $"Prefabs/InGame/{_prefabName}";

        /// <summary>
        /// テーマ画像のパス
        /// </summary>
        /// <param name="_themeId"></param>
        /// <param name="_index"></param>
        public static string GetThemeSpritePath(int _themeId, int _index) => GetThemeSpritePath(_themeId, _index.ToString());

        /// <summary>
        /// テーマ画像のパス
        /// </summary>
        /// <param name="_themeId"></param>
        /// <param name="_suffix"></param>
        public static string GetThemeSpritePath(int _themeId, string _suffix) => $"Sprites/Themes/theme_{_themeId:000000}/{_themeId:000000}_{_suffix}";

        /// <summary>
        /// 同期読み込み（実行中・エディタ両方から使える）
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_logIfMissing"></param>
        public static T LoadResource<T>(string _path, bool _logIfMissing = true) where T : Object
        {
            T resource = Resources.Load<T>(_path);
            if (resource == null && _logIfMissing)
            {
                Debug.LogError($"[ResourceManager] Resource not found at path: {_path}");
            }
            return resource;
        }

        /// <summary>
        /// フォルダ内をすべて同期読み込み（実行中・エディタ両方から使える）
        /// </summary>
        /// <param name="_path"></param>
        public static T[] LoadResourceAll<T>(string _path) where T : Object
        {
            return Resources.LoadAll<T>(_path);
        }

        /// <summary>
        /// 同期読み込み
        /// </summary>
        /// <param name="_path"></param>
        public T Load<T>(string _path) where T : Object
        {
            return LoadResource<T>(_path);
        }

        /// <summary>
        /// フォルダ内をすべて同期読み込み
        /// </summary>
        /// <param name="_path"></param>
        public T[] LoadAll<T>(string _path) where T : Object
        {
            return LoadResourceAll<T>(_path);
        }

        /// <summary>
        /// 非同期読み込み
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_token"></param>
        public async UniTask<T> LoadAsync<T>(string _path, CancellationToken _token = default) where T : Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(_path);
            await request.ToUniTask(cancellationToken: _token);

            T resource = request.asset as T;
            if (resource == null)
            {
                Debug.LogError($"[ResourceManager] Resource not found at path: {_path}");
            }
            return resource;
        }
    }
}
