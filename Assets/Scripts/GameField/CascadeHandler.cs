using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;


public class CascadeHandler : SettingsSubscriber, IInitializer
{
    public override GameSettings Settings { get; set; }
    GameField gf;

    int fieldWidth;
    int fieldHeight;
    int chipsFallDelay;
    public int maxYWithChip;
    public int totalChipsToFallCount = 0;
    Queue<Dictionary<Vector2Int, Chip>> chipsToFall = new Queue<Dictionary<Vector2Int, Chip>>();

    public event Action OnCascadeCompleted;


    public void Setup(GameSettings settings, GameField gf)
    {
        Settings = settings;
        this.gf = gf;
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

    public void BlockChipsToFall()
    {
        //List<Chip> chipsToFall = gf.GetChipsAboveMatched();

    }

    public async void CascadeChips()
    {
        maxYWithChip = gf.GetHighestCellWithChip();
        CollectFallingQueue();
        CountChipsToFall();

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

            gf.SyncFallingChipsWithBoard(chipsRowToFall);
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

            for (int y = 0; y < maxYWithChip; y++)
            {
                Vector2Int currentCell = new Vector2Int(x, y);
                Chip currentChip = gf.GetBoardChip(currentCell);

                if (currentChip is null)
                {
                    if (bottomCell is null)
                        bottomCell = currentCell;
                }
                else if (bottomCell.HasValue)
                {
                    if (chipsRow.TryAdd(bottomCell.Value, currentChip))
                    {
                        currentChip.SetState(ChipState.Falling);
                    }
                    else
                    {
                        Debug.LogError($"CascadeHandler: Duplicate target cell {bottomCell.Value} while adding chip");
                    }
                    break;
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
            Debug.Log($"Cell at {chip.Cell} was subscibed to HandleChipLanded");

            Vector3 targetWorldPos = gf.GetCellWorldPos(targetCell);
            chip.Fall(targetWorldPos);
        }
        yield return null;
    }

    public void HandleChipLanded(Chip chip)
    {
        chip.OnChipLanded -= HandleChipLanded;

        totalChipsToFallCount--;
        Debug.Log($"Chip {chip.Cell} handled: totalChipsToFallCount = {totalChipsToFallCount}");

        if (totalChipsToFallCount < 0)
        {
            Debug.LogError($"CascadeHandler: totalChipsToFallCount (= {totalChipsToFallCount}) shouldn't be negative!");
            return;
        }

        if (totalChipsToFallCount == 0)
            HandleCascadeComplete();
    }

    //public void HandleChipLanded()
    //{
    //    totalChipsToFallCount--;
    //    Debug.Log($"totalChipsToFallCount = {totalChipsToFallCount}");

    //    if (totalChipsToFallCount < 0)
    //    {
    //        Debug.LogError($"CascadeHandler: totalChipsToFallCount (= {totalChipsToFallCount}) shouldn't be negative!");
    //        return;
    //    }

    //    if (totalChipsToFallCount == 0)
    //        HandleCascadeComplete();
    //}

    void HandleCascadeComplete()
    {
        OnCascadeCompleted?.Invoke();
    }
}
