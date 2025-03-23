using System;
using UnityEngine;


public class GameField : MonoBehaviour, IInitializable
{
    MatchFinder matchFinder;
    [HideInInspector] public SwapHandler swapHandler;
    CollapseHandler collapseHandler;
    ChipDestroyer chipDestroyer;

    [Header("Grid Properties")]
    public Grid grid;
    [Range(5, 7)] public int width = 7;
    [Range(5, 14)] public int height = 14;

    public Chip[,] chips;
    public float cellSize;

    [Header("Chip Properties")]
    public float chipDragThreshold;   // dragged distance after which chip moves by itself

    public float chipDeathDuration = 2f;  // seconds of chip death animation du
    



    public void Setup(MatchFinder mf, SwapHandler sh, CollapseHandler ch, ChipDestroyer cd)
    {
        matchFinder = mf;
        swapHandler = sh;
        collapseHandler = ch;
        chipDestroyer = cd;
    }

    public void Init()
    {
        cellSize = grid.cellSize.x;
        chipDragThreshold = cellSize / 5;
        chips = new Chip[width, height];

        SetGameFieldPos();
    }

    // game field pivot is in left bottom. This will position field in screen center, depending on the field size
    void SetGameFieldPos()
    {
        var startGameFieldPos = transform.position;
        Vector3 newPos = new Vector3();
        newPos.x = (startGameFieldPos.x - cellSize * width) / 2 + cellSize / 2;
        newPos.y = (startGameFieldPos.y - cellSize * height) / 2 + cellSize / 2;
        transform.position = newPos;
    }

    public bool SyncChipWithBoard(Chip chip)
    {
        Vector2Int cell = chip.CellPos;

        if (!IsCellInField(cell)) return false;
        chips[cell.x, cell.y] = chip;

        return true;
    }

    public bool IsValidChip(int x, int y)
    {
        if (!IsCellInField(new Vector2Int (x, y))) {
            //Debug.Log($"Cell ({x}, {y}) is not in field.");
            return false;
        }
        if (chips[x, y] is null) {
            //Debug.Log($"Cell ({x}, {y}) is null.");
            return false;
        }

        return true;
    }

    public void UpdateSwappedChips(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.CellPos;
        Vector2Int cell2 = operation.swappedChip.CellPos;

        // change places in array
        chips[cell1.x, cell1.y] = operation.swappedChip;
        chips[cell2.x, cell2.y] = operation.draggedChip;

        // change cellposes in chip's properties
        operation.draggedChip.CellPos = cell2;
        operation.swappedChip.CellPos = cell1;

        HandleSwap(operation);
    }

    void HandleSwap(SwapOperation operation)
    {
        if (operation.isReverse)
        {
            operation.Stop();
            return;
        }

        if (matchFinder.FindMatches(operation))
        {
            operation.Stop();
            chipDestroyer.ClearMatches();
        }
        else
        {
            // reverse swap: previously swapped chip becomes the "dragged" one
            swapHandler.Swap(operation.swappedChip, operation.direction, true);
        }
    }

   



    public Vector2Int GetCellGridPos(Vector3 worldPos)
    {
        Vector3Int cellPos3 = grid.WorldToCell(worldPos);

        return new Vector2Int(cellPos3.x, cellPos3.y);
    }

    public Vector3 GetCellWorldPos(Vector2Int cellPos)
    {
        return grid.CellToWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
    }

    public bool IsCellInField(Vector2Int cellPos)
    {
        if (chips == null) Debug.LogError("Chips array is empty!");

        if (cellPos.x < 0 || cellPos.x >= chips.GetLength(0) ||
            cellPos.y < 0 || cellPos.y >= chips.GetLength(1)) {
            return false;
        }
        return true;
    }

    public Chip GetChip(Vector2Int cellPos)
    {
        return chips[cellPos.x, cellPos.y];
    }
}
