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
    [SerializeField] CascadeHandler cascadeHandler;
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
            cascadeHandler == null ||
            chipDestroyer == null)
        {
            Debug.LogError("GameBootstrapper: Some links are not set in the inspector!");
            return;
        }

        gameField.Setup(settings);
        levelGenerator.Setup(settings, gameField, swapHandler);
        matchFinder.Setup(settings, gameField);
        swapHandler.Setup(settings, gameField, matchFinder);
        cascadeHandler.Setup(settings, gameField);
        chipDestroyer.Setup(gameField);
        gameProcessor.Setup(gameplayConductor);
        gameplayConductor.Setup(gameField, levelGenerator, matchFinder, swapHandler, cascadeHandler, chipDestroyer);
    }

    // inits game settings
    void UseSettings()
    {
        settingsSubscribers = new SettingsSubscriber[]
        {
            cascadeHandler,
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
            cascadeHandler,
            gameplayConductor,
            matchFinder
        };

        foreach (var obj in preloadables)
        {
            obj.Init();
        }
    }

}
