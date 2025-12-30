// EnemyController.cs - NEW SCRIPT
using UnityEngine;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public float tileSize = 1f;
    public float moveSpeed = 5f;
    public int visionRange = 3; // How many tiles enemy can see
    public float visionAngle = 90f; // Cone angle in degrees

    private GridManager gridManager;
    private Vector2Int gridPosition;
    private Vector2Int plannedPosition;
    private Vector3 targetWorldPosition;
    private bool isExecuting = false;

    public bool useRandomPatrol = true;
    public int patrolSeed = 0; // Set different seeds for different enemies

    public string enemyID = "enemy_1"; // Set in inspector or generate from position

    // Facing direction (for vision cone)
    private Vector2Int facingDirection = Vector2Int.up;

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();

        if (string.IsNullOrEmpty(enemyID))
        {
            Vector2Int startPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / tileSize),
                Mathf.RoundToInt(transform.position.y / tileSize)
            );
            enemyID = $"enemy_{startPos.x}_{startPos.y}";
        }

        gridManager = FindAnyObjectByType<GridManager>();

        if (string.IsNullOrEmpty(enemyID))
        {
            Vector2Int startPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / tileSize),
                Mathf.RoundToInt(transform.position.y / tileSize)
            );
            enemyID = $"enemy_{startPos.x}_{startPos.y}";
        }

        // Start at spawn position
        gridPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / tileSize),
            Mathf.RoundToInt(transform.position.y / tileSize)
        );

        plannedPosition = gridPosition;
        targetWorldPosition = transform.position;

        Debug.Log($"Enemy {enemyID} initialized at {gridPosition}");
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


    void PlanNextMove()
    {
        // Get current turn to determine path
        int currentTurn = TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0;

        // Use turn number to get deterministic "random" position
        Vector2Int targetPosition = GetDeterministicPosition(currentTurn);

        Debug.Log($"Enemy {enemyID} turn {currentTurn}: at {gridPosition}, targeting {targetPosition}");

        // Move one step toward target
        plannedPosition = MoveToward(gridPosition, targetPosition);
        Debug.Log($"  Planned move: {gridPosition} ? {plannedPosition}");

        // Update facing
        Vector2Int moveDirection = plannedPosition - gridPosition;
        if (moveDirection != Vector2Int.zero)
        {
            facingDirection = moveDirection;
        }
    }

    Vector2Int GetDeterministicPosition(int turn)
    {
        // Change destination less frequently - every 15 turns
        int seed = enemyID.GetHashCode() + patrolSeed + (turn / 15);

        Random.State oldState = Random.state;
        Random.InitState(seed);

        Vector2Int destination = Vector2Int.zero;
        int attempts = 0;

        while (attempts < 20)
        {
            if (gridManager != null)
            {
                int x = Random.Range(1, gridManager.gridWidth - 1);
                int y = Random.Range(1, gridManager.gridHeight - 1);
                destination = new Vector2Int(x, y);

                // Make sure it's walkable AND not too close (force movement)
                float distFromCurrent = Vector2.Distance(destination, gridPosition);

                if (gridManager.IsWalkable(destination) && distFromCurrent > 3)
                {
                    break;
                }
            }
            attempts++;
        }

        Random.state = oldState;

        return destination;
    }

    Vector2Int MoveToward(Vector2Int from, Vector2Int to)
    {
        // Simple A* pathfinding
        List<Vector2Int> path = FindPath(from, to);

        if (path != null && path.Count > 1)
        {
            // Return next step on path
            return path[1]; // path[0] is current position
        }

        // Can't find path, stay put
        return from;
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (gridManager == null) return null;

        // Simple breadth-first search (good enough for small grids)
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        frontier.Enqueue(start);
        cameFrom[start] = start;

        int maxIterations = 200;
        int iterations = 0;

        while (frontier.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector2Int current = frontier.Dequeue();

            if (current == goal)
            {
                // Found path, reconstruct it
                return ReconstructPath(cameFrom, start, goal);
            }

            // Check all 4 neighbors
            Vector2Int[] neighbors = new Vector2Int[]
            {
            current + Vector2Int.up,
            current + Vector2Int.down,
            current + Vector2Int.left,
            current + Vector2Int.right
            };

            foreach (Vector2Int next in neighbors)
            {
                // Check if valid and not visited
                if (!cameFrom.ContainsKey(next) && IsPositionWalkable(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        // No path found
        Debug.Log($"Enemy {enemyID}: No path from {start} to {goal}");
        return null;
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = goal;

        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start);
        path.Reverse();

        return path;
    }

    bool IsPositionWalkable(Vector2Int pos)
    {
        // Check bounds
        if (gridManager != null)
        {
            if (pos.x < 0 || pos.x >= gridManager.gridWidth ||
                pos.y < 0 || pos.y >= gridManager.gridHeight)
            {
                return false;
            }

            if (!gridManager.IsWalkable(pos))
            {
                return false;
            }
        }

        // Don't walk through ghosts
        GhostPlayer[] ghosts = FindObjectsByType<GhostPlayer>(FindObjectsSortMode.None);
        foreach (GhostPlayer ghost in ghosts)
        {
            if (ghost.GetGridPosition() == pos)
            {
                return false;
            }
        }

        return true;
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

    public void ExecutePlannedMove()
    {
        Debug.Log($"Enemy {enemyID}: {gridPosition} > {plannedPosition}");

        if (plannedPosition != gridPosition)
        {
            gridPosition = plannedPosition;
            targetWorldPosition = GridToWorld(gridPosition);
            isExecuting = true;
        }
        else
        {
            isExecuting = false;
        }
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