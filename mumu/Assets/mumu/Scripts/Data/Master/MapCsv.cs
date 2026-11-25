using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップ一覧の1行
    /// </summary>
    public class MapInfo
    {
        /// <summary>マップ ID</summary>
        public int Id;
        /// <summary>表示名</summary>
        public string Name;
        /// <summary>テーマ ID</summary>
        public int ThemeId = ThemeDefine.DefaultThemeId;
        /// <summary>横のマス数</summary>
        public int Width;
        /// <summary>縦のマス数</summary>
        public int Height;
        /// <summary>難易度</summary>
        public int Difficulty = 1;
        /// <summary>ボーナスステージか（一度だけ遊べる）</summary>
        public bool IsBonus;
    }

    /// <summary>
    /// マップの CSV（Resources/MapData/map_list.csv と maps/map_{id:000000}.csv）
    /// map_list.csv: id,name,themeId,width,height,difficulty（difficulty は省略時 1）
    /// map_XXXXXX.csv: x,y,type,waypoint,gimmick（type は eBlockType の名前、waypoint は 1 始まりの経路順で空欄 = 経路外、gimmick は eBlockSubType の名前を ; 区切り）
    /// </summary>
    public static class MapCsv
    {
        /// <summary>Resources からのフォルダ</summary>
        public const string ResourceFolder = "MapData";
        /// <summary>エディタで読み書きするフォルダ</summary>
        public const string AssetFolder = "Assets/mumu/Resources/" + ResourceFolder;

        private const string ListName = "map_list";
        private static readonly string[] ListHeader = { "id", "name", "themeId", "width", "height", "difficulty", "bonus" };
        private const int ListRequiredColumns = 5;
        private static readonly string[] MapHeader = { "x", "y", "type", "waypoint", "gimmick" };
        /// <summary>gimmick 列の区切り</summary>
        private const char GimmickSeparator = ';';

        /// <summary>CSV の書き出しに使う文字コード（BOM なし UTF-8）</summary>
        public static readonly Encoding FileEncoding = new UTF8Encoding(false);

        /// <summary>
        /// マップ一覧のパス（Resources から、拡張子なし）
        /// </summary>
        public static string GetListResourcePath() => $"{ResourceFolder}/{ListName}";

        /// <summary>
        /// マップのパス（Resources から、拡張子なし）
        /// </summary>
        /// <param name="_id"></param>
        public static string GetMapResourcePath(int _id) => $"{ResourceFolder}/maps/map_{_id:000000}";

        /// <summary>
        /// マップ一覧のファイルパス（エディタ用）
        /// </summary>
        public static string GetListAssetPath() => $"{AssetFolder}/{ListName}.csv";

        /// <summary>
        /// マップのファイルパス（エディタ用）
        /// </summary>
        /// <param name="_id"></param>
        public static string GetMapAssetPath(int _id) => $"{AssetFolder}/maps/map_{_id:000000}.csv";

        /// <summary>
        /// マップ一覧を読み込む
        /// </summary>
        public static List<MapInfo> LoadList()
        {
            var asset = ResourceManager.LoadResource<TextAsset>(GetListResourcePath());
            if (asset == null)
            {
                Debug.LogError($"[MapCsv] Map list not found: {GetListResourcePath()}");
                return new List<MapInfo>();
            }
            return ParseList(asset.text);
        }

        /// <summary>
        /// 一覧の先頭のマップを読み込む
        /// </summary>
        public static MapData LoadFirstMap()
        {
            List<MapInfo> list = LoadList();
            return list.Count > 0 ? LoadMap(list[0]) : null;
        }

        /// <summary>
        /// マップを読み込む
        /// </summary>
        /// <param name="_info"></param>
        public static MapData LoadMap(MapInfo _info)
        {
            var asset = ResourceManager.LoadResource<TextAsset>(GetMapResourcePath(_info.Id));
            if (asset == null)
            {
                Debug.LogError($"[MapCsv] Map not found: {GetMapResourcePath(_info.Id)}");
                return null;
            }
            return ParseMap(_info, asset.text);
        }

        /// <summary>
        /// マップ一覧の CSV を解析
        /// </summary>
        /// <param name="_text"></param>
        public static List<MapInfo> ParseList(string _text)
        {
            var list = new List<MapInfo>();
            List<string[]> rows = CsvUtility.Parse(_text);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < ListRequiredColumns || !int.TryParse(row[0], out int id))
                {
                    Debug.LogWarning($"[MapCsv] Invalid map list row {i + 1}: {string.Join(",", row)}");
                    continue;
                }

                list.Add(new MapInfo
                {
                    Id = id,
                    Name = row[1],
                    ThemeId = parseInt(row[2], ThemeDefine.DefaultThemeId),
                    Width = parseInt(row[3], 0),
                    Height = parseInt(row[4], 0),
                    Difficulty = row.Length > 5 ? parseInt(row[5], 1) : 1,
                    IsBonus = row.Length > 6 && parseInt(row[6], 0) == 1,
                });
            }
            return list;
        }

        /// <summary>
        /// マップ一覧の CSV を作成
        /// </summary>
        /// <param name="_list"></param>
        public static string WriteList(IEnumerable<MapInfo> _list)
        {
            var builder = new StringBuilder();
            builder.Append(CsvUtility.ToLine(ListHeader)).Append('\n');
            foreach (MapInfo info in _list)
            {
                builder.Append(CsvUtility.ToLine(info.Id, info.Name, info.ThemeId, info.Width, info.Height, info.Difficulty, info.IsBonus ? 1 : 0)).Append('\n');
            }
            return builder.ToString();
        }

        /// <summary>
        /// マップの CSV を解析（書かれていないマスは Empty）
        /// </summary>
        /// <param name="_info"></param>
        /// <param name="_text"></param>
        public static MapData ParseMap(MapInfo _info, string _text)
        {
            var mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.name = $"map_{_info.Id:000000}";
            mapData.id = _info.Id;
            mapData.mapName = _info.Name;
            mapData.themeId = _info.ThemeId;
            mapData.difficulty = _info.Difficulty;
            mapData.isBonus = _info.IsBonus;
            mapData.goalStep = (_info.Id - 1) % MapGenerator.StepsPerBlock;
            mapData.Initialize(Mathf.Max(1, _info.Width), Mathf.Max(1, _info.Height));

            List<string[]> rows = CsvUtility.Parse(_text);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 3 || !int.TryParse(row[0], out int x) || !int.TryParse(row[1], out int y) || !mapData.IsInside(x, y))
                {
                    Debug.LogWarning($"[MapCsv] {mapData.name}: invalid row {i + 1}: {string.Join(",", row)}");
                    continue;
                }

                if (!Enum.TryParse(row[2].Trim(), out eBlockType type) || !Enum.IsDefined(typeof(eBlockType), type))
                {
                    Debug.LogWarning($"[MapCsv] {mapData.name}: unknown type '{row[2]}' at ({x},{y})");
                    continue;
                }

                mapData.SetBlockType(x, y, type);
                // CSV は 1 始まり、内部は 0 始まり
                int waypoint = row.Length > 3 ? parseInt(row[3], 0) : 0;
                mapData.SetWaypointIndex(x, y, waypoint > 0 ? waypoint - 1 : -1);

                // 前の版の CSV には gimmick 列がない
                if (row.Length > 4)
                {
                    foreach (string name in row[4].Split(GimmickSeparator))
                    {
                        if (Enum.TryParse(name.Trim(), out eBlockSubType subType) && subType != eBlockSubType.None)
                        {
                            mapData.AddBlockSubType(x, y, subType);
                        }
                        else if (name.Trim().Length > 0)
                        {
                            Debug.LogWarning($"[MapCsv] {mapData.name}: unknown gimmick '{name}' at ({x},{y})");
                        }
                    }
                }
            }
            return mapData;
        }

        /// <summary>
        /// マップの CSV を作成
        /// </summary>
        /// <param name="_mapData"></param>
        public static string WriteMap(MapData _mapData)
        {
            var builder = new StringBuilder();
            builder.Append(CsvUtility.ToLine(MapHeader)).Append('\n');
            for (int y = 0; y < _mapData.height; y++)
            {
                for (int x = 0; x < _mapData.width; x++)
                {
                    int waypoint = _mapData.GetWaypointIndex(x, y);
                    string gimmick = string.Join(GimmickSeparator.ToString(), _mapData.GetBlockSubTypes(x, y));
                    builder.Append(CsvUtility.ToLine(x, y, _mapData.GetBlockType(x, y), waypoint >= 0 ? (waypoint + 1).ToString() : "", gimmick)).Append('\n');
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// MapData から一覧の1行を作る
        /// </summary>
        /// <param name="_mapData"></param>
        public static MapInfo ToInfo(MapData _mapData)
        {
            return new MapInfo
            {
                Id = _mapData.id,
                Name = _mapData.mapName,
                ThemeId = _mapData.themeId,
                Width = _mapData.width,
                Height = _mapData.height,
                Difficulty = _mapData.difficulty,
                IsBonus = _mapData.isBonus,
            };
        }

        /// <summary>
        /// 整数に変換（失敗時は既定値）
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_default"></param>
        private static int parseInt(string _value, int _default)
        {
            return int.TryParse(_value.Trim(), out int result) ? result : _default;
        }
    }
}
