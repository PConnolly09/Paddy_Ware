// TurnAction.cs - NEW SCRIPT
using UnityEngine;

[System.Serializable]
public class TurnAction
{
    public int turnNumber;
    public Vector2Int startPosition;
    public Vector2Int endPosition;
    public ActionType actionType;
    public string interactionTarget; // ID of object interacted with
}

public enum ActionType
{
    Move,
    Wait,
    Interact
}