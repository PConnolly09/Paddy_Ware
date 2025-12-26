// PowderKeg.cs - NEW SCRIPT (replaces ExplosiveBarrel)
using UnityEngine;

public class PowderKeg : Explosive
{
    public int fuseLength = 3; // Turns until explosion

    private bool isLit = false;
    private int turnsRemaining = 0;

    public override void Interact(PlayerGridMovement player)
    {
        if (isConsumed) return;

        if (!isLit)
        {
            LightFuse();
        }
        else
        {
            Debug.Log("Fuse is already lit! Can't stop it now!");
        }
    }

    void LightFuse()
    {
        isLit = true;
        turnsRemaining = fuseLength;

        Debug.Log($"Powder keg fuse lit! {turnsRemaining} turns until BOOM!");

        // Visual feedback
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.red;
        }
    }

    public void OnTurnComplete()
    {
        if (isLit && turnsRemaining > 0)
        {
            turnsRemaining--;

            Debug.Log($"Powder keg countdown: {turnsRemaining}");

            // Flash faster as it gets closer
            if (turnsRemaining <= 1)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.Lerp(Color.red, Color.yellow, Time.time % 0.2f);
                }
            }

            if (turnsRemaining <= 0)
            {
                Detonate();
            }
        }
    }

    protected override void Detonate()
    {
        ExplodeAndDamage();
    }

    public override string GetInteractionPreview()
    {
        if (isConsumed)
        {
            return "Powder Keg (used)";
        }

        if (!isLit)
        {
            return $"Light Fuse (explodes in {fuseLength} turns)";
        }

        // Show what will be hit
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

        return $"Fuse Burning! ({turnsRemaining} turns, {enemiesInRange} targets)";
    }

    // For precog - show if lit and countdown
    public bool IsLit()
    {
        return isLit;
    }

    public int GetTurnsRemaining()
    {
        return turnsRemaining;
    }
}