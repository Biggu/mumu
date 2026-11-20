using System;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 進めなかった時の演出（Goal の手数不足・ギミックの決まり・壊れた橋）。同じマスでは1回だけ
    /// </summary>
    public class RefusalFeedback
    {
        private readonly Func<Vector2Int, Block> getBlock;
        private readonly Action<string> onMessage;

        /// <summary>演出を出したマス</summary>
        private Vector2Int refusedCell = GridUtility.NoCell;

        /// <summary>
        /// 演出に使うブロックの取得とお知らせ
        /// </summary>
        /// <param name="_getBlock"></param>
        /// <param name="_onMessage"></param>
        public RefusalFeedback(Func<Vector2Int, Block> _getBlock, Action<string> _onMessage)
        {
            getBlock = _getBlock;
            onMessage = _onMessage;
        }

        /// <summary>
        /// 動けた時などに、演出済みのマスを忘れる
        /// </summary>
        public void Reset()
        {
            refusedCell = GridUtility.NoCell;
        }

        /// <summary>
        /// 進めなかった理由（Goal の手数不足・ギミック）を演出する
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_path"></param>
        /// <param name="_mapData"></param>
        public void Check(Vector2Int _cell, PuzzlePath _path, MapData _mapData)
        {
            checkGoal(_cell, _path, _mapData);
            CheckGimmick(_cell, _path);
        }

        /// <summary>
        /// ギミック（やじるし・まがる・まっすぐ・扉・壊れた橋）で進めなかった演出
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_path"></param>
        public void CheckGimmick(Vector2Int _cell, PuzzlePath _path)
        {
            if (_cell == refusedCell)
            {
                return;
            }

            // こわれた橋より前へは戻れない
            if (_path.IsRewindLocked(_cell))
            {
                refusedCell = _cell;
                playRefused(_cell, _path);
                onMessage?.Invoke("TOAST_CRUMBLE_LOCKED");
                return;
            }

            bool isGate = _path.IsBlockedByGate(_cell);
            if (!isGate && !_path.IsBlockedByGimmick(_cell))
            {
                return;
            }

            refusedCell = _cell;
            if (isGate)
            {
                onMessage?.Invoke("TOAST_GATE");
            }
            playRefused(_cell, _path);
        }

        /// <summary>
        /// 手数不足で Goal に入れなかった演出
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_path"></param>
        /// <param name="_mapData"></param>
        private void checkGoal(Vector2Int _cell, PuzzlePath _path, MapData _mapData)
        {
            bool isRefused = !_path.IsCleared
                && _mapData.GetBlockType(_cell.x, _cell.y) == eBlockType.Goal
                && MapData.IsAdjacent(_path.Head, _cell)
                && !_path.CanEnterGoal;

            if (!isRefused)
            {
                if (_cell != _path.Head)
                {
                    refusedCell = GridUtility.NoCell;
                }
                return;
            }

            // 同じマスに乗っている間は1回だけ
            if (_cell == refusedCell)
            {
                return;
            }

            refusedCell = _cell;
            playRefusedBase(_path);
            getBlock(_cell).ShowMessage($"Need {_path.RequiredMoveCount - _path.MoveCount}");
        }

        /// <summary>
        /// ギミックで進めなかった演出（音・先端が揺れる・ギミックが跳ねる）
        /// </summary>
        /// <param name="_cell"></param>
        /// <param name="_path"></param>
        private void playRefused(Vector2Int _cell, PuzzlePath _path)
        {
            playRefusedBase(_path);
            getBlock(_path.Head).PunchGimmick();
            getBlock(_cell).PunchGimmick();
        }

        /// <summary>
        /// 進めなかった共通の演出（音と先端の揺れ）
        /// </summary>
        /// <param name="_path"></param>
        private void playRefusedBase(PuzzlePath _path)
        {
            CommonUtility.PlaySe(eSe.Refused);
            getBlock(_path.Head).PlayShake();
        }
    }
}
