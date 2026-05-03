using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Singleton manager for loading resources from Resources folder.
/// </summary>
public class ResourceManager : MonoSingleton<ResourceManager>
{

    public static string GetViewPath(ViewType _viewType)=> $"View/{_viewType}";
    public static string GetInGamePrefabPath(string _prefabName)=> $"Prefabs/InGame/{_prefabName}";
    public static string MapDataPath = $"MapData/mapdata";

    /// <summary>
    /// Loads a resource of the specified type from the Resources folder.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    /// <param name="path">The path to the resource relative to a Resources folder.</param>
    /// <returns>The loaded resource instance, or null if not found.</returns>
    public T Load<T>(string path) where T : Object
    {
        T resource = Resources.Load<T>(path);
        
        if (resource == null)
        {
            Debug.LogError($"[ResourceManager] Resource not found at path: {path}");
        }
        
        return resource;
    }

    /// <summary>
    /// Loads all resources of the specified type from the Resources folder.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    /// <param name="path">The path to the resources relative to a Resources folder.</param>
    /// <returns>An array of loaded resources.</returns>
    public T[] LoadAll<T>(string path) where T : Object
    {
        T[] resources = Resources.LoadAll<T>(path);
        return resources;
    }

    /// <summary>
    /// Asynchronously loads a resource of the specified type from the Resources folder.
    /// </summary>
    /// <typeparam name="T">The type of resource to load.</typeparam>
    /// <param name="path">The path to the resource relative to a Resources folder.</param>
    /// <returns>The loaded resource instance, or null if not found.</returns>
    public async UniTask<T> LoadAsync<T>(string path) where T : Object
    {
        ResourceRequest request = Resources.LoadAsync<T>(path);
        
        await request;
        
        if (request.asset == null)
        {
            Debug.LogError($"[ResourceManager] Resource not found at path: {path}");
        }
        
        return request.asset as T;
    }
}