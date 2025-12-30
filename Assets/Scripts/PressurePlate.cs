// PressurePlate.cs - NEW SCRIPT
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public Vector2Int gridPosition;
    public float tileSize = 1f;
    public PressurePlate pairedPlate; // Must both be pressed

    private bool isPressed = false;
    private GameObject indicator;

    void Start()
    {
        // Create visual indicator
        indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.transform.position = new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);
        indicator.transform.localScale = Vector3.one * 0.8f;

        Renderer r = indicator.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            r.sortingOrder = 1;
        }

        Collider c = indicator.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    void Update()
    {
        CheckForEntity();
    }

    void CheckForEntity()
    {
        bool wasPressed = isPressed;
        isPressed = false;

        // Check for player
        PlayerGridMovement player = FindAnyObjectByType<PlayerGridMovement>();
        if (player != null && player.GetGridPosition() == gridPosition)
        {
            isPressed = true;
        }

        // Check for ghosts
        GhostPlayer[] ghosts = FindObjectsByType<GhostPlayer>(FindObjectsSortMode.None);
        foreach (GhostPlayer ghost in ghosts)
        {
            if (ghost.GetGridPosition() == gridPosition)
            {
                isPressed = true;
                break;
            }
        }

        // Update visual
        if (indicator != null)
        {
            Renderer r = indicator.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = isPressed ?
                    new Color(0, 1, 0, 0.7f) :
                    new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        // Log state change
        if (wasPressed != isPressed)
        {
            Debug.Log($"Pressure plate at {gridPosition}: {(isPressed ? "PRESSED" : "RELEASED")}");
        }
    }

    public bool IsBothPlatesPressed()
    {
        if (pairedPlate == null) return isPressed;
        return isPressed && pairedPlate.isPressed;
    }
}