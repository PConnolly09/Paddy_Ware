using UnityEngine;

public class EchoController : MonoBehaviour
{
    private CloneData myData;
    private int playbackIndex = 0;
    private float timer = 0f;
    private Rigidbody2D rb;

    // NEW: Raycasting for ghosts
    public LayerMask interactLayer;

    public void Initialize(CloneData data)
    {
        myData = data;
        rb = GetComponent<Rigidbody2D>();

        float scale = 0.5f + (myData.stats.strength / 200f);
        transform.localScale = new Vector3(scale, scale, 1);
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.6f);
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

        // 1. Move
        rb.MovePosition(frame.position);

        // 2. Action Replay
        if (frame.actionID > 0)
        {
            // The clone wants to chop. Let's see if the tree exists.
            // We use a small OverlapCircle to find nearby objects because Raycasts 
            // from ghosts might be slightly misaligned due to physics drift.

            Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.5f, interactLayer);

            if (hit != null)
            {
                Interactable obj = hit.GetComponent<Interactable>();
                if (obj != null && obj.IsAvailable())
                {
                    // Success! The object is there.
                    // Damage it using the CLONE'S recorded stats
                    int damage = Mathf.Max(1, myData.stats.strength / 10);
                    obj.ReceiveHit(damage);
                }
                else
                {
                    // Paradox: Object is present but dead/stump
                    TriggerParadox();
                }
            }
            else
            {
                // Paradox: Object is missing entirely
                TriggerParadox();
            }
        }

        playbackIndex++;
    }

    void TriggerParadox()
    {
        // Visual: Flash Red
        GetComponent<SpriteRenderer>().color = Color.red;

        // Logic: Add Entropy
        TimelineManager.Instance.AddEntropy(1.5f);

        // Optional: Play "Glitch" sound
    }
}