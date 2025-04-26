using UnityEngine;


// Dependency Injection class: exports depencencies to classes. Starts first.
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
    [SerializeField] MainMenuAnimator mainMenuAnimator;
    IInitializer[] initializables;
    SettingsSubscriber[] settingsSubscribers;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Setup();
        UseSettings();
        Init();
    }

    // sets dependencies
    void Setup()
    {
        if (settings is null ||
            mainMenuAnimator is null ||
            gameProcessor is null ||
            gameplayConductor is null ||
            gameField is null ||
            levelGenerator is null ||
            matchFinder is null ||
            swapHandler is null ||
            cascadeHandler is null ||
            chipDestroyer is null)
        {
            Debug.LogError("GameBootstrapper: Some links are not set in the inspector!");
            return;
        }

        mainMenuAnimator.Setup(settings);
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
            mainMenuAnimator,
            cascadeHandler,
            swapHandler
        };

        foreach (var subscriber in settingsSubscribers)
        {
            subscriber.UseSettings(subscriber);
        }
    }

    // launches local processes
    void Init()
    {
        initializables = new IInitializer[]
        {
            gameField,
            levelGenerator,
            cascadeHandler,
            gameplayConductor,
            matchFinder
        };

        foreach (var obj in initializables)
        {
            obj.Init();
        }
    }

}
