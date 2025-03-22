using UnityEngine;


public class SwapOperation
{
    public Chip draggedChip;
    public Chip swappedChip;
    public Vector2Int direction;
    public bool isReverse;

    public SwapOperation(Chip draggedChip, Chip swappedChip, Vector2Int direction, bool isReverse)
    {
        this.draggedChip = draggedChip;
        this.swappedChip = swappedChip;
        this.isReverse = isReverse;
        if (!isReverse) this.direction = direction;
        else this.direction = direction *= -1;

        this.draggedChip.IsSwapping = true;
        this.swappedChip.IsSwapping = true;
    }

    public void Stop()
    {
        draggedChip.IsSwapping = false;
        swappedChip.IsSwapping = false;
    }
}
