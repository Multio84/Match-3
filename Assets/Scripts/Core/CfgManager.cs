using UnityEngine;


public class CfgManager : MonoBehaviour
{
    public static CfgManager Instance;
    public GameSettings settings;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
