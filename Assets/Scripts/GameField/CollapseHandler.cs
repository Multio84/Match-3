using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;


public class CollapseHandler : SettingsSubscriber, IPreloader
{
    public override GameSettings Settings { get; set; }
    GameField gf;
    LevelGenerator levelGenerator;

    int fieldWidth;
    int fieldHeight;
    int chipsFallDelay;
    public int totalChipsToFallCount = 0;

    public event Action OnCollapseCompleted;


    public void Setup(GameSettings settings, GameField gf, LevelGenerator lg)
    {
        Settings = settings;
        this.gf = gf;
        levelGenerator = lg;
    }

    public override void ApplyGameSettings()
    {
        chipsFallDelay = (int)(Settings.chipsFallDelay * 1000f);
    }

    public void Preload()
    {
        fieldHeight = Settings.height;
        fieldWidth = Settings.width;
    }

    public async void CollapseChips()
    {
        int iteration = 0;
        int maxIterations = fieldHeight * 10;    // protection from the infinite loop

        while (iteration < maxIterations)
        {
            if (iteration > fieldHeight)
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
            await Task.Delay(chipsFallDelay);

            iteration++;
        }
    }

    // Collects chips, that should fall simultaneously, and their target cells
    Dictionary<Vector2Int, Chip> CollectChipsToFall()
    {
        Dictionary<Vector2Int, Chip> chipsToFall = new Dictionary<Vector2Int, Chip>();

        for (int x = 0; x < fieldWidth; x++)
        {
            Vector2Int? bottomCell = null;  // first cell (from bottom) without chip in current column

            for (int y = 0; y < fieldHeight; y++)
            {
                Chip currentChip = gf.GetChip(new Vector2Int(x, y));
                if (currentChip is null)
                {
                    if (bottomCell is null)
                    {
                        bottomCell = new Vector2Int(x, y);  // save bottom cell
                    }

                    if (y == fieldHeight - 1)
                    {
                        // spawn and save new chip outside (over) field
                        Chip newChip = levelGenerator.SpawnChip(new Vector2Int(x, fieldHeight));
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
            Vector3 targetWorldPos = gf.GetCellWorldPos(targetCell);
            chip.Fall(targetWorldPos);
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
