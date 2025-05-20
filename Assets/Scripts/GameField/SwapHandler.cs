using UnityEngine;
using System.Collections;
using System;


public class SwapHandler : SettingsSubscriber
{
    public override GameSettings Settings { get; set; }
    GameField gameField;
    MatchFinder matchFinder;

    float chipSwapDuration;
    float reverseSwapDelay;

    public event Action<bool> OnSwapComplete;


    public void Setup(GameSettings settings, GameField gf, MatchFinder mf)
    {
        Settings = settings;
        gameField = gf;
        matchFinder = mf;
    }

    public override void ApplyGameSettings()
    {
        chipSwapDuration = Settings.chipSwapDuration;
        reverseSwapDelay = Settings.reverseSwapDelay;
    }

    public bool Swap(Chip chip, Vector2Int direction, bool isReverse)
    {
        SwapOperation operation = GetSwapOperation(chip, direction, isReverse);
        if (operation is null)
        {
            Debug.Log("SwapOperation is null: Swap declined.");
            return false;
        }
        StartCoroutine(AnimateSwap(operation));
        return true;
    }

    SwapOperation GetSwapOperation(Chip chip, Vector2Int direction, bool isReverse)
    {
        Vector2Int targetCell = chip.Cell + direction;  // adjacent cell to swap with chip in it

        if (!gameField.IsCellInField(targetCell)) return null;

        Chip swappedChip = gameField.GetFieldChip(targetCell); // a chip to be swapped
        if (swappedChip is null) return null;

        if ((swappedChip.IsIdle() && !isReverse) ||
            (swappedChip.HasState(ChipState.Swapping) && isReverse) ||
            (swappedChip.HasState(ChipState.Swapped) && isReverse))
        {
            return new SwapOperation(
                chip,
                swappedChip,
                direction,
                isReverse
            );
        }
        else
        {
            Debug.Log("Attempt to swap with inappropriate chip. SwappedChip state is " + swappedChip.GetState());
            return null;
        }
    }

    // animates 2 chips swap
    IEnumerator AnimateSwap(SwapOperation operation)
    {
        // set positions
        Vector3 chip1Pos = operation.draggedChip.transform.position;
        Vector3 chip2Pos = operation.swappedChip.transform.position;

        float elapsedTime = 0;

        // animate swap
        while (elapsedTime < chipSwapDuration)
        {
            operation.draggedChip.transform.position = Vector3.Lerp(chip1Pos, chip2Pos, elapsedTime / chipSwapDuration);
            operation.swappedChip.transform.position = Vector3.Lerp(chip2Pos, chip1Pos, elapsedTime / chipSwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        operation.draggedChip.transform.position = chip2Pos;
        operation.swappedChip.transform.position = chip1Pos;

        if (!operation.isReverse)
        {
            yield return new WaitForSeconds(reverseSwapDelay);
        }

        HandleSwap(operation);
    }

    void HandleSwap(SwapOperation operation)
    {
        gameField.UpdateSwappedChips(operation);

        if (operation.isReverse)
        {
            operation.Stop();
            OnSwapComplete?.Invoke(false);
            return;
        }

        operation.draggedChip.SetState(ChipState.Swapped);
        operation.swappedChip.SetState(ChipState.Swapped);

        if (matchFinder.FindMatches(operation))
        {
            operation.Stop();
            OnSwapComplete?.Invoke(true);
        }
        else
        {
            // reverse swap: previously swapped chip becomes the "dragged" one
            if(!Swap(operation.swappedChip, operation.direction, true))
            {
                Debug.LogWarning("Reverse swap is impossible: " +
                    "some of the swapping chips are not in swappable state!");
            }
        }
    }
}
