using Cysharp.Threading.Tasks;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    private void Awake()
    {
        init().Forget();
    }

    private async UniTaskVoid init()
    {
        var sceneManager = SceneManager.Instance;
        await sceneManager.ToTitle();
    }
}
