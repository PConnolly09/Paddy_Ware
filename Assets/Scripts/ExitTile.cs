// ExitTile.cs - NEW SCRIPT
using UnityEngine;

public class ExitTile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public float tileSize = 1f;

    void Start()
    {
        // Position in world
        transform.position = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);
    }

    void Update()
    {
        CheckForPlayer();
    }

    void CheckForPlayer()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.currentPhase != GamePhase.Planning) return;

        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player == null) return;

        // Check if player is on this tile
        if (player.GetGridPosition() == gridPosition)
        {
            TriggerWin();
        }
    }

    void TriggerWin()
    {
        Debug.Log("Player reached exit!");

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetWin();
        }
    }

    void OnDrawGizmos()
    {
        // Draw green square for exit
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 worldPos = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);
        Gizmos.DrawCube(worldPos, Vector3.one * tileSize * 0.8f);
    }
}