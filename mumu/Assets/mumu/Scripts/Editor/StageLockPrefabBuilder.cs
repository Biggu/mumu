using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 鍵のかかったステージの印（Resources/Prefabs/Common/stage_lock）を作る。無ければコンパイル後に自動で作る
    /// </summary>
    [InitializeOnLoad]
    public static class StageLockPrefabBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/Common/stage_lock.prefab";
        private const string SpritePath = "Assets/mumu/Resources/Sprites/UI/Kit/kit_icon_lock.png";
        private const float IconSize = 40f;

        /// <summary>
        /// コンパイル後に、無ければ作る
        /// </summary>
        static StageLockPrefabBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 無いときだけ作る
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }
            Build();
        }

        /// <summary>
        /// メニューから作る（既にあれば作り直す）
        /// </summary>
        [MenuItem("mumu/Build Stage Lock")]
        public static void Build()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[StageLockPrefabBuilder] 鍵の画像がありません: {SpritePath}");
                return;
            }

            var go = new GameObject("stage_lock", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("[StageLockPrefabBuilder] stage_lock を作りました");
        }
    }
}
