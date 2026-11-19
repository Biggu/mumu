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

#if UNITY_EDITOR
        /// <summary>B で増やすほねの数</summary>
        private const int DebugBones = 999999;

        /// <summary>
        /// デバッグ操作（V: パグを全部仲間に、B: ほねを増やす）
        /// </summary>
        private void Update()
        {
            if (isPopupOpen || ViewManager.Instance.IsTransitioning)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                UserSaveData data = SaveDataManager.Data;
                int added = 0;
                foreach (CharacterData character in CharacterMaster.All)
                {
                    if (!data.pugs.Contains(character.Id))
                    {
                        data.pugs.Add(character.Id);
                        added++;
                    }
                }
                SaveDataManager.Save();
                Debug.Log($"[TitleView] Debug: added {added} pugs (total {data.pugs.Count})");
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                UserSaveData data = SaveDataManager.Data;
                data.bones = (int)System.Math.Min((long)data.bones + DebugBones, int.MaxValue);
                SaveDataManager.Save();
                Debug.Log($"[TitleView] Debug: bones {data.bones}");
            }
        }
#endif

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
            UiImageUtility.SetTiled(bgImage, map.Sprites.Background);
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
                await MessageDialog.ShowAsync(transform, "ALL_CLEAR_TITLE", "ALL_CLEAR_BODY", false, destroyCancellationToken);
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
                await MessageDialog.ShowAsync(transform, "DAILY_DONE_TITLE", "DAILY_DONE_BODY", false, destroyCancellationToken);
                isPopupOpen = false;
                if (destroyCancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            GameSession.StartStroll();
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
            await SettingsPopup.OpenAsync(transform, null, null, destroyCancellationToken);
            isPopupOpen = false;
        }
    }
}
