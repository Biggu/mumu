using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ギミックの判定
    /// </summary>
    public static class GimmickUtility
    {
        /// <summary>色つきギミックの色の数（赤・青・黄）</summary>
        public const int ColorCount = 3;
        public static readonly eBlockSubType[] Keys = { eBlockSubType.KeyRed, eBlockSubType.KeyBlue, eBlockSubType.KeyYellow };
        public static readonly eBlockSubType[] Gates = { eBlockSubType.GateRed, eBlockSubType.GateBlue, eBlockSubType.GateYellow };
        public static readonly eBlockSubType[] Switches = { eBlockSubType.SwitchRed, eBlockSubType.SwitchBlue, eBlockSubType.SwitchYellow };
        /// <summary>一方通行の向き（上・下・左・右）</summary>
        public static readonly eBlockSubType[] OneWays = { eBlockSubType.OneWayUp, eBlockSubType.OneWayDown, eBlockSubType.OneWayLeft, eBlockSubType.OneWayRight };
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
        /// レベルごとの宝箱のほねの倍率（1〜10 = 1倍、11〜20 = 2倍、…、41〜50 = 5倍）。通常のステージでは使わず（1倍）、ボーナスステージで使う予定
        /// </summary>
        /// <param name="_difficulty"></param>
        public static int GetBoneMultiplier(int _difficulty)
        {
            return 1 + Mathf.Max(0, _difficulty - 1) / 10;
        }

        /// <summary>
        /// 通常のステージの宝箱のほね（木・銀・金）。レベルが上がると増える
        /// </summary>
        private static readonly int[] NormalTreasureBones = { 5, 10, 25 };
        /// <summary>通常のステージの宝箱の増え方の分母（レベル1 = 1倍、レベル50 = (20+49)/20 = 3.45倍）</summary>
        private const int NormalGrowthBase = 20;

        /// <summary>
        /// 宝箱でもらえるほね（宝箱でなければ 0）
        /// </summary>
        /// <param name="_subType"></param>
        public static int GetTreasureBones(eBlockSubType _subType)
        {
            return IsTreasure(_subType) ? TreasureBones[_subType - eBlockSubType.Treasure1] : 0;
        }

        /// <summary>
        /// 通常のステージのマスの宝箱のほね（レベルで増える）
        /// </summary>
        /// <param name="_subTypes"></param>
        /// <param name="_difficulty"></param>
        public static int GetNormalTreasureBones(IReadOnlyList<eBlockSubType> _subTypes, int _difficulty)
        {
            int bones = 0;
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsTreasure(subType))
                {
                    int baseBones = NormalTreasureBones[subType - eBlockSubType.Treasure1];
                    bones += baseBones * (NormalGrowthBase + Mathf.Max(0, _difficulty - 1)) / NormalGrowthBase;
                }
            }
            return bones;
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
        public static bool IsSwitch(eBlockSubType _subType) => _subType >= eBlockSubType.SwitchRed && _subType <= eBlockSubType.SwitchYellow;
        public static bool IsOneWay(eBlockSubType _subType) => _subType >= eBlockSubType.OneWayUp && _subType <= eBlockSubType.OneWayRight;
        public static bool IsFog(eBlockSubType _subType) => _subType == eBlockSubType.Fog;
        public static bool IsWarpIn(eBlockSubType _subType) => _subType >= eBlockSubType.WarpInRed && _subType <= eBlockSubType.WarpInYellow;
        public static bool IsWarpOut(eBlockSubType _subType) => _subType >= eBlockSubType.WarpOutRed && _subType <= eBlockSubType.WarpOutYellow;

        /// <summary>
        /// 色つきギミックの色番号（0 赤 / 1 青 / 2 黄、色がなければ -1）
        /// </summary>
        /// <param name="_subType"></param>
        public static int GetColorIndex(eBlockSubType _subType)
        {
            if (IsKey(_subType)) return _subType - eBlockSubType.KeyRed;
            if (IsSwitch(_subType)) return _subType - eBlockSubType.SwitchRed;
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
        /// マスの霧（なければ None）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static eBlockSubType GetFog(IReadOnlyList<eBlockSubType> _subTypes)
        {
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsFog(subType))
                {
                    return subType;
                }
            }
            return eBlockSubType.None;
        }

        /// <summary>
        /// マスのスイッチ（なければ None）
        /// </summary>
        /// <param name="_subTypes"></param>
        public static eBlockSubType GetSwitch(IReadOnlyList<eBlockSubType> _subTypes)
        {
            foreach (eBlockSubType subType in _subTypes)
            {
                if (IsSwitch(subType))
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
        /// 向きで決まる通り方か（やじるし・一方通行。決まりは同じ）
        /// </summary>
        /// <param name="_subType"></param>
        public static bool IsArrowLike(eBlockSubType _subType)
        {
            return IsArrow(_subType) || IsOneWay(_subType);
        }

        /// <summary>
        /// 通り方を決めるギミックか（やじるし・一方通行・まがる・まっすぐ。1マスに1つだけ）
        /// </summary>
        /// <param name="_subType"></param>
        public static bool IsRoute(eBlockSubType _subType)
        {
            return IsArrowLike(_subType) || _subType == eBlockSubType.Turn || _subType == eBlockSubType.Straight;
        }

        /// <summary>
        /// やじるし・一方通行の向き（マップの座標、y+1 が下）
        /// </summary>
        /// <param name="_subType"></param>
        public static Vector2Int GetArrowDirection(eBlockSubType _subType)
        {
            switch (_subType)
            {
                case eBlockSubType.ArrowUp: case eBlockSubType.OneWayUp: return new Vector2Int(0, -1);
                case eBlockSubType.ArrowDown: case eBlockSubType.OneWayDown: return new Vector2Int(0, 1);
                case eBlockSubType.ArrowLeft: case eBlockSubType.OneWayLeft: return new Vector2Int(-1, 0);
                case eBlockSubType.ArrowRight: case eBlockSubType.OneWayRight: return new Vector2Int(1, 0);
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
        /// 向き → 一方通行
        /// </summary>
        /// <param name="_direction"></param>
        public static eBlockSubType ToOneWay(Vector2Int _direction)
        {
            if (_direction.x > 0) return eBlockSubType.OneWayRight;
            if (_direction.x < 0) return eBlockSubType.OneWayLeft;
            if (_direction.y < 0) return eBlockSubType.OneWayUp;
            if (_direction.y > 0) return eBlockSubType.OneWayDown;
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
            if (IsArrowLike(_route))
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
