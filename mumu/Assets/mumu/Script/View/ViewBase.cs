using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ViewBase : MonoBehaviour
{
    
    public async UniTask OnInitView()
    {
        await onInitView();
    }


    protected virtual UniTask onInitView()
    {
       // Override in derived classes for view initialization logic
       return UniTask.CompletedTask;
    }
}
