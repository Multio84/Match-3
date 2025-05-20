using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Данный класс отвечает за хранение и обработку массива фишек на игровом поле.
/// 
/// Основные термины и соглашения по именованию: 
/// <list type="bullet">
///     <item>
///         <description>
///         <c>currentChip</c> - фишка, объект геймплея.
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

    float cellSize;
    int width;
    int height;
    public int boardHeight { get; private set; }
    Chip[,] board;
    int[] emptyCellsPerColumn;


    public void Setup(GameSettings gs)
    {
        settings = gs;
    }

    public void Init()
    {
        cellSize = settings.cellSize;
        width = settings.fieldWidth;
        height = settings.fieldHeight;
        boardHeight = height * 10;

        grid = GetComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 0);
        board = new Chip[width, boardHeight];    // board is 2 times higher than field to store new matchedChips for future collapsing

        SetGameFieldPos();
    }

    // game field pivot is in left bottom. This will position field in screen center, depending on the field size
    void SetGameFieldPos()
    {
        var startGameFieldPos = Vector3.zero;//transform.position;
        Vector3 newPos = new Vector3(
            startGameFieldPos.x - cellSize * width / 2,// + cellSize / 2,
            startGameFieldPos.y - cellSize * height / 2,// + cellSize / 2,
            0
        );

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
            if (chip is null)
            { 
                Debug.LogError("Attempt to set currentChip null: Failed");
                return;
            }
            
            chip.Cell = cell;
        }
        else
        {
            Debug.LogError($"SetChip By NewPos: Failed in cell {cell}");
        }
    }

    // changes currentChip's position in array: moves it from start currentChip's place to the cell
    public void DropChipsToEmptyCells(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall)
        {
            Chip chip = entry.Value;
            Vector2Int chipCell = entry.Value.Cell;
            Vector2Int targetCell = entry.Key;

            DeleteChip(chipCell);
            SetChipByNewPos(chip, targetCell);
        }
    }

    public void UpdateSwappedChips(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.Cell;
        Vector2Int cell2 = operation.swappedChip.Cell;

        // set matchedChips swapped cellPoses
        SetChipByNewPos(operation.swappedChip, cell1);
        SetChipByNewPos(operation.draggedChip, cell2);
    }

    // find the min Y on board, without chips to set a search limit of cascade cycle
    public int GetLowestEmptyRow()
    {
        for (int y = height; y < boardHeight; y++)
        {
            bool isRowEmpty = true;
            for (int x = 0; x < width; x++)
            {
                if (GetBoardChip(new Vector2Int(x, y)) is not null)
                {
                    isRowEmpty = false;
                    break;
                }
            }
            if (isRowEmpty)
                return y; 
        }
        return -1;
    }

    public List<Chip> CollectMatchedChips()
    {
        List<Chip> matchedChips = new List<Chip>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!IsEmptyFieldCell(cell))
                {
                    Chip chip = GetFieldChip(cell);
                    if (chip.IsMatched)
                    {
                        chip.IsMatched = false;
                        matchedChips.Add(chip);
                    }
                }
            }
        }

        return matchedChips;
    }


    public int[] GetEmptyCellsPerColumn(List<Vector2Int> clearedCells)
    {
        emptyCellsPerColumn = new int[width];
        foreach (Vector2Int cell in clearedCells)
        {
            if (!IsCellInBoard(cell))
            {
                Debug.LogWarning($"Cell {cell} among clearedCells is out of board!");
                continue;
            }

            emptyCellsPerColumn[cell.x]++;
        }

        return emptyCellsPerColumn;
    }

    // returns first empty cell's Y (in upward direction) in the X column
    public int GetMinYOverFieldWithoutChip(int x)
    {
        int y = height;
        do
        {
            if (GetBoardChip(new Vector2Int(x, y)) is null)
                break;
            y++;
        }
        while (y < boardHeight);

        return y;
    }

    public bool HasEmptyCells()
    {
        for (int y = 0; y < height; y++)
        { 
            for (int x = 0; x < width; x++)
            {
                if (IsEmptyFieldCell(new Vector2Int(x, y)))
                    return true;
            }
        }

        return false;
    }

    public bool IsEmptyFieldCell(Vector2Int cell)
    {
        if (GetFieldChip(cell) is null)
        {
            //Debug.LogWarning($"Cell {cell} is null.");
            return true;
        }

        return false;
    }

    public bool DeleteChip(Vector2Int cell)
    {
        if (SetChip(cell, null))
        {
            return true;
        }

        Debug.LogError($"Attempt to DeleteChip in {cell}: Failed.");
        return false;
    }

    public bool SetChip(Vector2Int cell, Chip chip)
    {
        if (!IsValidCell(cell))
        {
            Debug.LogError($"Attempt to SetChip to {cell}: Failed.");
            return false;
        }

        board[cell.x, cell.y] = chip;
        return true;
    }

    public Chip GetFieldChip(Vector2Int cell)
    {
        return GetChip(cell, IsCellInField, nameof(GetFieldChip));
    }

    public Chip GetBoardChip(Vector2Int cell)
    {
        return GetChip(cell, IsCellInBoard, nameof(GetBoardChip));
    }

    Chip GetChip(Vector2Int cell, Func<Vector2Int, bool> isValidCell, string methodName)
    {
        if (IsBoardNullOrEmpty() || !isValidCell(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell),
                $"Attempt to {methodName} in {cell}: Failed.");
        }

        return board[cell.x, cell.y];
    }

    public bool IsBoardNullOrEmpty()
    {
        return board is null || board.Length == 0;
    }

    bool IsValidCell(Vector2Int cell)
    {
        if (board is null || !IsCellInBoard(cell))
        {
            Debug.LogError($"Check cell {cell}: Invalid.");
            return false;
        }

        return true;
    }

    public bool IsCellInField(Vector2Int cell)
    {
        return IsCellWithinBounds(cell, width, height);
    }

    public bool IsCellInBoard(Vector2Int cell)
    {
        return IsCellWithinBounds(cell, width, boardHeight);
    }

    bool IsCellWithinBounds(Vector2Int cell, int width, int height)
    {
        if (cell.x >= 0 && cell.x < width &&
            cell.y >= 0 && cell.y < height)
        {
            return true;
        }

        return false;
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
