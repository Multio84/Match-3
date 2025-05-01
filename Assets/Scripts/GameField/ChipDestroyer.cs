using System;
using System.Collections.Generic;
using UnityEngine;


public class ChipDestroyer : MonoBehaviour
{
    GameField gameField;

    public List<Chip> chipsToDelete;
    int chipsToDeleteCount;  // number of chips to be deleted in current iteration

    public event Action<List<Chip>> OnMatchesCleared;


    public void Setup(GameField gf)
    {
        gameField = gf;
    }

    public void ClearMatches()
    {
        chipsToDeleteCount = chipsToDelete.Count;
        //Debug.Log("ChipDestroyer: Chips to be deleted now = " + chipsToDeleteCount);

        foreach (var chip in chipsToDelete)
        {
            chip.OnDeathCompleted += HandleChipDeath;
            chip.SetState(ChipState.Destroying);
            chip.Die();
        }

        //Debug.Log($"Chips sent to die: {chipsToDeleteCount}");
    }

    void HandleChipDeath(Chip chip)
    {
        if (!gameField.DeleteChip(chip.Cell))
        {
            Debug.Log("HandleChipDeath: chip wasn't deleted.");
            return;
        }

        if (chip is null || chip.gameObject is null)
        {
            Debug.LogWarning($"Trying to destroy non-existing chip.");
            return;
        }

        UnsubscribeFromChip(chip);

        Destroy(chip.gameObject);
        chip = null;
        chipsToDeleteCount--;
        //Debug.Log($"Chips left to die: {chipsToDeleteCount}");

        if (chipsToDeleteCount <= 0)
            HandleMatchesCleared();
    }

    void UnsubscribeFromChip(Chip chip)
    {
        if (chip is null)
        {
            Debug.Log("UnsubscribeFromChip: chip is null, won't be unsubscribed.");
            return;
        }
        chip.OnDeathCompleted -= HandleChipDeath;
    }

    void HandleMatchesCleared()
    {
        OnMatchesCleared?.Invoke(chipsToDelete);
    }
}
