using UnityEngine;

public enum InteractionType { None = 0, Chop = 1, Mine = 2, Water = 3 }

public class Interactable : MonoBehaviour
{
    [Header("Identity")]
    public string objectID;
    public InteractionType type;

    [Header("Stats")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Visuals")]
    public Sprite fullSprite;
    public Sprite emptySprite;
    private SpriteRenderer sr;
    private Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        // Ensure ResetState is called to set initial values
        ResetState();
    }

    public void ResetState()
    {
        // Safety check if components are missing
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();

        currentHealth = maxHealth;

        if (sr != null) sr.sprite = fullSprite;

        // CRITICAL: Ensure collider is enabled and NOT a trigger initially
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }
    }

    public bool ReceiveHit(int damage)
    {
        if (currentHealth <= 0) return false;

        currentHealth -= damage;
        // Debug.Log($"{name} hit! HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        return true;
    }

    void Die()
    {
        if (sr != null) sr.sprite = emptySprite;

        // When dead, make it a Trigger so Ghosts can still detect it (OverlapCircle hits triggers)
        // but Players can walk through it
        if (col != null) col.isTrigger = true;
    }

    public bool IsAvailable()
    {
        return currentHealth > 0;
    }
}