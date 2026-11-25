using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 共通処理（UiFactory）
    /// </summary>
    public static class UiFactory
    {
        /// <summary>
        /// 画像を作る（中央基準・当たり判定なし・縦横比を保つ）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        public static Image CreateImage(string _name, Transform _parent, Sprite _sprite, Vector2 _size)
        {
            var go = new GameObject(_name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = _sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = _size;
            return image;
        }


        /// <summary>
        /// 正方形の画像を作る
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        public static Image CreateImage(string _name, Transform _parent, Sprite _sprite, float _size)
        {
            return CreateImage(_name, _parent, _sprite, new Vector2(_size, _size));
        }


        /// <summary>
        /// パグの画像の共通設定（縦横比を保つ・当たり判定はオプション）
        /// </summary>
        /// <param name="_image"></param>
        /// <param name="_raycastTarget"></param>
        public static void SetPugImage(Image _image, bool _raycastTarget)
        {
            _image.preserveAspect = true;
            _image.raycastTarget = _raycastTarget;
        }


        /// <summary>
        /// 文字を作る（中央揃え・折り返しなし・当たり判定なし）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_text"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        public static TextMeshProUGUI CreateText(string _name, Transform _parent, string _text, float _size, Color _color)
        {
            var go = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = _text;
            text.fontSize = _size;
            text.color = _color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }


        /// <summary>
        /// 足元を上面の中心に合わせる正方形（パグの見た目の共通設定）
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_size"></param>
        public static void SetFootRect(RectTransform _rect, float _size)
        {
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.08f);
            _rect.sizeDelta = new Vector2(_size, _size);
        }

    }
}
