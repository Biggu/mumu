using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
            if (view == null || map == null || !isInputEnabled)
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

                        // ドラッグ開始：指の位置を画面の中心へ
                        view.MoveHeadTo(_eventData.position, eventCamera);
                        return;
                }
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
        }

        /// <summary>
        /// 経路のドラッグを終了（先端が変わっていたら寄せる）
        /// </summary>
        private void endPathDrag()
        {
            isPathDragging = false;
            if (map.Path.Head != headBeforeDrag)
            {
                view.FocusHead(view.Scale);
            }
        }

/// <summary>
/// 前回位置から今の位置までの方向へ経路を1マス進める（起点は進んだ位置へ移す）
/// カメラは動かさない（戻る時も）
/// </summary>
/// <param name="_position"></param>
private void dragPath(Vector2 _position)
{
    // 待ち時間の間は進まない（起点は動かさないので、指の移動はあとからまとめて効く）
    float delay = MapInputSettings.Current.DragDelayPerBlock;
    if (delay > 0f && Time.unscaledTime - lastStepTime < delay)
    {
        return;
    }
    if (!map.TryGetDragDirection(lastPathPosition, _position, eventCamera, out Vector2Int direction))
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