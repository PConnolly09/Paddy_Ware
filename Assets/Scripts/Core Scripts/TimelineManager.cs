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
    public float globalEntropy = 0f; // NEW: The Chaos Meter
    public StatSet currentDayStats;

    // Lists
    private List<CloneData> timelineHistory = new List<CloneData>();
    private CloneData currentRecording;
    private PlayerController activePlayer;

    // NEW: Track all world objects to reset them
    private Interactable[] worldObjects;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Initialize Stats
        currentDayStats = new StatSet();

        // NEW: Find all interactables
        worldObjects = FindObjectsOfType<Interactable>();

        StartNewDay();
    }

    // ... (Keep RegisterPlayer and RecordFrame the same) ...

    public void RegisterPlayer(PlayerController player)
    {
        activePlayer = player;
        activePlayer.Initialize(currentDayStats);
    }

    public void RecordFrame(Vector2 pos, bool interact, int action)
    {
        if (currentRecording != null)
        {
            currentRecording.recording.Add(new FrameData(pos, interact, action));
        }
    }

    // NEW: Add Entropy with visual log
    public void AddEntropy(float amount)
    {
        globalEntropy += amount;
        Debug.LogWarning($"ENTROPY SPIKE! Current: {globalEntropy}");
        // Update UI here later
    }

    public void EndDay()
    {
        if (currentRecording != null) timelineHistory.Add(currentRecording);

        currentDayStats = currentDayStats.GetDecayedCopy();
        currentDay++;
        StartNewDay();
    }

    private void StartNewDay()
    {
        // 1. Reset Logic
        currentRecording = new CloneData();
        currentRecording.originalDayNumber = currentDay;
        currentRecording.stats = currentDayStats.Clone();

        foreach (var echo in GameObject.FindGameObjectsWithTag("Echo")) Destroy(echo);

        // NEW: Reset World Objects (Trees grow back so Day 1 can chop them again)
        foreach (var obj in worldObjects) obj.ResetState();

        // 2. Spawn History
        foreach (var pastDay in timelineHistory) SpawnEcho(pastDay);

        // 3. Reset Player
        if (activePlayer != null)
        {
            activePlayer.transform.position = playerSpawnPoint.position;
            activePlayer.Initialize(currentDayStats);
        }
    }

    private void SpawnEcho(CloneData data)
    {
        if (data.recording.Count == 0) return;
        GameObject echoObj = Instantiate(echoPrefab, data.recording[0].position, Quaternion.identity);
        echoObj.GetComponent<EchoController>().Initialize(data);
    }
}