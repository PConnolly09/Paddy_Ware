using UnityEngine;

public class PlayerGridMovementManager : MonoBehaviour
{
    public float tileSize = 1f;
    public float moveSpeed = 5f; // For smooth interpolation

    private Vector2Int gridPosition = Vector2Int.zero;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;

    void Start()
    {
        // Start at grid (0,0)
        gridPosition = Vector2Int.zero;
        transform.position = GridToWorld(gridPosition);
        targetWorldPosition = transform.position;
    }

    void Update()
    {
        HandleInput();
        MoveToTarget();
    }

    void HandleInput()
    {
        if (isMoving) return; // Can't input while moving

        Vector2Int inputDirection = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W)) inputDirection = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S)) inputDirection = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A)) inputDirection = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) inputDirection = Vector2Int.right;

        if (inputDirection != Vector2Int.zero)
        {
            TryMove(inputDirection);
        }
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int newGridPos = gridPosition + direction;

        // TODO: Check if new position is valid (not wall, in bounds)
        if (IsValidPosition(newGridPos))
        {
            gridPosition = newGridPos;
            targetWorldPosition = GridToWorld(gridPosition);
            isMoving = true;
        }
    }

    void MoveToTarget()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;
        }
    }

    bool IsValidPosition(Vector2Int pos)
    {
        // For now, just check bounds
        // Later: check collision with walls, enemies, etc
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0);
    }

}