using System;
using UnityEngine;


public class LevelGenerator : MonoBehaviour
{
    GameField gf;
    CollapseHandler collapseHandler;

    [SerializeField] GameObject cellPrefab;
    [SerializeField] GameObject[] chipsPrefabs;
    [SerializeField] GameObject cellsRoot;

    public Action OnLevelGenerated;


    public void Setup(GameField gf, CollapseHandler ch)
    {
        this.gf = gf;
        collapseHandler = ch;
    }

    public void GenerateLevel()
    {
        GenerateFieldBack();
        GenerateGameField();
    }

    void GenerateFieldBack()
    {
        for (int y = 0; y < gf.height; y++)
        {
            for (int x = 0; x < gf.width; x++)
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
        for (int y = 0; y < gf.height; y++)
        {
            for (int x = 0; x < gf.width; x++)
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
        chip.Init(gf, collapseHandler, cellPos);
        //chip.name = "Chip_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();

        return chip;
    }
}
