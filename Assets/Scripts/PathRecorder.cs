// PathRecorder.cs - NEW SCRIPT
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyPathData
{
    public string enemyID;
    public List<Vector2Int> waypoints = new List<Vector2Int>();
}

public class PathRecorder : MonoBehaviour
{
    public static PathRecorder Instance;

    private Dictionary<string, List<Vector2Int>> recordedPaths = new Dictionary<string, List<Vector2Int>>();
    private bool isRecording = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartRecording()
    {
        isRecording = true;
        Debug.Log("PathRecorder: Started recording enemy paths");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"PathRecorder: Stopped recording. Have {recordedPaths.Count} enemy paths");
    }

    public void RecordWaypoint(string enemyID, Vector2Int waypoint)
    {
        if (!isRecording) return;

        if (!recordedPaths.ContainsKey(enemyID))
        {
            recordedPaths[enemyID] = new List<Vector2Int>();
        }

        recordedPaths[enemyID].Add(waypoint);
    }

    public List<Vector2Int> GetRecordedPath(string enemyID)
    {
        if (recordedPaths.ContainsKey(enemyID))
        {
            return new List<Vector2Int>(recordedPaths[enemyID]);
        }
        return null;
    }

    public bool HasRecordedPath(string enemyID)
    {
        return recordedPaths.ContainsKey(enemyID) && recordedPaths[enemyID].Count > 0;
    }

    public void Clear()
    {
        recordedPaths.Clear();
    }
}