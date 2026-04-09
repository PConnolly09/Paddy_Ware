using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class LetterStat
{
    public string faceText;
    public int timesMutated;
}

[Serializable]
public class WordPlayData
{
    public string word;
    public int playCount;
    public double highScore;
}

[Serializable]
public class LexiconSaveData
{
    public int dataCores = 0;
    public int bonusStartingQueries = 0;
    public int bonusStartingD20s = 0;

    public List<LetterStat> letterStats = new List<LetterStat>();
    public List<WordPlayData> wordPlayHistory = new List<WordPlayData>();

    // NEW: Persistent list of everything you have unlocked!
    public List<string> unlockedRelics = new List<string>();
    public List<string> completedTomes = new List<string>();
    public Dictionary<string, int> lifetimeWordPlays = new Dictionary<string, int>();
}

public class LexiconSaveManager : MonoBehaviour
{
    public static LexiconSaveManager Instance { get; private set; }

    public LexiconSaveData currentData = new LexiconSaveData();
    public int favoriteThreshold = 10;

    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        saveFilePath = Application.persistentDataPath + "/lexicon_save.json";
        LoadGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentData);
        File.WriteAllText(saveFilePath, json);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<LexiconSaveData>(json);
        }
    }

    // ==========================================
    // NEW: RELIC UNLOCK LOGIC
    // ==========================================
    public bool IsRelicUnlocked(string unlockID)
    {
        return currentData.unlockedRelics.Contains(unlockID);
    }

    public void UnlockRelic(string unlockID, string relicName)
    {
        if (!currentData.unlockedRelics.Contains(unlockID))
        {
            currentData.unlockedRelics.Add(unlockID);
            SaveGame();

            if (WordUIManager.Instance != null)
            {
                WordUIManager.Instance.ShowTransientMessage($"<color=#FFD700>NEW RELIC UNLOCKED: {relicName}!</color>");
            }
        }
    }

    // ==========================================
    // EXISTING: WORD PLAY & HIGHSCORE TRACKING
    // ==========================================
    public int GetWordPlayCount(string word)
    {
        var wordData = currentData.wordPlayHistory.FirstOrDefault(w => w.word.Equals(word, StringComparison.OrdinalIgnoreCase));
        return wordData != null ? wordData.playCount : 0;
    }

    public void RecordWordPlay(string word, double score, out bool isNewHigh)
    {
        isNewHigh = false;
        var wordData = currentData.wordPlayHistory.FirstOrDefault(w => w.word.Equals(word, StringComparison.OrdinalIgnoreCase));

        if (wordData == null)
        {
            wordData = new WordPlayData { word = word.ToUpper(), playCount = 1, highScore = score };
            currentData.wordPlayHistory.Add(wordData);
            isNewHigh = true;
        }
        else
        {
            wordData.playCount++;
            if (score > wordData.highScore)
            {
                wordData.highScore = score;
                isNewHigh = true;
            }
        }
        SaveGame();
    }

    // ==========================================
    // EXISTING: MUTATION TRACKING
    // ==========================================
    public void RecordMutation(string letter)
    {
        string upperLetter = letter.ToUpper();
        var stat = currentData.letterStats.FirstOrDefault(l => l.faceText == upperLetter);

        if (stat == null)
        {
            stat = new LetterStat { faceText = upperLetter, timesMutated = 1 };
            currentData.letterStats.Add(stat);
        }
        else
        {
            stat.timesMutated++;
        }
        SaveGame();
    }
}