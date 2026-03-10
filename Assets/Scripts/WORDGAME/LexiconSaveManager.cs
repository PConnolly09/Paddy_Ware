using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class WordRecord
{
    public string word;
    public int playCount;
    public double highestScore; // NEW: Personal Best Tracker
}

[Serializable]
public class LexiconSaveData
{
    public List<WordRecord> lifetimeWords = new();
    public List<string> unlockedManuscripts = new();
    public int dataCores = 0;
    public int bonusStartingQueries = 0;
    public int bonusStartingD20s = 0;
}

public class LexiconSaveManager : MonoBehaviour
{
    public static LexiconSaveManager Instance { get; private set; }

    public LexiconSaveData currentData;
    private string _saveFilePath;

    public readonly int favoriteThreshold = 5;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _saveFilePath = Application.persistentDataPath + "/LexiconBreachSave.json";
        LoadGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(_saveFilePath, json);
    }

    public void LoadGame()
    {
        if (File.Exists(_saveFilePath))
        {
            string json = File.ReadAllText(_saveFilePath);
            currentData = JsonUtility.FromJson<LexiconSaveData>(json);
            if (currentData.unlockedManuscripts == null) currentData.unlockedManuscripts = new List<string>();
        }
        else
        {
            currentData = new LexiconSaveData();
        }
    }

    public int GetWordPlayCount(string word)
    {
        WordRecord record = currentData.lifetimeWords.Find(w => w.word == word.ToUpper());
        return record != null ? record.playCount : 0;
    }

    public double GetWordHighestScore(string word)
    {
        WordRecord record = currentData.lifetimeWords.Find(w => w.word == word.ToUpper());
        return record != null ? record.highestScore : 0;
    }

    // NEW: Now returns whether this broke a personal best!
    public void RecordWordPlay(string word, double score, out bool isNewHighScore)
    {
        isNewHighScore = false;
        string upper = word.ToUpper();
        WordRecord record = currentData.lifetimeWords.Find(w => w.word == upper);

        if (record != null)
        {
            record.playCount++;
            if (score > record.highestScore)
            {
                record.highestScore = score;
                isNewHighScore = true;
            }
        }
        else
        {
            currentData.lifetimeWords.Add(new WordRecord { word = upper, playCount = 1, highestScore = score });
            isNewHighScore = true; // First time playing is a guaranteed high score!
        }

        SaveGame();
    }
}