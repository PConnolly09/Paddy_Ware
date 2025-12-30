using UnityEngine;
using System.Collections.Generic;

public class GhostPreview : MonoBehaviour
{
    public static GhostPreview Instance;

    public bool showingPreview = false;
    public int turnsToPreview = 5;

    private List<GameObject> previewObjects = new List<GameObject>();
    private bool needsRefresh = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Only in planning phase
        if (GameStateManager.Instance == null ||
            GameStateManager.Instance.currentPhase != GamePhase.Planning)
        {
            if (showingPreview)
            {
                HideGhostPreviews();
            }
            return;
        }

        // Toggle with TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showingPreview = !showingPreview;
            needsRefresh = true;
        }

        // Refresh preview when needed
        if (needsRefresh)
        {
            if (showingPreview)
            {
                ShowGhostPreviews();
            }
            else
            {
                HideGhostPreviews();
            }
            needsRefresh = false;
        }
    }

    // Replace GhostPreview.cs ShowGhostPreviews() entirely:
    void ShowGhostPreviews()
    {
        HideGhostPreviews();

        if (TimelineManager.Instance == null || TurnCounter.Instance == null)
        {
            Debug.LogWarning("Can't show preview - missing TimelineManager or TurnCounter");
            showingPreview = false;
            return;
        }

        TimelineManager.TimelineState activeTimeline = TimelineManager.Instance.GetActiveTimeline();
        if (activeTimeline == null)
        {
            Debug.LogWarning("No active timeline");
            showingPreview = false;
            return;
        }

        int currentTurn = TurnCounter.Instance.GetCurrentTurn();

        // Get all OTHER timelines to preview
        List<TimelineManager.TimelineState> otherTimelines = new List<TimelineManager.TimelineState>();
        foreach (var timeline in TimelineManager.Instance.GetAllTimelines())
        {
            if (timeline.timelineID != activeTimeline.timelineID && timeline.playerActions.Count > 0)
            {
                otherTimelines.Add(timeline);
            }
        }

        if (otherTimelines.Count == 0)
        {
            Debug.Log("No other timelines to preview");
            showingPreview = false;
            return;
        }

        Debug.Log($"Previewing {otherTimelines.Count} other timelines from turn {currentTurn}");

        Color[] runColors = new Color[] {
        new Color(1f, 0.5f, 0.5f, 0.4f),
        new Color(0.5f, 0.5f, 1f, 0.4f),
        new Color(0.5f, 1f, 0.5f, 0.4f),
        new Color(1f, 1f, 0.5f, 0.4f),
    };

        int colorIndex = 0;
        foreach (var timeline in otherTimelines)
        {
            Color runColor = runColors[colorIndex % runColors.Length];
            CreatePreviewForTimeline(timeline, currentTurn, runColor);
            colorIndex++;
        }

        Debug.Log($"Created {previewObjects.Count} preview objects");
    }

    void CreatePreviewForTimeline(TimelineManager.TimelineState timeline, int currentTurn, Color baseColor)
    {
        // Show next N actions from this timeline
        for (int i = 0; i < turnsToPreview; i++)
        {
            int actionIndex = currentTurn + i;

            if (actionIndex >= timeline.playerActions.Count)
            {
                // This timeline doesn't have actions this far ahead
                break;
            }

            TurnAction action = timeline.playerActions[actionIndex];
            Vector2Int position = action.endPosition;

            CreatePreviewSprite(position, i, timeline.timelineID, baseColor);
        }
    }


    void CreatePreviewSprite(Vector2Int position, int turnOffset, int runNum, Color baseColor)
    {
        // Create quad
        GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        preview.name = $"Preview_R{runNum}_T+{turnOffset}";
        preview.transform.position = new Vector3(position.x, position.y, 0);
        preview.transform.localScale = Vector3.one * 0.6f; // Smaller than tiles

        // Fade based on distance
        float alpha = 1f - (turnOffset / (float)turnsToPreview);
        Color color = baseColor;
        color.a = alpha * 0.5f;

        Renderer renderer = preview.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            renderer.material = mat;
            renderer.sortingOrder = 4;
        }

        // Remove collider
        Collider collider = preview.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // Label
        GameObject labelObj = new GameObject($"Label_R{runNum}_T+{turnOffset}");
        labelObj.transform.SetParent(preview.transform);
        labelObj.transform.localPosition = Vector3.zero;

        TextMesh label = labelObj.AddComponent<TextMesh>();
        label.text = $"+{turnOffset}"; // Just show turn offset
        label.fontSize = 30;
        label.characterSize = 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1, 1, 1, alpha);

        MeshRenderer labelRenderer = labelObj.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = 5;
        }

        // Add to tracking list
        previewObjects.Add(preview);
    }

    void HideGhostPreviews()
    {
        Debug.Log($"Destroying {previewObjects.Count} preview objects");

        // Destroy all tracked objects
        foreach (GameObject obj in previewObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        previewObjects.Clear();
        showingPreview = false;
    }

    void OnDisable()
    {
        HideGhostPreviews();
    }

    void OnDestroy()
    {
        HideGhostPreviews();
    }
}