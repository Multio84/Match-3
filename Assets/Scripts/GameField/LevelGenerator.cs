using System;
using UnityEngine;


public class LevelGenerator : MonoBehaviour, IPreloader
{
    GameSettings settings;
    GameField gf;
    SwapHandler swapHandler;

    [SerializeField] GameObject cellPrefab;
    [SerializeField] GameObject[] chipsPrefabs;
    [SerializeField] GameObject cellsRoot;

    int fieldWidth;
    int fieldHeight;

    public event Action OnLevelGenerated;


    public void Setup(GameSettings gs, GameField gf, SwapHandler sh)
    {
        settings = gs;
        this.gf = gf;
        swapHandler = sh;
    }

    public void Preload()
    {
        fieldWidth = settings.width;
        fieldHeight = settings.height;
    }

    public void GenerateLevel()
    {
        GenerateFieldBack();
        GenerateGameField();
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

    void SpawnFieldCell(Vector2Int cellPos)
    {
        GameObject cell = Instantiate(cellPrefab, gf.GetCellWorldPos(cellPos), Quaternion.identity);
        cell.transform.SetParent(cellsRoot.transform);
        cell.name = "Cell_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();
    }

    void GenerateGameField()
    {
        for (int y = 0; y < fieldHeight; y++)
        {
            for (int x = 0; x < fieldWidth; x++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);
                Chip chip = SpawnChip(cellPos);
                chip.IsVisible = true;
                if (!gf.SyncChipWithBoardByItsPos(chip))
                    Debug.LogWarning("Attempt to spawn a chip outside the GameField: chip wasn't synchronised");
            }
        }

        // generation done
        OnLevelGenerated?.Invoke();
    }

    public Chip SpawnChip(Vector2Int cellPos)
    {
        int randomIndex = UnityEngine.Random.Range(0, chipsPrefabs.Length);
        GameObject chipObj = Instantiate(chipsPrefabs[randomIndex], gf.GetCellWorldPos(cellPos), Quaternion.identity);
        chipObj.transform.SetParent(transform);

        Chip chip = chipObj.GetComponent<Chip>();
        chip.Init(settings, gf, swapHandler, cellPos);
        //chip.name = "Chip_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();

        return chip;
    }
}
