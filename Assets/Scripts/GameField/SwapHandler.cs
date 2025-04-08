using UnityEngine;
using System.Collections;
using System;


public class SwapHandler : SettingsSubscriber
{
    public override GameSettings Settings { get; set; }
    GameField gf;
    MatchFinder matchFinder;

    float chipSwapDuration;
    float reverseSwapDelay;

    public event Action OnSwapSuccessful;


    public void Setup(GameSettings settings, GameField gf, MatchFinder mf)
    {
        Settings = settings;
        this.gf = gf;
        matchFinder = mf;
    }

    public override void ApplyGameSettings()
    {
        chipSwapDuration = Settings.chipSwapDuration;
        reverseSwapDelay = Settings.reverseSwapDelay;
    }

    public void Swap(Chip chip, Vector2Int direction, bool isReverse)
    {
        SwapOperation operation = GetSwapOperation(chip, direction, isReverse);
        if (operation is null)
        {
            Debug.Log("SwapOperation is null: Swap declined.");
            return;
        }
        StartCoroutine(AnimateSwap(operation));
    }

    SwapOperation GetSwapOperation(Chip chip, Vector2Int direction, bool isReverse)
    {
        Vector2Int targetCell = chip.Cell + direction;   // find adjacent cell to swap with chip in it

        if (!gf.IsCellInField(targetCell)) return null;

        Chip swappedChip = gf.GetFieldChip(targetCell);
        if (swappedChip is null) return null;

        if ((swappedChip.HasState(ChipState.Idle) && !isReverse) ||
            (swappedChip.HasState(ChipState.Swapping) && isReverse))
        {
            return new SwapOperation(
                chip,
                gf.GetFieldChip(targetCell),
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

        // animate chips swap
        while (elapsedTime < chipSwapDuration)
        {
            operation.draggedChip.transform.position = Vector3.Lerp(chip1Pos, chip2Pos, elapsedTime / chipSwapDuration);
            operation.swappedChip.transform.position = Vector3.Lerp(chip2Pos, chip1Pos, elapsedTime / chipSwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        operation.draggedChip.transform.position = chip2Pos;
        operation.swappedChip.transform.position = chip1Pos;

        yield return new WaitForSeconds(reverseSwapDelay);

        HandleSwap(operation);
    }

    void HandleSwap(SwapOperation operation)
    {
        gf.UpdateSwappedChips(operation);

        if (operation.isReverse)
        {
            operation.Stop();
            return;
        }

        if (matchFinder.FindMatches(operation))
        {
            operation.Stop();
            OnSwapSuccessful?.Invoke();
        }
        else
        {
            // reverse swap: previously swapped chip becomes the "dragged" one
            Swap(operation.swappedChip, operation.direction, true);
        }
    }
}
