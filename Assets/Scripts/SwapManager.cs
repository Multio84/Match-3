using UnityEngine;
using System.Collections;


public class SwapManager : MonoBehaviour
{
    public static SwapManager Instance { get; private set; }
    GameField gameField;

    const float ChipSwapDuration = 0.2f;    // chips swap animation time duration in seconds
    const float ReverseSwapDelay = 0.15f;   // seconds before automatic reverse swap, when manual swap didn't lead to match


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        Initialize();
    }

    private void Initialize()
    {
        gameField = GameMode.Instance.gameField;
    }

    public void Swap(Chip chip, Vector2Int direction, bool isReverse)
    {
        SwapOperation swapOperation = GetSwapOperation(chip, direction, isReverse);
        StartCoroutine(AnimateSwap(swapOperation));
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
    IEnumerator AnimateSwap(SwapOperation swapOperation)
    {
        // set positions
        Vector3 chip1Pos = swapOperation.draggedChip.transform.position;
        Vector3 chip2Pos = swapOperation.swappedChip.transform.position;

        float elapsedTime = 0;

        // animate chips swap
        while (elapsedTime < ChipSwapDuration)
        {
            swapOperation.draggedChip.transform.position = Vector3.Lerp(chip1Pos, chip2Pos, elapsedTime / ChipSwapDuration);
            swapOperation.swappedChip.transform.position = Vector3.Lerp(chip2Pos, chip1Pos, elapsedTime / ChipSwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        swapOperation.draggedChip.transform.position = chip2Pos;
        swapOperation.swappedChip.transform.position = chip1Pos;

        yield return new WaitForSeconds(ReverseSwapDelay);

        gameField.UpdateSwapInGrid(swapOperation);
    }
}
