using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public float chipDeathDuration = 2f;  // seconds of chip death animation du
    public float chipFallDuration = 0.4f;   // duration of falling chip animation
    public float chipFallGravity = 2;   // gravity for falling chip, that is falling speed factor
    const int MinMatchSize = 3;    // number of cells, minimum for match in line, except the first chip
    const int MaxMatchSize = 14;    // number of cells, maximum for match in line, except the first chip
    const float ReverseSwapDelay = 0.15f;   // seconds before automatic swap, when manual swap didn't lead to match
    const int ChipsFallDelay = 10;  // miliseconds to await before next set of chips falling
    int totalChipsToFallCount = 0;  // chip collapse in rows: one row after another. This is total count of chips of all rows to collapse until collapse is done
    [HideInInspector] public Chip draggedChip;
    [HideInInspector] public Chip swappedChip;
    bool matchesFound = false;  // true, if at least 1 match was found

    public event Action OnSwapComplete;
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

    private void OnDestroy()
    {
        OnMatchesCleared -= HandleMatchesCleared;
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

    void GenerateFieldBack()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                SpawnCell(new Vector3Int(x, y, 0));
            }
        }
    }

    void SpawnCell(Vector3Int cellPos)
    {
        GameObject cell = Instantiate(cellPrefab, grid.CellToWorld(cellPos), Quaternion.identity);
        cell.transform.SetParent(cellsRoot.transform);
        cell.name = "Cell_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();
    }

    void GenerateGameField()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector2Int cellPos = new Vector2Int(x, y);
                Chip chip = SpawnChip(cellPos);
                chip.IsVisible = true;
                if (!SyncChipWithBoard(chip))
                    Debug.LogWarning("Attempt to swapn a chip outside the GameField: not synchronised");
            }
        }

        //if (FindMatches(GetFieldBounds())) {
        //    ClearMatches();
        //}
    }

    Chip SpawnChip(Vector2Int cellPos)
    {
        int randomIndex = UnityEngine.Random.Range(0, chipsPrefabs.Length);
        Vector3Int cell3DPos = new Vector3Int(cellPos.x, cellPos.y, 0);
        GameObject chipObj = Instantiate(chipsPrefabs[randomIndex], grid.CellToWorld(cell3DPos), Quaternion.identity);
        chipObj.gameObject.transform.SetParent(transform);
        Chip chip = chipObj.GetComponent<Chip>();
        chip.CellPos = cellPos;
        //chip.name = "Chip_" + cellPos.x.ToString() + "_" + cellPos.y.ToString();

        return chip;
    }

    bool SyncChipWithBoard(Chip chip)
    {
        if (!IsCellInField(chip.CellPos.x, chip.CellPos.y)) return false;
        chips[chip.CellPos.x, chip.CellPos.y] = chip;

        return true;
    }

    // ========= CHECK ===========

    // region of 1 cell
    Vector2Int[] GetFieldRegionBounds(Vector2Int cell)
    {
        int searchDistance = MinMatchSize - 1;    // distance (from swappind chips) in cells to search

        Vector2Int bottomLeft = new Vector2Int(
            Mathf.Max(cell.x - searchDistance, 0),
            Mathf.Max(cell.y - searchDistance, 0)
        );
        Vector2Int rightTop = new Vector2Int(
            Mathf.Min(cell.x + searchDistance, width - 1),
            Mathf.Min(cell.y + searchDistance, height - 1)
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
        // for search in reduced region in each direction
        // avoids searching in the whole field
        if (direction.x != 0) {
            max.x = bounds[1].x + 1 - MinMatchSize; 
        }
        else {
            max.y = bounds[1].y + 1 - MinMatchSize;
        }

        Chip currentChip = null;
        HashSet<Chip> chipsToCheck = new HashSet<Chip>(MaxMatchSize); // chips in line, which should be checked for match

        for (int y = min.y; y <= max.y; y++) {
            for (int x = min.x; x <= max.x; x++) {
                // TODO: when all common algorythm is done, check if it's really needed:
                if (!IsValidChip(x, y)) continue;

                // save the first chip without check
                currentChip = chips[x, y];
                chipsToCheck.Add(currentChip);

                for (int i = 1; i < MinMatchSize; i++) {
                    int checkX = x + i * direction.x;
                    int checkY = y + i * direction.y;
                    if (!IsValidChip(checkX, checkY)) break;

                    if (currentChip.Color == chips[checkX, checkY].Color) {
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
        if (!IsCellInField(x, y))
        {
            //Debug.Log($"Cell ({x}, {y}) is not in field.");
            return false;
        }
        if (chips[x, y] is null)
        {
            //Debug.Log($"Cell ({x}, {y}) is null.");
            return false;
        }

        return true;
    }


    // ========= SWAP ===========

    public void SwapChips(bool isReverse)
    {
        if (isReverse)
        {
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

        if (FindMatches(bounds))
        {
            //Debug.Log($"{draggedChip} and {swappedChip} were swapped.");
            ClearMatches();
        }
        else
        {
            SwapChips(true); // reverse swap
            //Debug.Log($"{draggedChip} and {swappedChip} reversed swap!");
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

        yield return new WaitForSeconds(ReverseSwapDelay);

        UpdateSwapInGrid(draggedChip, swappedChip);
    }

    public void UpdateSwapInGrid(Chip draggedChip, Chip swappedChip)
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

        // TODO: ����� �� ���?
        draggedChip.IsInAction = false;
        swappedChip.IsInAction = false;

        OnSwapComplete?.Invoke();
        OnSwapComplete = null;  // unsubscribe
    }


    // ========= CLEAR ===========

    int chipsToDelete = 0;  // number of chips, going to be deleted in current iteration

    // destroy the matched chips
    void ClearMatches()
    {
        foreach (var chip in chips) {
            if (chip is not null && chip.IsMatched) {
                chip.OnDeathCompleted -= HandleChipDeath;   // to exclude double subscription
                chip.OnDeathCompleted += HandleChipDeath;

                chipsToDelete++;
                chip.Die();
            }
        }
        totalChipsToFallCount = chipsToDelete;
        matchesFound = false;
    }

    void HandleChipDeath(Chip chip)
    {
        if (chips[chip.CellPos.x, chip.CellPos.y] == chip) {
            chips[chip.CellPos.x, chip.CellPos.y] = null;
            Debug.Log($"Chip_{chip} removed successfully.");
        }
        else {
            Debug.LogWarning($"Mismatch or null reference for Chip_{chip.CellPos}");
        }

        chip.OnDeathCompleted -= HandleChipDeath;

        if (chip is not null && chip.gameObject is not null) {
            Destroy(chip.gameObject);
            chip = null;
            chipsToDelete--;
            if (chipsToDelete == 0) {
                OnMatchesCleared?.Invoke();
            }
        }
        else {
            Debug.LogWarning($"Trying to destroy non-existing chip.");
        }
    }

    void HandleMatchesCleared()
    {
        CollapseChips();
    }
    

    // ========= COLLAPSE ===========

    async void CollapseChips()
    {
        int iteration = 0;
        int maxIterations = height * 10;    // protection from infinite loop

        while (iteration < maxIterations) {
            Dictionary<Vector2Int, Chip> chipsToFall = CollectChipsToFall();
            if (chipsToFall.Count == 0) {
                Debug.LogWarning("No more chips to fall were found.");
                break;
            }

            SyncFallingChipsWithBoard(chipsToFall);
            StartCoroutine(DropChips(chipsToFall));
            await Task.Delay(ChipsFallDelay);

            iteration++;
        }

        OnCollapseComplete?.Invoke();
    }

    // Collects chips, that should fall simultaneously, and their target cells
    Dictionary<Vector2Int, Chip> CollectChipsToFall()
    {
        Dictionary<Vector2Int, Chip> chipsToFall = new Dictionary<Vector2Int, Chip>();

        for (int x = 0; x < width; x++) {
            Vector2Int? bottomCell = null;  // first cell (from bottom) without chip in current column

            for (int y = 0; y < height; y++) {
                if (chips[x ,y] is null) {
                    if (bottomCell is null){
                        bottomCell = new Vector2Int(x, y);  // save bottom cell
                    }

                    if (y == height - 1) {
                        // spawn and save new chip outside (over) field
                        Chip newChip = SpawnChip(new Vector2Int(x, height));
                        if (!chipsToFall.TryAdd(bottomCell.Value, newChip))
                            Debug.LogError($"Duplicate target cell {bottomCell.Value} while adding new chip {newChip}");
                        break;
                    }
                }
                else if (bottomCell.HasValue) {
                    // save old chip to fall
                    if (!chipsToFall.TryAdd(bottomCell.Value, chips[x, y]))
                        Debug.LogError($"Duplicate target cell {bottomCell.Value} while adding existing chip {chips[x, y]}");
                    break;
                }
            }
        }

        return chipsToFall;
    }

    // changes chip's position in array: moves it from start chip's place to the cellPos
    void SyncFallingChipsWithBoard(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall) {
            Vector2Int targetCell = entry.Key;
            Vector2Int chipCell = entry.Value.CellPos;

            if (!IsCellInField(targetCell.x, targetCell.y)) {
                Debug.LogError("Sync field while collapsing: target cell for falling chip is outside the field.");
                break;
            }

            // chip created outside of the field is not in chips array yet,
            // so it doesn't have to be nullified
            if (IsCellInField(chipCell.x, chipCell.y)) {
                chips[chipCell.x, chipCell.y] = null;
            }
            chips[targetCell.x, targetCell.y] = entry.Value;
            chips[targetCell.x, targetCell.y].CellPos = targetCell;
        }
    }

    IEnumerator DropChips(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall) {
            Debug.Log($"Processing chip to Fall from {entry.Value.CellPos} to {entry.Key}");

            Vector3Int targetCell = new Vector3Int(entry.Key.x, entry.Key.y, 0);
            Chip chip = entry.Value;

            chip.OnChipLanded += OnChipsLanded;
            Vector3 pos = grid.CellToWorld(targetCell);
            chip.Fall(pos);
        }
        yield return null;
    }

    void OnChipsLanded()
    {
        totalChipsToFallCount--;
        if (totalChipsToFallCount == 0) OnCollapseComplete?.Invoke();
    }

    public Vector2Int GetCellGridPosition(Vector3 worldPos)
    {
        Vector2Int cellPos = new Vector2Int(
            grid.WorldToCell(worldPos).x,
            grid.WorldToCell(worldPos).y
        );
        return cellPos;
    }

    //public bool IsCellInGrid(Vector2Int cellPos)
    //{
    //    if (cellPos.x < 0 || cellPos.x > width - 1 || cellPos.y < 0 || cellPos.y > height - 1) {
    //        return false;
    //    }
    //    return true;
    //}

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

}
