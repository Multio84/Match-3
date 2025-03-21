using UnityEngine;


[DefaultExecutionOrder(100)]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameProcessor processor;
    [SerializeField] GameField gameField;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] MatchManager matchManager;
    [SerializeField] SwapManager swapManager;

    IInitializable[] initializables;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Setup();
        Init();
    }

    void Setup()
    {
        if (processor == null ||
            gameField == null ||
            levelGenerator == null ||
            matchManager == null ||
            swapManager == null)
        {
            Debug.LogError("Bootstrap: Не установлены ссылки в инспекторе!");
            return;
        }

        gameField.Setup(levelGenerator, matchManager, swapManager);
        levelGenerator.Setup(gameField, matchManager);
        matchManager.Setup(gameField);
        swapManager.Setup(gameField);

        processor.Setup(levelGenerator);
    }

    void Init()
    {
        initializables = new IInitializable[]
        {
            gameField,
            matchManager,
        };

        foreach (var initializable in initializables)
        {
            initializable.Init();
        }
    }

}
