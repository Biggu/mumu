using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// マスに浮かぶ拾うもの（ほね・カギ）。拾うと上に飛んで消え、巻き戻すとまた出る
    /// </summary>
    public class BlockFloatingItems
    {
        /// <summary>ブロックの中心からほねの中心までの高さ（チェックポイントの★より下）</summary>
        private const float BoneY = 28f;
        private const float BoneBob = 2.5f;
        private const float BoneBobSpeed = 4f;
        private const float BonePickDuration = 0.35f;
        private const float BonePickRise = 30f;

        private readonly CancellationToken destroyToken;
        private CancellationTokenSource cts = null;
        /// <summary>経路に乗ったか（拾った直後の演出に使う）</summary>
        private bool isPassed = false;
        private float passedTime = float.NegativeInfinity;

        /// <summary>
        /// 停止のためのトークンを受け取る
        /// </summary>
        /// <param name="_destroyToken"></param>
        public BlockFloatingItems(CancellationToken _destroyToken)
        {
            destroyToken = _destroyToken;
        }

        /// <summary>
        /// 浮かぶものを1つ追加する
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        /// <param name="_x"></param>
        /// <param name="_sortIndex"></param>
        /// <param name="_parent"></param>
        public void Add(string _name, string _sprite, float _size, Color _color, float _x, int _sortIndex, Transform _parent)
        {
            Image item = GimmickSprites.CreateImage(_name, _parent, _sprite, _size);
            item.color = _color;
            floatingAsync(item, _color, _x, _sortIndex, TokenUtility.Ensure(ref cts, destroyToken)).Forget();
        }

        /// <summary>
        /// 経路に乗ったか（拾う演出に使う）
        /// </summary>
        /// <param name="_isPassed"></param>
        /// <param name="_passedTime"></param>
        public void SetPassed(bool _isPassed, float _passedTime)
        {
            isPassed = _isPassed;
            passedTime = _passedTime;
        }

        /// <summary>
        /// ふわふわを止める
        /// </summary>
        public void Cancel()
        {
            TokenUtility.Cancel(ref cts);
        }

        /// <summary>
        /// ふわふわさせ、拾った時は上に飛ばして消す
        /// </summary>
        /// <param name="_item"></param>
        /// <param name="_color"></param>
        /// <param name="_x"></param>
        /// <param name="_sortIndex"></param>
        /// <param name="_token"></param>
        private async UniTaskVoid floatingAsync(Image _item, Color _color, float _x, int _sortIndex, CancellationToken _token)
        {
            RectTransform rect = _item.rectTransform;
            float phase = _sortIndex * 0.7f + _x;
            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                float y = BoneY + Mathf.Sin(Time.unscaledTime * BoneBobSpeed + phase) * BoneBob;
                float alpha = 1f;
                float scale = 1f;
                if (isPassed)
                {
                    // 拾った直後だけ上に飛んで消える
                    float t = (Time.unscaledTime - passedTime) / BonePickDuration;
                    if (t >= 1f)
                    {
                        _item.enabled = false;
                        continue;
                    }
                    y += t * BonePickRise;
                    alpha = 1f - t;
                    scale = 1f + t * 0.5f;
                }
                _item.enabled = true;
                rect.anchoredPosition = new Vector2(_x, y);
                rect.localScale = new Vector3(scale, scale, 1f);
                _item.color = new Color(_color.r, _color.g, _color.b, alpha);
            }
        }
    }
}
