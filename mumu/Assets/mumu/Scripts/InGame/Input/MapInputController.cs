using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// マップの操作（経路ドラッグ・マップ移動・ピンチ／ホイールで拡大縮小・ダブルタップで先端へ移動）
    /// 指の追跡と振り分けだけを行い、タップの判定は TapDetector、表示の動きは MapViewController、経路は InGameMap
    /// </summary>
    public class MapInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IDragHandler, IScrollHandler
    {
        private const int NoPointer = int.MinValue;

        /// <summary>ホイール1段あたりの倍率</summary>
        private static float ScrollZoomStep => MapInputSettings.Current.ScrollZoomStep;
        private static float MaxScrollDelta => MapInputSettings.Current.MaxScrollDelta;

        private MapViewController view = null;
        private InGameMap map = null;
        private Camera eventCamera = null;
        private readonly TapDetector tapDetector = new TapDetector();

        /// <summary>経路をドラッグしている指（一時停止中は NoPointer）</summary>
        private int pathPointerId = NoPointer;

        /// <summary>経路のドラッグ中か（一時停止・移動に切り替えていても true）</summary>
        private bool isPathDragging = false;

        /// <summary>経路をドラッグしている指の最新の位置</summary>
        private Vector2 routeFingerPosition = Vector2.zero;

        /// <summary>経路を最後に1マス進めた時刻（ドラッグの待ち時間に使う）</summary>
        private float lastStepTime = float.MinValue;

        private Vector2 lastPathPosition = Vector2.zero;

        /// <summary>経路ドラッグ開始前の先端（離した時に変わっていたらカメラを寄せる）</summary>
        private Vector2Int headBeforeDrag = Vector2Int.zero;

        /// <summary>移動・ピンチ中の指（pointerId → スクリーン座標）</summary>
        private readonly Dictionary<int, Vector2> viewPointers = new Dictionary<int, Vector2>();

        /// <summary>押している指の数</summary>
        private int pressedCount = 0;

        /// <summary>今回の操作中に2本指になったか（ピンチはタップにしない）</summary>
        private bool isMultiTouch = false;

        /// <summary>エディタで Space を押しているか（前フレームの状態）</summary>
        private bool wasSpaceHeld = false;

        /// <summary>入力を受け付けるか</summary>
        private bool isInputEnabled = true;

        /// <summary>経路ドラッグ中のプレビュー線</summary>
        private RectTransform dragPreview = null;

        /// <summary>プレビュー開始時の指の位置（コンテンツ座標）。指の移動量だけを線に反映する</summary>
        private Vector2 dragPreviewStartPointerContent = Vector2.zero;

        /// <summary>プレビュー開始時に記録した経路 Head。Head が変わったらプレビューをリセットする</summary>
        private Vector2Int dragPreviewHead = Vector2Int.zero;

        /// <summary>
        /// 操作対象を設定
        /// </summary>
        public void Setup(MapViewController _view, InGameMap _map)
        {
            view = _view;
            map = _map;
        }

        /// <summary>
        /// 入力の有効・無効（無効にすると操作中の指も解除）
        /// </summary>
        public void SetInputEnabled(bool _isEnabled)
        {
            isInputEnabled = _isEnabled;

            if (!_isEnabled)
            {
                resetPointers();
            }
        }

        /// <summary>
        /// エディタで Space の押下・解放を監視する（経路ドラッグ中の一時停止と再開）
        /// 実機では Space は押されないので何もしない
        /// </summary>
        private void Update()
        {
            bool isSpaceHeld = canPanWithSinglePointer();

            if (isSpaceHeld == wasSpaceHeld)
            {
                return;
            }

            wasSpaceHeld = isSpaceHeld;

            if (!isInputEnabled || !isPathDragging)
            {
                return;
            }

            if (isSpaceHeld && pathPointerId != NoPointer)
            {
                pauseRouteDrag();
            }
            else if (!isSpaceHeld && pathPointerId == NoPointer && viewPointers.Count == 1)
            {
                resumePathDrag();
            }
        }

        /// <summary>
        /// 押下
        /// </summary>
        public void OnPointerDown(PointerEventData _eventData)
        {
            if (view == null || map == null || !isInputEnabled ||
                _eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            view.CancelFocus();
            pressedCount++;

            if (pressedCount == 1)
            {
                isMultiTouch = false;
                tapDetector.OnPress(_eventData.position);
            }
            else
            {
                isMultiTouch = true;
            }

            eventCamera = _eventData.pressEventCamera;

            if (isPathDragging)
            {
                if (pathPointerId != NoPointer)
                {
                    pauseRouteDrag();
                }

                if (viewPointers.Count < 2)
                {
                    viewPointers[_eventData.pointerId] = _eventData.position;
                }

                return;
            }

            if (viewPointers.Count == 0 && !canPanWithSinglePointer())
            {
                Vector2Int headBefore = map.Path.Head;

                switch (map.GetPressTarget(_eventData.position, eventCamera))
                {
                    case ePressTarget.PathHead:
                    case ePressTarget.NearHead:
                        isPathDragging = true;
                        pathPointerId = _eventData.pointerId;
                        routeFingerPosition = _eventData.position;
                        lastPathPosition = _eventData.position;
                        headBeforeDrag = headBefore;
                        startDragPreview(_eventData.position);
                        return;
                }

                // 画面のどこからでも経路ドラッグ開始
                isPathDragging = true;
                pathPointerId = _eventData.pointerId;
                routeFingerPosition = _eventData.position;
                lastPathPosition = _eventData.position;
                headBeforeDrag = headBefore;
                startDragPreview(_eventData.position);
                return;
            }

            if (viewPointers.Count < 2)
            {
                viewPointers[_eventData.pointerId] = _eventData.position;
            }
        }

        /// <summary>
        /// ドラッグ判定のしきい値を無効化
        /// </summary>
        public void OnInitializePotentialDrag(PointerEventData _eventData)
        {
            _eventData.useDragThreshold = false;
        }

        /// <summary>
        /// ドラッグ
        /// </summary>
        public void OnDrag(PointerEventData _eventData)
        {
            if (!isInputEnabled)
            {
                return;
            }

            if (_eventData.pointerId == pathPointerId)
            {
                routeFingerPosition = _eventData.position;
                showDragPreview(_eventData.position);
                dragPath(_eventData.position);
                return;
            }

            if (!viewPointers.TryGetValue(_eventData.pointerId, out Vector2 previous))
            {
                return;
            }

            Vector2 current = _eventData.position;

            if (viewPointers.Count >= 2)
            {
                view.Pinch(previous, current, getOtherPointer(_eventData.pointerId), eventCamera);
            }
            else if (canPanWithSinglePointer())
            {
                view.PanBy(previous, current, eventCamera);
            }

            viewPointers[_eventData.pointerId] = current;
        }

        /// <summary>
        /// 指を離した
        /// </summary>
        public void OnPointerUp(PointerEventData _eventData)
        {
            if (!isInputEnabled)
            {
                return;
            }

            int pointerId = _eventData.pointerId;

            if (isPathDragging)
            {
                if (pointerId == pathPointerId)
                {
                    pathPointerId = NoPointer;

                    if (viewPointers.Count == 0)
                    {
                        endPathDrag();
                    }
                }
                else if (viewPointers.Remove(pointerId))
                {
                    if (viewPointers.Count == 1 && pathPointerId == NoPointer)
                    {
                        resumePathDrag();
                    }
                    else if (viewPointers.Count == 0 && pathPointerId == NoPointer)
                    {
                        endPathDrag();
                    }
                }
            }
            else
            {
                viewPointers.Remove(pointerId);
            }

            pressedCount = Mathf.Max(0, pressedCount - 1);

            if (pressedCount == 0 && !isMultiTouch &&
                tapDetector.OnRelease(_eventData.position))
            {
                view.FocusHead(view.FitScale);
            }
        }

        /// <summary>
        /// ホイールで拡大縮小
        /// </summary>
        public void OnScroll(PointerEventData _eventData)
        {
            if (!isInputEnabled || view == null)
            {
                return;
            }

            view.CancelFocus();
            eventCamera = _eventData.enterEventCamera;

            float delta = Mathf.Clamp(
                _eventData.scrollDelta.y,
                -MaxScrollDelta,
                MaxScrollDelta);

            view.ZoomAt(
                _eventData.position,
                view.Scale * Mathf.Pow(ScrollZoomStep, delta),
                eventCamera);
        }

        /// <summary>
        /// 無効化時に入力状態をリセット
        /// </summary>
        private void OnDisable()
        {
            resetPointers();

            if (view != null)
            {
                view.CancelFocus();
            }
        }

        /// <summary>
        /// 指の状態を初期化
        /// </summary>
        private void resetPointers()
        {
            pathPointerId = NoPointer;
            isPathDragging = false;
            routeFingerPosition = Vector2.zero;
            viewPointers.Clear();
            pressedCount = 0;
            hideDragPreview();
        }

        /// <summary>
        /// 1本の指（マウス）でマップを動かしてよいか
        /// </summary>
        private bool canPanWithSinglePointer()
        {
#if UNITY_EDITOR
            return Input.GetKey(KeyCode.Space);
#else
            return false;
#endif
        }

        /// <summary>
        /// 経路のドラッグを一時停止する
        /// </summary>
        private void pauseRouteDrag()
        {
            hideDragPreview();
            view.MoveHeadTo(routeFingerPosition, eventCamera);

            viewPointers[pathPointerId] = routeFingerPosition;
            pathPointerId = NoPointer;
        }

        /// <summary>
        /// 一時停止していた経路のドラッグを再開する
        /// </summary>
        private void resumePathDrag()
        {
            int remainingId = NoPointer;
            Vector2 remainingPosition = Vector2.zero;

            foreach (var pair in viewPointers)
            {
                remainingId = pair.Key;
                remainingPosition = pair.Value;
            }

            viewPointers.Clear();
            pathPointerId = remainingId;
            routeFingerPosition = remainingPosition;
            lastPathPosition = remainingPosition;
            startDragPreview(remainingPosition);
        }

        /// <summary>
        /// 経路のドラッグを終了
        /// </summary>
        private void endPathDrag()
        {
            isPathDragging = false;
            hideDragPreview();

            if (map.Path.Head != headBeforeDrag)
            {
                view.FocusHead(view.Scale);
            }
        }

        /// <summary>
        /// ドラッグ開始時の指位置を基準として保存する
        /// </summary>
        private void startDragPreview(Vector2 _pointerPosition)
        {
            dragPreviewStartPointerContent =
                view.ScreenToContentPosition(_pointerPosition, eventCamera);

            // 現在のHeadを記録
            dragPreviewHead = map.Path.Head;

            showDragPreview(_pointerPosition);
        }

        /// <summary>
        /// Head から、ドラッグ開始地点からの指の移動量だけプレビュー線を表示する
        /// Head が変わった場合は、プレビューを点にリセットして新しいドラッグを開始する
        /// </summary>
        private void showDragPreview(Vector2 _pointerPosition)
        {
            if (dragPreview == null)
            {
                const float PreviewWidth = 8f;

                Image image = UiFactory.CreateImage(
                    "routeDragPreview",
                    view.transform,
                    null,
                    new Vector2(PreviewWidth, PreviewWidth));

                image.color = new Color(1f, 0.82f, 0.25f, 0.9f);
                image.preserveAspect = false;
                dragPreview = image.rectTransform;
            }

            Vector2 currentPointerContent =
                view.ScreenToContentPosition(_pointerPosition, eventCamera);

            // Head が進んだ／戻った場合は現在の指位置を新しいドラッグ起点にする
            Vector2Int currentHead = map.Path.Head;

            if (currentHead != dragPreviewHead)
            {
                dragPreviewHead = currentHead;
                dragPreviewStartPointerContent = currentPointerContent;
            }

            Vector2 start = map.GetHeadPosition();

            Vector2 dragOffset =
                currentPointerContent - dragPreviewStartPointerContent;

            Vector2 end = start + dragOffset;
            Vector2 diff = end - start;
            float length = diff.magnitude;

            dragPreview.gameObject.SetActive(true);

            // Head 位置に小さな点を表示
            if (length < 0.5f)
            {
                dragPreview.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, 8f);

                dragPreview.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, 8f);

                dragPreview.anchoredPosition = start;
                dragPreview.localRotation = Quaternion.identity;
                return;
            }

            // 移動量と同じ長さのプレビュー線
            dragPreview.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, length);

            dragPreview.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, 8f);

            dragPreview.anchoredPosition = (start + end) * 0.5f;

            dragPreview.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// オブジェクトを残さずプレビュー線を隠す
        /// </summary>
        private void hideDragPreview()
        {
            if (dragPreview != null)
            {
                Destroy(dragPreview.gameObject);
                dragPreview = null;
            }
        }

        /// <summary>
        /// 前回位置から今の位置までの方向へ経路を1マス進める
        /// カメラは動かさない
        /// </summary>
        private void dragPath(Vector2 _position)
        {
            float delay = MapInputSettings.Current.DragDelayPerBlock;

            if (delay > 0f && Time.unscaledTime - lastStepTime < delay)
            {
                return;
            }

            if (!map.TryGetDragDirection(
                lastPathPosition,
                _position,
                eventCamera,
                out Vector2Int direction))
            {
                return;
            }

            map.DragStep(direction);
            lastPathPosition = _position;
            lastStepTime = Time.unscaledTime;
        }

        /// <summary>
        /// もう1本の指の位置
        /// </summary>
        private Vector2 getOtherPointer(int _pointerId)
        {
            foreach (var pair in viewPointers)
            {
                if (pair.Key != _pointerId)
                {
                    return pair.Value;
                }
            }

            return Vector2.zero;
        }
    }
}