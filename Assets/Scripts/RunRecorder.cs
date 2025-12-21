// RunRecorder.cs - NEW SCRIPT
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public int runNumber;
    public List<TurnAction> actions = new List<TurnAction>();
    public List<string> consumedResources = new List<string>(); // IDs of barrels/items used
    public bool completed = false;
    public int turnCount = 0;
}

public class RunRecorder : MonoBehaviour
{
    public static RunRecorder Instance;

    public List<RunData> completedRuns = new List<RunData>();
    private RunData currentRun;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scene reloads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNewRun()
    {
        currentRun = new RunData();
        currentRun.runNumber = completedRuns.Count + 1;

        Debug.Log($"Started Run #{currentRun.runNumber}");
    }

    public void RecordTurn(TurnAction action)
    {
        if (currentRun == null)
        {
            Debug.LogError("No active run to record to!");
            return;
        }

        action.turnNumber = currentRun.actions.Count;
        currentRun.actions.Add(action);

        Debug.Log($"Recorded turn {action.turnNumber}: {action.actionType} from {action.startPosition} to {action.endPosition}");
    }

    public void RecordResourceConsumption(string resourceID)
    {
        if (currentRun == null) return;

        if (!currentRun.consumedResources.Contains(resourceID))
        {
            currentRun.consumedResources.Add(resourceID);
            Debug.Log($"Recorded resource consumption: {resourceID}");
        }
    }

    public void CompleteRun(int finalTurnCount)
    {
        if (currentRun == null) return;

        currentRun.completed = true;
        currentRun.turnCount = finalTurnCount;
        completedRuns.Add(currentRun);

        Debug.Log($"Run #{currentRun.runNumber} completed in {finalTurnCount} turns");
        Debug.Log($"Consumed resources: {string.Join(", ", currentRun.consumedResources)}");

        currentRun = null;
    }

    public RunData GetRun(int runNumber)
    {
        if (runNumber <= 0 || runNumber > completedRuns.Count) return null;
        return completedRuns[runNumber - 1];
    }

    public int GetRunCount()
    {
        return completedRuns.Count;
    }

    public List<string> GetAllConsumedResources()
    {
        List<string> allConsumed = new List<string>();

        foreach (RunData run in completedRuns)
        {
            foreach (string resource in run.consumedResources)
            {
                if (!allConsumed.Contains(resource))
                {
                    allConsumed.Add(resource);
                }
            }
        }

        return allConsumed;
    }
}