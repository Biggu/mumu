using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static mumu.EditorUiBuilder;

namespace mumu
{
    /// <summary>
    /// 冒険のステージマップ（StageMap.prefab）を組み立てる
    /// 未作成ならコンパイル後に自動実行。作り直す時は mumu > Build Stage Map Screen
    /// </summary>
    [InitializeOnLoad]
    public static class StageMapScreenBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/View/StageMap.prefab";
        private const float HeaderHeight = 120f;
        private const float Margin = 16f;
        private const float PlayAreaHeight = 170f;
        private const string StampKey = "StageMapScreenBuilder";
        private static readonly Color TitleColor = new Color(1f, 0.92f, 0.63f);

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static StageMapScreenBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoBuild()
        {
            bool isBuilt = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorBuildStamp.NeedsBuild(StampKey, isBuilt, new[] { PrefabPath }))
            {
                return;
            }
            EditorBuildStamp.Save(StampKey);
            Build();
        }

        /// <summary>
        /// ステージマップを作り直す
        /// </summary>
        [MenuItem("mumu/Build Stage Map Screen")]
        public static void Build()
        {
            LoadFont();

            var go = new GameObject("StageMap", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            try
            {
                RectTransform root = Stretch((RectTransform)go.transform);
                var view = go.AddComponent<StageMapView>();

                // 背景（タイル）→ ノードの範囲 → UI の順
                RectTransform bg = Create("bg", root);
                SetRect(bg, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(4000f, 4000f));
                Image bgImage = AddImage(bg, null, Color.white, false);

                RectTransform nodes = Stretch(Create("nodes", root));
                nodes.offsetMin = new Vector2(Margin, PlayAreaHeight);
                nodes.offsetMax = new Vector2(-Margin, -(HeaderHeight + Margin * 2f));

                RectTransform ui = Stretch(Create("ui", root));
                RectTransform header = Create("header", ui);
                header.anchorMin = new Vector2(0f, 1f);
                header.anchorMax = Vector2.one;
                header.pivot = new Vector2(0.5f, 1f);
                header.anchoredPosition = new Vector2(0f, -Margin);
                header.sizeDelta = new Vector2(-Margin * 2f, HeaderHeight);
                AddImage(header, LoadSprite("ui_plate"), Color.white, true, Image.Type.Sliced);

                var middleLeft = new Vector2(0f, 0.5f);
                RectTransform back = Create("backButton", header);
                SetRect(back, middleLeft, middleLeft, new Vector2(Margin, 0f), new Vector2(96f, 96f));
                MumuButton backButton = AddButton(back, LoadSprite("ui_button_round"), SettingsColor, Image.Type.Simple, null);
                AddIcon(back, "ui_icon_home", 56f, Color.white);

                RectTransform title = Stretch(Create("title", header));
                TMP_Text titleText = AddPlainText(title, "ステージ 1〜10", 48, TitleColor);

                var bottom = new Vector2(0.5f, 0f);
                RectTransform play = Create("playButton", ui);
                SetRect(play, bottom, bottom, new Vector2(0f, 40f), new Vector2(400f, 112f));
                RectTransform playLabel = Stretch(Create("label", play));
                playLabel.anchoredPosition = new Vector2(0f, 6f);
                TMP_Text playText = AddText(playLabel, "PLAY", 48, TextBrown);
                MumuButton playButton = AddButton(play, LoadSprite("ui_button"), Color.white, Image.Type.Sliced, playText);

                SetField(view, "bgImage", bgImage);
                SetField(view, "nodeRoot", nodes);
                SetField(view, "backButton", backButton);
                SetField(view, "playButton", playButton);
                SetField(view, "titleText", titleText);

                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                EditorBuildStamp.Save(StampKey);
                Debug.Log("[StageMapScreenBuilder] StageMap.prefab built");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
