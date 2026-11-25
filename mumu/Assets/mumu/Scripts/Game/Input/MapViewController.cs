using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップの表示（移動・拡大縮小・先端への移動。操作の判定は MapInputController）
    /// </summary>
    public class MapViewController : MonoBehaviour
    {
        /// <summary>初期表示でマップが画面に占める割合（横）</summary>
        private static float FitRateX => MapInputSettings.Current.FitRateX;
        /// <summary>初期表示でマップが画面に占める割合（縦）</summary>
        private static float FitRateY => MapInputSettings.Current.FitRateY;
        /// <summary>初期倍率に対する最小倍率</summary>
        private static float MinZoomRate => MapInputSettings.Current.MinZoomRate;
        /// <summary>初期倍率に対する最大倍率</summary>
        private static float MaxZoomRate => MapInputSettings.Current.MaxZoomRate;
        /// <summary>拡大の上限（絶対値）。地図が大きくても、5×5 と同じくらい近くまで寄れるようにする</summary>
        private static float MaxScaleAbsolute => MapInputSettings.Current.MaxScaleAbsolute;
        /// <summary>マップが画面より小さい時、中心を画面端の何割まで動かせるか</summary>
        private static float PanLimitRate => MapInputSettings.Current.PanLimitRate;
        /// <summary>ダブルタップ時の移動時間（秒）</summary>
        private static float FocusDuration => MapInputSettings.Current.FocusDuration;

        /// <summary>移動・拡大する対象（自身）</summary>
        private RectTransform content = null;
        private RectTransform viewport = null;
        private IMapCameraTarget map = null;
        private Rect bounds;
        private float fitScale = 1f;
        private float minScale = 1f;
        private float maxScale = 1f;
        private CancellationTokenSource focusCts = null;

        /// <summary>現在の倍率</summary>
        public float Scale => content.localScale.x;
        /// <summary>初期表示の倍率（マップ全体が入る）</summary>
        public float FitScale => fitScale;

        

        /// <summary>経路の先端位置を viewport ローカル座標で取得する（pivot 基準）</summary>
        public Vector2 GetHeadViewportPosition()
        {
            return content.anchoredPosition + map.GetFocusPosition() * Scale;
        }


        /// <summary>
        /// 先端が画面端のマージン内にあり、かつ、その端へ向かって動いているかを判定する。
        /// _edgeMarginRate は画面幅／高さに対する割合（例：0.18 = 18%）。
        /// </summary>
        public bool IsHeadMovingTowardEdge(Vector2 _previousPosition, Vector2 _currentPosition, float _edgeMarginRate)
        {
            Rect rect = viewport.rect;
            float marginRate = Mathf.Clamp(_edgeMarginRate, 0f, 0.49f);
            float marginX = rect.width * marginRate;
            float marginY = rect.height * marginRate;

            bool movingTowardLeft = _currentPosition.x <= rect.xMin + marginX && _currentPosition.x < _previousPosition.x;
            bool movingTowardRight = _currentPosition.x >= rect.xMax - marginX && _currentPosition.x > _previousPosition.x;
            bool movingTowardBottom = _currentPosition.y <= rect.yMin + marginY && _currentPosition.y < _previousPosition.y;
            bool movingTowardTop = _currentPosition.y >= rect.yMax - marginY && _currentPosition.y > _previousPosition.y;

            return movingTowardLeft || movingTowardRight || movingTowardBottom || movingTowardTop;
        }

/// <summary>
        /// 指定したスクリーン位置を、移動・拡大中のコンテンツのローカル座標へ変換
        /// </summary>
        public Vector2 ScreenToContentPosition(Vector2 _screenPosition, Camera _camera)
        {
            Vector2 viewportPosition = toViewport(_screenPosition, _camera);
            float scale = content.localScale.x;
            return (viewportPosition - content.anchoredPosition) / scale;
        }

        /// <summary>
        /// 初期化（_startScale が 0 なら地図全体が入る倍率。0 より大きければその倍率で中心に表示）
        /// </summary>
        /// <param name="_viewport"></param>
        /// <param name="_map"></param>
        /// <param name="_startScale"></param>
        public void Setup(RectTransform _viewport, IMapCameraTarget _map, float _startScale = 0f)
        {
            content = (RectTransform)transform;
            viewport = _viewport;
            map = _map;
            bounds = map.GetContentBounds();

            fitScale = Mathf.Min(
                viewport.rect.width * FitRateX / bounds.width,
                viewport.rect.height * FitRateY / bounds.height);
            minScale = fitScale * MinZoomRate;
            maxScale = Mathf.Max(fitScale * MaxZoomRate, MaxScaleAbsolute);

            // No explicit start scale: use the configurable multiplier of FitScale.
            float start = _startScale > 0f
                ? _startScale
                : Mathf.Clamp(fitScale * MapInputSettings.Current.DefaultZoomRate, minScale, maxScale);
            setScale(start);
            content.anchoredPosition = clamp(-map.GetFocusPosition() * start, start);
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
            CancelFocus();
            float scale = viewport.rect.width / Mathf.Max(1f, _visibleWidth);
            return focusAsync(clamp(-_position * scale, scale), scale, _duration, _token);
        }

        /// <summary>
        /// 指の移動量だけマップを動かす
        /// </summary>
        /// <param name="_previous"></param>
        /// <param name="_current"></param>
        /// <param name="_camera"></param>
        public void PanBy(Vector2 _previous, Vector2 _current, Camera _camera)
        {
            content.anchoredPosition = clamp(content.anchoredPosition + toViewport(_current, _camera) - toViewport(_previous, _camera), content.localScale.x);
        }

        /// <summary>
        /// 2本指で拡大縮小・移動
        /// </summary>
        /// <param name="_previous"></param>
        /// <param name="_current"></param>
        /// <param name="_other"></param>
        /// <param name="_camera"></param>
        public void Pinch(Vector2 _previous, Vector2 _current, Vector2 _other, Camera _camera)
        {
            float previousDistance = Vector2.Distance(_previous, _other);
            float currentDistance = Vector2.Distance(_current, _other);
            Vector2 previousCenter = (_previous + _other) * 0.5f;
            Vector2 currentCenter = (_current + _other) * 0.5f;

            content.anchoredPosition += toViewport(currentCenter, _camera) - toViewport(previousCenter, _camera);
            float scale = previousDistance > 1f ? content.localScale.x * currentDistance / previousDistance : content.localScale.x;
            ZoomAt(currentCenter, scale, _camera);
        }

        /// <summary>
        /// 指定位置を中心に拡大縮小
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_scale"></param>
        /// <param name="_camera"></param>
        public void ZoomAt(Vector2 _screenPosition, float _scale, Camera _camera)
        {
            float oldScale = content.localScale.x;
            float newScale = Mathf.Clamp(_scale, minScale, maxScale);

            // 指定位置の下にある点を固定する
            Vector2 pivot = toViewport(_screenPosition, _camera);
            Vector2 contentPoint = (pivot - content.anchoredPosition) / oldScale;

            setScale(newScale);
            content.anchoredPosition = clamp(pivot - contentPoint * newScale, newScale);
        }

        /// <summary>
        /// 経路の先端を中心に指定倍率へ移動
        /// </summary>
        /// <param name="_scale"></param>
        public void FocusHead(float _scale)
        {
            Vector2 target = clamp(-map.GetFocusPosition() * _scale, _scale);
            focusAsync(target, _scale, FocusDuration, TokenUtility.Restart(ref focusCts, destroyCancellationToken)).Forget();
        }

        /// <summary>
        /// 指定した画面の位置が画面の中心に来るよう、なめらかに移動（倍率は変えない）
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        public void FocusScreenPoint(Vector2 _screenPosition, Camera _camera)
        {
            float scale = content.localScale.x;

            // 指の下にあるマップ上の点を求めて、それが中心に来る位置へ動かす
            Vector2 pivot = toViewport(_screenPosition, _camera);
            Vector2 contentPoint = (pivot - content.anchoredPosition) / scale;
            Vector2 target = clamp(-contentPoint * scale, scale);

            focusAsync(target, scale, FocusDuration, TokenUtility.Restart(ref focusCts, destroyCancellationToken)).Forget();
        }

        /// <summary>
        /// 経路の先端（フォーカス位置）が、指定した画面の位置に来るよう瞬時に移動（倍率は変えない）
        /// </summary>
        /// <param name="_screenPosition"></param>
        /// <param name="_camera"></param>
        public void MoveHeadTo(Vector2 _screenPosition, Camera _camera)
        {
            CancelFocus();
            float scale = content.localScale.x;
            Vector2 pivot = toViewport(_screenPosition, _camera);
            content.anchoredPosition = clamp(pivot - map.GetFocusPosition() * scale, scale);
        }

        /// <summary>
        /// 移動アニメーションを止める
        /// </summary>
        public void CancelFocus()
        {
            TokenUtility.Cancel(ref focusCts);
        }

        /// <summary>
        /// 無効化時に移動アニメーションを止める
        /// </summary>
        private void OnDisable()
        {
            CancelFocus();
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
        /// <param name="_camera"></param>
        private Vector2 toViewport(Vector2 _screenPosition, Camera _camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, _screenPosition, _camera, out Vector2 local);
            return local;
        }
    }
}