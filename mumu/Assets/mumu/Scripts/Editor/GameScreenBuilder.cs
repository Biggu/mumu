using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static mumu.EditorUiBuilder;

namespace mumu
{
    /// <summary>
    /// ゲーム画面（Game.prefab）のヘッダーとメニューを組み立てる
    /// 未作成ならコンパイル後に自動実行。作り直す時は mumu > Build Game Header
    /// </summary>
    [InitializeOnLoad]
    public static class GameScreenBuilder
    {
        private const string PrefabPath = "Assets/mumu/Resources/Prefabs/View/Game.prefab";
        private const string PugSheetPath = "Assets/mumu/Resources/Sprites/Common/pug.png";
        private const string UiName = "ui";
        private const string HeaderName = "header";
        /// <summary>前の版でプレハブに埋め込んでいたメニュー（今は Resources/Prefabs/Popup から読み込む）</summary>
        private const string OldPopupName = "menuPopup";
        private const string StagePlateName = "stagePlate";
        private const string CrownName = "crown";
        private const string BonePlateName = "bonePlate";
        private const string BoneIconPath = "Assets/mumu/Resources/Sprites/Common/gimmick_bone.png";
        /// <summary>作り直す時に消す古い UI</summary>
        private static readonly string[] OldUiNames = { HeaderName, "resetButton", "progressText", "star1", "star1 (1)", "star1 (2)" };

        private const float HeaderHeight = 140f;
        private const float HeaderMargin = 16f;
        private const float StarSize = 64f;
        private const float StarSpacing = 72f;
        private const int StarCount = 3;
        private const float MenuButtonSize = 104f;
        private static readonly Color StageTextColor = new Color(1f, 0.92f, 0.63f);

        /// <summary>
        /// コンパイル後に自動実行を予約
        /// </summary>
        static GameScreenBuilder()
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
            return prefab == null || prefab.transform.Find($"{UiName}/{HeaderName}/{BonePlateName}") != null;
        }

        /// <summary>
        /// ヘッダーとメニューを作り直す（マップ部分はそのまま）
        /// </summary>
        [MenuItem("mumu/Build Game Header")]
        public static void Build()
        {
            LoadFont();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                GameView view = root.GetComponent<GameView>();
                Transform ui = root.transform.Find(UiName);
                if (view == null || ui == null)
                {
                    Debug.LogError("[GameScreenBuilder] GameView or ui not found in Game.prefab");
                    return;
                }

                foreach (string name in OldUiNames)
                {
                    Transform old = ui.Find(name);
                    if (old != null)
                    {
                        Object.DestroyImmediate(old.gameObject);
                    }
                }
                Transform oldPopup = root.transform.Find(OldPopupName);
                if (oldPopup != null)
                {
                    Object.DestroyImmediate(oldPopup.gameObject);
                }

                buildHeader(ui, view);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[GameScreenBuilder] Game.prefab header built");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// ヘッダー（ゲージ・★・メニューボタン）
        /// </summary>
        /// <param name="_ui"></param>
        /// <param name="_view"></param>
        private static void buildHeader(Transform _ui, GameView _view)
        {
            RectTransform header = Create(HeaderName, _ui);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -HeaderMargin);
            header.sizeDelta = new Vector2(-HeaderMargin * 2f, HeaderHeight);
            // 下のマップに触れないよう入力を受ける
            AddImage(header, LoadSprite("ui_panel"), Color.white, true, Image.Type.Sliced);

            // ★と王冠の分だけゲージを短くする
            float right = MenuButtonSize + HeaderMargin * 2f + StarSpacing * (StarCount + 1);
            buildGauge(header, right, _view);

            // ★（メニューボタンの左）
            var stars = new Object[StarCount];
            var middleRight = new Vector2(1f, 0.5f);
            for (int i = 0; i < StarCount; i++)
            {
                RectTransform star = Create($"star{i + 1}", header);
                float x = -(MenuButtonSize + HeaderMargin * 2f) - StarSpacing * (StarCount - 1 - i) - StarSize * 0.5f;
                SetRect(star, middleRight, middleRight, new Vector2(x, 0f), new Vector2(StarSize, StarSize));
                star.pivot = new Vector2(0.5f, 0.5f);
                stars[i] = AddImage(star, LoadSprite("ui_star_off"), Color.white, false);
            }

            // 王冠（★の左、PERFECT で光る）
            RectTransform crown = Create(CrownName, header);
            float crownX = -(MenuButtonSize + HeaderMargin * 2f) - StarSpacing * StarCount - StarSize * 0.5f;
            SetRect(crown, middleRight, middleRight, new Vector2(crownX, 0f), new Vector2(StarSize, StarSize));
            crown.pivot = new Vector2(0.5f, 0.5f);
            Image crownImage = AddImage(crown, LoadSprite("ui_crown_off"), Color.white, false);

            // メニューボタン（右端）
            RectTransform menu = Create("menuButton", header);
            SetRect(menu, middleRight, middleRight, new Vector2(-HeaderMargin, 0f), new Vector2(MenuButtonSize, MenuButtonSize));
            MumuButton menuButton = AddButton(menu, LoadSprite("ui_button_round"), SettingsColor, Image.Type.Simple, null);
            AddIcon(menu, "ui_icon_menu", 60f, Color.white);

            // ステージ番号（ヘッダーの下に下げた札）
            var bottom = new Vector2(0.5f, 0f);
            RectTransform plate = Create(StagePlateName, header);
            SetRect(plate, bottom, bottom, new Vector2(0f, -44f), new Vector2(280f, 64f));
            plate.pivot = new Vector2(0.5f, 0.5f);
            AddImage(plate, LoadSprite("ui_plate"), Color.white, false, Image.Type.Sliced);
            RectTransform stageLabel = Stretch(Create("label", plate));
            TMP_Text stageText = AddPlainText(stageLabel, "ステージ 1", 34, StageTextColor);

            // ほねの札（ステージ札の右、ほねがないマップでは GameView が隠す）
            RectTransform bonePlate = Create(BonePlateName, header);
            SetRect(bonePlate, bottom, bottom, new Vector2(230f, -44f), new Vector2(170f, 56f));
            bonePlate.pivot = new Vector2(0.5f, 0.5f);
            AddImage(bonePlate, LoadSprite("ui_plate"), Color.white, false, Image.Type.Sliced);
            RectTransform boneIcon = Create("icon", bonePlate);
            SetRect(boneIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(48f, 30f));
            boneIcon.pivot = new Vector2(0f, 0.5f);
            AddImage(boneIcon, AssetDatabase.LoadAssetAtPath<Sprite>(BoneIconPath), Color.white, false);
            RectTransform boneLabel = Stretch(Create("label", bonePlate));
            boneLabel.offsetMin = new Vector2(60f, 0f);
            TMP_Text boneText = AddPlainText(boneLabel, "0/0", 30, StageTextColor);

            SetArrayField(_view, "stars", stars);
            SetField(_view, "bonePlate", bonePlate.gameObject);
            SetField(_view, "boneText", boneText);
            SetField(_view, "stageText", stageText);
            SetField(_view, "crownImage", crownImage);
            SetField(_view, "crownOn", LoadSprite("ui_crown_on"));
            SetField(_view, "crownOff", LoadSprite("ui_crown_off"));
            SetField(_view, "starOn", LoadSprite("ui_star_on"));
            SetField(_view, "starOff", LoadSprite("ui_star_off"));
            SetField(_view, "menuButton", menuButton);
        }

