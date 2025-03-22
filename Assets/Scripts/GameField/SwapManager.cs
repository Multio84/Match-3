using UnityEngine;
using System.Collections;


public class SwapManager : MonoBehaviour
{
    GameField gameField;

    const float ChipSwapDuration = 0.2f;    // chips swap animation time duration in seconds
    const float ReverseSwapDelay = 0.15f;   // seconds before automatic reverse swap, when manual swap didn't lead to match


    public void Setup(GameField gf)
    {
        gameField = gf;
    }

    public void Swap(Chip chip, Vector2Int direction, bool isReverse)
    {
        SwapOperation operation = GetSwapOperation(chip, direction, isReverse);
        if (operation is null)
        {
            Debug.Log("Swap operation is null.");
            return;
        }
        StartCoroutine(AnimateSwap(operation));
    }

    SwapOperation GetSwapOperation(Chip chip, Vector2Int direction, bool isReverse)
    {
        Vector2Int targetCell = chip.CellPos + direction;   // find adjacent chip to swap with this
        if (!gameField.IsValidChip(targetCell.x, targetCell.y))
        {
            Debug.Log("Attempt to make swap with invalid chip.");
            return null;
        }

        return new SwapOperation(
            chip,
            gameField.GetChip(targetCell),
            direction,
            isReverse
        );
    }

    // animates 2 chips swap
    IEnumerator AnimateSwap(SwapOperation operation)
    {
        // set positions
        Vector3 chip1Pos = operation.draggedChip.transform.position;
        Vector3 chip2Pos = operation.swappedChip.transform.position;

        float elapsedTime = 0;

        // animate chips swap
        while (elapsedTime < ChipSwapDuration)
        {
            operation.draggedChip.transform.position = Vector3.Lerp(chip1Pos, chip2Pos, elapsedTime / ChipSwapDuration);
            operation.swappedChip.transform.position = Vector3.Lerp(chip2Pos, chip1Pos, elapsedTime / ChipSwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        operation.draggedChip.transform.position = chip2Pos;
        operation.swappedChip.transform.position = chip1Pos;

        yield return new WaitForSeconds(ReverseSwapDelay);

        gameField.UpdateSwappedChips(operation);
    }
}
