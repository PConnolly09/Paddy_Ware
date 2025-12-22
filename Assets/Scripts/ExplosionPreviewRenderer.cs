// ExplosionPreviewRenderer.cs - NEW SCRIPT (attach to barrel)
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ExplosionPreviewRenderer : MonoBehaviour
{
    public ExplosiveBarrel barrel;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1, 0.5f, 0, 0.5f);
        lineRenderer.endColor = new Color(1, 0.5f, 0, 0.5f);
        lineRenderer.sortingOrder = 2;
    }

    void Update()
    {
        if (barrel == null || barrel.IsConsumed())
        {
            lineRenderer.enabled = false;
            return;
        }

        // Only show when player is adjacent
        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player == null) return;

        float dist = Vector2Int.Distance(player.GetGridPosition(), barrel.GetGridPosition());

        if (dist <= 1.5f && GameStateManager.Instance != null && GameStateManager.Instance.currentPhase == GamePhase.Planning)
        {
            DrawExplosionCircle();
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    void DrawExplosionCircle()
    {
        int segments = 30;
        lineRenderer.positionCount = segments + 1;

        float radius = barrel.explosionRadius * barrel.tileSize;
        Vector3 center = transform.position;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            lineRenderer.SetPosition(i, pos);
        }
    }
}