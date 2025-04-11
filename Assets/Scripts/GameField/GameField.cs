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

    float cellSize;
    int width;
    int height;
    public int boardHeight { get; private set; }
    Chip[,] board;
    public IEnumerable<Chip> BoardEnumerable => board.Cast<Chip>(); // property for iterating chips outside GameField
    public int[] newChipsColumnsSizes;


    public void Setup(GameSettings gs)
    {
        settings = gs;
    }

    public void Init()
    {
        cellSize = settings.cellSize;
        width = settings.fieldWidth;
        height = settings.fieldHeight;
        boardHeight = height * 2;

        grid = GetComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 0);
        board = new Chip[width, boardHeight];    // board is 2 times higher than field to store new chips for future collapsing
        newChipsColumnsSizes = new int[width];

        SetGameFieldPos();
    }

    // game field pivot is in left bottom. This will position field in screen center, depending on the field size
    void SetGameFieldPos()
    {
        var startGameFieldPos = transform.position;
        Vector3 newPos = new Vector3(
            (startGameFieldPos.x - cellSize * width) / 2 + cellSize / 2,
            (startGameFieldPos.y - cellSize * height) / 2 + cellSize / 2,
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
                Debug.LogError("Attempt to set chip null: Failed");
                return;
            }
            
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

        // set chips swapped cellPoses
        SetChipByNewPos(operation.swappedChip, cell1);
        SetChipByNewPos(operation.draggedChip, cell2);
    }

    // find the max Y cellPos of all chips to set a search limit of collapse cycle
    public int GetHighestCellWithChip()
    {
        if (newChipsColumnsSizes is null || newChipsColumnsSizes.Length == 0)
        {
            Debug.LogError("Attempt to process NewChipsColumnsSizes: Invalid");
            return 0;
        }

        int highestCell = newChipsColumnsSizes[0];
        for (int i = 1; i < newChipsColumnsSizes.Length; i++)
        {
            if (newChipsColumnsSizes[i] > highestCell)
            {
                highestCell = newChipsColumnsSizes[i];
            }
        }

        return height + highestCell;
    }

    public void SetEmptyColumnsSizes()
    {
        newChipsColumnsSizes = new int[width];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!IsEmptyCell(new Vector2Int(x, y)))
                {
                    newChipsColumnsSizes[x]++;
                }
            }
        }
    }

    //public List<Chip> GetChipsAboveMatched()
    //{
    //    List<Chip> aboveChips = new List<Chip>();


    //    for (int y = chipY - 1; y >= 0; y--)
    //    {
    //        Chip currentChip = chipsGrid[chipX, y];
    //        if (currentChip != null)
    //        {
    //            aboveChips.Add(currentChip);
    //        }
    //    }
    //    return aboveChips;
    //}

    public List<Chip> CollectChipsToDelete()
    {
        List<Chip> chips = new List<Chip>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsEmptyCell(cell))
                {
                    Chip chip = GetFieldChip(cell);
                    if (chip.IsMatched)
                        chips.Add(chip);
                }
            }
        }

        return chips;
    }

    public bool IsEmptyCell(Vector2Int cell)
    {
        if (GetFieldChip(cell) is null)
        {
            //Debug.LogWarning($"Cell {cell} is null.");
            return false;
        }

        return true;
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
