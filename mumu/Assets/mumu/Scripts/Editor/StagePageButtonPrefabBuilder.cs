using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ステージマップのページを送る矢印（Resources/Prefabs/Common/stage_page_button）を作る。無ければコンパイル後に自動で作る
    /// </summary>
    [InitializeOnLoad]
    public static class StagePageButtonPrefabBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/Common/stage_page_button.prefab";
        private const float ButtonSize = 72f;

        /// <summary>
        /// コンパイル後に、無ければ作る
        /// </summary>
        static StagePageButtonPrefabBuilder()
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
        [MenuItem("mumu/Build Stage Page Button")]
        public static void Build()
        {
            var go = new GameObject("stage_page_button", typeof(RectTransform), typeof(Image), typeof(MumuButton));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = true;
            var button = go.GetComponent<MumuButton>();
            button.targetGraphic = image;
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("[StagePageButtonPrefabBuilder] stage_page_button を作りました");
        }
    }
}
