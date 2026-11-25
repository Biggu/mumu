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
        /// <summary>ブロックの親</summary>
        [SerializeField]
        private GameObject mapRoot = null;
        /// <summary>マップと背景の親（ドラッグ・ピンチで移動拡大）</summary>
        [SerializeField]
        private RectTransform mapGroup = null;
        /// <summary>1つ戻すボタン</summary>
        [SerializeField]
        private MumuButton undoButton = null;
        /// <summary>背景</summary>
        [SerializeField]
        private Image bgImage = null;
        /// <summary>ヘッダー（ゲージ・★・王冠・ほね・札・メニュー）</summary>
        [SerializeField]
        private GameHeaderPart header = null;
        /// <summary>画面左下の操作の輪</summary>
        [SerializeField]
        private TouchPad touchPad = null;
        /// <summary>左上のヒントボタン</summary>
        [SerializeField]
        private MumuButton hintButton = null;

        /// <summary>クリアの演出中か</summary>
        private bool isClearing = false;
        /// <summary>クリアの演出の待ち時間を、画面を押して飛ばしたか</summary>
        private bool isClearSkipRequested = false;
        private InGameMap map = null;
        private MapViewController viewController = null;
        private MapInputController inputController = null;
        /// <summary>ポップアップ表示中か（重ねて開かない）</summary>
        private bool isPopupOpen = false;
        /// <summary>直前に遊んだステージのテーマ（テーマが変わった時の音に使う）</summary>
        private static int lastThemeId = 0;

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
                int ambientIndex = mapGroup.GetSiblingIndex() + 1;
                AmbientParticles.Create(transform, ambientIndex, map.MapData.themeId);
                AmbientCritters.Create(transform, ambientIndex + 1, map.MapData.themeId);
                header.Init(map);
            }
            bgImage.transform.SetAsFirstSibling();

            header.Setup(onClickMenuButton);
            header.SetStageLabel();

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

            foreach (GimmickData gimmick in unseen)
            {
                Sprite icon = GimmickMaster.LoadIcon(gimmick);
                await openPopupAsync(ePopup.Alert, new AlertParam(MumuText.GIMMICK_NEW_TITLE(gimmick.Name), gimmick.Description, false, icon != null ? new[] { icon } : null));
                if (destroyCancellationToken.IsCancellationRequested)
                {
                    return;
                }
                GimmickMaster.MarkSeen(gimmick.Key);
            }
        }

        /// <summary>
        /// ポップアップを開き、閉じるまで待つ（開いている間は他の操作を止める）
        /// </summary>
        /// <param name="_popup"></param>
        /// <param name="_param"></param>
        private async UniTask<bool> openPopupAsync(ePopup _popup, BaseParam _param)
        {
            isPopupOpen = true;
            bool isYes = await PopupManager.Instance.Open(_popup, _param, destroyCancellationToken);
            isPopupOpen = false;
            return isYes;
        }

        /// <summary>
        /// 画面下のお知らせを出す
        /// </summary>
        /// <param name="_message"></param>
        private void onMessage(string _message)
        {
            PopupManager.Instance.ShowToast(_message);
        }

        /// <summary>
        /// マップ操作の設定
        /// </summary>
        private void setupViewController()
        {
            // 背景がドラッグを受けて親の mapGroup に伝わる
            bgImage.raycastTarget = true;

            // 操作のコンポーネントは Game プレハブに入っている
            viewController = mapGroup.GetComponent<MapViewController>();
            inputController = mapGroup.GetComponent<MapInputController>();
            viewController.Setup((RectTransform)transform, map);
            inputController.Setup(viewController, map, touchPad);
        }

        /// <summary>
        /// メニューボタン押下時の処理
        /// </summary>
        private void onClickMenuButton()
        {
            openMenuAsync().Forget();
        }

        /// <summary>
        /// メニューを開き、選ばれた処理を閉じた後に行う
        /// </summary>
        private async UniTaskVoid openMenuAsync()
        {
            if (isPopupOpen || map.Path.IsCleared)
            {
                return;
            }
            Action afterClose = null;
            await openPopupAsync(ePopup.Settings, new SettingsParam(() => afterClose = onRetry, () => afterClose = onBackToTitle));
            afterClose?.Invoke();
        }

        /// <summary>
        /// やりなおしの確認
        /// </summary>
        private void onRetry()
        {
            confirmAsync(MumuText.CONFIRM_RETRY, ViewManager.Instance.RetryStage).Forget();
        }

        /// <summary>
        /// タイトルへ戻る確認
        /// </summary>
        private void onBackToTitle()
        {
            confirmAsync(MumuText.CONFIRM_TO_TITLE, ViewManager.Instance.ToTitleWithFade).Forget();
        }

        /// <summary>
        /// 多く戻る時の確認
        /// </summary>
        /// <param name="_cell"></param>
        private void onRewindConfirm(Vector2Int _cell)
        {
            confirmRewindAsync(_cell).Forget();
        }

        /// <summary>
        /// 何マス戻るかを確認し、はいなら戻す
        /// </summary>
        /// <param name="_cell"></param>
        private async UniTaskVoid confirmRewindAsync(Vector2Int _cell)
        {
            if (isPopupOpen)
            {
                return;
            }
            int count = map.Path.GetRewindCount(_cell);
            CommonUtility.PlaySe(eSe.RewindConfirm);
            map.SetRewindPreview(_cell, true);
            bool isYes = await openPopupAsync(ePopup.Alert, new AlertParam(MumuText.CONFIRM_TITLE(), MumuText.CONFIRM_REWIND(count), true));
            map.SetRewindPreview(_cell, false);
            if (isYes)
            {
                map.ConfirmRewind(_cell);
            }
        }

        /// <summary>
        /// はい／いいえ で確認し、はいなら実行
        /// </summary>
        /// <param name="_body"></param>
        /// <param name="_action"></param>
        private async UniTaskVoid confirmAsync(Func<string> _body, Func<UniTask> _action)
        {
            if (isPopupOpen)
            {
                return;
            }
            bool isYes = await openPopupAsync(ePopup.Alert, new AlertParam(MumuText.CONFIRM_TITLE(), _body(), true));
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
            header.OnPathChanged();
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
        /// クリア演出の待ち時間（画面を押したら飛ばす）。画面を閉じられたら true
        /// </summary>
        /// <param name="_seconds"></param>
        private UniTask<bool> delayOrSkipAsync(float _seconds)
        {
            return CommonUtility.WaitOrSkipAsync(_seconds, () => isClearSkipRequested, destroyCancellationToken);
        }

        /// <summary>
        /// クリア演出
        /// </summary>
        private async UniTaskVoid playClearAsync()
        {
            isClearing = true;
            isClearSkipRequested = false;
            inputController.SetInputEnabled(false);
            header.HideMenuButton();

            UniTask zoom = viewController.ZoomToAsync(map.GetGoalPosition() + new Vector2(0f, CommonDefine.ClearZoomOffsetY), CommonDefine.ClearZoomWidth, CommonDefine.ClearZoomDuration, destroyCancellationToken);
            await UniTask.WhenAll(zoom, map.PlayClearAsync(destroyCancellationToken));

            // 冒険で、指を離してからもどらずにクリアしたか
            bool isNoRewind = GameSession.Mode == eGameMode.Adventure && !map.HasRewound;
            if (await header.PlayStarsAsync(isNoRewind, () => isClearSkipRequested))
            {
                return;
            }

            bool isPerfect = map.Path.IsPerfect;
            ConfettiEffect.Play((RectTransform)transform, CommonDefine.ConfettiCount, destroyCancellationToken);
            if (isPerfect)
            {
                CommonUtility.PlaySe(eSe.Fanfare);
                header.ShowPerfectCrown();
                HeartEffect.Burst((RectTransform)transform, Vector2.zero, CommonDefine.PerfectHeartCount, CommonDefine.PerfectHeartRadius, destroyCancellationToken);
            }
            if (await header.PlayStampAsync(isPerfect))
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

            await showThemeStoryIfLastAsync();

            // もどらずクリアは初めての時だけボーナス
            if (isFirstNoRewind)
            {
                SaveDataManager.Data.bones += CommonDefine.MissionBonusBones;
                SaveDataManager.Save();
                await BoneBonusOverlay.ShowAsync(transform, MumuText.WANDERFUL_BONUS(CommonDefine.MissionBonusBones), MumuText.WANDERFUL_DETAIL(), destroyCancellationToken);
            }

            if (await playRewardAsync())
            {
                return;
            }

            // 今日最後のチャンスだったら、この後はパグに会えないことを知らせる
            if (GameSession.Mode == eGameMode.Stroll && GameSession.IsDailyStroll && GameSession.DailyStrollRemaining <= 1)
            {
                CommonUtility.PlaySe(eSe.AlertChime);
                await openPopupAsync(ePopup.Alert, new AlertParam(MumuText.DAILY_DONE_TITLE(), MumuText.DAILY_DONE_BODY(), false));
            }

            if (!destroyCancellationToken.IsCancellationRequested)
            {
                ViewManager.Instance.ToNextStage().Forget();
            }
        }

        /// <summary>
        /// テーマの最後のステージなら、そのテーマのおはなしを1回だけ見せる
        /// </summary>
        private async UniTask showThemeStoryIfLastAsync()
        {
            bool isStoryStage = GameSession.Mode == eGameMode.Adventure && !GameSession.IsReplay && !map.MapData.isBonus;
            if (!isStoryStage || !StageProgress.IsLastOfCurrentTheme())
            {
                return;
            }

            int themeId = StageProgress.GetCurrentThemeId();
            if (SaveDataManager.Data.seenThemeStories.Contains(themeId))
            {
                return;
            }
            SaveDataManager.Data.seenThemeStories.Add(themeId);
            SaveDataManager.Save();
            await PopupManager.Instance.ShowStoryAsync(MumuText.THEME_STORY(themeId));
        }

        /// <summary>
        /// 報酬（仲間になったパグ）を見せ、次のステージへ進むまで待つ（中断されたら true）
        /// </summary>
        private async UniTask<bool> playRewardAsync()
        {
            // 報酬は Advance（冒険の初回クリア判定が変わる）より前に受け取る
            int rewardId = GameSession.ClaimReward(out int duplicateBones);
            if (rewardId > 0)
            {
                if (await delayOrSkipAsync(CommonDefine.RewardDialogDelay))
                {
                    return true;
                }
                await showRewardAsync(rewardId, duplicateBones);
                return false;
            }
            return await delayOrSkipAsync(CommonDefine.NextStageDelay);
        }

        /// <summary>
        /// ヒントボタン押下時の処理をつける
        /// </summary>
        private void setupHintButton()
        {
            hintButton.SetOnClick(onClickHintButton);
        }

        /// <summary>
        /// ヒントボタンの押せる状態を更新し、ほねの数を書き直す
        /// </summary>
        private void refreshHint()
        {
            bool isHintStage = isHintAvailableStage();
            hintButton.gameObject.SetActive(isHintStage);
            hintButton.interactable = isHintStage && canUseMoreHints() && SaveDataManager.Data.bones >= currentHintCost();

            // ボタンの下のほね（ステージで変わるので、ここで書き直す）
            hintButton.transform.Find("cost").GetComponent<TMP_Text>().text = currentHintCost().ToString();
        }

        /// <summary>
        /// ヒントを使えるステージか（冒険の今のステージだけ。もう一度遊ぶ時は使えない）
        /// </summary>
        private static bool isHintAvailableStage()
        {
            return GameSession.Mode == eGameMode.Adventure && !GameSession.IsReplay;
        }

        /// <summary>
        /// 次に使うヒントのほね
        /// </summary>
        private int currentHintCost()
        {
            return CommonDefine.HintCost * (usedHints() + 1);
        }

        /// <summary>
        /// このステージでヒントを使った回数
        /// </summary>
        private int usedHints()
        {
            return SaveDataManager.Data.hintStage == StageProgress.CurrentNumber ? SaveDataManager.Data.hintUses : 0;
        }

        /// <summary>
        /// このステージで、まだヒントを使えるか
        /// </summary>
        private bool canUseMoreHints()
        {
            return map != null && usedHints() < map.HintMaxUses;
        }

        /// <summary>
        /// ヒントボタン押下時の処理
        /// </summary>
        private void onClickHintButton()
        {
            if (!isHintAvailableStage() || !canUseMoreHints() || SaveDataManager.Data.bones < currentHintCost() || isPopupOpen)
            {
                return;
            }
            var param = new ConfirmParam(MumuText.HINT_CONFIRM(currentHintCost()), MumuText.HINT_USE(), true, useHint, MumuText.HINT_CANCEL(), true, null);
            openPopupAsync(ePopup.Confirm, param).Forget();
        }

        /// <summary>
        /// ヒントを使う（ほねを使い、経路の一部を出す）
        /// </summary>
        private void useHint()
        {
            if (!isHintAvailableStage() || !canUseMoreHints() || SaveDataManager.Data.bones < currentHintCost())
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
            PopupManager.Instance.Notify(TextLocalizer.Get("HINT_USED"));
            refreshHint();
        }

        /// <summary>
        /// 拾ったほねを貯める
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
        /// 仲間になったパグ（もう持っている子ならほね）を見せる
        /// </summary>
        /// <param name="_characterId"></param>
        /// <param name="_duplicateBones"></param>
        private async UniTask showRewardAsync(int _characterId, int _duplicateBones)
        {
            CharacterData character = CharacterMaster.Get(_characterId);
            CharacterSprites sprites = CharacterSprites.Load(character);
            if (character != null && character.Tier >= CommonDefine.RareJoinTier)
            {
                CommonUtility.PlaySe(eSe.RareJoin);
            }
            CommonUtility.PlaySe(eSe.HappyBark);
            bool isDuplicate = _duplicateBones > 0;
            string title = (isDuplicate ? MumuText.PUG_DUPLICATE_TITLE() : MumuText.PUG_JOINED_TITLE());
            string body = isDuplicate ? MumuText.PUG_DUPLICATE_BODY(character.Name, _duplicateBones) : MumuText.PUG_JOINED_BODY(character.Name);
            await openPopupAsync(ePopup.Alert, new AlertParam(title, body, false, sprites != null ? sprites.Happy : null, character.Tier));
        }
    }
}
