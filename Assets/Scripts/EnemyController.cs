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

    // Facing direction (for vision cone)
    private Vector2Int facingDirection = Vector2Int.up;

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();

        // Generate patrol if needed - NEW
        if (useRandomPatrol && (patrolPath == null || patrolPath.Count == 0))
        {
            GeneratePatrolPath();
        }
        // Start at first waypoint
        if (patrolPath.Count > 0)
        {
            gridPosition = patrolPath[0];
            plannedPosition = gridPosition; // ADD THIS LINE
            transform.position = GridToWorld(gridPosition);
        }

        targetWorldPosition = transform.position;
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

    // NEW METHOD
    void GeneratePatrolPath()
    {
        // Use seed for deterministic generation
        Random.State oldState = Random.state;
        Random.InitState(patrolSeed);

        patrolPath = new List<Vector2Int>();

        // Start from enemy's current grid position
        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / tileSize),
            Mathf.RoundToInt(transform.position.y / tileSize)
        );

        // Generate 4-6 waypoints in a rough circle/rectangle
        int waypointCount = Random.Range(4, 7);
        float patrolRadius = Random.Range(2f, 4f);

        for (int i = 0; i < waypointCount; i++)
        {
            float angle = (i / (float)waypointCount) * Mathf.PI * 2;
            Vector2Int waypoint = start + new Vector2Int(
                Mathf.RoundToInt(Mathf.Cos(angle) * patrolRadius),
                Mathf.RoundToInt(Mathf.Sin(angle) * patrolRadius)
            );

            // Clamp to grid bounds - NEW
            waypoint.x = Mathf.Clamp(waypoint.x, 0, gridManager.gridWidth - 1);
            waypoint.y = Mathf.Clamp(waypoint.y, 0, gridManager.gridHeight - 1);

            // Make sure waypoint is valid
            if (gridManager != null && gridManager.IsWalkable(waypoint))
            {
                patrolPath.Add(waypoint);
            }
        }

        // If no valid waypoints, just stay in place
        if (patrolPath.Count == 0)
        {
            patrolPath.Add(start);
        }

        Random.state = oldState; // Restore random state

        Debug.Log($"Generated patrol with {patrolPath.Count} waypoints for enemy at {start}");
    }

    void PlanNextMove()
    {
        if (patrolPath.Count == 0)
        {
            plannedPosition = gridPosition; // Stay still if no path
            return;
        }

        // Get next waypoint
        Vector2Int targetWaypoint = patrolPath[currentWaypointIndex];

        // Are we at the waypoint?
        if (gridPosition == targetWaypoint)
        {
            // Move to next waypoint
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolPath.Count;
            targetWaypoint = patrolPath[currentWaypointIndex];
        }

        // Plan to move one step toward waypoint
        plannedPosition = MoveToward(gridPosition, targetWaypoint);

        // Update facing direction
        Vector2Int moveDirection = plannedPosition - gridPosition;
        if (moveDirection != Vector2Int.zero)
        {
            facingDirection = moveDirection;
        }
    }

    Vector2Int MoveToward(Vector2Int from, Vector2Int to)
    {
        // Simple pathfinding - move one step closer
        Vector2Int direction = to - from;

        // Move horizontally first, then vertically (simple but works)
        if (direction.x != 0)
        {
            return from + new Vector2Int((int)Mathf.Sign(direction.x), 0);
        }
        else if (direction.y != 0)
        {
            return from + new Vector2Int(0, (int)Mathf.Sign(direction.y));
        }

        return from; // Already at target
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

    public void CheckVisionAfterTurn()
    {
        Debug.Log("!!! CheckVisionAfterTurn WAS CALLED !!!");



        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player == null) return;

        Vector2Int playerPos = player.GetGridPosition();

        // DEBUG
        Debug.Log($"Checking vision: Enemy at {gridPosition} facing {facingDirection}, Player at {playerPos}");

        float distance = Vector2Int.Distance(gridPosition, playerPos);
        Debug.Log($"Distance: {distance}, Vision range: {visionRange}");

        if (distance > visionRange) return;

        Vector2 toPlayer = ((Vector2)(playerPos - gridPosition)).normalized;
        Vector2 facing = new Vector2(facingDirection.x, facingDirection.y).normalized;

        // Handle case where facing is zero
        if (facing.magnitude < 0.01f)
        {
            facing = Vector2.up; // Default facing if none set
        }

        float angle = Vector2.Angle(facing, toPlayer);
        Debug.Log($"Angle: {angle}, Max angle: {visionAngle / 2f}");

        if (angle <= visionAngle / 2f)
        {
            Debug.Log("PLAYER SPOTTED!");
            TriggerGameOver();
        }
    }

    public bool IsExecuting()
    {
        return isExecuting;
    }
}