using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class GameField : MonoBehaviour, IInitializable
{
    LevelGenerator levelGenerator;
    MatchManager matchManager;
    [HideInInspector] public SwapManager swapManager;

    [Header("Grid Properties")]
    public Grid grid;
    [Range(5, 7)] public int width = 7;
    [Range(5, 14)] public int height = 14;

    public Chip[,] chips;
    public float cellSize;

    [Header("Chip Properties")]
    public float chipDragThreshold;   // dragged distance after which chip moves by itself

    public float chipDeathDuration = 2f;  // seconds of chip death animation du
    public float chipFallDuration = 0.4f;   // duration of falling chip animation
    public float chipFallGravity = 2;   // gravity for falling chip, that is falling speed factor
    const int ChipsFallDelay = 10;  // miliseconds to await before next set of chips falling
    int totalChipsToFallCount = 0;  // chip collapse in rows: one row after another. This is total count of chips of all rows to collapse until collapse is done
    int chipsToDelete = 0;  // number of chips, going to be deleted in current iteration


    public void Init()
    {
        cellSize = grid.cellSize.x;
        chipDragThreshold = cellSize / 5;
        chips = new Chip[width, height];

        SetGameFieldPos();
    }

    public void Setup(LevelGenerator lg, MatchManager mm, SwapManager sm)
    {
        levelGenerator = lg;
        matchManager = mm;
        swapManager = sm;
    }

    // game field pivot is in left bottom. This will position field in screen center, depending on the field size
    void SetGameFieldPos()
    {
        var startGameFieldPos = transform.position;
        Vector3 newPos = new Vector3();
        newPos.x = (startGameFieldPos.x - cellSize * width) / 2 + cellSize / 2;
        newPos.y = (startGameFieldPos.y - cellSize * height) / 2 + cellSize / 2;
        transform.position = newPos;
    }

    public bool SyncChipWithBoard(Chip chip)
    {
        Vector2Int cell = chip.CellPos;
        if (!IsCellInField(cell.x, cell.y)) return false;
        chips[cell.x, cell.y] = chip;

        return true;
    }

    public bool IsValidChip(int x, int y)
    {
        if (!IsCellInField(x, y)) {
            //Debug.Log($"Cell ({x}, {y}) is not in field.");
            return false;
        }
        if (chips[x, y] is null) {
            //Debug.Log($"Cell ({x}, {y}) is null.");
            return false;
        }

        return true;
    }

    public void UpdateSwappedChips(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.CellPos;
        Vector2Int cell2 = operation.swappedChip.CellPos;

        // change places in array
        chips[cell1.x, cell1.y] = operation.swappedChip;
        chips[cell2.x, cell2.y] = operation.draggedChip;

        // change cellposes in chip's properties
        operation.draggedChip.CellPos = cell2;
        operation.swappedChip.CellPos = cell1;

        HandleSwap(operation);
    }

    void HandleSwap(SwapOperation operation)
    {
        if (operation.isReverse)
        {
            operation.Stop();
            return;
        }

        if (matchManager.FindMatches(operation))
        {
            operation.Stop();
            ClearMatches();
        }
        else
        {
            // reverse swap: previously swapped chip becomes the "dragged" one
            swapManager.Swap(operation.swappedChip, operation.direction, true);
        }
    }

    // destroy the matched chips
    public void ClearMatches()
    {
        chipsToDelete = 0;
        foreach (var chip in chips) {
            if (chip is not null && chip.IsMatched) {
                chip.OnDeathCompleted -= HandleChipDeath;   // to exclude double subscription
                chip.OnDeathCompleted += HandleChipDeath;

                chipsToDelete++;
                chip.Die();
            }
        }
        //Debug.Log($"Chips sent to die: {chipsToDelete}");
        totalChipsToFallCount = chipsToDelete;
    }

    void HandleChipDeath(Chip chip)
    {
        if (chips[chip.CellPos.x, chip.CellPos.y] == chip) {
            chips[chip.CellPos.x, chip.CellPos.y] = null;
            //Debug.Log($"Chip_{chip} removed successfully.");
        }
        else {
            //Debug.LogWarning($"Mismatch or null reference for Chip_{chip.CellPos}");
        }

        UnsubscribeFromChip(chip);

        if (chip is null || chip.gameObject is null) {
            Debug.LogWarning($"Trying to destroy non-existing chip.");
            return;
        }

        Destroy(chip.gameObject);
        chip = null;
        chipsToDelete--;
        //Debug.Log($"Chips left to die: {chipsToDelete}");

        if (chipsToDelete <= 0) 
            HandleMatchesCleared();
    }

    void UnsubscribeFromChip(Chip chip)
    {
        if (chip is null) return;

        chip.OnDeathCompleted -= HandleChipDeath;
        chip.OnChipLanded -= HandleChipLanded;
    }

    void HandleMatchesCleared()
    {
        //Debug.Log("Matches cleared. Starting Collapse.");
        CollapseChips();
    }
    

    // ========= COLLAPSE ===========

    async void CollapseChips()
    {
        int iteration = 0;
        int maxIterations = height * 10;    // protection from the infinite loop

        while (iteration < maxIterations) {
            Dictionary<Vector2Int, Chip> chipsToFall = CollectChipsToFall();
            if (chipsToFall.Count == 0) {
                //Debug.Log("No more chips to fall were found.");
                break;
            }

            SyncFallingChipsWithBoard(chipsToFall);
            StartCoroutine(DropChips(chipsToFall));
            await Task.Delay(ChipsFallDelay);

            iteration++;
        }
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
                        Chip newChip = levelGenerator.SpawnChip(new Vector2Int(x, height));
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
            if (IsCellInField(chipCell.x, chipCell.y))
                chips[chipCell.x, chipCell.y] = null;
            
            chips[targetCell.x, targetCell.y] = entry.Value;
            chips[targetCell.x, targetCell.y].CellPos = targetCell;
        }
    }

    IEnumerator DropChips(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall) {
            Vector3Int targetCell = new Vector3Int(entry.Key.x, entry.Key.y, 0);
            Chip chip = entry.Value;

            chip.OnChipLanded += HandleChipLanded;
            Vector3 pos = grid.CellToWorld(targetCell);
            chip.Fall(pos);
        }
        yield return null;
    }

    void HandleChipLanded()
    {
        totalChipsToFallCount--;
        if (totalChipsToFallCount == 0) HandleCollapseComplete();
    }

    void HandleCollapseComplete()
    {
        if (matchManager.FindMatches(null))
            ClearMatches();
    }

    public Vector2Int GetCellGridPos(Vector3 worldPos)
    {
        Vector3Int cellPos3 = grid.WorldToCell(worldPos);

        return new Vector2Int(cellPos3.x, cellPos3.y);
    }

    public Vector3 GetCellWorldPos(Vector2Int cellPos)
    {
        return grid.CellToWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
    }

    public bool IsCellInField(int x, int y)
    {
        if (chips == null) Debug.LogError("Chips array is empty!");

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
