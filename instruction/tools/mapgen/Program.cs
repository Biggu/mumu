using System;
using System.Collections.Generic;
using System.IO;
using mumu;
using UnityEngine;

// 冒険のテストステージ（500）を作る。
// ・テーマは 10 ステージごとに変わり、1〜14 を繰り返す（1〜10 = そうげん、11〜20 = なつ、…、131〜140 = オフィス、141〜150 = そうげん）
// ・難易度は 10 ステージごとに 1 上がる（1〜10 = 1、…、491〜500 = 50）
// ・ステージ1は手で作ったもの（ギミックなし）。11 から 10 ステージごとに、新しいギミックを一本道で出す（131 まで。下の順）
// ・それ以外のステージは、それまでに覚えたギミックからランダムに使う（ひとつも使わない時もある）
// 前の 50 ステージ版は Program_50stages_v2.cs.txt（参考用）。
// 使い方: dotnet run -c Release -- <Assets/mumu/Resources/MapData のパス>
public static class Program
{
    const int StageCount = 500;
    /// テーマ・難易度・新しいギミックの間隔（ステージ数）
    const int StagesPerStep = 10;
    const int ThemeCount = 14;
    const int MaxDifficulty = 50;
    const string FirstName = "はじめの いっぽ";

    /// テーマ1〜14 の名前（テーマ ID = 番号）
    static readonly string[] ThemeNames = { "そうげん", "なつ", "ふゆ", "あき", "はる", "さくら", "さばく", "ケーキ", "プリン", "まち", "むら", "うみ", "こおり", "オフィス" };

    /// 新しいギミックの順（ステージ 11, 21, 31, …）
    static readonly string[] Schedule =
    {
        "arrow_up", "key_red", "warp_red", "arrow_down", "key_blue", "warp_blue", "turn",
        "arrow_left", "key_yellow", "warp_yellow", "straight", "arrow_right", "crumble",
    };

    /// 一本道チュートリアルの名前（ゲーム内の表示）
    static readonly Dictionary<string, string> TutorialNames = new Dictionary<string, string>
    {
        { "arrow_up", "うえに いく やじるし" },
        { "arrow_down", "したに いく やじるし" },
        { "arrow_left", "ひだりに いく やじるし" },
        { "arrow_right", "みぎに いく やじるし" },
        { "key_red", "あかい カギと とびら" },
        { "key_blue", "あおい カギと とびら" },
        { "key_yellow", "きいろい カギと とびら" },
        { "warp_red", "あかい ワープ" },
        { "warp_blue", "あおい ワープ" },
        { "warp_yellow", "きいろい ワープ" },
        { "turn", "まがりかどを おぼえよう" },
        { "straight", "まっすぐを おぼえよう" },
        { "crumble", "こわれる はし" },
    };

    static readonly HashSet<int> WaterThemes = new HashSet<int> { 2, 11, 12, 13 };
    static readonly eMapPattern[] LandPatterns = { eMapPattern.Square, eMapPattern.WideRect, eMapPattern.TallRect, eMapPattern.Circle, eMapPattern.Oval, eMapPattern.Hexagon, eMapPattern.Diamond, eMapPattern.Cross, eMapPattern.Donut, eMapPattern.LShape, eMapPattern.UShape, eMapPattern.River, eMapPattern.Lake };
    static readonly eMapPattern[] WaterPatterns = { eMapPattern.River, eMapPattern.Lake, eMapPattern.Coast, eMapPattern.Ponds, eMapPattern.Circle, eMapPattern.Donut };

    /// 盤面に対して経路が少なすぎる形は選び直す
    const double MinFillRate = 0.3;
    const int SeedAttempts = 60;
    const int PatternTries = 30;

