using UnityEngine;


[DefaultExecutionOrder(100)]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameField gameField;
    [SerializeField] MatchManager matchManager;
    [SerializeField] SwapManager swapManager;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (gameField == null || matchManager == null || swapManager == null)
        {
            Debug.LogError("Bootstrap: Не установлены ссылки инспекторе!");
            return;
        }

        gameField.Setup(matchManager, swapManager);
        matchManager.Setup(gameField);
        swapManager.Setup(gameField);
    }
}
