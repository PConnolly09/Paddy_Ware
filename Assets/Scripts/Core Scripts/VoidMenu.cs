using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class VoidMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;
    public Transform listContainer;
    public GameObject buttonPrefab; // Assign a button that has a TextMeshProUGUI child

    void Start()
    {
        menuPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        menuPanel.SetActive(true);
        RefreshList();
        // Optional: Pause game here
        Time.timeScale = 0;
    }

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
        Time.timeScale = 1;
    }

    void RefreshList()
    {
        // Clear old buttons
        foreach (Transform child in listContainer) Destroy(child.gameObject);

        // Get all active clones from history
        List<CloneData> history = TimelineManager.Instance.GetHistory();

        if (history.Count == 0)
        {
            // Handle empty history (nothing to sacrifice)
            return;
        }

        foreach (var data in history)
        {
            GameObject btn = Instantiate(buttonPrefab, listContainer);

            string archName = data.archetype != null ? data.archetype.className : "Neutral";
            string label = $"Day {data.originalDayNumber}: {archName} (STR: {data.stats.strength})";

            // Set Text
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = label;
            else Debug.LogWarning("Button Prefab missing TMP component");

            // Add click listener
            btn.GetComponent<Button>().onClick.AddListener(() => OnSelectClone(data));
        }
    }

    void OnSelectClone(CloneData data)
    {
        // 1. Prepare the Draft
        DraftSystem.Instance.PrepareSacrificeDraft(data);

        // 2. Delete the history
        TimelineManager.Instance.DeleteHistory(data.originalDayNumber);

        // 3. Trigger End Day (which will pick up the draft)
        // Resume time first
        Time.timeScale = 1;
        TimelineManager.Instance.EndDay();

        CloseMenu();
    }
}