    public static void Main(string[] args)
    {
        string folder = args[0];
        string mapsFolder = Path.Combine(folder, "maps");
        string listPath = Path.Combine(folder, "map_list.csv");
        List<MapInfo> existing = MapCsv.ParseList(File.ReadAllText(listPath));
        MapInfo first = existing.Find(_i => _i.Id == 1);
        if (first == null || !File.Exists(Path.Combine(mapsFolder, "map_000001.csv")))
        {
            Console.WriteLine("map_000001.csv (hand-made stage 1) is missing");
            return;
        }

        // 古いステージ（2 以降）を消す（.meta も）
        foreach (string file in Directory.GetFiles(mapsFolder, "map_*.csv*"))
        {
            if (!Path.GetFileName(file).StartsWith("map_000001.csv", StringComparison.Ordinal))
            {
                File.Delete(file);
            }
        }

        var list = new List<MapInfo>();
        var picker = new System.Random(20261120);
        var unlocked = new List<string>();
        eMapPattern previous = (eMapPattern)(-1);

        for (int stage = 1; stage <= StageCount; stage++)
        {
            int themeId = (stage - 1) / StagesPerStep % ThemeCount + 1;
            int difficulty = Math.Min(MaxDifficulty, (stage - 1) / StagesPerStep + 1);

            // ステージ1: 手で作ったもの（ギミックなし）
            if (stage == 1)
            {
                first.Name = FirstName;
                first.ThemeId = themeId;
                first.Difficulty = difficulty;
                list.Add(first);
                Console.WriteLine($"{stage,3} tutorial (hand-made stage 1 kept)");
                continue;
            }

            // 11, 21, 31, …: 新しいギミックの一本道チュートリアル（順番が残っている間だけ）
            int scheduleIndex = stage / StagesPerStep - 1;
            if (stage % StagesPerStep == 1 && scheduleIndex < Schedule.Length)
            {
                string piece = Schedule[scheduleIndex];
                unlocked.Add(piece);
                save(folder, buildTutorial(piece), stage, TutorialNames[piece], themeId, difficulty, list, "tutorial");
                continue;
            }

            // それ以外: 覚えたギミックからランダムに使う（ひとつも使わない時もある）
            GimmickSettings settings = pickSettings(unlocked, picker);
            MapData best = null;
            for (int tries = 0; best == null && tries < PatternTries; tries++)
            {
                eMapPattern pattern;
                do { pattern = pickPattern(themeId, picker); } while (pattern == previous && LandPatterns.Length > 1);
                for (int attempt = 0; attempt < SeedAttempts; attempt++)
                {
                    var map = new MapData();
                    int seed = stage * 100000 + tries * 1000 + attempt;
                    var result = MapGenerator.Generate(map, difficulty, pattern, seed, false, settings);
                    if (!result.IsSuccess || result.Waypoints < MinFillRate * result.Width * result.Height || !hasAnyGimmick(map, unlocked))
                    {
                        continue;
                    }
                    best = map;
                    previous = pattern;
                    break;
                }
            }
            if (best == null)
            {
                Console.WriteLine($"{stage,3} FAILED to generate");
                continue;
            }
            save(folder, best, stage, $"{ThemeNames[themeId - 1]} {stage}", themeId, difficulty, list, "normal");
        }

        File.WriteAllText(listPath, MapCsv.WriteList(list), MapCsv.FileEncoding);
        playCheck(folder, list);
        Console.WriteLine($"done: {list.Count} stages");
    }

    /// 書き出したマップを読み直して、設計の経路をたどるとゴールまでクリアできるか（ゲームと同じ PuzzlePath で確認）
    static void playCheck(string _folder, List<MapInfo> _list)
    {
        int fail = 0, starFail = 0, perfectFail = 0;
        foreach (MapInfo info in _list)
        {
            string text = File.ReadAllText(Path.Combine(_folder, "maps", $"map_{info.Id:000000}.csv"));
            MapData map = MapCsv.ParseMap(info, text);
            var path = new PuzzlePath(map);
            Vector2Int[] waypoints = map.GetWaypointPositions(out _);
            bool ok = true;
            foreach (Vector2Int w in waypoints)
            {
                if (w == path.Head)
                {
                    continue;
                }
                if (path.MoveTo(w) != eMoveResult.Added)
                {
                    ok = false;
                    break;
                }
            }
            map.TryFindBlock(eBlockType.Goal, out Vector2Int goal);
            if (!ok || path.MoveTo(goal) != eMoveResult.Cleared)
            {
                fail++;
                Console.WriteLine($"play check FAIL: stage {info.Id}");
                continue;
            }
            if (path.StarCount < 3)
            {
                starFail++;
                Console.WriteLine($"star check FAIL: stage {info.Id} stars {path.StarCount}");
            }
            if (!path.IsPerfect)
            {
                perfectFail++;
            }
        }
        Console.WriteLine($"play check: {fail} fail / {_list.Count} stages");
        Console.WriteLine($"3 stars: {_list.Count - starFail} / {_list.Count}   PERFECT: {_list.Count - perfectFail} / {_list.Count}");
    }

    /// テーマに合う形をひとつ選ぶ
    static eMapPattern pickPattern(int _themeId, System.Random _picker)
    {
        eMapPattern[] pool = WaterThemes.Contains(_themeId) ? WaterPatterns : LandPatterns;
        return pool[_picker.Next(pool.Length)];
    }

