using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GameField : MonoBehaviour
{
    public Grid grid;
    [SerializeField, Range(3, 8)] public int width = 8;
    [SerializeField, Range(3, 8)] public int height = 8;
    public GameObject cellPrefab;
    public GameObject[] chipsPrefabs;
    private GameObject[,] chips;



    void Start()
    {
        SetGameFieldPos();
        chips = new GameObject[width, height];
        GenerateGameField();
    }

    void SetGameFieldPos()
    {
        var startGameFieldPos = transform.position;
        Vector2 cellSize = grid.cellSize;
        Vector3 newPos = new Vector3();
        newPos.x = (startGameFieldPos.x - cellSize.x * width) / 2 + cellSize.x / 2;
        newPos.y = (startGameFieldPos.y - cellSize.y * height) / 2 + cellSize.y / 2;
        transform.position = newPos;
    }

    void GenerateGameField()
    {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                SpawnCell(cellPos);
                SpawnChip(cellPos);
            }
        }
    }

    void SpawnCell(Vector3Int cellPos)
    {
        GameObject cell = Instantiate(cellPrefab, grid.CellToWorld(cellPos), Quaternion.identity);
        cell.transform.SetParent(transform);
    }

    void SpawnChip(Vector3Int cellPos)
    {
        int randomIndex = Random.Range(0, chipsPrefabs.Length);
        GameObject chip = Instantiate(chipsPrefabs[randomIndex], grid.CellToWorld(cellPos), Quaternion.identity);
        chip.transform.SetParent(transform);
        chips[cellPos.x, cellPos.y] = chip;
    }

}