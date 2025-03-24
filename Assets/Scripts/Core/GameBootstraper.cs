using UnityEngine;


[DefaultExecutionOrder(-1000)]
public class GameBootstraper : MonoBehaviour
{
    [SerializeField] GameProcessor gameProcessor;
    [SerializeField] GameplayConductor gameplayConductor;
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
        if (gameProcessor == null ||
            gameplayConductor == null ||
            gameField == null ||
            levelGenerator == null ||
            matchFinder == null ||
            swapHandler == null ||
            collapseHandler == null ||
            chipDestroyer == null)
        {
            Debug.LogError("GameBootstrapper: �� ����������� ������ � ����������!");
            return;
        }

        gameField.Setup(swapHandler);
        levelGenerator.Setup(gameField, collapseHandler);
        matchFinder.Setup(gameField);
        swapHandler.Setup(gameField, matchFinder);
        collapseHandler.Setup(gameField, levelGenerator);
        chipDestroyer.Setup(gameField, collapseHandler);

        gameProcessor.Setup(gameplayConductor);
        gameplayConductor.Setup(gameField, levelGenerator, matchFinder, swapHandler, collapseHandler, chipDestroyer);
    }

    void Init()
    {
        initializables = new IInitializable[]
        {
            gameField,
            matchFinder,
            gameplayConductor
        };

        foreach (var initializable in initializables)
        {
            initializable.Init();
        }
    }

}
