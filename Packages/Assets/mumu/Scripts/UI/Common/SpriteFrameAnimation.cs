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
        private float frameTime = 0.1f;
        private float elapsed = 0f;
        private int index = 0;

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
