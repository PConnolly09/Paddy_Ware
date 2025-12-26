// TurnCounter.cs
using UnityEngine;

public class TurnCounter : MonoBehaviour
{
    public static TurnCounter Instance;
    private int currentTurn = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void IncrementTurn()
    {
        currentTurn++;

    }

    void OnGUI()
    {
        // Draw turn counter in top-left corner
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 200, 30), $"Turn: {currentTurn}", style);
    }

    public int GetCurrentTurn()
    {
        return currentTurn;
    }
}