        /// <summary>
        /// Goal までの残り手数ゲージ（左端にパグ）
        /// </summary>
        /// <param name="_header"></param>
        /// <param name="_right"></param>
        /// <param name="_view"></param>
        private static void buildGauge(RectTransform _header, float _right, GameView _view)
        {
            RectTransform gaugeRect = Create("goalGauge", _header);
            gaugeRect.anchorMin = new Vector2(0f, 0.5f);
            gaugeRect.anchorMax = new Vector2(1f, 0.5f);
            gaugeRect.pivot = new Vector2(0f, 0.5f);
            gaugeRect.offsetMin = new Vector2(HeaderMargin + 8f, -50f);
            gaugeRect.offsetMax = new Vector2(-_right, 50f);
            var gauge = gaugeRect.gameObject.AddComponent<GoalGauge>();

            RectTransform back = Create("back", gaugeRect);
            back.anchorMin = new Vector2(0f, 0.5f);
            back.anchorMax = new Vector2(1f, 0.5f);
            back.pivot = new Vector2(0.5f, 0.5f);
            back.offsetMin = new Vector2(56f, -26f);
            back.offsetMax = new Vector2(0f, 26f);
            AddImage(back, LoadSprite("ui_gauge_back"), Color.white, false, Image.Type.Sliced);

            RectTransform fill = Create("fill", back);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(8f, 8f);
            fill.offsetMax = new Vector2(-8f, -8f);
            AddImage(fill, LoadSprite("ui_gauge_fill"), Color.white, false, Image.Type.Sliced);

            RectTransform count = Stretch(Create("count", back));
            count.offsetMin = new Vector2(48f, 0f);
            TMP_Text countText = AddPlainText(count, "0", 36, Color.white);

            RectTransform pug = Create("pugIcon", gaugeRect);
            SetRect(pug, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 4f), new Vector2(108f, 108f));
            pug.pivot = new Vector2(0f, 0.5f);
            Image pugImage = AddImage(pug, loadPugSprite(PugSprites.GirlCryPrefix + "0"), Color.white, false);

            SetField(gauge, "fill", fill);
            SetField(gauge, "countText", countText);
            SetField(gauge, "pugIcon", pugImage);
            SetField(_view, "goalGauge", gauge);
        }

        /// <summary>
        /// パグのコマを読み込む
        /// </summary>
        /// <param name="_name"></param>
        private static Sprite loadPugSprite(string _name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PugSheetPath))
            {
                if (asset is Sprite sprite && sprite.name == _name)
                {
                    return sprite;
                }
            }
            Debug.LogWarning($"[GameScreenBuilder] Pug sprite not found: {_name}");
            return null;
        }
    }
}
