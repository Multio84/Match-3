using System.Runtime.CompilerServices;
using UnityEngine;


public class LevelGenerator : MonoBehaviour
{
    GameField gf;
    MatchManager matchManager;

    [SerializeField] GameObject cellPrefab;
    [SerializeField] GameObject[] chipsPrefabs;
    [SerializeField] GameObject cellsRoot;



    public void Setup(GameField gf, MatchManager mm)
    {
        this.gf = gf;
        matchManager = mm;
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
                if (!gf.SyncChipWithBoard(chip))
                    Debug.LogWarning("Attempt to swapn a chip outside the GameField: not synchronised");
            }
        }

        if (matchManager.FindMatches(null)) gf.ClearMatches();
    }

    public Chip SpawnChip(Vector2Int cellPos)
    {
        int randomIndex = UnityEngine.Random.Range(0, chipsPrefabs.Length);
        GameObject chipObj = Instantiate(chipsPrefabs[randomIndex], gf.GetCellWorldPos(cellPos), Quaternion.identity);
        chipObj.transform.SetParent(transform);
        Chip chip = chipObj.GetComponent<Chip>();
        chip.Init(gf, cellPos);
        //chip.name = "Chip_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();

        return chip;
    }
}
