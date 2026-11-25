using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// マップの外の枠のブロック（土台とデコだけの軽いブロック。経路・ギミックは持たない）
    /// </summary>
    public class RingBlock : MonoBehaviour
    {
        /// <summary>デコの下に出す影（プレハブの子に置いて割り当てる。デコより前に描く）</summary>
        [SerializeField]
        private Image decoShadow = null;
        /// <summary>影の幅（ブロックの幅に対して）</summary>
        private const float ShadowWidthRate = 0.9f;
        /// <summary>影の高さ（ブロックの幅に対して）</summary>
        private const float ShadowHeightRate = 0.45f;

        private Image baseImage = null;
        private Image decoImage = null;
        private SpriteFrameAnimation decoAnimation = null;
        private bool isReady = false;

        /// <summary>
        /// 子の部品を探す（最初の1回だけ）
        /// </summary>
        private void prepare()
        {
            if (isReady)
            {
                return;
            }
            isReady = true;
            baseImage = transform.Find("base").GetComponent<Image>();
            decoImage = transform.Find("deco").GetComponent<Image>();
            decoAnimation = decoImage.GetComponent<SpriteFrameAnimation>();
            baseImage.raycastTarget = false;
            decoImage.raycastTarget = false;
        }

        /// <summary>
        /// 枠の中身を設定して表示する（土台・デコ・デコのコマ送り）
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="_deco"></param>
        /// <param name="_decoFrames"></param>
        /// <param name="_blockWidth"></param>
        public void Show(Sprite _base, Sprite _deco, Sprite[] _decoFrames, float _blockWidth)
        {
            prepare();
            gameObject.SetActive(true);

            baseImage.sprite = _base;
            baseImage.enabled = _base != null;
            baseImage.color = Block.ObstructTint;

            if (_deco == null)
            {
                decoImage.gameObject.SetActive(false);
                decoShadow.gameObject.SetActive(false);
                return;
            }

            // 影はデコの根元（ブロックの真ん中）に、薄く横長に置く
            decoShadow.gameObject.SetActive(true);
            RectTransform shadowRect = decoShadow.rectTransform;
            shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRect.pivot = new Vector2(0.5f, 0.5f);
            shadowRect.anchoredPosition = Vector2.zero;
            shadowRect.sizeDelta = new Vector2(_blockWidth * ShadowWidthRate, _blockWidth * ShadowHeightRate);

            // デコの根元はブロックの真ん中にそろえる（Block と同じ）
            decoImage.gameObject.SetActive(true);
            RectTransform rect = decoImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, _blockWidth * _deco.rect.height / _deco.rect.width);
            decoImage.sprite = _deco;

            if (_decoFrames != null && _decoFrames.Length > 1 && decoAnimation != null)
            {
                decoAnimation.Play(_decoFrames, ThemeSprites.DecoFps);
            }
        }

        /// <summary>
        /// 表示を消す（再利用のため残す）
        /// </summary>
        public void Hide()
        {
            prepare();
            gameObject.SetActive(false);
        }
    }
}
