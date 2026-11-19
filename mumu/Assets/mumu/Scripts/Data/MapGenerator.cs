using System;
using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 自動生成するマップの形
    /// </summary>
    public enum eMapPattern
    {
        Square,
        WideRect,
        TallRect,
        Circle,
        Oval,
        Hexagon,
        Diamond,
        Cross,
        Donut,
        LShape,
        UShape,
        SShape,
        River,
        TwoRivers,
        Lake,
        Islands,
        Coast,
        Checker,
        Ponds,
    }

    /// <summary>
    /// 難易度・形・シードからマップを生成（同じ引数なら同じマップ）
    /// </summary>
    public static class MapGenerator
    {
        public const int MinDifficulty = 1;
        public const int MaxDifficulty = 50;

        /// <summary>難易度カーブの基準レベル（この間は線形）</summary>
        private static readonly int[] CurveLevels = { 1, 20, 40, 50 };
        /// <summary>基準サイズ</summary>
        private static readonly float[] SizeCurve = { 5f, 14f, 18f, 20f };
        /// <summary>通れるマスのうち経路にする割合（上がるほど難しい）</summary>
        private static readonly float[] WaypointCurve = { 0.6f, 0.8f, 0.87f, 0.9f };
        /// <summary>通れるマスのうち自由に通れるマスの割合（Lv40 以降は 0）</summary>
        private static readonly float[] FreeCurve = { 0.1f, 0.02f, 0f, 0f };
        /// <summary>Goal 解放に必要な手数（経路数に対する割合）</summary>
        private static readonly float[] GoalUnlockCurve = { 0.6f, 0.72f, 0.84f, 0.9f };

        private const int MinSize = 5;
        /// <summary>Start と Goal の最低距離（間に2マス以上）</summary>
        public const int MinStartGoalDistance = 3;
        /// <summary>経路を作る回数（目標の長さに一番近いものを使う）</summary>
        private const int PathAttempts = 12;
        /// <summary>形の端を削る・盛る割合</summary>
        private const double EdgeErodeRate = 0.12;
        private const double EdgeGrowRate = 0.06;
        /// <summary>内側に穴を開ける割合</summary>
        private const double HoleRate = 0.03;

        /// <summary>
        /// 生成前のマスの種類
        /// </summary>
        private enum eCell
        {
            Empty,
            Land,
            Water,
            Bridge,
        }

        /// <summary>
        /// 生成結果
        /// </summary>
        public struct Result
        {
            /// <summary>置いたギミックの数</summary>
            public GimmickCounts Gimmicks;
            public bool IsSuccess;
            public int Width;
            public int Height;
            /// <summary>通れるマス（陸 + 橋）</summary>
            public int Moveable;
            public int Waypoints;
            public int Obstructs;
            public int Free;
            public int Bridges;

            /// <summary>
            /// ログ用の文字列
            /// </summary>
            public override string ToString()
            {
                string gimmicks = Gimmicks.Total > 0 ? $"  gimmick {Gimmicks}" : "";
                return $"{Width}x{Height} moveable {Moveable}: waypoint {Waypoints}, obstruct {Obstructs}, free {Free} (bridge {Bridges}){gimmicks}";
            }
        }

        /// <summary>
        /// 難易度カーブの値（基準レベルの間は線形）
        /// </summary>
        /// <param name="_difficulty"></param>
        /// <param name="_values"></param>
        private static float evaluate(int _difficulty, float[] _values)
        {
            int level = Mathf.Clamp(_difficulty, MinDifficulty, MaxDifficulty);
            for (int i = 1; i < CurveLevels.Length; i++)
            {
                if (level <= CurveLevels[i])
                {
                    float t = (level - CurveLevels[i - 1]) / (float)(CurveLevels[i] - CurveLevels[i - 1]);
                    return Mathf.Lerp(_values[i - 1], _values[i], t);
                }
            }
            return _values[_values.Length - 1];
        }

        /// <summary>
        /// 難易度ごとの基準サイズ（細い形は経路を通せるよう最低サイズを上げる）
        /// </summary>
        /// <param name="_difficulty"></param>
        /// <param name="_pattern"></param>
        public static int GetSize(int _difficulty, eMapPattern _pattern)
        {
            int size = Mathf.RoundToInt(evaluate(_difficulty, SizeCurve));
            return Mathf.Max(size, getMinSize(_pattern));
        }

        /// <summary>
        /// Goal 解放に必要な手数の割合（経路数に対して）
        /// </summary>
        /// <param name="_difficulty"></param>
        public static float GetGoalUnlockRate(int _difficulty) => evaluate(_difficulty, GoalUnlockCurve);

        /// <summary>
        /// 形ごとの最低サイズ
        /// </summary>
        /// <param name="_pattern"></param>
        private static int getMinSize(eMapPattern _pattern)
        {
            switch (_pattern)
            {
                case eMapPattern.SShape:
                case eMapPattern.TwoRivers:
                case eMapPattern.Checker:
                    return 8;
                case eMapPattern.Cross:
                case eMapPattern.Islands:
                    return 7;
                case eMapPattern.Diamond:
                case eMapPattern.Donut:
                case eMapPattern.UShape:
                case eMapPattern.River:
                case eMapPattern.Lake:
                case eMapPattern.Coast:
                    return 6;
                default:
                    return MinSize;
            }
        }

        /// <summary>
        /// 通れるマスのうち経路にする割合
        /// </summary>
        /// <param name="_difficulty"></param>
        public static float GetWaypointRate(int _difficulty) => evaluate(_difficulty, WaypointCurve);

        /// <summary>
        /// 通れるマスのうち通れないブロックにする割合（経路・自由マスの残り）
        /// </summary>
        /// <param name="_difficulty"></param>
        public static float GetObstructRate(int _difficulty) => Mathf.Max(0f, 1f - GetWaypointRate(_difficulty) - evaluate(_difficulty, FreeCurve));

        /// <summary>
        /// 形ごとの縦横のマス数
        /// </summary>
        /// <param name="_pattern"></param>
        /// <param name="_size"></param>
        public static Vector2Int GetDimensions(eMapPattern _pattern, int _size)
        {
            switch (_pattern)
            {
                case eMapPattern.WideRect: return new Vector2Int(Mathf.RoundToInt(_size * 1.4f), Mathf.Max(3, Mathf.RoundToInt(_size * 0.6f)));
                case eMapPattern.TallRect: return new Vector2Int(Mathf.Max(3, Mathf.RoundToInt(_size * 0.6f)), Mathf.RoundToInt(_size * 1.4f));
                case eMapPattern.Oval: return new Vector2Int(Mathf.RoundToInt(_size * 1.3f), Mathf.Max(3, Mathf.RoundToInt(_size * 0.8f)));
                default: return new Vector2Int(_size, _size);
            }
        }

        /// <summary>
        /// マップを生成して MapData に書き込む（ID・名前・テーマは変えない）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_difficulty"></param>
        /// <param name="_pattern"></param>
        /// <param name="_seed"></param>
        /// <param name="_isLogEnabled"></param>
        public static Result Generate(MapData _mapData, int _difficulty, eMapPattern _pattern, int _seed, bool _isLogEnabled = true, GimmickSettings _gimmicks = default)
        {
            var random = new System.Random(_seed);
            Vector2Int size = GetDimensions(_pattern, GetSize(_difficulty, _pattern));
            int width = size.x;
            int height = size.y;

            eCell[,] cells = createShape(_pattern, width, height, random);
            vary(cells, outsideOf(_pattern), random);
            int bridges = connect(cells);

            var moveable = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (isMoveable(cells[x, y]))
                    {
                        moveable.Add(new Vector2Int(x, y));
                    }
                }
            }

            var result = new Result { Width = width, Height = height, Moveable = moveable.Count, Bridges = bridges };
            // Start・Goal は経路の両端（Warnsdorff 法で長い経路を作り、端を決める）
            List<Vector2Int> path = createPath(cells, moveable, Mathf.CeilToInt(moveable.Count * GetWaypointRate(_difficulty)), random);

            write(_mapData, cells, path);
            _mapData.difficulty = Mathf.Clamp(_difficulty, MinDifficulty, MaxDifficulty);
            result.Waypoints = Mathf.Max(0, path.Count - 2);

            // 自由マスは決めた割合だけ残し、経路が足りない分も含めて残りは通れないブロック
            int freeCount = Mathf.RoundToInt(moveable.Count * evaluate(_difficulty, FreeCurve));
            result.Obstructs = placeObstructs(_mapData, cells, path, Mathf.Max(0, moveable.Count - path.Count - freeCount), random);
            result.Free = result.Moveable - path.Count - result.Obstructs;
            // ギミックは経路を決めた後に、設計した経路に合うマスだけへ置く（seed を変えて経路の結果は変えない）
            // 宝箱は必ず1つ置くので、ギミックの指定がなくても置く
            {
                result.Gimmicks = PlaceGimmicks(_mapData, GimmickCounts.FromPath(result.Waypoints, _gimmicks), new System.Random(_seed ^ GimmickSeedSalt));
                // ワープで飛ばしたマスの分、経路は短くなる
                result.Waypoints = _mapData.GetMaxWaypointIndex() + 1;
            }
            result.IsSuccess = _mapData.ValidatePath(out string error);

            if (_isLogEnabled)
            {
                string message = $"[MapGenerator] Lv{_difficulty} {_pattern} seed {_seed}: {result}";
                if (result.IsSuccess)
                {
                    Debug.Log(message);
                }
                else
                {
                    Debug.LogWarning($"{message} / invalid: {error}");
                }
            }
            return result;
        }

        // ---------- 形 ----------

        /// <summary>
        /// 形の外側を何にするか（水がある形は水、それ以外は何もない）
        /// </summary>
        /// <param name="_pattern"></param>
        private static eCell outsideOf(eMapPattern _pattern)
        {
            switch (_pattern)
            {
                case eMapPattern.Donut:
                case eMapPattern.UShape:
                case eMapPattern.SShape:
                case eMapPattern.River:
                case eMapPattern.TwoRivers:
                case eMapPattern.Lake:
                case eMapPattern.Islands:
                case eMapPattern.Coast:
                case eMapPattern.Checker:
                case eMapPattern.Ponds:
                    return eCell.Water;
                default:
                    return eCell.Empty;
            }
        }

        /// <summary>
        /// 形を作る（座標は -1〜1 に正規化して判定）
        /// </summary>
        /// <param name="_pattern"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_random"></param>
        private static eCell[,] createShape(eMapPattern _pattern, int _width, int _height, System.Random _random)
        {
            var cells = new eCell[_width, _height];
            eCell outside = outsideOf(_pattern);

            // シードごとに形を少し変える
            double phase = _random.NextDouble() * Math.PI * 2;
            double wave = 0.15 + _random.NextDouble() * 0.15;
            double scale = 0.9 + _random.NextDouble() * 0.1;
            double offsetX = (_random.NextDouble() - 0.5) * 0.3;
            double offsetY = (_random.NextDouble() - 0.5) * 0.3;
            List<double[]> circles = createCircles(_pattern, _width, _random);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    double nx = (x + 0.5) / _width * 2 - 1;
                    double ny = (y + 0.5) / _height * 2 - 1;
                    bool isLand = isLandAt(_pattern, nx, ny, phase, wave, scale, offsetX, offsetY, circles);
                    cells[x, y] = isLand ? eCell.Land : outside;
                }
            }
            return cells;
        }

        /// <summary>
        /// 島・池の円（中心 x, y と半径）
        /// </summary>
        /// <param name="_pattern"></param>
        /// <param name="_size"></param>
        /// <param name="_random"></param>
        private static List<double[]> createCircles(eMapPattern _pattern, int _size, System.Random _random)
        {
            var circles = new List<double[]>();
            int count = _pattern == eMapPattern.Islands ? 3 + _random.Next(3) : (_pattern == eMapPattern.Ponds ? Mathf.Max(2, _size / 3) : 0);
            for (int i = 0; i < count; i++)
            {
                double radius = _pattern == eMapPattern.Islands ? 0.3 + _random.NextDouble() * 0.15 : 0.1 + _random.NextDouble() * 0.08;
                circles.Add(new[] { (_random.NextDouble() * 2 - 1) * 0.7, (_random.NextDouble() * 2 - 1) * 0.7, radius });
            }
            return circles;
        }

        /// <summary>
        /// 正規化座標が陸か
        /// </summary>
        private static bool isLandAt(eMapPattern _pattern, double _x, double _y, double _phase, double _wave, double _scale,
            double _offsetX, double _offsetY, List<double[]> _circles)
        {
            double ax = Math.Abs(_x);
            double ay = Math.Abs(_y);
            double r = Math.Sqrt(_x * _x + _y * _y);

            switch (_pattern)
            {
                case eMapPattern.Square:
                case eMapPattern.WideRect:
                case eMapPattern.TallRect:
                    return true;
                case eMapPattern.Circle:
                case eMapPattern.Oval:
                    return r <= 0.98 * _scale;
                case eMapPattern.Hexagon:
                    return ay <= 0.92 * _scale && ax * 0.87 + ay * 0.5 <= 0.92 * _scale;
                case eMapPattern.Diamond:
                    return ax + ay <= 1.05 * _scale;
                case eMapPattern.Cross:
                    return ax <= 0.36 || ay <= 0.36;
                case eMapPattern.Donut:
                    return r <= 0.98 * _scale && r >= 0.38;
                case eMapPattern.LShape:
                    return _x <= -0.15 + _offsetX || _y >= 0.15 + _offsetY;
                case eMapPattern.UShape:
                    return _x <= -0.35 || _x >= 0.35 || _y >= 0.35;
                case eMapPattern.SShape:
                    return _y <= -0.5 || ay <= 0.18 || _y >= 0.5 || (_x <= -0.45 && _y < 0) || (_x >= 0.45 && _y > 0);
                case eMapPattern.River:
                    return Math.Abs(_x - _offsetX - Math.Sin(_y * Math.PI + _phase) * _wave) > 0.2;
                case eMapPattern.TwoRivers:
                    double curve = Math.Sin(_y * Math.PI + _phase) * _wave * 0.6;
                    return Math.Abs(_x + 0.45 - curve) > 0.14 && Math.Abs(_x - 0.45 - curve) > 0.14;
                case eMapPattern.Lake:
                    double lx = (_x - _offsetX) / 0.5, ly = (_y - _offsetY) / 0.42;
                    return lx * lx + ly * ly > 1;
                case eMapPattern.Islands:
                    foreach (double[] c in _circles) if (dist(_x, _y, c) <= c[2]) return true;
                    return false;
                case eMapPattern.Coast:
                    return _x <= 0.2 + Math.Sin(_y * Math.PI * 1.5 + _phase) * _wave;
                case eMapPattern.Checker:
                    double fx = frac((_x + 1) * 1.5 + 0.1), fy = frac((_y + 1) * 1.5 + 0.1);
                    return fx < 0.72 && fy < 0.72;
                case eMapPattern.Ponds:
                    foreach (double[] c in _circles) if (dist(_x, _y, c) <= c[2]) return false;
                    return true;
                default:
                    return true;
            }
        }

        private static double dist(double _x, double _y, double[] _circle)
        {
            double dx = _x - _circle[0], dy = _y - _circle[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double frac(double _value) => _value - Math.Floor(_value);

        /// <summary>
        /// 端を削る・盛る、内側に穴を開ける（シードで形を変える）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_outside"></param>
        /// <param name="_random"></param>
        private static void vary(eCell[,] _cells, eCell _outside, System.Random _random)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            var next = (eCell[,])_cells.Clone();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isLand = _cells[x, y] == eCell.Land;
                    bool isEdge = countLandNeighbors(_cells, x, y) < 4 || x == 0 || y == 0 || x == width - 1 || y == height - 1;

                    if (isLand && isEdge && _random.NextDouble() < EdgeErodeRate)
                    {
                        next[x, y] = _outside;
                    }
                    else if (isLand && !isEdge && _random.NextDouble() < HoleRate)
                    {
                        next[x, y] = _outside;
                    }
                    else if (!isLand && countLandNeighbors(_cells, x, y) > 0 && _random.NextDouble() < EdgeGrowRate)
                    {
                        next[x, y] = eCell.Land;
                    }
                }
            }
            Array.Copy(next, _cells, next.Length);
        }

        private static int countLandNeighbors(eCell[,] _cells, int _x, int _y)
        {
            int count = 0;
            foreach (Vector2Int d in Directions)
            {
                int x = _x + d.x, y = _y + d.y;
                if (isInside(_cells, x, y) && _cells[x, y] == eCell.Land) count++;
            }
            return count;
        }

        // ---------- 橋 ----------

        private static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private static bool isInside(eCell[,] _cells, int _x, int _y) => _x >= 0 && _y >= 0 && _x < _cells.GetLength(0) && _y < _cells.GetLength(1);

        private static bool isMoveable(eCell _cell) => _cell == eCell.Land || _cell == eCell.Bridge;

        /// <summary>
        /// 離れた陸を最短の橋でつなぐ（橋の数を返す）
        /// </summary>
        /// <param name="_cells"></param>
        private static int connect(eCell[,] _cells)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            int bridges = 0;

            while (true)
            {
                int[,] component = labelComponents(_cells, out int count, out int largest);
                if (count <= 1)
                {
                    return bridges;
                }

                // 一番大きい陸から、陸以外を通る距離が最短の別の陸を探す（0-1 BFS）
                var cost = new int[width, height];
                var from = new Vector2Int[width, height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) cost[x, y] = int.MaxValue;

                var deque = new LinkedList<Vector2Int>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (component[x, y] == largest)
                        {
                            cost[x, y] = 0;
                            deque.AddLast(new Vector2Int(x, y));
                        }
                    }
                }

                Vector2Int target = new Vector2Int(-1, -1);
                while (deque.Count > 0)
                {
                    Vector2Int current = deque.First.Value;
                    deque.RemoveFirst();
                    if (component[current.x, current.y] >= 0 && component[current.x, current.y] != largest)
                    {
                        target = current;
                        break;
                    }

                    foreach (Vector2Int d in Directions)
                    {
                        Vector2Int next = current + d;
                        if (!isInside(_cells, next.x, next.y))
                        {
                            continue;
                        }
                        int step = isMoveable(_cells[next.x, next.y]) ? 0 : 1;
                        if (cost[current.x, current.y] + step >= cost[next.x, next.y])
                        {
                            continue;
                        }
                        cost[next.x, next.y] = cost[current.x, current.y] + step;
                        from[next.x, next.y] = current;
                        if (step == 0) deque.AddFirst(next); else deque.AddLast(next);
                    }
                }

                if (target.x < 0)
                {
                    return bridges;
                }

                // たどった経路の陸以外を橋にする
                var corridor = new List<Vector2Int>();
                for (Vector2Int p = target; component[p.x, p.y] != largest; p = from[p.x, p.y])
                {
                    if (!isMoveable(_cells[p.x, p.y]))
                    {
                        corridor.Add(p);
                    }
                }
                foreach (Vector2Int p in corridor)
                {
                    _cells[p.x, p.y] = eCell.Bridge;
                }
                bridges += corridor.Count;

                // 渡って戻ってこられるよう、離れた場所にもう1本かける
                bridges += addSecondBridge(_cells, component, largest, component[target.x, target.y], corridor);
            }
        }

        /// <summary>
        /// 1本目の橋から離れた場所に、同じ陸どうしをつなぐ橋をもう1本かける（かけた数を返す）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_component"></param>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        /// <param name="_firstBridge"></param>
        private static int addSecondBridge(eCell[,] _cells, int[,] _component, int _from, int _to, List<Vector2Int> _firstBridge)
        {
            const int MaxExtraLength = 4;
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            if (_firstBridge.Count == 0)
            {
                return 0;
            }

            // 1本目とその周り1マスは通らない
            var blocked = new bool[width, height];
            foreach (Vector2Int p in _firstBridge)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = p.x + dx, y = p.y + dy;
                        if (isInside(_cells, x, y)) blocked[x, y] = true;
                    }
                }
            }

            var cost = new int[width, height];
            var previous = new Vector2Int[width, height];
            var deque = new LinkedList<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cost[x, y] = int.MaxValue;
                    if (_component[x, y] == _from && !blocked[x, y])
                    {
                        cost[x, y] = 0;
                        deque.AddLast(new Vector2Int(x, y));
                    }
                }
            }

            while (deque.Count > 0)
            {
                Vector2Int current = deque.First.Value;
                deque.RemoveFirst();
                if (_component[current.x, current.y] == _to)
                {
                    if (cost[current.x, current.y] > _firstBridge.Count + MaxExtraLength)
                    {
                        return 0;
                    }

                    int added = 0;
                    for (Vector2Int p = current; _component[p.x, p.y] != _from; p = previous[p.x, p.y])
                    {
                        if (_cells[p.x, p.y] != eCell.Land && _cells[p.x, p.y] != eCell.Bridge)
                        {
                            _cells[p.x, p.y] = eCell.Bridge;
                            added++;
                        }
                    }
                    return added;
                }

                foreach (Vector2Int d in Directions)
                {
                    Vector2Int next = current + d;
                    if (!isInside(_cells, next.x, next.y) || blocked[next.x, next.y])
                    {
                        continue;
                    }
                    // 他の陸は通らない（つなぐ2つの陸の間だけ）
                    int label = _component[next.x, next.y];
                    if (label >= 0 && label != _from && label != _to)
                    {
                        continue;
                    }
                    int step = label >= 0 ? 0 : 1;
                    if (cost[current.x, current.y] + step >= cost[next.x, next.y])
                    {
                        continue;
                    }
                    cost[next.x, next.y] = cost[current.x, current.y] + step;
                    previous[next.x, next.y] = current;
                    if (step == 0) deque.AddFirst(next); else deque.AddLast(next);
                }
            }
            return 0;
        }

        /// <summary>
        /// つながった陸ごとに番号を付ける（陸以外は -1）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_count"></param>
        /// <param name="_largest"></param>
        private static int[,] labelComponents(eCell[,] _cells, out int _count, out int _largest)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            var label = new int[width, height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) label[x, y] = -1;

            _count = 0;
            _largest = -1;
            int largestSize = 0;
            var stack = new Stack<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!isMoveable(_cells[x, y]) || label[x, y] >= 0)
                    {
                        continue;
                    }

                    int size = 0;
                    label[x, y] = _count;
                    stack.Push(new Vector2Int(x, y));
                    while (stack.Count > 0)
                    {
                        Vector2Int p = stack.Pop();
                        size++;
                        foreach (Vector2Int d in Directions)
                        {
                            Vector2Int n = p + d;
                            if (isInside(_cells, n.x, n.y) && isMoveable(_cells[n.x, n.y]) && label[n.x, n.y] < 0)
                            {
                                label[n.x, n.y] = _count;
                                stack.Push(n);
                            }
                        }
                    }

                    if (size > largestSize)
                    {
                        largestSize = size;
                        _largest = _count;
                    }
                    _count++;
                }
            }
            return label;
        }

        // ---------- 経路 ----------

        /// <summary>
        /// 長い経路を作る（両端が Start・Goal。Start・Goal を除いて _waypointCount マスを目指す）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_moveable"></param>
        /// <param name="_waypointCount"></param>
        /// <param name="_random"></param>
        private static List<Vector2Int> createPath(eCell[,] _cells, List<Vector2Int> _moveable, int _waypointCount, System.Random _random)
        {
            int targetLength = _waypointCount + 2;

            // 行き止まりに近い陸（隣が少ない）から始めると長い経路になりやすい
            var starts = _moveable.FindAll(_p => _cells[_p.x, _p.y] == eCell.Land);
            starts.Sort((_a, _b) => degree(_cells, _a).CompareTo(degree(_cells, _b)));

            List<Vector2Int> best = new List<Vector2Int>();
            int bestScore = int.MinValue;
            for (int i = 0; i < PathAttempts && starts.Count > 0; i++)
            {
                Vector2Int start = i < PathAttempts / 2 && i < starts.Count ? starts[i] : starts[_random.Next(starts.Count)];
                List<Vector2Int> walk = walkWarnsdorff(_cells, start, _random);
                expand(_cells, walk, targetLength, _random);
                List<Vector2Int> path = trim(_cells, walk, targetLength);
                if (path.Count < 2)
                {
                    continue;
                }

                // 目標に届いたものは超過が少ないほど、届かないものは長いほど良い
                int score = path.Count >= targetLength ? int.MaxValue - (path.Count - targetLength) : path.Count;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = path;
                }
                if (path.Count == targetLength)
                {
                    break;
                }
            }
            return best;
        }

        /// <summary>
        /// 先が少ないマスを優先して進む（Warnsdorff 法）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_start"></param>
        /// <param name="_random"></param>
        private static List<Vector2Int> walkWarnsdorff(eCell[,] _cells, Vector2Int _start, System.Random _random)
        {
            var walk = new List<Vector2Int> { _start };
            var visited = new HashSet<Vector2Int> { _start };
            extendEnd(_cells, walk, visited, _random);
            return walk;
        }

        /// <summary>
        /// 末尾から、先が少ない空きマスを優先して伸ばす（伸ばせたら true）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_walk"></param>
        /// <param name="_visited"></param>
        /// <param name="_random"></param>
        private static bool extendEnd(eCell[,] _cells, List<Vector2Int> _walk, HashSet<Vector2Int> _visited, System.Random _random)
        {
            bool isExtended = false;
            while (true)
            {
                Vector2Int current = _walk[_walk.Count - 1];
                Vector2Int next = new Vector2Int(-1, -1);
                int bestOnward = int.MaxValue;
                int ties = 0;
                foreach (Vector2Int d in Directions)
                {
                    Vector2Int candidate = current + d;
                    if (!isFree(_cells, _visited, candidate))
                    {
                        continue;
                    }

                    int onward = 0;
                    foreach (Vector2Int d2 in Directions)
                    {
                        Vector2Int n = candidate + d2;
                        if (n != current && isFree(_cells, _visited, n)) onward++;
                    }

                    // 同じ数なら等確率で選ぶ
                    if (onward < bestOnward)
                    {
                        bestOnward = onward;
                        next = candidate;
                        ties = 1;
                    }
                    else if (onward == bestOnward && _random.Next(++ties) == 0)
                    {
                        next = candidate;
                    }
                }

                if (next.x < 0)
                {
                    return isExtended;
                }
                _walk.Add(next);
                _visited.Add(next);
                isExtended = true;
            }
        }

        /// <summary>
        /// 目標の長さまで、寄り道を挟む・両端を伸ばすを繰り返す
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_walk"></param>
        /// <param name="_targetLength"></param>
        /// <param name="_random"></param>
        private static void expand(eCell[,] _cells, List<Vector2Int> _walk, int _targetLength, System.Random _random)
        {
            var visited = new HashSet<Vector2Int>(_walk);
            bool isChanged = true;
            while (isChanged && _walk.Count < _targetLength)
            {
                isChanged = extendEnd(_cells, _walk, visited, _random);
                _walk.Reverse();
                isChanged |= extendEnd(_cells, _walk, visited, _random);
                _walk.Reverse();

                // 毎回違う位置から寄り道を探して偏りを減らす
                int offset = _random.Next(Mathf.Max(1, _walk.Count - 1));
                for (int k = 0; k < _walk.Count - 1 && _walk.Count < _targetLength; k++)
                {
                    int i = (offset + k) % (_walk.Count - 1);
                    if (tryDetour(_cells, _walk, visited, i, _random))
                    {
                        isChanged = true;
                    }
                }
            }
        }

        /// <summary>
        /// 目標の長さに切り詰める（両端は陸、Start・Goal は MinStartGoalDistance 以上離す）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_walk"></param>
        /// <param name="_targetLength"></param>
        private static List<Vector2Int> trim(eCell[,] _cells, List<Vector2Int> _walk, int _targetLength)
        {
            // 先頭が橋なら陸まで捨てる
            int first = 0;
            while (first < _walk.Count && _cells[_walk[first].x, _walk[first].y] != eCell.Land)
            {
                first++;
            }

            // 目標の長さに近い順（長い方を優先）に、条件を満たす終端を探す
            int ideal = Mathf.Min(_walk.Count - 1, first + _targetLength - 1);
            for (int delta = 0; delta < _walk.Count; delta++)
            {
                foreach (int last in new[] { ideal + delta, ideal - delta })
                {
                    if (last <= first || last >= _walk.Count)
                    {
                        continue;
                    }
                    Vector2Int start = _walk[first], goal = _walk[last];
                    int distance = Mathf.Abs(start.x - goal.x) + Mathf.Abs(start.y - goal.y);
                    if (_cells[goal.x, goal.y] == eCell.Land && distance >= MinStartGoalDistance)
                    {
                        return _walk.GetRange(first, last - first + 1);
                    }
                }
            }
            return new List<Vector2Int>();
        }

        /// <summary>
        /// path[i] → path[i + 1] の間に空きマスだけを通る寄り道を入れる（入れられたら true）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_path"></param>
        /// <param name="_visited"></param>
        /// <param name="_index"></param>
        /// <param name="_random"></param>
        private static bool tryDetour(eCell[,] _cells, List<Vector2Int> _path, HashSet<Vector2Int> _visited, int _index, System.Random _random)
        {
            Vector2Int a = _path[_index], b = _path[_index + 1];
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (Vector2Int d in shuffledDirections(_random))
            {
                Vector2Int n = a + d;
                if (isFree(_cells, _visited, n))
                {
                    previous[n] = a;
                    queue.Enqueue(n);
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (MapData.IsAdjacent(current, b))
                {
                    var detour = new List<Vector2Int>();
                    for (Vector2Int p = current; p != a; p = previous[p])
                    {
                        detour.Add(p);
                    }
                    detour.Reverse();
                    _path.InsertRange(_index + 1, detour);
                    _visited.UnionWith(detour);
                    return true;
                }

                foreach (Vector2Int d in Directions)
                {
                    Vector2Int next = current + d;
                    if (isFree(_cells, _visited, next) && !previous.ContainsKey(next))
                    {
                        previous[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 上下左右をシードで並べ替える
        /// </summary>
        /// <param name="_random"></param>
        private static Vector2Int[] shuffledDirections(System.Random _random)
        {
            var directions = (Vector2Int[])Directions.Clone();
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Vector2Int tmp = directions[i];
                directions[i] = directions[j];
                directions[j] = tmp;
            }
            return directions;
        }

        /// <summary>
        /// 経路に使える空きマスか
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_visited"></param>
        /// <param name="_p"></param>
        private static bool isFree(eCell[,] _cells, HashSet<Vector2Int> _visited, Vector2Int _p)
        {
            return isInside(_cells, _p.x, _p.y) && isMoveable(_cells[_p.x, _p.y]) && !_visited.Contains(_p);
        }

        /// <summary>
        /// 通れる隣のマス数
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_p"></param>
        private static int degree(eCell[,] _cells, Vector2Int _p)
        {
            int count = 0;
            foreach (Vector2Int d in Directions)
            {
                Vector2Int n = _p + d;
                if (isInside(_cells, n.x, n.y) && isMoveable(_cells[n.x, n.y])) count++;
            }
            return count;
        }

        // ---------- 書き込み ----------

        /// <summary>
        /// 形と経路を MapData に書き込む
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_path"></param>
        private static void write(MapData _mapData, eCell[,] _cells, List<Vector2Int> _path)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            _mapData.Initialize(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _mapData.SetBlockType(x, y, toBlockType(_cells[x, y]));
                }
            }

            if (_path.Count < 2)
            {
                return;
            }

            _mapData.SetBlockType(_path[0].x, _path[0].y, eBlockType.Start);
            _mapData.SetBlockType(_path[_path.Count - 1].x, _path[_path.Count - 1].y, eBlockType.Goal);
            for (int i = 1; i < _path.Count - 1; i++)
            {
                _mapData.SetWaypointIndex(_path[i].x, _path[i].y, i - 1);
            }
        }

        private static eBlockType toBlockType(eCell _cell)
        {
            switch (_cell)
            {
                case eCell.Land: return eBlockType.Walkable;
                case eCell.Water: return eBlockType.Water;
                case eCell.Bridge: return eBlockType.Bridge;
                default: return eBlockType.Empty;
            }
        }

        /// <summary>
        /// 経路外の陸に通れないブロックを置く（置いた数を返す）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_path"></param>
        /// <param name="_count"></param>
        /// <param name="_random"></param>
        private static int placeObstructs(MapData _mapData, eCell[,] _cells, List<Vector2Int> _path, int _count, System.Random _random)
        {
            var onPath = new HashSet<Vector2Int>(_path);
            var candidates = new List<Vector2Int>();
            for (int y = 0; y < _mapData.height; y++)
            {
                for (int x = 0; x < _mapData.width; x++)
                {
                    var p = new Vector2Int(x, y);
                    if (_cells[x, y] == eCell.Land && !onPath.Contains(p))
                    {
                        candidates.Add(p);
                    }
                }
            }

            // シードで並べ替えて先頭から置く
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Vector2Int tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            int placed = Mathf.Min(_count, candidates.Count);
            for (int i = 0; i < placed; i++)
            {
                eBlockType obstruct = BlockTypeUtility.Obstructs[_random.Next(BlockTypeUtility.Obstructs.Length)];
                _mapData.SetBlockType(candidates[i].x, candidates[i].y, obstruct);
            }
            return placed;
        }

        // ---------- ギミック ----------

        /// <summary>ギミックの乱数を経路の乱数と分けるための値</summary>
        private const int GimmickSeedSalt = 0x5bd1e995;
        /// <summary>宝箱の種類の出やすさ（木・銀・金、合計 100）</summary>
        private static readonly int[] TreasureWeights = { 60, 30, 10 };

        /// <summary>
        /// 設計した経路に合うマスへギミックを置き直す（前のギミックは消す。置けた数を返す）
        /// ワープは経路の途中を飛ばして入口と出口をつなぎ、飛ばしたマスは通れないブロックにする。
        /// やじるし・まっすぐは経路がまっすぐなマス、まがるは曲がるマス、扉はカギより後、宝箱・カギは経路上。
        /// ほかに進める道が多いマスほど選ばれやすい（迷う所に置くと効く）。
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_counts"></param>
        /// <param name="_random"></param>
        public static GimmickCounts PlaceGimmicks(MapData _mapData, GimmickCounts _counts, System.Random _random)
        {
            _mapData.ClearSubTypes();
            var placed = new GimmickCounts();
            if (!_mapData.TryFindBlock(eBlockType.Start, out Vector2Int start) || !_mapData.TryFindBlock(eBlockType.Goal, out Vector2Int goal))
            {
                return placed;
            }

            var cells = new List<Vector2Int> { start };
            cells.AddRange(_mapData.GetWaypointPositions(out _));
            cells.Add(goal);

            for (int color = 0; color < Mathf.Min(_counts.Warps, GimmickUtility.ColorCount); color++)
            {
                if (insertWarp(_mapData, cells, color, _random))
                {
                    placed.Warps++;
                }
            }

            // 1マスに1つだけのギミックが置けるマス（扉・こわれる橋・やじるし・まがる・まっすぐ）
            var straights = new List<Vector2Int>();
            var turns = new List<Vector2Int>();
            var free = new List<Vector2Int>();
            var weights = new Dictionary<Vector2Int, int>();
            for (int i = 1; i < cells.Count - 1; i++)
            {
                Vector2Int cell = cells[i];
                if (_mapData.GetBlockType(cell.x, cell.y) != eBlockType.Walkable || GimmickUtility.GetExclusive(_mapData.GetBlockSubTypes(cell.x, cell.y)) != eBlockSubType.None)
                {
                    continue;
                }
                bool isJumpIn = !MapData.IsAdjacent(cells[i - 1], cell);
                bool isJumpOut = !MapData.IsAdjacent(cell, cells[i + 1]);
                if (isJumpIn || isJumpOut)
                {
                    continue;
                }
                Vector2Int stepIn = cell - cells[i - 1];
                Vector2Int stepOut = cells[i + 1] - cell;
                (stepIn == stepOut ? straights : turns).Add(cell);
                free.Add(cell);
                weights[cell] = 1 + countOpen(_mapData, cell, cells[i - 1], cells[i + 1]) * 2;
            }

            // 扉（後ろの方）とカギ（扉より前の経路上）
            for (int color = 0; color < Mathf.Min(_counts.Keys, GimmickUtility.ColorCount); color++)
            {
                var gateCandidates = free.FindAll(_c => cells.IndexOf(_c) >= 4);
                List<Vector2Int> gatePick = pick(gateCandidates, weights, 1, _random);
                if (gatePick.Count == 0)
                {
                    break;
                }
                Vector2Int gate = gatePick[0];
                int gateIndex = cells.IndexOf(gate);
                var keyCandidates = new List<Vector2Int>();
                for (int i = 1; i < gateIndex - 1; i++)
                {
                    Vector2Int cell = cells[i];
                    if (_mapData.GetBlockType(cell.x, cell.y) == eBlockType.Walkable && GimmickUtility.GetKey(_mapData.GetBlockSubTypes(cell.x, cell.y)) == eBlockSubType.None
                        && !GimmickUtility.IsGate(GimmickUtility.GetExclusive(_mapData.GetBlockSubTypes(cell.x, cell.y))))
                    {
                        keyCandidates.Add(cell);
                    }
                }
                if (keyCandidates.Count == 0)
                {
                    continue;
                }
                Vector2Int key = keyCandidates[_random.Next(keyCandidates.Count)];
                _mapData.AddBlockSubType(gate.x, gate.y, GimmickUtility.Gates[color]);
                _mapData.AddBlockSubType(key.x, key.y, GimmickUtility.Keys[color]);
                free.Remove(gate);
                straights.Remove(gate);
                turns.Remove(gate);
                placed.Keys++;
            }

            // こわれる橋（最初の数マスは避ける）
            var crumbleCandidates = free.FindAll(_c => cells.IndexOf(_c) >= 3);
            foreach (Vector2Int cell in pick(crumbleCandidates, weights, _counts.Crumbles, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Crumble);
                straights.Remove(cell);
                turns.Remove(cell);
                placed.Crumbles++;
            }

            foreach (Vector2Int cell in pick(straights, weights, _counts.Arrows, _random))
            {
                int index = cells.IndexOf(cell);
                _mapData.AddBlockSubType(cell.x, cell.y, GimmickUtility.ToArrow(cells[index + 1] - cell));
                placed.Arrows++;
            }
            foreach (Vector2Int cell in pick(straights, weights, _counts.Straights, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Straight);
                placed.Straights++;
            }
            foreach (Vector2Int cell in pick(turns, weights, _counts.Turns, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, eBlockSubType.Turn);
                placed.Turns++;
            }

            // 宝箱は設計した経路の上（必ず取れる）。カギと同じマスは避け、ほかに道がある所ほど選ばれやすい
            var treasureCells = new List<Vector2Int>();
            var treasureWeights = new Dictionary<Vector2Int, int>();
            for (int i = 1; i < cells.Count - 1; i++)
            {
                Vector2Int cell = cells[i];
                IReadOnlyList<eBlockSubType> subTypes = _mapData.GetBlockSubTypes(cell.x, cell.y);
                if (_mapData.GetBlockType(cell.x, cell.y) != eBlockType.Walkable || GimmickUtility.GetKey(subTypes) != eBlockSubType.None || GimmickUtility.GetTreasure(subTypes) != eBlockSubType.None)
                {
                    continue;
                }
                treasureCells.Add(cell);
                treasureWeights[cell] = weights.TryGetValue(cell, out int weight) ? weight : 1;
            }
            foreach (Vector2Int cell in pick(treasureCells, treasureWeights, _counts.Treasures, _random))
            {
                _mapData.AddBlockSubType(cell.x, cell.y, pickTreasure(_random));
                placed.Treasures++;
            }            return placed;
        }

        /// <summary>
        /// 宝箱の種類を選ぶ（木 60% / 銀 30% / 金 10%）
        /// </summary>
        /// <param name="_random"></param>
        private static eBlockSubType pickTreasure(System.Random _random)
        {
            int roll = _random.Next(100);
            int total = 0;
            for (int i = 0; i < TreasureWeights.Length; i++)
            {
                total += TreasureWeights[i];
                if (roll < total)
                {
                    return GimmickUtility.Treasures[i];
                }
            }
            return GimmickUtility.Treasures[0];
        }

        /// <summary>
        /// 経路の途中（3〜6マス）を飛ばしてワープでつなぐ（飛ばしたマスは通れないブロックにして、経路の番号を振り直す）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_color"></param>
        /// <param name="_random"></param>
        private static bool insertWarp(MapData _mapData, List<Vector2Int> _cells, int _color, System.Random _random)
        {
            const int MinSkip = 3;
            const int MaxSkip = 6;
            var candidates = new List<Vector2Int>();
            for (int i = 2; i < _cells.Count - 3; i++)
            {
                for (int skip = MinSkip; skip <= MaxSkip; skip++)
                {
                    int j = i + skip + 1;
                    if (j >= _cells.Count - 1)
                    {
                        break;
                    }
                    if (isWarpable(_mapData, _cells, i) && isWarpable(_mapData, _cells, j) && !MapData.IsAdjacent(_cells[i], _cells[j]) && isSkippable(_mapData, _cells, i + 1, j - 1))
                    {
                        candidates.Add(new Vector2Int(i, j));
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return false;
            }

            Vector2Int chosen = candidates[_random.Next(candidates.Count)];
            Vector2Int entrance = _cells[chosen.x];
            Vector2Int exit = _cells[chosen.y];
            for (int i = chosen.x + 1; i < chosen.y; i++)
            {
                Vector2Int skipped = _cells[i];
                _mapData.SetWaypointIndex(skipped.x, skipped.y, -1);
                _mapData.SetBlockType(skipped.x, skipped.y, BlockTypeUtility.Obstructs[_random.Next(BlockTypeUtility.Obstructs.Length)]);
            }
            _cells.RemoveRange(chosen.x + 1, chosen.y - chosen.x - 1);
            _mapData.AddBlockSubType(entrance.x, entrance.y, GimmickUtility.WarpIns[_color]);
            _mapData.AddBlockSubType(exit.x, exit.y, GimmickUtility.WarpOuts[_color]);

            for (int i = 1; i < _cells.Count - 1; i++)
            {
                _mapData.SetWaypointIndex(_cells[i].x, _cells[i].y, i - 1);
            }
            return true;
        }

        /// <summary>
        /// ワープの入口・出口にできるマスか（通れるマスで、前後がつながっていて、ほかのギミックがない）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_index"></param>
        private static bool isWarpable(MapData _mapData, List<Vector2Int> _cells, int _index)
        {
            Vector2Int cell = _cells[_index];
            return _mapData.GetBlockType(cell.x, cell.y) == eBlockType.Walkable
                && _mapData.GetBlockSubTypes(cell.x, cell.y).Count == 0
                && MapData.IsAdjacent(_cells[_index - 1], cell)
                && MapData.IsAdjacent(cell, _cells[_index + 1]);
        }

        /// <summary>
        /// 飛ばしてよいマスか（Start・Goal の近くやギミックのあるマスは飛ばさない）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cells"></param>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        private static bool isSkippable(MapData _mapData, List<Vector2Int> _cells, int _from, int _to)
        {
            for (int i = _from; i <= _to; i++)
            {
                Vector2Int cell = _cells[i];
                if (_mapData.GetBlockType(cell.x, cell.y) != eBlockType.Walkable || _mapData.GetBlockSubTypes(cell.x, cell.y).Count > 0 || _mapData.IsNearStartOrGoal(cell.x, cell.y))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 経路の前後以外に進める隣のマス数
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_cell"></param>
        /// <param name="_previous"></param>
        /// <param name="_next"></param>
        private static int countOpen(MapData _mapData, Vector2Int _cell, Vector2Int _previous, Vector2Int _next)
        {
            int count = 0;
            foreach (Vector2Int d in Directions)
            {
                Vector2Int n = _cell + d;
                if (n != _previous && n != _next && _mapData.IsInside(n.x, n.y) && BlockTypeUtility.IsWalkable(_mapData.GetBlockType(n.x, n.y)))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 重みつきで重ならないように選び、選んだマスは候補から外す
        /// </summary>
        /// <param name="_candidates"></param>
        /// <param name="_weights"></param>
        /// <param name="_count"></param>
        /// <param name="_random"></param>
        private static List<Vector2Int> pick(List<Vector2Int> _candidates, Dictionary<Vector2Int, int> _weights, int _count, System.Random _random)
        {
            var result = new List<Vector2Int>();
            while (result.Count < _count && _candidates.Count > 0)
            {
                int total = 0;
                foreach (Vector2Int cell in _candidates)
                {
                    total += _weights[cell];
                }
                int roll = _random.Next(total);
                for (int i = 0; i < _candidates.Count; i++)
                {
                    roll -= _weights[_candidates[i]];
                    if (roll < 0)
                    {
                        result.Add(_candidates[i]);
                        _candidates.RemoveAt(i);
                        break;
                    }
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 自動生成で使うギミック（数はマップの経路の長さから決める）
    /// </summary>
    public struct GimmickSettings
    {
        public bool Arrow;
        public bool Turn;
        public bool Straight;
        public bool Key;
        public bool Warp;
        public bool Crumble;

        /// <summary>どれか使うか（宝箱は指定がなくても必ず1つ置く）</summary>
        public bool IsAny => Arrow || Turn || Straight || Key || Warp || Crumble;
    }

    /// <summary>
    /// ギミックの数
    /// </summary>
    public struct GimmickCounts
    {
        /// <summary>経路の長さに対するやじるしの割合</summary>
        private const float ArrowRate = 0.06f;
        private const float TurnRate = 0.05f;
        private const float StraightRate = 0.04f;
        /// <summary>自動生成で置く宝箱の数（必ず1つ）</summary>
        private const int TreasuresPerMap = 1;

        public int Arrows;
        public int Turns;
        public int Straights;
        /// <summary>宝箱の数（種類は置く時に選ぶ）</summary>
        public int Treasures;
        /// <summary>カギと扉の組の数（最大3色）</summary>
        public int Keys;
        /// <summary>ワープの組の数（最大3色）</summary>
        public int Warps;
        public int Crumbles;

        /// <summary>合計</summary>
        public int Total => Arrows + Turns + Straights + Treasures + Keys + Warps + Crumbles;

        /// <summary>
        /// 経路の長さに合わせた数
        /// </summary>
        /// <param name="_waypoints"></param>
        /// <param name="_settings"></param>
        public static GimmickCounts FromPath(int _waypoints, GimmickSettings _settings)
        {
            return new GimmickCounts
            {
                Arrows = _settings.Arrow ? Mathf.Max(1, Mathf.RoundToInt(_waypoints * ArrowRate)) : 0,
                Turns = _settings.Turn ? Mathf.Max(1, Mathf.RoundToInt(_waypoints * TurnRate)) : 0,
                Straights = _settings.Straight ? Mathf.Max(1, Mathf.RoundToInt(_waypoints * StraightRate)) : 0,
                Treasures = TreasuresPerMap,
                Keys = _settings.Key ? (_waypoints >= 30 ? 2 : 1) : 0,
                Warps = _settings.Warp ? (_waypoints >= 40 ? 2 : 1) : 0,
                Crumbles = _settings.Crumble ? Mathf.Clamp(_waypoints / 25, 1, 2) : 0,
            };
        }

        /// <summary>
        /// 表示用
        /// </summary>
        public override string ToString()
        {
            return $"arrow {Arrows}, turn {Turns}, straight {Straights}, treasure {Treasures}, key {Keys}, warp {Warps}, crumble {Crumbles}";
        }
    }
}