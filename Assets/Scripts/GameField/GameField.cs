using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// ƒанный класс отвечает за хранение и обработку массива фишек на игровом поле.
/// 
/// ќсновные термины и соглашени€ по именованию: 
/// <list type="bullet">
///     <item>
///         <description>
///         <c>chip</c> - фишка, объект геймпле€.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>board</c> - "доска" - все фишки на игровом поле, хран€щиес€ в одноимЄнном двумерном массиве.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>Sync...</c> - приставка в названии методов, выполн€ющих приведение в соответствие 
///         положени€ фишек на поле и их индекса в массиве board.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>cell</c> или <c>cellPos</c> - переменные, содержащие позицию клетки в игровом поле,
///         заданную в координатах сетки. »спользуемые типы: <see cref="Vector2Int"/> или <see cref="Vector3Int"/>.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>worldPos</c> - переменные, содержащие мировую (пространственную) позицию объекта.
///         »спользуемые типы: <see cref="Vector3"/>.
///         </description>
///     </item>
/// </list>
/// </summary>

public class GameField : MonoBehaviour, IInitializable
{
    [HideInInspector] public SwapHandler swapHandler;

    [Header("Field Settings")]
    Grid grid;
    [Range(5, 7)] public int width = 7;
    [Range(5, 14)] public int height = 14;
    Chip[,] board;
    public IEnumerable<Chip> BoardEnumerable => board.Cast<Chip>(); // property for iterating chips outside GameField
    public float cellSize;

    [Header("Chip Settings")]
    public float chipDragThreshold;   // dragged distance after which chip moves by itself
    public float chipDeathDuration = 2f;  // seconds of chip death animation
    

    public void Setup(SwapHandler sh)
    {
        swapHandler = sh;
    }

    public void Init()
    {
        grid = GetComponent<Grid>();
        cellSize = grid.cellSize.x;
        chipDragThreshold = cellSize / 5;
        board = new Chip[width, height];

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

    public bool IsBoardNullOrEmpty()
    {
        return board is null || board.Length == 0;
    }

    public Chip GetChip(Vector2Int cellPos)
    {
        return board[cellPos.x, cellPos.y];
    }

    public void SetChip(Vector2Int cellPos, Chip chip)
    {
        board[cellPos.x, cellPos.y] = chip;
    }

    public bool SyncChipWithBoardByItsPos(Chip chip)
    {
        Vector2Int cell = chip.CellPos;

        if (!IsCellInField(cell)) return false;
        SetChip(cell, chip);

        return true;
    }

    public void SyncChipWithBoardByNewPos(Chip chip, Vector2Int cellPos)
    {
        SetChip(cellPos, chip);
        chip.CellPos = cellPos;
    }

    // changes chip's position in array: moves it from start chip's place to the cellPos
    public void SyncFallingChipsWithBoard(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall)
        {
            Vector2Int chipCell = entry.Value.CellPos;
            Vector2Int targetCell = entry.Key;

            if (!IsCellInField(targetCell))
            {
                Debug.LogError("Sync field while collapsing: target cell for falling chip is outside the field.");
                break;
            }

            // chip created outside of the field is not in chips array yet,
            // so it doesn't have to be deleted from the array
            if (IsCellInField(chipCell))
                SetChip(chipCell, null);

            SyncChipWithBoardByNewPos(entry.Value, targetCell);
        }
    }

    public void UpdateSwappedChips(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.CellPos;
        Vector2Int cell2 = operation.swappedChip.CellPos;

        // set chips swapped celPoses
        SyncChipWithBoardByNewPos(operation.swappedChip, cell1);
        SyncChipWithBoardByNewPos(operation.draggedChip, cell2);
    }

    public bool IsChipCellPosActual(Chip chip)
    {
        return board[chip.CellPos.x, chip.CellPos.y] == chip;
    }

    public bool IsValidChip(int x, int y)
    {
        var cell = new Vector2Int(x, y);

        if (!IsCellInField(cell))
        {
            //Debug.Log($"Cell {cell} is not in field.");
            return false;
        }
        if (GetChip(cell) is null)
        {
            //Debug.Log($"Cell {cell} is null.");
            return false;
        }

        return true;
    }

    public bool IsCellInField(Vector2Int cellPos)
    {
        if (board == null) Debug.LogError("Chips array is empty!");

        if (cellPos.x < 0 || cellPos.x >= board.GetLength(0) ||
            cellPos.y < 0 || cellPos.y >= board.GetLength(1))
        {
            return false;
        }
        return true;
    }

    public Vector2Int GetCellFieldPos(Vector3 worldPos)
    {
        Vector3Int cellPos3 = grid.WorldToCell(worldPos);

        return new Vector2Int(cellPos3.x, cellPos3.y);
    }

    public Vector3 GetCellWorldPos(Vector2Int cellPos)
    {
        return grid.CellToWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
    }

}
