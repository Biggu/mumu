using System;
using mumu;
public static class Program
{
    public static void Main()
    {
        int fail = 0, total = 0, minDist = int.MaxValue;
        foreach (eMapPattern pattern in Enum.GetValues(typeof(eMapPattern)))
        {
            var line = $"{pattern,-10}";
            foreach (int lv in new[] { 1, 10, 20, 30, 40, 50 })
            {
                double wpSum = 0, obSum = 0, frSum = 0; int okCount = 0; int moveable = 0; string size = "";
                for (int seed = 1; seed <= 5; seed++)
                {
                    var map = new MapData();
                    var r = MapGenerator.Generate(map, lv, pattern, seed, false);
                    total++;
                    var sp = new UnityEngine.Vector2Int(0, 0); var gp = sp; bool posOk = map.TryFindBlock(eBlockType.Start, out sp) && map.TryFindBlock(eBlockType.Goal, out gp) && Math.Abs(sp.x - gp.x) + Math.Abs(sp.y - gp.y) >= MapGenerator.MinStartGoalDistance && map.difficulty == lv; if (posOk) minDist = Math.Min(minDist, Math.Abs(sp.x - gp.x) + Math.Abs(sp.y - gp.y)); if (r.IsSuccess && posOk) okCount++; else fail++;
                    wpSum += r.Moveable > 0 ? (double)r.Waypoints / r.Moveable : 0;
                    obSum += r.Moveable > 0 ? (double)r.Obstructs / r.Moveable : 0; frSum += r.Moveable > 0 ? (double)r.Free / r.Moveable : 0;
                    moveable = r.Moveable; size = $"{r.Width}x{r.Height}";
                }
                line += $" | Lv{lv,2} {size,5} m{moveable,3} ok{okCount}/5 wp{wpSum / 5:P0} ob{obSum / 5:P0} fr{frSum / 5:P0}";
            }
            Console.WriteLine(line);
        }
        // gimmicks: every pattern/level/seed with all gimmicks must still validate
        int gFail = 0, gTotal = 0; var gSum = new GimmickCounts();
        var all = new GimmickSettings { Arrow = true, Turn = true, Straight = true, Key = true, Warp = true, Crumble = true };
        foreach (eMapPattern pattern in Enum.GetValues(typeof(eMapPattern)))
            foreach (int lv in new[] { 1, 10, 20, 30, 40, 50 })
                for (int seed = 1; seed <= 5; seed++)
                {
                    var map = new MapData();
                    var r = MapGenerator.Generate(map, lv, pattern, seed, false, all);
                    if (r.Gimmicks.Treasures != 1) { gFail++; Console.WriteLine($"NG treasure always 1: {pattern} lv{lv} s{seed} -> {r.Gimmicks.Treasures}"); }
                    gTotal++; if (!r.IsSuccess) { gFail++; map.ValidatePath(out string err); Console.WriteLine($"NG {pattern} lv{lv} s{seed}: {err}"); }
                    gSum.Arrows += r.Gimmicks.Arrows; gSum.Turns += r.Gimmicks.Turns; gSum.Straights += r.Gimmicks.Straights; gSum.Treasures += r.Gimmicks.Treasures; gSum.Keys += r.Gimmicks.Keys; gSum.Warps += r.Gimmicks.Warps; gSum.Crumbles += r.Gimmicks.Crumbles;
                }
        Console.WriteLine($"gimmick maps invalid: {gFail}/{gTotal}  placed total: {gSum}");        // play check: walking the designed path clears, and going against an arrow is refused
        int playFail = 0, arrowChecked = 0, arrowFail = 0;
        foreach (eMapPattern pattern in Enum.GetValues(typeof(eMapPattern)))
            for (int seed = 1; seed <= 5; seed++)
            {
                var map = new MapData();
                MapGenerator.Generate(map, 30, pattern, seed, false, all);
                var path = new PuzzlePath(map);
                var waypoints = map.GetWaypointPositions(out _);
                bool ok = true;
                foreach (var w in waypoints) { if (w == path.Head) continue; if (path.MoveTo(w) != eMoveResult.Added) { ok = false; break; } }
                map.TryFindBlock(eBlockType.Goal, out var goal);
                if (!ok || path.MoveTo(goal) != eMoveResult.Cleared) playFail++;
                // arrow: rewind to the cell before an arrow and try entering it from a wrong side is not possible directly,
                // so check leaving an arrow cell sideways
                var path2 = new PuzzlePath(map);
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != path2.Head) path2.MoveTo(waypoints[i]);
                    var route = GimmickUtility.GetRoute(map.GetBlockSubTypes(waypoints[i].x, waypoints[i].y));
                    if (!GimmickUtility.IsArrow(route)) continue;
                    var dir = GimmickUtility.GetArrowDirection(route);
                    var side = new UnityEngine.Vector2Int(dir.y, dir.x);
                    foreach (var s in new[] { side, new UnityEngine.Vector2Int(-side.x, -side.y) })
                    {
                        var target = waypoints[i] + s;
                        if (!map.IsInside(target.x, target.y) || !BlockTypeUtility.IsWalkable(map.GetBlockType(target.x, target.y)) || path2.IndexOf(target) >= 0) continue;
                        arrowChecked++;
                        if (path2.CanAdvanceTo(target)) arrowFail++;
                    }
                    break;
                }
            }
        Console.WriteLine($"play check fail: {playFail}  arrow side-exit checked: {arrowChecked} wrongly allowed: {arrowFail}");        // rule tests on small hand-made maps
        int ruleFail = 0;
        MapData Corridor(int w, int h) { var cm = new MapData(); cm.Initialize(w, h); for (int i = 0; i < cm.Count; i++) cm.blockTypes[i] = eBlockType.Obstruct1; return cm; }
        void Walk(MapData wm, params (int x, int y)[] cs) { foreach (var p in cs) wm.SetBlockType(p.x, p.y, eBlockType.Walkable); }
        void Check(bool ok, string name) { if (!ok) { ruleFail++; Console.WriteLine("RULE NG: " + name); } }
        UnityEngine.Vector2Int V(int x, int y) => new UnityEngine.Vector2Int(x, y);
        {   // gate: closed until the key is on the path
            var tm = Corridor(4, 2); Walk(tm, (1, 0), (2, 0), (0, 1), (1, 1));
            tm.SetBlockType(0, 0, eBlockType.Start); tm.SetBlockType(3, 0, eBlockType.Goal);
            tm.AddBlockSubType(1, 0, eBlockSubType.GateRed); tm.AddBlockSubType(0, 1, eBlockSubType.KeyRed);
            var p = new PuzzlePath(tm);
            Check(!p.CanAdvanceTo(V(1, 0)), "gate closed without key");
            Check(p.IsBlockedByGate(V(1, 0)), "gate reports blocked");
            p.MoveTo(V(0, 1)); p.MoveTo(V(1, 1));
            Check(p.CanAdvanceTo(V(1, 0)), "gate open with key");
        }
        {   // crumble: after stepping off, cannot rewind to it or before
            var tm = Corridor(5, 1); Walk(tm, (1, 0), (2, 0), (3, 0));
            tm.SetBlockType(0, 0, eBlockType.Start); tm.SetBlockType(4, 0, eBlockType.Goal);
            tm.AddBlockSubType(1, 0, eBlockSubType.Crumble);
            var p = new PuzzlePath(tm);
            p.MoveTo(V(1, 0));
            Check(p.LockedIndex == -1, "crumble not locked while standing on it");
            Check(p.MoveTo(V(0, 0)) == eMoveResult.Undone, "can rewind before leaving crumble");
            p.MoveTo(V(1, 0)); p.MoveTo(V(2, 0)); p.MoveTo(V(3, 0));
            Check(p.LockedIndex == 1, "crumble locked after leaving");
            Check(p.MoveTo(V(1, 0)) == eMoveResult.None && p.IsRewindLocked(V(1, 0)), "cannot rewind onto crumble");
            Check(p.MoveTo(V(0, 0)) == eMoveResult.None, "cannot rewind to start past crumble");
            Check(p.MoveTo(V(2, 0)) == eMoveResult.Undone, "can rewind after crumble");
        }
        {   // warp: one way, jumps to exit; entrance rewind goes before it
            var tm = Corridor(5, 1); Walk(tm, (1, 0), (3, 0));
            tm.SetBlockType(0, 0, eBlockType.Start); tm.SetBlockType(4, 0, eBlockType.Goal);
            tm.AddBlockSubType(1, 0, eBlockSubType.WarpInBlue); tm.AddBlockSubType(3, 0, eBlockSubType.WarpOutBlue);
            var p = new PuzzlePath(tm);
            p.MoveTo(V(1, 0));
            Check(p.Head == V(3, 0) && p.IsWarpArrival(2), "warp jumps to exit");
            Check(p.MoveTo(V(4, 0)) == eMoveResult.Cleared, "clear after warp");
            var p2 = new PuzzlePath(tm); p2.MoveTo(V(1, 0));
            Check(p2.MoveTo(V(1, 0)) == eMoveResult.Undone && p2.Head == V(0, 0), "rewind on entrance goes before it");
        }
        {   // warp inactive when the exit is already on the path
            var tm = Corridor(6, 1); Walk(tm, (1, 0), (2, 0), (3, 0), (4, 0));
            tm.SetBlockType(0, 0, eBlockType.Start); tm.SetBlockType(5, 0, eBlockType.Goal);
            tm.AddBlockSubType(1, 0, eBlockSubType.WarpOutRed); tm.AddBlockSubType(3, 0, eBlockSubType.WarpInRed);
            var p = new PuzzlePath(tm);
            p.MoveTo(V(1, 0)); p.MoveTo(V(2, 0)); p.MoveTo(V(3, 0));
            Check(p.Head == V(3, 0) && !p.IsWarpArrival(3), "no warp when exit already passed");
            Check(p.MoveTo(V(4, 0)) == eMoveResult.Added, "continue as a normal cell");
        }
        Console.WriteLine($"rule tests failed: {ruleFail}");        // plain generation (no gimmick options) must still place exactly one treasure, with valid tier distribution
        int plainBad = 0; var tierCount = new int[3];
        foreach (eMapPattern pattern in Enum.GetValues(typeof(eMapPattern)))
            for (int seed = 1; seed <= 20; seed++)
            {
                var map = new MapData();
                var r = MapGenerator.Generate(map, 10, pattern, seed, false);
                int found = 0;
                for (int t = 0; t < 3; t++) { int n = map.CountSubType(GimmickUtility.Treasures[t]); found += n; tierCount[t] += n; }
                if (found != 1 || !r.IsSuccess) plainBad++;
            }
        Console.WriteLine($"plain maps treasure wrong: {plainBad}  wood/silver/gold: {tierCount[0]}/{tierCount[1]}/{tierCount[2]}");        // validate checks: break a valid gimmick map on purpose and make sure ValidatePath reports it
        {
            int vFail = 0;
            void Expect(MapData vm, bool expectOk, string name)
            {
                bool ok = vm.ValidatePath(out string err);
                if (ok != expectOk) { vFail++; Console.WriteLine($"VALIDATE NG [{name}] expected {(expectOk ? "OK" : "error")} got {(ok ? "OK" : err)}"); }
                else Console.WriteLine($"  [{name}] -> {(ok ? "OK" : err)}");
            }
            MapData Fresh(GimmickSettings s, int seed) { var fm = new MapData(); MapGenerator.Generate(fm, 30, eMapPattern.Square, seed, false, s); return fm; }
            UnityEngine.Vector2Int Find(MapData fm, Func<eBlockSubType, bool> pred)
            {
                for (int y = 0; y < fm.height; y++) for (int x = 0; x < fm.width; x++) foreach (var st in fm.GetBlockSubTypes(x, y)) if (pred(st)) return new UnityEngine.Vector2Int(x, y);
                return new UnityEngine.Vector2Int(-1, -1);
            }
            var allOn = new GimmickSettings { Arrow = true, Turn = true, Straight = true, Key = true, Warp = true, Crumble = true };

            var m0 = Fresh(allOn, 3); Expect(m0, true, "generated map");

            var m1 = Fresh(allOn, 3); var ac = Find(m1, GimmickUtility.IsArrow); var arrow = GimmickUtility.GetRoute(m1.GetBlockSubTypes(ac.x, ac.y));
            m1.RemoveBlockSubType(ac.x, ac.y, arrow); var ad = GimmickUtility.GetArrowDirection(arrow); m1.AddBlockSubType(ac.x, ac.y, GimmickUtility.ToArrow(new UnityEngine.Vector2Int(-ad.x, -ad.y)));
            Expect(m1, false, "arrow flipped");

            var m2 = Fresh(allOn, 3); var t = Find(m2, st => st == eBlockSubType.Turn); m2.RemoveBlockSubType(t.x, t.y, eBlockSubType.Turn); m2.AddBlockSubType(t.x, t.y, eBlockSubType.Straight);
            Expect(m2, false, "straight mark on a corner");

            var m3 = Fresh(allOn, 3); var k = Find(m3, GimmickUtility.IsKey); var key = GimmickUtility.GetKey(m3.GetBlockSubTypes(k.x, k.y)); m3.RemoveBlockSubType(k.x, k.y, key);
            Expect(m3, false, "gate without key");

            var m4 = Fresh(allOn, 3); var k4 = Find(m4, GimmickUtility.IsKey); var key4 = GimmickUtility.GetKey(m4.GetBlockSubTypes(k4.x, k4.y)); var g4 = Find(m4, GimmickUtility.IsGate);
            int gi = m4.GetWaypointIndex(g4.x, g4.y); m4.RemoveBlockSubType(k4.x, k4.y, key4);
            var wp4 = m4.GetWaypointPositions(out _); var after = wp4[Math.Min(gi + 1, wp4.Length - 1)]; m4.AddBlockSubType(after.x, after.y, key4);
            Expect(m4, false, "key after gate");

            var m5 = Fresh(allOn, 3); var o = Find(m5, GimmickUtility.IsWarpOut); var outType = GimmickUtility.GetExclusive(m5.GetBlockSubTypes(o.x, o.y)); m5.RemoveBlockSubType(o.x, o.y, outType);
            Expect(m5, false, "warp without exit");

            var m6 = Fresh(allOn, 3); var obs = new UnityEngine.Vector2Int(-1, -1);
            for (int y = 0; y < m6.height && obs.x < 0; y++) for (int x = 0; x < m6.width && obs.x < 0; x++) if (BlockTypeUtility.IsObstruct(m6.GetBlockType(x, y))) obs = new UnityEngine.Vector2Int(x, y);
            m6.AddBlockSubType(obs.x, obs.y, eBlockSubType.Treasure1); Expect(m6, false, "gimmick on an obstacle");

            var m7 = Fresh(new GimmickSettings { Warp = true }, 5); var wi = Find(m7, GimmickUtility.IsWarpIn); int wii = m7.GetWaypointIndex(wi.x, wi.y);
            m7.RemoveWaypointsFrom(wii + 1); // path stops at the entrance: designed path no longer jumps to the exit
            Expect(m7, false, "path not using an active warp");

            var m8 = Fresh(allOn, 3); var c8 = Find(m8, st => st == eBlockSubType.Crumble); Expect(m8, true, "crumble (no rule to break)");
            Console.WriteLine($"validate checks failed: {vFail}");
        }        // crypto checks: round trip, tamper detection, random IV
        {
            string json = "{\"version\":1,\"bones\":120,\"pugs\":[1,46,150],\"name\":\"むぎ\"}";
            string a1 = SaveCrypto.Encrypt(json), a2 = SaveCrypto.Encrypt(json);
            bool roundTrip = SaveCrypto.TryDecrypt(a1, out string back) && back == json;
            char[] chars = a1.ToCharArray(); int mid = chars.Length / 2; chars[mid] = chars[mid] == 'A' ? 'B' : 'A';
            bool tamperRejected = !SaveCrypto.TryDecrypt(new string(chars), out _);
            bool plainRejected = !SaveCrypto.TryDecrypt(json, out _);
            bool randomIv = a1 != a2;
            Console.WriteLine($"crypto checks: roundTrip {roundTrip}, tamperRejected {tamperRejected}, plainRejected {plainRejected}, randomIv {randomIv}");
            Console.WriteLine($"  sample: {a1.Substring(0, 40)}...");
        }        Console.WriteLine($"invalid maps: {fail}/{total}  min start-goal distance: {minDist}");
        // determinism: same seed twice
        var a = new MapData(); var b = new MapData();
        MapGenerator.Generate(a, 10, eMapPattern.River, 42, false); MapGenerator.Generate(b, 10, eMapPattern.River, 42, false);
        bool same = true; for (int i = 0; i < a.Count; i++) if (a.blockTypes[i] != b.blockTypes[i] || a.blockSubTypes[i].waypointIndex != b.blockSubTypes[i].waypointIndex) same = false;
        Console.WriteLine($"same seed same map: {same}");
        // print one river map
        var m = new MapData(); var res = MapGenerator.Generate(m, 6, eMapPattern.Circle, 7, false);
        Console.WriteLine($"Circle Lv6 seed7: {res}");
        for (int y = 0; y < m.height; y++) { var s = ""; for (int x = 0; x < m.width; x++) { var t = m.GetBlockType(x, y); s += t switch { eBlockType.Empty => " .", eBlockType.Water => " ~", eBlockType.Bridge => " =", eBlockType.Start => " S", eBlockType.Goal => " G", eBlockType.Walkable => m.GetWaypointIndex(x, y) >= 0 ? " o" : " _", _ => " #" }; } Console.WriteLine(s); }
    }
}
