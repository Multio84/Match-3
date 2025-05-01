using UnityEngine;


public class SwapOperation
{
    public Chip draggedChip;    // a chip, moved by player
    public Chip swappedChip;    // a chip, moving to draggedChip's place
    public Vector2Int direction;
    public bool isReverse;

    public SwapOperation(Chip draggedChip, Chip swappedChip, Vector2Int direction, bool isReverse)
    {
        draggedChip.SetState(ChipState.Swapping);
        swappedChip.SetState(ChipState.Swapping);

        this.draggedChip = draggedChip;
        this.swappedChip = swappedChip;

        this.isReverse = isReverse;
        if (isReverse)
            this.direction = direction *= -1;
        else
            this.direction = direction;
    }

    public void Stop()
    {
        draggedChip.SetIdle();
        swappedChip.SetIdle();
    }
}
