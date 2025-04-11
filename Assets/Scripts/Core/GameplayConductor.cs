using System.Collections.Generic;
using UnityEngine;


public class GameplayConductor : MonoBehaviour, IInitializer
{
    GameField gameField;
    LevelGenerator levelGenerator;
    MatchFinder matchFinder;
    SwapHandler swapHandler;
    CascadeHandler cascadeHandler;
    ChipDestroyer chipDestroyer;


    public void Setup(
        GameField gf,
        LevelGenerator lg,
        MatchFinder mf,
        SwapHandler sh,
        CascadeHandler ch,
        ChipDestroyer cd
        )
    {
        gameField = gf;
        levelGenerator = lg;
        matchFinder = mf;
        swapHandler = sh;
        cascadeHandler = ch;
        chipDestroyer = cd;
    }

    public void Init()
    {
        levelGenerator.OnLevelGenerated += OnLevelGenerated;
        chipDestroyer.OnMatchesCleared += OnMatchesCleared;
        cascadeHandler.OnCascadeCompleted += OnCascadeCompleted;
        swapHandler.OnSwapSuccessful += OnSwapSuccessful;
    }

    void OnDisable()
    {
        levelGenerator.OnLevelGenerated -= OnLevelGenerated;
        chipDestroyer.OnMatchesCleared -= OnMatchesCleared;
        cascadeHandler.OnCascadeCompleted -= OnCascadeCompleted;
        swapHandler.OnSwapSuccessful -= OnSwapSuccessful;
    }

    public void StartGame()
    {
        //Debug.Log("Conductor: GameStarted.");
        levelGenerator.GenerateLevel();
    }

    void OnLevelGenerated()
    {
        //Debug.Log("Conductor: Level Generated.");
        if (matchFinder.FindMatches(null))
        {
            chipDestroyer.chipsToDelete = gameField.CollectChipsToDelete();
            chipDestroyer.ClearMatches();
        }
        levelGenerator.SpawnNewChips();
        cascadeHandler.CascadeChips();
    }

    void OnMatchesCleared()
    {
        //Debug.Log("Conductor: Matches Cleared.");
        levelGenerator.SpawnNewChips();
        cascadeHandler.CascadeChips();
    }

    void OnCascadeCompleted()
    {
        //Debug.Log("Conductor: Collapse Completed.");
        if (matchFinder.FindMatches(null))
        {
            chipDestroyer.chipsToDelete = gameField.CollectChipsToDelete();
            chipDestroyer.ClearMatches();
        }
    }

    void OnSwapSuccessful()
    {
        //Debug.Log("Conductor: Swap successful.");

        chipDestroyer.chipsToDelete = gameField.CollectChipsToDelete();
        chipDestroyer.ClearMatches();
    }

}
