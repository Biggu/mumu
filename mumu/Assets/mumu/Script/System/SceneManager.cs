using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages scene/view loading and transitions in the game.
/// </summary>
public enum ViewType
{
    Title,
    Game,
}

/// <summary>
/// Singleton manager for handling scene transitions and view loading.
/// </summary>
public class SceneManager : MonoSingleton<SceneManager>
{
    [SerializeField]
    private GameObject viewRoot = null;

    private ViewBase currentView = null;

    /// <summary>
    /// Loads a view of the specified type asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of view to load.</typeparam>
    /// <param name="viewType">The type of view to instantiate.</param>
    /// <returns>The loaded view instance.</returns>
    private async UniTask<T> loadView<T>(ViewType viewType) where T : ViewBase
    {
        // Unload current view if exists
        if (currentView != null)
        {
            Destroy(currentView.gameObject);
        }

        // Load new view from Resources/Prefabs
        
        T viewPrefab = await ResourceManager.Instance.LoadAsync<T>( ResourceManager.GetViewPath(viewType));

        if (viewPrefab == null)
        {
            return null;
        }

        // Instantiate and add to view root
        currentView = Instantiate(viewPrefab, viewRoot.transform);
        await currentView.OnInitView();
        return currentView as T;
    }

    /// <summary>
    /// Unloads the currently active view.
    /// </summary>
    public void UnloadCurrentView()
    {
        if (currentView != null)
        {
            Destroy(currentView);
            currentView = null;
        }
    }

    /// <summary>
    /// Transitions to the title scene.
    /// </summary>
    public async UniTask ToTitle()
    {
        await loadView<TitleView>(ViewType.Title);
    }

    /// <summary>
    /// Transitions to the game scene.
    /// </summary>
    public async UniTask ToGame()
    {
        await loadView<GameView>(ViewType.Game);
    }
}
