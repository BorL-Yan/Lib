
using UnityEngine;

public class SingletonScene<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance found. Destroying: {gameObject.name}");
            Destroy(gameObject);
        }
        Init();
    }

    protected virtual void Init()
    {}
    
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
