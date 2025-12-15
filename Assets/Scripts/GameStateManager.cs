// GameStateManager.cs - NEW SCRIPT
using UnityEngine;

public enum GamePhase
{
    Planning,    // Player is thinking, can preview
    Executing,   // Turn is executing (animations play)
    Complete     // Turn done, ready for next
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
        // Press SPACE to commit turn
        if (currentPhase == GamePhase.Planning && Input.GetKeyDown(KeyCode.Space))
        {
            CommitTurn();
        }

        // Auto-transition from Complete back to Planning
        if (currentPhase == GamePhase.Complete)
        {
            currentPhase = GamePhase.Planning;
        }
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

        // Later: Tell enemies to execute too
        // Later: Tell ghosts to execute too
    }

    public void OnTurnExecutionComplete()
    {
        // Called when all entities finish moving
        currentPhase = GamePhase.Complete;

        if (TurnCounter.Instance != null)
        {
            TurnCounter.Instance.IncrementTurn();
        }
    }

    public bool IsPlanning()
    {
        return currentPhase == GamePhase.Planning;
    }

    void OnGUI()
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

}