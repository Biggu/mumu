using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップ編集ウィンドウ（Resources/MapData の CSV を読み書き）
    /// </summary>
    public class MapDataEditor : EditorWindow
    {
        /// <summary>
        /// 編集モード
        /// </summary>
        private enum eEditMode
        {
            None,
            SetType,
            Waypoint,
            Gimmick,
        }

        /// <summary>
        /// ギミックの塗り方
        /// </summary>
        private enum eGimmickBrush
        {
            /// <summary>やじるし（向きは経路から自動）</summary>
            ArrowAuto,
            ArrowUp,
            ArrowDown,
            ArrowLeft,
            ArrowRight,
            Turn,
            Straight,
            Treasure1,
            Treasure2,
            Treasure3,
            KeyRed,
            KeyBlue,
            KeyYellow,
            GateRed,
            GateBlue,
            GateYellow,
            WarpInRed,
            WarpInBlue,
            WarpInYellow,
            WarpOutRed,
            WarpOutBlue,
            WarpOutYellow,
            Crumble,
            /// <summary>マスのギミックを全部消す</summary>
            Erase,
        }

        private const string GimmickSpriteFolder = "Assets/mumu/Resources/Sprites/Common/";

        private const float BtnSmall = 60f;
        private const float BtnMedium = 100f;
        private const float CellSize = 28f;
        private const float CellSpacing = 2f;
        private const float PreviewWidth = 70f;
        private const float PreviewSize = 64f;

        private static readonly Vector2Int NoCell = new Vector2Int(-1, -1);

        /// <summary>新規マップのサイズ</summary>
        private const int NewMapWidth = 10;
        private const int NewMapHeight = 5;

        /// <summary>見た目表示の ON/OFF の保存キー</summary>
        private const string ShowVisualKey = "mumu.MapEditor.ShowVisual";
        /// <summary>見た目表示のブロックの大きさ（px）</summary>
        private const float VisualMinTile = 12f;
        private const float VisualMaxTile = 64f;
        /// <summary>この大きさ未満ではブロックに番号を出さない</summary>
        private const float VisualLabelMinTile = 22f;
        /// <summary>ゲーム内のブロックの大きさ（持ち上げ量の換算用）</summary>
        private const float VisualBlockSize = 60f;
        /// <summary>デコ画像の縦横比（169x281）</summary>
        private const float DecoAspect = 281f / 169f;
        /// <summary>パグの大きさ（ブロックに対する割合）</summary>
        private const float PugSizeRate = 44f / 60f;
        private const string PugPreviewSpriteName = PugSprites.GirlCryPrefix + "0";

        private bool showVisual = true;
        private Vector2 windowScroll;
        private int cachedThemeId = -1;
        private readonly Dictionary<string, Sprite> themeSprites = new Dictionary<string, Sprite>();
        private Sprite pugSprite = null;
        private GUIStyle visualLabelStyle = null;

        /// <summary>map_list.csv の内容</summary>
        private List<MapInfo> mapList = new List<MapInfo>();
        private int selectedIndex = -1;
        /// <summary>未保存の変更があるか</summary>
        private bool isDirty = false;

        /// <summary>編集中のマップ（CSV から作った一時データ）</summary>
        private MapData mapData;
        private Vector2 scroll;
        private eEditMode mode;
        private eBlockType currentType = eBlockType.Walkable;
        private Vector2Int lastCell = NoCell;

        private int editWidth;
        private int editHeight;

        private int generateDifficulty = 5;
        private eMapPattern generatePattern = eMapPattern.Circle;
        private int generateSeed = 1;
        /// <summary>直前の生成結果（表示用）</summary>
        private string lastGenerateResult = null;
        /// <summary>生成時に置くギミック</summary>
        private GimmickSettings generateGimmicks;
        /// <summary>ギミックの塗り方</summary>
        private eGimmickBrush gimmickBrush = eGimmickBrush.ArrowAuto;
        /// <summary>Auto Place で置く数</summary>
        private GimmickCounts placeCounts = new GimmickCounts { Arrows = 2, Turns = 2, Straights = 1, Treasures = 1, Keys = 1, Warps = 1, Crumbles = 1 };
        private int placeSeed = 1;
        /// <summary>ギミックの画像（見た目表示用）</summary>
        private readonly Dictionary<string, Sprite> gimmickSprites = new Dictionary<string, Sprite>();
        /// <summary>検証で問題のあったマス（マス目で赤くする）</summary>
        private Vector2Int validateErrorCell = NoCell;
        private static readonly Color ValidateErrorColor = new Color(1f, 0.25f, 0.25f);

        /// <summary>テーマの選択肢</summary>
        private int[] themeIds = null;
        private string[] themeNames = null;
        /// <summary>描画中のチェックポイントの経路順</summary>
        private readonly HashSet<int> checkpointIndices = new HashSet<int>();

        /// <summary>
        /// ウィンドウを開く
        /// </summary>
        [MenuItem("Map/Edit")]
        private static void open()
        {
            GetWindow<MapDataEditor>("Map Editor");
        }

        /// <summary>
        /// 開いた時に一覧を読み込んで先頭のマップを開く
        /// </summary>
        private void OnEnable()
        {
            showVisual = EditorPrefs.GetBool(ShowVisualKey, true);
            reloadList();
            if (mapData == null && mapList.Count > 0)
            {
                openMap(0);
            }
        }

        /// <summary>
        /// 閉じた時に一時データを破棄
        /// </summary>
        private void OnDestroy()
        {
            if (isDirty && mapData != null && EditorUtility.DisplayDialog("Map Editor", $"{getMapLabel(MapCsv.ToInfo(mapData))} の変更を保存しますか？", "保存", "破棄"))
            {
                save();
            }
            destroyMapData();
        }

        /// <summary>
        /// 描画
        /// </summary>
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            showVisual = EditorGUILayout.ToggleLeft("Visual（画像とマップの見た目を表示）", showVisual);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(ShowVisualKey, showVisual);
            }

            windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
            drawContents();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// ウィンドウの中身
        /// </summary>
        private void drawContents()
        {
            drawMapSelect();
            if (mapData == null)
            {
                EditorGUILayout.HelpBox($"マップがありません。New で作成してください。（{MapCsv.GetListAssetPath()}）", MessageType.Info);
                return;
            }

            GUILayout.Space(8);

            if (!mapData.IsValidSize())
            {
                drawInitialize();
                return;
            }

            if (showVisual)
            {
                drawVisual();
            }
            drawTheme();
            drawSize();
            drawGenerate();
            drawMode();
            drawGrid();
        }

        /// <summary>
        /// ゲームと同じ見た目のマップ（操作はできない）
        /// </summary>
        private void drawVisual()
        {
            cacheThemeSprites(mapData.themeId);

            int width = mapData.width, height = mapData.height;
            float available = position.width - 40f;
            float tile = Mathf.Clamp(Mathf.Floor(available / ((width + height) * 0.5f)), VisualMinTile, VisualMaxTile);
            float half = tile * 0.5f, quarter = tile * 0.25f;
            float raise = tile * MapLayout.RaiseHeight / VisualBlockSize;
            float decoExtra = tile * (DecoAspect - 1f);
            float mapWidth = (width + height) * half;
            float mapHeight = (width + height - 2) * quarter + tile + decoExtra + raise;

            EditorGUILayout.BeginVertical("box");
            Rect area = GUILayoutUtility.GetRect(available, mapHeight + 8f);
            EditorGUILayout.EndVertical();
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float originX = area.x + (area.width - mapWidth) * 0.5f + height * half;
            float originY = area.y + 4f + decoExtra + raise;
            int maxSum = width + height - 2;

            // 奥（上の行・右の列）から手前の順に描く（Block.SetOrder と同じ順）
            for (int flippedY = height - 1; flippedY >= 0; flippedY--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    int y = (height - 1) - flippedY;
                    eBlockType type = mapData.GetBlockType(x, y);
                    if (type == eBlockType.Empty)
                    {
                        continue;
                    }

                    int waypoint = mapData.GetWaypointIndex(x, y);
                    bool isRaised = BlockTypeUtility.IsObstruct(type) || type == eBlockType.Start || type == eBlockType.Goal || waypoint >= 0;
                    float centerX = originX + (x - flippedY - 1) * half + half;
                    float top = originY + (maxSum - (x + flippedY)) * quarter - (isRaised ? raise : 0f);
                    var blockRect = new Rect(centerX - half, top, tile, tile);

                    // ゲームと同じく、通れないブロックの土台は暗く
                    Color baseColor = GUI.color;
                    if (BlockTypeUtility.IsObstruct(type))
                    {
                        GUI.color = Block.ObstructTint;
                    }
                    drawSprite(blockRect, getVisualSprite(type, waypoint));
                    GUI.color = baseColor;

                    int decoIndex = BlockTypeUtility.GetObstructIndex(type);
                    if (decoIndex >= 0 && !mapData.IsNearStartOrGoal(x, y))
                    {
                        float decoHeight = tile * DecoAspect;
                        drawSprite(new Rect(blockRect.x, blockRect.yMax - decoHeight, tile, decoHeight), getThemeSprite((ThemeSprites.DecoStartIndex + decoIndex).ToString()));
                    }

                    // 上面の中心（ブロックの上から 1/4）
                    float faceY = top + tile * 0.25f;
                    drawVisualGimmicks(x, y, centerX, faceY, tile);
                    if (type == eBlockType.Goal && pugSprite != null)
                    {
                        float pugSize = tile * PugSizeRate;
                        drawSprite(new Rect(centerX - pugSize * 0.5f, faceY - pugSize * 0.92f, pugSize, pugSize), pugSprite);
                    }

                    string label = type == eBlockType.Start ? "S" : (waypoint >= 0 ? (waypoint + 1).ToString() : "");
                    if (label.Length > 0 && tile >= VisualLabelMinTile)
                    {
                        GUI.Label(new Rect(centerX - half, faceY - 8f, tile, 16f), label, getVisualLabelStyle());
                    }
                }
            }
        }

        /// <summary>
        /// 見た目のギミック（やじるしは上面の向きに回す、ほねは少し浮かせる）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_centerX"></param>
        /// <param name="_faceY"></param>
        /// <param name="_tile"></param>
        private void drawVisualGimmicks(int _x, int _y, float _centerX, float _faceY, float _tile)
        {
            IReadOnlyList<eBlockSubType> subTypes = mapData.GetBlockSubTypes(_x, _y);
            if (subTypes.Count == 0)
            {
                return;
            }

            eBlockSubType route = GimmickUtility.GetRoute(subTypes);
            if (route != eBlockSubType.None)
            {
                float size = _tile * 0.5f;
                var rect = new Rect(_centerX - size * 0.5f, _faceY - size * 0.25f, size, size * 0.5f);
                if (GimmickUtility.IsArrow(route))
                {
                    // 上面のひし形に沿った角度（右 = 右上 26.6 度）
                    Vector2Int d = GimmickUtility.GetArrowDirection(route);
                    Vector2 screen = new Vector2(d.x + d.y, -(d.x - d.y) * 0.5f);
                    float angle = Mathf.Atan2(-screen.y, screen.x) * Mathf.Rad2Deg;
                    Matrix4x4 matrix = GUI.matrix;
                    GUIUtility.RotateAroundPivot(angle, rect.center);
                    drawSprite(new Rect(_centerX - size * 0.5f, _faceY - size * 0.2f, size, size * 0.4f), getGimmickSprite("gimmick_arrow"));
                    GUI.matrix = matrix;
                }
                else
                {
                    drawSprite(rect, getGimmickSprite(route == eBlockSubType.Turn ? "gimmick_turn" : "gimmick_straight"));
                }
            }
            eBlockSubType exclusive = GimmickUtility.GetExclusive(subTypes);
            int colorIndex = GimmickUtility.GetColorIndex(exclusive);
            Color oldColor = GUI.color;
            if (GimmickUtility.IsWarpIn(exclusive) || GimmickUtility.IsWarpOut(exclusive) || exclusive == eBlockSubType.Crumble)
            {
                float size = _tile * 0.5f;
                GUI.color = colorIndex >= 0 ? GimmickUtility.Colors[colorIndex] : Color.white;
                string sprite = GimmickUtility.IsWarpIn(exclusive) ? "gimmick_warp_in" : GimmickUtility.IsWarpOut(exclusive) ? "gimmick_warp_out" : "gimmick_crumble";
                drawSprite(new Rect(_centerX - size * 0.5f, _faceY - size * 0.25f, size, size * 0.5f), getGimmickSprite(sprite));
            }
            else if (GimmickUtility.IsGate(exclusive))
            {
                float size = _tile * 0.6f;
                GUI.color = GimmickUtility.Colors[colorIndex];
                drawSprite(new Rect(_centerX - size * 0.5f, _faceY - size * 0.55f, size, size * 0.6f), getGimmickSprite("gimmick_gate"));
            }
            eBlockSubType key = GimmickUtility.GetKey(subTypes);
            if (key != eBlockSubType.None)
            {
                float size = _tile * 0.45f;
                GUI.color = GimmickUtility.Colors[GimmickUtility.GetColorIndex(key)];
                drawSprite(new Rect(_centerX + _tile * 0.05f, _faceY - _tile * 0.6f, size, size * 0.5f), getGimmickSprite("gimmick_key"));
            }
            GUI.color = oldColor;

            eBlockSubType treasure = GimmickUtility.GetTreasure(subTypes);
            if (treasure != eBlockSubType.None)
            {
                float size = _tile * 0.45f;
                drawSprite(new Rect(_centerX - size * 0.5f, _faceY - _tile * 0.55f, size, size * 0.55f), getGimmickSprite("gimmick_treasure" + (treasure - eBlockSubType.Treasure1 + 1)));
            }
        }

        /// <summary>
        /// ギミックの画像（初回のみ読み込み）
        /// </summary>
        /// <param name="_name"></param>
        private Sprite getGimmickSprite(string _name)
        {
            if (!gimmickSprites.TryGetValue(_name, out Sprite sprite))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GimmickSpriteFolder + _name + ".png");
                gimmickSprites[_name] = sprite;
            }
            return sprite;
        }

        /// <summary>
        /// 見た目用のブロック画像（経路上は通過済みの画像）
        /// </summary>
        /// <param name="_type"></param>
        /// <param name="_waypoint"></param>
        private Sprite getVisualSprite(eBlockType _type, int _waypoint)
        {
            if (BlockTypeUtility.IsObstruct(_type))
            {
                return getThemeSprite(ThemeSprites.ObstructIndex.ToString());
            }

            switch (_type)
            {
                case eBlockType.Water: return getThemeSprite(ThemeSprites.WaterIndex.ToString());
                case eBlockType.Bridge: return getThemeSprite(ThemeSprites.BridgeIndex.ToString());
                case eBlockType.Start:
                case eBlockType.Goal:
                    return getThemeSprite(ThemeSprites.TouchedIndex.ToString());
                default:
                    return getThemeSprite((_waypoint >= 0 ? ThemeSprites.TouchedIndex : ThemeSprites.UntouchedIndex).ToString());
            }
        }

        /// <summary>
        /// テーマが変わったら画像を読み直す
        /// </summary>
        /// <param name="_themeId"></param>
        private void cacheThemeSprites(int _themeId)
        {
            if (cachedThemeId == _themeId)
            {
                return;
            }

            cachedThemeId = _themeId;
            themeSprites.Clear();
            if (pugSprite == null)
            {
                foreach (Sprite sprite in Resources.LoadAll<Sprite>(PugSprites.SheetPath))
                {
                    if (sprite.name == PugPreviewSpriteName)
                    {
                        pugSprite = sprite;
                    }
                }
            }
        }

        /// <summary>
        /// テーマ画像（読み込み済みならキャッシュ）
        /// </summary>
        /// <param name="_suffix"></param>
        private Sprite getThemeSprite(string _suffix)
        {
            if (!themeSprites.TryGetValue(_suffix, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>(ResourceManager.GetThemeSpritePath(mapData.themeId, _suffix));
                themeSprites[_suffix] = sprite;
            }
            return sprite;
        }

        /// <summary>
        /// スプライトをそのまま描く
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_sprite"></param>
        private static void drawSprite(Rect _rect, Sprite _sprite)
        {
            if (_sprite == null)
            {
                return;
            }

            Texture2D texture = _sprite.texture;
            Rect source = _sprite.textureRect;
            GUI.DrawTextureWithTexCoords(_rect, texture,
                new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height));
        }

        /// <summary>
        /// 見た目に重ねる文字のスタイル
        /// </summary>
        private GUIStyle getVisualLabelStyle()
        {
            if (visualLabelStyle == null)
            {
                visualLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                visualLabelStyle.normal.textColor = Color.white;
            }
            return visualLabelStyle;
        }

        /// <summary>
        /// サイズ入力欄を MapData に合わせる
        /// </summary>
        private void syncEditSize()
        {
            editWidth = mapData != null ? mapData.width : 0;
            editHeight = mapData != null ? mapData.height : 0;
        }

        /// <summary>
        /// Undo 付きで変更
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_change"></param>
        private void recordChange(string _name, System.Action _change)
        {
            Undo.RecordObject(mapData, _name);
            _change();
            EditorUtility.SetDirty(mapData);
            isDirty = true;
        }

        /// <summary>
        /// マップ選択欄（選択・新規・保存・再読み込み・名前）
        /// </summary>
        private void drawMapSelect()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            string[] labels = mapList.ConvertAll(getMapLabel).ToArray();
            int index = EditorGUILayout.Popup("Map", selectedIndex, labels);
            if (index != selectedIndex)
            {
                openMap(index);
            }

            if (GUILayout.Button("New", GUILayout.Width(BtnSmall)))
            {
                createMap();
            }

            using (new EditorGUI.DisabledScope(mapData == null))
            {
                if (GUILayout.Button("Copy", GUILayout.Width(BtnSmall)))
                {
                    copyMap();
                }
            }

            using (new EditorGUI.DisabledScope(!isDirty))
            {
                if (GUILayout.Button(isDirty ? "Save *" : "Save", GUILayout.Width(BtnSmall)))
                {
                    save();
                }
            }

            if (GUILayout.Button("Save Debug", GUILayout.Width(BtnSmall)) && mapData != null)
            {
                DebugMapFile.Save(mapData);
            }
            if (GUILayout.Button("Reload", GUILayout.Width(BtnSmall)) && confirmDiscard())
            {
                int reopen = selectedIndex;
                isDirty = false;
                reloadList();
                openMap(Mathf.Clamp(reopen, 0, mapList.Count - 1));
            }
            EditorGUILayout.EndHorizontal();

            if (mapData != null)
            {
                EditorGUI.BeginChangeCheck();
                string mapName = EditorGUILayout.TextField("Name", mapData.mapName);
                if (EditorGUI.EndChangeCheck())
                {
                    recordChange("Rename", () => mapData.mapName = mapName);
                }

                EditorGUI.BeginChangeCheck();
                int difficulty = EditorGUILayout.IntSlider("Difficulty", mapData.difficulty, MapGenerator.MinDifficulty, MapGenerator.MaxDifficulty);
                if (EditorGUI.EndChangeCheck())
                {
                    recordChange("Difficulty", () => mapData.difficulty = difficulty);
                }
                EditorGUILayout.LabelField(" ", $"Goal unlock: {MapGenerator.GetGoalUnlockRate(mapData.difficulty):P0} of waypoints");
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 一覧の表示名
        /// </summary>
        /// <param name="_info"></param>
        private static string getMapLabel(MapInfo _info)
        {
            return $"{_info.Id:000000} {_info.Name}";
        }

        /// <summary>
        /// map_list.csv を読み込む
        /// </summary>
        private void reloadList()
        {
            string path = MapCsv.GetListAssetPath();
            mapList = File.Exists(path) ? MapCsv.ParseList(File.ReadAllText(path)) : new List<MapInfo>();
        }

        /// <summary>
        /// 一覧の指定番号のマップを開く
        /// </summary>
        /// <param name="_index"></param>
        private void openMap(int _index)
        {
            // 未保存の新規マップを破棄すると一覧が変わるので確認を先に行う
            if (!confirmDiscard() || _index < 0 || _index >= mapList.Count)
            {
                return;
            }

            MapInfo info = mapList[_index];
            string path = MapCsv.GetMapAssetPath(info.Id);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Map Editor", $"{path} がありません", "OK");
                return;
            }

            destroyMapData();
            mapData = MapCsv.ParseMap(info, File.ReadAllText(path));
            mapData.hideFlags = HideFlags.DontSave;
            selectedIndex = _index;
            isDirty = false;
            syncEditSize();
        }

        /// <summary>
        /// 新しいマップを作成（Start・Goal を左右中央に置いた空のマップ）
        /// </summary>
        private void createMap()
        {
            if (!confirmDiscard())
            {
                return;
            }

            int id = getNextId();
            var newMap = CreateInstance<MapData>();
            newMap.id = id;
            newMap.mapName = $"map {id}";
            newMap.Initialize(NewMapWidth, NewMapHeight);
            for (int i = 0; i < newMap.Count; i++)
            {
                newMap.blockTypes[i] = eBlockType.Walkable;
            }
            newMap.SetBlockType(0, NewMapHeight / 2, eBlockType.Start);
            newMap.SetBlockType(NewMapWidth - 1, NewMapHeight / 2, eBlockType.Goal);

            openUnsavedMap(newMap);
        }

        /// <summary>
        /// 編集中のマップ（未保存の変更も含む）を新しい ID に複製
        /// </summary>
        private void copyMap()
        {
            // 確認ダイアログで元のマップが破棄されても複製できるよう先に作る
            MapData copy = Instantiate(mapData);
            copy.id = getNextId();
            copy.mapName = $"{mapData.mapName} copy";

            if (!confirmDiscard())
            {
                DestroyImmediate(copy);
                return;
            }

            // 保存しなかった新規マップが一覧から消えた場合に備えて ID を取り直す
            copy.id = getNextId();
            openUnsavedMap(copy);
        }

        /// <summary>
        /// 一覧で使われていない次の ID
        /// </summary>
        private int getNextId()
        {
            int id = 1;
            foreach (MapInfo info in mapList)
            {
                id = Mathf.Max(id, info.Id + 1);
            }
            return id;
        }

        /// <summary>
        /// 未保存のマップとして開いて一覧に追加
        /// </summary>
        /// <param name="_mapData"></param>
        private void openUnsavedMap(MapData _mapData)
        {
            destroyMapData();
            mapData = _mapData;
            mapData.hideFlags = HideFlags.DontSave;
            mapData.name = $"map_{mapData.id:000000}";

            mapList.Add(MapCsv.ToInfo(mapData));
            selectedIndex = mapList.Count - 1;
            isDirty = true;
            syncEditSize();
        }

        /// <summary>
        /// 編集中のマップと一覧を CSV に保存
        /// </summary>
        private void save()
        {
            if (mapData == null)
            {
                return;
            }

            if (!mapData.ValidatePath(out string error)
                && !EditorUtility.DisplayDialog("Map Editor", $"経路が正しくありません（{error}）。このまま保存しますか？", "保存", "キャンセル"))
            {
                return;
            }

            MapInfo info = MapCsv.ToInfo(mapData);
            int index = mapList.FindIndex(_info => _info.Id == info.Id);
            if (index >= 0)
            {
                mapList[index] = info;
            }
            else
            {
                mapList.Add(info);
                selectedIndex = mapList.Count - 1;
            }

            string mapPath = MapCsv.GetMapAssetPath(mapData.id);
            string listPath = MapCsv.GetListAssetPath();
            Directory.CreateDirectory(Path.GetDirectoryName(mapPath));
            File.WriteAllText(mapPath, MapCsv.WriteMap(mapData), MapCsv.FileEncoding);
            File.WriteAllText(listPath, MapCsv.WriteList(mapList), MapCsv.FileEncoding);
            AssetDatabase.ImportAsset(mapPath);
            AssetDatabase.ImportAsset(listPath);

            isDirty = false;
            Debug.Log($"[MapDataEditor] Saved {mapPath}");
        }

        /// <summary>
        /// 未保存の変更があれば保存・破棄を確認（キャンセルで false）
        /// </summary>
        private bool confirmDiscard()
        {
            if (!isDirty || mapData == null)
            {
                return true;
            }

            int choice = EditorUtility.DisplayDialogComplex("Map Editor", $"{getMapLabel(MapCsv.ToInfo(mapData))} の変更を保存しますか？", "保存", "キャンセル", "破棄");
            if (choice == 1)
            {
                return false;
            }
            if (choice == 0)
            {
                save();
            }
            else
            {
                // 未保存の新規マップは一覧から外す
                string path = MapCsv.GetMapAssetPath(mapData.id);
                if (!File.Exists(path))
                {
                    mapList.RemoveAll(_info => _info.Id == mapData.id);
                }
            }
            isDirty = false;
            return true;
        }

        /// <summary>
        /// 編集中の一時データを破棄
        /// </summary>
        private void destroyMapData()
        {
            if (mapData != null)
            {
                DestroyImmediate(mapData);
            }
            mapData = null;
        }

        /// <summary>
        /// 初期化欄
        /// </summary>
        private void drawInitialize()
        {
            EditorGUILayout.HelpBox("MapData のサイズと配列が一致していません。初期化してください。", MessageType.Warning);
            drawSizeFields();

            if (GUILayout.Button("Initialize", GUILayout.Width(BtnMedium)))
            {
                recordChange("Initialize", () => mapData.Initialize(Mathf.Max(1, editWidth), Mathf.Max(1, editHeight)));
                syncEditSize();
            }
        }

        /// <summary>
        /// テーマ欄
        /// </summary>
        private void drawTheme()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Theme", EditorStyles.boldLabel);

            if (themeIds == null)
            {
                var ids = new List<int>(ThemeDefine.ThemeIds);
                themeIds = ids.ToArray();
                themeNames = ids.ConvertAll(_id => $"{_id:000000} {ThemeDefine.GetName(_id)}").ToArray();
            }

            EditorGUI.BeginChangeCheck();
            int themeId = EditorGUILayout.IntPopup("Theme", mapData.themeId, themeNames, themeIds);
            if (EditorGUI.EndChangeCheck())
            {
                recordChange("Theme", () => mapData.themeId = themeId);
            }

            if (!showVisual)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            drawSpritePreview("Untouched", ThemeSprites.UntouchedIndex.ToString());
            drawSpritePreview("Touched", ThemeSprites.TouchedIndex.ToString());
            drawSpritePreview("Obstruct", ThemeSprites.ObstructIndex.ToString());
            for (int i = 0; i < ThemeSprites.DecoCount; i++)
            {
                drawSpritePreview($"Deco {i + 1}", (ThemeSprites.DecoStartIndex + i).ToString());
            }
            drawSpritePreview("Water", ThemeSprites.WaterIndex.ToString());
            drawSpritePreview("Bridge", ThemeSprites.BridgeIndex.ToString());
            drawSpritePreview("BG", ThemeSprites.BackgroundSuffix);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// テーマ画像のプレビュー
        /// </summary>
        /// <param name="_label"></param>
        /// <param name="_suffix"></param>
        private void drawSpritePreview(string _label, string _suffix)
        {
            string path = ResourceManager.GetThemeSpritePath(mapData.themeId, _suffix);
            Sprite[] frames = Resources.LoadAll<Sprite>(path);
            Sprite sprite = frames.Length > 0 ? frames[0] : null;

            EditorGUILayout.BeginVertical(GUILayout.Width(PreviewWidth));
            GUILayout.Label(_label);
            if (sprite != null)
            {
                GUILayout.Label(AssetPreview.GetAssetPreview(sprite), GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
            }
            else
            {
                EditorGUILayout.HelpBox($"{path} not found", MessageType.Error);
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 幅・高さの入力欄
        /// </summary>
        private void drawSizeFields()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("W", GUILayout.Width(15));
            editWidth = EditorGUILayout.IntField(editWidth, GUILayout.Width(80));
            GUILayout.Space(10);
            GUILayout.Label("H", GUILayout.Width(15));
            editHeight = EditorGUILayout.IntField(editHeight, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// サイズ変更欄
        /// </summary>
        private void drawSize()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            drawSizeFields();
            if (GUILayout.Button("Apply", GUILayout.Width(BtnSmall)))
            {
                recordChange("Resize", () => resize(Mathf.Max(1, editWidth), Mathf.Max(1, editHeight)));
                syncEditSize();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 自動生成欄
        /// </summary>
        private void drawGenerate()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Generate", EditorStyles.boldLabel);

            generateDifficulty = EditorGUILayout.IntSlider("Difficulty", generateDifficulty, MapGenerator.MinDifficulty, MapGenerator.MaxDifficulty);
            generatePattern = (eMapPattern)EditorGUILayout.EnumPopup("Pattern", generatePattern);

            EditorGUILayout.BeginHorizontal();
            generateSeed = EditorGUILayout.IntField("Seed", generateSeed);
            if (GUILayout.Button("Random", GUILayout.Width(BtnSmall + 10f)))
            {
                generateSeed = Random.Range(0, 1000000);
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 生成前に目安を表示
            Vector2Int size = MapGenerator.GetDimensions(generatePattern, MapGenerator.GetSize(generateDifficulty, generatePattern));
            float waypointRate = MapGenerator.GetWaypointRate(generateDifficulty);
            float obstructRate = MapGenerator.GetObstructRate(generateDifficulty);
            EditorGUILayout.LabelField(" ", $"{size.x}x{size.y}  waypoint {waypointRate:P0} / obstruct {obstructRate:P0} / free {1f - waypointRate - obstructRate:P0}");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Gimmick");
            generateGimmicks.Arrow = GUILayout.Toggle(generateGimmicks.Arrow, "Arrow", "Button", GUILayout.Width(BtnSmall));
            generateGimmicks.Turn = GUILayout.Toggle(generateGimmicks.Turn, "Turn", "Button", GUILayout.Width(BtnSmall));
            generateGimmicks.Straight = GUILayout.Toggle(generateGimmicks.Straight, "Straight", "Button", GUILayout.Width(BtnSmall + 10f));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            generateGimmicks.Key = GUILayout.Toggle(generateGimmicks.Key, "Key/Gate", "Button", GUILayout.Width(BtnMedium));
            generateGimmicks.Warp = GUILayout.Toggle(generateGimmicks.Warp, "Warp", "Button", GUILayout.Width(BtnSmall));
            generateGimmicks.Crumble = GUILayout.Toggle(generateGimmicks.Crumble, "Crumble", "Button", GUILayout.Width(BtnSmall + 10f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(" ", "宝箱は必ず1つ置く（木 60% / 銀 30% / 金 10%）");

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate", GUILayout.Width(BtnMedium))
                && EditorUtility.DisplayDialog("Generate", "現在のマップを上書きします。よろしいですか？", "OK", "Cancel"))
            {
                recordChange("Generate", () => lastGenerateResult = MapGenerator.Generate(mapData, generateDifficulty, generatePattern, generateSeed, true, generateGimmicks).ToString());
                syncEditSize();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(lastGenerateResult))
            {
                EditorGUILayout.HelpBox(lastGenerateResult, MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 編集モード欄
        /// </summary>
        private void drawMode()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(mode == eEditMode.SetType, "Type", "Button", GUILayout.Width(BtnSmall)))
            {
                mode = eEditMode.SetType;
            }
            if (mode == eEditMode.SetType)
            {
                currentType = (eBlockType)EditorGUILayout.EnumPopup(currentType);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(mode == eEditMode.Waypoint, "Waypoint", "Button", GUILayout.Width(BtnMedium)))
            {
                mode = eEditMode.Waypoint;
            }
            if (mode == eEditMode.Waypoint)
            {
                EditorGUILayout.LabelField("左: 末尾に追加 / 経路上を押すとそこまで戻して続きから描く  右: そこから後ろを削除");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(mode == eEditMode.Gimmick, "Gimmick", "Button", GUILayout.Width(BtnMedium)))
            {
                mode = eEditMode.Gimmick;
            }
            if (mode == eEditMode.Gimmick)
            {
                gimmickBrush = (eGimmickBrush)EditorGUILayout.EnumPopup(gimmickBrush, GUILayout.Width(BtnMedium));
                EditorGUILayout.LabelField("左: 置く（同じものを押すと外す）  右: そのマスを全部消す");
            }
            EditorGUILayout.EndHorizontal();
            if (mode == eEditMode.Gimmick)
            {
                drawGimmickPlace();
            }

            GUILayout.Space(6);

            // 編集するたびに検証して、結果と問題のマスを見せる
            bool isValid = mapData.ValidatePath(out string error, out validateErrorCell);
            if (isValid)
            {
                EditorGUILayout.HelpBox("Valid: このマップは遊べます", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(error + (validateErrorCell != MapData.NoPosition ? "\n（マス目の赤いマス）" : ""), MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate", GUILayout.Width(BtnMedium)))
            {
                validate();
            }
            if (GUILayout.Button("Clear Waypoints", GUILayout.Width(BtnMedium + 20f)))
            {
                recordChange("Clear Waypoints", mapData.ClearWaypoints);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// マス目
        /// </summary>
        private void drawGrid()
        {
            checkpointIndices.Clear();
            checkpointIndices.UnionWith(mapData.GetCheckpointIndices());

            EditorGUILayout.BeginVertical("box");
            scroll = EditorGUILayout.BeginScrollView(scroll);

            float gridWidth = mapData.width * (CellSize + CellSpacing);
            float pad = Mathf.Max(0, (position.width - gridWidth) * 0.5f);

            for (int y = 0; y < mapData.height; y++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(pad);

                for (int x = 0; x < mapData.width; x++)
                {
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = validateErrorCell == new Vector2Int(x, y) ? ValidateErrorColor : getColor(mapData.GetBlockType(x, y));

                    GUILayout.Box(getLabel(x, y), GUILayout.Width(CellSize), GUILayout.Height(CellSize));
                    Rect rect = GUILayoutUtility.GetLastRect();

                    GUI.backgroundColor = old;

                    handleInput(x, y, rect);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.MouseUp)
            {
                lastCell = NoCell;
            }
        }

        /// <summary>
        /// マスのラベル（チェックポイントは * 付き）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private string getLabel(int _x, int _y)
        {
            return getBaseLabel(_x, _y) + getGimmickLabel(_x, _y);
        }

        /// <summary>
        /// ギミックの記号（→ ← ↑ ↓ ↻ ║ と ほねは b）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private string getGimmickLabel(int _x, int _y)
        {
            string label = "";
            foreach (eBlockSubType subType in mapData.GetBlockSubTypes(_x, _y))
            {
                switch (subType)
                {
                    case eBlockSubType.ArrowUp: label += "↑"; break;
                    case eBlockSubType.ArrowDown: label += "↓"; break;
                    case eBlockSubType.ArrowLeft: label += "←"; break;
                    case eBlockSubType.ArrowRight: label += "→"; break;
                    case eBlockSubType.Turn: label += "↻"; break;
                    case eBlockSubType.Straight: label += "║"; break;
                    case eBlockSubType.Treasure1: label += "t1"; break;
                    case eBlockSubType.Treasure2: label += "t2"; break;
                    case eBlockSubType.Treasure3: label += "t3"; break;
                    case eBlockSubType.Crumble: label += "C"; break;
                }
                // 色つきは 種類の頭文字 + 色（r / b / y）: K カギ、G 扉、W ワープ入口、O ワープ出口
                int color = GimmickUtility.GetColorIndex(subType);
                if (color >= 0)
                {
                    string kind = GimmickUtility.IsKey(subType) ? "K" : GimmickUtility.IsGate(subType) ? "G" : GimmickUtility.IsWarpIn(subType) ? "W" : "O";
                    label += kind + "rby"[color];
                }
            }
            return label;
        }

        /// <summary>
        /// マスの番号・種類のラベル（チェックポイントは * 付き）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private string getBaseLabel(int _x, int _y)
        {
            int waypointIndex = mapData.GetWaypointIndex(_x, _y);
            if (waypointIndex >= 0)
            {
                return (waypointIndex + 1) + (checkpointIndices.Contains(waypointIndex) ? "*" : "");
            }

            eBlockType type = mapData.GetBlockType(_x, _y);
            int obstructIndex = BlockTypeUtility.GetObstructIndex(type);
            if (obstructIndex >= 0)
            {
                return (obstructIndex + 1).ToString();
            }

            switch (type)
            {
                case eBlockType.Start: return "S";
                case eBlockType.Goal: return "G";
                default: return "";
            }
        }

        /// <summary>
        /// マスのクリック・ドラッグ
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_rect"></param>
        private void handleInput(int _x, int _y, Rect _rect)
        {
            if (mode == eEditMode.None)
            {
                return;
            }

            Event e = Event.current;
            if (!_rect.Contains(e.mousePosition))
            {
                return;
            }

            Vector2Int cell = new Vector2Int(_x, _y);
            bool isPaint = e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag);
            bool isDelete = e.button == 1 && e.type == EventType.MouseDown;

            if (mode == eEditMode.Waypoint)
            {
                if (isPaint && cell != lastCell)
                {
                    recordChange("Waypoint", () => addWaypoint(_x, _y));
                    lastCell = cell;
                    useEvent(e);
                }
                else if (isDelete)
                {
                    recordChange("Waypoint Delete", () => deleteWaypoint(_x, _y));
                    useEvent(e);
                }
                return;
            }

            if (mode == eEditMode.Gimmick)
            {
                if (isPaint && cell != lastCell)
                {
                    // ドラッグ中は同じものを外さない（塗り広げる）
                    bool isToggle = e.type == EventType.MouseDown;
                    recordChange("Gimmick", () => paintGimmick(cell, isToggle));
                    lastCell = cell;
                    useEvent(e);
                }
                else if (isDelete)
                {
                    recordChange("Gimmick Erase", () => eraseGimmicks(cell));
                    useEvent(e);
                }
                return;
            }

            if (mode == eEditMode.SetType && isPaint && cell != lastCell)
            {
                recordChange("SetType", () => setType(_x, _y, currentType));
                lastCell = cell;
                useEvent(e);
            }
        }

        /// <summary>
        /// ギミックの自動配置欄（設計した経路に合うマスへ置き直す）
        /// </summary>
        private void drawGimmickPlace()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            placeCounts.Arrows = EditorGUILayout.IntField("Arrow", Mathf.Max(0, placeCounts.Arrows));
            placeCounts.Turns = EditorGUILayout.IntField("Turn", Mathf.Max(0, placeCounts.Turns));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            placeCounts.Straights = EditorGUILayout.IntField("Straight", Mathf.Max(0, placeCounts.Straights));
            placeCounts.Treasures = EditorGUILayout.IntField("Treasure", Mathf.Max(0, placeCounts.Treasures));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            placeCounts.Keys = EditorGUILayout.IntField("Key/Gate", Mathf.Clamp(placeCounts.Keys, 0, GimmickUtility.ColorCount));
            placeCounts.Warps = EditorGUILayout.IntField("Warp", Mathf.Clamp(placeCounts.Warps, 0, GimmickUtility.ColorCount));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            placeCounts.Crumbles = EditorGUILayout.IntField("Crumble", Mathf.Max(0, placeCounts.Crumbles));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            placeSeed = EditorGUILayout.IntField("Seed", placeSeed);
            if (GUILayout.Button("Random", GUILayout.Width(BtnSmall + 10f)))
            {
                placeSeed = Random.Range(0, 1000000);
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Suggest", GUILayout.Width(BtnMedium)))
            {
                // 経路の長さに合わせたおすすめの数
                var all = new GimmickSettings { Arrow = true, Turn = true, Straight = true, Key = true, Warp = true, Crumble = true };
                placeCounts = GimmickCounts.FromPath(Mathf.Max(0, mapData.GetMaxWaypointIndex() + 1), all);
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("Auto Place", GUILayout.Width(BtnMedium)))
            {
                recordChange("Auto Place", () => lastGenerateResult = "gimmick " + MapGenerator.PlaceGimmicks(mapData, placeCounts, new System.Random(placeSeed)));
            }
            if (GUILayout.Button("Clear Gimmicks", GUILayout.Width(BtnMedium + 20f)))
            {
                recordChange("Clear Gimmicks", mapData.ClearSubTypes);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(" ", $"今: arrow {countArrows()}, turn {mapData.CountSubType(eBlockSubType.Turn)}, straight {mapData.CountSubType(eBlockSubType.Straight)}, treasure {countTreasureBones()} bones, crumble {mapData.CountSubType(eBlockSubType.Crumble)}");
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 宝箱でもらえるほねの合計
        /// </summary>
        private int countTreasureBones()
        {
            int bones = 0;
            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    bones += GimmickUtility.GetTreasureBones(mapData.GetBlockSubTypes(x, y));
                }
            }
            return bones;
        }

        /// <summary>
        /// 置かれているやじるしの数
        /// </summary>
        private int countArrows()
        {
            int count = 0;
            foreach (eBlockSubType arrow in GimmickUtility.Arrows)
            {
                count += mapData.CountSubType(arrow);
            }
            return count;
        }

        /// <summary>
        /// ギミックを置く（通り方のギミックは1マス1つで置き換え、_isToggle で同じものなら外す）
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_isToggle"></param>
        private void paintGimmick(Vector2Int _cell, bool _isToggle)
        {
            if (gimmickBrush == eGimmickBrush.Erase)
            {
                eraseGimmicks(_cell);
                return;
            }
            if (mapData.GetBlockType(_cell.x, _cell.y) != eBlockType.Walkable)
            {
                ShowNotification(new GUIContent("ギミックは通れるマス（Walkable）にだけ置けます"));
                return;
            }

            eBlockSubType subType = toSubType(gimmickBrush, _cell);
            if (subType == eBlockSubType.None)
            {
                ShowNotification(new GUIContent("Arrow Auto は経路上のマスにだけ置けます"));
                return;
            }

            bool hasSame = mapData.HasSubType(_cell.x, _cell.y, subType);
            if (hasSame)
            {
                if (_isToggle)
                {
                    mapData.RemoveBlockSubType(_cell.x, _cell.y, subType);
                }
                return;
            }

            // 1マス1つのギミック・カギ・宝箱はそれぞれ置き換える（宝箱・カギは一緒に置ける）
            IReadOnlyList<eBlockSubType> current = mapData.GetBlockSubTypes(_cell.x, _cell.y);
            eBlockSubType old = GimmickUtility.IsExclusive(subType) ? GimmickUtility.GetExclusive(current)
                : (GimmickUtility.IsKey(subType) ? GimmickUtility.GetKey(current) : (GimmickUtility.IsTreasure(subType) ? GimmickUtility.GetTreasure(current) : eBlockSubType.None));
            if (old != eBlockSubType.None)
            {
                mapData.RemoveBlockSubType(_cell.x, _cell.y, old);
            }
            mapData.AddBlockSubType(_cell.x, _cell.y, subType);
        }

        /// <summary>
        /// マスのギミックを全部消す
        /// </summary>
        /// <param name="_cell"></param>
        private void eraseGimmicks(Vector2Int _cell)
        {
            foreach (eBlockSubType subType in new List<eBlockSubType>(mapData.GetBlockSubTypes(_cell.x, _cell.y)))
            {
                mapData.RemoveBlockSubType(_cell.x, _cell.y, subType);
            }
        }

        /// <summary>
        /// 塗り方 → ギミック（Arrow Auto は経路の次のマスへの向き、経路外なら None）
        /// </summary>
        /// <param name="_brush"></param>
        /// <param name="_cell"></param>
        private eBlockSubType toSubType(eGimmickBrush _brush, Vector2Int _cell)
        {
            switch (_brush)
            {
                case eGimmickBrush.ArrowUp: return eBlockSubType.ArrowUp;
                case eGimmickBrush.ArrowDown: return eBlockSubType.ArrowDown;
                case eGimmickBrush.ArrowLeft: return eBlockSubType.ArrowLeft;
                case eGimmickBrush.ArrowRight: return eBlockSubType.ArrowRight;
                case eGimmickBrush.Turn: return eBlockSubType.Turn;
                case eGimmickBrush.Straight: return eBlockSubType.Straight;
                case eGimmickBrush.Treasure1: return eBlockSubType.Treasure1;
                case eGimmickBrush.Treasure2: return eBlockSubType.Treasure2;
                case eGimmickBrush.Treasure3: return eBlockSubType.Treasure3;
                case eGimmickBrush.KeyRed: return eBlockSubType.KeyRed;
                case eGimmickBrush.KeyBlue: return eBlockSubType.KeyBlue;
                case eGimmickBrush.KeyYellow: return eBlockSubType.KeyYellow;
                case eGimmickBrush.GateRed: return eBlockSubType.GateRed;
                case eGimmickBrush.GateBlue: return eBlockSubType.GateBlue;
                case eGimmickBrush.GateYellow: return eBlockSubType.GateYellow;
                case eGimmickBrush.WarpInRed: return eBlockSubType.WarpInRed;
                case eGimmickBrush.WarpInBlue: return eBlockSubType.WarpInBlue;
                case eGimmickBrush.WarpInYellow: return eBlockSubType.WarpInYellow;
                case eGimmickBrush.WarpOutRed: return eBlockSubType.WarpOutRed;
                case eGimmickBrush.WarpOutBlue: return eBlockSubType.WarpOutBlue;
                case eGimmickBrush.WarpOutYellow: return eBlockSubType.WarpOutYellow;
                case eGimmickBrush.Crumble: return eBlockSubType.Crumble;
            }

            int index = mapData.GetWaypointIndex(_cell.x, _cell.y);
            if (index < 0)
            {
                return eBlockSubType.None;
            }
            bool hasNext = index == mapData.GetMaxWaypointIndex()
                ? mapData.TryFindBlock(eBlockType.Goal, out Vector2Int next)
                : mapData.TryFindWaypoint(index + 1, out next);
            return hasNext && MapData.IsAdjacent(_cell, next) ? GimmickUtility.ToArrow(next - _cell) : eBlockSubType.None;
        }

        /// <summary>
        /// イベントを消費して再描画
        /// </summary>
        /// <param name="_e"></param>
        private void useEvent(Event _e)
        {
            _e.Use();
            Repaint();
        }

        /// <summary>
        /// ブロック種別を設定
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_type"></param>
        private void setType(int _x, int _y, eBlockType _type)
        {
            // Start / Goal は1つだけ
            if ((_type == eBlockType.Start || _type == eBlockType.Goal)
                && mapData.TryFindBlock(_type, out Vector2Int prev)
                && prev != new Vector2Int(_x, _y))
            {
                mapData.SetBlockType(prev.x, prev.y, eBlockType.Walkable);
            }

            mapData.SetBlockType(_x, _y, _type);

            // 通れなくなったマス以降の経路を削除
            if (!BlockTypeUtility.IsWalkable(_type))
            {
                int index = mapData.GetWaypointIndex(_x, _y);
                if (index >= 0)
                {
                    mapData.RemoveWaypointsFrom(index);
                }
            }
        }

        /// <summary>
        /// 経路の末尾に追加（前のマスに隣接している時のみ）
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private void addWaypoint(int _x, int _y)
        {
            // 経路上（Start も）を押したらそこまで戻して、続きから描けるようにする（ゲームの巻き戻しと同じ）
            int existing = mapData.GetWaypointIndex(_x, _y);
            if (existing >= 0)
            {
                mapData.RemoveWaypointsFrom(existing + 1);
                return;
            }
            if (mapData.GetBlockType(_x, _y) == eBlockType.Start)
            {
                mapData.ClearWaypoints();
                return;
            }

            if (!BlockTypeUtility.IsWalkable(mapData.GetBlockType(_x, _y)))
            {
                return;
            }

            Vector2Int pos = new Vector2Int(_x, _y);
            int max = mapData.GetMaxWaypointIndex();

            Vector2Int last;
            bool hasLast = max < 0
                ? mapData.TryFindBlock(eBlockType.Start, out last)
                : mapData.TryFindWaypoint(max, out last);

            // ワープの入口の次は、離れていても同じ色の出口を置ける
            var visited = new HashSet<Vector2Int>(mapData.GetWaypointPositions(out _));
            if (mapData.TryFindBlock(eBlockType.Start, out Vector2Int start))
            {
                visited.Add(start);
            }
            if (hasLast && (MapData.IsAdjacent(pos, last) || mapData.IsWarpJump(last, pos, visited)))
            {
                mapData.SetWaypointIndex(_x, _y, max + 1);
            }
        }

        /// <summary>
        /// 指定マス以降の経路を削除
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private void deleteWaypoint(int _x, int _y)
        {
            int index = mapData.GetWaypointIndex(_x, _y);
            if (index >= 0)
            {
                mapData.RemoveWaypointsFrom(index);
            }
        }

        /// <summary>
        /// 経路を検証して結果を表示
        /// </summary>
        private void validate()
        {
            if (mapData.ValidatePath(out string error))
            {
                EditorUtility.DisplayDialog("OK", "Valid", "Nice");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", error, "OK");
            }
        }

        /// <summary>
        /// サイズ変更（既存のマスは維持、増えたマスは Walkable）
        /// </summary>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        private void resize(int _width, int _height)
        {
            int oldWidth = mapData.width;
            int oldHeight = mapData.height;
            var oldTypes = mapData.blockTypes;
            var oldSubTypes = mapData.blockSubTypes;

            mapData.width = _width;
            mapData.height = _height;
            mapData.blockTypes = new eBlockType[_width * _height];
            mapData.blockSubTypes = new MapData.BlockSubTypeList[_width * _height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int newIndex = y * _width + x;

                    if (x < oldWidth && y < oldHeight)
                    {
                        int oldIndex = y * oldWidth + x;
                        mapData.blockTypes[newIndex] = oldTypes[oldIndex];
                        mapData.blockSubTypes[newIndex] = oldSubTypes[oldIndex] ?? new MapData.BlockSubTypeList();
                    }
                    else
                    {
                        mapData.blockTypes[newIndex] = eBlockType.Walkable;
                        mapData.blockSubTypes[newIndex] = new MapData.BlockSubTypeList();
                    }
                }
            }

            // 縮小で範囲外に出た経路以降を削除
            mapData.TrimBrokenWaypoints();
        }

        /// <summary>
        /// マスの色
        /// </summary>
        /// <param name="_type"></param>
        private static Color getColor(eBlockType _type)
        {
            switch (_type)
            {
                case eBlockType.Empty: return Color.gray;
                case eBlockType.Walkable: return Color.green;
                case eBlockType.Bridge: return new Color(0.8f, 0.6f, 0.35f);
                case eBlockType.Obstruct1: return new Color(0.1f, 0.1f, 0.1f);
                case eBlockType.Obstruct2: return new Color(0.3f, 0.2f, 0.1f);
                case eBlockType.Obstruct3: return new Color(0.15f, 0.3f, 0.15f);
                case eBlockType.Obstruct4: return new Color(0.3f, 0.15f, 0.3f);
                case eBlockType.Start: return Color.cyan;
                case eBlockType.Goal: return Color.red;
                case eBlockType.Water: return new Color(0.3f, 0.55f, 1f);
                default: return Color.white;
            }
        }
    }
}
