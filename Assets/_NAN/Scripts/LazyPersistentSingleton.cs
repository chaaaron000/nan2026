using UnityEngine;

/// <summary>
/// 최초 접근 시 인스턴스를 자동 생성하고 씬 전환 후에도 유지하는 제너릭 싱글톤입니다.
/// </summary>
/// <typeparam name="T">싱글톤으로 관리할 컴포넌트 형식입니다.</typeparam>
public abstract class LazyPersistentSingleton<T> : MonoBehaviour where T : LazyPersistentSingleton<T>
{
    private static T _instance;

    /// <summary>
    /// 현재 인스턴스를 반환하며, 존재하지 않으면 새 게임 오브젝트에 자동 생성합니다.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (_instance != null)
            {
                _instance.MakePersistent();
                return _instance;
            }

            GameObject singletonObject = new(typeof(T).Name);
            _instance = singletonObject.AddComponent<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
        MakePersistent();
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void MakePersistent()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }
}