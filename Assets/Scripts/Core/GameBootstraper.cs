using UnityEngine;


[DefaultExecutionOrder(-1000)]
public class GameBootstraper : MonoBehaviour
{
    [SerializeField] GameProcessor processor;
    [SerializeField] GameField gameField;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] MatchManager matchManager;
    [SerializeField] SwapManager swapManager;
    [SerializeField] CollapseManager collapseManager;
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
            swapManager == null ||
            collapseManager == null)
        {
            Debug.LogError("GameBootstrapper: Не установлены ссылки в инспекторе!");
            return;
        }

        gameField.Setup(matchManager, swapManager, collapseManager);
        levelGenerator.Setup(gameField, matchManager, collapseManager);
        matchManager.Setup(gameField);
        swapManager.Setup(gameField);
        collapseManager.Setup(gameField,levelGenerator,matchManager);

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
