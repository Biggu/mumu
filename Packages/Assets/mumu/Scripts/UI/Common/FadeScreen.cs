using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// 画面全体の黒フェード（Resources/Prefabs/Common/fade）
    /// </summary>
    public class FadeScreen : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/Common/fade";
        public const float DefaultDuration = 0.4f;

        private static FadeScreen instance = null;

        /// <summary>黒い画像（表示中は入力を止める）</summary>
        [SerializeField]
        private Image image = null;

        /// <summary>インスタンス（なければプレハブから生成）</summary>
        public static FadeScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    FadeScreen prefab = ResourceManager.Instance.Load<FadeScreen>(PrefabPath);
                    if (prefab == null)
                    {
                        return null;
                    }
                    instance = Instantiate(prefab);
                    instance.name = prefab.name;
                    DontDestroyOnLoad(instance.gameObject);
                    instance.setAlpha(0f);
                }
                return instance;
            }
        }

        /// <summary>
        /// 暗くする
        /// </summary>
        /// <param name="_token"></param>
        /// <param name="_duration"></param>
        public UniTask FadeOutAsync(CancellationToken _token, float _duration = DefaultDuration)
        {
            return fadeAsync(1f, _duration, _token);
        }

        /// <summary>
        /// 明るくする
        /// </summary>
        /// <param name="_token"></param>
        /// <param name="_duration"></param>
        public UniTask FadeInAsync(CancellationToken _token, float _duration = DefaultDuration)
        {
            return fadeAsync(0f, _duration, _token);
        }

        /// <summary>
        /// 破棄時に登録解除
        /// </summary>
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// 指定の濃さへ変化
        /// </summary>
        /// <param name="_alpha"></param>
        /// <param name="_duration"></param>
        /// <param name="_token"></param>
        private async UniTask fadeAsync(float _alpha, float _duration, CancellationToken _token)
        {
            float start = image.color.a;
            float time = 0f;
            image.enabled = true;
            while (time < _duration)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
                {
                    return;
                }
                time += Time.unscaledDeltaTime;
                setAlpha(Mathf.Lerp(start, _alpha, time / _duration));
            }
            setAlpha(_alpha);
        }

        /// <summary>
        /// 濃さを反映（透明なら非表示にして入力を通す）
        /// </summary>
        /// <param name="_alpha"></param>
        private void setAlpha(float _alpha)
        {
            Color color = image.color;
            color.a = _alpha;
            image.color = color;
            image.enabled = _alpha > 0f;
        }
    }
}
