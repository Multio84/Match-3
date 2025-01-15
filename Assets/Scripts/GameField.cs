using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


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
    public float chipDeathDuration = 3.5f;  // seconds of chip death animation du
    public float chipFallDuration = 0.4f;   // duration of falling chip animation
    public float chipFallGravity = 2;   // gravity for falling chip, that is falling speed factor
    float nextChipFallDelay = 0.2f;
    int fallingChipsCount = 0;  // count chips before falling
    const int MinMatchSize = 3;    // number of cells, minimum for match in line, except the first chip
    const int MaxMatchSize = 14;    // number of cells, maximum for match in line, except the first chip

    [HideInInspector] public Chip draggedChip;
    [HideInInspector] public Chip swappedChip;
    bool matchesFound = false;  // true, if at least 1 match was found

    public event Action OnSwapComplete;
    public event Action OnMatchesFound; // если очистка мэтчей запускаема без события, то не надо
    public event Action OnMatchesCleared;
    public event Action OnCollapseComplete;


    void Start()
    {
        Initialize();
        SetGameFieldPos();
        chips = new Chip[width, height];
        GenerateFieldBack();
        GenerateGameField();

        OnMatchesCleared += HandleMatchesCleared;
        //OnCollapseComplete += CheckForNewMatches;
    }

    // ========= INIT ===========

    void Initialize()
    {
        cellSize = grid.cellSize.x;
        chipDragThreshold = cellSize / 5;
    }

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
        chipComponent.CellPos = new Vector2Int(cellPos.x, cellPos.y);
    }

    void GenerateGameField()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                SpawnChip(cellPos);
            }
        }

        //if (FindMatches(GetFieldBounds())) {
        //    ClearMatches();
        //}
    }

    // ========= CHECK ===========

    // region of 1 cell
    Vector2Int[] GetFieldRegionBounds(Vector2Int cell)
    {
        Vector2Int bottomLeft = new Vector2Int(
            Mathf.Max(cell.x - MaxMatchSize - 1, 0),
            Mathf.Max(cell.y - MaxMatchSize - 1, 0)
        );

        Vector2Int rightTop = new Vector2Int(
            Mathf.Min(cell.x + MaxMatchSize - 1, width - 1),
            Mathf.Min(cell.y + MaxMatchSize - 1, height - 1)
        );

        return new Vector2Int[] { bottomLeft, rightTop };
    }

    // region to search for matches for 2 adjacent cells, on which chips are being swapped
    public Vector2Int[] GetFieldRegionBounds(Vector2Int cell1, Vector2Int cell2)
    {
        int searchDistance = MinMatchSize - 1;    // distance (from swappind chips) in cells to search
        int minX = Mathf.Max(Mathf.Min(cell1.x, cell2.x) - searchDistance, 0);
        int minY = Mathf.Max(Mathf.Min(cell1.y, cell2.y) - searchDistance, 0);
        int maxX = Mathf.Min(Mathf.Max(cell1.x, cell2.x) + searchDistance, width - 1);
        int maxY = Mathf.Min(Mathf.Max(cell1.y, cell2.y) + searchDistance, height - 1);

        Vector2Int bottomLeft = new Vector2Int(minX, minY);
        Vector2Int topRight = new Vector2Int(maxX, maxY);

        return new Vector2Int[] { bottomLeft, topRight };
    }

    public Vector2Int[] GetFieldBounds()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0),
            new Vector2Int(width - 1, height - 1)
        };
    }

    //public void GetFieldBounds(out Vector2Int min, out Vector2Int max)
    //{
    //    min = new Vector2Int(0, 0);
    //    max = new Vector2Int(width - 1, height - 1);
    //}

    bool FindMatches(Vector2Int[] bounds)
    {
        if (bounds.Length != 2) throw new ArgumentException("The method requires exactly two cell corners of GameField.");

        CheckMatchesInDirection(bounds, Vector2Int.right);
        CheckMatchesInDirection(bounds, Vector2Int.up);

        return matchesFound;
    }

    bool CheckMatchesInDirection(Vector2Int[] bounds, Vector2Int direction)
    {
        Vector2Int min = bounds[0];
        Vector2Int max = bounds[1];
        // for search in reduced region for each direction
        if (direction.x != 0) {
            max.x = bounds[1].x - MinMatchSize + 1; 
        }
        else {
            max.y = bounds[1].y - MinMatchSize + 1;
        }

        Chip currentChip = null;
        List<Chip> chipsToCheck = new List<Chip>(MaxMatchSize); // chips in line, which should be checked for match

        // horizontal check
        for (int y = min.y; y <= max.y; y++) {
            for (int x = min.x; x <= max.x; x++) {
                if (!IsValidChip(x, y)) continue;
                //if (chips[x, y].IsMatched == true) continue;

                for (int i = 0; i < MaxMatchSize; i++) {
                    int checkX = x + i * direction.x;
                    int checkY = y + i * direction.y;
                    if (!IsValidChip(checkX, checkY)) break;

                    if (i == 0) {
                        // save the first chip without check
                        currentChip = chips[x, y];
                        chipsToCheck.Add(currentChip);
                    }
                    else if (currentChip.Color == chips[checkX, checkY].Color) {
                        chipsToCheck.Add(chips[checkX, checkY]);
                    }
                    else break;
                }

                if (chipsToCheck.Count >= MinMatchSize) {
                    foreach (Chip chip in chipsToCheck) {
                        if (chip is null) break;
                        chip.IsMatched = true;
                    }
                    matchesFound = true;
                }
                chipsToCheck.Clear();
            }
        }
        return matchesFound;
    }

    bool IsValidChip(int x, int y)
    {
        if (!IsCellInField(x, y)) {
            //Debug.Log($"Cell ({x}, {y}) is not in field.");
            return false;
        }
        if (chips[x, y] == null) {
            //Debug.Log($"Cell ({x}, {y}) is null.");
            return false;
        }
        if (chips[x, y].IsMatched == true) {
            //Debug.Log($"Cell ({x}, {y}) is Matched already.");
            return false;
        }

        return true;
    }



    // ========= CLEAR ===========


    // destroy the matched chips
    void ClearMatches()
    {
        foreach (var chip in chips) {
            if (chip is not null && chip.IsMatched) {
                chip.OnDeathCompleted -= HandleChipDeath;   // to exclude double subscription
                chip.OnDeathCompleted += HandleChipDeath;
                //Debug.LogError($"Chip {chip.CellPos} is to be deleted");
                chip.Die();
            }
        }

        matchesFound = false;
    }

    //void HandleChipDeath(Chip chip)
    //{
    //    chips[chip.CellPos.x, chip.CellPos.y] = null;
    //    chip.OnDeathCompleted -= HandleChipDeath;
    //    Destroy(chip.gameObject);
    //    //Debug.LogError($"Chip {chip.CellPos} was deleted");
    //}

    void HandleChipDeath(Chip chip)
    {
        if (chips[chip.CellPos.x, chip.CellPos.y] == chip) {
            chips[chip.CellPos.x, chip.CellPos.y] = null;
            Debug.Log($"Chip_{chip.CellPos.x}_{chip.CellPos.y} removed successfully.");
        }
        else {
            Debug.LogWarning($"Mismatch or null reference for Chip_{chip.CellPos.x}_{chip.CellPos.y}");
        }

        chip.OnDeathCompleted -= HandleChipDeath;

        if (chip is not null && chip.gameObject is not null) {
            Destroy(chip.gameObject);
            chip = null;
        }
        else {
            Debug.LogWarning($"Trying to destroy non-existing chip.");
        }
    }

    void HandleMatchesCleared()
    {
        StartCoroutine(CollapseChips());
    }

    // ========= SWAP ===========

    public void SwapChips(bool isReverseSwap)
    {
        if (isReverseSwap) {
            Chip reverseDraggedChip = swappedChip;
            Chip reverseSwappedChip = draggedChip;

            swappedChip = reverseDraggedChip;
            draggedChip = reverseSwappedChip;
        }

        draggedChip.IsMoving = true;
        swappedChip.IsMoving = true;

        OnSwapComplete += HandleSwap;

        StartCoroutine(AnimateChipsSwapping());
    }

    void HandleSwap()
    {
        Vector2Int[] bounds = GetFieldRegionBounds(draggedChip.CellPos, swappedChip.CellPos);

        if (FindMatches(bounds)) {
            Debug.Log($"{draggedChip} and {swappedChip} were swapped.");
            ClearMatches();
        }
        else {
            SwapChips(true); // reverse swap
            Debug.Log($"{draggedChip} and {swappedChip} reversed swap!");
        }
    }

    // animates 2 chips swap
    IEnumerator AnimateChipsSwapping()
    {
        // set positions
        Vector3 chip1Pos = draggedChip.transform.position;
        Vector3 chip2Pos = swappedChip.transform.position;

        float elapsedTime = 0;

        // animate chips swap
        while (elapsedTime < chipSwapDuration)
        {
            draggedChip.transform.position = Vector3.Lerp(chip1Pos, chip2Pos, elapsedTime / chipSwapDuration);
            swappedChip.transform.position = Vector3.Lerp(chip2Pos, chip1Pos, elapsedTime / chipSwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        draggedChip.transform.position = chip2Pos;
        swappedChip.transform.position = chip1Pos;

        SwapChipsCellsInGrid(draggedChip, swappedChip);
    }

    public void SwapChipsCellsInGrid(Chip draggedChip, Chip swappedChip)
    {
        Vector2Int cellPos1 = draggedChip.CellPos;
        Vector2Int cellPos2 = swappedChip.CellPos;

        // change places in array
        chips[cellPos1.x, cellPos1.y] = swappedChip;
        chips[cellPos2.x, cellPos2.y] = draggedChip;

        // change cellposes in chip's properties
        draggedChip.CellPos = cellPos2;
        swappedChip.CellPos = cellPos1;

        draggedChip.IsMoving = false;
        swappedChip.IsMoving = false;

        // TODO: нужно ли это?
        draggedChip.IsInAction = false;
        swappedChip.IsInAction = false;

        OnSwapComplete?.Invoke();
        OnSwapComplete = null;  // unsubscribe
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

    public bool IsCellInField(int x, int y)
    {
        if (x < 0 || x >= chips.GetLength(0) ||
            y < 0 || y >= chips.GetLength(1)) {
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

}
