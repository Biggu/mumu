using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static mumu.EditorUiBuilder;

namespace mumu
{
    /// <summary>
    /// おにわ（Garden.prefab）を組み立てる
    /// 未作成ならコンパイル後に自動実行。作り直す時は mumu > Build Garden Screen
    /// </summary>
    [InitializeOnLoad]
    public static class GardenScreenBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/View/Garden.prefab";
        private const string StampKey = "GardenScreenBuilder";
        private const float HeaderHeight = 120f;
        private const float Margin = 16f;
        /// <summary>作成済みの判定に使う（今の構成で作ったか）</summary>
        private const string BookButtonPath = "ui/bookButton";
        private static readonly Color TitleColor = new Color(1f, 0.92f, 0.63f);

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static GardenScreenBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoBuild()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            bool isBuilt = prefab != null && prefab.transform.Find(BookButtonPath) != null;
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorBuildStamp.NeedsBuild(StampKey, isBuilt, new[] { PrefabPath }))
            {
                return;
            }
            EditorBuildStamp.Save(StampKey);
            Build();
        }

        /// <summary>
        /// おにわを作り直す
        /// </summary>
        [MenuItem("mumu/Build Garden Screen")]
        public static void Build()
        {
            LoadFont();

            var go = new GameObject("Garden", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            try
            {
                RectTransform root = Stretch((RectTransform)go.transform);
                var view = go.AddComponent<GardenView>();
                buildStage(root, view);
                buildUi(root, view);
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                EditorBuildStamp.Save(StampKey);
                Debug.Log("[GardenScreenBuilder] Garden.prefab built");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// 背景と箱庭
        /// </summary>
        /// <param name="_root"></param>
        /// <param name="_view"></param>
        private static void buildStage(RectTransform _root, GardenView _view)
        {
            RectTransform stage = Stretch(Create("stage", _root));

            RectTransform bg = Create("bg", stage);
            SetRect(bg, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(4000f, 4000f));
            AddImage(bg, null, Color.white, false);

            RectTransform mapGroup = Create("mapGroup", stage);
            SetRect(mapGroup, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(100f, 100f));
            RectTransform mapRoot = Create("maproot", mapGroup);
            SetRect(mapRoot, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

            SetField(_view, "mapRoot", mapRoot.gameObject);
            SetField(_view, "mapGroup", mapGroup);
            // カメラと箱庭の操作（作る時に付ける。実行時には付けない）
            mapGroup.gameObject.AddComponent<MapViewController>();
            stage.gameObject.AddComponent<GardenInputController>();
            SetField(_view, "bgImage", bg.GetComponent<Image>());
        }

        /// <summary>
        /// ヘッダー（戻る・見出し・仲間の数）、案内、下の一覧
        /// </summary>
        /// <param name="_root"></param>
        /// <param name="_view"></param>
        private static void buildUi(RectTransform _root, GardenView _view)
        {
            RectTransform ui = Stretch(Create("ui", _root));

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
            AddText(title, "GARDEN", 52, TitleColor);

            var middleRight = new Vector2(1f, 0.5f);
            RectTransform count = Create("count", header);
            SetRect(count, middleRight, middleRight, new Vector2(-Margin * 2f, 0f), new Vector2(220f, 60f));
            TMP_Text countText = AddPlainText(count, "0", 30, TitleColor);
            countText.alignment = TextAlignmentOptions.Right;

            RectTransform empty = Create("empty", ui);
            SetRect(empty, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 60f), new Vector2(640f, 200f));
            AddImage(empty, LoadSprite("ui_panel"), Color.white, false, Image.Type.Sliced);
            RectTransform emptyLabel = Stretch(Create("label", empty));
            emptyLabel.offsetMin = new Vector2(24f, 16f);
            emptyLabel.offsetMax = new Vector2(-24f, -16f);
            TMP_Text emptyText = AddText(emptyLabel, "GARDEN_EMPTY", 30, TextBrown);

            // ずかんボタン（下）
            var bottom = new Vector2(0.5f, 0f);
            RectTransform book = Create("bookButton", ui);
            SetRect(book, bottom, bottom, new Vector2(0f, 40f), new Vector2(360f, 112f));
            RectTransform bookLabel = Stretch(Create("label", book));
            bookLabel.anchoredPosition = new Vector2(0f, 6f);
            TMP_Text bookText = AddText(bookLabel, "PUG_BOOK", 44, TextBrown);
            MumuButton bookButton = AddButton(book, LoadSprite("ui_button"), Color.white, Image.Type.Sliced, bookText);

            SetField(_view, "backButton", backButton);
            SetField(_view, "countText", countText);
            SetField(_view, "emptyText", emptyText);
            SetField(_view, "bookButton", bookButton);
        }
    }
}
