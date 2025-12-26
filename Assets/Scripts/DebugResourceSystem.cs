// DebugResourceSystem.cs - Attach to any GameObject
using UnityEngine;

public class DebugResourceSystem : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugResources();
        }
    }

    void DebugResources()
    {
        Debug.Log("=== RESOURCE DEBUG ===");

        if (RunRecorder.Instance == null)
        {
            Debug.Log("RunRecorder: NULL");
        }
        else
        {
            Debug.Log($"RunRecorder: {RunRecorder.Instance.GetRunCount()} runs");
            var consumed = RunRecorder.Instance.GetAllConsumedResources();
            Debug.Log($"Consumed resources: {consumed.Count}");
            foreach (var id in consumed)
            {
                Debug.Log($"  - {id}");
            }
        }

        Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        Debug.Log($"Scene interactables: {interactables.Length}");
        foreach (var obj in interactables)
        {
            Debug.Log($"  - {obj.interactableID} (consumed: {obj.IsConsumed()}, active: {obj.gameObject.activeSelf})");
        }
    }
}