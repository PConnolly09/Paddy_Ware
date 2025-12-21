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
    private Interactable targetInteractable = null; // What we're planning to interact with

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
            targetInteractable = null; // Clear interaction if moving
        }
        else
        {
            // No input = plan to stay in place
            plannedPosition = gridPosition;
        }

        // E to interact with adjacent object - NEW
        if (Input.GetKeyDown(KeyCode.E))
        {
            FindAndPlanInteraction();
        }

        // Cancel interaction - NEW
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            targetInteractable = null;
            Debug.Log("Interaction cancelled");
        }
    }

    // NEW METHOD
    void FindAndPlanInteraction()
    {
        // Find interactable objects adjacent to current position
        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        Interactable closest = null;
        float closestDist = float.MaxValue;

        foreach (Interactable obj in interactables)
        {
            if (obj.IsConsumed()) continue;

            float dist = Vector2Int.Distance(gridPosition, obj.GetGridPosition());

            if (dist <= 1.5f && dist < closestDist) // Adjacent (distance ~1)
            {
                closest = obj;
                closestDist = dist;
            }
        }

        if (closest != null)
        {
            targetInteractable = closest;
            Debug.Log($"Planning to interact with: {closest.GetInteractionPreview()}");
        }
        else
        {
            Debug.Log("No interactable objects nearby");
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

        TurnAction action = new TurnAction();
        action.startPosition = gridPosition;

        // If we have a target interaction, do that instead of moving
        if (targetInteractable != null && !targetInteractable.IsConsumed())
        {
            Debug.Log("Executing interaction");

            action.endPosition = gridPosition; // Don't move
            action.actionType = ActionType.Interact;
            action.interactionTarget = targetInteractable.interactableID;

            targetInteractable.Interact(this);
            targetInteractable = null;

            // Record the action - NEW
            if (RunRecorder.Instance != null)
            {
                RunRecorder.Instance.RecordTurn(action);
            }

            // Don't move, just complete turn
            OnMoveComplete();
            return;
        }

        if (IsValidPosition(plannedPosition))
        {
            // Valid move - execute it
            gridPosition = plannedPosition;
            targetWorldPosition = GridToWorld(gridPosition);
            isExecuting = true;

            action.endPosition = plannedPosition;
            action.actionType = ActionType.Move;

            if (RunRecorder.Instance != null)
            {
                RunRecorder.Instance.RecordTurn(action);
            }

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

            action.endPosition = gridPosition;
            action.actionType = ActionType.Wait;

            // Record the action - NEW
            if (RunRecorder.Instance != null)
            {
                RunRecorder.Instance.RecordTurn(action);
            }

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

    public bool IsExecuting()
    {
        return isExecuting;
    }
}