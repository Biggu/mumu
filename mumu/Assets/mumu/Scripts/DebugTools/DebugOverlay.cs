#if UNITY_EDITOR
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// デバッグメニューの画面（全画面。D キーで開閉）。項目と操作は DebugTools。エディタ専用
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private const int SortingOrder = 10000;
        private const int Columns = 2;
        private const float ButtonWidth = 520f;
        private const float ButtonHeight = 120f;
        private const float Spacing = 20f;
        private const float TitleHeight = 110f;
        private const float ListMargin = 30f;

        private GameObject panel = null;

        /// <summary>
        /// 画面を作って閉じておく
        /// </summary>
        private void Awake()
        {
            buildCanvas();
            panel.SetActive(false);
        }

        /// <summary>
        /// D キーで開閉し、キーのデバッグ操作を行う
        /// </summary>
        private void Update()
        {
            DebugTools.Update();
            if (Input.GetKeyDown(KeyCode.D))
            {
                panel.SetActive(!panel.activeSelf);
            }
        }

        /// <summary>
        /// 全画面のパネル（題名・スクロールできるボタンの一覧）を作る
        /// </summary>
        private void buildCanvas()
        {
            var canvasGo = new GameObject("DebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // 全画面（黒く半透明）
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            stretch((RectTransform)panel.transform, Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.92f);

            // 題名
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(0f, -TitleHeight);
            titleRect.offsetMax = Vector2.zero;
            var title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "DEBUG  （D で閉じる）";
            title.fontSize = 48f;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Center;

            // スクロールできる一覧
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = (RectTransform)scrollGo.transform;
            stretch(scrollRect, new Vector2(ListMargin, ListMargin), new Vector2(-ListMargin, -TitleHeight));
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            stretch((RectTransform)viewportGo.transform, Vector2.zero, Vector2.zero);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(ButtonWidth, ButtonHeight);
            grid.spacing = new Vector2(Spacing, Spacing);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewportGo.transform;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            foreach (DebugItem item in DebugTools.Items)
            {
                createButton(contentRect, item);
            }
        }

        /// <summary>
        /// 親いっぱいに広げる（左下と右上の余白を指定）
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_min"></param>
        /// <param name="_max"></param>
        private static void stretch(RectTransform _rect, Vector2 _min, Vector2 _max)
        {
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = _min;
            _rect.offsetMax = _max;
        }

        /// <summary>
        /// 項目1つ分のボタン（ボタンには項目の Press を登録する）
        /// </summary>
        /// <param name="_content"></param>
        /// <param name="_item"></param>
        private static void createButton(Transform _content, DebugItem _item)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_content, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 0.95f, 0.8f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(_item.Press);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            stretch((RectTransform)textGo.transform, Vector2.zero, Vector2.zero);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = _item.Label;
            text.fontSize = 40f;
            text.color = new Color(0.29f, 0.17f, 0.06f);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22f;
            text.fontSizeMax = 40f;
        }
    }
}
#endif
