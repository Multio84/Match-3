using UnityEngine;


public class GameMode : MonoBehaviour
{
    public static GameMode Instance { get; private set; }
    public GameField gameField;


    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this) {
            Destroy(gameObject);
        }
    }

}


