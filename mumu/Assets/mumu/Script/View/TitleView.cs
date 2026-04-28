using Cysharp.Threading.Tasks;
using UnityEngine;

public class TitleView : ViewBase
{
    [SerializeField] private MumuButton startButton = null;

    protected override async UniTask onInitView()
    {
        startButton.SetOnClick(OnStartButtonClicked);
        await base.onInitView();
    }

    private void OnStartButtonClicked()
    {
       SceneManager.Instance.ToGame().Forget();
    }
}
