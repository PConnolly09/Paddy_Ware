using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    public float tileSize = 1f;
    public float moveSpeed = 5f;
    public GameObject moveHighlightPrefab; // NEW - Assign in inspector

    private GridManager gridManager;
    private Vector2Int gridPosition = Vector2Int.zero;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private GameObject currentHighlight; // NEW

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        gridPosition = Vector2Int.zero;
        transform.position = GridToWorld(gridPosition);
        targetWorldPosition = transform.position;

        // Create highlight object - NEW
        if (moveHighlightPrefab != null)
        {
            currentHighlight = Instantiate(moveHighlightPrefab);
            currentHighlight.SetActive(false);
        }
    }

    void Update()
    {
        UpdateHighlight(); // NEW
        HandleInput();
        MoveToTarget();
    }

    void UpdateHighlight() // NEW METHOD
    {
        if (isMoving || currentHighlight == null)
        {
            currentHighlight.SetActive(false);
            return;
        }

        // Check which direction is being held
        Vector2Int previewDirection = Vector2Int.zero;

        if (Input.GetKey(KeyCode.W)) previewDirection = Vector2Int.up;
        else if (Input.GetKey(KeyCode.S)) previewDirection = Vector2Int.down;
        else if (Input.GetKey(KeyCode.A)) previewDirection = Vector2Int.left;
        else if (Input.GetKey(KeyCode.D)) previewDirection = Vector2Int.right;

        if (previewDirection != Vector2Int.zero)
        {
            Vector2Int previewPos = gridPosition + previewDirection;
            bool isValid = IsValidPosition(previewPos);

            currentHighlight.SetActive(true);
            currentHighlight.transform.position = GridToWorld(previewPos);

            // Change color based on validity
            SpriteRenderer sr = currentHighlight.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = isValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f); // Green or Red
            }
        }
        else
        {
            currentHighlight.SetActive(false);
        }
    }

    void HandleInput()
    {
        if (isMoving) return;

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

        if (IsValidPosition(newGridPos))
        {
            gridPosition = newGridPos;
            targetWorldPosition = GridToWorld(gridPosition);
            isMoving = true;

            // Increment turn counter - NEW
            if (TurnCounter.Instance != null)
            {
                TurnCounter.Instance.IncrementTurn();
            }
        }
        else
        {
            // Optional: Visual/audio feedback for invalid move
            Debug.Log($"Can't move to {newGridPos}");
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
        // Check bounds
        if (pos.x < 0 || pos.x >= 8 || pos.y < 0 || pos.y >= 8)
            return false;

        // Check walkability (walls) - NEW
        if (gridManager != null && !gridManager.IsWalkable(pos))
            return false;

        return true;
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0);
    }
}