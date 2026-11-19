using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// Goal 解放までの残り手数ゲージ（パグのアイコン付き）
    /// </summary>
    public class GoalGauge : MonoBehaviour
    {
        private const float CryFps = 4f;
        private const float HappyFps = 8f;
        private const float CountPunchAmount = 0.25f;
        private const float CountPunchDuration = 0.15f;
        private const float OpenPunchAmount = 0.5f;
        private const float OpenPunchDuration = 0.35f;
        private const float HeartSize = 30f;
        private const float HeartRise = 60f;
        private const float HeartDuration = 0.6f;


        /// <summary>残りの量（anchorMax.x で長さを変える）</summary>
        [SerializeField]
        private RectTransform fill = null;
        /// <summary>残り手数</summary>
        [SerializeField]
        private TMP_Text countText = null;
        /// <summary>パグのアイコン</summary>
        [SerializeField]
        private Image pugIcon = null;

        private SpriteFrameAnimation pugAnimation = null;
        private PugSprites pugSprites = null;
        private int required = 1;
        private int remaining = -1;
        private bool isCleared = false;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_required"></param>
        /// <param name="_pugSprites"></param>
        public void Setup(int _required, PugSprites _pugSprites)
        {
            required = Mathf.Max(1, _required);
            pugSprites = _pugSprites;
            remaining = -1;
            isCleared = false;

            if (pugIcon != null && pugSprites != null)
            {
                pugAnimation = pugIcon.GetComponent<SpriteFrameAnimation>();
                if (pugAnimation == null)
                {
                    pugAnimation = pugIcon.gameObject.AddComponent<SpriteFrameAnimation>();
                }
                pugAnimation.Play(pugSprites.GirlCry, CryFps);
            }
            SetRemaining(_required, false);
        }

        /// <summary>
        /// 残り手数を反映（0 で Goal 解放）
        /// </summary>
        /// <param name="_remaining"></param>
        /// <param name="_isCleared"></param>
        public void SetRemaining(int _remaining, bool _isCleared)
        {
            int value = Mathf.Clamp(_remaining, 0, required);
            int previous = remaining;
            bool isOpened = remaining > 0 && value == 0;
            bool isChanged = remaining >= 0 && value != remaining;
            remaining = value;

            if (fill != null)
            {
                fill.gameObject.SetActive(value > 0);
                fill.anchorMax = new Vector2((float)value / required, fill.anchorMax.y);
            }

            if (countText != null)
            {
                countText.text = value.ToString();
                if (isChanged)
                {
                    UiTween.PunchScale(countText.transform, Vector3.one, CountPunchAmount, CountPunchDuration, destroyCancellationToken).Forget();
                }
                if (isChanged && value < previous)
                {
                    Vector2 from = transform.InverseTransformPoint(countText.transform.position);
                    HeartEffect.Play((RectTransform)transform, from, new Vector2(Random.Range(-16f, 16f), HeartRise), HeartSize, HeartDuration, destroyCancellationToken).Forget();
                }
            }

            if (pugIcon != null && (isOpened || (_isCleared && !isCleared)))
            {
                UiTween.PunchScale(pugIcon.transform, Vector3.one, OpenPunchAmount, OpenPunchDuration, destroyCancellationToken).Forget();
            }

            if (_isCleared && !isCleared && pugAnimation != null)
            {
                pugAnimation.Play(pugSprites.GirlHappy, HappyFps);
            }
            isCleared = _isCleared;
        }
    }
}
