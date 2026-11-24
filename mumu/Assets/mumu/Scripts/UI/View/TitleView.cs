using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// タイトル画面（背景にランダムなステージを表示）
    /// </summary>
    public class TitleView : ViewBase
    {
        /// <summary>背景ステージが画面に占める割合（横）</summary>
        private const float StageFitRateX = 0.9f;
        /// <summary>背景ステージが画面に占める割合（縦）</summary>
        private const float StageFitRateY = 0.62f;
        /// <summary>背景ステージの中心のずらし量（タイトル文字を避けて下へ）</summary>
        private static readonly Vector2 StageOffset = new Vector2(0f, -70f);
        /// <summary>見本として表示する経路の割合</summary>
        private const float DemoPathRate = 0.4f;

        /// <summary>背景ステージのブロックの親</summary>
        [SerializeField]
        private GameObject mapRoot = null;
        /// <summary>背景ステージの親（画面に合わせて拡大）</summary>
        [SerializeField]
        private RectTransform mapGroup = null;
        /// <summary>背景</summary>
        [SerializeField]
        private Image bgImage = null;
        /// <summary>冒険ボタン</summary>
        [SerializeField]
        private MumuButton adventureButton = null;
        /// <summary>おさんぽボタン</summary>
        [SerializeField]
        private MumuButton strollButton = null;
        /// <summary>おにわボタン</summary>
        [SerializeField]
        private MumuButton gardenButton = null;
        /// <summary>設定ボタン</summary>
        [SerializeField]
        private MumuButton settingsButton = null;

        private MapData stageData = null;
        /// <summary>ポップアップ表示中か（重ねて開かない）</summary>
        private bool isPopupOpen = false;

        /// <summary>
        /// 初期化
        /// </summary>
        protected override async UniTask onInitView()
        {
            if (adventureButton != null)
            {
                adventureButton.SetOnClick(onClickAdventureButton);
            }
            if (strollButton != null)
            {
                strollButton.SetOnClick(onClickStrollButton);
                showStrollChances();
            }
            if (gardenButton != null)
            {
                gardenButton.SetOnClick(onClickGardenButton);
            }
            if (settingsButton != null)
            {
                settingsButton.SetOnClick(onClickSettingsButton);
            }

            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlayTitleBgm();
                CommonUtility.PlaySe(eSe.BirdFly);
            }

            await setupStageAsync();
            await base.onInitView();
        }

        /// <summary>
        /// 破棄時に生成したマップデータを破棄
        /// </summary>
        private void OnDestroy()
        {
            if (stageData != null)
            {
                Destroy(stageData);
            }
        }


        /// <summary>
        /// 背景にランダムなステージを表示（操作不可）
        /// </summary>
        private async UniTask setupStageAsync()
        {
            if (mapRoot == null || mapGroup == null)
            {
                return;
            }

            stageData = TitleStage.CreateRandom();
            var map = new InGameMap();
            if (!await map.InitAsync(mapRoot, destroyCancellationToken, stageData))
            {
                return;
            }

            map.ShowDemoPath(DemoPathRate);
            ThemeBackground.Show(bgImage, map.Sprites);
            fitStage(map.GetContentBounds());
            AmbientParticles.Create(mapGroup.parent, mapGroup.GetSiblingIndex() + 1, stageData.themeId);
            AmbientCritters.Create(mapGroup.parent, mapGroup.GetSiblingIndex() + 2, stageData.themeId);
        }

        /// <summary>
        /// ステージを画面に合わせる
        /// </summary>
        /// <param name="_bounds"></param>
        private void fitStage(Rect _bounds)
        {
            Rect view = ((RectTransform)transform).rect;
            float scale = Mathf.Min(view.width * StageFitRateX / _bounds.width, view.height * StageFitRateY / _bounds.height);
            mapGroup.localScale = new Vector3(scale, scale, 1f);
            mapGroup.anchoredPosition = StageOffset - _bounds.center * scale;
        }

        /// <summary>
        /// 冒険ボタン押下時の処理
        /// </summary>
        private void onClickAdventureButton()
        {
            startAdventureAsync().Forget();
        }

        /// <summary>
        /// 冒険を始める（全ステージクリア済みなら先にお知らせを出し、最後のステージを繰り返す）
        /// </summary>
        private async UniTaskVoid startAdventureAsync()
        {
            if (isPopupOpen)
            {
                return;
            }

            if (StageProgress.IsAllCleared())
            {
                isPopupOpen = true;
                await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(TextLocalizer.Get("ALL_CLEAR_TITLE"), TextLocalizer.Get("ALL_CLEAR_BODY"), false), destroyCancellationToken);
                isPopupOpen = false;
                if (destroyCancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            GameSession.StartAdventure();
            GameSession.WalkFromIndex = -1;
            ViewManager.Instance.ToStageMap().Forget();
        }

        /// <summary>
        /// おさんぽボタン押下時の処理
        /// </summary>
        private void onClickStrollButton()
        {
            startStrollAsync().Forget();
        }

        /// <summary>
        /// 今日のおさんぽの残りのチャンスを、おさんぽボタンの下に出す
        /// </summary>
        private void showStrollChances()
        {
            TMPro.TMP_Text label = FloatText.CreateLabel(strollButton.transform, TextLocalizer.Get("STROLL_CHANCES", GameSession.DailyStrollRemaining, GameSession.DailyStrollCount), new Color(0.29f, 0.17f, 0.06f));
            if (label == null)
            {
                return;
            }
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
        }

        /// <summary>
        /// おさんぽを始める（今日のチャンスを使い切っていたら先に知らせる）
        /// </summary>
        private async UniTaskVoid startStrollAsync()
        {
            if (isPopupOpen)
            {
                return;
            }

            if (GameSession.DailyStrollRemaining == 0)
            {
                isPopupOpen = true;
                CommonUtility.PlaySe(eSe.AlertChime);
                await PopupManager.Instance.Open(ePopup.Alert, new AlertParam(TextLocalizer.Get("DAILY_DONE_TITLE"), TextLocalizer.Get("DAILY_DONE_BODY"), false), destroyCancellationToken);
                isPopupOpen = false;
                if (destroyCancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            GameSession.StartStroll();
            if (GameSession.IsDailyStroll)
            {
                CommonUtility.PlaySe(eSe.StrollStart);
            }
            ViewManager.Instance.ToGame().Forget();
        }

        /// <summary>
        /// おにわボタン押下時の処理
        /// </summary>
        private void onClickGardenButton()
        {
            if (isPopupOpen)
            {
                return;
            }
            ViewManager.Instance.ToGarden().Forget();
        }

        /// <summary>
        /// 設定ボタン押下時の処理
        /// </summary>
        private void onClickSettingsButton()
        {
            openSettingsAsync().Forget();
        }

        /// <summary>
        /// 設定ポップアップを開く
        /// </summary>
        private async UniTaskVoid openSettingsAsync()
        {
            if (isPopupOpen)
            {
                return;
            }
            isPopupOpen = true;
            await PopupManager.Instance.Open(ePopup.Settings, new SettingsParam(null, null), destroyCancellationToken);
            isPopupOpen = false;
        }
    }
}
