using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;


public class CollapseHandler : MonoBehaviour
{
    GameField gf;
    LevelGenerator levelGenerator;

    public float chipFallDuration = 0.4f;   // duration of falling chip animation
    public float chipFallGravity = 2;   // gravity for falling chip, that is falling speed factor
    const int ChipsFallDelay = 10;  // miliseconds to await before next set of chips falling

    // chips collapse in rows: one row after another.
    // This is total count of chips in all rows to collapse for current collapse to be completed
    public int totalChipsToFallCount = 0;

    public Action OnCollapseCompleted;


    public void Setup(GameField gf, LevelGenerator lg)
    {
        this.gf = gf;
        levelGenerator = lg;
    }

    public async void CollapseChips()
    {
        int iteration = 0;
        int maxIterations = gf.height * 10;    // protection from the infinite loop

        while (iteration < maxIterations)
        {
            if (iteration > gf.height)
            {
                Debug.LogWarning("CollapseHandler: Attempt to drop chips more, than level's height.");
            }

            Dictionary<Vector2Int, Chip> chipsToFall = CollectChipsToFall();
            if (chipsToFall.Count == 0)
            {
                //Debug.Log("No more chips to fall were found.");
                break;
            }

            gf.SyncFallingChipsWithBoard(chipsToFall);
            StartCoroutine(DropChips(chipsToFall));
            await Task.Delay(ChipsFallDelay);

            iteration++;
        }
    }

    // Collects chips, that should fall simultaneously, and their target cells
    Dictionary<Vector2Int, Chip> CollectChipsToFall()
    {
        Dictionary<Vector2Int, Chip> chipsToFall = new Dictionary<Vector2Int, Chip>();

        for (int x = 0; x < gf.width; x++)
        {
            Vector2Int? bottomCell = null;  // first cell (from bottom) without chip in current column

            for (int y = 0; y < gf.height; y++)
            {
                Chip currentChip = gf.GetChip(new Vector2Int(x, y));
                if (currentChip is null)
                {
                    if (bottomCell is null)
                    {
                        bottomCell = new Vector2Int(x, y);  // save bottom cell
                    }

                    if (y == gf.height - 1)
                    {
                        // spawn and save new chip outside (over) field
                        Chip newChip = levelGenerator.SpawnChip(new Vector2Int(x, gf.height));
                        if (!chipsToFall.TryAdd(bottomCell.Value, newChip))
                            Debug.LogError($"Duplicate target cell {bottomCell.Value} while adding new chip {newChip}");
                        break;
                    }
                }
                else if (bottomCell.HasValue)
                {
                    // save old chip to fall
                    if (!chipsToFall.TryAdd(bottomCell.Value, currentChip))
                        Debug.LogError($"Duplicate target cell {bottomCell.Value} while adding existing chip {currentChip}");
                    break;
                }
            }
        }

        return chipsToFall;
    }

    IEnumerator DropChips(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall)
        {
            Vector2Int targetCell = entry.Key;
            Chip chip = entry.Value;

            chip.OnChipLanded += HandleChipLanded;
            Vector3 targetPos = gf.GetCellWorldPos(entry.Key);
            chip.Fall(targetPos);
        }
        yield return null;
    }

    public void HandleChipLanded()
    {
        totalChipsToFallCount--;
        if (totalChipsToFallCount <= 0)
            HandleCollapseComplete();
    }

    void HandleCollapseComplete()
    {
        OnCollapseCompleted?.Invoke();
    }
}
