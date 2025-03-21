using System.Collections.Generic;
using UnityEngine;


public class MatchManager : MonoBehaviour
{
    GameField gameField;

    static int fieldWidth;
    static int fieldHeight;

    // field bounds
    static Vector2Int fieldBottomLeft;
    static Vector2Int fieldTopRight;

    // current region bounds
    Vector2Int bottomLeft;
    Vector2Int topRight;

    const int MinMatchSize = 3;    // number of cells, minimum for match in line, except the first chip
    const int MaxMatchSize = 5;    // number of cells, maximum for match in line, except the first chip

    HashSet<Chip> chipsToCheck = new HashSet<Chip>(MaxMatchSize);   // chips in line, that should be checked for match


    void Awake()
    {
        //if (Instance == null)
        //{
        //    Instance = this;
        //    DontDestroyOnLoad(gameObject);
        //}
        //else if (Instance != this)
        //{
        //    Destroy(gameObject);
        //}

        //Initialize();
    }
    public void Setup(GameField gf)
    {
        gameField = gf;
        Initialize();
    }

    void Initialize()
    {
        fieldWidth = gameField.width;
        fieldHeight = gameField.height;
        fieldBottomLeft = Vector2Int.zero;
        fieldTopRight = new Vector2Int(fieldWidth - 1, fieldHeight - 1);
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

    // region to search matches around 2 swapped cells
    void SetSwapEffectZone(SwapOperation operation)
    {
        Vector2Int cell1 = operation.draggedChip.CellPos;
        Vector2Int cell2 = operation.swappedChip.CellPos;

        int searchDistance = MinMatchSize - 1;    // distance (from swapped chips) in cells to search
        int minX = Mathf.Max(Mathf.Min(cell1.x, cell2.x) - searchDistance, 0);
        int minY = Mathf.Max(Mathf.Min(cell1.y, cell2.y) - searchDistance, 0);
        int maxX = Mathf.Min(Mathf.Max(cell1.x, cell2.x) + searchDistance, fieldWidth - 1);
        int maxY = Mathf.Min(Mathf.Max(cell1.y, cell2.y) + searchDistance, fieldHeight - 1);

        bottomLeft = new Vector2Int(minX, minY);
        topRight = new Vector2Int(maxX, maxY);
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
            max.y = topRight.y + 1 - MinMatchSize;
        }
        else
        {
            direction = Vector2Int.right;
            max.x = topRight.x + 1 - MinMatchSize;
        }

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                // TODO: when all common algorythm is done, check if it's really needed:
                if (!gameField.IsValidChip(x, y)) continue;

                // save the first chip without check
                Chip currentChip = gameField.chips[x, y];
                chipsToCheck.Clear();
                chipsToCheck.Add(currentChip);

                for (int i = 1; i < MinMatchSize; i++)
                {
                    int checkX = x + i * direction.x;
                    int checkY = y + i * direction.y;

                    if (!gameField.IsValidChip(checkX, checkY)) break;
                    Chip nextChip = gameField.chips[checkX, checkY];

                    if (currentChip.Color != nextChip.Color) break;
                    chipsToCheck.Add(nextChip);
                }

                if (chipsToCheck.Count >= MinMatchSize)
                {
                    foreach (Chip chip in chipsToCheck) chip.IsMatched = true;
                    matchesFound = true;
                }
            }
        }

        return matchesFound;
    }
    
}
