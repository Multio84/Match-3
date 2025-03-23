using UnityEngine;


public class GameplayConductor : MonoBehaviour, IInitializable
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

    public void Init()
    {
        levelGenerator.OnLevelGenerated += OnLevelGenerated;
        chipDestroyer.OnMatchesCleared += OnMatchesCleared;
        collapseHandler.OnCollapseCompleted += OnCollapseCompleted;
        swapHandler.OnSwapCompleted += OnSwapCompleted;
    }

    void OnDisable()
    {
        
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
        //Debug.Log("Matches cleared. Starting Collapse.");
        collapseHandler.CollapseChips();
    }

    void OnCollapseCompleted()
    {
        if (matchFinder.FindMatches(null))
            chipDestroyer.ClearMatches();
    }

    void OnSwapCompleted(SwapOperation operation)
    {
        gameField.UpdateSwappedChips(operation);
    }

}
