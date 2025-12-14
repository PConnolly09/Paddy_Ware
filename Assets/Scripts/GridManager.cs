using UnityEngine;
using System.Collections.Generic;


public class GridManager : MonoBehaviour
{

    
    [Header ("Grid Settings")]
    public float gridWidth = 8f;
    public float gridHeight = 8f;
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    private GameObject[,] gridCells;
    private float cellSize = 1f;


    // Define which tiles are walls
    private HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupWalls();
        GenerateGrid();
    }

    void SetupWalls()
    {
        // Manually define some walls for testing
        // Make a simple corridor or room shape
        for (int i = 0; i < gridWidth; i++)

            for (int j = 0; j < gridHeight; j++)
            {
                // Example: create walls around the border
                //if (i == 0 || j == 0 || i == gridWidth - 1 || j == gridHeight - 1)
                //{
                //    wallPositions.Add(new Vector2Int(i, j));
                //}
                //else
                //{                     // Randomly place some internal walls for testing

                if (i != 0 && j != 0)
                {
                    if (Random.value < 0.2f)
                    {
                        wallPositions.Add(new Vector2Int(i, j));
                    }
                }
               // }
            }     
    }

    void GenerateGrid()
    {
        int cellsX = Mathf.CeilToInt(gridWidth / cellSize);
        int cellsY = Mathf.CeilToInt(gridHeight / cellSize);
        gridCells = new GameObject[cellsX, cellsY];
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize, 0);
                Vector2Int gridPos = new Vector2Int(x, y);

                // Spawn wall or floor based on data
                GameObject prefab = wallPositions.Contains(gridPos) ? wallTilePrefab : floorTilePrefab;
                Instantiate(prefab, cellPosition, Quaternion.identity, transform);
            }
        }

    }

            // PUBLIC METHOD for other scripts to check
    public bool IsWalkable(Vector2Int position)
    {
        return !wallPositions.Contains(position);
    }
}
