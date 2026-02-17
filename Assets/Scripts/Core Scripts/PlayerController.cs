using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Base Settings")]
    public float baseMoveSpeed = 5f;
    public float interactionRadius = 1.2f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private StatSet myStats;
    private float recordTimer;
    private Vector2 moveInput;

    // BUFFER
    private int bufferedActionID = 0;
    private bool bufferedInteract = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); // Need sprite renderer
        if (TimelineManager.Instance != null)
            TimelineManager.Instance.RegisterPlayer(this);
    }

    // NEW: Accepts Archetype
    public void Initialize(StatSet stats, ArchetypeData archetype)
    {
        myStats = stats;

        // Stat visuals
        float scale = 0.5f + (myStats.strength / 200f);
        transform.localScale = new Vector3(scale, scale, 1);

        // Archetype visuals
        if (archetype != null && sr != null)
        {
            sr.color = archetype.tint;
        }
    }

    // ... (Keep Update, HandleInteraction, FixedUpdate, OnDrawGizmos exactly as they were in previous step) ...
    // Note: I am not re-pasting the Update loop to save space, assuming you kept the Robust Player code.
    // If you need the full file again, let me know.

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        if (Input.GetKeyDown(KeyCode.Space)) DetectInteraction();

        recordTimer += Time.deltaTime;
        if (TimelineManager.Instance != null && recordTimer >= TimelineManager.Instance.frameRate)
        {
            TimelineManager.Instance.RecordFrame(rb.position, bufferedInteract, bufferedActionID);

            if (bufferedActionID > 0) Debug.Log($"RECORDED Action {bufferedActionID}");

            bufferedActionID = 0;
            bufferedInteract = false;
            recordTimer = 0;
        }

        if (Input.GetKeyDown(KeyCode.Return)) TimelineManager.Instance.EndDay();
    }
    void DetectInteraction()
    {
        bufferedInteract = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        foreach (var hit in hits)
        {
            // Check for Altar FIRST
            SacrificeAltar altar = hit.GetComponent<SacrificeAltar>();
            if (altar != null)
            {
                altar.OpenAltar();
                return; // Stop processing, we opened a menu
            }

            // Standard Interactable check
            Interactable obj = hit.GetComponent<Interactable>();
            if (obj != null && obj.IsAvailable())
            {
                int damage = Mathf.Max(1, myStats.strength / 10);
                bool success = obj.ReceiveHit(damage);

                if (success)
                {
                    Debug.Log($"PLAYER: Buffering Action {(int)obj.type}");
                    bufferedActionID = (int)obj.type;
                    return;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (myStats == null) return;
        float speedMultiplier = Mathf.Max(0.2f, myStats.agility / 100f);
        rb.MovePosition(rb.position + moveInput * baseMoveSpeed * speedMultiplier * Time.fixedDeltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}