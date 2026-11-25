using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 画面左下の操作の輪（経路をドラッグした向きにつまみが動く）
    /// </summary>
    public class TouchPad : MonoBehaviour
    {
        /// <summary>輪の画像</summary>
        [SerializeField]
        private Image baseImage = null;
        /// <summary>つまみの画像（輪の子に置く）</summary>
        [SerializeField]
        private Image knobImage = null;

        /// <summary>つまみの大きさ（輪の大きさに対する割合）</summary>
        private const float KnobSizeRate = 0.5f;
        /// <summary>つまみの濃さ（輪の透明度に対する倍率。1 を超えたら 1 まで）</summary>
        private const float KnobAlphaRate = 2f;

        /// <summary>経路をドラッグした量（押した場所から今の指まで）</summary>
        private Vector2 dragOffset = Vector2.zero;
        private Canvas canvas = null;

        /// <summary>
        /// 初期化
        /// </summary>
        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// 経路をドラッグした量を設定（つまみはこの向きに動く）
        /// </summary>
        /// <param name="_offset"></param>
        public void SetDragOffset(Vector2 _offset)
        {
            dragOffset = _offset;
        }

        /// <summary>
        /// ドラッグを止めた時に、つまみを中央へ戻す
        /// </summary>
        public void ResetDragOffset()
        {
            dragOffset = Vector2.zero;
        }

        /// <summary>
        /// 毎フレーム、輪とつまみを設定に合わせて表示する
        /// </summary>
        private void Update()
        {
            MapInputSettings settings = MapInputSettings.Current;
            baseImage.enabled = settings.TouchPadEnabled;
            knobImage.enabled = settings.TouchPadEnabled;
            if (!settings.TouchPadEnabled)
            {
                return;
            }

            RectTransform parent = (RectTransform)transform.parent;
            Camera eventCamera = canvas.worldCamera;
            float size = settings.TouchPadSize;
            RectTransform rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(size, size);
            baseImage.color = new Color(1f, 1f, 1f, settings.TouchPadAlpha);

            // 輪は画面の左下（余白 TouchPadMargin）に固定する
            float half = size * 0.5f;
            Vector2 corner = new Vector2(settings.TouchPadMargin + half, settings.TouchPadMargin + half);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, corner, eventCamera, out Vector2 ringLocal))
            {
                return;
            }

            // つまみは経路をドラッグした向き（TouchPadMaxDistance まで）に動く
            RectTransform knobRect = knobImage.rectTransform;
            knobRect.sizeDelta = new Vector2(size * KnobSizeRate, size * KnobSizeRate);
            Vector2 offset = dragOffset;
            if (offset.magnitude > settings.TouchPadMaxDistance)
            {
                offset = offset.normalized * settings.TouchPadMaxDistance;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, corner + offset, eventCamera, out Vector2 knobLocal))
            {
                knobRect.anchoredPosition = knobLocal - ringLocal;
            }
            knobImage.color = new Color(1f, 1f, 1f, Mathf.Min(1f, settings.TouchPadAlpha * KnobAlphaRate));
        }
    }
}