    /// 覚えたギミックから、ランダムに使うものを選ぶ（覚えたものがあれば、少なくとも1つは選ぶ）
    static GimmickSettings pickSettings(List<string> _unlocked, System.Random _picker)
    {
        var settings = new GimmickSettings();
        if (_unlocked.Count == 0)
        {
            return settings;
        }
        var chosen = new List<string>();
        foreach (string piece in _unlocked)
        {
            if (_picker.Next(2) == 1)
            {
                chosen.Add(piece);
            }
        }
        if (chosen.Count == 0)
        {
            chosen.Add(_unlocked[_picker.Next(_unlocked.Count)]);
        }
        foreach (string piece in chosen)
        {
            learnGimmick(ref settings, typeOf(piece));
        }
        return settings;
    }

    /// ピース名（arrow_up など）からギミックの種類（arrow など）
    static string typeOf(string _piece)
    {
        return _piece.Split('_')[0];
    }

    /// 覚えたギミックの種類を足す
    static void learnGimmick(ref GimmickSettings _settings, string _type)
    {
        switch (_type)
        {
            case "arrow": _settings.Arrow = true; break;
            case "turn": _settings.Turn = true; break;
            case "straight": _settings.Straight = true; break;
            case "key": _settings.Key = true; break;
            case "warp": _settings.Warp = true; break;
            case "crumble": _settings.Crumble = true; break;
        }
    }

    /// 覚えたギミックのどれかが、マップに入っているか（覚えたものがなければ true）
    static bool hasAnyGimmick(MapData _map, List<string> _unlocked)
    {
        if (_unlocked.Count == 0)
        {
            return true;
        }
        foreach (string piece in _unlocked)
        {
            if (hasGimmick(_map, typeOf(piece)))
            {
                return true;
            }
        }
        return false;
    }

    /// マップに指定の種類のギミックが入っているか
    static bool hasGimmick(MapData _map, string _type)
    {
        int Count(params eBlockSubType[] _types) { int n = 0; foreach (var t in _types) n += _map.CountSubType(t); return n; }
        switch (_type)
        {
            case "arrow": return Count(GimmickUtility.Arrows) > 0;
            case "turn": return Count(eBlockSubType.Turn) > 0;
            case "straight": return Count(eBlockSubType.Straight) > 0;
            case "key": return Count(GimmickUtility.Gates) > 0;
            case "warp": return Count(GimmickUtility.WarpIns) > 0;
            case "crumble": return Count(eBlockSubType.Crumble) > 0;
            default: return true;
        }
    }

    /// 検証して書き出す（CSV に書いて読み直しても有効か）
    static void save(string _folder, MapData _map, int _id, string _name, int _themeId, int _difficulty, List<MapInfo> _list, string _kind)
    {
        _map.id = _id;
        _map.mapName = _name;
        _map.themeId = _themeId;
        _map.difficulty = _difficulty;
        bool ok = _map.ValidatePath(out string error);
        MapInfo info = MapCsv.ToInfo(_map);
        string text = MapCsv.WriteMap(_map);
        ok &= MapCsv.ParseMap(info, text).ValidatePath(out string checkError);
        File.WriteAllText(Path.Combine(_folder, "maps", $"map_{_id:000000}.csv"), text, MapCsv.FileEncoding);
        _list.Add(info);

        // 1行だけ表示（500 ステージ分の表示を短くするため、チュートリアルと NG だけ詳しく出す）
        if (_kind == "tutorial" || !ok)
        {
            Console.WriteLine($"{_id,3} {_kind,-8} {_name} lv{_difficulty} {_map.width}x{_map.height} {(ok ? "OK" : "NG " + (error ?? checkError))}");
        }
        else if (_id % 50 == 0)
        {
            Console.WriteLine($"{_id,3} ... done ({_name})");
        }
    }

