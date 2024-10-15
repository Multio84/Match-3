using System.Collections.Generic;
using UnityEngine;



public class GameField : MonoBehaviour
{
    [Header("Grid Properties")]
    public Grid grid;
    [SerializeField, Range(3, 7)] public int width = 7;
    [SerializeField, Range(3, 7)] public int height = 7;
    public GameObject cellPrefab;
    public GameObject[] chipsPrefabs;
    private Chip[,] chips;
    public float cellSize;

    [Header("Chip Properties")]
    public float minChipDragThreshold;   // dragged distance after which chip moves by itself
    public float chipMoveAnimDuration = 0.33f;  // chips swap animation time duration



    void Start()
    {
        Init();
        SetGameFieldPos();
        chips = new Chip[width, height];
        GenerateGameField();
    }

    void Init()
    {
        cellSize = grid.cellSize.x;
        minChipDragThreshold = cellSize / 2;
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

    void GenerateGameField()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                SpawnCell(cellPos);
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
                if (chips[x, y] != null && chips[x + 1, y] != null && chips[x + 2, y] != null) {
                    if (chips[x, y].color == chips[x + 1, y].color &&
                        chips[x, y].color == chips[x + 2, y].color) {

                        matches.Add(chips[x, y]);
                        matches.Add(chips[x + 1, y]);
                        matches.Add(chips[x + 2, y]);
                    }
                }
            }
        }

        // vertical check
        for (int y = 0; y < height - 2; y++) {
            for (int x = 0; x < width; x++) {
                if (chips[x, y] != null && chips[x, y + 1] != null && chips[x, y + 2] != null) {
                    if (chips[x, y].color == chips[x, y + 1].color &&
                        chips[x, y].color == chips[x, y + 2].color) {

                        matches.Add(chips[x, y]);
                        matches.Add(chips[x, y + 1]);
                        matches.Add(chips[x, y + 2]);
                    }
                }
            }
        }
        return matches;
    }

    // destroy the matched chips
    void ClearMatches(List<Chip> matches)
    {
        foreach (var chip in matches) {
            Destroy(chip);
        }
    }
    

    void CollapseTiles()
    {
        for (int x = 0; x < width; x++) {
            // Начинаем с 1, потому что нет фишек выше y = 0
            for (int y = 1; y < height; y++) {
                if (chips[x, y] == null) {
                    for (int aboveY = y; aboveY < height; aboveY++) {

                        chips[x, aboveY - 1] = chips[x, aboveY];
                        chips[x, aboveY] = null;
                    }
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

    public Vector3Int GetCellPosition(Vector3 worldPos)
    {
        return grid.WorldToCell(worldPos);
    }

    public Chip GetChipInCell(Vector3Int cellPos)
    {
        return chips[cellPos.x, cellPos.y];
    }

    public void AddChip(Vector3Int cellPos, Chip chip)
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