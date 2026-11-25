using UnityEngine;

namespace mumu
{
    /// <summary>
    /// アイソメのブロックの間にパグなどを差し込む描画順
    /// </summary>
    public static class IsoDepth
    {
        /// <summary>
        /// _item を _block のすぐ後ろ（手前のブロックより奥）に並べる（すでにそこなら何もしない）
        /// </summary>
        /// <param name="_item"></param>
        /// <param name="_block"></param>
        public static void PlaceAfter(Transform _item, Transform _block)
        {
            int blockIndex = _block.GetSiblingIndex();
            int index = _item.GetSiblingIndex();

            // 自分が前にいる時は、抜けた分だけブロックの番号が1つ下がる
            int target = index < blockIndex ? blockIndex : blockIndex + 1;
            if (index != target)
            {
                _item.SetSiblingIndex(target);
            }
        }

        /// <summary>
        /// 2つのブロックのうち手前（後から描く方）
        /// </summary>
        /// <param name="_a"></param>
        /// <param name="_b"></param>
        public static Block Front(Block _a, Block _b)
        {
            if (_a == null)
            {
                return _b;
            }
            if (_b == null)
            {
                return _a;
            }
            return _b.SortIndex > _a.SortIndex ? _b : _a;
        }
    }
}
