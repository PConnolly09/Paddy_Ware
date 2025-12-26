using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    public static void Create(Vector3 position)
    {
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = position;

        // Create particle system
        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.5f, 0f, 1f); // Orange
        main.maxParticles = 50;
        main.duration = 0.3f;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 50)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.yellow, 0.0f),
                new GradientColorKey(Color.red, 0.5f),
                new GradientColorKey(Color.black, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Auto-destroy after effect finishes
        Destroy(explosion, 1f);

        Debug.Log($"Created explosion effect at {position}");
    }
}