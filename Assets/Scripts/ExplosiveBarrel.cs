// ExplosiveBarrel.cs - NEW SCRIPT
using UnityEngine;

public class ExplosiveBarrel : Interactable
{
    public float tileSize = 1f;
    public int explosionRadius = 1; // How many tiles away it damages

    void Start()
    {
        // Set ID if not set in inspector
        if (string.IsNullOrEmpty(interactableID))
        {
            interactableID = $"barrel_{gridPosition.x}_{gridPosition.y}";
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

    void KillEnemy(EnemyController enemy)
    {
        // For now, just destroy the enemy
        Destroy(enemy.gameObject);

        // TODO: Death animation, corpse left behind, etc
    }

    public override string GetInteractionPreview()
    {
        if (isConsumed)
        {
            return "Barrel (already used)";
        }

        // Count enemies in range
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int enemiesInRange = 0;

        foreach (EnemyController enemy in enemies)
        {
            Vector2Int enemyPos = enemy.GetGridPosition();
            float distance = Vector2Int.Distance(gridPosition, enemyPos);

            if (distance <= explosionRadius)
            {
                enemiesInRange++;
            }
        }

        return $"Trigger Barrel (will hit {enemiesInRange} enemies)";
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