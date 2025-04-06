using System;
using System.Collections.Generic;
using UnityEngine;


public class ChipDestroyer : MonoBehaviour
{
    GameField gf;

    int chipsToDelete = 0;  // number of chips to be deleted in current iteration
    public event Action OnMatchesCleared;


    public void Setup(GameField gf)
    {
        this.gf = gf;
    }

    public void ClearMatches()
    {
        List<Chip> chips = gf.CollectChipsToDelete();
        chipsToDelete = chips.Count;
        //Debug.Log("ChipDestroyer: Chips to be deleted now = " + chipsToDelete);

        foreach (var chip in chips)
        {
            chip.OnDeathCompleted += HandleChipDeath;
            chip.Die();
        }

        //Debug.Log($"Chips sent to die: {chipsToDelete}");
    }

    void HandleChipDeath(Chip chip)
    {
        if (!gf.DeleteChip(chip.Cell))
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
        chipsToDelete--;
        //Debug.Log($"Chips left to die: {chipsToDelete}");

        if (chipsToDelete <= 0)
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
        OnMatchesCleared?.Invoke();
    }
}
