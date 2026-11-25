using UnityEngine;

namespace mumu
{
    /// <summary>
    /// MonoSingleton 共通の終了フラグ（ジェネリック型では RuntimeInitializeOnLoadMethod が使えないため分離）
    /// </summary>
    internal static class MonoSingletonState
    {
        /// <summary>アプリ終了中か</summary>
        public static bool IsQuitting { get; private set; }

        /// <summary>
        /// 再生開始時にリセット（ドメインリロード無効時の対策）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void reset()
        {
            IsQuitting = false;
            Application.quitting -= onQuitting;
            Application.quitting += onQuitting;
        }

        /// <summary>
        /// アプリ終了時の処理
        /// </summary>
        private static void onQuitting()
        {
            IsQuitting = true;
        }
    }

    /// <summary>
    /// MonoBehaviour 用シングルトン基底
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance = null;

        /// <summary>インスタンス（なければ生成、終了中は null）</summary>
        public static T Instance
        {
            get
            {
                if (MonoSingletonState.IsQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                    {
                        // シーンに無いので空の物を作る（動かすためだが、システムシーンの設定漏れに気付けるようエラーを出す）
                        Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} がシーンにありません。空の物を作りました（system シーンを確認）");
                        // AddComponent 内で Awake が走り DontDestroyOnLoad される
                        instance = new GameObject(typeof(T).Name).AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// インスタンス登録
        /// </summary>
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
            }
            else if (instance != this)
            {
                // 同じ GameObject の他コンポーネントを巻き込まないよう自身だけ破棄
                Destroy(this);
                return;
            }

            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// インスタンス解除
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
