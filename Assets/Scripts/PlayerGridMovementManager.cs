// PlayerGridMovement.cs - MAJOR REWRITE
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    public float tileSize = 1f;
    public float moveSpeed = 5f;
    public GameObject moveHighlightPrefab;

    private GridManager gridManager;
    private Vector2Int gridPosition = Vector2Int.zero;
    private Vector2Int plannedPosition = Vector2Int.zero; // NEW - where we plan to move
    private Vector3 targetWorldPosition;
    private bool isExecuting = false; // NEW - are we animating?
    private GameObject currentHighlight;

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        gridPosition = Vector2Int.zero;
        plannedPosition = gridPosition; // Start with no planned move
        transform.position = GridToWorld(gridPosition);
        targetWorldPosition = transform.position;

        if (moveHighlightPrefab != null)
        {
            currentHighlight = Instantiate(moveHighlightPrefab);
            currentHighlight.SetActive(false);
        }
    }

    void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlanning())
        {
            HandlePlanningInput();
            UpdateHighlight();
        }

        if (isExecuting)
        {
            ExecuteMovement();
        }
    }

    void HandlePlanningInput()
    {
        // During planning, arrow keys select where to move (don't move yet)
        Vector2Int inputDirection = Vector2Int.zero;

        if (Input.GetKey(KeyCode.W)) inputDirection = Vector2Int.up;
        else if (Input.GetKey(KeyCode.S)) inputDirection = Vector2Int.down;
        else if (Input.GetKey(KeyCode.A)) inputDirection = Vector2Int.left;
        else if (Input.GetKey(KeyCode.D)) inputDirection = Vector2Int.right;

        if (inputDirection != Vector2Int.zero)
        {
            plannedPosition = gridPosition + inputDirection;
        }
        else
        {
            // No input = plan to stay in place
            plannedPosition = gridPosition;
        }
    }

    void UpdateHighlight()
    {
        if (currentHighlight == null) return;

        // If planned position is different from current, show highlight
        if (plannedPosition != gridPosition)
        {
            bool isValid = IsValidPosition(plannedPosition);

            currentHighlight.SetActive(true);
            currentHighlight.transform.position = GridToWorld(plannedPosition);

            SpriteRenderer sr = currentHighlight.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = isValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            }
        }
        else
        {
            currentHighlight.SetActive(false);
        }
    }

    // Called by GameStateManager when SPACE is pressed
    public void ExecutePlannedMove()
    {
        if (IsValidPosition(plannedPosition))
        {
            // Valid move - execute it
            gridPosition = plannedPosition;
            targetWorldPosition = GridToWorld(gridPosition);
            isExecuting = true;

            // Hide highlight during execution
            if (currentHighlight != null)
            {
                currentHighlight.SetActive(false);
            }
        }
        else
        {
            // Invalid move - just stay in place
            Debug.Log("Invalid move, staying in place");
            OnMoveComplete();
        }
    }

    void ExecuteMovement()
    {
        // Smoothly move to target position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            transform.position = targetWorldPosition;
            OnMoveComplete();
        }
    }

    void OnMoveComplete()
    {
        isExecuting = false;

        // Tell game manager we're done
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnTurnExecutionComplete();
        }
    }

    bool IsValidPosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= 8 || pos.y < 0 || pos.y >= 8)
            return false;

        if (gridManager != null && !gridManager.IsWalkable(pos))
            return false;

        return true;
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0);
    }

    // PUBLIC - for other scripts to query
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public Vector2Int GetPlannedPosition()
    {
        return plannedPosition;
    }
}