using UnityEngine;


[DefaultExecutionOrder(-1000)]
public class GameBootstraper : MonoBehaviour
{
    [SerializeField] GameSettings settings;
    [SerializeField] GameProcessor gameProcessor;
    [SerializeField] GameplayConductor gameplayConductor;
    [SerializeField] GameField gameField;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] MatchFinder matchFinder;
    [SerializeField] SwapHandler swapHandler;
    [SerializeField] CollapseHandler collapseHandler;
    [SerializeField] ChipDestroyer chipDestroyer;
    IInitializer[] preloadables;
    SettingsSubscriber[] settingsSubscribers;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Setup();
        UseSettings();
        Preload();
    }

    // sets dependencies
    void Setup()
    {
        if (settings == null ||
            gameProcessor == null ||
            gameplayConductor == null ||
            gameField == null ||
            levelGenerator == null ||
            matchFinder == null ||
            swapHandler == null ||
            collapseHandler == null ||
            chipDestroyer == null)
        {
            Debug.LogError("GameBootstrapper: Some links are not set in the inspector!");
            return;
        }

        gameField.Setup(settings);
        levelGenerator.Setup(settings, gameField, swapHandler);
        matchFinder.Setup(gameField, settings);
        swapHandler.Setup(settings, gameField, matchFinder);
        collapseHandler.Setup(settings, gameField, levelGenerator);
        chipDestroyer.Setup(gameField, collapseHandler);
        gameProcessor.Setup(gameplayConductor);
        gameplayConductor.Setup(gameField, levelGenerator, matchFinder, swapHandler, collapseHandler, chipDestroyer);
    }

    // inits game settings
    void UseSettings()
    {
        settingsSubscribers = new SettingsSubscriber[]
        {
            collapseHandler,
            swapHandler
        };

        foreach (var subscriber in settingsSubscribers)
        {
            subscriber.UseSettings(subscriber);
        }
    }

    // launches local processes
    void Preload()
    {
        preloadables = new IInitializer[]
        {
            gameField,
            levelGenerator,
            collapseHandler,
            gameplayConductor,
            matchFinder
        };

        foreach (var obj in preloadables)
        {
            obj.Init();
        }
    }

}
