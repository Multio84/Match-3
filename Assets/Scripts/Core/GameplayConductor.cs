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
        swapHandler.OnSwapComplete += OnSwapComplete;
    }

    void OnDisable()
    {
        levelGenerator.OnLevelGenerated -= OnLevelGenerated;
        chipDestroyer.OnMatchesCleared -= OnMatchesCleared;
        cascadeHandler.OnCascadeCompleted -= OnCascadeCompleted;
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

    void OnMatchesCleared(List<Chip> deletedChips)
    {
        Debug.Log("Conductor: Matches Cleared.");
        levelGenerator.SpawnNewChips(deletedChips);
        cascadeHandler.StartCascade();//CascadeChips();
    }

    void OnCascadeCompleted()
    {
        if (gameField.HasEmptyCells())
        {
            cascadeHandler.StartCascade();
        }

            Debug.Log("Conductor: Collapse Completed.");
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
                cascadeHandler.StartCascade();//CascadeChips();
                return;
            }
            Debug.Log("Conductor: Swap unsuccessful.");
        }
    }


    void ClearMatches()
    {
        chipDestroyer.chipsToDelete = gameField.CollectChipsToDelete();
        chipDestroyer.ClearMatches();
    }
}
