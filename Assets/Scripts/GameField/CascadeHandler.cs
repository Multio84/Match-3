using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;


public class CascadeHandler : SettingsSubscriber, IInitializer
{
    public override GameSettings Settings { get; set; }
    GameField gameField;

    int fieldWidth;
    int fieldHeight;
    int chipsFallDelay;
    public int lowestEmptyRow;
    public int totalChipsToFallCount = 0;
    Queue<Dictionary<Vector2Int, Chip>> chipsToFall = new Queue<Dictionary<Vector2Int, Chip>>();

    public event Action OnCascadeComplete;


    public void Setup(GameSettings settings, GameField gf)
    {
        Settings = settings;
        gameField = gf;
    }

    public override void ApplyGameSettings()
    {
        chipsFallDelay = (int)(Settings.chipsFallDelay * 1000f);
    }

    public void Init()
    {
        fieldHeight = Settings.fieldHeight;
        fieldWidth = Settings.fieldWidth;
    }

    //public void BlockChipsToFall(HashSet<Chip> chipsToFall)
    //{
    //    foreach (Chip chip in chipsToFall)
    //    {
    //        chip.SetState(ChipState.Blocked);
    //    }
    //}

    public void StartCascade()
    {
        RunCascade();
    }

    void RunCascade()
    {
        lowestEmptyRow = gameField.GetLowestEmptyRow();
        if (lowestEmptyRow < 0)
            Debug.LogError("Cascade: lowest empty row is over board height.");
        CollectFallingQueue();
        CountChipsToFall();

        if (chipsToFall.Count <= 0)
            Debug.LogWarning("Chips to fall: 0");
        else
            CascadeChips();
    }

    public async void CascadeChips()
    {
        while (chipsToFall.Count > 0)
        {
            var chipsRow = chipsToFall.Dequeue();
            StartCoroutine(DropChips(chipsRow));

            await Task.Delay(chipsFallDelay);
        }
    }

    void CountChipsToFall()
    {
        totalChipsToFallCount = 0;
        foreach (var chipsRow in chipsToFall)
        {
            totalChipsToFallCount += chipsRow.Count;
        }
        Debug.Log("CountChipsToFall: totalChipsToFallCount = " + totalChipsToFallCount);
    }

    void CollectFallingQueue()
    {
        int i = 0;

        while (i < fieldHeight)
        {
            Dictionary<Vector2Int, Chip> chipsRowToFall = CollectChipsRowToFall();
            if (chipsRowToFall.Count == 0)
            {
                //Debug.Log("No more chips to fall were found.");
                break;
            }

            gameField.DropChipsToEmptyCells(chipsRowToFall);
            chipsToFall.Enqueue(chipsRowToFall);
            
            i++;
        }
    }

    // Collects chips, that should fall simultaneously, and their target cells
    Dictionary<Vector2Int, Chip> CollectChipsRowToFall()
    {
        Dictionary<Vector2Int, Chip> chipsRow = new Dictionary<Vector2Int, Chip>();

        for (int x = 0; x < fieldWidth; x++)
        {
            Vector2Int? bottomCell = null;  // first empty cell (from bottom) in current column

            for (int y = 0; y < lowestEmptyRow; y++)
            {
                Vector2Int currentCell = new Vector2Int(x, y);
                Chip currentChip = gameField.GetBoardChip(currentCell);

                if (currentChip is null)
                {
                    if (bottomCell is null)
                        bottomCell = currentCell;
                }
                else
                {
                    if (bottomCell.HasValue)
                    {
                        if (!currentChip.IsIdle() && !currentChip.HasState(ChipState.Blocked))
                            break;  // skip all chips, that shouldn't fall, and all above them

                        if (chipsRow.TryAdd(bottomCell.Value, currentChip))
                        {
                            currentChip.SetState(ChipState.Falling);
                        }
                        else
                        {
                            Debug.LogError($"CascadeHandler: Duplicate target cell {bottomCell.Value} while adding chip");
                        }

                        break;  // a chip to fall was found.
                                // Only 1 chip in each column should fall at a moment,
                                // so we interrupt the search in the column
                    }
                }
            }
        }

        return chipsRow;
    }

    IEnumerator DropChips(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall)
        {
            Vector2Int targetCell = entry.Key;
            Chip chip = entry.Value;

            chip.OnChipLanded += HandleChipLanded;
            //Debug.Log($"Cell at {chip.Cell} was subscibed to HandleChipLanded");

            Vector3 targetWorldPos = gameField.GetCellWorldPos(targetCell);
            chip.Fall(targetWorldPos);
        }
        yield return null;
    }

    public void HandleChipLanded(Chip chip)
    {
        chip.OnChipLanded -= HandleChipLanded;

        totalChipsToFallCount--;
        //Debug.Log($"Chip {chip.Cell} handled: totalChipsToFallCount = {totalChipsToFallCount}");

        if (totalChipsToFallCount < 0)
        {
            Debug.LogError($"CascadeHandler: totalChipsToFallCount (= {totalChipsToFallCount}) shouldn't be negative!");
            return;
        }

        if (totalChipsToFallCount == 0)
            HandleCascadeComplete();
    }

    //void HandleCascadeComplete()
    //{
    //    OnCascadeComplete?.Invoke();
    //}

    void HandleCascadeComplete()
    {
        //isCascadeRunning = false;
        //TryRunNextCascade();
        OnCascadeComplete?.Invoke();
    }
}
