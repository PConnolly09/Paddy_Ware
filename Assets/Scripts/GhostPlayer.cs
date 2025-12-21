// GhostPlayer.cs - NEW SCRIPT
using UnityEngine;
using System.Collections.Generic;

public class GhostPlayer : MonoBehaviour
{
    public RunData runData;
    public float tileSize = 1f;
    public float moveSpeed = 5f;

    private int currentTurnIndex = 0;
    private Vector2Int gridPosition;
    private Vector3 targetWorldPosition;
    private bool isExecuting = false;

    void Start()
    {
        if (runData == null || runData.actions.Count == 0)
        {
            Debug.LogError("Ghost has no run data!");
            Destroy(gameObject);
            return;
        }

        // Start at first position
        gridPosition = runData.actions[0].startPosition;
        transform.position = GridToWorld(gridPosition);
        targetWorldPosition = transform.position;

        Debug.Log($"Ghost spawned for Run #{runData.runNumber} at {gridPosition}");
    }

    void Update()
    {
        if (isExecuting)
        {
            ExecuteMovement();
        }
    }

    // Called by GameStateManager when turn is committed
    public void ExecuteGhostTurn()
    {
        if (currentTurnIndex >= runData.actions.Count)
        {
            Debug.Log($"Ghost finished all {runData.actions.Count} actions");
            return; // Ghost finished its run
        }

        TurnAction action = runData.actions[currentTurnIndex];

        Debug.Log($"Ghost executing turn {currentTurnIndex}: {action.actionType}");

        switch (action.actionType)
        {
            case ActionType.Move:
            case ActionType.Wait:
                gridPosition = action.endPosition;
                targetWorldPosition = GridToWorld(gridPosition);
                isExecuting = true;
                break;

            case ActionType.Interact:
                // Ghost doesn't re-interact, resources already consumed
                // Just stay in place
                Debug.Log($"Ghost would interact with {action.interactionTarget} (already consumed)");
                gridPosition = action.endPosition;
                break;
        }

        currentTurnIndex++;
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

    public bool IsExecuting()
    {
        return isExecuting;
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
}   