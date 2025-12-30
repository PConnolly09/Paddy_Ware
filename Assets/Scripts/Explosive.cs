// Explosive.cs - NEW BASE CLASS
using UnityEngine;

public abstract class Explosive : Interactable
{
    public float tileSize = 1f;
    public int explosionRadius = 2;

    protected abstract void Detonate();

    protected void ExplodeAndDamage()
    {
        Debug.Log($"Explosion at {gridPosition}!");

        Vector3 worldPos = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);
        ExplosionEffect.Create(worldPos);

        // Record to Timeline - UPDATED
        if (TimelineManager.Instance != null)
        {
            TimelineManager.Instance.RecordResourceConsumption(interactableID);
        }

        // Damage enemies...
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            Vector2Int enemyPos = enemy.GetGridPosition();
            float distance = Vector2Int.Distance(gridPosition, enemyPos);

            if (distance <= explosionRadius)
            {
                Debug.Log($"Enemy at {enemyPos} killed!");
                KillEnemy(enemy);
            }
        }

        Consume();
    }

    protected void KillEnemy(EnemyController enemy)
    {
        StartCoroutine(FadeOutAndDestroy(enemy.gameObject));
    }

    System.Collections.IEnumerator FadeOutAndDestroy(GameObject obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Destroy(obj);
            yield break;
        }

        float duration = 0.5f;
        float elapsed = 0f;
        Color originalColor = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            float scale = alpha;
            obj.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(obj);
    }

    // Visualize explosion radius
    void OnDrawGizmos()
    {
        if (isConsumed) return;

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Vector3 worldPos = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);

        float worldRadius = explosionRadius * tileSize;

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