    /// 一本道のチュートリアル（5 x 5）。ピース名（arrow_up・key_red など）に合わせて並べる。
    /// 横のくねくね（右・左に進む）と、縦のくねくね（上・下に進む）の2つを使い分ける
    static MapData buildTutorial(string _piece)
    {
        const int W = 5, H = 5;
        bool vertical = _piece == "arrow_up" || _piece == "arrow_down";
        bool warp = typeOf(_piece) == "warp";

        var path = new List<Vector2Int>();
        if (vertical)
        {
            // 左の列を下へ、(1,4) で右へ、次の列を上へ、(3,0) で右へ、右の列を下へ
            for (int y = 0; y < H; y++) path.Add(new Vector2Int(0, y));
            path.Add(new Vector2Int(1, H - 1));
            for (int y = H - 1; y >= 0; y--) path.Add(new Vector2Int(2, y));
            path.Add(new Vector2Int(3, 0));
            for (int y = 0; y < H; y++) path.Add(new Vector2Int(4, y));
        }
        else
        {
            // 上の段 →、右端で下へ、中の段 ←、左端で下へ、下の段 →（ワープは右端の縦の通路を使わない）
            for (int x = 0; x < W; x++) path.Add(new Vector2Int(x, 0));
            if (!warp) path.Add(new Vector2Int(W - 1, 1));
            for (int x = W - 1; x >= 0; x--) path.Add(new Vector2Int(x, 2));
            path.Add(new Vector2Int(0, 3));
            for (int x = 0; x < W; x++) path.Add(new Vector2Int(x, 4));
        }

        var map = new MapData();
        map.Initialize(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                map.SetBlockType(x, y, BlockTypeUtility.Obstructs[(x * 7 + y * 3) % BlockTypeUtility.Obstructs.Length]);

        foreach (var p in path) map.SetBlockType(p.x, p.y, eBlockType.Walkable);
        map.SetBlockType(path[0].x, path[0].y, eBlockType.Start);
        map.SetBlockType(path[path.Count - 1].x, path[path.Count - 1].y, eBlockType.Goal);
        for (int i = 1; i < path.Count - 1; i++) map.SetWaypointIndex(path[i].x, path[i].y, i - 1);

        Vector2Int Dir(int _x, int _y) { int i = path.IndexOf(new Vector2Int(_x, _y)); return path[i + 1] - path[i]; }
        void Add(int _x, int _y, eBlockSubType _type) => map.AddBlockSubType(_x, _y, _type);

        switch (_piece)
        {
            case "arrow_right":
                foreach (int x in new[] { 1, 2, 3 }) Add(x, 0, GimmickUtility.ToArrow(Dir(x, 0)));
                break;
            case "arrow_left":
                foreach (int x in new[] { 1, 2, 3 }) Add(x, 2, GimmickUtility.ToArrow(Dir(x, 2)));
                break;
            case "arrow_down":
                foreach (int y in new[] { 1, 2, 3 }) Add(0, y, GimmickUtility.ToArrow(Dir(0, y)));
                break;
            case "arrow_up":
                foreach (int y in new[] { 1, 2, 3 }) Add(2, y, GimmickUtility.ToArrow(Dir(2, y)));
                break;
            case "key_red": case "key_blue": case "key_yellow":
                Add(2, 0, keyOf(_piece)); Add(2, 2, gateOf(_piece));
                break;
            case "warp_red": case "warp_blue": case "warp_yellow":
                Add(W - 1, 0, warpInOf(_piece)); Add(W - 1, 2, warpOutOf(_piece));
                break;
            case "turn":
                Add(W - 1, 0, eBlockSubType.Turn); Add(W - 1, 2, eBlockSubType.Turn); Add(0, 2, eBlockSubType.Turn); Add(0, 4, eBlockSubType.Turn);
                break;
            case "straight":
                Add(2, 0, eBlockSubType.Straight); Add(W - 1, 1, eBlockSubType.Straight); Add(2, 2, eBlockSubType.Straight); Add(0, 3, eBlockSubType.Straight); Add(2, 4, eBlockSubType.Straight);
                break;
            case "crumble":
                Add(2, 0, eBlockSubType.Crumble); Add(2, 2, eBlockSubType.Crumble);
                break;
        }

        // 宝箱（木）を下の段に1つ（縦の道では真ん中の下の連結点）
        Add(1, H - 1, eBlockSubType.Treasure1);
        return map;
    }

    /// 色の名前（red・blue・yellow）ごとの鍵・扉・ワープ
    static eBlockSubType keyOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.KeyRed : colorOf(_piece) == "blue" ? eBlockSubType.KeyBlue : eBlockSubType.KeyYellow;
    static eBlockSubType gateOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.GateRed : colorOf(_piece) == "blue" ? eBlockSubType.GateBlue : eBlockSubType.GateYellow;
    static eBlockSubType warpInOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.WarpInRed : colorOf(_piece) == "blue" ? eBlockSubType.WarpInBlue : eBlockSubType.WarpInYellow;
    static eBlockSubType warpOutOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.WarpOutRed : colorOf(_piece) == "blue" ? eBlockSubType.WarpOutBlue : eBlockSubType.WarpOutYellow;

    /// ピース名の末尾の色（red・blue・yellow）
    static string colorOf(string _piece)
    {
        return _piece.Substring(_piece.LastIndexOf('_') + 1);
    }
}
