using System;
using System.Collections.Generic;
using UnityEngine;


public class LevelGenerator : MonoBehaviour, IInitializer
{
    GameSettings settings;
    GameField gameField;
    SwapHandler swapHandler;

    [SerializeField] GameObject cellPrefab;
    [SerializeField] GameObject[] chipsPrefabs;
    [SerializeField] GameObject cellsRoot;

    int fieldWidth;
    int fieldHeight;
    int boardHeight;

    public event Action OnLevelGenerated;


    public void Setup(GameSettings gs, GameField gf, SwapHandler sh)
    {
        settings = gs;
        gameField = gf;
        swapHandler = sh;
    }

    public void Init()
    {
        fieldWidth = settings.fieldWidth;
        fieldHeight = settings.fieldHeight;
        boardHeight = gameField.boardHeight;
    }

    public void GenerateLevel()
    {
        GenerateFieldBack();
        GenerateBoard();
    }

    void GenerateFieldBack()
    {
        for (int y = 0; y < fieldHeight; y++)
        {
            for (int x = 0; x < fieldWidth; x++)
            {
                SpawnFieldCell(new Vector2Int(x, y));
            }
        }
    }

    void SpawnFieldCell(Vector2Int cell)
    {
        GameObject cellObj = Instantiate(cellPrefab, gameField.GetCellWorldPos(cell), Quaternion.identity);
        cellObj.transform.SetParent(cellsRoot.transform);
        cellObj.name = "Cell_" + cell.x.ToString() + "_" + cell.y.ToString();
    }

    void GenerateBoard()
    {
        //for (int y = fieldHeight; y < boardHeight; y++)
        for (int y = 0; y < fieldHeight; y++)
        {
            for (int x = 0; x < fieldWidth; x++)
            {
                Chip chip = SpawnChip(new Vector2Int(x, y));
                chip.IsVisible = true;
                gameField.SetChipByItsPos(chip);
            }
        }

        OnLevelGenerated?.Invoke();
    }

    // new chips are spawned over gamefield, on the top half of the board
    public void SpawnNewChips(List<Vector2Int> clearedCells)
    {
        int[] emptyCellsPerColumn = gameField.GetEmptyCellsPerColumn(clearedCells);
        for (int x = 0; x < fieldWidth; x++)
        {
            if (emptyCellsPerColumn[x] <= 0) continue;

            int minSpawnY = gameField.GetMinYOverFieldWithoutChip(x);

            for (int y = minSpawnY; y < minSpawnY + emptyCellsPerColumn[x]; y++)
            {
                if (y >= boardHeight)
                {
                    Debug.LogError("WRONG CELL!");
                }

                Chip chip = SpawnChip(new Vector2Int(x, y));
                chip.SetState(ChipState.Blocked);
                chip.IsVisible = true;
                gameField.SetChipByItsPos(chip);
            }
        }
    }

    public Chip SpawnChip(Vector2Int cellPos)
    {
        if (gameField.GetBoardChip(cellPos) is not null)
        {
            Debug.LogError("Attempt to spawn chip in the cell with another chip!");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, chipsPrefabs.Length);
        GameObject chipGO = Instantiate(chipsPrefabs[randomIndex], gameField.GetCellWorldPos(cellPos), Quaternion.identity);
        chipGO.transform.SetParent(transform);

        Chip chip = chipGO.GetComponent<Chip>();
        //chip.name = "Chip_" + cellPos;
        chip.Init(settings, gameField, swapHandler, cellPos);

        return chip;
    }
}
