using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;


/// <summary>
/// Данный класс отвечает за хранение и обработку массива фишек на игровом поле.
/// 
/// Основные термины и соглашения по именованию: 
/// <list type="bullet">
///     <item>
///         <description>
///         <c>chip</c> - фишка, объект геймплея.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>board</c> - "доска" - все фишки на игровом поле, хранящиеся в одноимённом двумерном массиве.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>field</c> - "поле" - игровое поле, составленное из данных уровня и содержащее board.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>Sync...</c> - приставка в названии методов, выполняющих приведение в соответствие 
///         положения фишек на поле и их индекса в массиве board.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>cell</c> или <c>cell</c> - переменные, содержащие позицию клетки в игровом поле,
///         заданную в координатах сетки. Используемые типы: <see cref="Vector2Int"/> или <see cref="Vector3Int"/>.
///         </description>
///     </item>
///     <item>
///         <description>
///         <c>worldPos</c> - переменные, содержащие мировую (пространственную) позицию объекта.
///         Используемые типы: <see cref="Vector3"/>.
///         </description>
///     </item>
/// </list>
/// </summary>

public class GameField : MonoBehaviour, IInitializer
{
    GameSettings settings;
    Grid grid;

    public float cellSize;
    public int width;
    public int height;
    Chip[,] board;
    public IEnumerable<Chip> BoardEnumerable => board.Cast<Chip>(); // property for iterating chips outside GameField


    public void Setup(GameSettings gs)
    {
        settings = gs;
    }

    public void Init()
    {
        cellSize = settings.cellSize;
        width = settings.width;
        height = settings.height;

        grid = GetComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 0);
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

    public bool SetChipByItsPos(Chip chip)
    {
        if (SetChip(chip.Cell, chip))
        {
            return true;
        }

        Debug.LogError($"SetChip By ItsPos: Failed in cell {chip.Cell}");
        return false;
    }

    public void SetChipByNewPos(Chip chip, Vector2Int cell)
    {
        if (SetChip(cell, chip))
        {
            if (chip != null)
                chip.Cell = cell;
        }
        else
        {
            Debug.LogError($"SetChip By NewPos: Failed in cell {cell}");
        }
    }

    // changes chip's position in array: moves it from start chip's place to the cell
    public void SyncFallingChipsWithBoard(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall)
        {
            Vector2Int chipCell = entry.Value.Cell;
            Vector2Int targetCell = entry.Key;

            // new chip is created outside of the field and is not on board yet,
            // so it doesn't have to be deleted from board
            if (IsCellInField(chipCell))
            {
                DeleteChip(chipCell);
            }

            SetChipByNewPos(entry.Value, targetCell);
        }
    }

    public void UpdateSwappedChips(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.Cell;
        Vector2Int cell2 = operation.swappedChip.Cell;

        // set chips swapped celPoses
        SetChipByNewPos(operation.swappedChip, cell1);
        SetChipByNewPos(operation.draggedChip, cell2);
    }

    public bool IsChipCellActual(Chip chip)
    {
        if (GetChip(chip.Cell) == chip)
            return true;

        return false;
    }

    public bool IsValidChip(Vector2Int cell)
    {
        if (GetChip(cell) is null)
        {
            Debug.Log($"Cell {cell} is null.");
            return false;
        }

        return true;
    }

    public Chip GetChip(Vector2Int cell)
    {
        if (IsBoardNullOrEmpty() || !IsCellInField(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell), $"Attempt to GetChip from {cell}: Cell is invalid.");
        }

        return board[cell.x, cell.y];
    }

    public bool SetChip(Vector2Int cell, Chip chip)
    {
        if (!IsValidCell(cell))
        {
            Debug.LogError($"Attempt to SetChip to {cell}: Cell is invalid.");
            return false;
        }

        board[cell.x, cell.y] = chip;
        return true;
    }

    public bool DeleteChip(Vector2Int cell)
    {
        if (!IsValidCell(cell))
        {
            Debug.LogError($"Attempt to DeleteChip in {cell}: Cell is invalid.");
            return false;
        }

        board[cell.x, cell.y] = null;
        return true;
    }

    public bool IsCellInField(Vector2Int cell)
    {
        if (board == null)
        {
            Debug.LogError("Board is null.");
            return false;
        }

        if (cell.x >= 0 && cell.x < board.GetLength(0) &&
            cell.y >= 0 && cell.y < board.GetLength(1))
        {
            return true;
        }

        return false;
    }

    bool IsValidCell(Vector2Int cell)
    {
        if (board is null || !IsCellInField(cell))
        {
            Debug.LogError($"Chek cell {cell}: Invalid.");
            return false;
        }

        return true;
    }

    public bool IsBoardNullOrEmpty()
    {
        return board is null || board.Length == 0;
    }

    public Vector2Int GetCellFieldPos(Vector3 worldPos)
    {
        Vector3Int cell3 = grid.WorldToCell(worldPos);

        return new Vector2Int(cell3.x, cell3.y);
    }

    public Vector3 GetCellWorldPos(Vector2Int cell)
    {
        return grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
    }



}
