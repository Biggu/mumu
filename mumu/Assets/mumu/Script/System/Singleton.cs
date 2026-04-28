/// <summary>
/// Singleton base class for normal (non-MonoBehaviour) classes.
/// Usage: Inherit from Singleton<T> in your class.
/// </summary>
public class Singleton<T> where T : class, new()
{
    private static T instance = null;
    private static readonly object lockObject = new object();

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

    protected Singleton()
    {
        if (instance != null)
        {
            throw new System.Exception($"Singleton instance of {typeof(T).Name} already exists.");
        }
    }
}
