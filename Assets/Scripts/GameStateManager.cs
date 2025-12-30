// GameStateManager.cs - NEW SCRIPT
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Planning,    // Player is thinking, can preview
    Executing,   // Turn is executing (animations play)
    Complete,     // Turn done, ready for next
    GameOver,    // NEW: Game over state
    Victory,
    TimelineMenu,
    TimelineComplete
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public GamePhase currentPhase = GamePhase.Planning;

    // Add field
    private List<GhostPlayer> activeGhosts = new List<GhostPlayer>();
    public GameObject ghostPrefab; // Assign in inspector
                                   // Add field
    private bool canProgressToNextLevel = false;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {

        if (TimelineManager.Instance == null)
        {
            GameObject tm = new GameObject("TimelineManager");
            tm.AddComponent<TimelineManager>();
        }
        // Spawn ghosts from OTHER timelines
        SpawnGhostsFromOtherTimelines();

        // Restore active timeline state
        RestoreActiveTimelineState();

        // Disable consumed resources
        DisableConsumedResources();
    }


    void Update()
    {
        // Update game over to use this:
        if (currentPhase == GamePhase.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCurrentTimeline();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                if (TimelineManager.Instance != null)
                {
                    TimelineManager.Instance.ShowTimelineSwitchMenu();
                }
            }
            return;
        }

        if (currentPhase == GamePhase.Victory)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                // Switch timelines from victory screen
                if (TimelineManager.Instance != null)
                {
                    TimelineManager.Instance.ShowTimelineSwitchMenu();
                }
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                // Check if all timelines complete
                bool allComplete = true;
                if (TimelineManager.Instance != null)
                {
                    foreach (var timeline in TimelineManager.Instance.GetAllTimelines())
                    {
                        if (!timeline.isComplete)
                        {
                            allComplete = false;
                            break;
                        }
                    }
                }

                if (allComplete)
                {
                    LoadNextLevel();
                }
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                if (TimelineManager.Instance != null)
                {
                    TimelineManager.Instance.CreateNewTimeline();
                }

                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RestartFromBeginning();
            }
            return;
        }
        if (currentPhase == GamePhase.TimelineMenu)
        {
            HandleTimelineMenuInput();
            return;
        }
        // Only allow input if not game over - UPDATED
        if (currentPhase == GamePhase.Planning && Input.GetKeyDown(KeyCode.Space))
        {
            CommitTurn();
        }

        if (currentPhase == GamePhase.Planning && Input.GetKeyDown(KeyCode.N))
        {
            if (TimelineManager.Instance != null)
            {
                TimelineManager.Instance.FreezeTimeline();
                TimelineManager.Instance.CreateNewTimeline();

                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartCurrentTimeline();
        }

        if (currentPhase == GamePhase.Complete)
        {
            currentPhase = GamePhase.Planning;
        }

        // Update to handle TimelineComplete in Update():
        if (currentPhase == GamePhase.TimelineComplete)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                // Switch to another timeline
                if (TimelineManager.Instance != null)
                {
                    TimelineManager.Instance.ShowTimelineSwitchMenu();
                }
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                // Create new timeline
                if (TimelineManager.Instance != null)
                {
                    TimelineManager.Instance.CreateNewTimeline();
                    RestartFromBeginning(); // Reload with new timeline
                }
            }
            return;
        }

        // Don't allow planning if game over
    }


    void SpawnGhostsFromOtherTimelines()
    {
        if (TimelineManager.Instance == null || ghostPrefab == null) return;

        int activeID = TimelineManager.Instance.activeTimelineID;

        foreach (var timeline in TimelineManager.Instance.GetAllTimelines())
        {
            // Don't spawn ghost for active timeline
            if (timeline.timelineID == activeID) continue;

            // Only spawn if timeline has actions
            if (timeline.playerActions.Count == 0) continue;

            Debug.Log($"Spawning ghost for Timeline {timeline.timelineID} with {timeline.playerActions.Count} actions");

            GameObject ghostObj = Instantiate(ghostPrefab);
            GhostPlayer ghost = ghostObj.GetComponent<GhostPlayer>();

            if (ghost != null)
            {
                // Create RunData from timeline
                RunData runData = new RunData();
                runData.runNumber = timeline.timelineID;
                runData.actions = new List<TurnAction>(timeline.playerActions);
                runData.completed = timeline.isComplete;
                runData.turnCount = timeline.currentTurn;

                ghost.runData = runData;
                ghost.isFrozenTimeline = timeline.isFrozen; // NEW field needed in GhostPlayer
                activeGhosts.Add(ghost);
            }
        }
    }

    void RestoreActiveTimelineState()
    {
        if (TimelineManager.Instance == null) return;

        TimelineManager.TimelineState active = TimelineManager.Instance.GetActiveTimeline();
        if (active == null) return;

        // Only restore if there's actual progress to restore
        if (active.currentTurn == 0)
        {
            Debug.Log("Timeline at turn 0, starting fresh");
            return;
        }

        Debug.Log($"Restoring Timeline {active.timelineID} to turn {active.currentTurn}");

        if (TurnCounter.Instance != null)
        {
            TurnCounter.Instance.SetTurn(active.currentTurn);
        }

        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player != null)
        {
            player.RestoreFromTimeline(active.playerPosition, active.currentTurn);
        }
    }

    void DisableConsumedResources()
    {
        if (TimelineManager.Instance == null) return;

        List<string> consumedIDs = TimelineManager.Instance.GetAllConsumedResources();

        Debug.Log($"Disabling {consumedIDs.Count} consumed resources");

        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        foreach (Interactable obj in interactables)
        {
            if (consumedIDs.Contains(obj.interactableID))
            {
                Debug.Log($"Consuming {obj.interactableID}");
                obj.Consume();
            }
        }
    }

    void LoadNextLevel()
{
    int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
    int nextSceneIndex = currentSceneIndex + 1;
    
    // Clear timelines when changing levels
    if (TimelineManager.Instance != null)
    {
        Destroy(TimelineManager.Instance.gameObject);
    }
    
    if (nextSceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
    }
    else
    {
        Debug.Log("No more levels!");
    }
}
    void RestartFromBeginning()
    {
        Debug.Log("Restarting from beginning...");

        // Clear TimelineManager
        if (TimelineManager.Instance != null)
        {
            Destroy(TimelineManager.Instance.gameObject);
        }

        // Reload scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void SetWin()
    {
        if (TimelineManager.Instance != null)
        {
            TimelineManager.Instance.CompleteTimeline();
        }

        currentPhase = GamePhase.Victory;
    }

    void RestartCurrentTimeline()
    {
        Debug.Log("Restarting current timeline...");

        if (TimelineManager.Instance != null)
        {
            TimelineManager.TimelineState active = TimelineManager.Instance.GetActiveTimeline();

            if (active != null)
            {
                // Clear this timeline's data only
                active.currentTurn = 0;
                active.playerPosition = Vector2Int.zero;
                active.playerActions.Clear();
                active.consumedResources.Clear();
                active.isFrozen = false;
                active.isComplete = false;
                // Keep enemy paths - they're deterministic anyway

                Debug.Log($"Timeline {active.timelineID} data cleared");
            }
        }

        // Reload scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    void SaveCurrentTimelineState()
    {
        if (TimelineManager.Instance == null) return;

        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player == null) return;

        TimelineManager.Instance.SaveTimelineState(
            player.GetGridPosition(),
            TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0
        );

        Debug.Log("Current timeline state saved");
    }

    void CommitTurn()
    {
        Debug.Log("Turn committed!");
        currentPhase = GamePhase.Executing;

        // Tell all entities to execute their planned actions
        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player != null)
        {
            player.ExecutePlannedMove();
        }

        // Tell all enemies to execute - NEW
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (EnemyController enemy in enemies)
        {
            enemy.ExecutePlannedMove();
        }

        // Tell all ghosts to execute - NEW
        foreach (GhostPlayer ghost in activeGhosts)
        {
            ghost.ExecuteGhostTurn();
        }

        // Later: Tell ghosts to execute too
        StartCoroutine(WaitForAllExecutions());
    }

    void HandleTimelineMenuInput()
    {
        // ESC to close menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            currentPhase = GamePhase.Planning;
            Time.timeScale = 1;
        }

        // Number keys to switch timelines
        if (TimelineManager.Instance != null)
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    if (TimelineManager.Instance.timelines.ContainsKey(i))
                    {
                        Time.timeScale = 1;
                        TimelineManager.Instance.SwitchToTimeline(i);
                    }
                }
            }
        }
    }
    // NEW METHOD
    System.Collections.IEnumerator WaitForAllExecutions()
    {
        // Wait one frame for execution to start
        yield return null;

        // Wait until player and all enemies finish
        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        bool stillExecuting = true;
        while (stillExecuting)
        {
            stillExecuting = false;

            // Check if player is still executing
            if (player != null && player.IsExecuting())
            {
                stillExecuting = true;
            }

            // Check if any enemy is still executing
            foreach (EnemyController enemy in enemies)
            {
                if (enemy.IsExecuting())
                {
                    stillExecuting = true;
                    break;
                }
            }

            // Check ghosts too - NEW
            foreach (GhostPlayer ghost in activeGhosts)
            {
                if (ghost.IsExecuting())
                {
                    stillExecuting = true;
                    break;
                }
            }
            yield return null;
        }

        // Everyone done moving, NOW check vision - NEW
        foreach (EnemyController enemy in enemies)
        {
            enemy.CheckVisionAfterTurn();
        }

        // CHECK if game over happened during vision check - NEW
        if (currentPhase == GamePhase.GameOver)
        {
            Debug.Log("Game over detected, stopping turn completion");
            yield break; // EXIT coroutine, don't call OnTurnExecutionComplete
        }

        // Only complete turn if game isn't over
        // Everyone done, complete turn
        Debug.Log("TURN EXECUTION COMPLETE");
        OnTurnExecutionComplete();

    }

    // In GameStateManager.OnTurnExecutionComplete():
    public void OnTurnExecutionComplete()
    {
        if (currentPhase == GamePhase.GameOver)
        {
            return;
        }

        currentPhase = GamePhase.Complete;

        if (TurnCounter.Instance != null)
        {
            TurnCounter.Instance.IncrementTurn();
        }

        // Update powder kegs - NEW
        PowderKeg[] kegs = FindObjectsByType<PowderKeg>(FindObjectsSortMode.None);
        foreach (PowderKeg keg in kegs)
        {
            keg.OnTurnComplete();
        }
    }

    public bool IsPlanning()
    {
    bool planning = (currentPhase == GamePhase.Planning);
    return planning;
    }

    public void SetGameOver()
    {
        currentPhase = GamePhase.GameOver;

    }

    void StartNextRun()
    {
        Debug.Log("Starting next run...");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

    }

    void OnGUI()
    {
        // GAME OVER - Takes over entire screen
        if (currentPhase == GamePhase.GameOver)
        {
            DrawGameOverScreen();
            return; // Don't draw anything else
        }

        // VICTORY - Takes over entire screen
        if (currentPhase == GamePhase.Victory)
        {
            DrawVictoryScreen();
            return; // Don't draw anything else
        }

        // NORMAL GAMEPLAY UI
        DrawPhaseIndicator();

        if (currentPhase == GamePhase.Planning)
        {
            DrawPlanningHints();
        }

        // Timeline menu
        if (currentPhase == GamePhase.TimelineMenu)
        {
            DrawTimelineMenu();
        }

        if (currentPhase == GamePhase.TimelineComplete)
        {
            DrawTimelineCompleteScreen();
        }
    }

    void DrawTimelineCompleteScreen()
    {
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bigStyle = new GUIStyle();
        bigStyle.fontSize = 48;
        bigStyle.normal.textColor = Color.cyan;
        bigStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 60),
            "TIMELINE COMPLETE!", bigStyle);

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 24;
        textStyle.normal.textColor = Color.white;
        textStyle.alignment = TextAnchor.MiddleCenter;

        int timelineID = TimelineManager.Instance != null ? TimelineManager.Instance.activeTimelineID : 0;
        int incomplete = 0;
        if (TimelineManager.Instance != null)
        {
            incomplete = TimelineManager.Instance.GetIncompleteTimelineIDs().Count;
        }

        GUI.Label(new Rect(0, Screen.height / 2 - 10, Screen.width, 30),
            $"Timeline {timelineID} solved!", textStyle);

        GUI.Label(new Rect(0, Screen.height / 2 + 30, Screen.width, 30),
            $"{incomplete} timeline(s) remaining", textStyle);

        GUI.Label(new Rect(0, Screen.height / 2 + 70, Screen.width, 60),
            "[T] Switch Timeline  |  [N] New Timeline", textStyle);
    }
    public void SaveTimelineState(Vector2Int playerPos, int turn)
    {
        TimelineManager.TimelineState timeline;
        timeline = TimelineManager.Instance.GetActiveTimeline();
        if (timeline != null)
        {
            timeline.playerPosition = playerPos;
            timeline.currentTurn = turn;
        }
    }                 
    void DrawTimelineMenu()
    {
        GUI.color = new Color(0, 0, 0, 0.9f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 36;
        titleStyle.normal.textColor = Color.cyan;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, 80, Screen.width, 50), "TIMELINE SELECTION", titleStyle);

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 22;
        textStyle.alignment = TextAnchor.MiddleLeft;

        if (TimelineManager.Instance != null)
        {
            int y = 160;
            int currentID = TimelineManager.Instance.activeTimelineID;

            foreach (var kvp in TimelineManager.Instance.timelines)
            {
                int id = kvp.Key;
                TimelineManager.TimelineState timeline = kvp.Value; //may not work?

                // Box background
                if (id == currentID)
                {
                    GUI.color = new Color(1, 1, 0, 0.3f);
                    GUI.DrawTexture(new Rect(50, y - 5, Screen.width - 100, 70), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                // Timeline info
                Color color = timeline.isComplete ? Color.green : Color.white;
                if (id == currentID) color = Color.yellow;

                textStyle.normal.textColor = color;

                string status = timeline.isComplete ? "COMPLETE" : $"In Progress (Turn {timeline.currentTurn})";
                string marker = id == currentID ? ">" : "  ";

                GUI.Label(new Rect(70, y, 200, 30), $"{marker}[{id}] Timeline {id}", textStyle);

                textStyle.fontSize = 18;
                textStyle.normal.textColor = Color.gray;
                GUI.Label(new Rect(70, y + 30, Screen.width - 140, 30), status, textStyle);
                textStyle.fontSize = 22;

                y += 80;
            }

            // Instructions
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.fontSize = 20;

            GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 30),
                "Press number key [1-9] to switch to that timeline", textStyle);

            textStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(0, Screen.height - 90, Screen.width, 30),
                "[N] Create New Timeline (save current progress)", textStyle);

            textStyle.normal.textColor = Color.gray;
            GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 30),
                "[ESC] Close Menu", textStyle);
        }
    }
    void DrawPhaseIndicator()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;

        string phaseText = currentPhase switch
        {
            GamePhase.Planning => "PLANNING - Hold WASD to preview, SPACE to commit",
            GamePhase.Executing => "EXECUTING...",
            GamePhase.Complete => "Turn complete",
            _ => ""
        };

        GUI.Label(new Rect(10, 50, 600, 30), phaseText, style);
    }

    void DrawPlanningHints()
    {
        int yOffset = 80;

        // Check for nearby interactables
        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player != null)
        {
            Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            foreach (Interactable obj in interactables)
            {
                if (obj.IsConsumed()) continue;

                float dist = Vector2Int.Distance(player.GetGridPosition(), obj.GetGridPosition());

                if (dist <= 1.5f)
                {
                    GUIStyle hintStyle = new GUIStyle();
                    hintStyle.fontSize = 18;
                    hintStyle.normal.textColor = Color.cyan;

                    GUI.Label(new Rect(10, yOffset, 400, 30),
                        $"[E] {obj.GetInteractionPreview()}", hintStyle);
                    yOffset += 30;
                    break; // Only show one interaction at a time
                }
            }
        }

        // Future vision hint
        GUIStyle previewStyle = new GUIStyle();
        previewStyle.fontSize = 18;
        previewStyle.normal.textColor = GhostPreview.Instance != null && GhostPreview.Instance.showingPreview
            ? Color.cyan
            : Color.gray;

        string previewText = GhostPreview.Instance != null && GhostPreview.Instance.showingPreview
            ? "[TAB] Hide Future Vision"
            : "[TAB] Show Future Vision";

        GUI.Label(new Rect(10, yOffset, 400, 30), previewText, previewStyle);
    }

    void DrawGameOverScreen()
    {
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bigStyle = new GUIStyle();
        bigStyle.fontSize = 48;
        bigStyle.normal.textColor = Color.red;
        bigStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100), "DETECTED", bigStyle);

        GUIStyle smallStyle = new GUIStyle();
        smallStyle.fontSize = 24;
        smallStyle.normal.textColor = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;

        int timelineID = TimelineManager.Instance != null ? TimelineManager.Instance.activeTimelineID : 0;

        GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 50),
            $"Timeline {timelineID}\n[R] Restart Timeline  |  [T] Switch Timeline", smallStyle);
    }

    void DrawVictoryScreen()
    {
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bigStyle = new GUIStyle();
        bigStyle.fontSize = 48;
        bigStyle.normal.textColor = Color.green;
        bigStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, Screen.height / 2 - 100, Screen.width, 60), "TIMELINE COMPLETE!", bigStyle);

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 20;
        textStyle.normal.textColor = Color.white;
        textStyle.alignment = TextAnchor.MiddleCenter;

        int turns = TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0;
        int timelineID = TimelineManager.Instance != null ? TimelineManager.Instance.activeTimelineID : 0;

        GUI.Label(new Rect(0, Screen.height / 2 - 30, Screen.width, 30),
            $"Timeline {timelineID} completed in {turns} turns!", textStyle);

        // Count incomplete timelines
        int incompleteCount = 0;
        bool allComplete = true;
        if (TimelineManager.Instance != null)
        {
            foreach (var timeline in TimelineManager.Instance.GetAllTimelines())
            {
                if (!timeline.isComplete && !timeline.isFrozen)
                {
                    allComplete = false;
                    incompleteCount++;
                }
            }
        }

        textStyle.fontSize = 18;
        textStyle.normal.textColor = Color.gray;

        if (allComplete)
        {
            GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 30),
                "All timelines complete!", textStyle);
        }
        else
        {
            GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 30),
                $"{incompleteCount} frozen timeline(s) remaining", textStyle);
        }

        // Options
        textStyle.fontSize = 22;
        textStyle.normal.textColor = Color.white;

        string options = "[T] Switch Timeline  |  [N] New Timeline  |  [R] Restart All";

        if (allComplete)
        {
            options = "[L] Next Level  |  " + options;
        }

        GUI.Label(new Rect(0, Screen.height / 2 + 60, Screen.width, 30), options, textStyle);
    }


}