using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// マップの外の枠のブロック（Resources/Prefabs/InGame/ring_block）を作る。未作成のときだけ作る
    /// </summary>
    [InitializeOnLoad]
    public static class RingBlockPrefabBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/InGame/ring_block.prefab";

        /// <summary>
        /// コンパイル後に、未作成なら作る
        /// </summary>
        static RingBlockPrefabBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成のときだけ作る
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Build();
            }
        }

        /// <summary>
        /// メニューから作る（作成済みなら何もしない）
        /// </summary>
        [MenuItem("mumu/Build Ring Block")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>
        /// 枠のブロックのプレハブを作る（既にあれば残す）
        /// </summary>
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }

            var root = new GameObject("ring_block", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(60f, 60f);
            root.AddComponent<RingBlock>();

            var baseGo = new GameObject("base", typeof(RectTransform), typeof(Image));
            baseGo.transform.SetParent(root.transform, false);
            var baseRect = (RectTransform)baseGo.transform;
            baseRect.anchorMin = Vector2.zero;
            baseRect.anchorMax = Vector2.one;
            baseRect.offsetMin = Vector2.zero;
            baseRect.offsetMax = Vector2.zero;
            baseGo.GetComponent<Image>().raycastTarget = false;

            var decoGo = new GameObject("deco", typeof(RectTransform), typeof(Image));
            decoGo.transform.SetParent(root.transform, false);
            var decoRect = (RectTransform)decoGo.transform;
            decoRect.anchorMin = new Vector2(0f, 0.5f);
            decoRect.anchorMax = new Vector2(1f, 0.5f);
            decoRect.pivot = new Vector2(0.5f, 0f);
            decoRect.anchoredPosition = Vector2.zero;
            decoGo.GetComponent<Image>().raycastTarget = false;
            decoGo.AddComponent<SpriteFrameAnimation>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }
    }
}
