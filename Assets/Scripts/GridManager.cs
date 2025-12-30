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
    public GameObject enemyPrefab; // Assign in inspector
    public int enemyCount = 1;

    [Header("Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject powderKegPrefab; // RENAMED from barrelPrefab
    public GameObject remoteBombPrefab; // NEW
    public GameObject smokeBombPrefab;
    public GameObject exitPrefab;
    public GameObject pressurePlatePrefab;
    public GameObject lockedDoorPrefab;
    public bool usePressurePlates = false; // Enable for puzzle levels

    private HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

    Vector2Int playerStart = Vector2Int.zero;

    void Start()
    {
        Random.InitState(randomSeed);

        SetupWalls();
        GenerateGrid();

        // Spawn enemies - NEW
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(i);
        }

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

        if (usePressurePlates)
        {
            SpawnPressurePlatePuzzle();
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

    void SpawnEnemy(int index)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab not assigned!");
            return;
        }

        // Find position at least 5 tiles from player start (0,0)
        Vector2Int enemyPos = Vector2Int.zero;
        int attempts = 0;

        while (attempts < 100)
        {
            int x = Random.Range(2, gridWidth - 2);
            int y = Random.Range(2, gridHeight - 2);
            enemyPos = new Vector2Int(x, y);

            // Check distance from player
            float distFromPlayer = Vector2Int.Distance(enemyPos, Vector2Int.zero);

            // Check if walkable and far enough
            if (IsWalkable(enemyPos) && distFromPlayer >= 5f)
            {
                break;
            }

            attempts++;
        }

        if (attempts >= 100)
        {
            Debug.LogError("Couldn't find valid enemy spawn position!");
            enemyPos = new Vector2Int(5, 5); // Fallback
        }

        GameObject enemy = Instantiate(enemyPrefab);
        enemy.transform.position = new Vector3(enemyPos.x * tileSize, enemyPos.y * tileSize, 0);

        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.enemyID = $"enemy_{index}";
            enemyController.patrolSeed = randomSeed + index;
        }

        Debug.Log($"Spawned enemy {index} at {enemyPos}, distance from player: {Vector2Int.Distance(enemyPos, Vector2Int.zero)}");
    }

    void SpawnPressurePlatePuzzle()
    {
        if (pressurePlatePrefab == null || lockedDoorPrefab == null)
        {
            Debug.LogWarning("Pressure plate prefabs not assigned!");
            return;
        }

        // Find positions for 2 plates and 1 door
        Vector2Int plate1Pos = GetRandomWalkablePosition(Vector2Int.zero, 3);
        Vector2Int plate2Pos = GetRandomWalkablePosition(plate1Pos, 3);

        // Door should be between plates and exit
        Vector2Int doorPos = new Vector2Int(
            (plate1Pos.x + plate2Pos.x) / 2,
            (plate1Pos.y + plate2Pos.y) / 2
        );

        // Make sure door position is valid (adjust if needed)
        if (!IsWalkable(doorPos))
        {
            doorPos = GetRandomWalkablePosition(plate1Pos, 2);
        }

        // Spawn plates
        GameObject plate1Obj = Instantiate(pressurePlatePrefab);
        PressurePlate plate1 = plate1Obj.GetComponent<PressurePlate>();
        plate1.gridPosition = plate1Pos;
        plate1Obj.transform.position = new Vector3(plate1Pos.x * tileSize, plate1Pos.y * tileSize, 0);

        GameObject plate2Obj = Instantiate(pressurePlatePrefab);
        PressurePlate plate2 = plate2Obj.GetComponent<PressurePlate>();
        plate2.gridPosition = plate2Pos;
        plate2Obj.transform.position = new Vector3(plate2Pos.x * tileSize, plate2Pos.y * tileSize, 0);

        // Link plates
        plate1.pairedPlate = plate2;
        plate2.pairedPlate = plate1;

        // Spawn door
        GameObject doorObj = Instantiate(lockedDoorPrefab);
        LockedDoor door = doorObj.GetComponent<LockedDoor>();
        door.gridPosition = doorPos;
        door.plateA = plate1;
        door.plateB = plate2;
        doorObj.transform.position = new Vector3(doorPos.x * tileSize, doorPos.y * tileSize, 0);

        // Add door to wall list so it blocks movement initially
        wallPositions.Add(doorPos);

        Debug.Log($"Spawned pressure plate puzzle: Plate1={plate1Pos}, Plate2={plate2Pos}, Door={doorPos}");
    }

    // Add method for door to call when opening:
    public void MakeWalkable(Vector2Int position)
    {
        if (wallPositions.Contains(position))
        {
            wallPositions.Remove(position);
            Debug.Log($"Position {position} is now walkable");
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

                    // Also check distance from player start (0,0)
                    float distFromPlayer = Vector2Int.Distance(pos, Vector2Int.zero);
                    if (distFromPlayer < 3) continue; // Keep enemies 3+ tiles from player start

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
