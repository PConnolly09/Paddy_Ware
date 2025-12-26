// GameStateManager.cs - NEW SCRIPT
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Planning,    // Player is thinking, can preview
    Executing,   // Turn is executing (animations play)
    Complete,     // Turn done, ready for next
    GameOver,    // NEW: Game over state
    Victory
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
        Debug.Log("=== GameStateManager Start ===");

        if (RunRecorder.Instance == null)
        {
            Debug.LogError("RunRecorder.Instance is NULL!");
        }
        else
        {
            Debug.Log($"RunRecorder found, run count: {RunRecorder.Instance.GetRunCount()}");
        }
        // Spawn ghosts for previous runs
        SpawnGhosts();

        // Start first run
        if (RunRecorder.Instance != null)
        {
            RunRecorder.Instance.StartNewRun();
        }
        // Disable consumed resources
        DisableConsumedResources();
    }


    void Update()
    {
        if (currentPhase == GamePhase.GameOver)
        {
            // Only allow restart
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartLevel();
            }
            return; // Don't process any other input
        }

        if (currentPhase == GamePhase.Victory)
        {
            if (canProgressToNextLevel && Input.GetKeyDown(KeyCode.L))
            {
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.CompleteLevel();
                }
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                StartNextRun();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RestartFromBeginning();
            }
            return;
        }
        // Only allow input if not game over - UPDATED
        if (currentPhase == GamePhase.Planning && Input.GetKeyDown(KeyCode.Space))
        {
            CommitTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }

        if (currentPhase == GamePhase.Complete)
        {
            currentPhase = GamePhase.Planning;
        }

        // Don't allow planning if game over
    }

    void SpawnGhosts()
    {
        Debug.Log("=== SpawnGhosts called ===");

        if (RunRecorder.Instance == null)
        {
            Debug.LogError("RunRecorder.Instance is NULL in SpawnGhosts");
            return;
        }

        int runCount = RunRecorder.Instance.GetRunCount();
        Debug.Log($"Run count: {runCount}");

        if (runCount == 0)
        {
            Debug.Log("First run, no ghosts to spawn");
            return;
        }

        Debug.Log($"Spawning {runCount} ghosts from previous runs");

        for (int i = 1; i <= runCount; i++)
        {
            RunData run = RunRecorder.Instance.GetRun(i);

            if (run == null)
            {
                Debug.LogError($"Run {i} is NULL!");
                continue;
            }

            Debug.Log($"Run {i}: completed={run.completed}, actions={run.actions.Count}");

            if (run.completed)
            {
                SpawnGhost(run);
            }
            else
            {
                Debug.LogWarning($"Run {i} not completed, skipping ghost spawn");
            }
        }
    }

    void SpawnGhost(RunData runData)
    {
        Debug.Log($"=== SpawnGhost for Run #{runData.runNumber} ===");

        if (ghostPrefab == null)
        {
            Debug.LogError("ghostPrefab is NULL! Assign it in GameStateManager inspector!");
            return;
        }

        Debug.Log("Instantiating ghost prefab...");
        GameObject ghostObj = Instantiate(ghostPrefab);

        Debug.Log("Getting GhostPlayer component...");
        GhostPlayer ghost = ghostObj.GetComponent<GhostPlayer>();

        if (ghost == null)
        {
            Debug.LogError("Ghost prefab doesn't have GhostPlayer component!");
            Destroy(ghostObj);
            return;
        }

        Debug.Log("Setting ghost runData...");
        ghost.runData = runData;
        activeGhosts.Add(ghost);

        Debug.Log($"Successfully spawned ghost for Run #{runData.runNumber}");
    }

    void DisableConsumedResources()
    {
        if (RunRecorder.Instance == null) return;

        List<string> consumedIDs = RunRecorder.Instance.GetAllConsumedResources();

        Debug.Log($"Disabling {consumedIDs.Count} consumed resources");

        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        foreach (Interactable obj in interactables)
        {
            if (consumedIDs.Contains(obj.interactableID))
            {
                Debug.Log($"Resource {obj.interactableID} already consumed, disabling");
                obj.Consume(); // Hide it
            }
        }
    }

    void RestartFromBeginning()
    {
        Debug.Log("Restarting from beginning...");

        // Clear all runs
        if (RunRecorder.Instance != null)
        {
            Destroy(RunRecorder.Instance.gameObject);
        }

        // Reload scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void SetWin()
    {
        currentPhase = GamePhase.Victory;

        // Complete the run - NEW
        if (RunRecorder.Instance != null && TurnCounter.Instance != null)
        {
            RunRecorder.Instance.CompleteRun(TurnCounter.Instance.GetCurrentTurn());
        }
        // Check if level has win condition (need X runs to unlock next level?) - NEW
        int runCount = RunRecorder.Instance != null ? RunRecorder.Instance.GetRunCount() : 0;

        // Simple: complete 3 runs to unlock next level
        if (runCount >= 3)
        {
            Debug.Log("Level requirements met! Can progress to next level");
            canProgressToNextLevel = true;
        }
        Debug.Log("VICTORY! - Press N for next run or R to restart");
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

        // Reload scene but keep RunRecorder
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        // RunRecorder persists (DontDestroyOnLoad), will start new run on scene load
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
        // Dark overlay
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Big red text
        GUIStyle bigStyle = new GUIStyle();
        bigStyle.fontSize = 48;
        bigStyle.normal.textColor = Color.red;
        bigStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100), "GAME OVER", bigStyle);

        // Restart instruction
        GUIStyle smallStyle = new GUIStyle();
        smallStyle.fontSize = 24;
        smallStyle.normal.textColor = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 50), "Press R to Restart", smallStyle);
    }

    void DrawVictoryScreen()
    {
        // Dark overlay
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Big green text
        GUIStyle bigStyle = new GUIStyle();
        bigStyle.fontSize = 48;
        bigStyle.normal.textColor = Color.green;
        bigStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 100), "VICTORY!", bigStyle);

        // Stats and options
        GUIStyle smallStyle = new GUIStyle();
        smallStyle.fontSize = 20;
        smallStyle.normal.textColor = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;

        int turns = TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0;
        int runCount = RunRecorder.Instance != null ? RunRecorder.Instance.GetRunCount() : 0;

        // Turn count
        GUI.Label(new Rect(0, Screen.height / 2 - 20, Screen.width, 30),
            $"Completed in {turns} turns", smallStyle);

        // Run count
        GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 30),
            $"Run #{runCount} complete", smallStyle);

        // Progress to next level
        int runsNeeded = 3;
        string progressText;

        if (canProgressToNextLevel)
        {
            progressText = "[L] Next Level  |  [N] Next Run  |  [R] Restart All";
        }
        else
        {
            int runsRemaining = runsNeeded - runCount;
            progressText = $"[N] Next Run ({runsRemaining} more for next level)  |  [R] Restart All";
        }

        GUI.Label(new Rect(0, Screen.height / 2 + 50, Screen.width, 30), progressText, smallStyle);
    }

}