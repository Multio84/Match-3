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

    //bool cascadeIsProcessing = false;

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
        cascadeHandler.OnCascadeComplete += OnCascadeComplete;
        swapHandler.OnSwapComplete += OnSwapComplete;
    }

    void OnDisable()
    {
        levelGenerator.OnLevelGenerated -= OnLevelGenerated;
        chipDestroyer.OnMatchesCleared -= OnMatchesCleared;
        cascadeHandler.OnCascadeComplete -= OnCascadeComplete;
        swapHandler.OnSwapComplete -= OnSwapComplete;
    }

    public void StartGame()
    {
        Debug.Log("Conductor: GameStarted.");
        levelGenerator.GenerateLevel();
    }

    void OnLevelGenerated()
    {
        Debug.Log("Conductor: Level Generated.");
        if (matchFinder.FindMatches(null))
            ClearMatches();
    }

    void OnMatchesCleared(List<Vector2Int> clearedCells)
    {
        Debug.Log("Conductor: Matches Cleared.");
        levelGenerator.SpawnNewChips(clearedCells);
        Cascade();
    }

    void OnCascadeComplete()
    {
        Debug.Log("Conductor: Cascade Completed.");

        //cascadeIsProcessing = false;
        if (gameField.HasEmptyCells())
        {
            Cascade();
        }

        if (matchFinder.FindMatches(null))
            ClearMatches();
    }

    void OnSwapComplete(bool isSuccessful)
    {

        if (isSuccessful)
        {
            Debug.Log("Conductor: Swap successful.");
            ClearMatches();
        }
        else
        {
            if (gameField.HasEmptyCells())
            {
                Debug.Log("Conductor: Swap unsuccessful and caused additional Cascade.");
                Cascade();
                return;
            }
            Debug.Log("Conductor: Swap unsuccessful.");
        }
    }


    void ClearMatches()
    {
        chipDestroyer.DestroyChips(gameField.CollectMatchedChips());
    }

        //while (cascadeIsProcessing) { }
        //cascadeIsProcessing = true;
    void Cascade()
    {
        cascadeHandler.StartCascade();
    }
}
