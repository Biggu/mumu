using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 経路の今の状態を、マスのギミックの見た目（扉の開閉・こわれる橋）に反映する
    /// 新しいギミックを経路の状態に合わせて見せたい時は、ここに足す
    /// </summary>
    public class GimmickSync
    {
        /// <summary>
        /// マスのギミックの見た目を経路の状態に合わせる
        /// </summary>
        /// <param name="_block"></param>
        /// <param name="_cell"></param>
        /// <param name="_step">経路上の順番（経路外は -1）</param>
        /// <param name="_lockedIndex">こわれた橋の一番後ろの順番</param>
        /// <param name="_path"></param>
        /// <param name="_mapData"></param>
        public void Apply(Block _block, Vector2Int _cell, int _step, int _lockedIndex, PuzzlePath _path, MapData _mapData)
        {
            eBlockSubType exclusive = GimmickUtility.GetExclusive(_mapData.GetBlockSubTypes(_cell.x, _cell.y));
            if (GimmickUtility.IsGate(exclusive))
            {
                _block.SetGateOpen(_path.HasKey(GimmickUtility.GetColorIndex(exclusive)));
            }
            _block.SetCrumbled(exclusive == eBlockSubType.Crumble && _step >= 0 && _step <= _lockedIndex);

            // 霧は、経路がそのマスか隣に来るまで隠れる（巻き戻すとまた隠れる）
            bool hasFog = GimmickUtility.GetFog(_mapData.GetBlockSubTypes(_cell.x, _cell.y)) != eBlockSubType.None;
            _block.SetFogged(hasFog && !isRevealed(_cell, _step, _path));
        }

        /// <summary>
        /// 経路がそのマスに乗っているか、隣に乗っているか（霧を晴らす条件）
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_step"></param>
        /// <param name="_path"></param>
        private bool isRevealed(Vector2Int _cell, int _step, PuzzlePath _path)
        {
            if (_step >= 0)
            {
                return true;
            }
            foreach (Vector2Int direction in GridUtility.Directions4)
            {
                if (_path.IndexOf(_cell + direction) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
