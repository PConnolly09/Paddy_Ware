using UnityEngine;
using System.Collections.Generic;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;

    [Header("Settings")]
    public float frameRate = 0.05f;
    public GameObject echoPrefab;
    public Transform playerSpawnPoint;

    [Header("Global State")]
    public int currentDay = 1;
    public float globalEntropy = 0f;
    public StatSet currentDayStats;
    public ArchetypeData currentArchetype;

    private List<CloneData> timelineHistory = new List<CloneData>();
    private CloneData currentRecording;
    private PlayerController activePlayer;
    private Interactable[] worldObjects;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentDayStats = new StatSet();

        // Default Neutral Archetype
        if (currentArchetype == null && EvolutionManager.Instance != null)
            currentArchetype = EvolutionManager.Instance.neutralArchetype;

        worldObjects = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        StartNewDay();
    }

    public void RegisterPlayer(PlayerController player)
    {
        activePlayer = player;
        activePlayer.Initialize(currentDayStats, currentArchetype);
    }

    public void RecordFrame(Vector2 pos, bool interact, int action)
    {
        if (currentRecording != null)
        {
            currentRecording.recording.Add(new FrameData(pos, interact, action));
        }
    }

    public void AddEntropy(float amount)
    {
        globalEntropy += amount;
        Debug.LogWarning($"ENTROPY SPIKE! Current: {globalEntropy}");
    }

    // --- HISTORY MANAGEMENT ---

    public List<CloneData> GetHistory()
    {
        return timelineHistory;
    }

    public void DeleteHistory(int dayNum)
    {
        // Find the day in the list
        CloneData toRemove = timelineHistory.Find(x => x.originalDayNumber == dayNum);

        if (toRemove != null)
        {
            timelineHistory.Remove(toRemove);
            Debug.Log($"TIMELINE: Day {dayNum} has been permanently erased.");
        }
    }

    // --------------------------

    public void EndDay()
    {
        // 1. Save the Current Day's Recording Logic
        if (currentRecording != null)
        {
            // Lock in what the player WAS today
            currentRecording.archetype = currentArchetype;

            // Only add to history if we aren't about to reset the timeline via sacrifice
            // (Optional: You could choose to throw away today's run if you sacrifice)
            timelineHistory.Add(currentRecording);
        }

        // 2. Check for Manual Draft (Sacrifice)
        if (DraftSystem.Instance != null && DraftSystem.Instance.nextDayDraft != null)
        {
            Debug.Log("TIMELINE: Starting day from DRAFT (Sacrifice Reset).");

            // Load stats from the sacrificed clone
            currentDayStats = DraftSystem.Instance.nextDayDraft.startingStats;
            currentArchetype = DraftSystem.Instance.nextDayDraft.startingArchetype;

            // If the draft was a fresh start, maybe reset currentDay? 
            // For now, we keep incrementing to keep IDs unique.

            // Clear the draft so we don't use it forever
            DraftSystem.Instance.ClearDraft();
        }
        else
        {
            // 3. Standard Natural Progression
            Debug.Log("TIMELINE: Natural Progression.");

            // Decay Stats
            currentDayStats = currentDayStats.GetDecayedCopy();

            // Evolve Archetype based on actions
            if (currentRecording != null)
            {
                currentArchetype = EvolutionManager.Instance.DetermineArchetype(currentRecording.recording);
            }
        }

        currentDay++;
        StartNewDay();
    }

    private void StartNewDay()
    {
        currentRecording = new CloneData();
        currentRecording.originalDayNumber = currentDay;
        currentRecording.stats = currentDayStats.Clone();
        // currentRecording.archetype is set at END of day

        foreach (var echo in GameObject.FindGameObjectsWithTag("Echo")) Destroy(echo);

        if (worldObjects != null)
        {
            foreach (var obj in worldObjects) if (obj != null) obj.ResetState();
        }

        foreach (var pastDay in timelineHistory) SpawnEcho(pastDay);

        if (activePlayer != null)
        {
            activePlayer.transform.position = playerSpawnPoint.position;
            activePlayer.Initialize(currentDayStats, currentArchetype);
        }
    }

    private void SpawnEcho(CloneData data)
    {
        if (data.recording.Count == 0) return;

        GameObject echoObj = Instantiate(echoPrefab, data.recording[0].position, Quaternion.identity);
        echoObj.GetComponent<EchoController>().Initialize(data);
    }
}