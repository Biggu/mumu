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
        /// <summary>スイッチ（赤・青・黄。経路で通ると同じ色の扉が開く。カギと同じ）</summary>
        SwitchRed,
        SwitchBlue,
        SwitchYellow,
        /// <summary>一方通行（やじるしと同じ決まり。決めた向きへ進む時だけ通れる）</summary>
        OneWayUp,
        OneWayDown,
        OneWayLeft,
        OneWayRight,
        /// <summary>霧（経路がそのマスか隣に来るまで隠れる。巻き戻すとまた隠れる。見た目だけ）</summary>
        Fog,
    }

    /// <summary>
    /// ポップアップの種類（PopupManager.Open で使う）
    /// </summary>
    public enum ePopup
    {
        /// <summary>OK、または はい・いいえ</summary>
        Alert,
        /// <summary>設定</summary>
        Settings,
        /// <summary>ずかん</summary>
        PugBook,
        /// <summary>2つのボタンの確認（おにわ・ずかんで共通）</summary>
        Confirm,
    }

    /// <summary>
    /// 部品のプレハブの種類（PartsLoaderManager で作る。Resources/Prefabs/Common）
    /// </summary>
    public enum ePart
    {
        /// <summary>もどらずクリアの大きな★</summary>
        BigStar,
        /// <summary>ステージマップの男の子のパグ</summary>
        StagePug,
        /// <summary>マップの中の男の子のパグ（主人公）</summary>
        PlayerPug,
    }
}
