using UnityEngine;
using System.Collections.Generic;


public class GridManager : MonoBehaviour
{


    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float tileSize = 1f;
    [Header("Level Configuration")]
    public int randomSeed = 12345; // Different seed per level
    public Vector2Int exitPosition = new Vector2Int(7, 7);
    public int barrelCount = 1;
    public int smokeBombCount = 0; // NEW

    [Header("Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject barrelPrefab;
    public GameObject smokeBombPrefab;
    public GameObject exitPrefab;

    private HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

    void Start()
    {
        // Set fixed seed for deterministic generation - NEW
        Random.InitState(481516); // Any number, but keep it the same

        SetupWalls();
        GenerateGrid();
        // Spawn multiple barrels - UPDATED
        for (int i = 0; i < barrelCount; i++)
        {
            SpawnBarrel(i);
        }

        // Spawn multiple smoke bombs - NEW
        for (int i = 0; i < smokeBombCount; i++)
        {
            SpawnSmokeBomb(i);
        }

        SpawnExit();    // NEW
    }

    // NEW METHOD
    void SpawnBarrel(int index) // Added index parameter
    {
        if (barrelPrefab == null) return;

        Vector2Int barrelPos = GetRandomWalkablePosition(Vector2Int.zero, 3);

        if (barrelPos == new Vector2Int(-1, -1))
        {
            Debug.LogError($"No walkable position for barrel {index}!");
            return;
        }

        GameObject barrel = Instantiate(barrelPrefab);
        ExplosiveBarrel barrelScript = barrel.GetComponent<ExplosiveBarrel>();

        if (barrelScript != null)
        {
            barrelScript.gridPosition = barrelPos;
            barrelScript.interactableID = $"barrel_{index}"; // Use index for unique ID
            barrel.transform.position = new Vector3(barrelPos.x * tileSize, barrelPos.y * tileSize, 0);

            Debug.Log($"Spawned barrel {index} at {barrelPos}");
        }
    }

    void SpawnSmokeBomb(int index) // Added index parameter
    {
        if (smokeBombPrefab == null) return;

        Vector2Int smokePos = GetRandomWalkablePosition(Vector2Int.zero, 3);

        if (smokePos == new Vector2Int(-1, -1))
        {
            Debug.LogError($"No walkable position for smoke bomb {index}!");
            return;
        }

        GameObject smoke = Instantiate(smokeBombPrefab);
        SmokeBomb smokeScript = smoke.GetComponent<SmokeBomb>();

        if (smokeScript != null)
        {
            smokeScript.gridPosition = smokePos;
            smokeScript.interactableID = $"smoke_{index}"; // Use index for unique ID
            smoke.transform.position = new Vector3(smokePos.x * tileSize, smokePos.y * tileSize, 0);

            Debug.Log($"Spawned smoke bomb {index} at {smokePos}");
        }
    }
    // NEW METHOD
    void SpawnExit()
    {
        if (exitPrefab == null) return;

        // Use configurable exit position - UPDATED
        if (!IsWalkable(exitPosition))
        {
            Debug.LogWarning($"Exit position {exitPosition} is blocked, finding alternative");
            exitPosition = GetRandomWalkablePosition();
        }

        GameObject exit = Instantiate(exitPrefab);
        ExitTile exitScript = exit.GetComponent<ExitTile>();

        if (exitScript != null)
        {
            exitScript.gridPosition = exitPosition;
            exit.transform.position = new Vector3(exitPosition.x * tileSize, exitPosition.y * tileSize, 0);

            Debug.Log($"Spawned exit at {exitPosition}");
        }
    }

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
                    if (avoidPosition != default)
                    {
                        float dist = Vector2Int.Distance(pos, avoidPosition);
                        if (dist < minDistance) continue;
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
        // You can make this more sophisticated later
        wallPositions.Add(new Vector2Int(3, 3));
        wallPositions.Add(new Vector2Int(3, 4));
        wallPositions.Add(new Vector2Int(3, 5));
        wallPositions.Add(new Vector2Int(5, 3));
        wallPositions.Add(new Vector2Int(5, 4));
        wallPositions.Add(new Vector2Int(5, 5));

        // Add more walls based on grid size
        if (gridWidth > 10 || gridHeight > 10)
        {
            // Bigger level = more walls
            for (int i = 0; i < 10; i++)
            {
                int x = Random.Range(1, gridWidth - 1);
                int y = Random.Range(1, gridHeight - 1);
                wallPositions.Add(new Vector2Int(x, y));
            }
        }
    }

    void GenerateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);
                Vector2Int gridPos = new Vector2Int(x, y);

                GameObject prefab = wallPositions.Contains(gridPos) ? wallTilePrefab : floorTilePrefab;
                GameObject tile = Instantiate(prefab, pos, Quaternion.identity, transform);

                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = wallPositions.Contains(gridPos) ? 1 : 0;
                }
            }
        }
    }

}
