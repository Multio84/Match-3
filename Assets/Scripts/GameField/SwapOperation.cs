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

        Debug.Log($"SwapOperation: DraggedChip state = {draggedChip.GetState()}, " +
            $"SwappedShip state = {swappedChip.GetState()}");

        this.draggedChip = draggedChip;
        this.swappedChip = swappedChip;

        this.isReverse = isReverse;
        if (isReverse)
            this.direction = direction *= -1;
        else
            this.direction = direction;

        //this.draggedChip.IsSwapping = true;
        //this.swappedChip.IsSwapping = true;
    }

    public void Stop()
    {
        draggedChip.SetState(ChipState.Idle);
        swappedChip.SetState(ChipState.Idle);
        //draggedChip.IsSwapping = false;
        //swappedChip.IsSwapping = false;
    }
}
