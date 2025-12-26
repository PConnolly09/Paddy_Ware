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
    public int powderKegCount = 1; // RENAMED from barrelCount
    public int remoteBombCount = 0; // NEW
    public int smokeBombCount = 0;

    [Header("Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject powderKegPrefab; // RENAMED from barrelPrefab
    public GameObject remoteBombPrefab; // NEW
    public GameObject smokeBombPrefab;
    public GameObject exitPrefab;

    private HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

    void Start()
    {
        Random.InitState(randomSeed);

        SetupWalls();
        GenerateGrid();

        // Spawn everything FIRST
        for (int i = 0; i < powderKegCount; i++)
        {
            SpawnPowderKeg(i);
        }

        for (int i = 0; i < remoteBombCount; i++)
        {
            SpawnRemoteBomb(i);
        }

        for (int i = 0; i < smokeBombCount; i++)
        {
            SpawnSmokeBomb(i);
        }

        SpawnExit();

        // THEN disable consumed ones - ADD DEBUG
        DisableConsumedResources();
    }

    void DisableConsumedResources()
    {
        if (RunRecorder.Instance == null)
        {
            Debug.Log("No RunRecorder, first run - all resources available");
            return;
        }

        List<string> consumedIDs = RunRecorder.Instance.GetAllConsumedResources();

        Debug.Log($"=== DISABLING CONSUMED RESOURCES ===");
        Debug.Log($"Consumed count: {consumedIDs.Count}");
        foreach (string id in consumedIDs)
        {
            Debug.Log($"  - {id}");
        }

        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        Debug.Log($"Found {interactables.Length} total interactables in scene");

        foreach (Interactable obj in interactables)
        {
            Debug.Log($"Checking: {obj.interactableID}");

            if (consumedIDs.Contains(obj.interactableID))
            {
                Debug.Log($"*** CONSUMING: {obj.interactableID} ***");
                obj.Consume();
            }
        }
    }

    // NEW METHOD
    void SpawnPowderKeg(int index)
    {
        if (powderKegPrefab == null) return;

        Vector2Int pos = GetRandomWalkablePosition(Vector2Int.zero, 3);

        if (pos == new Vector2Int(-1, -1))
        {
            Debug.LogError($"No walkable position for powder keg {index}!");
            return;
        }

        GameObject keg = Instantiate(powderKegPrefab);
        PowderKeg kegScript = keg.GetComponent<PowderKeg>();

        if (kegScript != null)
        {
            kegScript.gridPosition = pos;
            kegScript.interactableID = $"keg_{index}";
            keg.transform.position = new Vector3(pos.x * tileSize, pos.y * tileSize, 0);

            Debug.Log($"Spawned powder keg {index} at {pos}");
        }
    }

    void SpawnRemoteBomb(int index)
    {
        if (remoteBombPrefab == null) return;

        Vector2Int pos = GetRandomWalkablePosition(Vector2Int.zero, 3);

        if (pos == new Vector2Int(-1, -1))
        {
            Debug.LogError($"No walkable position for remote bomb {index}!");
            return;
        }

        GameObject bomb = Instantiate(remoteBombPrefab);
        RemoteBomb bombScript = bomb.GetComponent<RemoteBomb>();

        if (bombScript != null)
        {
            bombScript.gridPosition = pos;
            bombScript.interactableID = $"bomb_{pos.x}_{pos.y}"; // IMPORTANT: Use position-based ID
            bomb.transform.position = new Vector3(pos.x * tileSize, pos.y * tileSize, 0);

            Debug.Log($"Spawned remote bomb {index} at {pos}");
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
        // Clear any existing walls
        wallPositions.Clear();


        // Add some interior walls for Level 1 (8x8)
        if (gridWidth == 8 && gridHeight == 8)
        {
            wallPositions.Add(new Vector2Int(3, 3));
            wallPositions.Add(new Vector2Int(3, 4));
            wallPositions.Add(new Vector2Int(3, 5));
            wallPositions.Add(new Vector2Int(5, 3));
            wallPositions.Add(new Vector2Int(5, 4));
            wallPositions.Add(new Vector2Int(5, 5));
        }

        // Add more interior walls for Level 2 (12x12)
        if (gridWidth == 12 && gridHeight == 12)
        {
            // Create some corridors/obstacles
            for (int i = 3; i < 9; i++)
            {
                wallPositions.Add(new Vector2Int(i, 6));
            }

            for (int i = 2; i < 8; i++)
            {
                wallPositions.Add(new Vector2Int(6, i));
            }

            // Add random walls
            for (int i = 0; i < 15; i++)
            {
                int x = Random.Range(2, gridWidth - 2);
                int y = Random.Range(2, gridHeight - 2);
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
