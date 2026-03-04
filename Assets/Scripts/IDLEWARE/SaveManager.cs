using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

// 1. The Data Containers
[Serializable]
public class GameData
{
    public double totalHype = 0;
    public int creds = 0; // FIXED: Creds now save to the hard drive!
    public string lastExitTime = "";
    public List<BotSaveData> activeBots = new List<BotSaveData>();
}

[Serializable]
public class BotSaveData
{
    public string keyword;
    public int level;
    // Add other stats you want to save here (e.g., custom UI colors, multipliers)
}

// 2. The Save Manager
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public GameData currentData;
    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        // This points to a safe, persistent folder on Windows, Mac, iOS, or Android
        saveFilePath = Application.persistentDataPath + "/IdleCrawlerSave.json";
        LoadGame();
    }

    public void SaveGame()
    {
        // Update the exit time right before saving
        currentData.lastExitTime = DateTime.Now.ToBinary().ToString();

        string json = JsonUtility.ToJson(currentData, true); // 'true' formats it nicely
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved to: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<GameData>(json);
            Debug.Log("Save file loaded successfully.");
        }
        else
        {
            Debug.Log("No save file found. Creating new game profile.");
            currentData = new GameData();

            // Give them a starter bot!
            currentData.activeBots.Add(new BotSaveData { keyword = "gaming", level = 1 });
        }
    }

    // Auto-save when the app is closed or backgrounded
    private void OnApplicationQuit() { SaveGame(); }
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }
}