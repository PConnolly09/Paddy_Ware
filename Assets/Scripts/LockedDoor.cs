// LockedDoor.cs - NEW SCRIPT
using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    public Vector2Int gridPosition;
    public PressurePlate plateA;
    public PressurePlate plateB;

    private bool isOpen = false;

    void Start()
    {
        // Visual representation
        GetComponent<SpriteRenderer>().color = Color.red;
    }

    void Update()
    {
        if (!isOpen && plateA != null && plateA.IsBothPlatesPressed())
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        Debug.Log("Door opened!");

        GetComponent<SpriteRenderer>().color = Color.green;

        // Make walkable
        GridManager grid = FindAnyObjectByType<GridManager>();
        if (grid != null)
        {
            grid.MakeWalkable(gridPosition);
        }

        gameObject.SetActive(false);
    }
}