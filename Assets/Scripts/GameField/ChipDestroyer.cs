using System;
using System.Collections.Generic;
using UnityEngine;


public class ChipDestroyer : MonoBehaviour
{
    GameField gameField;

    //public List<Chip> chipsToDelete;
    //List<Vector2Int> clearedCells;
    int chipsToDeleteCount;  // number of chips to be deleted in current iteration

    public event Action<List<Vector2Int>> OnMatchesCleared;


    public void Setup(GameField gf)
    {
        gameField = gf;
    }

    /// <summary>
    /// Информация об одном пакете, пришедшем в DestroyChips.
    /// </summary>
    private sealed class ChipsBatch
    {
        public int Remaining;           // сколько фишек ещё не умерло
        public List<Vector2Int> deadChipsCells;  // координаты уже умерших
    }

    // Для каждой фишки знаем, к какому пакету она относится
    readonly Dictionary<Chip, ChipsBatch> chipToBatch = new Dictionary<Chip, ChipsBatch>();

    /// <summary>
    /// Отправляет любую коллекцию фишек на уничтожение.
    /// Пакеты обрабатываются параллельно (новый обрабатывается без ожидания уничтожения всех фишек первого)
    /// </summary>
    public void DestroyChips(List<Chip> chips)
    {
        if (chips == null || chips.Count == 0) return;

        // Создаём пакет для этого вызова
        var batch = new ChipsBatch
        {
            Remaining = chips.Count,
            deadChipsCells = new List<Vector2Int>(chips.Count)
        };

        foreach (var chip in chips)
        {
            // Если по ошибке пришла та же фишка второй раз — пропустим
            if (chipToBatch.ContainsKey(chip)) continue;

            chipToBatch[chip] = batch;

            chip.OnDeathCompleted += HandleChipDeath;
            chip.SetState(ChipState.Destroying);
            chip.Die();
        }
    }

    private void HandleChipDeath(Chip chip)
    {
        chip.OnDeathCompleted -= HandleChipDeath;

        if (!chipToBatch.TryGetValue(chip, out var batch))
            return;

        if (!gameField.DeleteChip(chip.Cell))
        {
            Debug.Log("HandleChipDeath: chip wasn't deleted.");
            return;
        }

        batch.deadChipsCells.Add(chip.Cell);    // запомнили координаты
        chipToBatch.Remove(chip);

        Destroy(chip.gameObject);

        batch.Remaining--;
        if (batch.Remaining > 0) return;        // ещё есть живые

        // all chips of the batch are dead:
        OnMatchesCleared?.Invoke(batch.deadChipsCells);
    }








    //public void DestroyChips()
    //{
    //    clearedCells = new List<Vector2Int>();
    //    chipsToDeleteCount = chipsToDelete.Count;
    //    //Debug.Log("ChipDestroyer: Chips to be deleted now = " + chipsToDeleteCount);

    //    foreach (var chip in chipsToDelete)
    //    {
    //        chip.OnDeathCompleted += HandleChipDeath;
    //        chip.SetState(ChipState.Destroying);
    //        chip.Die();
    //    }

    //    //Debug.Log($"Chips sent to die: {chipsToDeleteCount}");
    //}

    //void HandleChipDeath(Chip chip)
    //{
    //    if (!gameField.DeleteChip(chip.Cell))
    //    {
    //        Debug.Log("HandleChipDeath: chip wasn't deleted.");
    //        return;
    //    }

    //    if (chip is null || chip.gameObject is null)
    //    {
    //        Debug.LogWarning($"Trying to destroy non-existing chip.");
    //        return;
    //    }

    //    clearedCells.Add(chip.Cell);

    //    UnsubscribeFromChip(chip);

    //    Destroy(chip.gameObject);
    //    chipsToDeleteCount--;
    //    //Debug.Log($"Chips left to die: {chipsToDeleteCount}");

    //    if (chipsToDeleteCount <= 0)
    //        HandleMatchesCleared();
    //}

    //void UnsubscribeFromChip(Chip chip)
    //{
    //    if (chip is null)
    //    {
    //        Debug.Log("UnsubscribeFromChip: chip is null, won't be unsubscribed.");
    //        return;
    //    }
    //    chip.OnDeathCompleted -= HandleChipDeath;
    //}

    //void HandleMatchesCleared()
    //{
    //    OnMatchesCleared?.Invoke(clearedCells);
    //}
}
