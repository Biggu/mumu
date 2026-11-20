using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// Goal のパグ（泣いている女の子）の見た目。クリア前は泣き、クリアで喜ぶ
    /// </summary>
    public class BlockPugView
    {
        private const float PugCryFps = 4f;
        private const float PugSitFps = 3f;
        private const float PugHappyFps = 8f;
        private const float PugPunchAmount = 0.35f;
        private const float PugPunchDuration = 0.3f;

        private readonly GameObject pug;
        private readonly System.Threading.CancellationToken destroyToken;
        private SpriteFrameAnimation animation = null;
        private PugSprites sprites = null;
        private Vector2 basePosition = Vector2.zero;

        /// <summary>
        /// パグのオブジェクトを受け取る
        /// </summary>
        /// <param name="_pug"></param>
        /// <param name="_destroyToken"></param>
        public BlockPugView(GameObject _pug, System.Threading.CancellationToken _destroyToken)
        {
            pug = _pug;
            destroyToken = _destroyToken;
        }

        /// <summary>
        /// パグの画像を設定して表示する
        /// </summary>
        /// <param name="_sprites"></param>
        public void Setup(PugSprites _sprites)
        {
            sprites = _sprites;
            animation = CommonUtility.GetOrAddComponent<SpriteFrameAnimation>(pug);

            Image pugImage = pug.GetComponent<Image>();
            UiFactory.SetPugImage(pugImage, false);
            pugImage.color = Color.white;
            basePosition = ((RectTransform)pug.transform).anchoredPosition;
            PugAura.Attach((RectTransform)pug.transform, sprites.GirlTier, ((RectTransform)pug.transform).sizeDelta.x);

            pug.SetActive(true);
        }

        /// <summary>
        /// 状態に合わせてアニメーションと位置を反映
        /// </summary>
        /// <param name="_isCleared"></param>
        /// <param name="_isNoticed"></param>
        public void Apply(bool _isCleared, bool _isNoticed)
        {
            if (animation == null || sprites == null)
            {
                return;
            }

            if (_isCleared)
            {
                animation.Play(sprites.GirlHappy, PugHappyFps);
            }
            else if (_isNoticed)
            {
                animation.Play(sprites.GirlSit, PugSitFps);
            }
            else
            {
                animation.Play(sprites.GirlCry, PugCryFps);
            }
            ((RectTransform)pug.transform).anchoredPosition = basePosition + new Vector2(_isCleared ? Block.GoalPairOffset : 0f, 0f);
        }

        /// <summary>
        /// パグを跳ねさせる
        /// </summary>
        public void Punch()
        {
            if (animation != null)
            {
                CommonUtility.Punch(pug.transform, PugPunchAmount, PugPunchDuration, destroyToken);
            }
        }
    }
}
