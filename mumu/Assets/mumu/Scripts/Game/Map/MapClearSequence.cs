using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// クリア演出（走るパグが着いたら2匹が喜び、Start から順にブロックが跳ねる）
    /// </summary>
    public class MapClearSequence
    {
        /// <summary>クリア時の跳ねる間隔（秒）</summary>
        private const float WaveInterval = 0.06f;
        /// <summary>クリア時、走るパグの到着を待つ最大時間（秒）</summary>
        private const float ArriveTimeout = 3f;

        /// <summary>
        /// クリア演出を再生
        /// </summary>
        /// <param name="_playerPug">走るパグ（いなければ待たない）</param>
        /// <param name="_markGoalCleared">Goal のブロックをクリア状態にする処理</param>
        /// <param name="_cells">経路のマス（Start から順）</param>
        /// <param name="_getBlock">マスのブロックを取得する処理</param>
        /// <param name="_token"></param>
        public async UniTask PlayAsync(PlayerPug _playerPug, Action _markGoalCleared, IReadOnlyList<Vector2Int> _cells, Func<Vector2Int, Block> _getBlock, CancellationToken _token)
        {
            if (_playerPug != null)
            {
                float waited = 0f;
                while (!_playerPug.IsArrived && waited < ArriveTimeout)
                {
                    if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                    {
                        return;
                    }
                    waited += Time.unscaledDeltaTime;
                }
                _playerPug.SetHappy(true);
            }

            _markGoalCleared?.Invoke();

            for (int i = 0; i < _cells.Count; i++)
            {
                _getBlock(_cells[i]).PlayCelebrate();

                if (await UniTask.Delay(TimeSpan.FromSeconds(WaveInterval), cancellationToken: _token).SuppressCancellationThrow())
                {
                    return;
                }
            }
        }
    }
}
