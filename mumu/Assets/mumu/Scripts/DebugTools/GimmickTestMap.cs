#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ギミックのテスト用マップ（デバッグ）。障害物なしの5×5の一本道に、ギミックを1種類ずつ置く
    /// </summary>
    public static class GimmickTestMap
    {
        private const int W = 5;
        private const int H = 5;

        /// <summary>デバッグメニューに並べるギミック（ピース名）</summary>
        public static readonly string[] Pieces =
        {
            "arrow_up", "arrow_down", "arrow_left", "arrow_right", "turn", "straight",
            "key_red", "warp_red", "key_blue", "warp_blue", "key_yellow", "warp_yellow", "crumble",
            "oneway_up", "oneway_down", "oneway_left", "oneway_right", "switch_red", "switch_blue", "switch_yellow", "fog",
        };

        /// <summary>
        /// ピース名のギミックだけを置いた5×5の一本道（障害物なし）
        /// </summary>
        /// <param name="_piece"></param>
        public static MapData Build(string _piece)
        {
            bool vertical = _piece == "arrow_up" || _piece == "arrow_down" || _piece == "oneway_up" || _piece == "oneway_down";
            bool warp = typeOf(_piece) == "warp";

            var path = new List<Vector2Int>();
            if (vertical)
            {
                for (int y = 0; y < H; y++) path.Add(new Vector2Int(0, y));
                path.Add(new Vector2Int(1, H - 1));
                for (int y = H - 1; y >= 0; y--) path.Add(new Vector2Int(2, y));
                path.Add(new Vector2Int(3, 0));
                for (int y = 0; y < H; y++) path.Add(new Vector2Int(4, y));
            }
            else
            {
                for (int x = 0; x < W; x++) path.Add(new Vector2Int(x, 0));
                if (!warp) path.Add(new Vector2Int(W - 1, 1));
                for (int x = W - 1; x >= 0; x--) path.Add(new Vector2Int(x, 2));
                path.Add(new Vector2Int(0, 3));
                for (int x = 0; x < W; x++) path.Add(new Vector2Int(x, 4));
            }

            var map = new MapData();
            map.Initialize(W, H);
            foreach (Vector2Int p in path) map.SetBlockType(p.x, p.y, eBlockType.Walkable);
            map.SetBlockType(path[0].x, path[0].y, eBlockType.Start);
            map.SetBlockType(path[path.Count - 1].x, path[path.Count - 1].y, eBlockType.Goal);
            for (int i = 1; i < path.Count - 1; i++) map.SetWaypointIndex(path[i].x, path[i].y, i - 1);

            Vector2Int Dir(int _x, int _y)
            {
                int i = path.IndexOf(new Vector2Int(_x, _y));
                return path[i + 1] - path[i];
            }
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
                case "oneway_right":
                    foreach (int x in new[] { 1, 2, 3 }) Add(x, 0, eBlockSubType.OneWayRight);
                    break;
                case "oneway_left":
                    foreach (int x in new[] { 1, 2, 3 }) Add(x, 2, eBlockSubType.OneWayLeft);
                    break;
                case "oneway_down":
                    foreach (int y in new[] { 1, 2, 3 }) Add(0, y, eBlockSubType.OneWayDown);
                    break;
                case "oneway_up":
                    foreach (int y in new[] { 1, 2, 3 }) Add(2, y, eBlockSubType.OneWayUp);
                    break;
                case "switch_red": case "switch_blue": case "switch_yellow":
                    Add(2, 0, switchOf(_piece)); Add(2, 2, gateOf(_piece));
                    break;
                case "fog":
                    Add(2, 4, eBlockSubType.Fog);
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

            // 宝箱（木）は必ず1つ置く
            Add(1, H - 1, eBlockSubType.Treasure1);
            return map;
        }

        /// <summary>ピース名の先頭（arrow・key・warp など）</summary>
        private static string typeOf(string _piece) => _piece.Split('_')[0];

        /// <summary>ピース名の末尾の色（red・blue・yellow）</summary>
        private static string colorOf(string _piece) => _piece.Substring(_piece.LastIndexOf('_') + 1);

        private static eBlockSubType keyOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.KeyRed : colorOf(_piece) == "blue" ? eBlockSubType.KeyBlue : eBlockSubType.KeyYellow;
        private static eBlockSubType switchOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.SwitchRed : colorOf(_piece) == "blue" ? eBlockSubType.SwitchBlue : eBlockSubType.SwitchYellow;
        private static eBlockSubType gateOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.GateRed : colorOf(_piece) == "blue" ? eBlockSubType.GateBlue : eBlockSubType.GateYellow;
        private static eBlockSubType warpInOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.WarpInRed : colorOf(_piece) == "blue" ? eBlockSubType.WarpInBlue : eBlockSubType.WarpInYellow;
        private static eBlockSubType warpOutOf(string _piece) => colorOf(_piece) == "red" ? eBlockSubType.WarpOutRed : colorOf(_piece) == "blue" ? eBlockSubType.WarpOutBlue : eBlockSubType.WarpOutYellow;
    }
}
#endif