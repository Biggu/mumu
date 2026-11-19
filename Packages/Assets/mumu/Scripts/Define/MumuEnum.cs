using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ブロック種別（値は MapData にシリアライズされるため末尾に追加すること）
    /// </summary>
    public enum eBlockType
    {
        /// <summary>何もない（非表示）</summary>
        Empty = 0,
        /// <summary>通れる</summary>
        Walkable = 1,
        /// <summary>通れない（デコ1）</summary>
        Obstruct1 = 2,
        /// <summary>スタート</summary>
        Start = 3,
        /// <summary>ゴール</summary>
        Goal = 4,
        /// <summary>水（通れない・背景扱い）</summary>
        Water = 5,
        /// <summary>通れない（デコ2）</summary>
        Obstruct2 = 6,
        /// <summary>通れない（デコ3）</summary>
        Obstruct3 = 7,
        /// <summary>通れない（デコ4）</summary>
        Obstruct4 = 8,
        /// <summary>橋（水の上を通れる）</summary>
        Bridge = 9,
    }

    /// <summary>
    /// ブロック種別の判定
    /// </summary>
    public static class BlockTypeUtility
    {
        /// <summary>通れないブロックの種類（デコ番号順）</summary>
        public static readonly eBlockType[] Obstructs = { eBlockType.Obstruct1, eBlockType.Obstruct2, eBlockType.Obstruct3, eBlockType.Obstruct4 };

        /// <summary>
        /// 経路にできるブロックか（通れる・橋）
        /// </summary>
        /// <param name="_type"></param>
        public static bool IsWalkable(eBlockType _type)
        {
            return _type == eBlockType.Walkable || _type == eBlockType.Bridge;
        }

        /// <summary>
        /// 通れないブロックか
        /// </summary>
        /// <param name="_type"></param>
        public static bool IsObstruct(eBlockType _type)
        {
            return GetObstructIndex(_type) >= 0;
        }

        /// <summary>
        /// 通れないブロックのデコ番号（0〜3、それ以外は -1）
        /// </summary>
        /// <param name="_type"></param>
        public static int GetObstructIndex(eBlockType _type)
        {
            return System.Array.IndexOf(Obstructs, _type);
        }
    }

    /// <summary>
    /// マスに置くギミック（1マスに複数置ける。CSV の gimmick 列に名前を ; 区切りで書く）
    /// </summary>
    public enum eBlockSubType
    {
        None,
        /// <summary>やじるし（上 = 画面の奥、y-1 へしか通れない）</summary>
        ArrowUp,
        /// <summary>やじるし（下 = y+1）</summary>
        ArrowDown,
        /// <summary>やじるし（左 = x-1）</summary>
        ArrowLeft,
        /// <summary>やじるし（右 = x+1）</summary>
        ArrowRight,
        /// <summary>ここで必ず曲がる</summary>
        Turn,
        /// <summary>ここはまっすぐ通る</summary>
        Straight,
        /// <summary>宝箱（木。通るとほね 2）</summary>
        Treasure1,
        /// <summary>宝箱（銀。通るとほね 4）</summary>
        Treasure2,
        /// <summary>宝箱（金。通るとほね 10）</summary>
        Treasure3,
        /// <summary>カギ（赤・青・黄。経路で通ると同じ色の扉が開く）</summary>
        KeyRed,
        KeyBlue,
        KeyYellow,
        /// <summary>扉（同じ色のカギを通るまで入れない）</summary>
        GateRed,
        GateBlue,
        GateYellow,
        /// <summary>ワープの入口（乗ると同じ色の出口へ飛ぶ。出口がもう経路上なら普通のマス）</summary>
        WarpInRed,
        WarpInBlue,
        WarpInYellow,
        /// <summary>ワープの出口</summary>
        WarpOutRed,
        WarpOutBlue,
        WarpOutYellow,
        /// <summary>こわれる橋（降りると壊れて、そこより前には戻れない）</summary>
        Crumble,
    }

    /// <summary>
    /// ギミックの判定
    /// </summary>
    public static class GimmickUtility
    {
        /// <summary>色つきギミックの色の数（赤・青・黄）</summary>
        public const int ColorCount = 3;
        public static readonly eBlockSubType[] Keys = { eBlockSubType.KeyRed, eBlockSubType.KeyBlue, eBlockSubType.KeyYellow };
        public static readonly eBlockSubType[] Gates = { eBlockSubType.GateRed, eBlockSubType.GateBlue, eBlockSubType.GateYellow };
        public static readonly eBlockSubType[] WarpIns = { eBlockSubType.WarpInRed, eBlockSubType.WarpInBlue, eBlockSubType.WarpInYellow };
        public static readonly eBlockSubType[] WarpOuts = { eBlockSubType.WarpOutRed, eBlockSubType.WarpOutBlue, eBlockSubType.WarpOutYellow };
        /// <summary>色つきギミックの色（赤・青・黄）</summary>
        public static readonly Color[] Colors = { new Color(1f, 0.42f, 0.42f), new Color(0.4f, 0.62f, 1f), new Color(1f, 0.85f, 0.25f) };

        /// <summary>宝箱の種類（木・銀・金）</summary>
        public static readonly eBlockSubType[] Treasures = { eBlockSubType.Treasure1, eBlockSubType.Treasure2, eBlockSubType.Treasure3 };
        /// <summary>宝箱でもらえるほね（木・銀・金）</summary>
        private static readonly int[] TreasureBones = { 2, 4, 10 };

        public static bool IsTreasure(eBlockSubType _subType) => _subType >= eBlockSubType.Treasure1 && _subType <= eBlockSubType.Treasure3;

        /// <summary>
        /// 宝箱でもらえるほね（宝箱でなければ 0）
        /// </summary>
        /// <param name="_subType"></param>
        public static int GetTreasureBones(eBlockSubType _subType)
        {
            return IsTreasure(_subType) ? TreasureBones[_subType - eBlockSubType.Treasure1] : 0;
        }

        /// <summary>
        /// マスの宝箱でもらえるほね（なければ 0）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static int GetTreasureBones(IReadOnlyList<eBlockSubType> _subTypes)
        {
            int bones = 0;
            foreach (eBlockSubType subType in _subTypes)
            {
                bones += GetTreasureBones(subType);
            }
            return bones;
        }

        /// <summary>
        /// マスの宝箱（なければ None）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static eBlockSubType GetTreasure(IReadOnlyList<eBlockSubType> _subTypes)
        {
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsTreasure(subType))
                {
                    return subType;
                }
            }
            return eBlockSubType.None;
        }

        public static bool IsKey(eBlockSubType _subType) => _subType >= eBlockSubType.KeyRed && _subType <= eBlockSubType.KeyYellow;
        public static bool IsGate(eBlockSubType _subType) => _subType >= eBlockSubType.GateRed && _subType <= eBlockSubType.GateYellow;
        public static bool IsWarpIn(eBlockSubType _subType) => _subType >= eBlockSubType.WarpInRed && _subType <= eBlockSubType.WarpInYellow;
        public static bool IsWarpOut(eBlockSubType _subType) => _subType >= eBlockSubType.WarpOutRed && _subType <= eBlockSubType.WarpOutYellow;

        /// <summary>
        /// 色つきギミックの色番号（0 赤 / 1 青 / 2 黄、色がなければ -1）
        /// </summary>
        /// <param name="_subType"></param>
        public static int GetColorIndex(eBlockSubType _subType)
        {
            if (IsKey(_subType)) return _subType - eBlockSubType.KeyRed;
            if (IsGate(_subType)) return _subType - eBlockSubType.GateRed;
            if (IsWarpIn(_subType)) return _subType - eBlockSubType.WarpInRed;
            if (IsWarpOut(_subType)) return _subType - eBlockSubType.WarpOutRed;
            return -1;
        }

        /// <summary>
        /// 1マスに1つだけ置けるギミックか（通り方・扉・ワープ・こわれる橋。宝箱とカギは一緒に置ける）
        /// </summary>
        /// <param name="_subType"></param>
        public static bool IsExclusive(eBlockSubType _subType)
        {
            return IsRoute(_subType) || IsGate(_subType) || IsWarpIn(_subType) || IsWarpOut(_subType) || _subType == eBlockSubType.Crumble;
        }

        /// <summary>
        /// マスの1つだけのギミック（なければ None）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static eBlockSubType GetExclusive(IReadOnlyList<eBlockSubType> _subTypes)
        {
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsExclusive(subType))
                {
                    return subType;
                }
            }
            return eBlockSubType.None;
        }

        /// <summary>
        /// マスのカギ（なければ None）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static eBlockSubType GetKey(IReadOnlyList<eBlockSubType> _subTypes)
        {
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsKey(subType))
                {
                    return subType;
                }
            }
            return eBlockSubType.None;
        }

        /// <summary>やじるしの種類</summary>
        public static readonly eBlockSubType[] Arrows = { eBlockSubType.ArrowUp, eBlockSubType.ArrowDown, eBlockSubType.ArrowLeft, eBlockSubType.ArrowRight };

        /// <summary>
        /// やじるしか
        /// </summary>
        /// <param name="_subType"></param>
        public static bool IsArrow(eBlockSubType _subType)
        {
            return _subType >= eBlockSubType.ArrowUp && _subType <= eBlockSubType.ArrowRight;
        }

        /// <summary>
        /// 通り方を決めるギミックか（やじるし・まがる・まっすぐ。1マスに1つだけ）
        /// </summary>
        /// <param name="_subType"></param>
        public static bool IsRoute(eBlockSubType _subType)
        {
            return IsArrow(_subType) || _subType == eBlockSubType.Turn || _subType == eBlockSubType.Straight;
        }

        /// <summary>
        /// やじるしの向き（マップの座標、y+1 が下）
        /// </summary>
        /// <param name="_subType"></param>
        public static Vector2Int GetArrowDirection(eBlockSubType _subType)
        {
            switch (_subType)
            {
                case eBlockSubType.ArrowUp: return new Vector2Int(0, -1);
                case eBlockSubType.ArrowDown: return new Vector2Int(0, 1);
                case eBlockSubType.ArrowLeft: return new Vector2Int(-1, 0);
                case eBlockSubType.ArrowRight: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }

        /// <summary>
        /// 向き → やじるし
        /// </summary>
        /// <param name="_direction"></param>
        public static eBlockSubType ToArrow(Vector2Int _direction)
        {
            if (_direction.x > 0) return eBlockSubType.ArrowRight;
            if (_direction.x < 0) return eBlockSubType.ArrowLeft;
            if (_direction.y < 0) return eBlockSubType.ArrowUp;
            if (_direction.y > 0) return eBlockSubType.ArrowDown;
            return eBlockSubType.None;
        }

        /// <summary>
        /// マスの通り方のギミック（なければ None）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static eBlockSubType GetRoute(IReadOnlyList<eBlockSubType> _subTypes)
        {
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsRoute(subType))
                {
                    return subType;
                }
            }
            return eBlockSubType.None;
        }

        /// <summary>
        /// 入ってきた向きと出ていく向きがギミックの決まりに合うか（向きが zero の側は判定しない）
        /// </summary>
        /// <param name="_route"></param>
        /// <param name="_in"></param>
        /// <param name="_out"></param>
        public static bool IsAllowed(eBlockSubType _route, Vector2Int _in, Vector2Int _out)
        {
            if (IsArrow(_route))
            {
                Vector2Int direction = GetArrowDirection(_route);
                return (_in == Vector2Int.zero || _in == direction) && (_out == Vector2Int.zero || _out == direction);
            }
            if (_in == Vector2Int.zero || _out == Vector2Int.zero)
            {
                return true;
            }
            switch (_route)
            {
                case eBlockSubType.Turn: return _in != _out;
                case eBlockSubType.Straight: return _in == _out;
                default: return true;
            }
        }
    }
}
