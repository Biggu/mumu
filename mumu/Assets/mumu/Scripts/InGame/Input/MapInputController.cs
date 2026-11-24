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
        /// <summary>経路をドラッグしている指の下の淡い丸（離すと消える）</summary>
        private RectTransform touchPad = null;
        /// <summary>経路をドラッグした量（押した場所から今の指まで。つまみはこの向きに動く）</summary>
        private Vector2 touchPadDragOffset = Vector2.zero;
        private const string TouchPadPath = "Prefabs/Common/touch_pad";
        private const string TouchPadSpritePath = "Sprites/UI/ui_button_round";
        /// <summary>つまみの大きさ（輪の大きさに対する割合）</summary>
        private const float KnobSizeRate = 0.5f;
        /// <summary>つまみの濃さ（輪の透明度に対する倍率。1 を超えたら 1 まで）</summary>
        private const float KnobAlphaRate = 2f;
        /// <summary>プレビュー開始時の指の位置（コンテンツ座標）。指の移動量だけを線に反映する</summary>
        private Vector2 dragPreviewStartPointerContent = Vector2.zero;
        /// <summary>プレビュー開始時に記録した経路 Head。Head が変わったらプレビューをリセットする</summary>
        private Vector2Int dragPreviewHead = Vector2Int.zero;


        /// <summary>最後に自動フォーカスした時刻（連続発生防止）</summary>
        private float lastAutoFocusTime = float.NegativeInfinity;

        /// <summary>
        /// 操作対象を設定
        /// </summary>
        /// <param name="_view"></param>
        /// <param name="_map"></param>
        public void Setup(MapViewController _view, InGameMap _map)
        {
            view = _view;
            map = _map;
        }

        /// <summary>
        /// 入力の有効・無効（無効にすると操作中の指も解除）
        /// </summary>
        /// <param name="_isEnabled"></param>
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
            refreshTouchPad();
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
                // Space を押した：経路のドラッグを止めて、マウスで移動に切り替える
                pauseRouteDrag();
            }
            else if (!isSpaceHeld && pathPointerId == NoPointer && viewPointers.Count == 1)
            {
                // Space を離した（マウスは押したまま）：経路のドラッグを再開する
                resumePathDrag();
            }
        }

        /// <summary>
        /// 押下（1本目の指が経路上なら経路ドラッグ。ドラッグ中に2本目が来たら移動・ピンチに切り替える）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerDown(PointerEventData _eventData)
        {
            if (view == null || map == null || !isInputEnabled || _eventData.button != PointerEventData.InputButton.Left)
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

            // 経路のドラッグ中に指が増えた：経路のドラッグを一時停止して、移動・ピンチに切り替える
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

            // 経路ドラッグは、エディタでスペースを押していない時だけ始められる
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

                // 経路上でも周辺でもない場所でも経路ドラッグを始める。
                // この分岐ではマップ移動用の viewPointers に登録しない。
                isPathDragging = true;
                pathPointerId = _eventData.pointerId;
                routeFingerPosition = _eventData.position;
                lastPathPosition = _eventData.position;
                headBeforeDrag = headBefore;
                startDragPreview(_eventData.position);
                return;
            }

            // 移動・ピンチ用に登録（最大2本）
            if (viewPointers.Count < 2)
            {
                viewPointers[_eventData.pointerId] = _eventData.position;
            }
        }

        /// <summary>
        /// ドラッグ判定のしきい値を無効化
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnInitializePotentialDrag(PointerEventData _eventData)
        {
            _eventData.useDragThreshold = false;
        }

        /// <summary>
        /// ドラッグ（経路・ピンチ・エディタのスペース押下中の移動）
        /// </summary>
        /// <param name="_eventData"></param>
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
                touchPadDragOffset = _eventData.position - _eventData.pressPosition;
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
                // 2本指：移動と拡大縮小
                view.Pinch(previous, current, getOtherPointer(_eventData.pointerId), eventCamera);
            }
            else if (canPanWithSinglePointer())
            {
                // エディタでスペース押下中：1本のマウスで移動
                view.PanBy(previous, current, eventCamera);
            }

            // 動かさなかった時も位置を更新する。後から2本目の指が来た時に、大きくずれないようにするため
            viewPointers[_eventData.pointerId] = current;
        }

        /// <summary>
        /// 指を離した（2本指から1本になったら経路のドラッグを再開。全部離したら終了し、タップならダブルタップ判定）
        /// </summary>
        /// <param name="_eventData"></param>
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
                    // 経路の指を離した（他に指がなければ終了）
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
                        // 2本指（または Space 移動）から1本へ：指を離さずに経路のドラッグを続ける
                        resumePathDrag();
                    }
                    else if (viewPointers.Count == 0 && pathPointerId == NoPointer)
                    {
                        // 一時停止中の指を全部離した：終了
                        endPathDrag();
                    }
                }
            }
            else
            {
                viewPointers.Remove(pointerId);
            }

            pressedCount = Mathf.Max(0, pressedCount - 1);

            if (pressedCount == 0 && !isMultiTouch && tapDetector.OnRelease(_eventData.position))
            {
                view.FocusHead(view.FitScale);
            }
        }

        /// <summary>
        /// ホイールで拡大縮小
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnScroll(PointerEventData _eventData)
        {
            if (!isInputEnabled || view == null)
            {
                return;
            }

            view.CancelFocus();
            eventCamera = _eventData.enterEventCamera;
            float delta = Mathf.Clamp(_eventData.scrollDelta.y, -MaxScrollDelta, MaxScrollDelta);
            view.ZoomAt(_eventData.position, view.Scale * Mathf.Pow(ScrollZoomStep, delta), eventCamera);
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
        /// 1本の指（マウス）でマップを動かしてよいか。
        /// 実機では不可（2本指のみ）。エディタではスペースを押している間だけ可
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
        /// 経路のドラッグを一時停止する。先端を1本目の指の位置へ移し、その指は移動用の指として登録し直す
        /// </summary>
        private void pauseRouteDrag()
        {
            hideDragPreview();
            view.MoveHeadTo(routeFingerPosition, eventCamera);

            viewPointers[pathPointerId] = routeFingerPosition;
            pathPointerId = NoPointer;
        }

        /// <summary>
        /// 一時停止していた経路のドラッグを、残った1本の指で再開する（開始位置・回数はそのまま続ける）
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
        /// 経路のドラッグを終了（先端が変わっていたら寄せる）
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
        /// ドラッグ開始時の指位置を基準として保存する。開始時のプレビューは Head 上の点になる。
        /// </summary>
        private void startDragPreview(Vector2 _pointerPosition)
        {
            dragPreviewStartPointerContent = view.ScreenToContentPosition(_pointerPosition, eventCamera);
            dragPreviewHead = map.Path.Head;
            showDragPreview(_pointerPosition);
        }

        /// <summary>
        /// Head から、ドラッグ開始地点からの指の移動量だけプレビュー線を表示する。
        /// </summary>
        private void showDragPreview(Vector2 _pointerPosition)
        {
            if (dragPreview == null)
            {
                const float PreviewWidth = 8f;
                Image image = UiFactory.CreateImage("routeDragPreview", view.transform, null, new Vector2(PreviewWidth, PreviewWidth));
                image.color = new Color(1f, 0.82f, 0.25f, 0.9f);
                image.preserveAspect = false;
                dragPreview = image.rectTransform;
            }

            Vector2 currentPointerContent = view.ScreenToContentPosition(_pointerPosition, eventCamera);

            // Head が進んだ／戻った場合は、現在の指位置を新しいドラッグ起点にしてプレビューを点へリセットする。
            Vector2Int currentHead = map.Path.Head;
            if (currentHead != dragPreviewHead)
            {
                dragPreviewHead = currentHead;
                dragPreviewStartPointerContent = currentPointerContent;
            }

            Vector2 start = map.GetHeadPosition();
            Vector2 dragOffset = currentPointerContent - dragPreviewStartPointerContent;
            Vector2 end = start + dragOffset;
            Vector2 diff = end - start;
            float length = diff.magnitude;

            dragPreview.gameObject.SetActive(true);

            // Drag Mode 開始直後／指が動いていない時は Head 上に小さな点を表示する。
            if (length < 0.5f)
            {
                dragPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 8f);
                dragPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 8f);
                dragPreview.anchoredPosition = start;
                dragPreview.localRotation = Quaternion.identity;
                return;
            }

            dragPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length);
            dragPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 8f);
            dragPreview.anchoredPosition = (start + end) * 0.5f;
            dragPreview.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
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
            touchPadDragOffset = Vector2.zero;
        }

        /// <summary>
        /// 画面の左下に、いつも輪を出す（設定でオフにできる）。経路をドラッグした向きにつまみが動く
        /// </summary>
        private void refreshTouchPad()
        {
            MapInputSettings settings = MapInputSettings.Current;
            var parent = view != null ? view.transform as RectTransform : null;
            if (!settings.TouchPadEnabled || parent == null)
            {
                hideTouchPad();
                return;
            }

            if (touchPad == null)
            {
                GameObject prefab = ResourceManager.Instance.Load<GameObject>(TouchPadPath);
                if (prefab == null)
                {
                    return;
                }
                GameObject go = Instantiate(prefab, parent, false);
                touchPad = (RectTransform)go.transform;
                Sprite circle = ResourceManager.Instance.Load<Sprite>(TouchPadSpritePath);
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = circle;
                }
                Transform knobTransform = go.transform.Find("knob");
                if (knobTransform != null)
                {
                    knobTransform.GetComponent<Image>().sprite = circle;
                }
            }

            touchPad.gameObject.SetActive(true);
            touchPad.sizeDelta = new Vector2(settings.TouchPadSize, settings.TouchPadSize);
            Image padImage = touchPad.GetComponent<Image>();
            if (padImage != null)
            {
                padImage.color = new Color(1f, 1f, 1f, settings.TouchPadAlpha);
            }

            // 輪は画面の左下（余白 TouchPadMargin）に固定する
            float half = settings.TouchPadSize * 0.5f;
            Vector2 corner = new Vector2(settings.TouchPadMargin + half, settings.TouchPadMargin + half);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, corner, eventCamera, out Vector2 ringLocal))
            {
                return;
            }
            touchPad.anchoredPosition = ringLocal;

            // つまみは経路をドラッグした向き（TouchPadMaxDistance まで）に動く
            Transform knob = touchPad.Find("knob");
            if (knob == null)
            {
                return;
            }
            var knobRect = (RectTransform)knob;
            knobRect.sizeDelta = new Vector2(settings.TouchPadSize * KnobSizeRate, settings.TouchPadSize * KnobSizeRate);
            Vector2 offset = touchPadDragOffset;
            if (offset.magnitude > settings.TouchPadMaxDistance)
            {
                offset = offset.normalized * settings.TouchPadMaxDistance;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, corner + offset, eventCamera, out Vector2 knobLocal))
            {
                knobRect.anchoredPosition = knobLocal - ringLocal;
            }
            Image knobImage = knob.GetComponent<Image>();
            if (knobImage != null)
            {
                knobImage.color = new Color(1f, 1f, 1f, Mathf.Min(1f, settings.TouchPadAlpha * KnobAlphaRate));
            }
        }

        /// <summary>
        /// 輪を消す（設定でオフの時）
        /// </summary>
        private void hideTouchPad()
        {
            if (touchPad != null)
            {
                touchPad.gameObject.SetActive(false);
            }
        }

