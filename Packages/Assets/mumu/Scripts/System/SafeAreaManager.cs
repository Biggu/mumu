using UnityEngine;

namespace mumu
{
    /// <summary>
    /// セーフエリア内に基準解像度の領域を中央配置
    /// </summary>
    public class SafeAreaManager : MonoBehaviour
    {
        private const float BaseWidth = 1280f;
        private const float BaseHeight = 720f;

        /// <summary>対象（Canvas 自身の RectTransform は Canvas に制御されるため子を指定）</summary>
        [SerializeField]
        private RectTransform safeAreaTransform;

        private Canvas rootCanvas = null;
        private Rect lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private float lastScaleFactor = 0f;

        /// <summary>
        /// 初期化
        /// </summary>
        private void Awake()
        {
            if (safeAreaTransform == null)
            {
                safeAreaTransform = GetComponent<RectTransform>();
            }

            if (safeAreaTransform.GetComponent<Canvas>() != null)
            {
                Debug.LogWarning("[SafeAreaManager] Canvas の RectTransform は対象にできません。子の RectTransform を指定してください。");
            }

            Canvas canvas = safeAreaTransform.GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;

            applySafeArea();
        }

        /// <summary>
        /// 回転・解像度変更・CanvasScaler の反映に追従
        /// </summary>
        private void Update()
        {
            if (Screen.safeArea != lastSafeArea
                || Screen.width != lastScreenSize.x
                || Screen.height != lastScreenSize.y
                || !Mathf.Approximately(getScaleFactor(), lastScaleFactor))
            {
                applySafeArea();
            }
        }

        /// <summary>
        /// Canvas の拡大率
        /// </summary>
        private float getScaleFactor()
        {
            return rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        }

        /// <summary>
        /// セーフエリアを反映
        /// </summary>
        private void applySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastScaleFactor = getScaleFactor();

            if (Screen.width <= 0 || Screen.height <= 0 || lastScaleFactor <= 0f)
            {
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            safeAreaTransform.anchorMin = anchorMin;
            safeAreaTransform.anchorMax = anchorMax;
            safeAreaTransform.pivot = new Vector2(0.5f, 0.5f);

            // ピクセル → Canvas 単位
            float scale = Mathf.Min(safeArea.width / BaseWidth, safeArea.height / BaseHeight) / lastScaleFactor;
            safeAreaTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, BaseWidth * scale);
            safeAreaTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BaseHeight * scale);

            // アンカーがセーフエリアなので 0 で中央
            safeAreaTransform.anchoredPosition = Vector2.zero;
        }
    }
}
