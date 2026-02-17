using UnityEngine;

public class SacrificeAltar : Interactable
{
    private VoidMenu voidMenu;

    void Start()
    {
        // Important: Set type to None so checking logic doesn't try to 'Chop' it
        type = InteractionType.None;

        // Find the UI
        voidMenu = FindAnyObjectByType<VoidMenu>();

        // Initialize base
        ResetState();
    }

    // We add a custom method for the PlayerController to call
    public void OpenAltar()
    {
        if (voidMenu != null)
        {
            Debug.Log("ALTAR: Opening Void Menu...");
            voidMenu.OpenMenu();
        }
        else
        {
            Debug.LogError("ALTAR: VoidMenu not found in scene!");
        }
    }
}