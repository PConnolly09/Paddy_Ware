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
    public ArchetypeData currentArchetype; // The class the Player IS right now

    private List<CloneData> timelineHistory = new List<CloneData>();
    private CloneData currentRecording;
    private PlayerController activePlayer;
    private Interactable[] worldObjects;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentDayStats = new StatSet();

        // Ensure default archetype
        if (currentArchetype == null && EvolutionManager.Instance != null)
            currentArchetype = EvolutionManager.Instance.neutralArchetype;

        // Use modern Find function
        worldObjects = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        StartNewDay();
    }

    public void RegisterPlayer(PlayerController player)
    {
        activePlayer = player;
        // The Player is initialized with the CURRENT day's archetype and stats
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

    public void EndDay()
    {
        if (currentRecording != null)
        {
            // CRITICAL FIX: 
            // 1. Lock in what the player WAS today.
            currentRecording.archetype = currentArchetype;

            // 2. Calculate what the player WILL BE tomorrow based on today's actions.
            ArchetypeData nextClass = EvolutionManager.Instance.DetermineArchetype(currentRecording.recording);

            // 3. Update global state for the Next Day
            currentArchetype = nextClass;

            timelineHistory.Add(currentRecording);

            Debug.Log($"DAY ENDED. Saved Recording as {currentRecording.archetype.className}. Tomorrow you will be {nextClass.className}.");
        }

        currentDayStats = currentDayStats.GetDecayedCopy();
        currentDay++;
        StartNewDay();
    }

    private void StartNewDay()
    {
        currentRecording = new CloneData();
        currentRecording.originalDayNumber = currentDay;
        currentRecording.stats = currentDayStats.Clone();
        // Note: currentRecording.archetype is not set yet, it gets set at EndDay

        foreach (var echo in GameObject.FindGameObjectsWithTag("Echo")) Destroy(echo);

        if (worldObjects != null)
        {
            foreach (var obj in worldObjects) if (obj != null) obj.ResetState();
        }

        foreach (var pastDay in timelineHistory) SpawnEcho(pastDay);

        if (activePlayer != null)
        {
            activePlayer.transform.position = playerSpawnPoint.position;
            // Initialize Player with the NEW evolved archetype
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