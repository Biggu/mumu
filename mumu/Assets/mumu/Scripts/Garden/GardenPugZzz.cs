using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// お昼寝中のパグの頭の上に浮かぶ「z」
    /// </summary>
    public class GardenPugZzz
    {
        private const float Duration = 1.6f;
        private const float Rise = 28f;
        private const float Y = 42f;
        private const float FontSize = 22f;
        private static readonly Color Color = new Color(0.35f, 0.45f, 0.85f);

        /// <summary>
        /// z を1つ浮かべる（パグの向きに合わせて左右を反転させる）
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_pugScaleX"></param>
        /// <param name="_token"></param>
        public void Spawn(Transform _parent, float _pugScaleX, CancellationToken _token)
        {
            TextMeshProUGUI text = UiFactory.CreateText("text", _parent, "z", FontSize, Color);
            var zRect = (RectTransform)text.transform;
            zRect.anchorMin = zRect.anchorMax = new Vector2(0.5f, 0f);
            zRect.anchoredPosition = new Vector2(10f, Y);
            zRect.sizeDelta = new Vector2(30f, 30f);
            zRect.localScale = new Vector3(_pugScaleX, 1f, 1f);
            UiTween.MoveFade(zRect, text, new Vector2(10f, Rise), Duration, _token).Forget();
        }
    }
}
