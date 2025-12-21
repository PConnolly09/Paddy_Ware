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
            if (Input.GetKeyDown(KeyCode.N)) // NEW - next run
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
        OnTurnExecutionComplete();

    }

    public void OnTurnExecutionComplete()
    {
        // DON'T change phase if game over
        if (currentPhase == GamePhase.GameOver)
        {
            Debug.Log("Turn complete but game is over, not changing phase");
            return;
        }
        // Called when all entities finish moving
        currentPhase = GamePhase.Complete;

        if (TurnCounter.Instance != null)
        {
            TurnCounter.Instance.IncrementTurn();
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
        // Phase indicator (existing)
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;

        string phaseText = currentPhase switch
        {
            GamePhase.Planning => "PLANNING - Hold WASD to preview, SPACE to commit",
            GamePhase.Executing => "EXECUTING...",
            GamePhase.Complete => "Turn complete",
            GamePhase.GameOver => "*** GAME OVER *** - Press R to restart",
            GamePhase.Victory => "*** VICTORY *** - Press R to restart", // NEW
            _ => ""
        };

        GUI.Label(new Rect(10, 50, 600, 30), phaseText, style);

        // Victory screen - NEW
        if (currentPhase == GamePhase.Victory)
        {
            GUIStyle bigStyle = new GUIStyle();
            bigStyle.fontSize = 48;
            bigStyle.normal.textColor = Color.green;
            bigStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100), "VICTORY!", bigStyle);

            GUIStyle smallStyle = new GUIStyle();
            smallStyle.fontSize = 24;
            smallStyle.normal.textColor = Color.white;
            smallStyle.alignment = TextAnchor.MiddleCenter;

            // Show turn count
            int turns = TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0;
            GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 50),
                $"Completed in {turns} turns\nPress R to Restart", smallStyle);

            int runCount = RunRecorder.Instance != null ? RunRecorder.Instance.GetRunCount() : 0;
 
            string message = runCount == 0
                ? $"Run 1 completed in {turns} turns\n[N] Next Run  [R] Restart"
                : $"Run {runCount + 1} completed in {turns} turns\n[N] Next Run  [R] Restart All";

            GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 80), message, smallStyle);
        }

        // Show interaction hint - NEW
        if (currentPhase == GamePhase.Planning)
        {
            PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
            if (player != null)
            {
                // Check for nearby interactables
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

                        GUI.Label(new Rect(10, 80, 400, 30),
                            $"[E] {obj.GetInteractionPreview()}", hintStyle);
                        break;
                    }
                }
            }

            // Add preview hint - NEW
            GUIStyle previewStyle = new GUIStyle();
            previewStyle.fontSize = 18;
            previewStyle.normal.textColor = GhostPreview.Instance != null && GhostPreview.Instance.showingPreview
                ? Color.cyan
                : Color.gray;

            string previewText = GhostPreview.Instance != null && GhostPreview.Instance.showingPreview
                ? "[TAB] Hide Future Vision"
                : "[TAB] Show Future Vision";

            GUI.Label(new Rect(10, 110, 400, 30), previewText, previewStyle);
        }




        // BIG GAME OVER SCREEN (new)
        if (currentPhase == GamePhase.GameOver)
        {
            // Semi-transparent dark overlay
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            // Big red text
            GUIStyle bigStyle = new GUIStyle();
            bigStyle.fontSize = 48;
            bigStyle.normal.textColor = Color.red;
            bigStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100), "GAME OVER", bigStyle);

            // Smaller restart instruction
            GUIStyle smallStyle = new GUIStyle();
            smallStyle.fontSize = 24;
            smallStyle.normal.textColor = Color.white;
            smallStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 50), "Press R to Restart", smallStyle);
        }
    }

}