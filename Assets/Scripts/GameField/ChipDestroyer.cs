using System;
using UnityEngine;


public class ChipDestroyer : MonoBehaviour
{
    GameField gf;
    CollapseHandler collapseHandler;

    int chipsToDelete = 0;  // number of chips to be deleted in current iteration
    public event Action OnMatchesCleared;


    public void Setup(GameField gf, CollapseHandler ch)
    {
        this.gf = gf;
        collapseHandler = ch;
    }

    // destroy the matched chips
    public void ClearMatches()
    {
        chipsToDelete = 0;
        if (gf.IsBoardNullOrEmpty())
        {
            Debug.Log("ChipDestroyer: No chips to delete.");
            return;
        }
        foreach (var chip in gf.BoardEnumerable)
        {
            if (chip is not null && chip.IsMatched)
            {
                chip.OnDeathCompleted -= HandleChipDeath;   // to exclude double subscription
                chip.OnDeathCompleted += HandleChipDeath;

                chipsToDelete++;
                chip.Die();
            }
        }
        //Debug.Log($"Chips sent to die: {chipsToDelete}");
        collapseHandler.totalChipsToFallCount = chipsToDelete;
    }

    void HandleChipDeath(Chip chip)
    {
        if (gf.IsChipCellPosActual(chip))
        {
            gf.SetChip(chip.CellPos, null);
            //Debug.Log($"Chip_{chip} removed successfully.");
        }

        UnsubscribeFromChip(chip);

        if (chip is null || chip.gameObject is null)
        {
            Debug.LogWarning($"Trying to destroy non-existing chip.");
            return;
        }

        Destroy(chip.gameObject);
        chip = null;
        chipsToDelete--;
        //Debug.Log($"Chips left to die: {chipsToDelete}");

        if (chipsToDelete <= 0)
            HandleMatchesCleared();
    }

    void UnsubscribeFromChip(Chip chip)
    {
        if (chip is null) return;
        chip.OnDeathCompleted -= HandleChipDeath;
    }

    void HandleMatchesCleared()
    {
        OnMatchesCleared?.Invoke();
    }
}
