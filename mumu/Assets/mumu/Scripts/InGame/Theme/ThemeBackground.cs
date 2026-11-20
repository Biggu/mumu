using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// テーマ背景の表示（きらめくコマ送り）
    /// </summary>
    public static class ThemeBackground
    {
        /// <summary>背景のコマ送りの速さ（秒あたりのコマ数）</summary>
        private const float BackgroundFps = 4f;

        /// <summary>
        /// テーマの背景を敷き詰め、コマ送りで動かす
        /// </summary>
        /// <param name="_image"></param>
        /// <param name="_sprites"></param>
        public static void Show(Image _image, ThemeSprites _sprites)
        {
            UiImageUtility.SetTiled(_image, _sprites.Background);
            if (_image == null || _sprites.BackgroundFrames == null)
            {
                return;
            }

            var animation = _image.GetComponent<SpriteFrameAnimation>();
            if (animation == null)
            {
                animation = _image.gameObject.AddComponent<SpriteFrameAnimation>();
            }
            animation.PlayIfChanged(_sprites.BackgroundFrames, BackgroundFps);
        }
    }
}
