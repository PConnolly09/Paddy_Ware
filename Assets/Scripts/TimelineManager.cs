using UnityEngine;
using System.Collections.Generic;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;

    [System.Serializable]
    public class TimelineState
    {
        public int timelineID;
        public int currentTurn;
        public Vector2Int playerPosition;
        public bool isFrozen; // Frozen = paused, not complete
        public bool isComplete; // Actually reached exit
        public List<TurnAction> playerActions;
        public List<string> consumedResources;
        public Dictionary<string, List<Vector2Int>> enemyPaths; // Per-enemy recorded paths

        public TimelineState(int id)
        {
            timelineID = id;
            currentTurn = 0;
            playerPosition = Vector2Int.zero;
            isFrozen = false;
            isComplete = false;
            playerActions = new List<TurnAction>();
            consumedResources = new List<string>();
            enemyPaths = new Dictionary<string, List<Vector2Int>>();
        }
    }

    public int activeTimelineID = 1;
    public Dictionary<int, TimelineState> timelines = new Dictionary<int, TimelineState>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("TimelineManager created");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (timelines.Count == 0)
        {
            CreateNewTimeline();
        }
    }

    public void CreateNewTimeline()
    {
        int newID = timelines.Count + 1;
        TimelineState newTimeline = new TimelineState(newID);
        timelines[newID] = newTimeline;
        activeTimelineID = newID;

        Debug.Log($"Created Timeline {newID}");
    }

    public TimelineState GetActiveTimeline()
    {
        if (timelines.ContainsKey(activeTimelineID))
        {
            return timelines[activeTimelineID];
        }
        return null;
    }

    public void RecordPlayerAction(TurnAction action)
    {
        TimelineState timeline = GetActiveTimeline();
        if (timeline != null && !timeline.isFrozen && !timeline.isComplete)
        {
            timeline.playerActions.Add(action);
            timeline.currentTurn = action.turnNumber;
            timeline.playerPosition = action.endPosition;
            Debug.Log($"Timeline {activeTimelineID}: Recorded action at turn {action.turnNumber}");
        }
    }

    public void RecordEnemyPosition(string enemyID, Vector2Int position)
    {
        TimelineState timeline = GetActiveTimeline();
        if (timeline != null && !timeline.isFrozen && !timeline.isComplete)
        {
            if (!timeline.enemyPaths.ContainsKey(enemyID))
            {
                timeline.enemyPaths[enemyID] = new List<Vector2Int>();
            }

            timeline.enemyPaths[enemyID].Add(position);
            Debug.Log($"Timeline {activeTimelineID}: Recorded enemy {enemyID} at {position} (turn {timeline.currentTurn})");
        }
    }

    public void RecordResourceConsumption(string resourceID)
    {
        TimelineState timeline = GetActiveTimeline();
        if (timeline != null && !timeline.consumedResources.Contains(resourceID))
        {
            timeline.consumedResources.Add(resourceID);
            Debug.Log($"Timeline {activeTimelineID}: Consumed {resourceID}");
        }
    }

    public void FreezeTimeline()
    {
        TimelineState timeline = GetActiveTimeline();
        if (timeline != null)
        {
            timeline.isFrozen = true;
            Debug.Log($"Timeline {activeTimelineID} FROZEN at turn {timeline.currentTurn}");
        }
    }

    public void CompleteTimeline()
    {
        TimelineState timeline = GetActiveTimeline();
        if (timeline != null)
        {
            timeline.isComplete = true;
            Debug.Log($"Timeline {activeTimelineID} COMPLETED");
        }
    }

    public void SwitchToTimeline(int timelineID)
    {
        if (!timelines.ContainsKey(timelineID))
        {
            Debug.LogError($"Timeline {timelineID} doesn't exist!");
            return;
        }

        Debug.Log($"=== SWITCHING TO TIMELINE {timelineID} ===");
        TimelineState target = timelines[timelineID];
        Debug.Log($"  Current turn: {target.currentTurn}");
        Debug.Log($"  Player position: {target.playerPosition}");
        Debug.Log($"  Is frozen: {target.isFrozen}");
        Debug.Log($"  Is complete: {target.isComplete}");

        activeTimelineID = timelineID;

        // Unpause time before reload
        Time.timeScale = 1;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public List<Vector2Int> GetEnemyPath(string enemyID, int forTimelineID)
    {
        if (timelines.ContainsKey(forTimelineID))
        {
            if (timelines[forTimelineID].enemyPaths.ContainsKey(enemyID))
            {
                return timelines[forTimelineID].enemyPaths[enemyID];
            }
        }
        return null;
    }

    public List<TimelineState> GetAllTimelines()
    {
        List<TimelineState> list = new List<TimelineState>();
        foreach (var timeline in timelines.Values)
        {
            list.Add(timeline);
        }
        return list;
    }

    public List<string> GetAllConsumedResources()
    {
        HashSet<string> allConsumed = new HashSet<string>();
        foreach (var timeline in timelines.Values)
        {
            foreach (string resource in timeline.consumedResources)
            {
                allConsumed.Add(resource);
            }
        }
        return new List<string>(allConsumed);
    }

    //NEW ADDED BACK METHODS

    public List<int> GetIncompleteTimelineIDs()
    {
        List<int> incomplete = new List<int>();
        foreach (var kvp in timelines)
        {
            if (!kvp.Value.isComplete)
            {
                incomplete.Add(kvp.Key);
            }
        }
        return incomplete;
    }

    public void SaveTimelineState(Vector2Int playerPos, int turn)
    {
        TimelineManager.TimelineState timeline = GetActiveTimeline();
        if (timeline != null)
        {
            timeline.playerPosition = playerPos;
            timeline.currentTurn = turn;
        }
    }

    public void ShowTimelineSwitchMenu()
    {
        // Pause game
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.currentPhase = GamePhase.TimelineMenu;
        }
        Time.timeScale = 0;
    }
}