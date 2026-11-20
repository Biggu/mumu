using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 部品のプレハブ（Resources/Prefabs/Common）を読み込んで作る。画面に固定で置く部品はここから作る（system シーンに置く）
    /// </summary>
    public class PartsLoaderManager : MonoSingleton<PartsLoaderManager>
    {
        /// <summary>プレハブのフォルダ（Resources から）</summary>
        public const string Folder = "Prefabs/Common";

        /// <summary>
        /// 部品を親の下に作る（見つからない時は null）
        /// </summary>
        /// <param name="_part"></param>
        /// <param name="_parent"></param>
        public GameObject Create(ePart _part, Transform _parent)
        {
            string path = Folder + "/" + GetName(_part);
            GameObject prefab = ResourceManager.Instance.Load<GameObject>(path);
            if (prefab == null || _parent == null)
            {
                Debug.LogError($"[PartsLoaderManager] 部品を作れません: {path}");
                return null;
            }
            return UnityEngine.Object.Instantiate(prefab, _parent, false);
        }

        /// <summary>
        /// 部品の種類からプレハブの名前を返す
        /// </summary>
        /// <param name="_part"></param>
        public static string GetName(ePart _part)
        {
            switch (_part)
            {
                case ePart.BigStar:
                    return "big_star";
                case ePart.StagePug:
                    return "stage_pug";
                case ePart.PlayerPug:
                    return "player_pug";
            }
            return _part.ToString();
        }
    }
}
