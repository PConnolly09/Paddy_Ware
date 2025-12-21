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
    // GridManager.cs - Add these fields at top
    public GameObject barrelPrefab; // Assign in inspector
    public GameObject exitPrefab;   // Assign in inspector

    void Start()
    {
        // Set fixed seed for deterministic generation - NEW
        Random.InitState(481516); // Any number, but keep it the same

        SetupWalls();
        GenerateGrid();
        SpawnBarrel();  // NEW
        SpawnExit();    // NEW
    }

    // NEW METHOD
    void SpawnBarrel()
    {
        if (barrelPrefab == null)
        {
            Debug.LogWarning("Barrel prefab not assigned to GridManager");
            return;
        }

        // Avoid spawning near player start (0, 0)
        Vector2Int barrelPos = GetRandomWalkablePosition(Vector2Int.zero, 3);

        if (barrelPos == new Vector2Int(-1, -1))
        {
            Debug.LogError("No walkable positions found for barrel!");
            return;
        }

        // Instantiate barrel
        GameObject barrel = Instantiate(barrelPrefab);
        ExplosiveBarrel barrelScript = barrel.GetComponent<ExplosiveBarrel>();

        if (barrelScript != null)
        {
            barrelScript.gridPosition = barrelPos;
            barrelScript.interactableID = $"barrel_{barrelPos.x}_{barrelPos.y}";
            barrel.transform.position = new Vector3(barrelPos.x * cellSize, barrelPos.y * cellSize, 0);

            Debug.Log($"Spawned barrel at {barrelPos}");
        }
    }

    // NEW METHOD
    void SpawnExit()
    {
        if (exitPrefab == null)
        {
            Debug.LogWarning("Exit prefab not assigned to GridManager");
            return;
        }

        Vector2Int exitPos = new Vector2Int(7, 7);

        // Make sure exit position is walkable
        if (!IsWalkable(exitPos))
        {
            Debug.LogWarning($"Exit position {exitPos} is blocked by wall, finding alternative");
            exitPos = GetRandomWalkablePosition();
        }

        // Instantiate exit
        GameObject exit = Instantiate(exitPrefab);
        ExitTile exitScript = exit.GetComponent<ExitTile>();

        if (exitScript != null)
        {
            exitScript.gridPosition = exitPos;
            exit.transform.position = new Vector3(exitPos.x * cellSize, exitPos.y * cellSize, 0);

            Debug.Log($"Spawned exit at {exitPos}");
        }
    }

    // NEW METHOD - Helper to find random walkable spot
    Vector2Int GetRandomWalkablePosition(Vector2Int avoidPosition = default, int minDistance = 2)
    {
        List<Vector2Int> walkablePositions = new List<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (IsWalkable(pos))
                {
                    // If avoiding a position, check distance
                    if (avoidPosition != default)
                    {
                        float dist = Vector2Int.Distance(pos, avoidPosition);
                        if (dist < minDistance) continue; // Too close, skip
                    }

                    walkablePositions.Add(pos);
                }
            }
        }

        if (walkablePositions.Count == 0)
        {
            return new Vector2Int(-1, -1);
        }

        return walkablePositions[Random.Range(0, walkablePositions.Count)];
    }

    // Make IsWalkable public if it isn't already
    public bool IsWalkable(Vector2Int position)
    {
        return !wallPositions.Contains(position);
    }

    void SetupWalls()
    {
        // Manually define some walls for testing
        // Make a simple corridor or room shape
        for (int i = 0; i < gridWidth; i++)

            for (int j = 0; j < gridHeight; j++)
            {

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

}
