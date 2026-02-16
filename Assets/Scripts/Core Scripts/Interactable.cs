using UnityEngine;

public enum InteractionType { None = 0, Chop = 1, Mine = 2, Water = 3 }

public class Interactable : MonoBehaviour
{
    [Header("Identity")]
    public string objectID; // Manually set this in Inspector (e.g., "Tree_01", "Rock_North")
    public InteractionType type;

    [Header("Stats")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Visuals")]
    public Sprite fullSprite;
    public Sprite emptySprite; // Stump/Rubble
    private SpriteRenderer sr;
    private Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        ResetState();
    }

    // Called by TimelineManager at the start of every day
    public void ResetState()
    {
        currentHealth = maxHealth;
        sr.sprite = fullSprite;
        col.enabled = true; // Make it solid again
    }

    // Returns TRUE if object was successfully hit
    public bool ReceiveHit(int damage)
    {
        if (currentHealth <= 0) return false; // Already dead

        currentHealth -= damage;
        // Play wobble animation or particle here

        if (currentHealth <= 0)
        {
            Die();
        }
        return true;
    }

    void Die()
    {
        sr.sprite = emptySprite;
        col.enabled = false; // Player can walk over stump
        // Play destroy sound
    }

    public bool IsAvailable()
    {
        return currentHealth > 0;
    }
}