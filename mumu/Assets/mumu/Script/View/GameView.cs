using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameView : ViewBase
{
    [SerializeField]
    private GameObject mapRoot = null;
       private InGameMap map = null;

    protected override async UniTask onInitView()
    {
        map = new InGameMap();
        await map.InitAsync(mapRoot);

        await base.onInitView();
    }

    void Update()
    {
        map.Update();
    }

}
