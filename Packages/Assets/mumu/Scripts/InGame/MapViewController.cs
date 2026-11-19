using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace mumu
{
    /// <summary>
    /// マップの入力（経路ドラッグ・マップ移動・ピンチ／ホイールで拡大縮小・ダブルタップで先端へ移動）
    /// </summary>
    public class MapViewController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IDragHandler, IScrollHandler
    {
        private const int NoPointer = int.MinValue;

        /// <summary>高速ドラッグでマスを飛ばさないための補間間隔（スクリーン px）</summary>
        private const float SampleStep = 5f;
        /// <summary>初期表示でマップが画面に占める割合（横）</summary>
        private const float FitRateX = 0.85f;
        /// <summary>初期表示でマップが画面に占める割合（縦）</summary>
        private const float FitRateY = 0.75f;
        /// <summary>初期倍率に対する最小倍率</summary>
        private const float MinZoomRate = 0.6f;
        /// <summary>初期倍率に対する最大倍率</summary>
        private const float MaxZoomRate = 2.5f;
        /// <summary>ホイール1段あたりの倍率</summary>
        private const float ScrollZoomStep = 1.1f;
        private const float MaxScrollDelta = 3f;
        /// <summary>マップが画面より小さい時、中心を画面端の何割まで動かせるか</summary>
        private const float PanLimitRate = 0.8f;

        /// <summary>タップとみなす最大移動量（スクリーン px）</summary>
        private const float TapMoveThreshold = 20f;
        /// <summary>タップとみなす最大押下時間（秒）</summary>
        private const float TapMaxDuration = 0.3f;
        /// <summary>ダブルタップの間隔（秒）</summary>
        private const float DoubleTapInterval = 0.35f;
        /// <summary>ダブルタップの2回の距離（スクリーン px）</summary>
        private const float DoubleTapDistance = 80f;
        /// <summary>ダブルタップ時の移動時間（秒）</summary>
        private const float FocusDuration = 0.35f;

        /// <summary>移動・拡大する対象（自身）</summary>
        private RectTransform content = null;
        private RectTransform viewport = null;
        private InGameMap map = null;
        private Rect bounds;
        private float fitScale = 1f;
        private float minScale = 1f;
        private float maxScale = 1f;

        private int pathPointerId = NoPointer;
        private Vector2 lastPathPosition = Vector2.zero;
        /// <summary>移動・ピンチ中の指（pointerId → スクリーン座標）</summary>
        private readonly Dictionary<int, Vector2> viewPointers = new Dictionary<int, Vector2>();
        private Camera eventCamera = null;

        /// <summary>押している指の数</summary>
        private int pressedCount = 0;
        /// <summary>今回の操作中に2本指になったか（ピンチはタップにしない）</summary>
        private bool isMultiTouch = false;
        private Vector2 pressPosition = Vector2.zero;
        private float pressTime = 0f;
        private Vector2 lastTapPosition = Vector2.zero;
        private float lastTapTime = float.NegativeInfinity;

        private CancellationTokenSource focusCts = null;
        /// <summary>経路ドラッグ開始前の先端（離した時に変わっていたらカメラを寄せる）</summary>
        private Vector2Int headBeforeDrag = Vector2Int.zero;
        /// <summary>入力を受け付けるか</summary>
        private bool isInputEnabled = true;

        /// <summary>
        /// 初期化（マップ全体が入る倍率で Start を中心に表示）
        /// </summary>
        /// <param name="_viewport"></param>
        /// <param name="_map"></param>
        public void Setup(RectTransform _viewport, InGameMap _map)
        {
            content = (RectTransform)transform;
            viewport = _viewport;
            map = _map;
            bounds = map.GetContentBounds();

            fitScale = Mathf.Min(
                viewport.rect.width * FitRateX / bounds.width,
                viewport.rect.height * FitRateY / bounds.height);
            minScale = fitScale * MinZoomRate;
            maxScale = fitScale * MaxZoomRate;

            setScale(fitScale);
            content.anchoredPosition = clamp(-map.GetHeadPosition() * fitScale, fitScale);
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
                pathPointerId = NoPointer;
                viewPointers.Clear();
                pressedCount = 0;
            }
        }

        /// <summary>
        /// 指定位置（マップの座標）が中心に来るよう、画面に _visibleWidth の幅が映る倍率まで寄る
        /// </summary>
        /// <param name="_position"></param>
        /// <param name="_visibleWidth"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        public UniTask ZoomToAsync(Vector2 _position, float _visibleWidth, float _duration, CancellationToken _token)
        {
            cancelFocus();
            float scale = viewport.rect.width / Mathf.Max(1f, _visibleWidth);
            return focusAsync(clamp(-_position * scale, scale), scale, _duration, _token);
        }

        /// <summary>
        /// 押下（1本目の指が経路上なら経路ドラッグ、それ以外はマップ移動）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerDown(PointerEventData _eventData)
        {
            if (map == null || !isInputEnabled)
            {
                return;
            }

            cancelFocus();
            pressedCount++;
            if (pressedCount == 1)
            {
                isMultiTouch = false;
                pressPosition = _eventData.position;
                pressTime = Time.unscaledTime;
            }
            else
            {
                isMultiTouch = true;
            }

            if (pathPointerId != NoPointer)
            {
                return;
            }

            eventCamera = _eventData.pressEventCamera;

            if (viewPointers.Count == 0)
            {
                Vector2Int headBefore = map.Path.Head;
                switch (map.GetPressTarget(_eventData.position, eventCamera))
                {
                    case ePressTarget.PathHead:
                        pathPointerId = _eventData.pointerId;
                        lastPathPosition = _eventData.position;
                        headBeforeDrag = headBefore;
                        return;
                    case ePressTarget.NearHead:
                        // 先端付近は誤操作防止のため移動しない
                        return;
                }
            }

            // 2本目以降はピンチ用
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
        /// ドラッグ（経路・移動・ピンチ）
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
                dragPath(_eventData.position);
                return;
            }

            if (!viewPointers.TryGetValue(_eventData.pointerId, out Vector2 previous))
            {
                return;
            }

            Vector2 current = _eventData.position;
            if (viewPointers.Count == 1)
            {
                content.anchoredPosition = clamp(content.anchoredPosition + toViewport(current) - toViewport(previous), content.localScale.x);
            }
            else
            {
                pinch(previous, current, getOtherPointer(_eventData.pointerId));
            }

            viewPointers[_eventData.pointerId] = current;
        }

        /// <summary>
        /// 指を離した（ダブルタップ判定）
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerUp(PointerEventData _eventData)
        {
            if (!isInputEnabled)
            {
                return;
            }

            if (_eventData.pointerId == pathPointerId)
            {
                pathPointerId = NoPointer;
                if (map.Path.Head != headBeforeDrag)
                {
                    focusHead(content.localScale.x);
                }
            }
            viewPointers.Remove(_eventData.pointerId);
            pressedCount = Mathf.Max(0, pressedCount - 1);

            if (pressedCount == 0 && !isMultiTouch)
            {
                checkDoubleTap(_eventData.position);
            }
        }

        /// <summary>
        /// ホイールで拡大縮小
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnScroll(PointerEventData _eventData)
        {
            if (!isInputEnabled)
            {
                return;
            }

            cancelFocus();
            eventCamera = _eventData.enterEventCamera;
            float delta = Mathf.Clamp(_eventData.scrollDelta.y, -MaxScrollDelta, MaxScrollDelta);
            zoomAround(_eventData.position, content.localScale.x * Mathf.Pow(ScrollZoomStep, delta));
        }

        /// <summary>
        /// 無効化時に入力状態をリセット
        /// </summary>
        private void OnDisable()
        {
            pathPointerId = NoPointer;
            viewPointers.Clear();
            pressedCount = 0;
            cancelFocus();
        }

        /// <summary>
        /// タップならダブルタップか判定して先端へ移動
        /// </summary>
        /// <param name="_position"></param>
        private void checkDoubleTap(Vector2 _position)
        {
            bool isTap = Vector2.Distance(_position, pressPosition) <= TapMoveThreshold
                && Time.unscaledTime - pressTime <= TapMaxDuration;
            if (!isTap)
            {
                lastTapTime = float.NegativeInfinity;
                return;
            }

            if (Time.unscaledTime - lastTapTime <= DoubleTapInterval && Vector2.Distance(_position, lastTapPosition) <= DoubleTapDistance)
            {
                lastTapTime = float.NegativeInfinity;
                focusHead(fitScale);
                return;
            }

            lastTapTime = Time.unscaledTime;
            lastTapPosition = _position;
        }

        /// <summary>
        /// 経路の先端を中心に指定倍率へ移動
        /// </summary>
        /// <param name="_scale"></param>
        private void focusHead(float _scale)
        {
            cancelFocus();
            Vector2 target = clamp(-map.GetHeadPosition() * _scale, _scale);
            focusCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            focusAsync(target, _scale, FocusDuration, focusCts.Token).Forget();
        }

        /// <summary>
        /// 指定位置・倍率へなめらかに移動
        /// </summary>
        /// <param name="_position"></param>
        /// <param name="_scale"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        private async UniTask focusAsync(Vector2 _position, float _scale, float _duration, CancellationToken _token)
        {
            Vector2 startPosition = content.anchoredPosition;
            float startScale = content.localScale.x;
            float time = 0f;

            while (time < _duration)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    return;
                }
                time += Time.unscaledDeltaTime;

                // ease out
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(time / _duration), 3f);
                setScale(Mathf.Lerp(startScale, _scale, t));
                content.anchoredPosition = Vector2.Lerp(startPosition, _position, t);
            }
        }

        /// <summary>
        /// 移動アニメーションを止める
        /// </summary>
        private void cancelFocus()
        {
            if (focusCts == null)
            {
                return;
            }
            focusCts.Cancel();
            focusCts.Dispose();
            focusCts = null;
        }

        /// <summary>
        /// 前回位置から補間して経路を伸ばす
        /// </summary>
        /// <param name="_position"></param>
        private void dragPath(Vector2 _position)
        {
            float distance = Vector2.Distance(lastPathPosition, _position);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / SampleStep));
            for (int i = 1; i <= steps; i++)
            {
                map.DragPathTo(Vector2.Lerp(lastPathPosition, _position, (float)i / steps), eventCamera);
            }
            lastPathPosition = _position;
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

        /// <summary>
        /// ピンチで拡大縮小・移動
        /// </summary>
        /// <param name="_previous"></param>
        /// <param name="_current"></param>
        /// <param name="_other"></param>
        private void pinch(Vector2 _previous, Vector2 _current, Vector2 _other)
        {
            float previousDistance = Vector2.Distance(_previous, _other);
            float currentDistance = Vector2.Distance(_current, _other);
            Vector2 previousCenter = (_previous + _other) * 0.5f;
            Vector2 currentCenter = (_current + _other) * 0.5f;

            content.anchoredPosition += toViewport(currentCenter) - toViewport(previousCenter);
            float scale = previousDistance > 1f ? content.localScale.x * currentDistance / previousDistance : content.localScale.x;
            zoomAround(currentCenter, scale);
        }

        /// <summary>
        /// 指定位置を中心に拡大縮小
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_scale"></param>
        private void zoomAround(Vector2 _screenPosition, float _scale)
        {
            float oldScale = content.localScale.x;
            float newScale = Mathf.Clamp(_scale, minScale, maxScale);

            // 指定位置の下にある点を固定する
            Vector2 pivot = toViewport(_screenPosition);
            Vector2 contentPoint = (pivot - content.anchoredPosition) / oldScale;

            setScale(newScale);
            content.anchoredPosition = clamp(pivot - contentPoint * newScale, newScale);
        }

        /// <summary>
        /// 倍率を設定
        /// </summary>
        /// <param name="_scale"></param>
        private void setScale(float _scale)
        {
            content.localScale = new Vector3(_scale, _scale, 1f);
        }

        /// <summary>
        /// 移動範囲に収めた位置
        /// </summary>
        /// <param name="_position"></param>
        /// <param name="_scale"></param>
        private Vector2 clamp(Vector2 _position, float _scale)
        {
            Vector2 viewHalf = viewport.rect.size * 0.5f;
            Vector2 mapHalf = bounds.size * 0.5f * _scale;

            // 小さい時は中心が画面内、大きい時は端が画面中央まで来られる範囲
            Vector2 limit = Vector2.Max(viewHalf * PanLimitRate, mapHalf);
            Vector2 center = _position + bounds.center * _scale;
            center.x = Mathf.Clamp(center.x, -limit.x, limit.x);
            center.y = Mathf.Clamp(center.y, -limit.y, limit.y);

            return center - bounds.center * _scale;
        }

        /// <summary>
        /// スクリーン座標 → viewport のローカル座標
        /// </summary>
        /// <param name="_screenPosition"></param>
        private Vector2 toViewport(Vector2 _screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, _screenPosition, eventCamera, out Vector2 local);
            return local;
        }
    }
}
