// GameStateManager.cs - NEW SCRIPT
using UnityEngine;

public enum GamePhase
{
    Planning,    // Player is thinking, can preview
    Executing,   // Turn is executing (animations play)
    Complete,     // Turn done, ready for next
    GameOver     // NEW: Game over state
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public GamePhase currentPhase = GamePhase.Planning;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
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

    void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    void CommitTurn()
    {
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
            _ => ""
        };

        GUI.Label(new Rect(10, 50, 600, 30), phaseText, style);

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