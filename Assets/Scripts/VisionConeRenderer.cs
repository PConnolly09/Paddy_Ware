// VisionConeRenderer.cs - NEW SCRIPT
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VisionConeRenderer : MonoBehaviour
{
    public EnemyController enemy;
    public int segments = 10;
    public float visionRange = 3f;
    public float visionAngle = 90f;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1, 0, 0, 0.3f);
        lineRenderer.endColor = new Color(1, 0, 0, 0.3f);
    }

    void Update()
    {
        if (enemy == null) return;

        DrawVisionCone();
    }

    void DrawVisionCone()
    {
        Vector2 facing = enemy.GetFacingDirection();
        // For now, just draw a simple cone

        lineRenderer.positionCount = segments + 3;

        Vector3 origin = transform.position;
        lineRenderer.SetPosition(0, origin);

        float angleStep = visionAngle / segments;
        float startAngle = -visionAngle / 2f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, 0, angle) * facing;
            Vector3 point = origin + (direction * visionRange);
            lineRenderer.SetPosition(i + 1, point);
        }

        lineRenderer.SetPosition(segments + 2, origin); // Close the cone
    }
}