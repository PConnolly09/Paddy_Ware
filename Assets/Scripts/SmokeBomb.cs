// SmokeBomb.cs - NEW SCRIPT
using UnityEngine;
using System.Collections;

public class SmokeBomb : Interactable
{
    public float tileSize = 1f;
    public int smokeRadius = 2;
    public int smokeDuration = 3; // Turns

    private GameObject smokeEffect;
    private int turnsRemaining = 0;

    void Update()
    {
        // Check if smoke should dissipate
        if (turnsRemaining > 0 && GameStateManager.Instance != null)
        {
            // Smoke dissipates after duration
            // This would need turn tracking...
        }
    }

    public override void Interact(PlayerGridMovement player)
    {
        if (isConsumed) return;

        Debug.Log($"Smoke bomb at {gridPosition} activated!");

        // Record consumption
        if (RunRecorder.Instance != null)
        {
            RunRecorder.Instance.RecordResourceConsumption(interactableID);
        }

        // Create smoke effect
        CreateSmokeEffect();

        // Blind enemies in radius for N turns
        BlindNearbyEnemies();

        Consume();
    }

    void CreateSmokeEffect()
    {
        // Create visual smoke cloud
        smokeEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        smokeEffect.transform.position = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);
        smokeEffect.transform.localScale = Vector3.one * smokeRadius * tileSize;

        Renderer renderer = smokeEffect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        // Destroy after duration
        Destroy(smokeEffect, smokeDuration * 2f); // Rough estimate of time
    }

    void BlindNearbyEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            Vector2Int enemyPos = enemy.GetGridPosition();
            float distance = Vector2Int.Distance(gridPosition, enemyPos);

            if (distance <= smokeRadius)
            {
                Debug.Log($"Enemy at {enemyPos} blinded by smoke!");
                // TODO: Add blind status to enemy
                // enemy.SetBlinded(smokeDuration);
            }
        }
    }

    public override string GetInteractionPreview()
    {
        if (isConsumed)
        {
            return "Smoke Bomb (used)";
        }

        return $"Throw Smoke Bomb (blinds enemies for {smokeDuration} turns)";
    }
}