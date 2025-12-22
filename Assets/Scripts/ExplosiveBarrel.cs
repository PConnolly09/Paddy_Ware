// ExplosiveBarrel.cs - NEW SCRIPT
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveBarrel : Interactable
{
    public float tileSize = 1f;
    public int explosionRadius = 3; // How many tiles away it damages
    public int fuseDelay = 0; // Turns before explosion (0 = immediate)
    public bool showExplosionPreview = true; // NEW

    void Start()
    {
        // Set ID if not set in inspector
        if (string.IsNullOrEmpty(interactableID))
        {
            interactableID = $"barrel_{gridPosition.x}_{gridPosition.y}";
        }
    }

    void Update()
    {
        // Show explosion radius when player is adjacent - NEW
        if (showExplosionPreview && !isConsumed)
        {
            ShowExplosionPreview();
        }
    }


    public override void Interact(PlayerGridMovement player)
    {
        if (isConsumed)
        {
            Debug.Log("Barrel already used!");
            return;
        }

        Debug.Log($"Barrel at {gridPosition} EXPLODES!");

        // Record consumption - NEW
        if (RunRecorder.Instance != null)
        {
            RunRecorder.Instance.RecordResourceConsumption(interactableID);
        }


        // Find and damage enemies in radius
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            Vector2Int enemyPos = enemy.GetGridPosition();
            float distance = Vector2Int.Distance(gridPosition, enemyPos);

            if (distance <= explosionRadius)
            {
                Debug.Log($"Enemy at {enemyPos} killed by explosion!");
                KillEnemy(enemy);
            }
        }

        // Consume the barrel
        Consume();

        // TODO: Particle effect, sound, screen shake
    }

    void ShowExplosionPreview()
    {
        if (GameStateManager.Instance == null || GameStateManager.Instance.currentPhase != GamePhase.Planning)
            return;

        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player == null) return;

        float dist = Vector2Int.Distance(player.GetGridPosition(), gridPosition);

        // Only show when adjacent
        if (dist > 1.5f) return;

        // Draw explosion radius in Scene view (already have OnDrawGizmos)
        // Could also add a runtime circle renderer here
    }

    public override string GetInteractionPreview()
    {
        if (isConsumed)
        {
            return "Barrel (already used)";
        }

        // Show which enemies will be hit - IMPROVED
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        List<Vector2Int> enemiesInRange = new List<Vector2Int>();

        foreach (EnemyController enemy in enemies)
        {
            Vector2Int enemyPos = enemy.GetGridPosition();
            float distance = Vector2Int.Distance(gridPosition, enemyPos);

            if (distance <= explosionRadius)
            {
                enemiesInRange.Add(enemyPos);
            }
        }

        if (enemiesInRange.Count > 0)
        {
            return $"Trigger Barrel (will hit {enemiesInRange.Count} enemies at {string.Join(", ", enemiesInRange)})";
        }
        else
        {
            return "Trigger Barrel (no enemies in range)";
        }
    }

    void KillEnemy(EnemyController enemy)
    {
        // For now, just destroy the enemy
        Destroy(enemy.gameObject);

        // TODO: Death animation, corpse left behind, etc
    }


    // Visualize explosion radius
    void OnDrawGizmos()
    {
        if (isConsumed) return;

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Vector3 worldPos = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);

        // Draw explosion radius circle
        float worldRadius = explosionRadius * tileSize;

        // Draw circle segments
        int segments = 20;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

            Vector3 p1 = worldPos + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * worldRadius;
            Vector3 p2 = worldPos + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * worldRadius;

            Gizmos.DrawLine(p1, p2);
        }
    }
}