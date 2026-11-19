using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static mumu.EditorUiBuilder;

namespace mumu
{
    /// <summary>
    /// タイトル画面（Title.prefab）を組み立てる
    /// 未作成ならコンパイル後に自動実行。作り直す時は mumu > Build Title Screen
    /// </summary>
    [InitializeOnLoad]
    public static class TitleScreenBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/View/Title.prefab";
        /// <summary>作成済みの判定に使う（今の構成で作ったか）</summary>
        private const string BuiltMarkerPath = "ui/gardenButton";

        private static readonly Color TitleColor = new Color(1f, 0.92f, 0.63f);
        private static readonly Color ShadowColor = new Color(0.18f, 0.1f, 0.05f, 0.85f);
        /// <summary>おさんぽボタンの色（冒険と見分ける）</summary>
        private static readonly Color StrollColor = new Color(0.75f, 1f, 0.75f);
        /// <summary>おにわボタンの色</summary>
        private static readonly Color GardenColor = new Color(1f, 0.82f, 0.9f);

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static TitleScreenBuilder()
        {
            EditorApplication.delayCall += autoBuild;
        }

        /// <summary>
        /// 未作成なら実行
        /// </summary>
        private static void autoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || isBuilt())
            {
                return;
            }
            Build();
        }

        /// <summary>
        /// 作成済みか
        /// </summary>
        private static bool isBuilt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return prefab == null || prefab.transform.Find(BuiltMarkerPath) != null;
        }

        /// <summary>
        /// タイトル画面を作り直す
        /// </summary>
        [MenuItem("mumu/Build Title Screen")]
        public static void Build()
        {
            LoadFont();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }

                TitleView view = root.GetComponent<TitleView>();
                if (view == null)
                {
                    view = root.AddComponent<TitleView>();
                }

                buildStage(root.transform, view);
                buildUi(root.transform, view);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[TitleScreenBuilder] Title.prefab built");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 背景ステージ（背景・マップ）
        /// </summary>
        /// <param name="_root"></param>
        /// <param name="_view"></param>
        private static void buildStage(Transform _root, TitleView _view)
        {
            RectTransform stage = Stretch(Create("stage", _root));

            // セーフエリア外も埋まるよう大きくする
            RectTransform bg = Create("bg", stage);
            SetRect(bg, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(4000f, 4000f));
            AddImage(bg, null, Color.white, false);

            RectTransform mapGroup = Create("mapGroup", stage);
            SetRect(mapGroup, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(100f, 100f));
            RectTransform mapRoot = Create("maproot", mapGroup);
            SetRect(mapRoot, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);

            SetField(_view, "mapRoot", mapRoot.gameObject);
            SetField(_view, "mapGroup", mapGroup);
            SetField(_view, "bgImage", bg.GetComponent<Image>());
        }

        /// <summary>
        /// タイトル・キャッチコピー・ボタン
        /// </summary>
        /// <param name="_root"></param>
        /// <param name="_view"></param>
        private static void buildUi(Transform _root, TitleView _view)
        {
            RectTransform ui = Stretch(Create("ui", _root));
            var top = new Vector2(0.5f, 1f);

            // タイトル（木の看板に影付きの文字）
            RectTransform plate = Create("titlePlate", ui);
            SetRect(plate, top, top, new Vector2(0f, -40f), new Vector2(640f, 176f));
            AddImage(plate, LoadSprite("ui_plate"), Color.white, false, Image.Type.Sliced);
            RectTransform shadow = Stretch(Create("titleShadow", plate));
            shadow.anchoredPosition = new Vector2(6f, -6f);
            AddText(shadow, "TITLE", 96, ShadowColor);
            AddText(Stretch(Create("titleText", plate)), "TITLE", 96, TitleColor);

            // キャッチコピー
            RectTransform catchBack = Create("catchphrase", ui);
            SetRect(catchBack, top, top, new Vector2(0f, -232f), new Vector2(760f, 72f));
            AddImage(catchBack, LoadSprite("ui_panel"), Color.white, false, Image.Type.Sliced);
            AddText(Stretch(Create("label", catchBack)), "CATCHPHRASE", 32, TextBrown);

            // 冒険・おさんぽボタン
            MumuButton adventureButton = buildModeButton(ui, "adventureButton", "ADVENTURE", new Vector2(0f, 392f), Color.white);
            MumuButton strollButton = buildModeButton(ui, "strollButton", "STROLL", new Vector2(0f, 256f), StrollColor);
            MumuButton gardenButton = buildModeButton(ui, "gardenButton", "GARDEN", new Vector2(0f, 120f), GardenColor);

            // 設定ボタン（右上）
            RectTransform settings = Create("settingsButton", ui);
            SetRect(settings, Vector2.one, Vector2.one, new Vector2(-72f, -72f), new Vector2(104f, 104f));
            MumuButton settingsButton = AddButton(settings, LoadSprite("ui_button_round"), SettingsColor, Image.Type.Simple, null);
            AddIcon(settings, "ui_icon_gear", 60f, Color.white);

            SetField(_view, "adventureButton", adventureButton);
            SetField(_view, "strollButton", strollButton);
            SetField(_view, "gardenButton", gardenButton);
            SetField(_view, "settingsButton", settingsButton);
        }

        /// <summary>
        /// 画面下のモードボタン
        /// </summary>
        /// <param name="_ui"></param>
        /// <param name="_name"></param>
        /// <param name="_key"></param>
        /// <param name="_position"></param>
        /// <param name="_color"></param>
        private static MumuButton buildModeButton(RectTransform _ui, string _name, string _key, Vector2 _position, Color _color)
        {
            var bottom = new Vector2(0.5f, 0f);
            RectTransform rect = Create(_name, _ui);
            SetRect(rect, bottom, bottom, _position, new Vector2(400f, 112f));
            RectTransform label = Stretch(Create("label", rect));
            label.anchoredPosition = new Vector2(0f, 6f);
            var text = AddText(label, _key, 48, TextBrown);
            return AddButton(rect, LoadSprite("ui_button"), _color, Image.Type.Sliced, text);
        }
    }
}
