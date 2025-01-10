using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameField : MonoBehaviour
{
    [Header("Grid Properties")]
    public Grid grid;
    [Range(5, 7)] public int width = 7;
    [Range(5, 14)] public int height = 14;
    [SerializeField] GameObject cellsRoot;
    public GameObject cellPrefab;
    public GameObject[] chipsPrefabs;
    public Chip[,] chips;
    public float cellSize;

    [Header("Chip Properties")]
    public float chipDragThreshold;   // dragged distance after which chip moves by itself
    public float chipSwapDuration = 0.2f;  // chips swap animation time duration
    public float chipDeathDuration = 0.5f;  // seconds of chip death animation du
    public float chipFallDuration = 0.4f;   // duration of falling chip animation
    public float chipFallGravity = 2;   // gravity for falling chip, that is falling speed factor
    float nextChipFallDelay = 0.2f;
    int fallingChipsCount = 0;  // count chips before falling
    int fieldRegionSize = 2;    // number of cells to search around the cell for all 4 directions
    
    public Vector2Int[] swappingCells;  // positions of chips, which have started to swap

    public event Action OnFiedlChanged;
    public event Action OnMatchesFound; // если очистка мэтчей запускаема без события, то не надо
    public event Action OnMatchesCleared;
    public event Action OnCollapseComplete;


    void Start()
    {
        Init();
        SetGameFieldPos();
        chips = new Chip[width, height];
        GenerateFieldBack();
        GenerateGameField();

        OnMatchesCleared += HandleMatchesCleared;
        OnCollapseComplete += CheckForNewMatches;
    }

    // ========= INIT ===========

    void Init()
    {
        cellSize = grid.cellSize.x;
        chipDragThreshold = cellSize / 5;
    }

    //private void Update()
    //{
    //    ClearMatches(FindMatches());
    //    StartCoroutine(CollapseChips());
    //}



    // ========= GENERATE ===========

    // game field pivot is in left bottom. This will position it in screen center depending on the field size
    void SetGameFieldPos()
    {
        var startGameFieldPos = transform.position;
        Vector3 newPos = new Vector3();
        newPos.x = (startGameFieldPos.x - cellSize * width) / 2 + cellSize / 2;
        newPos.y = (startGameFieldPos.y - cellSize * height) / 2 + cellSize / 2;
        transform.position = newPos;
    }

    void SpawnCell(Vector3Int cellPos)
    {
        GameObject cell = Instantiate(cellPrefab, grid.CellToWorld(cellPos), Quaternion.identity);
        cell.transform.SetParent(cellsRoot.transform);
        cell.name = "Cell_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();
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

    void SpawnChip(Vector3Int cellPos)
    {
        int randomIndex = UnityEngine.Random.Range(0, chipsPrefabs.Length);
        GameObject chip = Instantiate(chipsPrefabs[randomIndex], grid.CellToWorld(cellPos), Quaternion.identity);
        chip.gameObject.transform.SetParent(transform);
        chip.name = "Chip_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();

        Chip chipComponent = chip.GetComponent<Chip>();
        chips[cellPos.x, cellPos.y] = chipComponent;
        chipComponent.cellPos = new Vector2Int(cellPos.x, cellPos.y);
    }

    void GenerateGameField()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                SpawnChip(cellPos);
            }
        }

        CheckForNewMatches();
    }


    // ========= SWAP ===========

    // animates 2 chips swap
    public IEnumerator SwapChips(Chip chip1, Chip chip2)
    {
        chip1.isMoving = true;
        chip2.isMoving = true;

        // set positions
        Vector3 chip1Pos = chip1.transform.position;
        Vector3 chip2Pos = chip2.transform.position;

        Vector3 chip1NewPos = chip2Pos;
        Vector3 chip2NewPos = chip1Pos;

        float elapsedTime = 0;

        // animate chips swap
        while (elapsedTime < chipSwapDuration)
        {
            chip1.transform.position = Vector3.Lerp(chip1Pos, chip2Pos, elapsedTime / chipSwapDuration);
            chip2.transform.position = Vector3.Lerp(chip2Pos, chip2NewPos, elapsedTime / chipSwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        chip1.transform.position = chip2Pos;
        chip2.transform.position = chip2NewPos;

        // update chips array
        SwapChipsPositions(chip1.transform.position, chip2.transform.position);

        chip1.isMoving = false;
        chip2.isMoving = false;
        // нужно ли это?
        chip1.isInAction = false;
        chip2.isInAction = false;

        //OnChipLanded?.Invoke(); // GameField is counting landed chips
        //OnChipLanded = null;    // unsubscrive from landing event
    }


    // ========= CHECK ===========

    void CheckForNewMatches()
    {
        var matches = FindMatches();

        if (matches.Count > 0) {
            ClearMatches(matches);
            OnMatchesCleared?.Invoke();     // start collapsing chips
        }
    }

    // region of 1 cell
    Vector2Int[] GetFieldRegionBounds(Vector2Int cell)
    {
        Vector2Int bottomLeft = new Vector2Int(
            Mathf.Max(cell.x - fieldRegionSize, 0),
            Mathf.Max(cell.y - fieldRegionSize, 0)
        );

        Vector2Int rightTop = new Vector2Int(
            Mathf.Min(cell.x + fieldRegionSize, width - 1),
            Mathf.Min(cell.y + fieldRegionSize, height - 1)
        );

        return new Vector2Int[] { bottomLeft, rightTop };
    }

    // region of 2 adjacent cells, on which chips are being swapped
    public Vector2Int[] GetFieldRegionBounds(Vector2Int cell1, Vector2Int cell2)
    {
        int minX = Mathf.Max(Mathf.Min(cell1.x, cell2.x) - fieldRegionSize, 0);
        int minY = Mathf.Max(Mathf.Min(cell1.y, cell2.y) - fieldRegionSize, 0);
        int maxX = Mathf.Min(Mathf.Max(cell1.x, cell2.x) + fieldRegionSize, width - 1);
        int maxY = Mathf.Min(Mathf.Max(cell1.y, cell2.y) + fieldRegionSize, height - 1);

        Vector2Int bottomLeft = new Vector2Int(minX, minY);
        Vector2Int topRight = new Vector2Int(maxX, maxY);

        return new Vector2Int[] { bottomLeft, topRight };
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

    // ========= CLEAR ===========


    void HandleMatchesCleared()
    {
        StartCoroutine(CollapseChips());
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

    IEnumerator KillChip(Chip chip)
    {
        chip.Kill();
        while (!chip.isDead) { yield return null; }
        if (chip != null) Destroy(chip.gameObject);
    }

    

    // ========= FALL ===========

    // chips falling down
    IEnumerator CollapseChips()
    {
        for (int x = 0; x < width; x++) {
            int emptyCellsCount = 0;

            for (int y = 0; y < height; y++) {

                // if it's an empty cell, count it
                if (chips[x, y] == null) {
                    emptyCellsCount++;
                }
                // if it's not emplty & there are empty cells under this one, start falling onto the first empty cell
                else if (emptyCellsCount > 0) {
                    // position of the lowest empty cell in this X column
                    Vector3 targetPos = grid.CellToWorld(new Vector3Int(x, y - emptyCellsCount, 0));
                    chips[x, y].OnChipLanded += OnChipLanded;
                    chips[x, y].Fall(targetPos);

                    chips[x, y - emptyCellsCount] = chips[x, y];
                    chips[x, y] = null;

                    yield return new WaitForSeconds(nextChipFallDelay);
                }               
            }
        }
    }

    void OnChipLanded()
    {
        fallingChipsCount--;

        if (fallingChipsCount == 0) OnCollapseComplete?.Invoke();
    }



    // fill the empty places with new chips
    //void RefillGrid()
    //{
    //    for (int x = 0; x < width; x++) {
    //        for (int y = 0; y < height; y++) {
    //            if (chips[x, y] == null) {
    //                Vector3Int cellPos = new Vector3Int(x, y, 0);
    //                SpawnChip(cellPos);
    //            }
    //        }
    //    }
    //}

    // ========= HELPERS ===========

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

    //public void AddChip(Vector2Int cellPos, Chip chip)
    //{
    //    if (chips[cellPos.x, cellPos.y] != null) {
    //        chips[cellPos.x, cellPos.y] = chip;
    //    }
    //}

    // ========= SWAP ===========

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
