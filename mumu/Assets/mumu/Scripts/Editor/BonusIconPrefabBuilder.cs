using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ボーナスステージの印（Resources/Prefabs/Common/bonus_icon）を作る。無ければコンパイル後に自動で作る
    /// </summary>
    [InitializeOnLoad]
    public static class BonusIconPrefabBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/Common/bonus_icon.prefab";
        private const float IconSize = 56f;

        /// <summary>
        /// コンパイル後に、無ければ作る
        /// </summary>
        static BonusIconPrefabBuilder()
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
        /// メニューから作る（既にあれば作り直す。絵は実行時に入れる）
        /// </summary>
        [MenuItem("mumu/Build Bonus Icon")]
        public static void Build()
        {
            var go = new GameObject("bonus_icon", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            Image image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("[BonusIconPrefabBuilder] bonus_icon を作りました");
        }
    }
}
