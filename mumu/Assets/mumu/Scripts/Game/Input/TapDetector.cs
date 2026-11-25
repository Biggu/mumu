using UnityEngine;

namespace mumu
{
    /// <summary>
    /// タップとダブルタップの判定（押した位置・時間と、前回のタップ）
    /// </summary>
    public class TapDetector
    {
        /// <summary>タップとみなす最大移動量（スクリーン px）</summary>
        private static float TapMoveThreshold => MapInputSettings.Current.TapMoveThreshold;
        /// <summary>タップとみなす最大押下時間（秒）</summary>
        private static float TapMaxDuration => MapInputSettings.Current.TapMaxDuration;
        /// <summary>ダブルタップの間隔（秒）</summary>
        private static float DoubleTapInterval => MapInputSettings.Current.DoubleTapInterval;
        /// <summary>ダブルタップの2回の距離（スクリーン px）</summary>
        private static float DoubleTapDistance => MapInputSettings.Current.DoubleTapDistance;

        private Vector2 pressPosition = Vector2.zero;
        private float pressTime = 0f;
        private Vector2 lastTapPosition = Vector2.zero;
        private float lastTapTime = float.NegativeInfinity;

        /// <summary>
        /// 指を置いた（1本目の指だけ使う）
        /// </summary>
        /// <param name="_position"></param>
        public void OnPress(Vector2 _position)
        {
            pressPosition = _position;
            pressTime = Time.unscaledTime;
        }

        /// <summary>
        /// 指を離した。ダブルタップなら true（次の判定はやり直す）
        /// </summary>
        /// <param name="_position"></param>
        public bool OnRelease(Vector2 _position)
        {
            bool isTap = Vector2.Distance(_position, pressPosition) <= TapMoveThreshold
                && Time.unscaledTime - pressTime <= TapMaxDuration;
            if (!isTap)
            {
                lastTapTime = float.NegativeInfinity;
                return false;
            }

            if (Time.unscaledTime - lastTapTime <= DoubleTapInterval && Vector2.Distance(_position, lastTapPosition) <= DoubleTapDistance)
            {
                lastTapTime = float.NegativeInfinity;
                return true;
            }

            lastTapTime = Time.unscaledTime;
            lastTapPosition = _position;
            return false;
        }
    }
}
