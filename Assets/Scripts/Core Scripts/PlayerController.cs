using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Base Settings")]
    public float baseMoveSpeed = 5f;
    public float interactionRange = 1.5f; // Distance to chop
    public LayerMask interactLayer;       // Set this to "World" in Inspector

    private Rigidbody2D rb;
    private StatSet myStats;
    private float recordTimer;
    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down; // For raycasting

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (TimelineManager.Instance != null) TimelineManager.Instance.RegisterPlayer(this);
    }

    public void Initialize(StatSet stats)
    {
        myStats = stats;
        float scale = 0.5f + (myStats.strength / 200f);
        transform.localScale = new Vector3(scale, scale, 1);
    }

    void Update()
    {
        // Movement
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        if (moveInput.magnitude > 0) lastDirection = moveInput;

        // Interaction Logic
        int actionID = 0;
        bool interactPressed = Input.GetKeyDown(KeyCode.Space); // "Action" button

        if (interactPressed)
        {
            // Raycast forward to see if we hit a Tree
            RaycastHit2D hit = Physics2D.Raycast(transform.position, lastDirection, interactionRange, interactLayer);

            if (hit.collider != null)
            {
                Interactable obj = hit.collider.GetComponent<Interactable>();
                if (obj != null && obj.IsAvailable())
                {
                    // Calculate Damage based on Strength
                    int damage = Mathf.Max(1, myStats.strength / 10);
                    bool success = obj.ReceiveHit(damage);

                    if (success)
                    {
                        actionID = (int)obj.type; // Record "Chop" (1) or "Mine" (2)
                        // Play Local Animation/Sound
                    }
                }
            }
            else
            {
                // Whiffed action (still record it so clone swings at air)
                actionID = 1; // Default swing
            }
        }

        // Recording
        recordTimer += Time.deltaTime;
        if (TimelineManager.Instance != null && recordTimer >= TimelineManager.Instance.frameRate)
        {
            TimelineManager.Instance.RecordFrame(rb.position, interactPressed, actionID);
            recordTimer = 0;
        }

        if (Input.GetKeyDown(KeyCode.Return)) TimelineManager.Instance.EndDay();
    }

    void FixedUpdate()
    {
        if (myStats == null) return;
        float speedMultiplier = Mathf.Max(0.2f, myStats.agility / 100f);
        rb.MovePosition(rb.position + moveInput * baseMoveSpeed * speedMultiplier * Time.fixedDeltaTime);
    }
}