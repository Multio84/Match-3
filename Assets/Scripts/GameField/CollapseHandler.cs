using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;


public class CollapseHandler : MonoBehaviour
{
    GameField gf;
    LevelGenerator levelGenerator;
    MatchFinder matchFinder;
    ChipDestroyer chipDestroyer;

    public float chipFallDuration = 0.4f;   // duration of falling chip animation
    public float chipFallGravity = 2;   // gravity for falling chip, that is falling speed factor
    const int ChipsFallDelay = 10;  // miliseconds to await before next set of chips falling
    // chip collapse in rows: one row after another.
    // This is total count of chips of all rows to collapse until current collapse is done
    public int totalChipsToFallCount = 0;  


    public void Setup(GameField gf, LevelGenerator lg, MatchFinder mf, ChipDestroyer cd)
    {
        this.gf = gf;
        levelGenerator = lg;
        matchFinder = mf;
        chipDestroyer = cd;
    }

    public async void CollapseChips()
    {
        int iteration = 0;
        int maxIterations = gf.height * 10;    // protection from the infinite loop

        while (iteration < maxIterations)
        {
            Dictionary<Vector2Int, Chip> chipsToFall = CollectChipsToFall();
            if (chipsToFall.Count == 0)
            {
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

        for (int x = 0; x < gf.width; x++)
        {
            Vector2Int? bottomCell = null;  // first cell (from bottom) without chip in current column

            for (int y = 0; y < gf.height; y++)
            {
                if (gf.chips[x, y] is null)
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
                    if (!chipsToFall.TryAdd(bottomCell.Value, gf.chips[x, y]))
                        Debug.LogError($"Duplicate target cell {bottomCell.Value} while adding existing chip {gf.chips[x, y]}");
                    break;
                }
            }
        }

        return chipsToFall;
    }

    // changes chip's position in array: moves it from start chip's place to the cellPos
    void SyncFallingChipsWithBoard(Dictionary<Vector2Int, Chip> chipsToFall)
    {
        foreach (var entry in chipsToFall)
        {
            Vector2Int chipCell = entry.Value.CellPos;
            Vector2Int targetCell = entry.Key;

            if (!gf.IsCellInField(targetCell))
            {
                Debug.LogError("Sync field while collapsing: target cell for falling chip is outside the field.");
                break;
            }

            // chip created outside of the field is not in chips array yet,
            // so it doesn't have to be nullified
            if (gf.IsCellInField(chipCell))
                gf.chips[chipCell.x, chipCell.y] = null;

            gf.chips[targetCell.x, targetCell.y] = entry.Value;
            gf.chips[targetCell.x, targetCell.y].CellPos = targetCell;
        }
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
        if (matchFinder.FindMatches(null))
            chipDestroyer.ClearMatches();
    }
}
