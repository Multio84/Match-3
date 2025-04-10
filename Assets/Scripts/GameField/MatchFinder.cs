using System.Collections.Generic;
using UnityEngine;


public class MatchFinder : MonoBehaviour, IInitializer
{
    GameSettings settings;
    GameField gf;

    int fieldWidth;
    int fieldHeight;
    int minMatchSize;
    int maxMatchSize;

    // constant field bounds to search for mathes
    static Vector2Int fieldBottomLeft;
    static Vector2Int fieldTopRight;

    // current bounds region to search for mathes
    Vector2Int bottomLeft;
    Vector2Int topRight;

    HashSet<Chip> chipsToCheck;   // chips in line, that should be checked for match


    public void Setup(GameSettings gs, GameField gf)
    {
        this.gf = gf;
        settings = gs;
    }

    public void Init()
    {
        fieldWidth = settings.fieldWidth;
        fieldHeight = settings.fieldHeight;
        fieldBottomLeft = Vector2Int.zero;
        fieldTopRight = new Vector2Int(fieldWidth - 1, fieldHeight - 1);

        minMatchSize = settings.minMatchSize;
        maxMatchSize = settings.maxMatchSize;
        chipsToCheck = new HashSet<Chip>(maxMatchSize);
    }

    public bool FindMatches(SwapOperation operation)
    {
        if (operation is null) UseCachedFieldBounds();
        else SetSwapEffectZone(operation);

        bool matchesFound = false;
        matchesFound |= FindLineMatches(false);
        matchesFound |= FindLineMatches(true);

        return matchesFound;
    }

    void UseCachedFieldBounds()
    {
        bottomLeft = fieldBottomLeft;
        topRight = fieldTopRight;
    }

    // set search region for matches only around 2 swapped cells
    void SetSwapEffectZone(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.Cell;
        Vector2Int cell2 = operation.swappedChip.Cell;

        int searchDistance = minMatchSize - 1;    // distance (from swapped chips) in cells to search
        int minX = Mathf.Max(Mathf.Min(cell1.x, cell2.x) - searchDistance, 0);
        int minY = Mathf.Max(Mathf.Min(cell1.y, cell2.y) - searchDistance, 0);
        int maxX = Mathf.Min(Mathf.Max(cell1.x, cell2.x) + searchDistance, fieldWidth - 1);
        int maxY = Mathf.Min(Mathf.Max(cell1.y, cell2.y) + searchDistance, fieldHeight - 1);

        bottomLeft = new Vector2Int(minX, minY);
        topRight = new Vector2Int(maxX, maxY);
        //Debug.Log($"SwapSearchZone is {bottomLeft} : {topRight}");
    }

    // finds line matches inside the set region
    bool FindLineMatches(bool isVertical)
    {
        Vector2Int min = bottomLeft;
        Vector2Int max = topRight;
        Vector2Int direction;
        bool matchesFound = false;

        // for searching line matches in reduced region in each direction
        if (isVertical)
        {
            direction = Vector2Int.up;
            max.y = topRight.y + 1 - minMatchSize;
        }
        else
        {
            direction = Vector2Int.right;
            max.x = topRight.x + 1 - minMatchSize;
        }

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                // save the first chip without color check
                Chip currentChip = gf.GetFieldChip(new Vector2Int(x, y));
                if (!ChipCanBeMatched(currentChip))
                    continue;

                chipsToCheck.Clear();
                chipsToCheck.Add(currentChip);

                for (int i = 1; i < minMatchSize; i++)
                {
                    Vector2Int checkCell = new Vector2Int(
                        x + i * direction.x,
                        y + i * direction.y
                    );

                    Chip nextChip = gf.GetFieldChip(checkCell);
                    if (!ChipCanBeMatched(nextChip))
                        break;
                    if (currentChip.Color != nextChip.Color)
                        break;

                    chipsToCheck.Add(nextChip);
                }

                if (chipsToCheck.Count >= minMatchSize)
                {
                    foreach (Chip chip in chipsToCheck)
                    {
                        chip.IsMatched = true;
                    }
                    matchesFound = true;
                }
            }
        }

        return matchesFound;
    }
    
    bool ChipCanBeMatched(Chip chip)
    {
        if (chip is null)
            return false;

        if (!chip.IsIdle() && !chip.HasState(ChipState.Swapped))
            return false;

        return true;
    }
}
