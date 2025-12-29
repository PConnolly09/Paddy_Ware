// EnemyController.cs - NEW SCRIPT
using UnityEngine;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public float tileSize = 1f;
    public float moveSpeed = 5f;
    public List<Vector2Int> patrolPath = new List<Vector2Int>(); // Set in inspector
    public int visionRange = 3; // How many tiles enemy can see
    public float visionAngle = 90f; // Cone angle in degrees

    private GridManager gridManager;
    private int currentWaypointIndex = 0;
    private Vector2Int gridPosition;
    private Vector2Int plannedPosition;
    private Vector3 targetWorldPosition;
    private bool isExecuting = false;

    public bool useRandomPatrol = true;
    public int patrolSeed = 0; // Set different seeds for different enemies

    public string enemyID = "enemy_1"; // Set in inspector or generate from position
    private List<Vector2Int> recordedWaypoints = new List<Vector2Int>();
    private int recordedWaypointIndex = 0;
    private bool useRecordedPath = false;

    // Facing direction (for vision cone)
    private Vector2Int facingDirection = Vector2Int.up;

    void Start()
    {

        Debug.Log($"=== Enemy {enemyID} Start ===");

        gridManager = FindAnyObjectByType<GridManager>();

        // Generate unique ID if not set
        if (string.IsNullOrEmpty(enemyID))
        {
            Vector2Int startPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / tileSize),
                Mathf.RoundToInt(transform.position.y / tileSize)
            );
            enemyID = $"enemy_{startPos.x}_{startPos.y}";
        }
        Debug.Log($"Enemy ID: {enemyID}");

        // Check if we have a recorded path from previous run
        if (PathRecorder.Instance != null && PathRecorder.Instance.HasRecordedPath(enemyID))
        {
            recordedWaypoints = PathRecorder.Instance.GetRecordedPath(enemyID);
            useRecordedPath = true;
            Debug.Log($"Enemy {enemyID} using recorded path with {recordedWaypoints.Count} waypoints");
        }
        else
        {
            // First run - generate random and record
            Debug.Log("No recorded path, will generate new one");
            if (PathRecorder.Instance != null)
            {
                PathRecorder.Instance.StartRecording();
            }
            GeneratePatrolPath();
            useRecordedPath = false;
            Debug.Log($"Enemy {enemyID} generating new random path");
        }
        // Generate patrol if needed - NEW
        if (useRandomPatrol && (patrolPath == null || patrolPath.Count == 0))
        {
            GeneratePatrolPath();
        }

        Vector2Int initialPos;

        if (patrolPath.Count > 0)
        {
            initialPos = patrolPath[0];
        }
        else
        {
            initialPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / tileSize),
                Mathf.RoundToInt(transform.position.y / tileSize)
            );
        }

        // Make sure not spawning on player start (0,0)
        if (Vector2Int.Distance(initialPos, Vector2Int.zero) < 3)
        {
            Debug.LogWarning($"Enemy too close to player start, adjusting...");
            initialPos = new Vector2Int(5, 5); // Safe starting position
        }

        gridPosition = initialPos;
        plannedPosition = gridPosition;
        transform.position = GridToWorld(gridPosition);

        targetWorldPosition = transform.position;
        Debug.Log($"Enemy {enemyID} initialized at {gridPosition}, useRecordedPath={useRecordedPath}");
    }

    void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlanning())
        {
            PlanNextMove();
        }

        if (isExecuting)
        {
            ExecuteMovement();
        }
    }

    void GeneratePatrolPath()
    {
        Random.State oldState = Random.state;
        Random.InitState(patrolSeed);

        patrolPath = new List<Vector2Int>();

        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / tileSize),
            Mathf.RoundToInt(transform.position.y / tileSize)
        );

        int waypointCount = Random.Range(4, 7);
        float patrolRadius = Random.Range(2f, 4f);

        int attempts = 0;
        while (patrolPath.Count < waypointCount && attempts < 50)
        {
            float angle = Random.value * Mathf.PI * 2;
            Vector2Int waypoint = start + new Vector2Int(
                Mathf.RoundToInt(Mathf.Cos(angle) * patrolRadius),
                Mathf.RoundToInt(Mathf.Sin(angle) * patrolRadius)
            );

            // Clamp to grid bounds
            if (gridManager != null)
            {
                waypoint.x = Mathf.Clamp(waypoint.x, 0, gridManager.gridWidth - 1);
                waypoint.y = Mathf.Clamp(waypoint.y, 0, gridManager.gridHeight - 1);

                // Only add if walkable
                if (gridManager.IsWalkable(waypoint))
                {
                    patrolPath.Add(waypoint);
                }
            }

            attempts++;
        }

        if (patrolPath.Count == 0)
        {
            patrolPath.Add(start);
        }

        Random.state = oldState;

        Debug.Log($"Generated patrol with {patrolPath.Count} valid waypoints");
    }

    void PlanNextMove()
    {
        Debug.Log($"Enemy {enemyID} PlanNextMove - useRecordedPath={useRecordedPath}, at {gridPosition}");

        if (useRecordedPath)
        {
            Debug.Log($"Using recorded path, index {recordedWaypointIndex}/{recordedWaypoints.Count}");
            // Use recorded path
            if (recordedWaypointIndex >= recordedWaypoints.Count)
            {
                // Finished recorded path, loop it
                recordedWaypointIndex = 0;
                Debug.Log("Looping recorded path");
            }

            Vector2Int targetWaypoint = recordedWaypoints[recordedWaypointIndex];
            Debug.Log($"Target waypoint: {targetWaypoint}");

            if (gridPosition == targetWaypoint)
            {
                recordedWaypointIndex++;
                Debug.Log($"Reached waypoint, moving to next (index now {recordedWaypointIndex})");
                if (recordedWaypointIndex < recordedWaypoints.Count)
                {
                    targetWaypoint = recordedWaypoints[recordedWaypointIndex];
                }
            }

            plannedPosition = MoveToward(gridPosition, targetWaypoint);
            Debug.Log($"Planned position: {plannedPosition}");
        }
        else
        {
            // Generate new random path during first run
            if (patrolPath.Count == 0)
            {
                GenerateNewRandomDestination();
            }

            Vector2Int targetWaypoint = patrolPath[currentWaypointIndex];

            if (gridPosition == targetWaypoint)
            {
                // Reached waypoint, record it
                if (PathRecorder.Instance != null)
                {
                    PathRecorder.Instance.RecordWaypoint(enemyID, targetWaypoint);
                }

                // Generate next destination
                currentWaypointIndex++;
                if (currentWaypointIndex >= patrolPath.Count)
                {
                    GenerateNewRandomDestination();
                    currentWaypointIndex = 0;
                }

                targetWaypoint = patrolPath[currentWaypointIndex];
            }

            plannedPosition = MoveToward(gridPosition, targetWaypoint);
        }

        // Update facing
        Vector2Int moveDirection = plannedPosition - gridPosition;
        if (moveDirection != Vector2Int.zero)
        {
            facingDirection = moveDirection;
        }
    }

    void GenerateNewRandomDestination()
    {
        patrolPath.Clear();

        // Use enemy-specific seed based on how many waypoints we've generated
        int seed = patrolSeed + recordedWaypoints.Count;
        Random.State oldState = Random.state;
        Random.InitState(seed);

        // Pick a random walkable destination
        if (gridManager != null)
        {
            int attempts = 0;
            while (patrolPath.Count == 0 && attempts < 20)
            {
                int x = Random.Range(1, gridManager.gridWidth - 1);
                int y = Random.Range(1, gridManager.gridHeight - 1);
                Vector2Int destination = new Vector2Int(x, y);

                if (gridManager.IsWalkable(destination))
                {
                    patrolPath.Add(destination);
                    Debug.Log($"Enemy {enemyID} new destination: {destination}");
                }

                attempts++;
            }
        }

        if (patrolPath.Count == 0)
        {
            patrolPath.Add(gridPosition); // Stay in place if can't find destination
        }

        Random.state = oldState;
    }

    Vector2Int MoveToward(Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = to - from;
        Vector2Int nextStep;

        // Try moving horizontally first
        if (direction.x != 0)
        {
            nextStep = from + new Vector2Int((int)Mathf.Sign(direction.x), 0);
            if (gridManager != null && gridManager.IsWalkable(nextStep))
            {
                return nextStep;
            }
        }

        // Try moving vertically
        if (direction.y != 0)
        {
            nextStep = from + new Vector2Int(0, (int)Mathf.Sign(direction.y));
            if (gridManager != null && gridManager.IsWalkable(nextStep))
            {
                return nextStep;
            }
        }

        // Can't move closer, stay put
        Debug.Log($"Enemy at {from} blocked, can't reach {to}");
        return from;
    }

    void TriggerGameOver()
    {
        Debug.Log("SPOTTED! Game Over");

        // TODO: Proper game over screen
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameOver();
        }
    }

    public Vector2Int GetFacingDirection()
    {
        return facingDirection;
    }

    // Called by GameStateManager when turn commits
    public void ExecutePlannedMove()
    {
        gridPosition = plannedPosition;
        targetWorldPosition = GridToWorld(gridPosition);
        isExecuting = true;
    }

    void ExecuteMovement()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            isExecuting = false;

        }
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0);
    }

    // Visualize vision cone in editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 worldPos = GridToWorld(gridPosition);

        // Get facing as Vector3
        Vector3 facing = new Vector3(facingDirection.x, facingDirection.y, 0);

        // Handle zero facing
        if (facing.magnitude < 0.01f)
        {
            facing = Vector3.up;
        }

        facing = facing.normalized * visionRange * tileSize;

        // Draw center line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(worldPos, worldPos + facing);

        // Draw cone edges
        Gizmos.color = new Color(1, 0, 0, 0.5f);

        // Left edge
        Vector3 leftEdge = Quaternion.Euler(0, 0, visionAngle / 2) * facing;
        Gizmos.DrawLine(worldPos, worldPos + leftEdge);

        // Right edge  
        Vector3 rightEdge = Quaternion.Euler(0, 0, -visionAngle / 2) * facing;
        Gizmos.DrawLine(worldPos, worldPos + rightEdge);

        // Draw arc (multiple segments for better visualization)
        int segments = 10;
        Vector3 previousPoint = worldPos + rightEdge;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -visionAngle / 2 + (visionAngle / segments) * i;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * facing;
            Vector3 point = worldPos + direction;

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }

    // PUBLIC - for other scripts
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    // In EnemyController.cs, update CheckVisionAfterTurn:
    public void CheckVisionAfterTurn()
    {
        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player == null) return;

        Vector2Int playerPos = player.GetGridPosition();

        float distance = Vector2Int.Distance(gridPosition, playerPos);

        if (distance > visionRange) return;

        // Check line of sight - NEW
        if (!HasLineOfSight(playerPos))
        {
            Debug.Log("Player blocked by wall, no detection");
            return;
        }

        Vector2 toPlayer = ((Vector2)(playerPos - gridPosition)).normalized;
        Vector2 facing = new Vector2(facingDirection.x, facingDirection.y).normalized;

        if (facing.magnitude < 0.01f)
        {
            facing = Vector2.up;
        }

        float angle = Vector2.Angle(facing, toPlayer);

        if (angle <= visionAngle / 2f)
        {
            Debug.Log("PLAYER SPOTTED!");
            TriggerGameOver();
        }
    }

    // NEW METHOD - Check if path to target is clear
    bool HasLineOfSight(Vector2Int target)
    {
        if (gridManager == null) return false;

        // Bresenham's line algorithm to check each tile between enemy and target
        int x0 = gridPosition.x;
        int y0 = gridPosition.y;
        int x1 = target.x;
        int y1 = target.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Check if this tile blocks vision (is a wall)
            Vector2Int checkPos = new Vector2Int(x0, y0);

            if (!gridManager.IsWalkable(checkPos))
            {
                Debug.Log($"Wall at {checkPos} blocks line of sight");
                return false; // Wall blocks vision
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return true; // Clear line of sight
    }

    public bool IsExecuting()
    {
        return isExecuting;
    }
}