// Interactable.cs - NEW SCRIPT
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string interactableID; // Unique identifier like "barrel_1"
    public Vector2Int gridPosition;
    public bool isConsumed = false;

    // Override in child classes
    public abstract void Interact(PlayerGridMovement player);

    // Called when player plans to interact
    public abstract string GetInteractionPreview();

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public bool IsConsumed()
    {
        return isConsumed;
    }

    public void Consume()
    {
        isConsumed = true;
        gameObject.SetActive(false); // Hide it
    }
}