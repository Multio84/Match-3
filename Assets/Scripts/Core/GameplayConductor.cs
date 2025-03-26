using UnityEngine;


public class GameplayConductor : MonoBehaviour, IPreloader
{
    GameField gameField;
    LevelGenerator levelGenerator;
    MatchFinder matchFinder;
    SwapHandler swapHandler;
    CollapseHandler collapseHandler;
    ChipDestroyer chipDestroyer;


    public void Setup(
        GameField gf,
        LevelGenerator lg,
        MatchFinder mf,
        SwapHandler sh,
        CollapseHandler ch,
        ChipDestroyer cd
        )
    {
        gameField = gf;
        levelGenerator = lg;
        matchFinder = mf;
        swapHandler = sh;
        collapseHandler = ch;
        chipDestroyer = cd;
    }

    public void Preload()
    {
        levelGenerator.OnLevelGenerated += OnLevelGenerated;
        chipDestroyer.OnMatchesCleared += OnMatchesCleared;
        collapseHandler.OnCollapseCompleted += OnCollapseCompleted;
        swapHandler.OnSwapSuccessful += OnSwapSuccessful;
    }

    void OnDisable()
    {
        levelGenerator.OnLevelGenerated -= OnLevelGenerated;
        chipDestroyer.OnMatchesCleared -= OnMatchesCleared;
        collapseHandler.OnCollapseCompleted -= OnCollapseCompleted;
        swapHandler.OnSwapSuccessful -= OnSwapSuccessful;
    }

    public void StartGame()
    {
        levelGenerator.GenerateLevel();
    }

    void OnLevelGenerated()
    { 
        if (matchFinder.FindMatches(null)) chipDestroyer.ClearMatches();
    }

    void OnMatchesCleared()
    {
        collapseHandler.CollapseChips();
    }

    void OnCollapseCompleted()
    {
        if (matchFinder.FindMatches(null))
            chipDestroyer.ClearMatches();
    }

    void OnSwapSuccessful()
    {
        chipDestroyer.ClearMatches();
    }

}
