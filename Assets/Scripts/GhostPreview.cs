// GhostPreview.cs - NEW SCRIPT
using UnityEngine;
using System.Collections.Generic;

public class GhostPreview : MonoBehaviour
{
    public static GhostPreview Instance;

    public GameObject ghostPreviewPrefab; // Very transparent ghost sprite
    public bool showingPreview = false;
    public int turnsToPreview = 5;

    private List<GameObject> previewObjects = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Toggle preview with TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showingPreview = !showingPreview;

            if (showingPreview)
            {
                ShowGhostPreviews();
            }
            else
            {
                HideGhostPreviews();
            }
        }
    }

    void ShowGhostPreviews()
    {
        if (RunRecorder.Instance == null) return;

        int currentTurn = TurnCounter.Instance != null ? TurnCounter.Instance.GetCurrentTurn() : 0;
        int runCount = RunRecorder.Instance.GetRunCount();

        // For each completed run, show where ghosts will be
        for (int runNum = 1; runNum <= runCount; runNum++)
        {
            RunData run = RunRecorder.Instance.GetRun(runNum);
            if (run == null || !run.completed) continue;

            // Show next N turns for this ghost
            for (int turnOffset = 0; turnOffset < turnsToPreview; turnOffset++)
            {
                int futureTurn = currentTurn + turnOffset;

                if (futureTurn >= run.actions.Count) break; // Ghost finished

                TurnAction action = run.actions[futureTurn];
                Vector2Int position = action.endPosition;

                // Create preview sprite
                GameObject preview = Instantiate(ghostPreviewPrefab);
                preview.transform.position = new Vector3(position.x, position.y, 0);

                // Fade out for further turns
                SpriteRenderer sr = preview.GetComponent<SpriteRenderer>();
                float alpha = 1f - (turnOffset / (float)turnsToPreview);
                if (sr != null)
                {

                    Color color = sr.color;
                    color.a = alpha * 0.3f; // Very transparent
                    sr.color = color;
                }

                // Add turn number label
                TextMesh label = preview.AddComponent<TextMesh>();
                label.text = $"T+{turnOffset}";
                label.fontSize = 20;
                label.characterSize = 0.05f;
                label.anchor = TextAnchor.MiddleCenter;
                label.color = new Color(1, 1, 1, alpha * 0.5f);

                previewObjects.Add(preview);
            }
        }

        Debug.Log($"Showing preview for next {turnsToPreview} turns");
    }

    void HideGhostPreviews()
    {
        foreach (GameObject obj in previewObjects)
        {
            Destroy(obj);
        }
        previewObjects.Clear();
    }

    void OnDestroy()
    {
        HideGhostPreviews();
    }
}