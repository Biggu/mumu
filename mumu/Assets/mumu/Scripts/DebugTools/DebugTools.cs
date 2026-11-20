#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// デバッグ機能（エディタだけ）。キー操作（C: クリア、X 長押し: 経路の番号、V: パグを全部、B: ほね）と、D のメニューの項目をここにまとめる。
    /// 項目を増やす時は registerItems に Add(名前, 処理) を1行書く
    /// </summary>
    public static class DebugTools
    {
        /// <summary>ほねを最大にする時の数</summary>
        private const int MaxBones = 999999;
        /// <summary>B で増やすほねの数</summary>
        private const int BonesPerKey = 999999;
        /// <summary>ゲームの速さ（2倍と1倍を切り替える）</summary>
        private const float FastSpeed = 2f;

        private const System.Reflection.BindingFlags PrivateInstance = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        private static readonly List<DebugItem> items = new List<DebugItem>();
        private static bool isPreview = false;
        /// <summary>デバッグマップを遊ぶために待っているマップ（一度使ったら空になる）</summary>
        private static MapData pendingMap = null;
        /// <summary>デバッグマップの場所（Resources から）</summary>
        private const string DebugMapFolder = "MapData/debug/";

        /// <summary>メニューに並ぶ項目（登録した順）</summary>
        public static IReadOnlyList<DebugItem> Items => items;

        /// <summary>待っているデバッグマップがあるか（GameSession が使う）</summary>
        public static bool HasPendingMap => pendingMap != null;

        /// <summary>待っているデバッグマップを渡して、空にする（GameSession が使う）</summary>
        public static MapData TakePendingMap()
        {
            MapData map = pendingMap;
            pendingMap = null;
            return map;
        }

        /// <summary>
        /// 項目を追加する（名前と、押した時の処理）
        /// </summary>
        /// <param name="_label"></param>
        /// <param name="_action"></param>
        public static void Add(string _label, Action _action)
        {
            items.Add(new DebugItem(_label, _action));
        }

        /// <summary>
        /// 起動後にメニューを作る（シーンが変わっても1つだけ）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void create()
        {
            if (UnityEngine.Object.FindAnyObjectByType<DebugOverlay>() != null)
            {
                return;
            }
            items.Clear();
            registerItems();

            var go = new GameObject("DebugMenu");
            go.AddComponent<DebugOverlay>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        /// <summary>
        /// 毎フレーム呼ぶ。キーでデバッグ操作をする
        /// </summary>
        public static void Update()
        {
            InGameMap map = currentMap();
            bool isPlaying = map != null && map.Path != null && !map.Path.IsCleared;

            if (isPlaying && Input.GetKeyDown(KeyCode.C))
            {
                map.DebugSolve();
            }

            bool preview = isPlaying && Input.GetKey(KeyCode.X);
            if (map != null && preview != isPreview)
            {
                isPreview = preview;
                map.SetWaypointPreview(preview);
            }

            if (UnityEngine.Object.FindAnyObjectByType<TitleView>() != null)
            {
                if (Input.GetKeyDown(KeyCode.V))
                {
                    addAllPugs();
                }
                if (Input.GetKeyDown(KeyCode.B))
                {
                    addBones(BonesPerKey);
                }
            }
        }

        /// <summary>
        /// メニューの項目（ここに Add を足す）
        /// </summary>
        private static void registerItems()
        {
            // 今のステージ
            Add("今のステージを クリア", solveCurrentStage);
            Add("今のステージを クリア（もどり あり・ボーナスなし）", solveWithRewind);
            Add("経路を全部 ヒントの線で出す", showAllHint);

            // ステージ全部
            Add("全ステージを ★3で クリア", () => clearAllStages(3, false));
            Add("全ステージを ★4（大きな星）で クリア", () => clearAllStages(3, true));
            Add("全ステージを 未クリアに戻す", unclearAllStages);

            // パグ
            Add("パグを全部 仲間に", addAllPugs);
            Add("パグを 1匹だけ残す", keepOnePug);
            Add("おにわの喜びを 次に出す", () =>
            {
                SaveDataManager.Data.gardenCheerPending = true;
                SaveDataManager.Save();
            });

            // ほね
            Add("ほねを 最大にする", () =>
            {
                SaveDataManager.Data.bones = MaxBones;
                SaveDataManager.Save();
            });

            // そのほか
            Add("ヒントを消す", () =>
            {
                SaveDataManager.Data.hintStage = 0;
                SaveDataManager.Save();
            });
            Add("テーマの おはなしを リセット", () =>
            {
                SaveDataManager.Data.seenThemeStories.Clear();
                SaveDataManager.Save();
            });
            Add("ギミックの 説明を リセット", () =>
            {
                SaveDataManager.Data.seenGimmicks.Clear();
                SaveDataManager.Save();
            });
            Add("今日のおさんぽを やり直す（回数と日付を 戻す）", () =>
            {
                SaveDataManager.Data.dailyStrollCount = 0;
                SaveDataManager.Data.dailyStrollDate = "";
                SaveDataManager.Save();
            });
            Add("ゲームの速さ 2倍 ⇄ 1倍", () => Time.timeScale = Time.timeScale > 1f ? 1f : FastSpeed);

            // デバッグマップ（マップ編集の Save Debug で保存したもの）
            Add("デバッグマップを 読み込んで 遊ぶ", playDebugMap);
        }

        /// <summary>
        /// 今のステージを、設計した経路どおりにその場でクリアする
        /// </summary>
        private static void solveCurrentStage()
        {
            InGameMap map = currentMap();
            if (map != null && !map.Path.IsCleared)
            {
                map.DebugSolve();
            }
        }

        /// <summary>
        /// 今のステージを、もどった扱いでクリアする（もどらずボーナスと大きな★は出ない）
        /// </summary>
        private static void solveWithRewind()
        {
            InGameMap map = currentMap();
            if (map == null || map.Path.IsCleared)
            {
                return;
            }
            // 本体のコードは変えず、もどった記録の欄だけ立ててからクリアする
            typeof(InGameMap).GetField("hasRewound", PrivateInstance).SetValue(map, true);
            map.DebugSolve();
        }

        /// <summary>
        /// 今のステージの経路を全部ヒントの線で出す
        /// </summary>
        private static void showAllHint()
        {
            InGameMap map = currentMap();
            if (map == null || map.Path.IsCleared)
            {
                return;
            }

            var plan = new HintPlan(HintPlan.GetDesignedPath(map.MapData), 0);
            plan.SelectAll();
            // 本体のコードは変えず、非公開の欄に反映して描き直す
            typeof(InGameMap).GetField("hint", PrivateInstance).SetValue(map, plan);
            typeof(InGameMap).GetMethod("refreshPath", PrivateInstance).Invoke(map, null);
        }

        /// <summary>
        /// 今表示しているマップ（ステージ画面でなければ null）。GameView の非公開の map を読む
        /// </summary>
        private static InGameMap currentMap()
        {
            GameView view = UnityEngine.Object.FindAnyObjectByType<GameView>();
            if (view == null)
            {
                return null;
            }
            var map = typeof(GameView).GetField("map", PrivateInstance).GetValue(view) as InGameMap;
            return map != null && map.Path != null ? map : null;
        }

        /// <summary>
        /// 全ステージを指定の★で、もどらず（任意）でクリアにする
        /// </summary>
        /// <param name="_stars"></param>
        /// <param name="_isNoRewind"></param>
        private static void clearAllStages(int _stars, bool _isNoRewind)
        {
            int count = MapCsv.LoadList().Count;
            for (int i = 0; i < count; i++)
            {
                StageRecord.Save(i + 1, _stars, false, _isNoRewind);
            }
            SaveDataManager.Data.clearedCount = count;
            SaveDataManager.Data.stageIndex = Mathf.Max(0, count - 1);
            SaveDataManager.Save();
        }

        /// <summary>
        /// 全ステージの記録と進み具合を消す（最初のステージから）
        /// </summary>
        private static void unclearAllStages()
        {
            SaveDataManager.Data.stageRecords.Clear();
            SaveDataManager.Data.clearedCount = 0;
            SaveDataManager.Data.stageIndex = 0;
            SaveDataManager.Data.hintStage = 0;
            SaveDataManager.Save();
        }

        /// <summary>
        /// 持っているパグを全部仲間にする
        /// </summary>
        private static void addAllPugs()
        {
            foreach (CharacterData character in CharacterMaster.All)
            {
                if (!SaveDataManager.Data.pugs.Contains(character.Id))
                {
                    SaveDataManager.Data.pugs.Add(character.Id);
                }
            }
            SaveDataManager.Save();
        }

        /// <summary>
        /// パグを1匹だけ残す（おにわにも1匹だけ。連れていく子は男の子）
        /// </summary>
        private static void keepOnePug()
        {
            UserSaveData data = SaveDataManager.Data;
            int keep = data.pugs.Count > 0 ? data.pugs[0] : PugCollection.DefaultPlayerId;
            data.pugs.Clear();
            data.pugs.Add(keep);
            data.gardenPugs.Clear();
            data.gardenPugs.Add(keep);
            data.equippedPugId = PugCollection.DefaultPlayerId;
            SaveDataManager.Save();
        }

        /// <summary>
        /// デバッグマップを読み込んで、おさんぽとして遊ぶ（クリアしても冒険の進みは変わらない）
        /// </summary>
        private static void playDebugMap()
        {
            MapData map = loadDebugMap();
            if (map == null)
            {
                return;
            }
            pendingMap = map;
            GameSession.StartStroll();
            ViewManager.Instance.ToGameWithFade().Forget();
        }

        /// <summary>
        /// 保存したデバッグマップを読む（無ければ null）
        /// </summary>
        private static MapData loadDebugMap()
        {
            var listAsset = Resources.Load<TextAsset>(DebugMapFolder + "debug_map_list");
            var mapAsset = Resources.Load<TextAsset>(DebugMapFolder + "debug_map");
            if (listAsset == null || mapAsset == null)
            {
                Debug.LogError("[DebugTools] デバッグマップがありません（マップ編集の Save Debug で保存する）");
                return null;
            }
            System.Collections.Generic.List<MapInfo> list = MapCsv.ParseList(listAsset.text);
            return list.Count > 0 ? MapCsv.ParseMap(list[0], mapAsset.text) : null;
        }

        /// <summary>
        /// ほねを増やす
        /// </summary>
        /// <param name="_amount"></param>
        private static void addBones(int _amount)
        {
            UserSaveData data = SaveDataManager.Data;
            data.bones = (int)Math.Min((long)data.bones + _amount, int.MaxValue);
            SaveDataManager.Save();
        }
    }
}
#endif
