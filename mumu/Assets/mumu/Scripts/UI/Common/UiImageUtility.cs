using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// Image の設定
    /// </summary>
    public static class UiImageUtility
    {
        /// <summary>
        /// 画像をタイル表示
        /// </summary>
        /// <param name="_image"></param>
        /// <param name="_sprite"></param>
        public static void SetTiled(Image _image, Sprite _sprite)
        {
            if (_image == null || _sprite == null)
            {
                return;
            }

            _image.sprite = _sprite;
            _image.type = Image.Type.Tiled;
            _image.color = Color.white;
        }
    }
}
