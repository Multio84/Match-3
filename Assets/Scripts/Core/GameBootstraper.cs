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
    [SerializeField] ChipDestroyer chipDestroyer;
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
            collapseHandler == null ||
            chipDestroyer == null)
        {
            Debug.LogError("GameBootstrapper: Не установлены ссылки в инспекторе!");
            return;
        }

        gameField.Setup(matchFinder, swapHandler, collapseHandler, chipDestroyer);
        levelGenerator.Setup(gameField, matchFinder, collapseHandler, chipDestroyer);
        matchFinder.Setup(gameField);
        swapHandler.Setup(gameField);
        collapseHandler.Setup(gameField, levelGenerator, matchFinder, chipDestroyer);
        chipDestroyer.Setup(gameField, collapseHandler);

        processor.Setup(levelGenerator);
    }

    void Init()
    {
        initializables = new IInitializable[]
        {
            gameField,
            matchFinder
        };

        foreach (var initializable in initializables)
        {
            initializable.Init();
        }
    }

}
