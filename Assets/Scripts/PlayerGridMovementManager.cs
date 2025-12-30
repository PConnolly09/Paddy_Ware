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
    private Interactable targetInteractable = null;


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

        // E to interact instantly - NO TURN COST - NEW
        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractImmediately();
        }

    }

    // NEW METHOD
    // NEW METHOD - Instant interaction
    void InteractImmediately()
    {
        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        Interactable closest = null;
        float closestDist = float.MaxValue;

        foreach (Interactable obj in interactables)
        {
            if (obj.IsConsumed()) continue;

            float dist = Vector2Int.Distance(gridPosition, obj.GetGridPosition());

            if (dist <= 1.5f && dist < closestDist)
            {
                closest = obj;
                closestDist = dist;
            }
        }

        if (closest != null)
        {
            Debug.Log($"Instantly interacting with: {closest.GetInteractionPreview()}");
            closest.Interact(this);
        }
        else
        {
            Debug.Log("No interactable objects nearby");
        }
    }

    public void SetPosition(Vector2Int pos)
    {
        gridPosition = pos;
        plannedPosition = pos;
        transform.position = GridToWorld(pos);
        targetWorldPosition = transform.position;
        Debug.Log($"Player position set to {pos}");
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
        TurnAction action = new TurnAction();
        action.startPosition = gridPosition;
        action.turnNumber = TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0;

        // Handle interaction
        if (targetInteractable != null && !targetInteractable.IsConsumed())
        {
            action.endPosition = gridPosition;
            action.actionType = ActionType.Interact;
            action.interactionTarget = targetInteractable.interactableID;

            targetInteractable.Interact(this);
            targetInteractable = null;

            // Record to Timeline - UPDATED
            if (TimelineManager.Instance != null)
            {
                TimelineManager.Instance.RecordPlayerAction(action);
            }

            OnMoveComplete();
            return;
        }

        // Handle movement
        if (IsValidPosition(plannedPosition))
        {
            gridPosition = plannedPosition;
            targetWorldPosition = GridToWorld(gridPosition);
            isExecuting = true;

            action.endPosition = plannedPosition;
            action.actionType = ActionType.Move;

            // Record to Timeline - UPDATED
            if (TimelineManager.Instance != null)
            {
                TimelineManager.Instance.RecordPlayerAction(action);
            }

            if (currentHighlight != null)
            {
                currentHighlight.SetActive(false);
            }
        }
        else
        {
            action.endPosition = gridPosition;
            action.actionType = ActionType.Wait;

            // Record to Timeline - UPDATED
            if (TimelineManager.Instance != null)
            {
                TimelineManager.Instance.RecordPlayerAction(action);
            }

            OnMoveComplete();
        }
    }

    public void RestoreFromTimeline(Vector2Int position, int turn)
    {
        gridPosition = position;
        plannedPosition = position;
        transform.position = GridToWorld(position);
        targetWorldPosition = transform.position;

        Debug.Log($"Player restored to {position} at turn {turn}");
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

    }

    bool IsValidPosition(Vector2Int pos)
    {
        if (gridManager == null) return false;

        // Check bounds
        if (pos.x < 0 || pos.x >= gridManager.gridWidth ||
            pos.y < 0 || pos.y >= gridManager.gridHeight)
            return false;

        // Check walkability
        if (!gridManager.IsWalkable(pos))
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

    public bool IsExecuting()
    {
        return isExecuting;
    }
}