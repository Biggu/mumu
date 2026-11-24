using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// マップの外の枠のブロック（土台とデコだけの軽いブロック。経路・ギミックは持たない）
    /// </summary>
    public class RingBlock : MonoBehaviour
    {
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
                return;
            }

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
