using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    
    public static DontDestroy Instance { get; private set; }

    void Start()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }
}