/// <summary>
/// 前回位置から今の位置までの方向へ経路を1マス進める（起点は進んだ位置へ移す）
/// カメラは動かさない（戻る時も）
/// </summary>
/// <param name="_position"></param>
private void dragPath(Vector2 _position)
        {
            if (!map.TryGetDragDirection(lastPathPosition, _position, eventCamera, out Vector2Int direction))
            {
                return;
            }

            // Wait between route steps, without changing the drag origin. Going back uses its own delay.
            MapInputSettings settings = MapInputSettings.Current;
            float delay = map.IsRewindDirection(direction) ? settings.DragRewindDelayPerBlock : settings.DragDelayPerBlock;
            if (delay > 0f && Time.unscaledTime - lastStepTime < delay)
            {
                return;
            }

            // Capture the Head's viewport position before the route step.
            Vector2 previousHeadViewportPosition = view.GetHeadViewportPosition();

            map.DragStep(direction);
            lastPathPosition = _position;
            lastStepTime = Time.unscaledTime;

            checkAutoFocusNearEdge(previousHeadViewportPosition);
        }


        /// <summary>
        /// When the Head enters the configured edge margin while moving toward that edge,
        /// focus it using the existing FocusHead behavior.
        /// </summary>
        private void checkAutoFocusNearEdge(Vector2 _previousHeadViewportPosition)
        {
            MapInputSettings settings = MapInputSettings.Current;
            if (!settings.AutoFocusNearEdge)
            {
                return;
            }

            if (Time.unscaledTime - lastAutoFocusTime < settings.AutoFocusCooldown)
            {
                return;
            }

            Vector2 currentHeadViewportPosition = view.GetHeadViewportPosition();
            if (!view.IsHeadMovingTowardEdge(
                _previousHeadViewportPosition,
                currentHeadViewportPosition,
                settings.AutoFocusEdgeMarginRate))
            {
                return;
            }

            lastAutoFocusTime = Time.unscaledTime;
            view.FocusHead(view.Scale);
        }

        /// <summary>
        /// もう1本の指の位置
        /// </summary>
        /// <param name="_pointerId"></param>
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