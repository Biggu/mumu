using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 経路を動いた時の効果音（手数が増えるほどピッチが上がる）
    /// </summary>
    public class MoveSound
    {
        /// <summary>1手ごとの効果音ピッチの上がり幅</summary>
        private const float StepPitchRate = 0.04f;
        /// <summary>効果音ピッチの上限</summary>
        private const float MaxStepPitch = 2f;

        /// <summary>
        /// 移動の結果に合わせて効果音を鳴らす
        /// </summary>
        /// <param name="_result"></param>
        /// <param name="_moveCount"></param>
        /// <param name="_isGoalUnlocked"></param>
        /// <param name="_isCheckpointPassed"></param>
        public void Play(eMoveResult _result, int _moveCount, bool _isGoalUnlocked, bool _isCheckpointPassed)
        {
            SoundManager sound = SoundManager.Instance;
            if (sound == null)
            {
                return;
            }

            float pitch = Mathf.Min(MaxStepPitch, 1f + _moveCount * StepPitchRate);
            switch (_result)
            {
                case eMoveResult.Added:
                    sound.PlaySe(eSe.Step, pitch);
                    break;
                case eMoveResult.Undone:
                    sound.PlaySe(eSe.Undo, pitch);
                    break;
                case eMoveResult.Cleared:
                    sound.PlaySe(eSe.Clear);
                    return;
            }

            if (_isCheckpointPassed || _isGoalUnlocked)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
        }
    }
}
