using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

[Serializable]
public class GameData
{
    public double totalHype = 0;
    public int creds = 100;
    public string lastExitTime = "";

    public List<BotSaveData> activeBots = new();

    public List<string> unlockedPowerWords = new();
    public List<string> unlockedAchievements = new();

    public int lifetimeLiquidations = 0;
    public double lifetimeHypeMined = 0;
}

[Serializable]
public class BotSaveData
{
    public string keyword;
    public int buyInCostCreds;
    public double totalHypeMined;
    public string targetSubreddit;

    // NEW: Added the history array for the line graph to serialize
    public List<int> hypeHistory;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public GameData currentData;
    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        saveFilePath = Application.persistentDataPath + "/IdleBrokerSave.json";
        LoadGame();
    }

    public void SaveGame()
    {
        currentData.lastExitTime = DateTime.Now.ToBinary().ToString();
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(saveFilePath, json);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<GameData>(json);

            // Safety check: ensure arrays aren't null if loading an old save file
            if (currentData.activeBots == null) currentData.activeBots = new List<BotSaveData>();
            foreach (var bot in currentData.activeBots)
            {
                if (bot.hypeHistory == null) bot.hypeHistory = new List<int>();
            }
        }
        else
        {
            currentData = new GameData();
        }
    }

    private void OnApplicationQuit() { SaveGame(); }
    private void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveGame(); }
}