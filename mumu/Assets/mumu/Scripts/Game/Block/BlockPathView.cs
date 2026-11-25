using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ブロックの経路の見た目（前後への線・足あと・光る枠）
    /// </summary>
    public class BlockPathView
    {
        private const string PawSpritePath = "Sprites/Common/paw";
        private const float PawSize = 14f;
        /// <summary>足あとを左右にずらす量（交互に並べる）</summary>
        private const float PawSideOffset = 4f;
        private const float HighlightPulseSpeed = 5f;
        private const float HighlightMinAlpha = 0.45f;

        private static Sprite pawSprite = null;

        private readonly GameObject lineRoot;
        private readonly RectTransform linePrevious;
        private readonly RectTransform lineNext;
        private readonly Image highlight;
        /// <summary>枠の普通の色（赤い予告を消した時に戻す）</summary>
        private readonly Color highlightBaseColor;
        /// <summary>多く戻る時に消える道を赤く出す色</summary>
        private static readonly Color RewindPreviewColor = new Color(1f, 0.2f, 0.2f, 1f);
        /// <summary>ヒントの線がふわっと出る時間（秒）</summary>
        private const float HintFadeDuration = 0.4f;
        /// <summary>ヒントの線を出しているか（出た時だけフェードする）</summary>
        private bool isHintShown = false;
        private CancellationTokenSource hintFadeCts = null;
        private readonly CancellationToken destroyToken;

        /// <summary>経路上の足あと</summary>
        private RectTransform paw = null;
        private CancellationTokenSource highlightCts = null;

        /// <summary>ヒントの線の色（通ると普通の色に戻る）</summary>
        // TODO: ヒントの色は決まったら差し替える
        private static readonly Color HintLineColor = new Color(0.55f, 0.85f, 1f);
        /// <summary>線の普通の色（Prefab の色）</summary>
        private readonly Color normalLineColor = Color.white;
        private readonly Image[] lineImages = null;

        /// <summary>
        /// 線・足あと・枠を受け取る
        /// </summary>
        /// <param name="_lineRoot"></param>
        /// <param name="_linePrevious"></param>
        /// <param name="_lineNext"></param>
        /// <param name="_highlight"></param>
        /// <param name="_destroyToken"></param>
        public BlockPathView(GameObject _lineRoot, RectTransform _linePrevious, RectTransform _lineNext, Image _highlight, CancellationToken _destroyToken)
        {
            lineRoot = _lineRoot;
            linePrevious = _linePrevious;
            lineNext = _lineNext;
            highlight = _highlight;
            highlightBaseColor = _highlight.color;
            destroyToken = _destroyToken;
            lineImages = new[] { _linePrevious.GetComponent<Image>(), _lineNext.GetComponent<Image>() };
            if (lineImages[0] != null)
            {
                normalLineColor = lineImages[0].color;
            }
        }

        /// <summary>
        /// 経路の線（前後のブロックへの方向、zero でその側なし）
        /// </summary>
        /// <param name="_isOnPath"></param>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        /// <param name="_hasPaw">足あとを置くか（Start・Goal は置かない）</param>
        /// <param name="_pathStep">経路上の順番（足あとを左右に交互にする）</param>
        public void SetLine(bool _isOnPath, Vector2Int _toPrevious, Vector2Int _toNext, bool _hasPaw, int _pathStep)
        {
            lineRoot.SetActive(_isOnPath);
            if (!_isOnPath)
            {
                return;
            }

            setLineColor(normalLineColor);
            setLineSegment(linePrevious, _toPrevious);
            setLineSegment(lineNext, _toNext);
            setPaw(_toPrevious, _toNext, _hasPaw, _pathStep);
        }

        /// <summary>
        /// ヒントの線（経路の外にあるマスだけ。経路に乗ると SetLine で普通の色に戻る）
        /// </summary>
        /// <param name="_isHint"></param>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        public void SetHint(bool _isHint, Vector2Int _toPrevious, Vector2Int _toNext)
        {
            if (!_isHint)
            {
                isHintShown = false;
                return;
            }

            lineRoot.SetActive(true);
            if (!isHintShown)
            {
                // 初めて出る時だけ、ふわっと出す
                isHintShown = true;
                fadeHintInAsync(TokenUtility.Restart(ref hintFadeCts, destroyToken)).Forget();
            }
            else
            {
                setLineColor(HintLineColor);
            }
            setLineSegment(linePrevious, _toPrevious);
            setLineSegment(lineNext, _toNext);
            if (paw != null)
            {
                paw.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// ヒントの線を透明から出す（途中で消えたら止める）
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid fadeHintInAsync(CancellationToken _token)
        {
            for (float time = 0f; time < HintFadeDuration; time += Time.unscaledDeltaTime)
            {
                setLineAlpha(time / HintFadeDuration);
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    return;
                }
            }
            setLineAlpha(1f);
        }

        /// <summary>
        /// ヒントの線の透明度を変える（色は HintLineColor のまま）
        /// </summary>
        /// <param name="_alpha"></param>
        private void setLineAlpha(float _alpha)
        {
            Color color = HintLineColor;
            color.a = _alpha;
            foreach (Image image in lineImages)
            {
                if (image != null)
                {
                    image.color = color;
                }
            }
        }

        /// <summary>
        /// 線の色を変える
        /// </summary>
        /// <param name="_color"></param>
        private void setLineColor(Color _color)
        {
            foreach (Image image in lineImages)
            {
                if (image != null)
                {
                    image.color = _color;
                }
            }
        }

        /// <summary>
        /// 多く戻る時に消える道を赤く出す（外すと普通の枠に戻す）
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetRewindPreview(bool _isVisible)
        {
            TokenUtility.Cancel(ref highlightCts);
            highlight.color = _isVisible ? RewindPreviewColor : highlightBaseColor;
            highlight.gameObject.SetActive(_isVisible);
        }

        /// <summary>
        /// 次に進めるブロックの光る枠
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetHighlight(bool _isVisible)
        {
            if (!_isVisible)
            {
                TokenUtility.Cancel(ref highlightCts);
                highlight.gameObject.SetActive(false);
                return;
            }

            if (highlightCts == null)
            {
                highlight.gameObject.SetActive(true);
                highlightPulseAsync(TokenUtility.Ensure(ref highlightCts, destroyToken)).Forget();
            }
        }

        /// <summary>
        /// 光る枠の点滅を止める
        /// </summary>
        public void Cancel()
        {
            TokenUtility.Cancel(ref highlightCts);
        }

        /// <summary>
        /// 進む向きに足あとを置く
        /// </summary>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        /// <param name="_hasPaw"></param>
        /// <param name="_pathStep"></param>
        private void setPaw(Vector2Int _toPrevious, Vector2Int _toNext, bool _hasPaw, int _pathStep)
        {
            if (!_hasPaw)
            {
                if (paw != null)
                {
                    paw.gameObject.SetActive(false);
                }
                return;
            }

            if (paw == null)
            {
                paw = UiFactory.CreateImage("paw", lineRoot.transform, SpriteUtility.LoadCached(ref pawSprite, PawSpritePath), PawSize).rectTransform;
            }

            // 次のマスへ向ける（先端は前のマスから来た向き）
            Vector2Int forward = _toNext != Vector2Int.zero ? _toNext : -_toPrevious;
            paw.gameObject.SetActive(forward != Vector2Int.zero);
            float angle = GridUtility.GetPlaneAngle(forward) - 90f;
            paw.localRotation = Quaternion.Euler(0f, 0f, angle);

            // 左右の足を交互に
            float side = (_pathStep % 2 == 0 ? 1f : -1f) * PawSideOffset;
            paw.anchoredPosition = (Vector2)(paw.localRotation * new Vector3(side, 0f, 0f));
            paw.SetAsLastSibling();
        }

        /// <summary>
        /// 線の片側を設定
        /// </summary>
        /// <param name="_segment"></param>
        /// <param name="_direction"></param>
        private void setLineSegment(RectTransform _segment, Vector2Int _direction)
        {
            bool isVisible = _direction != Vector2Int.zero;
            _segment.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                _segment.localRotation = Quaternion.Euler(0f, 0f, GridUtility.GetPlaneAngle(_direction));
            }
        }

        /// <summary>
        /// 光る枠を点滅させる
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid highlightPulseAsync(CancellationToken _token)
        {
            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                // 全ブロックで位相をそろえる
                float wave = Mathf.Sin(Time.unscaledTime * HighlightPulseSpeed) * 0.5f + 0.5f;
                Color color = highlight.color;
                color.a = Mathf.Lerp(HighlightMinAlpha, 1f, wave);
                highlight.color = color;
            }
        }
    }
}
