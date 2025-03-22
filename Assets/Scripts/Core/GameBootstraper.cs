using UnityEngine;


[DefaultExecutionOrder(-1000)]
public class GameBootstraper : MonoBehaviour
{
    [SerializeField] GameProcessor processor;
    [SerializeField] GameField gameField;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] MatchFinder matchFinder;
    [SerializeField] SwapHandler swapHandler;
    [SerializeField] CollapseHandler collapseHandler;
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
            matchFinder == null ||
            swapHandler == null ||
            collapseHandler == null)
        {
            Debug.LogError("GameBootstrapper: Не установлены ссылки в инспекторе!");
            return;
        }

        gameField.Setup(matchFinder, swapHandler, collapseHandler);
        levelGenerator.Setup(gameField, matchFinder, collapseHandler);
        matchFinder.Setup(gameField);
        swapHandler.Setup(gameField);
        collapseHandler.Setup(gameField,levelGenerator,matchFinder);

        processor.Setup(levelGenerator);
    }

    void Init()
    {
        initializables = new IInitializable[]
        {
            gameField,
            matchFinder,
        };

        foreach (var initializable in initializables)
        {
            initializable.Init();
        }
    }

}
