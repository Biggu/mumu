using System;
using System.Collections.Generic;
using System.IO;
using mumu;
using UnityEngine;

// 冒険のテスト用ステージ（21）を作る。
// テーマごとに 3 ステージ: チュートリアル（一本道、新しいギミックを何回か使う）→ ふつう → むずかしい（自動生成）。
// テーマが変わるたびにギミックが1つ増える。ステージ1（一本道、宝箱なし）は手で作ったものをそのまま使う。
// 前の 50 ステージ版は Program_50stages.cs.txt（参考用）。
// 使い方: dotnet run -c Release -- <Assets/mumu/Resources/MapData のパス>
public static class Program
{
    /// テーマごとの設定（テーマ ID、日本語名、新しいギミック、ふつう・むずかしいの難易度、チュートリアル名）
    class ThemePlan
    {
        public int ThemeId;
        public string Name;
        public string Gimmick;
        public int Normal;
        public int Hard;
        public string TutorialName;
    }

    static readonly ThemePlan[] Plans =
    {
        new ThemePlan { ThemeId = 1, Name = "そうげん", Gimmick = null, Normal = 3, Hard = 6, TutorialName = "はじめの いっぽ" },
        new ThemePlan { ThemeId = 2, Name = "なつ", Gimmick = "arrow", Normal = 5, Hard = 8, TutorialName = "やじるしを おぼえよう" },
        new ThemePlan { ThemeId = 3, Name = "ふゆ", Gimmick = "turn", Normal = 7, Hard = 10, TutorialName = "まがりかどを おぼえよう" },
        new ThemePlan { ThemeId = 4, Name = "あき", Gimmick = "straight", Normal = 9, Hard = 12, TutorialName = "まっすぐを おぼえよう" },
        new ThemePlan { ThemeId = 5, Name = "はる", Gimmick = "key", Normal = 11, Hard = 14, TutorialName = "カギと とびら" },
        new ThemePlan { ThemeId = 6, Name = "さくら", Gimmick = "warp", Normal = 13, Hard = 17, TutorialName = "ワープを おぼえよう" },
        new ThemePlan { ThemeId = 7, Name = "さばく", Gimmick = "crumble", Normal = 15, Hard = 20, TutorialName = "こわれる はし" },
    };

    static readonly HashSet<int> WaterThemes = new HashSet<int> { 2, 11, 12, 13 };
    static readonly eMapPattern[] LandPatterns = { eMapPattern.Square, eMapPattern.WideRect, eMapPattern.TallRect, eMapPattern.Circle, eMapPattern.Oval, eMapPattern.Hexagon, eMapPattern.Diamond, eMapPattern.Cross, eMapPattern.Donut, eMapPattern.LShape, eMapPattern.UShape, eMapPattern.River, eMapPattern.Lake };
    static readonly eMapPattern[] WaterPatterns = { eMapPattern.River, eMapPattern.Lake, eMapPattern.Coast, eMapPattern.Ponds, eMapPattern.Circle, eMapPattern.Donut };

    /// 盤面に対して経路が少なすぎる形は選び直す
    const double MinFillRate = 0.3;
    const int SeedAttempts = 60;

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
        var learned = new GimmickSettings();
        int id = 0;
        eMapPattern previous = (eMapPattern)(-1);

        foreach (ThemePlan plan in Plans)
        {
            learnGimmick(ref learned, plan.Gimmick);

            // 1つ目: チュートリアル（ステージ1は手で作ったものを使う）
            id++;
            if (id == 1)
            {
                first.Name = plan.TutorialName;
                first.ThemeId = plan.ThemeId;
                first.Difficulty = 1;
                list.Add(first);
                Console.WriteLine($"{id,2} tutorial (hand-made stage 1 kept)");
            }
            else
            {
                save(folder, buildTutorial(plan.Gimmick), id, plan.TutorialName, plan.ThemeId, 1, list, "tutorial");
            }

            // 2つ目・3つ目: ふつう・むずかしい（それまでに覚えたギミック全部、新しいギミックは必ず入れる）
            for (int step = 2; step <= 3; step++)
            {
                id++;
                int difficulty = step == 2 ? plan.Normal : plan.Hard;
                eMapPattern[] pool = WaterThemes.Contains(plan.ThemeId) ? WaterPatterns : LandPatterns;
                MapData best = null;
                for (int tries = 0; best == null && tries < 20; tries++)
                {
                    eMapPattern pattern;
                    do { pattern = pool[picker.Next(pool.Length)]; } while (pattern == previous);
                    for (int attempt = 0; attempt < SeedAttempts; attempt++)
                    {
                        var map = new MapData();
                        int seed = id * 1000 + tries * 100 + attempt;
                        var result = MapGenerator.Generate(map, difficulty, pattern, seed, false, learned);
                        if (!result.IsSuccess || result.Waypoints < MinFillRate * result.Width * result.Height || !hasGimmick(map, plan.Gimmick))
                        {
                            continue;
                        }
                        best = map;
                        previous = pattern;
                        Console.Write($"   [{pattern} seed {seed}] ");
                        break;
                    }
                }
                if (best == null)
                {
                    Console.WriteLine($"{id,2} FAILED to generate");
                    continue;
                }
                save(folder, best, id, $"{plan.Name} {step}", plan.ThemeId, difficulty, list, step == 2 ? "normal" : "hard");
            }
        }

