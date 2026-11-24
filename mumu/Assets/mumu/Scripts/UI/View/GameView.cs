using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ゲーム画面
    /// </summary>
    public class GameView : ViewBase
    {
        private const float StarPunchAmount = 0.5f;
        private const float StarPunchDuration = 0.3f;
        private const float ClearStarInterval = 0.15f;
        private const int ConfettiCount = 60;
        /// <summary>クリア時に寄った画面に映るマップの幅（2匹のパグのアップ）</summary>
        private const float ClearZoomWidth = 150f;
        private const float ClearZoomDuration = 0.8f;
        /// <summary>クリア時の寄る位置（上面の中心から2匹の体の中心まで）</summary>
        private const float ClearZoomOffsetY = 18f;
        /// <summary>クリア演出の後、次のステージへ進むまでの時間（秒）</summary>
        private const float NextStageDelay = 2f;
        private const float StampFlipDuration = 0.25f;
        private const float StampPunchAmount = 0.6f;
        private const float StampPunchDuration = 0.3f;
        private const int PerfectHeartCount = 16;
        private const float PerfectHeartRadius = 260f;
        private const float RewardDialogDelay = 0.6f;
        /// <summary>もどらずクリアのボーナス（ほね、初めて達成した時だけ）</summary>
        private const int MissionBonusBones = 10;
        /// <summary>ヒント1回分のほね（今回が何回目かを掛けた分がかかる。1回目 5、2回目 10 …）</summary>
        public const int HintCost = 5;
        /// <summary>大きな★の大きさ（★3つを1つにまとめる時）</summary>
        private const float BigStarSize = 96f;

        /// <summary>ブロックの親</summary>
        [SerializeField]
        private GameObject mapRoot = null;
        /// <summary>マップと背景の親（ドラッグ・ピンチで移動拡大）</summary>
        [SerializeField]
        private RectTransform mapGroup = null;
        /// <summary>クリアの演出中か（画面を押したら飛ばせる）</summary>
        private bool isClearing = false;
        /// <summary>クリアの演出の待ち時間を、画面を押して飛ばしたか</summary>
        private bool isClearSkipRequested = false;
        /// <summary>1つ戻すボタン（右下。Prefab で置いて割り当てる）</summary>
        [SerializeField]
        private MumuButton undoButton = null;
        /// <summary>背景</summary>
        [SerializeField]
        private Image bgImage = null;
        /// <summary>★</summary>
        [SerializeField]
        private Image[] stars = null;
        /// <summary>点いた★の画像</summary>
        [SerializeField]
        private Sprite starOn = null;
        /// <summary>消えた★の画像</summary>
        [SerializeField]
        private Sprite starOff = null;
        /// <summary>Goal までの残り手数ゲージ</summary>
        [SerializeField]
        private GoalGauge goalGauge = null;
        /// <summary>メニューボタン</summary>
        [SerializeField]
        private MumuButton menuButton = null;
        /// <summary>ステージ番号（おさんぽは ???、クリアで CLEAR! に変わる）</summary>
        [SerializeField]
        private TMP_Text stageText = null;
        /// <summary>PERFECT の王冠</summary>
        [SerializeField]
        private Image crownImage = null;
        /// <summary>PERFECT を取った王冠の画像</summary>
        [SerializeField]
        private Sprite crownOn = null;
        /// <summary>PERFECT 前の王冠の画像</summary>
        [SerializeField]
        private Sprite crownOff = null;
        /// <summary>ほねの札（ほねがないマップでは隠す）</summary>
        [SerializeField]
        private GameObject bonePlate = null;
        /// <summary>拾ったほね / マップのほね</summary>
        [SerializeField]
        private TMP_Text boneText = null;

        private int lastBoneCount = -1;
        /// <summary>もどらずクリアの大きな★（クリア時だけ作る）</summary>
        private Image bigStar = null;
        /// <summary>左上のヒントボタン（冒険だけ。Game プレハブに入っている）</summary>
        [SerializeField]
        private MumuButton hintButton = null;

        private InGameMap map = null;
        private MapViewController viewController = null;
        private MapInputController inputController = null;
        private int litStarCount = 0;
        /// <summary>ポップアップ表示中か（重ねて開かない）</summary>
        private bool isPopupOpen = false;
        /// <summary>直前に遊んだステージのテーマ（テーマが変わった時の音に使う）</summary>
        private static int lastThemeId = 0;
        /// <summary>このティア以上のパグが仲間になった時は、ファンファーレ</summary>
        private const int RareJoinTier = 4;

        /// <summary>
        /// 破棄時に CSV から作ったマップデータを破棄
        /// </summary>
        private void OnDestroy()
        {
            if (map != null && map.MapData != null)
            {
                Destroy(map.MapData);
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        protected override async UniTask onInitView()
        {
            map = new InGameMap();
            CharacterSprites goalCharacter = CharacterSprites.Load(CharacterMaster.Get(GameSession.GetGoalCharacterId()));
            CharacterSprites playerCharacter = CharacterSprites.Load(CharacterMaster.Get(PugCollection.EquippedId));
            if (await map.InitAsync(mapRoot, destroyCancellationToken, GameSession.CreateMap(), goalCharacter, playerCharacter))
            {
                map.SetOnPathChanged(onPathChanged);
                map.SetOnCleared(onCleared);
                map.SetOnMessage(onMessage);
                map.SetOnRewindConfirm(onRewindConfirm);
                undoButton.SetOnClick(onClickUndo);
                refreshUndo();
                setupHintButton();
                if (usedHints() > 0)
                {
                    map.ShowHint(usedHints());
                }
                refreshHint();
                ThemeBackground.Show(bgImage, map.Sprites);
                if (map.MapData.isBonus)
                {
                    CommonUtility.PlaySe(eSe.BonusEnter);
                }
                if (lastThemeId != 0 && lastThemeId != map.MapData.themeId)
                {
                    CommonUtility.PlaySe(eSe.ThemeWhoosh);
                }
                lastThemeId = map.MapData.themeId;
                SoundManager sound = SoundManager.Instance;
                if (sound != null)
                {
                    sound.PlayThemeBgm(map.MapData.themeId);
                }
                setupViewController();
                int ambientIndex = mapGroup != null ? mapGroup.GetSiblingIndex() + 1 : transform.childCount;
                AmbientParticles.Create(transform, ambientIndex, map.MapData.themeId);
                AmbientCritters.Create(transform, ambientIndex + 1, map.MapData.themeId);
                if (goalGauge != null)
                {
                    goalGauge.Setup(map.Path.RequiredMoveCount, map.PugSprites);
                }
                setStars(map.Path.StarCount, false);
                setCrown(false);
                updateGauge();
                updateBones();
            }
            bgImage.transform.SetAsFirstSibling();

            if (menuButton != null)
            {
                menuButton.SetOnClick(onClickMenuButton);
            }
            if (stageText != null)
            {
                stageText.text = getStageLabel();
            }

            await base.onInitView();

            if (map.MapData != null)
            {
                showGimmickIntroAsync().Forget();
            }
        }

        /// <summary>
        /// 初めて見るギミックの説明を1つずつ出す
        /// </summary>
        private async UniTaskVoid showGimmickIntroAsync()
        {
            // こわれる橋はダイアログではなく、壊れた時に画面下で知らせる
            List<GimmickData> unseen = GimmickMaster.GetUnseen(map.MapData);
            unseen.RemoveAll(_g => _g.Key == GimmickMaster.CrumbleKey);
            if (unseen.Count == 0)
            {
                return;
            }

            // 黒フェードが明けてから出す
            if (await UniTask.Delay(TimeSpan.FromSeconds(FadeScreen.DefaultDuration), cancellationToken: destroyCancellationToken).SuppressCancellationThrow())
            {
                return;
            }

            isPopupOpen = true;
            foreach (GimmickData gimmick in unseen)
            {
                Sprite icon = GimmickMaster.LoadIcon(gimmick);
                await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(TextLocalizer.Get("GIMMICK_NEW_TITLE", gimmick.Name), gimmick.Description, false, icon != null ? new[] { icon } : null), destroyCancellationToken);
                if (destroyCancellationToken.IsCancellationRequested)
                {
                    return;
                }
                GimmickMaster.MarkSeen(gimmick.Key);
            }
            isPopupOpen = false;
        }

        /// <summary>
        /// 画面下のお知らせの処理
        /// </summary>
        /// <param name="_key"></param>
        private void onMessage(string _key)
        {
            PopupManager.Instance.ShowToast(TextLocalizer.Get(_key));
        }

        /// <summary>
        /// ほねの札を更新（増えたら跳ねる）
        /// </summary>
        private void updateBones()
        {
            PuzzlePath path = map.Path;
            if (bonePlate != null)
            {
                bonePlate.SetActive(path.BoneCount > 0);
            }
            if (boneText == null || path.BoneCount == 0)
            {
                return;
            }

            int collected = path.CollectedBoneCount;
            boneText.text = $"{collected}/{path.BoneCount}";
            if (lastBoneCount >= 0 && collected > lastBoneCount)
            {
                UiTween.PunchScale(boneText.transform.parent, Vector3.one, StarPunchAmount, StarPunchDuration, destroyCancellationToken).Forget();
            }
            lastBoneCount = collected;
        }


        /// <summary>
        /// マップ操作の設定
        /// </summary>
        private void setupViewController()
        {
            if (mapGroup == null)
            {
                Debug.LogError("[GameView] mapGroup is not set");
                return;
            }

            // 背景がドラッグを受けて親の mapGroup に伝わる
            if (bgImage != null)
            {
                bgImage.raycastTarget = true;
            }

            // 操作のコンポーネントは Game プレハブに入っている（無ければビルダーで作り直す）
            MapViewController controller = mapGroup.GetComponent<MapViewController>();
            MapInputController input = mapGroup.GetComponent<MapInputController>();
            if (controller != null && input != null)
            {
                controller.Setup((RectTransform)transform, map);
                viewController = controller;
                input.Setup(controller, map);
                inputController = input;
            }
            else
            {
                Debug.LogError("[GameView] マップの操作が付いていません（mumu > Build Game Header で作り直す）");
            }
        }

        /// <summary>
        /// メニューボタン押下時の処理
        /// </summary>
        private void onClickMenuButton()
        {
            openMenuAsync().Forget();
        }

        /// <summary>
        /// メニューを開く
        /// </summary>
        private async UniTaskVoid openMenuAsync()
        {
            if (isPopupOpen || map.Path.IsCleared)
            {
                return;
            }
            isPopupOpen = true;
            Action afterClose = null;
            await PopupManager.Instance.Open(
                ePopup.Settings,
                new SettingsParam(() => afterClose = onRetry, () => afterClose = onBackToTitle),
                destroyCancellationToken);
            isPopupOpen = false;
            afterClose?.Invoke();
        }

        /// <summary>
        /// やりなおし（確認して同じステージを読み直す）
        /// </summary>
        private void onRetry()
        {
            confirmAsync("CONFIRM_RETRY", ViewManager.Instance.RetryStage).Forget();
        }

        /// <summary>
        /// タイトルへ戻る（確認してから）
        /// </summary>
        private void onBackToTitle()
        {
            confirmAsync("CONFIRM_TO_TITLE", ViewManager.Instance.ToTitleWithFade).Forget();
        }

        /// <summary>
        /// 多く戻る時の確認（はいなら戻す）
        /// </summary>
        /// <param name="_cell"></param>
        private void onRewindConfirm(Vector2Int _cell)
        {
            confirmRewindAsync(_cell).Forget();
        }

        /// <summary>
        /// 何マス戻るかを出して、はいなら戻す
        /// </summary>
        /// <param name="_cell"></param>
        private async UniTaskVoid confirmRewindAsync(Vector2Int _cell)
        {
            if (isPopupOpen)
            {
                return;
            }
            isPopupOpen = true;
            int count = map.Path.GetRewindCount(_cell);
            CommonUtility.PlaySe(eSe.RewindConfirm);
            map.SetRewindPreview(_cell, true);
            bool isYes = await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(TextLocalizer.Get("CONFIRM_TITLE"), TextLocalizer.Get("CONFIRM_REWIND", count), true), destroyCancellationToken);
            map.SetRewindPreview(_cell, false);
            isPopupOpen = false;
            if (isYes)
            {
                map.ConfirmRewind(_cell);
            }
        }

        /// <summary>
        /// はい／いいえ で確認して、はいなら実行
        /// </summary>
        /// <param name="_bodyKey"></param>
        /// <param name="_action"></param>
        private async UniTaskVoid confirmAsync(string _bodyKey, Func<UniTask> _action)
        {
            if (isPopupOpen)
            {
                return;
            }
            isPopupOpen = true;
            bool isYes = await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(TextLocalizer.Get("CONFIRM_TITLE"), TextLocalizer.Get(_bodyKey), true), destroyCancellationToken);
            isPopupOpen = false;
            if (isYes)
            {
                _action().Forget();
            }
        }

        /// <summary>
        /// 経路変更時の処理
        /// </summary>
        private void onPathChanged()
        {
            if (!map.Path.IsCleared)
            {
                setStars(map.Path.StarCount, true);
            }
            updateGauge();
            updateBones();
            refreshUndo();
        }

        /// <summary>
        /// 1つ戻すボタンを、戻せる時だけ押せるようにする
        /// </summary>
        private void refreshUndo()
        {
            undoButton.interactable = map.CanUndo && !isPopupOpen;
        }

        /// <summary>
        /// 1つ戻すボタン押下時の処理
        /// </summary>
        private void onClickUndo()
        {
            if (isPopupOpen || map == null)
            {
                return;
            }
            map.UndoLastStep();
            refreshUndo();
        }

        /// <summary>
        /// クリア時の処理
        /// </summary>
        private void onCleared()
        {
            if (map.MapData.isBonus)
            {
                CommonUtility.PlaySe(eSe.BonusClear);
            }
            playClearAsync().Forget();
        }

        /// <summary>
        /// クリアの演出中に画面を押したら、待ち時間を飛ばす
        /// </summary>
        private void Update()
        {
            bool isTapped = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (isClearing && isTapped)
            {
                isClearSkipRequested = true;
            }
        }

        /// <summary>
        /// 指定の秒数待つ。画面を押したら途中で止まる。画面を閉じられたら true
        /// </summary>
        /// <param name="_seconds"></param>
        private async UniTask<bool> delayOrSkipAsync(float _seconds)
        {
            for (float time = 0f; time < _seconds; time += Time.unscaledDeltaTime)
            {
                if (isClearSkipRequested)
                {
                    return false;
                }
                if (await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken).SuppressCancellationThrow())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// クリア演出（2匹のパグに寄って喜び、黒フェードで次のステージへ）
        /// </summary>
        private async UniTaskVoid playClearAsync()
        {
            isClearing = true;
            isClearSkipRequested = false;
            if (inputController != null)
            {
                inputController.SetInputEnabled(false);
            }
            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(false);
            }

            UniTask zoom = UniTask.CompletedTask;
            if (viewController != null)
            {
                zoom = viewController.ZoomToAsync(map.GetGoalPosition() + new Vector2(0f, ClearZoomOffsetY), ClearZoomWidth, ClearZoomDuration, destroyCancellationToken);
            }
            await UniTask.WhenAll(zoom, map.PlayClearAsync(destroyCancellationToken));

            if (stars != null)
            {
                int starCount = map.Path.StarCount;
                setStars(starCount, false);
                for (int i = 0; i < starCount && i < stars.Length; i++)
                {
                    CommonUtility.PlaySe(eSe.StarPing);
                    punchStar(i);
                    if (await delayOrSkipAsync(ClearStarInterval))
                    {
                        return;
                    }
                }
            }

            // 冒険で、指を離してからもどらずにクリアし、★が3つなら大きな★にまとめる
            bool isNoRewind = GameSession.Mode == eGameMode.Adventure && !map.HasRewound;
            if (isNoRewind && stars != null && map.Path.StarCount == stars.Length)
            {
                showBigStar();
                if (await delayOrSkipAsync(ClearStarInterval))
                {
                    return;
                }
            }

            bool isPerfect = map.Path.IsPerfect;
            ConfettiEffect.Play((RectTransform)transform, ConfettiCount, destroyCancellationToken);
            if (isPerfect)
            {
                CommonUtility.PlaySe(eSe.Fanfare);
                setCrown(true);
                if (crownImage != null)
                {
                    UiTween.PunchScale(crownImage.transform, Vector3.one, StarPunchAmount, StarPunchDuration, destroyCancellationToken).Forget();
                }
                HeartEffect.Burst((RectTransform)transform, Vector2.zero, PerfectHeartCount, PerfectHeartRadius, destroyCancellationToken);
            }
            if (await playStampAsync(isPerfect ? "PERFECT" : "CLEAR"))
            {
                return;
            }
            if (await ClearSummary.ShowAsync(transform, map.Path.CollectedBoneCount, map.Path.StarCount, !map.HasRewound, destroyCancellationToken))
            {
                return;
            }

            bool isFirstNoRewind = false;
            if (GameSession.Mode == eGameMode.Adventure)
            {
                // クリアしたらヒントは終わり（次の冒険に持ち越さない）
                SaveDataManager.Data.hintStage = 0;
                isFirstNoRewind = StageRecord.Save(GameSession.PlayedStageNumber, map.Path.StarCount, isPerfect, isNoRewind);
            }
            // クリアしたら、次におにわを開いた時に子が喜ぶ
            SaveDataManager.Data.gardenCheerPending = true;
            SaveDataManager.Save();
            addBones();

            // テーマの最後のステージは、そのテーマのおはなしを1回だけ見せる（次のステージへ進む前）
            if (GameSession.Mode == eGameMode.Adventure && !GameSession.IsReplay && !map.MapData.isBonus && StageProgress.IsLastOfCurrentTheme())
            {
                int themeId = StageProgress.GetCurrentThemeId();
                if (!SaveDataManager.Data.seenThemeStories.Contains(themeId))
                {
                    SaveDataManager.Data.seenThemeStories.Add(themeId);
                    SaveDataManager.Save();
                    await PopupManager.Instance.ShowStoryAsync(TextLocalizer.Get("THEME_STORY_" + themeId));
                }
            }

            // もどらずクリアは初めての時だけボーナス
            if (isFirstNoRewind)
            {
                SaveDataManager.Data.bones += MissionBonusBones;
                SaveDataManager.Save();
                await BoneBonusOverlay.ShowAsync(transform, TextLocalizer.Get("WANDERFUL_BONUS", MissionBonusBones), TextLocalizer.Get("WANDERFUL_DETAIL"), destroyCancellationToken);
            }

            // 報酬は Advance（冒険の初回クリア判定が変わる）より前に受け取る
            int rewardId = GameSession.ClaimReward(out int duplicateBones);
            if (rewardId > 0)
            {
                if (await delayOrSkipAsync(RewardDialogDelay))
                {
                    return;
                }
                await showRewardAsync(rewardId, duplicateBones);
            }
            else if (await delayOrSkipAsync(NextStageDelay))
            {
                return;
            }

            // 今日最後のチャンスだったら、この後はパグに会えないことを知らせる
            if (GameSession.Mode == eGameMode.Stroll && GameSession.IsDailyStroll && GameSession.DailyStrollRemaining <= 1)
            {
                isPopupOpen = true;
                CommonUtility.PlaySe(eSe.AlertChime);
                await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(TextLocalizer.Get("DAILY_DONE_TITLE"), TextLocalizer.Get("DAILY_DONE_BODY"), false), destroyCancellationToken);
                isPopupOpen = false;
            }

            if (!destroyCancellationToken.IsCancellationRequested)
            {
                ViewManager.Instance.ToNextStage().Forget();
            }
        }

        /// <summary>
        /// ヒントボタンに押した時の処理をつける（ボタンは Game プレハブに入っている）
        /// </summary>
        private void setupHintButton()
        {
            if (hintButton != null)
            {
                hintButton.SetOnClick(onClickHintButton);
            }
        }

        /// <summary>
        /// ヒントボタンの押せるか（冒険だけ、ヒントが出ていない時、ほねが足りる時）
        /// </summary>
        private void refreshHint()
        {
            if (hintButton == null)
            {
                return;
            }
            // ヒントは冒険の今のステージだけ（もう一度遊ぶ時は使えない）
            bool isAdventure = GameSession.Mode == eGameMode.Adventure && !GameSession.IsReplay;
            hintButton.gameObject.SetActive(isAdventure);
            hintButton.interactable = isAdventure && canUseMoreHints() && SaveDataManager.Data.bones >= currentHintCost();

            // ボタンの下のほね（ステージで変わるので、ここで書き直す）
            Transform costRect = hintButton.transform.Find("cost");
            if (costRect != null)
            {
                costRect.GetComponent<TMPro.TMP_Text>().text = currentHintCost().ToString();
            }
        }

        /// <summary>
        /// 次に使うヒントのほね（回数 × HintCost）
        /// </summary>
        private int currentHintCost()
        {
            return HintCost * (usedHints() + 1);
        }

        /// <summary>
        /// このステージでヒントを使った回数
        /// </summary>
        private int usedHints()
        {
            return SaveDataManager.Data.hintStage == StageProgress.CurrentNumber ? SaveDataManager.Data.hintUses : 0;
        }

        /// <summary>
        /// このステージで、まだヒントを使えるか（経路の長さで決まる上限まで）
        /// </summary>
        private bool canUseMoreHints()
        {
            return map != null && usedHints() < map.HintMaxUses;
        }

        /// <summary>
        /// ヒントボタン押下時の処理（使うか確認してから、ほねを使って経路の一部を出す）
        /// </summary>
        private void onClickHintButton()
        {
            if (GameSession.IsReplay || !canUseMoreHints() || SaveDataManager.Data.bones < currentHintCost() || isPopupOpen)
            {
                return;
            }
            openHintConfirmAsync().Forget();
        }

        /// <summary>
        /// ヒントを使うか確認する（はい・いいえ）
        /// </summary>
        private async UniTaskVoid openHintConfirmAsync()
        {
            isPopupOpen = true;
            var param = new ConfirmParam(TextLocalizer.Get("HINT_CONFIRM", currentHintCost()), TextLocalizer.Get("HINT_USE"), true, useHint, TextLocalizer.Get("HINT_CANCEL"), true, null);
            await PopupManager.Instance.Open(ePopup.Confirm, param, destroyCancellationToken);
            isPopupOpen = false;
        }

        /// <summary>
        /// ヒントを使う（ほねを使い、経路の一部を出す。クリアまで残る）
        /// </summary>
        private void useHint()
        {
            if (GameSession.IsReplay || !canUseMoreHints() || SaveDataManager.Data.bones < currentHintCost())
            {
                return;
            }

            int uses = usedHints() + 1;
            SaveDataManager.Data.bones -= currentHintCost();
            SaveDataManager.Data.hintStage = StageProgress.CurrentNumber;
            SaveDataManager.Data.hintUses = uses;
            SaveDataManager.Save();
            CommonUtility.PlaySe(uses == 1 ? eSe.HintChime : eSe.HintReveal);
            map.ShowHint(uses);
            refreshHint();
        }

        /// <summary>
        /// ★3つを大きな★1つにまとめて跳ねさせる（もどらずクリア）
        /// </summary>
        private void showBigStar()
        {
            Image middle = stars[stars.Length / 2];
            if (middle == null)
            {
                return;
            }

            foreach (Image star in stars)
            {
                if (star != null)
                {
                    star.gameObject.SetActive(false);
                }
            }
            GameObject go = PartsLoaderManager.Instance.Create(ePart.BigStar, middle.transform.parent);
            if (go == null)
            {
                return;
            }
            bigStar = go.GetComponent<Image>();
            bigStar.sprite = RainbowStar.Still;
            CommonUtility.GetOrAddComponent<SpriteFrameAnimation>(go).Play(RainbowStar.Frames, RainbowStar.TwinkleFps);
            bigStar.rectTransform.sizeDelta = new Vector2(BigStarSize, BigStarSize);
            bigStar.rectTransform.anchoredPosition = ((RectTransform)middle.transform).anchoredPosition;
            CommonUtility.PlaySe(eSe.Sparkle);
            UiTween.PunchScale(bigStar.transform, Vector3.one, StarPunchAmount, StarPunchDuration, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// ステージ番号（1 始まり）のテーマ番号（マップの一覧から）
        /// </summary>
        /// <param name="_stageNumber"></param>
        private static int themeOfStage(int _stageNumber)
        {
            List<MapInfo> list = MapCsv.LoadList();
            int index = _stageNumber - 1;
            return index >= 0 && index < list.Count ? list[index].ThemeId : ThemeDefine.DefaultThemeId;
        }

        /// <summary>
        /// 札の文字（冒険はステージ番号とテーマ名、おさんぽは今日の何回目か、チャンス後は ???）
        /// </summary>
        private static string getStageLabel()
        {
            if (GameSession.Mode == eGameMode.Adventure)
            {
                int number = GameSession.PlayedStageNumber;
                return TextLocalizer.Get("STAGE_THEME", number, TextLocalizer.Get("THEME_NAME_" + themeOfStage(number)));
            }
            if (GameSession.IsDailyStroll)
            {
                return TextLocalizer.Get("STROLL_DAILY", GameSession.DailyStrollNumber, GameSession.DailyStrollCount);
            }
            return TextLocalizer.Get("STAGE", "???");
        }

        /// <summary>
        /// ステージ番号の札をめくって CLEAR!（PERFECT!）に変える（中断されたら true）
        /// </summary>
        /// <param name="_key"></param>
        private async UniTask<bool> playStampAsync(string _key)
        {
            if (stageText == null)
            {
                return false;
            }

            Transform plate = stageText.transform.parent;
            float half = StampFlipDuration * 0.5f;
            for (float time = 0f; time < StampFlipDuration; time += Time.unscaledDeltaTime)
            {
                // 半分で文字を差し替え、縦につぶして戻す
                if (time >= half && stageText.text != TextLocalizer.Get(_key))
                {
                    stageText.text = TextLocalizer.Get(_key);
                }
                plate.localScale = new Vector3(1f, Mathf.Abs(Mathf.Cos(time / StampFlipDuration * Mathf.PI)), 1f);
                if (await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken).SuppressCancellationThrow())
                {
                    return true;
                }
            }
            plate.localScale = Vector3.one;
            stageText.text = TextLocalizer.Get(_key);

            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
            UiTween.PunchScale(plate, Vector3.one, StampPunchAmount, StampPunchDuration, destroyCancellationToken).Forget();
            return false;
        }

        /// <summary>
        /// 拾ったほねを貯める（冒険は前回より多く拾った分だけ）
        /// </summary>
        private void addBones()
        {
            int collected = map.Path.CollectedBoneCount;
            if (collected <= 0)
            {
                return;
            }

            int gained = GameSession.Mode == eGameMode.Adventure ? StageRecord.SaveBones(GameSession.PlayedStageNumber, collected) : collected;
            if (gained > 0)
            {
                CommonUtility.PlaySe(eSe.CoinGet);
                SaveDataManager.Data.bones += gained;
                SaveDataManager.Save();
            }
        }

        /// <summary>
        /// 仲間になったパグを見せる（もう持っている子なら、代わりにもらったほねを見せる）
        /// </summary>
        /// <param name="_characterId"></param>
        /// <param name="_duplicateBones"></param>
        private async UniTask showRewardAsync(int _characterId, int _duplicateBones)
        {
            CharacterData character = CharacterMaster.Get(_characterId);
            CharacterSprites sprites = CharacterSprites.Load(character);
            if (character != null && character.Tier >= RareJoinTier)
            {
                CommonUtility.PlaySe(eSe.RareJoin);
            }
            CommonUtility.PlaySe(eSe.HappyBark);
            bool isDuplicate = _duplicateBones > 0;
            string title = TextLocalizer.Get(isDuplicate ? "PUG_DUPLICATE_TITLE" : "PUG_JOINED_TITLE");
            string body = isDuplicate ? TextLocalizer.Get("PUG_DUPLICATE_BODY", character.Name, _duplicateBones) : TextLocalizer.Get("PUG_JOINED_BODY", character.Name);
            isPopupOpen = true;
            await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(title, body, false, sprites != null ? sprites.Happy : null, character.Tier), destroyCancellationToken);
            isPopupOpen = false;
        }

        /// <summary>
        /// 王冠の表示
        /// </summary>
        /// <param name="_isPerfect"></param>
        private void setCrown(bool _isPerfect)
        {
            if (crownImage != null)
            {
                Sprite sprite = _isPerfect ? crownOn : crownOff;
                if (sprite != null)
                {
                    crownImage.sprite = sprite;
                }
            }
        }

        /// <summary>
        /// ゲージを更新
        /// </summary>
        private void updateGauge()
        {
            if (goalGauge != null)
            {
                PuzzlePath path = map.Path;
                goalGauge.SetRemaining(path.RequiredMoveCount - path.MoveCount, path.IsCleared);
            }
        }

        /// <summary>
        /// ★の点灯数を反映
        /// </summary>
        /// <param name="_count"></param>
        /// <param name="_animate"></param>
        private void setStars(int _count, bool _animate)
        {
            if (stars == null)
            {
                return;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null)
                {
                    continue;
                }

                bool isLit = i < _count;
                Sprite sprite = isLit ? starOn : starOff;
                if (sprite != null)
                {
                    stars[i].sprite = sprite;
                }

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
            if (stars[_index] != null)
            {
                UiTween.PunchScale(stars[_index].transform, Vector3.one, StarPunchAmount, StarPunchDuration, destroyCancellationToken).Forget();
            }
        }
    }
}
