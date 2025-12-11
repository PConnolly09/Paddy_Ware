using UnityEngine;

public class GridManager : MonoBehaviour
{

    
    [Header ("Grid Settings")]
    public float gridWidth = 8f;
    public float gridHeight = 8f;
    public GameObject cellPrefab;
    private GameObject[,] gridCells;
    private float cellSize = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        int cellsX = Mathf.CeilToInt(gridWidth / cellSize);
        int cellsY = Mathf.CeilToInt(gridHeight / cellSize);
        gridCells = new GameObject[cellsX, cellsY];
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize,0);
                GameObject cell = Instantiate(cellPrefab, cellPosition, Quaternion.identity);
                cell.transform.parent = this.transform;
                gridCells[x, y] = cell;
            }
        }
    }
}
