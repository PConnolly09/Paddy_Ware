// CameraController.cs - NEW SCRIPT
using UnityEngine;

public class CameraController : MonoBehaviour
{
    void Start()
    {
        AdjustCameraToGrid();
    }

    void AdjustCameraToGrid()
    {
        GridManager grid = FindAnyObjectByType<GridManager>();
        if (grid == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Center camera on grid
        float centerX = (grid.gridWidth - 1) * grid.tileSize / 2f;
        float centerY = (grid.gridHeight - 1) * grid.tileSize / 2f;

        cam.transform.position = new Vector3(centerX, centerY, -10);

        // Adjust orthographic size to fit grid
        float gridAspect = (float)grid.gridWidth / grid.gridHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        float padding = 1.5f; // Adjust this for more/less border space

        if (screenAspect >= gridAspect)
        {
            // Screen is wider, fit height
            cam.orthographicSize = (grid.gridHeight * grid.tileSize) / 2f + padding;
        }
        else
        {
            // Screen is taller, fit width
            float targetHeight = (grid.gridWidth * grid.tileSize) / screenAspect;
            cam.orthographicSize = targetHeight / 2f + padding;
        }

        Debug.Log($"Camera adjusted for {grid.gridWidth}x{grid.gridHeight} grid");
    }
}