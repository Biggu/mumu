using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// Image のコマ送りアニメーション
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SpriteFrameAnimation : MonoBehaviour
    {
        private Image image = null;
        private Sprite[] frames = null;
        /// <summary>最後に指定されたコマ（同じなら切り替えない判定用）</summary>
        private Sprite[] lastFrames = null;
        private float frameTime = 0.1f;
        private float elapsed = 0f;
        private int index = 0;

        /// <summary>
        /// 前と違うコマの時だけ再生し直す（同じなら続ける）
        /// </summary>
        /// <param name="_frames"></param>
        /// <param name="_fps"></param>
        public void PlayIfChanged(Sprite[] _frames, float _fps)
        {
            if (_frames != lastFrames)
            {
                Play(_frames, _fps);
            }
        }

        /// <summary>
        /// 再生（ループ）
        /// </summary>
        /// <param name="_frames"></param>
        /// <param name="_fps"></param>
        public void Play(Sprite[] _frames, float _fps)
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            frames = _frames;
            lastFrames = _frames;
            frameTime = 1f / Mathf.Max(0.01f, _fps);
            elapsed = 0f;
            index = 0;
            apply();
        }

        /// <summary>
        /// コマ送り
        /// </summary>
        private void Update()
        {
            if (frames == null || frames.Length <= 1)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            while (elapsed >= frameTime)
            {
                elapsed -= frameTime;
                index = (index + 1) % frames.Length;
                apply();
            }
        }

        /// <summary>
        /// 現在のコマを表示
        /// </summary>
        private void apply()
        {
            if (frames != null && frames.Length > 0)
            {
                image.sprite = frames[index];
            }
        }
    }
}
