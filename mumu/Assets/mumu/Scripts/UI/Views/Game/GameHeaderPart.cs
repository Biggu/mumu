using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ゲーム画面のヘッダー（ゴールゲージ・★・王冠・ほね・ステージ札・メニュー）
    /// </summary>
    public class GameHeaderPart : MonoBehaviour
    {
        /// <summary>Goal までの残り手数ゲージ</summary>
        [SerializeField]
        private GoalGauge goalGauge = null;
        /// <summary>★</summary>
        [SerializeField]
        private Image[] stars = null;
        /// <summary>点いた★の画像</summary>
        [SerializeField]
        private Sprite starOn = null;
        /// <summary>消えた★の画像</summary>
        [SerializeField]
        private Sprite starOff = null;
        /// <summary>PERFECT の王冠</summary>
        [SerializeField]
        private Image crownImage = null;
        /// <summary>PERFECT を取った王冠の画像</summary>
        [SerializeField]
        private Sprite crownOn = null;
        /// <summary>PERFECT 前の王冠の画像</summary>
        [SerializeField]
        private Sprite crownOff = null;
        /// <summary>ほねの札</summary>
        [SerializeField]
        private GameObject bonePlate = null;
        /// <summary>拾ったほね / マップのほね</summary>
        [SerializeField]
        private TMP_Text boneText = null;
        /// <summary>メニューボタン</summary>
        [SerializeField]
        private MumuButton menuButton = null;
        /// <summary>ステージ札の文字</summary>
        [SerializeField]
        private TMP_Text stageText = null;

        /// <summary>表示中のマップ</summary>
        private InGameMap map = null;
        /// <summary>メニューボタンを押した時の処理（GameView から受け取る）</summary>
        private Action onClickMenuAction = null;
        private int lastBoneCount = -1;
        private int litStarCount = 0;

        /// <summary>
        /// メニューボタン押下時の処理を設定
        /// </summary>
        /// <param name="_onClickMenu"></param>
        public void Setup(Action _onClickMenu)
        {
            onClickMenuAction = _onClickMenu;
            menuButton.SetOnClick(onClickMenuButton);
        }

        /// <summary>
        /// マップに合わせて表示を初期化する
        /// </summary>
        /// <param name="_map"></param>
        public void Init(InGameMap _map)
        {
            map = _map;
            goalGauge.Setup(map.Path.RequiredMoveCount, map.PugSprites);
            setStars(map.Path.StarCount, false);
            setCrown(false);
            updateGauge();
            updateBones();
        }

        /// <summary>
        /// 札の文字を今のステージに合わせる
        /// </summary>
        public void SetStageLabel()
        {
            stageText.text = getStageLabel();
        }

        /// <summary>
        /// 経路が変わった時の表示更新
        /// </summary>
        public void OnPathChanged()
        {
            if (!map.Path.IsCleared)
            {
                setStars(map.Path.StarCount, true);
            }
            updateGauge();
            updateBones();
        }

        /// <summary>
        /// メニューボタンを隠す（クリア演出中）
        /// </summary>
        public void HideMenuButton()
        {
            menuButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// ★を1つずつ点ける。もどらずで★3つなら大きな★にまとめる（中断されたら true）
        /// </summary>
        /// <param name="_isNoRewind"></param>
        /// <param name="_isSkipRequested"></param>
        public async UniTask<bool> PlayStarsAsync(bool _isNoRewind, Func<bool> _isSkipRequested)
        {
            int starCount = map.Path.StarCount;
            setStars(starCount, false);
            for (int i = 0; i < starCount && i < stars.Length; i++)
            {
                CommonUtility.PlaySe(eSe.StarPing);
                punchStar(i);
                if (await CommonUtility.WaitOrSkipAsync(CommonDefine.ClearStarInterval, _isSkipRequested, destroyCancellationToken))
                {
                    return true;
                }
            }

            if (_isNoRewind && starCount == stars.Length)
            {
                showBigStar();
                return await CommonUtility.WaitOrSkipAsync(CommonDefine.ClearStarInterval, _isSkipRequested, destroyCancellationToken);
            }
            return false;
        }

        /// <summary>
        /// PERFECT の王冠を点けて跳ねさせる
        /// </summary>
        public void ShowPerfectCrown()
        {
            setCrown(true);
            CommonUtility.Punch(crownImage.transform, Vector3.one, CommonDefine.StarPunchAmount, CommonDefine.StarPunchDuration, destroyCancellationToken);
        }

        /// <summary>
        /// ステージ札をめくって CLEAR!（PERFECT!）に変える（中断されたら true）
        /// </summary>
        /// <param name="_isPerfect"></param>
        public async UniTask<bool> PlayStampAsync(bool _isPerfect)
        {
            Transform plate = stageText.transform.parent;
            if (await UiTween.FlipTextAsync(plate, stageText, _isPerfect ? MumuText.PERFECT() : MumuText.CLEAR(), CommonDefine.StampFlipDuration, destroyCancellationToken))
            {
                return true;
            }

            CommonUtility.PlaySe(eSe.Checkpoint);
            CommonUtility.Punch(plate, Vector3.one, CommonDefine.StampPunchAmount, CommonDefine.StampPunchDuration, destroyCancellationToken);
            return false;
        }

        /// <summary>
        /// メニューボタン押下時の処理（GameView の処理を呼ぶ）
        /// </summary>
        private void onClickMenuButton()
        {
            onClickMenuAction.Invoke();
        }

        /// <summary>
        /// ほねの札を更新（増えたら跳ねる）
        /// </summary>
        private void updateBones()
        {
            PuzzlePath path = map.Path;
            bonePlate.SetActive(path.BoneCount > 0);
            if (path.BoneCount == 0)
            {
                return;
            }

            int collected = path.CollectedBoneCount;
            boneText.text = $"{collected}/{path.BoneCount}";
            if (lastBoneCount >= 0 && collected > lastBoneCount)
            {
                CommonUtility.Punch(boneText.transform.parent, Vector3.one, CommonDefine.StarPunchAmount, CommonDefine.StarPunchDuration, destroyCancellationToken);
            }
            lastBoneCount = collected;
        }

        /// <summary>
        /// ゲージを更新
        /// </summary>
        private void updateGauge()
        {
            PuzzlePath path = map.Path;
            goalGauge.SetRemaining(path.RequiredMoveCount - path.MoveCount, path.IsCleared);
        }

        /// <summary>
        /// 王冠の表示
        /// </summary>
        /// <param name="_isPerfect"></param>
        private void setCrown(bool _isPerfect)
        {
            crownImage.sprite = _isPerfect ? crownOn : crownOff;
        }

        /// <summary>
        /// ★の点灯数を反映
        /// </summary>
        /// <param name="_count"></param>
        /// <param name="_animate"></param>
        private void setStars(int _count, bool _animate)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                bool isLit = i < _count;
                stars[i].sprite = isLit ? starOn : starOff;

                // 新しく点いた★だけ跳ねる
                if (_animate && isLit && i >= litStarCount)
                {
                    punchStar(i);
                }
            }
            litStarCount = _count;
        }

        /// <summary>
        /// ★を跳ねさせる
        /// </summary>
        /// <param name="_index"></param>
        private void punchStar(int _index)
        {
            CommonUtility.Punch(stars[_index].transform, Vector3.one, CommonDefine.StarPunchAmount, CommonDefine.StarPunchDuration, destroyCancellationToken);
        }

        /// <summary>
        /// ★3つを大きな★1つにまとめて跳ねさせる
        /// </summary>
        private void showBigStar()
        {
            Image middle = stars[stars.Length / 2];
            foreach (Image star in stars)
            {
                star.gameObject.SetActive(false);
            }
            GameObject go = PartsLoaderManager.Instance.Create(ePart.BigStar, middle.transform.parent);
            Image bigStar = go.GetComponent<Image>();
            bigStar.sprite = RainbowStar.Still;
            CommonUtility.GetOrAddComponent<SpriteFrameAnimation>(go).Play(RainbowStar.Frames, RainbowStar.TwinkleFps);
            bigStar.rectTransform.sizeDelta = new Vector2(CommonDefine.BigStarSize, CommonDefine.BigStarSize);
            bigStar.rectTransform.anchoredPosition = ((RectTransform)middle.transform).anchoredPosition;
            CommonUtility.PlaySe(eSe.Sparkle);
            CommonUtility.Punch(bigStar.transform, Vector3.one, CommonDefine.StarPunchAmount, CommonDefine.StarPunchDuration, destroyCancellationToken);
        }

        /// <summary>
        /// 札の文字（冒険はステージ番号とテーマ名、おさんぽは今日の何回目か、チャンス後は ???）
        /// </summary>
        private static string getStageLabel()
        {
            if (GameSession.Mode == eGameMode.Adventure)
            {
                int number = GameSession.PlayedStageNumber;
                return MumuText.STAGE_THEME(number, MumuText.THEME_NAME(StageProgress.GetThemeIdOfStage(number)));
            }
            if (GameSession.IsDailyStroll)
            {
                return MumuText.STROLL_DAILY(GameSession.DailyStrollNumber, GameSession.DailyStrollCount);
            }
            return MumuText.STAGE("???");
        }
    }
}