        File.WriteAllText(listPath, MapCsv.WriteList(list), MapCsv.FileEncoding);
        Console.WriteLine($"done: {list.Count} stages");
    }

    /// 覚えたギミックを足す
    static void learnGimmick(ref GimmickSettings _settings, string _gimmick)
    {
        switch (_gimmick)
        {
            case "arrow": _settings.Arrow = true; break;
            case "turn": _settings.Turn = true; break;
            case "straight": _settings.Straight = true; break;
            case "key": _settings.Key = true; break;
            case "warp": _settings.Warp = true; break;
            case "crumble": _settings.Crumble = true; break;
        }
    }

    /// マップに新しいギミックが入っているか
    static bool hasGimmick(MapData _map, string _gimmick)
    {
        int Count(params eBlockSubType[] _types) { int n = 0; foreach (var t in _types) n += _map.CountSubType(t); return n; }
        switch (_gimmick)
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

        var counts = new List<string>();
        for (int y = 0; y < _map.height; y++)
            for (int x = 0; x < _map.width; x++)
                foreach (var t in _map.GetBlockSubTypes(x, y)) counts.Add(t.ToString());
        counts.Sort();
        var summary = new Dictionary<string, int>();
        foreach (var t in counts) { string k = t.StartsWith("Arrow") ? "Arrow" : t; summary[k] = summary.TryGetValue(k, out int n) ? n + 1 : 1; }
        var parts = new List<string>();
        foreach (var pair in summary) parts.Add($"{pair.Key}x{pair.Value}");
        Console.WriteLine($"{_id,2} {_kind,-8} {_name} lv{_difficulty} {_map.width}x{_map.height} wp{_map.GetMaxWaypointIndex() + 1} [{string.Join(" ", parts)}] {(ok ? "OK" : "NG " + (error ?? checkError))}");
    }

    /// チュートリアルの一本道（5 x 5 のくねくね: 横の通路 3 本を左右の端でつなぐ）に新しいギミックを並べる
    static MapData buildTutorial(string _gimmick)
    {
        const int W = 5, H = 5;
        var map = new MapData();
        map.Initialize(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                map.SetBlockType(x, y, BlockTypeUtility.Obstructs[(x * 7 + y * 3) % BlockTypeUtility.Obstructs.Length]);

        // 上の段 →、右端で下へ、中の段 ←、左端で下へ、下の段 →（ワープは上の段の右端から中の段の右端へ飛ぶので、右端の縦の通路はない）
        bool isWarp = _gimmick == "warp";
        var path = new List<Vector2Int>();
        for (int x = 0; x < W; x++) path.Add(new Vector2Int(x, 0));
        if (!isWarp) path.Add(new Vector2Int(W - 1, 1));
        for (int x = W - 1; x >= 0; x--) path.Add(new Vector2Int(x, 2));
        path.Add(new Vector2Int(0, 3));
        for (int x = 0; x < W; x++) path.Add(new Vector2Int(x, 4));

        foreach (var p in path) map.SetBlockType(p.x, p.y, eBlockType.Walkable);
        map.SetBlockType(path[0].x, path[0].y, eBlockType.Start);
        map.SetBlockType(path[path.Count - 1].x, path[path.Count - 1].y, eBlockType.Goal);
        for (int i = 1; i < path.Count - 1; i++) map.SetWaypointIndex(path[i].x, path[i].y, i - 1);

        Vector2Int Dir(int _x, int _y) { int i = path.IndexOf(new Vector2Int(_x, _y)); return path[i + 1] - path[i]; }
        void Add(int _x, int _y, eBlockSubType _type) => map.AddBlockSubType(_x, _y, _type);

        switch (_gimmick)
        {
            case "arrow":
                Add(2, 0, GimmickUtility.ToArrow(Dir(2, 0))); Add(2, 2, GimmickUtility.ToArrow(Dir(2, 2))); Add(2, 4, GimmickUtility.ToArrow(Dir(2, 4)));
                break;
            case "turn":
                Add(W - 1, 0, eBlockSubType.Turn); Add(W - 1, 2, eBlockSubType.Turn); Add(0, 2, eBlockSubType.Turn); Add(0, 4, eBlockSubType.Turn);
                break;
            case "straight":
                Add(2, 0, eBlockSubType.Straight); Add(W - 1, 1, eBlockSubType.Straight); Add(2, 2, eBlockSubType.Straight); Add(0, 3, eBlockSubType.Straight); Add(2, 4, eBlockSubType.Straight);
                break;
            case "key":
                Add(2, 0, eBlockSubType.KeyRed); Add(2, 2, eBlockSubType.GateRed);
                Add(3, 2, eBlockSubType.KeyBlue); Add(2, 4, eBlockSubType.GateBlue);
                break;
            case "warp":
                Add(W - 1, 0, eBlockSubType.WarpInRed); Add(W - 1, 2, eBlockSubType.WarpOutRed);
                break;
            case "crumble":
                Add(2, 0, eBlockSubType.Crumble); Add(2, 2, eBlockSubType.Crumble);
                break;
        }

        // 宝箱（木）を下の段に1つ
        Add(1, 4, eBlockSubType.Treasure1);
        return map;
    }
}
