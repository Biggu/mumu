using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// block.prefab の中の Goal 用のパグを、別のプレハブ（Resources/Prefabs/InGame/goal_pug）に分ける。Goal のときだけ読み込む
    /// </summary>
    [InitializeOnLoad]
    public static class GoalPugPrefabBuilder
    {
        private const string BlockPath = "Assets/mumu/Resources/Prefabs/InGame/block.prefab";
        private const string GoalPugPath = "Assets/mumu/Resources/Prefabs/InGame/goal_pug.prefab";
        private const string PugChildName = "pug";
        private const string CageChildName = "cage";
        private const string CageSpritePath = "Assets/mumu/Resources/Sprites/Common/cage.png";

        /// <summary>
        /// コンパイル後に、まだ分けていなければ分ける
        /// </summary>
        static GoalPugPrefabBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 分けていないときだけ実行
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GoalPugPath) == null || hasPugChild())
            {
                Build();
            }
            ensureCage();
        }

        /// <summary>
        /// goal_pug に檻（cage）の子が無ければ追加する（パグの上に重なる。解放で上に上がる）
        /// </summary>
        private static void ensureCage()
        {
            GameObject goalPug = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPugPath);
            if (goalPug == null || goalPug.transform.Find(CageChildName) != null)
            {
                return;
            }

            Sprite cageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CageSpritePath);
            if (cageSprite == null)
            {
                Debug.LogWarning($"[GoalPugPrefabBuilder] 檻の画像がありません: {CageSpritePath}");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(GoalPugPath);
            var cageGo = new GameObject(CageChildName, typeof(RectTransform), typeof(Image));
            cageGo.transform.SetParent(contents.transform, false);
            var rect = (RectTransform)cageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = cageGo.GetComponent<Image>();
            image.sprite = cageSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(contents, GoalPugPath);
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            Debug.Log("[GoalPugPrefabBuilder] goal_pug に檻を追加しました");
        }

        /// <summary>
        /// メニューから実行（既に分けてあれば何もしない）
        /// </summary>
        [MenuItem("mumu/Build Goal Pug")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>
        /// 階層の中から pug を探す（body の下にある）
        /// </summary>
        /// <param name="_root"></param>
        private static Transform findPug(GameObject _root)
        {
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            {
                if (t != _root.transform && t.name == PugChildName)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// block.prefab に pug の子があるか
        /// </summary>
        private static bool hasPugChild()
        {
            GameObject block = AssetDatabase.LoadAssetAtPath<GameObject>(BlockPath);
            return block != null && findPug(block) != null;
        }

        /// <summary>
        /// pug の子から goal_pug を作り、block.prefab からは外す
        /// </summary>
        public static void Build()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(BlockPath);
            if (contents == null)
            {
                Debug.LogError($"[GoalPugPrefabBuilder] block が読めません: {BlockPath}");
                return;
            }

            Transform pugChild = findPug(contents);
            if (pugChild != null)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(GoalPugPath) == null)
                {
                    GameObject copy = Object.Instantiate(pugChild.gameObject);
                    copy.name = "goal_pug";
                    copy.transform.SetParent(null, false);
                    if (copy.GetComponent<SpriteFrameAnimation>() == null)
                    {
                        copy.AddComponent<SpriteFrameAnimation>();
                    }
                    PrefabUtility.SaveAsPrefabAsset(copy, GoalPugPath);
                    Object.DestroyImmediate(copy);
                }

                Object.DestroyImmediate(pugChild.gameObject);
                PrefabUtility.SaveAsPrefabAsset(contents, BlockPath);
            }
            PrefabUtility.UnloadPrefabContents(contents);
            AssetDatabase.SaveAssets();
            Debug.Log("[GoalPugPrefabBuilder] goal_pug を分けました");
        }
    }
}

