using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GameField : MonoBehaviour
{
    [Header("Grid Properties")]
    public Grid grid;
    [SerializeField, Range(5, 7)] public int width = 7;
    [SerializeField, Range(5, 14)] public int height = 7;
    public GameObject cellPrefab;
    public GameObject[] chipsPrefabs;
    public Chip[,] chips;
    public float cellSize;

    [Header("Chip Properties")]
    public float chipDragThreshold;   // dragged distance after which chip moves by itself
    public float chipSwapDuration = 0.2f;  // chips swap animation time duration
    public float chipDeathDuration = 0.5f;  // seconds of chip death animation du


    void Start()
    {
        Init();
        SetGameFieldPos();
        chips = new Chip[width, height];
        GenerateFieldBack();
        GenerateGameField();
    }

    void Init()
    {
        cellSize = grid.cellSize.x;
        chipDragThreshold = cellSize / 5;
    }

    private void Update()
    {
        ClearMatches(FindMatches());
        //CollapseTiles();
        CollapseChips();
    }

    // game field pivot is in left bottom. This whill position it in screen center depending on the field size
    void SetGameFieldPos()
    {
        var startGameFieldPos = transform.position;
        Vector3 newPos = new Vector3();
        newPos.x = (startGameFieldPos.x - cellSize * width) / 2 + cellSize / 2;
        newPos.y = (startGameFieldPos.y - cellSize * height) / 2 + cellSize / 2;
        transform.position = newPos;
    }

    void GenerateFieldBack()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                SpawnCell(cellPos);
            }
        }
    }

    void GenerateGameField()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                SpawnChip(cellPos);
            }
        }
    }

    void SpawnCell(Vector3Int cellPos)
    {
        GameObject cell = Instantiate(cellPrefab, grid.CellToWorld(cellPos), Quaternion.identity);
        cell.transform.SetParent(transform);
    }

    void SpawnChip(Vector3Int cellPos)
    {
        int randomIndex = Random.Range(0, chipsPrefabs.Length);
        GameObject chip = Instantiate(chipsPrefabs[randomIndex], grid.CellToWorld(cellPos), Quaternion.identity);
        chip.gameObject.transform.SetParent(transform);
        chips[cellPos.x, cellPos.y] = chip.GetComponent<Chip>();
    }

    public List<Chip> FindMatches()
    {
        List<Chip> matches = new List<Chip>();

        // horizontal check
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width - 2; x++) {

                if (chips[x, y] == null || 
                    chips[x + 1, y] == null || 
                    chips[x + 2, y] == null)
                    continue;

                if (chips[x, y].isInAction == true ||
                    chips[x + 1, y].isInAction == true ||
                    chips[x + 2, y].isInAction == true)
                    continue;

                if (chips[x, y].color == chips[x + 1, y].color &&
                    chips[x, y].color == chips[x + 2, y].color) {

                    matches.Add(chips[x, y]);
                    matches.Add(chips[x + 1, y]);
                    matches.Add(chips[x + 2, y]);
                }
            }
        }

        // vertical check
        for (int y = 0; y < height - 2; y++) {
            for (int x = 0; x < width; x++) {

                if (chips[x, y] == null || 
                    chips[x, y + 1] == null || 
                    chips[x, y + 2] == null)
                    continue;

                if (chips[x, y].isInAction == true ||
                    chips[x, y + 1].isInAction == true ||
                    chips[x, y + 2].isInAction == true)
                    continue;

                if (chips[x, y].color == chips[x, y + 1].color &&
                    chips[x, y].color == chips[x, y + 2].color) {

                    matches.Add(chips[x, y]);
                    matches.Add(chips[x, y + 1]);
                    matches.Add(chips[x, y + 2]);
                }  
            }
        }

        if (matches is null)
        { Debug.Log("null matches"); }

        if (matches.Count > 0)
        { Debug.Log("There are matches"); }

        return matches;
    }

    IEnumerator KillChip(Chip chip)
    {
        chip.AnimateDeath();
        while (!chip.isDead) { yield return null; }
        if (chip != null) Destroy(chip.gameObject);
    }

    // destroy the matched chips
    void ClearMatches(List<Chip> matches)
    {
        if (matches is null || matches.Count == 0) return;

        foreach (var chip in matches) {
            StartCoroutine(KillChip(chip));
        }

        matches.Clear();
    }

    // chips falling down
    void CollapseChips()
    {
        for (int x = 0; x < width; x++) {
            int emptyCellsCount = 0;

            for (int y = 0; y < height; y++) {

                // if it's an empty cell, count it
                if (chips[x, y] == null) {
                    emptyCellsCount++;
                }
                // if it's not & there are empty cells under this one, start falling onto the first empty cell
                else if (emptyCellsCount > 0) {
                    // the lowest empty cell in this X column
                    Vector3 targetPos = grid.CellToWorld(new Vector3Int(x, y - emptyCellsCount, 0));
                    chips[x, y].AnimateFall(targetPos);

                    chips[x, y - emptyCellsCount] = chips[x, y];
                    chips[x, y] = null;
                }               
            }
        }
    }

    // fill the empty places with new chips
    void RefillGrid()
    {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (chips[x, y] == null) {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    SpawnChip(cellPos);
                }
            }
        }
    }

    public Vector2Int GetCellPosition(Vector3 worldPos)
    {
        Vector2Int cellPos = new Vector2Int(
            grid.WorldToCell(worldPos).x,
            grid.WorldToCell(worldPos).y
        );
        return cellPos;
    }

    public bool IsCellInGrid(Vector2Int cellPos)
    {
        if (cellPos.x < 0 || cellPos.x > width - 1 || cellPos.y < 0 || cellPos.y > height - 1) {
            return false;
        }

        return true;
    }

    public Chip GetChip(Vector2Int cellPos)
    {
        return chips[cellPos.x, cellPos.y];
    }

    public void AddChip(Vector2Int cellPos, Chip chip)
    {
        if (chips[cellPos.x, cellPos.y] != null) {
            chips[cellPos.x, cellPos.y] = chip;
        }
    }

    public void SwapChipsPositions(Vector3 pos1, Vector3 pos2)
    {
        Vector3Int cellPos1 = grid.WorldToCell(pos1);
        Vector3Int cellPos2 = grid.WorldToCell(pos2);

        Chip chip1 = chips[cellPos1.x, cellPos1.y];
        Chip chip2 = chips[cellPos2.x, cellPos2.y];

        chips[cellPos1.x, cellPos1.y] = chip2;
        chips[cellPos2.x, cellPos2.y] = chip1;
    }
    
}
