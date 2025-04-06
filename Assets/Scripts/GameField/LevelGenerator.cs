using System;
using UnityEngine;


public class LevelGenerator : MonoBehaviour, IInitializer
{
    GameSettings settings;
    GameField gf;
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
        this.gf = gf;
        swapHandler = sh;
    }

    public void Init()
    {
        fieldWidth = settings.fieldWidth;
        fieldHeight = settings.fieldHeight;
        boardHeight = gf.boardHeight;
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
        GameObject cellObj = Instantiate(cellPrefab, gf.GetCellWorldPos(cell), Quaternion.identity);
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
                gf.SetChipByItsPos(chip);
            }
        }

        OnLevelGenerated?.Invoke();
    }

    // new chips are spawned over gamefield, on the top half of the board
    public void SpawnNewChips()
    {
        gf.SetEmptyColumnsSizes();
        int[] emptyColumnsSizes = gf.newChipsColumnsSizes;
        for (int x = 0; x < fieldWidth; x++)
        {
            if (emptyColumnsSizes[x] <= 0) continue;

            for (int y = fieldHeight; y < fieldHeight + emptyColumnsSizes[x]; y++)
            {
                Chip chip = SpawnChip(new Vector2Int(x, y));
                chip.IsVisible = true;
                gf.SetChipByItsPos(chip);
            }
        }
    }

    public Chip SpawnChip(Vector2Int cellPos)
    {
        int randomIndex = UnityEngine.Random.Range(0, chipsPrefabs.Length);
        GameObject chipGO = Instantiate(chipsPrefabs[randomIndex], gf.GetCellWorldPos(cellPos), Quaternion.identity);
        chipGO.transform.SetParent(transform);

        Chip chip = chipGO.GetComponent<Chip>();
        //chip.name = "Chip_" + cellPos;
        chip.Init(settings, gf, swapHandler, cellPos);

        return chip;
    }
}
