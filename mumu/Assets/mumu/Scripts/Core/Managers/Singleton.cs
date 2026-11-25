namespace mumu
{
    /// <summary>
    /// 通常クラス用シングルトン基底
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static T instance = null;
        private static readonly object lockObject = new object();

        /// <summary>インスタンス（なければ生成）</summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new T();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 二重生成の防止
        /// </summary>
        protected Singleton()
        {
            if (instance != null)
            {
                throw new System.Exception($"Singleton instance of {typeof(T).Name} already exists.");
            }
        }
    }
}
