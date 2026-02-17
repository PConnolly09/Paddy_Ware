using UnityEngine;

public class EchoController : MonoBehaviour
{
    private CloneData myData;
    private int playbackIndex = 0;
    private float timer = 0f;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    [Header("Settings")]
    public float interactionRadius = 1.5f;

    [Header("Debug Info")]
    public ArchetypeData currentArchetype; // Visible in Inspector for debugging

    public void Initialize(CloneData data)
    {
        myData = data;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // DEBUG: Expose and Log the Archetype
        currentArchetype = myData.archetype;
        string archName = (currentArchetype != null) ? currentArchetype.className : "Null (Neutral)";
        Debug.Log($"ECHO INIT: Day {myData.originalDayNumber} spawned. Archetype: {archName}");

        // 1. Size based on Strength
        float scale = 0.5f + (myData.stats.strength / 200f);
        transform.localScale = new Vector3(scale, scale, 1);

        // 2. Color based on Archetype
        // We use the Archetype tint, but make it transparent (Ghostly)
        SetGhostColor();
    }

    void SetGhostColor()
    {
        Color baseColor = Color.white;
        if (currentArchetype != null) baseColor = currentArchetype.tint;
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);
    }

    void Update()
    {
        if (myData == null || playbackIndex >= myData.recording.Count) return;

        timer += Time.deltaTime;

        if (TimelineManager.Instance != null && timer >= TimelineManager.Instance.frameRate)
        {
            PlayFrame();
            timer = 0;
        }
    }

    void PlayFrame()
    {
        FrameData frame = myData.recording[playbackIndex];
        rb.MovePosition(frame.position);

        if (frame.actionID > 0)
        {
            PerformAction(frame.actionID);
        }
        else
        {
            // Reset color if we aren't doing anything special (recovering from Paradox Red)
            if (sr.color != Color.red) SetGhostColor();
        }

        playbackIndex++;
    }

    void PerformAction(int actionID)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        bool foundTarget = false;

        foreach (var hit in hits)
        {
            Interactable obj = hit.GetComponent<Interactable>();
            if (obj != null)
            {
                foundTarget = true;
                if (obj.IsAvailable())
                {
                    obj.ReceiveHit(Mathf.Max(1, myData.stats.strength / 10));
                    // Optional: Flash solid color on hit, then fade back
                    sr.color = currentArchetype != null ? currentArchetype.tint : Color.white;
                }
                else
                {
                    TriggerParadox();
                }
                return;
            }
        }

        if (!foundTarget) TriggerParadox();
    }

    void TriggerParadox()
    {
        sr.color = Color.red;
        TimelineManager.Instance.AddEntropy(2f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}