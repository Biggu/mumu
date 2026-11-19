using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static mumu.EditorUiBuilder;

namespace mumu
{
    /// <summary>
    /// ポップアップのプレハブ（Resources/Prefabs/Popup）を組み立てる
    /// 未作成ならコンパイル後に自動実行。作り直す時は mumu > Build Popups
    /// </summary>
    [InitializeOnLoad]
    public static class PopupPrefabBuilder
    {
        private const string Folder = "Assets/mumu/Resources/" + PopupLoader.Folder;
        private const float RowHeight = 110f;
        private const float MenuButtonHeight = 108f;

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static PopupPrefabBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            // アイコン追加前のダイアログも作り直す
            var dialog = AssetDatabase.LoadAssetAtPath<GameObject>(getPath(MessageDialog.PrefabName));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(getPath(SettingsPopup.PrefabName)) == null
                || !hasChild(getPath(PugBookPopup.PrefabName), "panel/tabs")
                || dialog == null || dialog.transform.Find("panel/icon") == null)
            {
                Build();
            }
        }

        /// <summary>
        /// プレハブがあって、指定の子があるか（今の構成で作ったかの判定）
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_child"></param>
        private static bool hasChild(string _path, string _child)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_path);
            return prefab != null && prefab.transform.Find(_child) != null;
        }

        /// <summary>
        /// ポップアップを作り直す
        /// </summary>
        [MenuItem("mumu/Build Popups")]
        public static void Build()
        {
            LoadFont();
            EditorAssetUtility.EnsureFolder(Folder);
            save(buildSettingsPopup(), SettingsPopup.PrefabName);
            save(buildMessageDialog(), MessageDialog.PrefabName);
            save(buildPugBook(), PugBookPopup.PrefabName);
            Debug.Log("[PopupPrefabBuilder] Popups built");
        }

        /// <summary>
        /// プレハブとして保存して一時オブジェクトを破棄
        /// </summary>
        /// <param name="_root"></param>
        /// <param name="_name"></param>
        private static void save(GameObject _root, string _name)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(_root, getPath(_name));
            }
            finally
            {
                Object.DestroyImmediate(_root);
            }
        }

        /// <summary>
        /// プレハブのパス
        /// </summary>
        /// <param name="_name"></param>
        private static string getPath(string _name) => $"{Folder}/{_name}.prefab";

        /// <summary>
        /// 親いっぱいに広がるルートを作る
        /// </summary>
        /// <param name="_name"></param>
        private static RectTransform createRoot(string _name)
        {
            var go = new GameObject(_name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            return Stretch((RectTransform)go.transform);
        }

        /// <summary>
        /// 設定ポップアップ（縦に並べ、非表示のボタンの分は詰める）
        /// </summary>
        private static GameObject buildSettingsPopup()
        {
            RectTransform root = createRoot(SettingsPopup.PrefabName);
            var popup = root.gameObject.AddComponent<SettingsPopup>();
            MumuButton dimButton = BuildDim(root);

            RectTransform panel = Create("panel", root);
            SetRect(panel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(600f, 0f));
            AddImage(panel, LoadSprite("ui_panel"), Color.white, true, Image.Type.Sliced);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 28, 44);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform title = Create("title", panel);
            title.sizeDelta = new Vector2(520f, 0f);
            setHeight(title, 72f);
            AddText(title, "SETTINGS", 48, TextBrown);

            MumuButton seToggle = BuildToggleRow(panel, "seRow", "ui_icon_se", "SE", 0f);
            setHeight((RectTransform)seToggle.transform.parent, RowHeight);
            MumuButton bgmToggle = BuildToggleRow(panel, "bgmRow", "ui_icon_bgm", "BGM", 0f);
            setHeight((RectTransform)bgmToggle.transform.parent, RowHeight);
            MumuButton retryButton = buildMenuButton(panel, "retryButton", "ui_icon_retry", "RETRY");
            MumuButton titleButton = buildMenuButton(panel, "titleButton", "ui_icon_home", "TO_TITLE");

            MumuButton closeButton = BuildCloseButton(panel);
            closeButton.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            SetField(popup, "panel", panel);
            SetField(popup, "seToggle", seToggle);
            SetField(popup, "bgmToggle", bgmToggle);
            SetField(popup, "closeButton", closeButton);
            SetField(popup, "dimButton", dimButton);
            SetField(popup, "toggleOn", LoadSprite("ui_toggle_on"));
            SetField(popup, "toggleOff", LoadSprite("ui_toggle_off"));
            SetField(popup, "retryButton", retryButton);
            SetField(popup, "titleButton", titleButton);
            return root.gameObject;
        }

        /// <summary>
        /// メッセージダイアログ（見出し・本文・はい／いいえ）
        /// </summary>
        private static GameObject buildMessageDialog()
        {
            RectTransform root = createRoot(MessageDialog.PrefabName);
            var dialog = root.gameObject.AddComponent<MessageDialog>();
            MumuButton dimButton = BuildDim(root);

            RectTransform panel = Create("panel", root);
            SetRect(panel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(660f, 600f));
            AddImage(panel, LoadSprite("ui_panel"), Color.white, true, Image.Type.Sliced);

            var top = new Vector2(0.5f, 1f);
            RectTransform title = Create("title", panel);
            SetRect(title, top, top, new Vector2(0f, -40f), new Vector2(580f, 72f));
            TMP_Text titleText = AddPlainText(title, "Title", 48, TextBrown);

            // アイコン（なければ MessageDialog が隠して本文を上げる）
            RectTransform icon = Create("icon", panel);
            SetRect(icon, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 90f), new Vector2(150f, 150f));
            Image iconImage = AddImage(icon, null, Color.white, false);
            iconImage.preserveAspect = true;

            RectTransform body = Create("body", panel);
            SetRect(body, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -50f), new Vector2(580f, 160f));
            TMP_Text bodyText = AddPlainText(body, "Message", 32, TextBrown);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 20f;
            bodyText.fontSizeMax = 32f;

            var bottom = new Vector2(0.5f, 0f);
            MumuButton yesButton = buildDialogButton(panel, "yesButton", new Vector2(-150f, 48f), bottom);
            MumuButton noButton = buildDialogButton(panel, "noButton", new Vector2(150f, 48f), bottom);
            noButton.image.color = new Color(0.85f, 0.85f, 0.85f);

            SetField(dialog, "panel", panel);
            SetField(dialog, "titleText", titleText);
            SetField(dialog, "bodyText", bodyText);
            SetField(dialog, "yesButton", yesButton);
            SetField(dialog, "noButton", noButton);
            SetField(dialog, "dimButton", dimButton);
            SetField(dialog, "iconImage", iconImage);
            return root.gameObject;
        }

        /// <summary>
        /// ずかん（大きなパネルに縦スクロールのグリッド。中身は PugBookPopup が作る）
        /// </summary>
        private static GameObject buildPugBook()
        {
            RectTransform root = createRoot(PugBookPopup.PrefabName);
            var popup = root.gameObject.AddComponent<PugBookPopup>();
            MumuButton dimButton = BuildDim(root);

            RectTransform panel = Stretch(Create("panel", root));
            panel.offsetMin = new Vector2(32f, 100f);
            panel.offsetMax = new Vector2(-32f, -100f);
            AddImage(panel, LoadSprite("ui_panel"), Color.white, true, Image.Type.Sliced);

            var top = new Vector2(0.5f, 1f);
            RectTransform title = Create("title", panel);
            SetRect(title, top, top, new Vector2(0f, -40f), new Vector2(520f, 72f));
            AddText(title, "PUG_BOOK", 48, TextBrown);

            RectTransform bones = Create("bones", panel);
            SetRect(bones, top, top, new Vector2(0f, -100f), new Vector2(520f, 44f));
            TMP_Text boneText = AddPlainText(bones, "0", 32, TextBrown);

            MumuButton closeButton = BuildCloseButton(panel);

            // tier のタブ（★1〜★6）
            RectTransform tabs = Create("tabs", panel);
            SetRect(tabs, top, top, new Vector2(0f, -160f), new Vector2(600f, 60f));
            var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = false;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = false;
            var tierTabs = new Object[CharacterMaster.MaxTier];
            for (int i = 0; i < tierTabs.Length; i++)
            {
                RectTransform tab = Create($"tab{i + 1}", tabs);
                tab.sizeDelta = new Vector2(88f, 56f);
                RectTransform label = Stretch(Create("label", tab));
                label.anchoredPosition = new Vector2(0f, 3f);
                TMP_Text text = AddPlainText(label, $"★{i + 1}", 28, TextBrown);
                tierTabs[i] = AddButton(tab, LoadSprite("ui_button"), Color.white, Image.Type.Sliced, text);
            }

            RectTransform scrollRect = Stretch(Create("scroll", panel));
            scrollRect.offsetMin = new Vector2(20f, 24f);
            scrollRect.offsetMax = new Vector2(-20f, -200f);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            // 何もない所を押してもスクロールできるよう透明な Image を置く
            AddImage(scrollRect, null, new Color(1f, 1f, 1f, 0f), true);

            RectTransform viewport = Stretch(Create("viewport", scrollRect));
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Create("content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200f, 262f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            SetField(popup, "panel", panel);
            SetField(popup, "closeButton", closeButton);
            SetField(popup, "dimButton", dimButton);
            SetField(popup, "boneText", boneText);
            SetField(popup, "content", content);
            SetArrayField(popup, "tierTabs", tierTabs);
            return root.gameObject;
        }

        /// <summary>
        /// ダイアログのボタン（文字はコードで設定）
        /// </summary>
        /// <param name="_panel"></param>
        /// <param name="_name"></param>
        /// <param name="_position"></param>
        /// <param name="_anchor"></param>
        private static MumuButton buildDialogButton(RectTransform _panel, string _name, Vector2 _position, Vector2 _anchor)
        {
            RectTransform rect = Create(_name, _panel);
            SetRect(rect, _anchor, _anchor, _position, new Vector2(260f, 100f));
            rect.pivot = new Vector2(0.5f, 0f);
            RectTransform label = Stretch(Create("label", rect));
            label.anchoredPosition = new Vector2(0f, 6f);
            TMP_Text text = AddPlainText(label, "OK", 40, TextBrown);
            return AddButton(rect, LoadSprite("ui_button"), Color.white, Image.Type.Sliced, text);
        }

        /// <summary>
        /// アイコンと文字のボタン（設定ポップアップの縦並び用）
        /// </summary>
        /// <param name="_panel"></param>
        /// <param name="_name"></param>
        /// <param name="_icon"></param>
        /// <param name="_key"></param>
        private static MumuButton buildMenuButton(RectTransform _panel, string _name, string _icon, string _key)
        {
            RectTransform rect = Create(_name, _panel);
            rect.sizeDelta = new Vector2(440f, 0f);
            setHeight(rect, MenuButtonHeight);

            RectTransform icon = Create("icon", rect);
            SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 6f), new Vector2(56f, 56f));
            AddImage(icon, LoadSprite(_icon), TextBrown, false);

            RectTransform label = Stretch(Create("label", rect));
            label.offsetMin = new Vector2(80f, 0f);
            label.anchoredPosition = new Vector2(40f, 6f);
            TMP_Text text = AddText(label, _key, 40, TextBrown);
            return AddButton(rect, LoadSprite("ui_button"), Color.white, Image.Type.Sliced, text);
        }

        /// <summary>
        /// 縦並びでの高さ
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_height"></param>
        private static void setHeight(RectTransform _rect, float _height)
        {
            var element = _rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = _height;
            element.preferredHeight = _height;
        }
    }
}
