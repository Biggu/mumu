using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// 画面の種類（Resources/Prefabs/View/{名前}）
    /// </summary>
    public enum eViewType
    {
        Title,
        Game,
        Garden,
        StageMap,
    }

    /// <summary>
    /// 画面の切り替え管理
    /// </summary>
    public class ViewManager : MonoSingleton<ViewManager>
    {
        /// <summary>画面の親</summary>
        [SerializeField]
        private GameObject viewRoot = null;

        private ViewBase currentView = null;
        private bool isTransitioning = false;

        /// <summary>遷移中か</summary>
        public bool IsTransitioning => isTransitioning;

        /// <summary>
        /// 現在の画面を破棄
        /// </summary>
        public void UnloadCurrentView()
        {
            if (currentView != null)
            {
                Destroy(currentView.gameObject);
            }
            currentView = null;
        }

        /// <summary>
        /// タイトル画面へ
        /// </summary>
        public async UniTask ToTitle()
        {
            await loadView<TitleView>(eViewType.Title);
        }

        /// <summary>
        /// ゲーム画面へ
        /// </summary>
        public async UniTask ToGame()
        {
            await loadView<GameView>(eViewType.Game);
        }

        /// <summary>
        /// 冒険のステージマップへ
        /// </summary>
        public async UniTask ToStageMap()
        {
            await loadView<StageMapView>(eViewType.StageMap);
        }

        /// <summary>
        /// 黒フェードしてステージマップへ
        /// </summary>
        public UniTask ToStageMapWithFade()
        {
            return fadeTransitionAsync(ToStageMap);
        }

        /// <summary>
        /// 黒フェードしてゲーム画面へ
        /// </summary>
        public UniTask ToGameWithFade()
        {
            return fadeTransitionAsync(ToGame);
        }

        /// <summary>
        /// おにわへ
        /// </summary>
        public async UniTask ToGarden()
        {
            await loadView<GardenView>(eViewType.Garden);
        }

        /// <summary>
        /// 黒フェードして次へ（冒険はステージマップ、おさんぽは新しいマップ）
        /// </summary>
        public UniTask ToNextStage()
        {
            return fadeTransitionAsync(() =>
            {
                // 冒険はステージマップで次のステージまで歩いてから始める
                GameSession.Advance();
                return GameSession.Mode == eGameMode.Adventure ? ToStageMap() : ToGame();
            });
        }

        /// <summary>
        /// 黒フェードして同じステージをやりなおす（おさんぽも同じマップ）
        /// </summary>
        public UniTask RetryStage()
        {
            return fadeTransitionAsync(ToGame);
        }

        /// <summary>
        /// 黒フェードしてタイトル画面へ
        /// </summary>
        public UniTask ToTitleWithFade()
        {
            return fadeTransitionAsync(ToTitle);
        }

        /// <summary>
        /// 暗くしてから画面を切り替え、明るくする
        /// </summary>
        /// <param name="_load"></param>
        private async UniTask fadeTransitionAsync(Func<UniTask> _load)
        {
            if (isTransitioning)
            {
                return;
            }

            FadeScreen fade = FadeScreen.Instance;
            if (fade != null)
            {
                await fade.FadeOutAsync(destroyCancellationToken);
            }

            await _load();

            if (fade != null)
            {
                await fade.FadeInAsync(destroyCancellationToken);
            }
        }

        /// <summary>
        /// 画面を読み込んで差し替え（遷移中は無視）
        /// </summary>
        /// <param name="_viewType"></param>
        private async UniTask<T> loadView<T>(eViewType _viewType) where T : ViewBase
        {
            if (isTransitioning)
            {
                return null;
            }

            if (viewRoot == null)
            {
                Debug.LogError("[ViewManager] viewRoot が設定されていません（system シーンを確認）");
                return null;
            }

            isTransitioning = true;
            try
            {
                T viewPrefab = await ResourceManager.Instance.LoadAsync<T>(ResourceManager.GetViewPath(_viewType), destroyCancellationToken);
                if (viewPrefab == null)
                {
                    return null;
                }

                UnloadCurrentView();

                T view = Instantiate(viewPrefab, viewRoot.transform);
                currentView = view;
                await view.OnInitView();
                return view;
            }
            finally
            {
                isTransitioning = false;
            }
        }
    }
}
