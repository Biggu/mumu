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
    /// 難易度・形・シードからマップを生成（同じ引数なら同じマップ）。形・橋・経路・障害物は MapShape / MapBridge / MapPath / MapObstruct
    /// </summary>
    public static class MapGenerator
    {
        public const int MinDifficulty = 1;
        public const int MaxDifficulty = 50;
        /// <summary>Start と Goal の最低距離（間に2マス以上）</summary>
        public const int MinStartGoalDistance = 3;

        /// <summary>難易度カーブの基準レベル（この間は線形）</summary>
        private static readonly int[] CurveLevels = { 1, 20, 40, 50 };
        /// <summary>基準サイズ</summary>
        private static readonly float[] SizeCurve = { 5f, 14f, 18f, 20f };
        /// <summary>通れるマスのうち経路にする割合（上がるほど難しい）</summary>
        private static readonly float[] WaypointCurve = { 0.6f, 0.8f, 0.87f, 0.9f };
        /// <summary>通れないブロックの割合（レベル1〜10 で段階的に増える。10 以上は従来どおり）</summary>
        private static readonly int[] ObstructCurveLevels = { 1, 10, 20, 40, 50 };
        private static readonly float[] ObstructCurve = { 0.15f, 0.243f, 0.18f, 0.13f, 0.10f };
        /// <summary>Goal 解放に必要な手数（経路数に対する割合）</summary>
        /// <summary>Goal 解放の割合（テーマ（10ステージ）の中で、1ステージ目 80% → 9ステージ目 95%）</summary>
        private const float GoalUnlockMin = 0.8f;
        private const float GoalUnlockMax = 0.95f;
        /// <summary>テーマの中のステージ数（10 の最後はボーナスステージ）</summary>
        public const int StepsPerBlock = 10;
        /// <summary>Goal 解放の割合を上げるステージ数（1〜9ステージ目）</summary>
        private const int GoalUnlockSteps = 9;

        private const int MinSize = 5;

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
        /// 指定した基準レベルでの難易度カーブの値（基準レベルの間は線形）
        /// </summary>
        /// <param name="_difficulty"></param>
        /// <param name="_levels"></param>
        /// <param name="_values"></param>
        private static float evaluateOn(float _difficulty, int[] _levels, float[] _values)
        {
            float level = Mathf.Clamp(_difficulty, MinDifficulty, MaxDifficulty);
            for (int i = 1; i < _levels.Length; i++)
            {
                if (level <= _levels[i])
                {
                    float t = (level - _levels[i - 1]) / (float)(_levels[i] - _levels[i - 1]);
                    return Mathf.Lerp(_values[i - 1], _values[i], t);
                }
            }
            return _values[_values.Length - 1];
        }

        /// <summary>
        /// 難易度カーブの値（基準レベルの間は線形）
        /// </summary>
        /// <param name="_difficulty"></param>
        /// <param name="_values"></param>
        private static float evaluate(float _difficulty, float[] _values)
        {
            float level = Mathf.Clamp(_difficulty, MinDifficulty, MaxDifficulty);
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
        public static int GetSize(float _difficulty, eMapPattern _pattern)
        {
            int size = Mathf.RoundToInt(evaluate(_difficulty, SizeCurve));
            return Mathf.Max(size, getMinSize(_pattern));
        }

        /// <summary>
        /// Goal 解放に必要な手数の割合（経路数に対して）
        /// </summary>
        /// <param name="_difficulty"></param>
        public static float GetGoalUnlockRate(int _step)
        {
            float t = Mathf.Clamp(_step, 0, GoalUnlockSteps - 1) / (float)(GoalUnlockSteps - 1);
            return Mathf.Lerp(GoalUnlockMin, GoalUnlockMax, t);
        }

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
        public static float GetWaypointRate(float _difficulty) => evaluate(_difficulty, WaypointCurve);

        /// <summary>
        /// 通れるマスのうち通れないブロックにする割合（経路・自由マスの残り）
        /// </summary>
        /// <param name="_difficulty"></param>
        public static float GetObstructRate(float _difficulty) => evaluateOn(_difficulty, ObstructCurveLevels, ObstructCurve);

        /// <summary>
        /// 通れるマスのうち自由に通れるマスの割合（経路と通れないブロックの残り）
        /// </summary>
        /// <param name="_difficulty"></param>
        public static float GetFreeRate(float _difficulty) => Mathf.Max(0f, 1f - GetWaypointRate(_difficulty) - GetObstructRate(_difficulty));

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
        public static Result Generate(MapData _mapData, float _difficulty, eMapPattern _pattern, int _seed, bool _isLogEnabled = true, GimmickSettings _gimmicks = default, int _fixedSize = 0)
        {
            var random = new System.Random(_seed);
            // 指定があれば、形に関係なく正方形の枠（ボーナスステージは 10×10）
            Vector2Int size = _fixedSize > 0 ? new Vector2Int(_fixedSize, _fixedSize) : GetDimensions(_pattern, GetSize(_difficulty, _pattern));
            int width = size.x;
            int height = size.y;

            eCell[,] cells = MapShape.CreateShape(_pattern, width, height, random);
            MapShape.Vary(cells, MapShape.OutsideOf(_pattern), random);
            int bridges = MapBridge.Connect(cells);

            var moveable = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (MapGrid.IsMoveable(cells[x, y]))
                    {
                        moveable.Add(new Vector2Int(x, y));
                    }
                }
            }

            var result = new Result { Width = width, Height = height, Moveable = moveable.Count, Bridges = bridges };
            // Start・Goal は経路の両端（Warnsdorff 法で長い経路を作り、端を決める）
            List<Vector2Int> path = MapPath.CreatePath(cells, moveable, Mathf.CeilToInt(moveable.Count * GetWaypointRate(_difficulty)), random);

            write(_mapData, cells, path);
            _mapData.difficulty = Mathf.FloorToInt(Mathf.Clamp(_difficulty, MinDifficulty, MaxDifficulty));
            result.Waypoints = Mathf.Max(0, path.Count - 2);

            // 自由マスは決めた割合だけ残し、経路が足りない分も含めて残りは通れないブロック
            int freeCount = Mathf.RoundToInt(moveable.Count * GetFreeRate(_difficulty));
            result.Obstructs = MapObstruct.Place(_mapData, cells, path, Mathf.Max(0, moveable.Count - path.Count - freeCount), random);
            result.Free = result.Moveable - path.Count - result.Obstructs;
            // ギミックは経路を決めた後に、設計した経路に合うマスだけへ置く（seed を変えて経路の結果は変えない）
            // 宝箱は必ず1つ置くので、ギミックの指定がなくても置く
            {
                result.Gimmicks = PlaceGimmicks(_mapData, GimmickCounts.FromPath(result.Waypoints, _gimmicks), new System.Random(_seed ^ GimmickPlacer.SeedSalt));
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

        /// <summary>
        /// 経路に合わせてギミックを置き直す（エディタの Auto Place と生成で使う）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_counts"></param>
        /// <param name="_random"></param>
        public static GimmickCounts PlaceGimmicks(MapData _mapData, GimmickCounts _counts, System.Random _random)
        {
            return new GimmickPlacer().Place(_mapData, _counts, _random);
        }

        /// <summary>
        /// 通る道を決めずに地形だけ作る（おにわ用。草・水・木や岩・橋を、seed で毎回同じに作る）
        /// </summary>
        /// <param name="_mapData"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_seed"></param>
        public static void GenerateTerrain(MapData _mapData, int _width, int _height, int _seed)
        {
            const double ObstructRate = 0.15;
            var random = new System.Random(_seed);
            _mapData.Initialize(_width, _height);
            eMapPattern[] patterns = { eMapPattern.Square, eMapPattern.Circle, eMapPattern.Oval, eMapPattern.Hexagon, eMapPattern.Diamond, eMapPattern.Donut, eMapPattern.Lake, eMapPattern.Islands, eMapPattern.Coast, eMapPattern.Ponds, eMapPattern.River, eMapPattern.Cross, eMapPattern.LShape, eMapPattern.UShape };
            eMapPattern pattern = patterns[random.Next(patterns.Length)];

            eCell[,] cells = MapShape.CreateShape(pattern, _width, _height, random);
            MapShape.Vary(cells, MapShape.OutsideOf(pattern), random);
            MapBridge.Connect(cells);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    eBlockType type;
                    switch (cells[x, y])
                    {
                        case eCell.Water: type = eBlockType.Water; break;
                        case eCell.Bridge: type = eBlockType.Bridge; break;
                        case eCell.Land: type = random.NextDouble() < ObstructRate ? BlockTypeUtility.Obstructs[random.Next(BlockTypeUtility.Obstructs.Length)] : eBlockType.Walkable; break;
                        default: type = eBlockType.Empty; break;
                    }
                    _mapData.SetBlockType(x, y, type);
                }
            }
        }

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

        /// <summary>
        /// 生成前のマスを MapData のブロック種別にする
        /// </summary>
        /// <param name="_cell"></param>
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
    }
}
