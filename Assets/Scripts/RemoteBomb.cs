// RemoteBomb.cs - NEW SCRIPT
using UnityEngine;

public class RemoteBomb : Explosive
{
    private bool isPlaced = false;

    public override void Interact(PlayerGridMovement player)
    {
        if (isConsumed) return;

        if (!isPlaced)
        {
            PlaceBomb();
        }
        else
        {
            // Detonate remotely
            Detonate();
        }
    }

    void PlaceBomb()
    {
        isPlaced = true;
        Debug.Log($"Remote bomb placed at {gridPosition}. Interact again to detonate.");

        // Visual feedback
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0f, 1f, 0f); // Green = armed and ready
        }
    }

    protected override void Detonate()
    {
        Debug.Log("Remote bomb detonating!");
        ExplodeAndDamage();
    }

    public override string GetInteractionPreview()
    {
        if (isConsumed)
        {
            return "Remote Bomb (used)";
        }

        if (!isPlaced)
        {
            return "Place Remote Bomb";
        }

        // Show detonation preview
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

        return $"Detonate Remote Bomb ({enemiesInRange} targets in range)";
    }

    public bool IsPlaced()
    {
        return isPlaced;
    }
}